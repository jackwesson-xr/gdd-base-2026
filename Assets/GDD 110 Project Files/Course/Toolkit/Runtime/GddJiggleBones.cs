using System.Collections.Generic;
using UnityEngine;

namespace GDD110
{
    /// <summary>
    /// Makes the bones of a rigged model (ears, antennae, tails, hair, a slime's wobbly top) swing and settle
    /// as the object moves. Add it to anything with a Skinned Mesh Renderer. The root bone stays put; every
    /// bone below it lags behind, overshoots and springs back. Collectible "Jiggle" adds this automatically.
    /// </summary>
    [AddComponentMenu("GDD 110/Effects/Jiggle Bones")]
    [DefaultExecutionOrder(10100)]   // after animation, after pose mirroring and after attachments move
    public class GddJiggleBones : MonoBehaviour
    {
        [Tooltip("The bone that stays still. Empty = the root bone of the first Skinned Mesh Renderer.")]
        public Transform rootBone;
        [Tooltip("0 = loose and floppy, 1 = nearly rigid.")]
        [Range(0, 1)] public float firmness = 0.4f;
        [Tooltip("How quickly the swinging dies down.")]
        [Range(0, 1)] public float damping = 0.25f;
        [Tooltip("How much the bones droop under their own weight.")]
        public float gravity = 0.4f;
        [Tooltip("How much the whole model squashes and stretches when it bounces up and down.")]
        [Range(0, 0.5f)] public float squash = 0.12f;
        [Tooltip("Degrees the whole model leans and wobbles when the thing it sits on speeds up, stops or turns.")]
        [Range(0, 45)] public float sway = 12f;

        class Node
        {
            public Transform bone;
            public Quaternion restLocal;
            public Vector3 tailLocal;
            public float length;
            public Vector3 tip, previousTip;
        }

        readonly List<Node> nodes = new List<Node>();
        Vector3 rootRestScale, lastRootPos, lastVelocity, tilt, tiltVelocity;
        Quaternion rootRestRotation;
        float stretch, stretchVelocity, accumulator;
        const float Step = 1f / 60f;

        void Awake() { Setup(); }

        public void Setup()
        {
            nodes.Clear();
            if (rootBone == null)
            {
                var smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (smr != null) rootBone = smr.rootBone != null ? smr.rootBone : (smr.bones.Length > 0 ? smr.bones[0] : null);
            }
            if (rootBone == null) { enabled = false; return; }
            var skinned = new HashSet<Transform>();
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true)) foreach (var b in smr.bones) if (b != null) skinned.Add(b);
            // Parents before children, so each bone swings from wherever its parent ended up.
            foreach (var t in rootBone.GetComponentsInChildren<Transform>(true))
            {
                if (t == rootBone || t.childCount == 0 || !skinned.Contains(t)) continue;
                var child = t.GetChild(0);
                var n = new Node { bone = t, restLocal = t.localRotation, tailLocal = child.localPosition };
                n.length = Vector3.Scale(n.tailLocal, t.lossyScale).magnitude;
                if (n.length < 1e-5f) continue;
                n.tip = n.previousTip = t.TransformPoint(n.tailLocal);
                nodes.Add(n);
            }
            rootRestScale = rootBone.localScale;
            rootRestRotation = rootBone.localRotation;
            lastRootPos = rootBone.position;
        }

        /// <summary>Snap every bone back to rest, e.g. after a teleport.</summary>
        public void ResetPose()
        {
            foreach (var n in nodes) { n.bone.localRotation = n.restLocal; n.tip = n.previousTip = n.bone.TransformPoint(n.tailLocal); }
            if (rootBone != null) { rootBone.localScale = rootRestScale; rootBone.localRotation = rootRestRotation; lastRootPos = rootBone.position; }
            stretch = stretchVelocity = 0;
            tilt = tiltVelocity = lastVelocity = Vector3.zero;
        }

        void OnDisable() { if (rootBone != null) ResetPose(); }

        void LateUpdate()
        {
            if (rootBone == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            if ((rootBone.position - lastRootPos).sqrMagnitude > 4f) ResetPose();   // teleported

            // Squash and stretch from vertical motion; lean from changes in speed and direction.
            rootBone.localRotation = rootRestRotation;
            Vector3 velocity = dt > 0 ? (rootBone.position - lastRootPos) / dt : Vector3.zero;
            Vector3 accel = dt > 0 ? (velocity - lastVelocity) / dt : Vector3.zero;
            lastVelocity = velocity;
            float vy = velocity.y;
            lastRootPos = rootBone.position;
            if (sway > 0 && dt > 0)
            {
                float k = Mathf.Lerp(60f, 400f, firmness);
                float c = 2f * Mathf.Sqrt(k) * Mathf.Lerp(0.15f, 0.7f, damping);
                Vector3 flat = Vector3.ProjectOnPlane(accel, Vector3.up);
                Vector3 target = Vector3.ClampMagnitude(Vector3.Cross(flat, Vector3.up) * 1.5f, sway);   // lags behind: lean away from the push
                tiltVelocity += ((target - tilt) * k - tiltVelocity * c) * dt;
                tilt = Vector3.ClampMagnitude(tilt + tiltVelocity * dt, sway * 1.5f);
                if (tilt.sqrMagnitude > 1e-6f)
                    rootBone.rotation = Quaternion.AngleAxis(tilt.magnitude, tilt.normalized) * rootBone.rotation;
            }
            if (squash > 0)
            {
                float k = Mathf.Lerp(120f, 500f, firmness);
                float c = 2f * Mathf.Sqrt(k) * Mathf.Lerp(0.2f, 0.7f, damping);
                float target = Mathf.Clamp(vy * 0.12f, -squash, squash);   // rising stretches, falling squashes
                stretchVelocity += ((target - stretch) * k - stretchVelocity * c) * dt;
                stretch = Mathf.Clamp(stretch + stretchVelocity * dt, -squash, squash);
                float y = 1f + stretch, xz = 1f / Mathf.Sqrt(Mathf.Max(0.2f, y));
                rootBone.localScale = new Vector3(rootRestScale.x * xz, rootRestScale.y * y, rootRestScale.z * xz);
            }

            foreach (var n in nodes) n.bone.localRotation = n.restLocal;
            accumulator = Mathf.Min(accumulator + dt, Step * 4);
            int steps = 0;
            while (accumulator >= Step) { accumulator -= Step; steps++; }
            float pull = 1f - Mathf.Exp(-Mathf.Lerp(4f, 40f, firmness * firmness) * Step);
            float keep = 1f - Mathf.Lerp(0.02f, 0.3f, damping);

            foreach (var n in nodes)
            {
                // Parent has already been posed this frame: work out where this bone's tip wants to be at rest.
                n.bone.localRotation = n.restLocal;
                Vector3 origin = n.bone.position;
                Vector3 rest = n.bone.TransformPoint(n.tailLocal);
                for (int i = 0; i < steps; i++)
                {
                    Vector3 tipVelocity = (n.tip - n.previousTip) * keep;
                    n.previousTip = n.tip;
                    n.tip += tipVelocity + Vector3.down * gravity * Step * Step * 9.81f;
                    n.tip += (rest - n.tip) * pull;
                    n.tip = origin + (n.tip - origin).normalized * n.length;
                }
                Vector3 from = rest - origin, to = n.tip - origin;
                if (from.sqrMagnitude > 1e-10f && to.sqrMagnitude > 1e-10f)
                    n.bone.rotation = Quaternion.FromToRotation(from, to) * n.bone.rotation;
            }
        }
    }
}
