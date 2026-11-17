using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// Something the player collects, by touching it or pressing Interact. Three kinds:
    /// Snap To Player (worn or held on a body part, optionally with jiggle),
    /// Pickup Counter (flies to an "icon x 12" counter in the HUD),
    /// Key Item (flies to the HUD and waits there until a Key Lock uses it up).
    /// Grabbed Position / Rotation / Scale fine-tune the result: on the body for Snap, on the HUD icon for the others.
    /// </summary>
    [RequireComponent(typeof(BoxCollider)), AddComponentMenu("GDD 110/Functions/Collectible")]
    public class GddCollectible : GddEntity
    {
        public enum Kind { SnapToPlayer, PickupCounter, KeyItem }
        public enum Activation { TouchIt, PressInteract }
        public enum KeyIconStyle { Sprite, Flattened3DModel }

        public Kind kind = Kind.SnapToPlayer;
        public Activation activation = Activation.TouchIt;
        [Tooltip("Verb shown in the prompt when Press Interact is used.")]
        public string actionLabel = "Pick up";

        // Snap to player
        public GddSocket snapTo = GddSocket.RightHand;
        [Tooltip("Attach to a specific humanoid bone instead of the body socket.")]
        public bool overrideJoint;
        public HumanBodyBones joint = HumanBodyBones.RightHand;
        [Tooltip("Rigged models (with bones) stay fixed to the body and their bones swing as you move. Models without bones wobble on a spring instead.")]
        public bool useJiggle;
        [Tooltip("0 = loose and floppy, 1 = nearly rigid.")]
        [Range(0, 1)] public float jiggleFirmness = 0.55f;
        [Tooltip("Press Drop (Q / B) to take it off. It goes back to where it started.")]
        public bool canTakeOff = true;

        // Shared fine-tuning: on the body (Snap) or on the HUD icon (Counter, Key)
        [Tooltip("Snap: offset from the bone. Counter / Key: pixel offset of the HUD icon.")]
        public Vector3 grabbedPosition;
        [Tooltip("Snap: rotation relative to the bone. Counter / Key: Z rotates the HUD icon.")]
        public Vector3 grabbedRotation;
        [Tooltip("Snap: scale relative to its size in the world. Counter / Key: scale of the HUD icon.")]
        public Vector3 grabbedScale = Vector3.one;

        // Counter
        [Tooltip("Collectibles with the same name share one counter.")]
        public string counterName = "Gems";
        public int amount = 1;
        [Min(0)] public int scoreValue;
        public Sprite icon;
        [Tooltip("Keep the HUD counter hidden until the first one is found (good for secrets).")]
        public bool secret;

        // Key
        [Tooltip("Key Locks ask for this exact name.")]
        public string keyName = "Red Key";
        public KeyIconStyle keyIcon = KeyIconStyle.Flattened3DModel;

        public UnityEvent onCollected = new UnityEvent();
        public UnityEvent onTakenOff = new UnityEvent();

        public bool Collected { get; private set; }
        public GddActor Wearer { get; private set; }

        static readonly List<GddCollectible> worn = new List<GddCollectible>();
        Transform homeParent;
        Vector3 homePosition, homeScale, wornScale;
        Quaternion homeRotation;
        bool homeSaved, addedJiggleBones, wasKinematic, hadGravity;
        Collider[] ownColliders;
        bool[] colliderWasEnabled;
        public override string Prompt => actionLabel;
        public override bool CanInteract(GddActor actor) => !Collected && activation == Activation.PressInteract;

        void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        void Awake() { SaveHome(); }

        void SaveHome()
        {
            if (homeSaved) return;
            homeSaved = true;
            homeParent = transform.parent;
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;
            homeScale = transform.localScale;
            ownColliders = GetComponents<Collider>();
            colliderWasEnabled = new bool[ownColliders.Length];
            for (int i = 0; i < ownColliders.Length; i++) colliderWasEnabled[i] = ownColliders[i].enabled;
        }

        void Start()
        {
            if (kind == Kind.PickupCounter && !secret)
                GddHud.Get().EnsureCounter(counterName, icon, grabbedPosition, grabbedRotation.z, grabbedScale);
        }

        void OnTriggerEnter(Collider other)
        {
            if (Collected || activation != Activation.TouchIt) return;
            var actor = other.GetComponentInParent<GddActor>();
            if (actor != null) Collect(actor);
        }

        public override void Interact(GddActor actor) { if (CanInteract(actor)) Collect(actor); }

        public override void Activate()
        {
            var actor = LastActor != null ? LastActor : FindAnyObjectByType<GddActor>();
            if (actor != null && !Collected) Collect(actor);
        }

        public void Collect(GddActor actor)
        {
            if (Collected) return;
            SaveHome();
            Collected = true;
            LastActor = actor;
            if (scoreValue > 0) actor.AddScore(scoreValue);
            foreach (var c in GetComponents<Collider>()) c.enabled = false;
            RaiseActivated();
            onCollected.Invoke();
            switch (kind)
            {
                case Kind.SnapToPlayer: Snap(actor); break;
                case Kind.PickupCounter: StartCoroutine(FlyToCounter()); break;
                case Kind.KeyItem: StartCoroutine(FlyToKeyRow()); break;
            }
        }

        // ---------- snap ----------

        /// <summary>The bone or socket this item attaches to on a given rig.</summary>
        public Transform AttachPoint(GddRig rig)
        {
            if (rig == null) return null;
            if (overrideJoint && rig.characterAnimator != null && rig.characterAnimator.isHuman)
            {
                var b = rig.characterAnimator.GetBoneTransform(joint);
                if (b != null) return b;
            }
            return rig.Socket(snapTo);
        }

        /// <summary>Like AttachPoint(rig), but follows the body the player is wearing, if any.</summary>
        public Transform AttachPoint(GddActor actor)
        {
            if (actor == null) return null;
            var body = GddWearableBody.BodyFor(actor);
            if (body != null)
            {
                if (overrideJoint) { var b = body.GetBoneTransform(joint); if (b != null) return b; }
                var s = GddRig.BodySocket(body, snapTo);
                if (s != null) return s;
            }
            return AttachPoint(actor.Rig);
        }

        bool SameSpot(GddCollectible other) =>
            overrideJoint == other.overrideJoint && (overrideJoint ? joint == other.joint : snapTo == other.snapTo);

        void Snap(GddActor actor)
        {
            var bone = AttachPoint(actor);
            if (bone == null) { Debug.LogWarning("[GDD 110] " + name + ": the player has no body sockets to snap to.", this); return; }
            // One thing per spot: whatever was already there goes back home.
            foreach (var other in worn.ToArray())
                if (other != this && other.Wearer == actor && other.SameSpot(this)) other.TakeOff();
            wornScale = Vector3.Scale(transform.lossyScale, grabbedScale);
            var body = GetComponent<Rigidbody>();
            if (body != null) { wasKinematic = body.isKinematic; hadGravity = body.useGravity; body.isKinematic = true; body.useGravity = false; }
            Wearer = actor;
            worn.Add(this);
            AttachTo(bone);
        }

        bool Rigged()
        {
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (smr.bones.Length > 1) return true;
            return false;
        }

        void AttachTo(Transform bone)
        {
            var follow = GetComponent<GddJiggleFollow>();
            if (follow != null) DestroyImmediate(follow);
            if (useJiggle && !Rigged())
            {
                // No bones to swing: the whole object wobbles after the bone on a spring.
                follow = gameObject.AddComponent<GddJiggleFollow>();
                follow.Init(bone, grabbedPosition, Quaternion.Euler(grabbedRotation), jiggleFirmness);
                transform.SetParent(null, true);
                transform.localScale = wornScale;
                return;
            }
            // Fixed to the bone. Rigged models swing their own bones.
            transform.SetParent(bone, false);
            transform.localPosition = grabbedPosition;
            transform.localRotation = Quaternion.Euler(grabbedRotation);
            var b = bone.lossyScale;
            transform.localScale = new Vector3(wornScale.x / Mathf.Max(b.x, 1e-4f), wornScale.y / Mathf.Max(b.y, 1e-4f), wornScale.z / Mathf.Max(b.z, 1e-4f));
            if (useJiggle)
            {
                var jiggle = GetComponent<GddJiggleBones>();
                if (jiggle == null) { jiggle = gameObject.AddComponent<GddJiggleBones>(); addedJiggleBones = true; }
                jiggle.firmness = jiggleFirmness;
                if (addedJiggleBones)   // added here: derive the rest from Firmness so one slider does it all
                {
                    jiggle.damping = Mathf.Lerp(0.04f, 0.4f, jiggleFirmness);
                    jiggle.sway = Mathf.Lerp(30f, 6f, jiggleFirmness);
                    jiggle.squash = Mathf.Lerp(0.3f, 0.06f, jiggleFirmness);
                    jiggle.gravity = Mathf.Lerp(0.6f, 0.2f, jiggleFirmness);
                }
                jiggle.enabled = true;
                jiggle.ResetPose();
            }
        }

        /// <summary>Take a worn Snap item off: it goes back to where it started and can be collected again.</summary>
        public void TakeOff()
        {
            if (kind != Kind.SnapToPlayer || Wearer == null) return;
            var actor = Wearer;
            worn.Remove(this);
            Wearer = null;
            var follow = GetComponent<GddJiggleFollow>();
            if (follow != null) Destroy(follow);
            var jiggle = GetComponent<GddJiggleBones>();
            if (jiggle != null) { jiggle.ResetPose(); if (addedJiggleBones) { Destroy(jiggle); addedJiggleBones = false; } }
            transform.SetParent(homeParent, false);
            transform.localPosition = homePosition;
            transform.localRotation = homeRotation;
            transform.localScale = homeScale;
            var body = GetComponent<Rigidbody>();
            if (body != null) { body.isKinematic = wasKinematic; body.useGravity = hadGravity; }
            Collected = false;
            onTakenOff.Invoke();
            if (isActiveAndEnabled) StartCoroutine(ReenableWhenClear(actor)); else RestoreColliders();
        }

        IEnumerator ReenableWhenClear(GddActor actor)
        {
            // Wait until the player has stepped away, or touching it would put it straight back on.
            while (actor != null && !Collected && (actor.transform.position - transform.position).sqrMagnitude < 2.5f) yield return null;
            if (!Collected) RestoreColliders();
        }

        void RestoreColliders()
        {
            if (ownColliders == null) return;
            for (int i = 0; i < ownColliders.Length; i++) if (ownColliders[i] != null) ownColliders[i].enabled = colliderWasEnabled[i];
        }

        void OnDestroy() { worn.Remove(this); }

        /// <summary>True if this player is wearing any Snap item that can be taken off.</summary>
        public static bool AnyWorn(GddActor actor)
        {
            foreach (var w in worn) if (w != null && w.Wearer == actor && w.canTakeOff) return true;
            return false;
        }

        /// <summary>Takes off the item this player put on most recently. Used by the Drop button.</summary>
        public static bool TakeOffLatest(GddActor actor)
        {
            for (int i = worn.Count - 1; i >= 0; i--)
                if (worn[i] != null && worn[i].Wearer == actor && worn[i].canTakeOff) { worn[i].TakeOff(); return true; }
            return false;
        }

        /// <summary>Moves everything a player is wearing onto their current body (after a Wearable Body swap).</summary>
        public static void Reattach(GddActor actor)
        {
            foreach (var w in worn.ToArray())
                if (w != null && w.Wearer == actor) { var bone = w.AttachPoint(actor); if (bone != null) w.AttachTo(bone); }
        }

        // ---------- HUD flights ----------

        IEnumerator FlyToCounter()
        {
            var hud = GddHud.Get();
            var target = hud.EnsureCounter(counterName, icon, grabbedPosition, grabbedRotation.z, grabbedScale);
            yield return Fly(GddHud.Get().CounterTarget(counterName));
            GddInventory.Add(counterName, amount);
            gameObject.SetActive(false);
        }

        IEnumerator FlyToKeyRow()
        {
            var hud = GddHud.Get();
            var slot = hud.AddKeySlot(keyName, keyIcon == KeyIconStyle.Sprite ? icon : null, grabbedPosition, grabbedRotation.z, grabbedScale);
            GameObject model = null;
            if (keyIcon == KeyIconStyle.Flattened3DModel || icon == null) { model = MakeFlatIcon(slot); if (model != null) model.SetActive(false); }
            yield return null;   // let the layout place the slot
            yield return Fly(slot);
            if (model != null) model.SetActive(true);
            hud.ShowKey(slot, model);
            GddInventory.AddKey(keyName);
            gameObject.SetActive(false);
        }

        IEnumerator Fly(RectTransform target)
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            Vector3 start = transform.position, startScale = transform.localScale;
            transform.SetParent(null, true);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
            {
                float k = t * t * (3f - 2f * t);
                Vector3 end = GddHud.WorldAt(target, 1.2f);
                transform.position = Vector3.Lerp(start, end, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.8f;
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.15f, k);
                transform.Rotate(0, 540f * Time.unscaledDeltaTime, 0, Space.World);
                yield return null;
            }
        }

        /// <summary>A copy of this object's look, squashed flat and parked in front of the camera over the HUD slot.</summary>
        GameObject MakeFlatIcon(RectTransform slot)
        {
            var cam = Camera.main;
            if (cam == null) return null;
            var copy = new GameObject("Key Icon - " + keyName);
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>();
                if (r == null) continue;
                var part = new GameObject(mf.name);
                part.transform.SetParent(copy.transform, false);
                part.transform.localPosition = Quaternion.Inverse(transform.rotation) * (mf.transform.position - transform.position);
                part.transform.localRotation = Quaternion.Inverse(transform.rotation) * mf.transform.rotation;
                part.transform.localScale = mf.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var nr = part.AddComponent<MeshRenderer>();
                nr.sharedMaterials = r.sharedMaterials;
                nr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                nr.receiveShadows = false;
            }
            var b = new Bounds(copy.transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in copy.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            float size = any ? Mathf.Max(b.size.x, b.size.y, b.size.z) : 1f;
            var holder = copy.AddComponent<GddCameraIcon>();
            holder.Init(slot, size);
            return copy;
        }
    }

    /// <summary>Keeps a flattened 3D copy pinned in front of the camera over a HUD slot, slowly turning.</summary>
    [AddComponentMenu("")]
    public class GddCameraIcon : MonoBehaviour
    {
        RectTransform slot;
        float modelSize;
        const float Depth = 0.6f;

        public void Init(RectTransform target, float size) { slot = target; modelSize = Mathf.Max(0.01f, size); }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || slot == null) { Destroy(gameObject); return; }
            transform.position = GddHud.WorldAt(slot, Depth);
            float pixels = slot.rect.height * slot.lossyScale.y;
            float worldHeight = 2f * Depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * (pixels / Screen.height);
            float s = worldHeight / modelSize * 0.8f;
            transform.rotation = cam.transform.rotation * Quaternion.Euler(10f, Time.unscaledTime * 60f, 0);
            transform.localScale = new Vector3(s, s, s * 0.12f);   // flattened toward the camera
        }
    }

    /// <summary>Springy attachment used by Collectible jiggle: follows a bone with lag and overshoot.</summary>
    [AddComponentMenu("")]
    [DefaultExecutionOrder(1000)]
    public class GddJiggleFollow : MonoBehaviour
    {
        Transform bone;
        Vector3 offset;
        Quaternion rotOffset;
        float stiffness, damping, turnRate;
        Vector3 velocity;

        public void Init(Transform target, Vector3 positionOffset, Quaternion rotationOffset, float firmness)
        {
            bone = target;
            offset = positionOffset;
            rotOffset = rotationOffset;
            stiffness = Mathf.Lerp(60f, 900f, firmness);
            damping = 2f * Mathf.Sqrt(stiffness) * Mathf.Lerp(0.3f, 0.85f, firmness);
            turnRate = Mathf.Lerp(7f, 40f, firmness);
            transform.position = bone.TransformPoint(offset);
            transform.rotation = bone.rotation * rotOffset;
        }

        void LateUpdate()
        {
            if (bone == null) { Destroy(this); return; }
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Vector3 goal = bone.TransformPoint(offset);
            Vector3 force = (goal - transform.position) * stiffness - velocity * damping;
            velocity += force * dt;
            transform.position += velocity * dt;
            // never trail too far behind on big moves (teleports, respawns)
            if ((transform.position - goal).sqrMagnitude > 1f) { transform.position = goal; velocity = Vector3.zero; }
            transform.rotation = Quaternion.Slerp(transform.rotation, bone.rotation * rotOffset, 1f - Mathf.Exp(-turnRate * dt));
        }
    }
}
