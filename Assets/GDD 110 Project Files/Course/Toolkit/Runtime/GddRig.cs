using UnityEngine;

namespace GDD110
{
    public enum GddSocket { RightHand, LeftHand, Head, Chest, Back, Hips, LeftFoot, RightFoot }

    [DisallowMultipleComponent, AddComponentMenu("GDD 110/Player/Body Sockets")]
    public class GddRig : GddEntity
    {
        [Tooltip("Automatically found in children. Humanoid rigs map directly to their bones.")]
        public Animator characterAnimator;
        [Tooltip("Optional overrides for non-humanoid rigs or custom attachment positions.")]
        public Transform rightHand, leftHand, head, chest, back, hips, leftFoot, rightFoot;
        public Transform Socket(GddSocket socket)
        {
            Transform existing = Read(socket);
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
            if (existing != null) return AlignHand(socket, existing);
            Transform bone = FindBone(socket);
            string socketName = "GDD " + socket + " Socket";
            var parent = bone != null ? bone : transform;
            var result = parent.Find(socketName);
            if (result == null)
            {
                result = new GameObject(socketName).transform;
                result.SetParent(parent, false);
                if (bone == null) result.localPosition = FallbackPosition(socket);
                if (socket == GddSocket.Back) result.localPosition += Vector3.back * 0.15f;
            }
            Write(socket, result);
            return AlignHand(socket, result);
        }

        /// <summary>Default hand sockets (the ones named "GDD RightHand Socket" etc.) sit in the palm; custom ones are left alone.</summary>
        Transform AlignHand(GddSocket socket, Transform s)
        {
            if (socket != GddSocket.RightHand && socket != GddSocket.LeftHand) return s;
            if (s.name != "GDD " + socket + " Socket" || characterAnimator == null || !characterAnimator.isHuman) return s;
            bool left = socket == GddSocket.LeftHand;
            if (s.parent != characterAnimator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand)) return s;
            if (GripPose(characterAnimator, left, out var p, out var r)) s.SetPositionAndRotation(p, r);
            return s;
        }

        /// <summary>A socket on any humanoid body (used for bodies the player wears): the matching bone, with hands at the grip.</summary>
        public static Transform BodySocket(Animator anim, GddSocket socket)
        {
            if (anim == null || !anim.isHuman) return null;
            HumanBodyBones hb;
            switch (socket)
            {
                case GddSocket.RightHand: hb = HumanBodyBones.RightHand; break;
                case GddSocket.LeftHand: hb = HumanBodyBones.LeftHand; break;
                case GddSocket.Head: hb = HumanBodyBones.Head; break;
                case GddSocket.Chest: case GddSocket.Back: hb = HumanBodyBones.Chest; break;
                case GddSocket.LeftFoot: hb = HumanBodyBones.LeftFoot; break;
                case GddSocket.RightFoot: hb = HumanBodyBones.RightFoot; break;
                default: hb = HumanBodyBones.Hips; break;
            }
            var bone = anim.GetBoneTransform(hb);
            if (bone == null && hb == HumanBodyBones.Chest) bone = anim.GetBoneTransform(HumanBodyBones.Spine);
            if (bone == null) return null;
            string socketName = "GDD " + socket + " Socket";
            var s = bone.Find(socketName);
            if (s == null)
            {
                s = new GameObject(socketName).transform;
                s.SetParent(bone, false);
                if (socket == GddSocket.Back) s.localPosition += bone.InverseTransformDirection(-anim.transform.forward) * 0.15f;
            }
            bool hand = socket == GddSocket.RightHand || socket == GddSocket.LeftHand;
            if (hand && GripPose(anim, socket == GddSocket.LeftHand, out var p, out var r)) s.SetPositionAndRotation(p, r);
            // Scale-neutral, so grabbed offsets mean the same thing on any body.
            var ls = bone.lossyScale;
            s.localScale = new Vector3(1f / Mathf.Max(1e-4f, ls.x), 1f / Mathf.Max(1e-4f, ls.y), 1f / Mathf.Max(1e-4f, ls.z));
            return s;
        }

