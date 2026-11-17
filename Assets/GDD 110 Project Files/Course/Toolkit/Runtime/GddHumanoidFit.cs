using System;
using System.Linq;
using UnityEngine;

namespace GDD110
{
    public static class GddHumanoidFit
    {
        public static void Fit(Transform mount, Animator animator, Transform root,
            float targetHeight, float baseHeight, Vector3 center, float height, float yaw)
        {
            var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var right = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (left == null || right == null || hips == null || head == null)
                throw new InvalidOperationException("The Avatar must map its upper legs, hips, and head.");
            var up = (head.position - hips.position).normalized;
            var side = Vector3.ProjectOnPlane(right.position - left.position, up).normalized;
            if (side.sqrMagnitude < .5f || up.sqrMagnitude < .5f)
                throw new InvalidOperationException("The model's rest pose does not define a usable up and forward direction.");
            var basis = Quaternion.LookRotation(Vector3.Cross(side, up), up);
            mount.transform.rotation = root.rotation * Quaternion.Euler(0, yaw, 0) *
                Quaternion.Inverse(Quaternion.Inverse(root.rotation) * basis);

            var renderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.enabled && r.sharedMesh != null).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Choose the character FBX with a mesh, rather than an animation-only FBX.");
            var bounds = BoundsInPlayer(renderers, root);
            if (bounds.size.y < .001f) throw new InvalidOperationException("The selected mesh has no usable height.");
            float scale = (height > 0 ? height : targetHeight) / bounds.size.y;
            mount.transform.localScale = Vector3.one * scale;
            float floor = baseHeight;
            var pelvis = root.InverseTransformPoint(hips.position);
            mount.transform.localPosition = new Vector3(center.x - pelvis.x, floor - bounds.min.y * scale,
                center.z - pelvis.z);

        }

        static Bounds BoundsInPlayer(SkinnedMeshRenderer[] renderers, Transform player)
        {
            var result = new Bounds();
            bool first = true;
            foreach (var r in renderers)
            {
                // Bake the rest mesh; the imported culling bounds can include animation padding.
                var mesh = new Mesh();
                try
                {
                    r.BakeMesh(mesh);
                    foreach (var v in mesh.vertices)
                    {
                        var point = player.InverseTransformPoint(r.transform.TransformPoint(v));
                        if (first) { result = new Bounds(point, Vector3.zero); first = false; }
                        else result.Encapsulate(point);
                    }
                }
                finally { if(Application.isPlaying) UnityEngine.Object.Destroy(mesh); else UnityEngine.Object.DestroyImmediate(mesh); }
            }
            return result;
        }
    }
}
