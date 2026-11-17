using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// A body the player can put on during the game. Drop any humanoid character into the level (a Mixamo download
    /// works: right-click it > GDD 110 > Make Humanoid first), add this, then walk up and press Interact.
    /// The player's own animation drives the new body every frame, so it runs, jumps and idles like the player.
    /// Press Interact on it again to take it off. Works in builds, unlike the editor's permanent Swap Player Character.
    /// </summary>
    [AddComponentMenu("GDD 110/Functions/Wearable Body")]
    public class GddWearableBody : GddEntity
    {
        public GddCollectible.Activation activation = GddCollectible.Activation.PressInteract;
        public string actionLabel = "Wear";
        [Tooltip("Hide this stand-in while someone is wearing it.")]
        public bool hideWhileWorn = true;
        [Tooltip("Give the stand-in the player's animations so it idles instead of standing in a T-pose.")]
        public bool idleWhileWaiting = true;
        public UnityEvent onWorn = new UnityEvent();
        public UnityEvent onTakenOff = new UnityEvent();

        public bool IsWorn => worn != null;
        static GddWearableBody current;

        Animator body;
        GameObject worn;
        Animator wornAnimator, wearerAnimator;
        GddActor wearer;
        AnimatorCullingMode previousCulling;

        /// <summary>The body this player is wearing right now, or null.</summary>
        public static Animator BodyFor(GddActor actor) =>
            current != null && current.worn != null && current.wearer == actor ? current.wornAnimator : null;
        readonly List<Renderer> hiddenOnPlayer = new List<Renderer>();
        readonly List<Renderer> hiddenHere = new List<Renderer>();
        float cooldown;

        public override string Prompt => IsWorn ? "Take off" : actionLabel;
        public override bool CanInteract(GddActor actor) => activation == GddCollectible.Activation.PressInteract;

        void Start()
        {
            body = GetComponentInChildren<Animator>();
            if (body == null || body.avatar == null || !body.avatar.isHuman)
                Debug.LogWarning("[GDD 110] " + name + ": Wearable Body needs a humanoid character (right-click its model > GDD 110 > Make Humanoid).", this);
            var player = FindAnyObjectByType<GddActor>();
            var playerAnim = player != null ? PlayerAnimator(player) : null;
            if (body != null && idleWhileWaiting && body.runtimeAnimatorController == null && playerAnim != null)
            {
                body.runtimeAnimatorController = playerAnim.runtimeAnimatorController;
                body.applyRootMotion = false;
            }
            if (GetComponentInChildren<Collider>() == null)
            {
                var b = Bounds();
                var cap = gameObject.AddComponent<CapsuleCollider>();
                cap.center = transform.InverseTransformPoint(b.center);
                cap.height = b.size.y / Mathf.Max(0.01f, transform.lossyScale.y);
                cap.radius = Mathf.Max(b.extents.x, b.extents.z) / Mathf.Max(0.01f, transform.lossyScale.x);
                cap.isTrigger = activation == GddCollectible.Activation.TouchIt;
            }
        }

        Bounds Bounds()
        {
            var rs = GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(transform.position, Vector3.one);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        void OnTriggerEnter(Collider other)
        {
            if (activation != GddCollectible.Activation.TouchIt || IsWorn || Time.time < cooldown) return;
            var actor = other.GetComponentInParent<GddActor>();
            if (actor != null) Wear(actor);
        }

        public override void Interact(GddActor actor) { if (IsWorn) TakeOff(); else Wear(actor); }
        public override void Activate() { if (IsWorn) TakeOff(); else Wear(LastActor != null ? LastActor : FindAnyObjectByType<GddActor>()); }

        public void Wear(GddActor actor)
        {
            if (actor == null || body == null || body.avatar == null || !body.avatar.isHuman) return;
            var source = PlayerAnimator(actor);
            if (source == null || source.avatar == null || !source.avatar.isHuman) return;
            if (current != null && current != this) current.TakeOff();
            LastActor = actor;

            // A copy of this body's look, riding on the player.
            worn = Instantiate(body.gameObject);
            worn.name = "Worn Body - " + DisplayName;
            foreach (var mb in worn.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
            foreach (var c in worn.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var rb in worn.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
            Vector3 world = body.transform.lossyScale;
            worn.transform.SetParent(actor.transform, false);
            worn.transform.localPosition = Vector3.zero;
            worn.transform.localRotation = Quaternion.identity;
            var a = actor.transform.lossyScale;
            worn.transform.localScale = new Vector3(world.x / a.x, world.y / a.y, world.z / a.z);
            var wornAnim = worn.GetComponent<Animator>();
            wornAnim.runtimeAnimatorController = null;
            wornAnim.enabled = false;
            foreach (var smr in worn.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
            worn.AddComponent<GddPoseMirror>().Init(source, wornAnim);
            wornAnimator = wornAnim;
            wearer = actor;
            wearerAnimator = source;
            // The player's own meshes get hidden below. An Animator set to cull when unseen would then stop moving
            // its bones, and the worn body would freeze, so keep it animating while a body is worn.
            previousCulling = source.cullingMode;
            source.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            hiddenOnPlayer.Clear();
            foreach (var r in source.GetComponentsInChildren<Renderer>())
                if (r.enabled && !r.transform.IsChildOf(worn.transform) && r.GetComponentInParent<GddCollectible>() == null) { r.enabled = false; hiddenOnPlayer.Add(r); }   // worn items stay visible
            hiddenHere.Clear();
            if (hideWhileWorn) foreach (var r in GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; hiddenHere.Add(r); }

            current = this;
            GddCollectible.Reattach(actor);   // hats, swords and so on move onto the new body
            onWorn.Invoke();
            RaiseActivated();
        }

        static Animator PlayerAnimator(Component actor)
        {
            var motor = actor.GetComponent<StarterAssets.ThirdPersonController>();
            return motor != null && motor.CharacterAnimator != null
                ? motor.CharacterAnimator
                : actor.GetComponent<Animator>();
        }

        public void TakeOff()
        {
            if (worn == null) return;
            var old = worn;
            worn = null;
            if (wearer != null) GddCollectible.Reattach(wearer);   // move worn items back before the body goes away
            Destroy(old);
            if (wearerAnimator != null) wearerAnimator.cullingMode = previousCulling;
            wornAnimator = null; wearerAnimator = null; wearer = null;
            foreach (var r in hiddenOnPlayer) if (r != null) r.enabled = true;
            foreach (var r in hiddenHere) if (r != null) r.enabled = true;
            hiddenOnPlayer.Clear(); hiddenHere.Clear();
            if (current == this) current = null;
            cooldown = Time.time + 1f;
            onTakenOff.Invoke();
        }

        void OnDestroy() { TakeOff(); }
    }

    /// <summary>Copies one humanoid's pose onto another every frame (after animation), at any size or proportions.</summary>
    [AddComponentMenu("")]
    [DefaultExecutionOrder(10000)]
    public class GddPoseMirror : MonoBehaviour
    {
        HumanPoseHandler from, to;
        HumanPose pose;
        Transform sourceRoot, targetRoot, sourceHips, targetHips;
        float ratio = 1f;

        public void Init(Animator source, Animator target)
        {
            from = new HumanPoseHandler(source.avatar, source.transform);
            to = new HumanPoseHandler(target.avatar, target.transform);
            sourceRoot = source.transform;
            targetRoot = target.transform;
            sourceHips = source.GetBoneTransform(HumanBodyBones.Hips);
            targetHips = target.GetBoneTransform(HumanBodyBones.Hips);
            // Hip height of each body (rest pose vs current standing pose), so a taller or shorter body stays on the ground.
            float s = sourceHips != null ? sourceRoot.InverseTransformPoint(sourceHips.position).y : 1f;
            float t = targetHips != null ? targetRoot.InverseTransformPoint(targetHips.position).y : 1f;
            if (s > 0.05f && t > 0.05f) ratio = t / s;
        }

        void LateUpdate()
        {
            if (from == null) return;
            from.GetHumanPose(ref pose);
            to.SetHumanPose(ref pose);
            // Pose copying gets the bones right; pin the hips over the player's hips so the body stays where the player is.
            if (sourceHips != null && targetHips != null)
            {
                Vector3 local = sourceRoot.InverseTransformPoint(sourceHips.position);
                targetHips.position = sourceRoot.TransformPoint(new Vector3(local.x, local.y * ratio, local.z));
            }
        }

        void OnDestroy() { from?.Dispose(); to?.Dispose(); }
    }
}