        /// <summary>
        /// Where a held item's handle sits in a humanoid hand: the middle of the palm, with the item's Y axis running
        /// across the fist and out the thumb side (so a sword's blade sticks up out of the fist) and Z along the fingers.
        /// Default hand sockets snap to this, so any humanoid holds things the same way.
        /// </summary>
        public static bool GripPose(Animator anim, bool left, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero; rotation = Quaternion.identity;
            if (anim == null || !anim.isHuman) return false;
            Transform B(HumanBodyBones b) => anim.GetBoneTransform(b);
            var hand = B(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            var middle = B(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            var index = B(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
            var little = B(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
            if (little == null) little = B(left ? HumanBodyBones.LeftRingProximal : HumanBodyBones.RightRingProximal);
            if (hand == null || middle == null || index == null || little == null) return false;
            Vector3 fingers = middle.position - hand.position;
            float length = fingers.magnitude;
            if (length < 1e-4f) return false;
            fingers /= length;
            Vector3 across = Vector3.ProjectOnPlane(index.position - little.position, fingers);
            if (across.sqrMagnitude < 1e-8f) return false;
            across.Normalize();
            Vector3 palm = Vector3.Cross(fingers, across) * (left ? -1f : 1f);
            position = hand.position + fingers * (length * 0.75f) + palm * (length * 0.3f);
            rotation = Quaternion.LookRotation(fingers, across);
            return true;
        }
        [ContextMenu("Auto Map Body Sockets")]
        public void AutoMap() { foreach (GddSocket socket in System.Enum.GetValues(typeof(GddSocket))) Socket(socket); }
        public void MapNewSkin(Animator animator)
        {
            characterAnimator = animator;
            foreach (GddSocket socket in System.Enum.GetValues(typeof(GddSocket))) Write(socket, null);
            AutoMap();
        }
        Transform FindBone(GddSocket socket)
        {
            HumanBodyBones bone = HumanBodyBones.Hips;
            switch (socket)
            {
                case GddSocket.RightHand: bone = HumanBodyBones.RightHand; break;
                case GddSocket.LeftHand: bone = HumanBodyBones.LeftHand; break;
                case GddSocket.Head: bone = HumanBodyBones.Head; break;
                case GddSocket.Chest: case GddSocket.Back: bone = HumanBodyBones.Chest; break;
                case GddSocket.LeftFoot: bone = HumanBodyBones.LeftFoot; break;
                case GddSocket.RightFoot: bone = HumanBodyBones.RightFoot; break;
            }
            if (characterAnimator != null && characterAnimator.isHuman)
            {
                var result = characterAnimator.GetBoneTransform(bone);
                if (result == null && (socket == GddSocket.Chest || socket == GddSocket.Back)) result = characterAnimator.GetBoneTransform(HumanBodyBones.Spine);
                if (result != null) return result;
            }
            string wanted = socket == GddSocket.Back ? "chest" : socket.ToString().ToLowerInvariant();
            foreach (var t in (characterAnimator != null ? characterAnimator.transform : transform).GetComponentsInChildren<Transform>(true))
            {
                string normalized = t.name.ToLowerInvariant().Replace("_", "").Replace("-", "").Replace(" ", "");
                int colon = normalized.LastIndexOf(':');
                if (colon >= 0) normalized = normalized.Substring(colon + 1);
                if (normalized == wanted) return t;
            }
            return null;
        }
        static Vector3 FallbackPosition(GddSocket socket)
        {
            switch (socket)
            {
                case GddSocket.RightHand: return new Vector3(0.4f, 1.1f, 0.2f);
                case GddSocket.LeftHand: return new Vector3(-0.4f, 1.1f, 0.2f);
                case GddSocket.Head: return new Vector3(0, 1.8f, 0);
                case GddSocket.Chest: case GddSocket.Back: return new Vector3(0, 1.3f, 0);
                case GddSocket.LeftFoot: return new Vector3(-0.15f, 0.1f, 0);
                case GddSocket.RightFoot: return new Vector3(0.15f, 0.1f, 0);
                default: return new Vector3(0, 0.9f, 0);
            }
        }
        Transform Read(GddSocket s)
        {
            switch (s) { case GddSocket.RightHand: return rightHand; case GddSocket.LeftHand: return leftHand; case GddSocket.Head: return head; case GddSocket.Chest: return chest; case GddSocket.Back: return back; case GddSocket.LeftFoot: return leftFoot; case GddSocket.RightFoot: return rightFoot; default: return hips; }
        }
        void Write(GddSocket s, Transform t)
        {
            switch (s) { case GddSocket.RightHand: rightHand=t; break; case GddSocket.LeftHand: leftHand=t; break; case GddSocket.Head: head=t; break; case GddSocket.Chest: chest=t; break; case GddSocket.Back: back=t; break; case GddSocket.LeftFoot: leftFoot=t; break; case GddSocket.RightFoot: rightFoot=t; break; default: hips=t; break; }
        }
    }
}
