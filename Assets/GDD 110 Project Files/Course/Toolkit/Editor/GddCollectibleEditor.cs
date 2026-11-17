using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GDD110.Editor
{
    /// <summary>
    /// Collectible inspector: shows only the settings for the chosen kind, a one-click icon maker,
    /// and (for Snap To Player) a live ghost skeleton in the Scene view holding the object exactly the way it will be held.
    /// </summary>
    [CustomEditor(typeof(GddCollectible)), CanEditMultipleObjects]
    public class GddCollectibleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var c = (GddCollectible)target;
            void P(string name, string label = null) => EditorGUILayout.PropertyField(serializedObject.FindProperty(name), label == null ? null : new GUIContent(label));

            P("friendlyName");
            P("kind");
            P("activation", "Collect by");
            if (c.activation == GddCollectible.Activation.PressInteract) P("actionLabel", "Prompt verb");
            EditorGUILayout.Space();

            switch (c.kind)
            {
                case GddCollectible.Kind.SnapToPlayer:
                    EditorGUILayout.LabelField("Where it goes", EditorStyles.boldLabel);
                    P("overrideJoint", "Use a specific joint");
                    if (c.overrideJoint) P("joint"); else P("snapTo", "Body socket");
                    P("useJiggle", "Jiggle");
                    if (c.useJiggle) P("jiggleFirmness", "Firmness");
                    P("canTakeOff", "Can take off (Q / B)");
                    EditorGUILayout.LabelField("How it's held (relative to the bone)", EditorStyles.boldLabel);
                    P("grabbedPosition", "Position");
                    P("grabbedRotation", "Rotation");
                    P("grabbedScale", "Scale");
                    EditorGUILayout.HelpBox("Select this object to see a ghost body in the Scene view holding it the way it will be held. Move or rotate the object to see the ghost follow.", MessageType.None);
                    break;
                case GddCollectible.Kind.PickupCounter:
                    P("counterName", "Counter name");
                    P("amount");
                    IconField(c);
                    HudNudge();
                    break;
                case GddCollectible.Kind.KeyItem:
                    P("keyName", "Key name");
                    P("keyIcon", "HUD icon");
                    if (c.keyIcon == GddCollectible.KeyIconStyle.Sprite) IconField(c);
                    HudNudge();
                    break;
            }
            EditorGUILayout.Space();
            P("onCollected");
            if (c.kind == GddCollectible.Kind.SnapToPlayer) P("onTakenOff");
            P("onActivated");
            serializedObject.ApplyModifiedProperties();

            void HudNudge()
            {
                EditorGUILayout.LabelField("HUD icon nudge", EditorStyles.boldLabel);
                P("grabbedPosition", "Offset (pixels, X/Y)");
                P("grabbedRotation", "Rotation (Z)");
                P("grabbedScale", "Scale");
            }
        }

        void IconField(GddCollectible c)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
            if (GUILayout.Button("Make Icon From This Object"))
            {
                var sprite = GddIconMaker.Make(c.gameObject, c.DisplayName);
                if (sprite != null) { serializedObject.FindProperty("icon").objectReferenceValue = sprite; }
            }
        }

        // ---------- ghost skeleton gizmo ----------

        static readonly HumanBodyBones[][] Chains =
        {
            new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Neck, HumanBodyBones.Head },
            new[] { HumanBodyBones.Chest, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand },
            new[] { HumanBodyBones.Chest, HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand },
            new[] { HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes },
            new[] { HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes },
            new[] { HumanBodyBones.LeftHand, HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal },
            new[] { HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal },
            new[] { HumanBodyBones.LeftHand, HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal },
            new[] { HumanBodyBones.LeftHand, HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal },
            new[] { HumanBodyBones.RightHand, HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal },
            new[] { HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal },
            new[] { HumanBodyBones.RightHand, HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal },
            new[] { HumanBodyBones.RightHand, HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal },
        };

        /// <summary>Read the attach point without creating sockets in the scene.</summary>
        static Transform AttachPoint(GddCollectible c, GddRig rig, Animator anim)
        {
            if (c.overrideJoint && anim != null) return anim.GetBoneTransform(c.joint);
            Transform t = null;
            switch (c.snapTo)
            {
                case GddSocket.RightHand: t = rig.rightHand; break;
                case GddSocket.LeftHand: t = rig.leftHand; break;
                case GddSocket.Head: t = rig.head; break;
                case GddSocket.Chest: t = rig.chest; break;
                case GddSocket.Back: t = rig.back; break;
                case GddSocket.Hips: t = rig.hips; break;
                case GddSocket.LeftFoot: t = rig.leftFoot; break;
                case GddSocket.RightFoot: t = rig.rightFoot; break;
            }
            if (t != null || anim == null) return t;
            switch (c.snapTo)
            {
                case GddSocket.RightHand: return anim.GetBoneTransform(HumanBodyBones.RightHand);
                case GddSocket.LeftHand: return anim.GetBoneTransform(HumanBodyBones.LeftHand);
                case GddSocket.Head: return anim.GetBoneTransform(HumanBodyBones.Head);
                case GddSocket.LeftFoot: return anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                case GddSocket.RightFoot: return anim.GetBoneTransform(HumanBodyBones.RightFoot);
                case GddSocket.Hips: return anim.GetBoneTransform(HumanBodyBones.Hips);
                default: return anim.GetBoneTransform(HumanBodyBones.Chest) ?? anim.GetBoneTransform(HumanBodyBones.Spine);
            }
        }

        static bool IsHandSnap(GddCollectible c) =>
            c.overrideJoint ? c.joint.ToString().Contains("Hand") || c.joint.ToString().Contains("Thumb") || c.joint.ToString().Contains("Index") || c.joint.ToString().Contains("Middle") || c.joint.ToString().Contains("Ring") || c.joint.ToString().Contains("Little")
                            : c.snapTo == GddSocket.LeftHand || c.snapTo == GddSocket.RightHand;

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGhost(GddCollectible c, GizmoType type)
        {
            if (c.kind != GddCollectible.Kind.SnapToPlayer || c.Collected) return;
            var rig = Object.FindAnyObjectByType<GddRig>();
            var anim = rig != null ? rig.characterAnimator : null;
            if (anim != null && !anim.isHuman) anim = null;
            var bone = rig != null ? AttachPoint(c, rig, anim) : null;

            // Default hand sockets sit in the palm at play time (GddRig.GripPose); show the same here.
            Vector3 attachPos = bone != null ? bone.position : Vector3.zero;
            Quaternion attachRot = bone != null ? bone.rotation : Quaternion.identity;
            if (bone != null && anim != null && !c.overrideJoint && (c.snapTo == GddSocket.RightHand || c.snapTo == GddSocket.LeftHand)
                && (bone.name == "GDD " + c.snapTo + " Socket" || bone == anim.GetBoneTransform(c.snapTo == GddSocket.LeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand))
                && GddRig.GripPose(anim, c.snapTo == GddSocket.LeftHand, out var gp, out var gr)) { attachPos = gp; attachRot = gr; }

            // Where the bone would be if the object were held exactly as set up.
            Quaternion offRot = Quaternion.Euler(c.grabbedRotation);
            Vector3 boneScale = bone != null ? bone.lossyScale : Vector3.one;
            Quaternion boneRot = c.transform.rotation * Quaternion.Inverse(offRot);
            Vector3 bonePos = c.transform.position - boneRot * Vector3.Scale(c.grabbedPosition, boneScale);

            bool hand = IsHandSnap(c);
            var ghost = new Color(0.1f, 0.42f, 1f, 0.95f);
            var faint = new Color(0.1f, 0.42f, 1f, hand ? 0.3f : 0.55f);

            if (bone == null || anim == null)
            {
                // No player in the scene: a generic hand or body, upright, around the attach point.
                Handles.color = ghost;
                Handles.SphereHandleCap(0, bonePos, Quaternion.identity, 0.06f, EventType.Repaint);
                if (hand)
                {
                    Handles.DrawWireCube(bonePos + boneRot * new Vector3(0, -0.04f, 0.06f), new Vector3(0.09f, 0.03f, 0.11f));
                    for (int f = 0; f < 4; f++) Handles.DrawLine(bonePos + boneRot * new Vector3(-0.035f + f * 0.023f, -0.04f, 0.11f), bonePos + boneRot * new Vector3(-0.035f + f * 0.023f, -0.07f, 0.18f), 2f);
                    Handles.DrawLine(bonePos, bonePos - boneRot * Vector3.forward * 0.28f, 3f);
                }
                Handles.Label(bonePos + Vector3.up * 0.15f, "Ghost " + (c.overrideJoint ? c.joint.ToString() : c.snapTo.ToString()) + "\n(add the player rig to the scene for a full body)");
                return;
            }

            // Every humanoid bone, re-expressed relative to the attach bone, then placed around the object.
            Vector3 Ghost(Transform b) => bonePos + boneRot * (Quaternion.Inverse(attachRot) * (b.position - attachPos));
            var limb = new HashSet<Transform>();
            for (var t = bone; t != null && t != anim.transform; t = t.parent) limb.Add(t);
            foreach (var chain in Chains)
            {
                Transform prev = null;
                foreach (var hb in chain)
                {
                    var b = anim.GetBoneTransform(hb);
                    if (b == null) continue;
                    if (prev != null)
                    {
                        bool strong = limb.Contains(b) || (hand && hb.ToString().StartsWith(bone.name.Contains("Left") ? "Left" : "Right") && (hb.ToString().Contains("Index") || hb.ToString().Contains("Middle") || hb.ToString().Contains("Ring") || hb.ToString().Contains("Thumb")));
                        Handles.color = strong ? ghost : faint;
                        Handles.DrawLine(Ghost(prev), Ghost(b), strong ? 5f : 3f);
                        Handles.SphereHandleCap(0, Ghost(b), Quaternion.identity, strong ? 0.035f : 0.025f, EventType.Repaint);
                    }
                    prev = b;
                }
            }
            var head = anim.GetBoneTransform(HumanBodyBones.Head);
            if (head != null && !hand) { Handles.color = faint; Handles.DrawWireDisc(Ghost(head) + boneRot * Vector3.up * 0.08f, SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera.transform.forward : Vector3.forward, 0.11f); }
            Handles.color = ghost;
            Handles.SphereHandleCap(0, bonePos, boneRot, 0.05f, EventType.Repaint);
            Handles.Label(bonePos + Vector3.up * 0.12f, "Held by: " + (c.overrideJoint ? c.joint.ToString() : c.snapTo.ToString()));
        }
    }

    /// <summary>
    /// Turns an object into a transparent icon sprite. Renders it in a private preview scene; if that fails it falls
    /// back to Unity's asset preview (the approach from the Bee Game resource editor).
    /// </summary>
    public static class GddIconMaker
    {
        const string Folder = "Assets/GDD 110 Project Files/Wesson Assets/UI/Icons";

        public static Sprite Make(GameObject source, string name, int size = 256)
        {
            Texture2D tex = Render(source, size);
            if (tex == null || IsEmpty(tex))
            {
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(source) ?? source;
                var preview = AssetPreview.GetAssetPreview(prefab);
                if (preview != null) tex = Readable(preview);
            }
            if (tex == null) { EditorUtility.DisplayDialog("Icon", "Unity could not render this object yet. Try again in a moment.", "OK"); return null; }

            GddCourseMenus.EnsureFolder(Folder);
            string path = Folder + "/" + Safe(name) + " Icon.png";
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath), path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Texture2D Render(GameObject source, int size)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var copy = Object.Instantiate(source);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, scene);
                copy.transform.SetPositionAndRotation(Vector3.zero, source.transform.rotation);
                var renderers = copy.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return null;
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);

                var lightGo = new GameObject("Key Light", typeof(Light));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, scene);
                var light = lightGo.GetComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.3f;
                lightGo.transform.rotation = Quaternion.Euler(40, -30, 0);

                var camGo = new GameObject("Icon Camera", typeof(Camera));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.GetComponent<Camera>();
                cam.scene = scene;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0, 0, 0, 0);
                cam.fieldOfView = 25f;
                float radius = b.extents.magnitude;
                float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
                Vector3 dir = new Vector3(0.55f, 0.45f, -1f).normalized;
                cam.transform.position = b.center + dir * dist;
                cam.transform.LookAt(b.center);
                cam.nearClipPlane = 0.01f; cam.farClipPlane = dist * 4f;

                var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
                return tex;
            }
            catch (System.Exception e) { Debug.LogWarning("[GDD 110] Icon render failed, using the asset preview instead: " + e.Message); return null; }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        static bool IsEmpty(Texture2D t)
        {
            var px = t.GetPixels32();
            int solid = 0;
            for (int i = 0; i < px.Length; i += 13) if (px[i].a > 10) solid++;
            return solid < 4 || solid > px.Length / 13 * 0.97f;   // nothing drawn, or the background came out opaque
        }

        static Texture2D Readable(Texture source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            t.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return t;
        }

        static string Safe(string s) { foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_'); return s; }
    }
}
