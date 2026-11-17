using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GDD110.Editor
{
    /// <summary>
    /// One-button player character swap. Takes any humanoid model (a Mixamo "FBX for Unity, With Skin" download,
    /// or any other humanoid), sets it up as Humanoid, puts it on the player, keeps every animation, moves body
    /// sockets and anything held to the new bones, and can put the original back with Revert.
    /// </summary>
    public class GddCharacterSwapTool : EditorWindow
    {
        GameObject model;
        Animator player;

        const string AvatarFolder = "Assets/GDD 110 Project Files/Wesson Assets/Animation/Player/Avatars";
        const string TextureFolder = "Assets/GDD 110 Project Files/Wesson Assets/Textures/Player/Characters";

        [MenuItem("Tools/GDD 110/Character/Swap Player Character...", false, 200)]
        public static void Open()
        {
            var w = GetWindow<GddCharacterSwapTool>(true, "Swap Player Character");
            w.minSize = new Vector2(380, 300);
            if (Selection.activeObject is GameObject g && AssetDatabase.Contains(g)) w.model = g;
            w.player = FindPlayer();
        }

        [MenuItem("Assets/GDD 110/Use As Player Character", false, 2000)]
        static void UseSelected()
        {
            var g = Selection.activeObject as GameObject;
            var p = FindPlayer();
            if (p == null) { EditorUtility.DisplayDialog("Swap Player Character", "Open a level that has the player in it first.", "OK"); return; }
            Swap(g, p);
        }

        [MenuItem("Assets/GDD 110/Use As Player Character", true)]
        static bool UseSelectedValid() => Selection.activeObject is GameObject g && AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(g)) is ModelImporter;

        [MenuItem("Tools/GDD 110/Character/Revert Player Character", false, 201)]
        static void RevertMenu()
        {
            var p = FindPlayer();
            if (p != null && p.GetComponent<GddCharacterSwap>() != null) Revert(p);
            else EditorUtility.DisplayDialog("Revert Player Character", "The player in this level is already using its original character.", "OK");
        }

        public static Animator FindPlayer()
        {
            var actor = Object.FindAnyObjectByType<GddActor>();
            if (actor != null)
            {
                var a = actor.GetComponent<Animator>();
                if (a != null) return a;
                a = actor.GetComponentInChildren<Animator>();
                if (a != null) return a;
            }
            var tpc = Object.FindAnyObjectByType<StarterAssets.ThirdPersonController>();
            return tpc != null ? tpc.GetComponent<Animator>() : null;
        }

        void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "1. On mixamo.com pick a character and Download: Format \"FBX for Unity (.fbx)\", Pose \"T-pose\", Skin \"With Skin\".\n" +
                "2. Drag the .fbx into the Project window.\n" +
                "3. Drop it below and press Swap. Every animation, socket and held item carries over.", MessageType.Info);
            model = (GameObject)EditorGUILayout.ObjectField("New character", model, typeof(GameObject), false);
            player = (Animator)EditorGUILayout.ObjectField("Player", player, typeof(Animator), true);
            if (player == null && GUILayout.Button("Find the player in this level")) player = FindPlayer();
            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(model == null || player == null))
            {
                if (GUILayout.Button("Swap Character", GUILayout.Height(42))) Swap(model, player);
            }
            var swap = player != null ? player.GetComponent<GddCharacterSwap>() : null;
            using (new EditorGUI.DisabledScope(swap == null))
            {
                if (GUILayout.Button(swap != null ? "Revert to Original Character" : "Revert (nothing swapped)", GUILayout.Height(28))) Revert(player);
            }
            if (swap != null) EditorGUILayout.LabelField("Currently using: " + (swap.model != null ? swap.model.name : "?"), EditorStyles.miniLabel);
        }

        // ---------------------------------------------------------------------------------------------

        public static bool Swap(GameObject modelAsset, Animator target)
        {
            if (modelAsset == null || target == null) return false;
            string path = AssetDatabase.GetAssetPath(modelAsset);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) { EditorUtility.DisplayDialog("Swap Player Character", "Pick the character's .fbx (or other model file) from the Project window.", "OK"); return false; }

            if (!PrepareImport(importer, path)) return false;
            var source = importer.humanDescription;
            if (source.human == null || source.human.Length == 0) { EditorUtility.DisplayDialog("Swap Player Character", modelAsset.name + " couldn't be set up as a humanoid. Mixamo characters work; make sure the download was 'With Skin'.", "OK"); return false; }

            if (target.GetComponent<GddCharacterSwap>() != null) Revert(target, false);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Swap Player Character");

            // Bones of the current character, so sockets and held items can follow.
            var oldBones = new Dictionary<Transform, HumanBodyBones>();
            for (int b = 0; b < (int)HumanBodyBones.LastBone; b++)
            {
                var t = target.isHuman ? target.GetBoneTransform((HumanBodyBones)b) : null;
                if (t != null && !oldBones.ContainsKey(t)) oldBones[t] = (HumanBodyBones)b;
            }
            float oldHeight = Height(target.transform, null);

            // The new model, under the player.
            var inst = (GameObject)Object.Instantiate(modelAsset);
            inst.name = "Character - " + modelAsset.name;
            Undo.RegisterCreatedObjectUndo(inst, "Swap Player Character");
            foreach (var a in inst.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
            inst.transform.SetParent(target.transform, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            float newHeight = Height(inst.transform, null);
            if (oldHeight > 0.1f && newHeight > 0.01f)
            {
                float ratio = oldHeight / newHeight;
                if (ratio < 0.75f || ratio > 1.33f)
                {
                    inst.transform.localScale = Vector3.one * ratio;
                    Debug.Log("[GDD 110] " + modelAsset.name + " was " + newHeight.ToString("0.00") + " m tall; scaled to match the old character (" + oldHeight.ToString("0.00") + " m).");
                }
            }

            // Hide the old character (meshes and skeleton), keeping it for Revert.
            var swap = Undo.AddComponent<GddCharacterSwap>(target.gameObject);
            swap.model = inst;
            swap.modelAssetPath = path;
            swap.originalAvatar = target.avatar;
            Transform oldHips = target.isHuman ? target.GetBoneTransform(HumanBodyBones.Hips) : null;
            foreach (Transform child in target.transform)
            {
                if (child == inst.transform) continue;
                bool hasSkin = child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
                bool hasBones = oldHips != null && (oldHips == child || oldHips.IsChildOf(child));
                if (!(hasSkin || hasBones) || !child.gameObject.activeSelf) continue;
                Undo.RecordObject(child.gameObject, "Swap Player Character");
                swap.hiddenOriginals.Add(child.gameObject);
            }

            // Avatar that matches the new model sitting under the player.
            var avatar = BuildAvatar(target.transform, inst.transform, source);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Undo.RevertAllDownToGroup(group);
                EditorUtility.DisplayDialog("Swap Player Character", "Unity couldn't build a humanoid avatar for " + modelAsset.name + ". If it has a nonstandard skeleton, open its Rig tab, set Animation Type to Humanoid, press Configure and fix any red bones.", "OK");
                return false;
            }
            Directory.CreateDirectory(AvatarFolder);
            string avatarPath = AvatarFolder + "/" + Safe(modelAsset.name) + " (Player Avatar).asset";
            AssetDatabase.DeleteAsset(avatarPath);
            AssetDatabase.CreateAsset(avatar, avatarPath);
            swap.swappedAvatar = avatar;

            Undo.RecordObject(target, "Swap Player Character");
            target.avatar = avatar;
            target.Rebind();

            // Sockets and anything held: move them to the same body part on the new character.
            foreach (var pair in oldBones)
            {
                var newBone = target.GetBoneTransform(pair.Value);
                if (newBone == null) continue;
                var extras = new List<Transform>();
                foreach (Transform c in pair.Key) if (!oldBones.ContainsKey(c) && IsAttachment(c)) extras.Add(c);
                foreach (var item in extras) Carry(swap, item, newBone, target);
            }
            foreach (var g in swap.hiddenOriginals) g.SetActive(false);

            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = inst;
            Debug.Log("[GDD 110] Player is now " + modelAsset.name + ". Tools > GDD 110 > Character > Revert Player Character puts the original back.");
            return true;
        }

        /// <summary>Humanoid import settings, embedded textures pulled out so materials show up.</summary>
        public static bool PrepareImport(ModelImporter importer, string path)
        {
            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human) { importer.animationType = ModelImporterAnimationType.Human; changed = true; }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) { importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; changed = true; }
            if (importer.materialImportMode == ModelImporterMaterialImportMode.None) { importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; changed = true; }
            if (changed) importer.SaveAndReimport();

            string folder = TextureFolder + "/" + Safe(Path.GetFileNameWithoutExtension(path));
            if (!Directory.Exists(folder) && AssetDatabase.LoadAllAssetsAtPath(path).Any(o => o is Texture))
            {
                Directory.CreateDirectory(folder);
                if (importer.ExtractTextures(folder)) { AssetDatabase.Refresh(); importer.SaveAndReimport(); }
            }
            importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isHuman)
            {
                EditorUtility.DisplayDialog("Swap Player Character", Path.GetFileName(path) + " isn't a humanoid rig Unity can read. Mixamo characters downloaded 'With Skin' work.", "OK");
                return false;
            }
            return true;
        }

        static Avatar BuildAvatar(Transform root, Transform model, HumanDescription source)
        {
            // Import pose for each bone by name (the T-pose), falling back to the current pose.
            var pose = new Dictionary<string, SkeletonBone>();
            foreach (var s in source.skeleton) pose[s.name] = s;
            var skeleton = new List<SkeletonBone>
            {
                new SkeletonBone { name = root.name, position = Vector3.zero, rotation = Quaternion.identity, scale = Vector3.one },
                new SkeletonBone { name = model.name, position = model.localPosition, rotation = model.localRotation, scale = model.localScale },
            };
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t == model) continue;
                if (pose.TryGetValue(t.name, out var s)) skeleton.Add(new SkeletonBone { name = t.name, position = s.position, rotation = s.rotation, scale = s.scale });
                else skeleton.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            }
            var desc = source;
            desc.skeleton = skeleton.ToArray();
            var avatar = AvatarBuilder.BuildHumanAvatar(root.gameObject, desc);
            avatar.name = model.name.Replace("Character - ", "") + " (Player Avatar)";
            return avatar;
        }

        /// <summary>Move a socket or held item to the matching bone; if it belongs to a prefab, make a stand-in socket.</summary>
        static void Carry(GddCharacterSwap swap, Transform item, Transform newBone, Animator target)
        {
            var record = new GddCharacterSwap.Moved { item = item, originalParent = item.parent, localPosition = item.localPosition, localRotation = item.localRotation, localScale = item.localScale };
            if (!PrefabUtility.IsPartOfPrefabInstance(item))
            {
                Undo.SetTransformParent(item, newBone, "Swap Player Character");
                item.localPosition = record.localPosition;
                item.localRotation = record.localRotation;
                item.localScale = record.localScale;
                swap.moved.Add(record);
                return;
            }
            // Prefab-owned socket: create a matching one on the new bone and point the rig at it.
            var copy = new GameObject(item.name);
            Undo.RegisterCreatedObjectUndo(copy, "Swap Player Character");
            copy.transform.SetParent(newBone, false);
            copy.transform.localPosition = record.localPosition;
            copy.transform.localRotation = record.localRotation;
            copy.transform.localScale = record.localScale;
            var rig = target.GetComponent<GddRig>();
            if (rig != null)
            {
                var so = new SerializedObject(rig);
                var it = so.GetIterator();
                while (it.NextVisible(true))
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == item) it.objectReferenceValue = copy.transform;
                so.ApplyModifiedProperties();
            }
            swap.moved.Add(new GddCharacterSwap.Moved { item = copy.transform, originalParent = item, createdCopy = true });
        }

        public static void Revert(Animator target, bool log = true)
        {
            var swap = target.GetComponent<GddCharacterSwap>();
            if (swap == null) return;
            Undo.SetCurrentGroupName("Revert Player Character");
            var rig = target.GetComponent<GddRig>();
            foreach (var m in swap.moved)
            {
                if (m.item == null) continue;
                if (m.createdCopy)
                {
                    if (rig != null)
                    {
                        var so = new SerializedObject(rig);
                        var it = so.GetIterator();
                        while (it.NextVisible(true))
                            if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == m.item) it.objectReferenceValue = m.originalParent;
                        so.ApplyModifiedProperties();
                    }
                    Undo.DestroyObjectImmediate(m.item.gameObject);
                    continue;
                }
                if (m.originalParent == null) continue;
                Undo.SetTransformParent(m.item, m.originalParent, "Revert Player Character");
                m.item.localPosition = m.localPosition;
                m.item.localRotation = m.localRotation;
                m.item.localScale = m.localScale;
            }
            foreach (var g in swap.hiddenOriginals) if (g != null) { Undo.RecordObject(g, "Revert Player Character"); g.SetActive(true); }
            Undo.RecordObject(target, "Revert Player Character");
            target.avatar = swap.originalAvatar;
            if (swap.model != null) Undo.DestroyObjectImmediate(swap.model);
            Undo.DestroyObjectImmediate(swap);
            target.Rebind();
            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            if (log) Debug.Log("[GDD 110] Player character reverted to the original.");
        }

        /// <summary>A body socket or something held (it has real components). Plain bones, including end bones, stay put.</summary>
        static bool IsAttachment(Transform t) => t.name.StartsWith("GDD ") || t.GetComponents<Component>().Length > 1;

        static float Height(Transform root, Transform exclude)
        {
            bool any = false;
            var b = new Bounds();
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (exclude != null && r.transform.IsChildOf(exclude)) continue;
                var mesh = r.sharedMesh;
                if (mesh == null) continue;
                var rb = r.bounds;
                if (!any) { b = rb; any = true; } else b.Encapsulate(rb);
            }
            return any ? b.size.y : 0f;
        }

        static string Safe(string s) { foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s; }
    }

    /// <summary>Swap / Revert buttons right on the player's Body Sockets component.</summary>
    [CustomEditor(typeof(GddRig))]
    public class GddRigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var rig = (GddRig)target;
            var anim = rig.characterAnimator != null ? rig.characterAnimator : rig.GetComponent<Animator>();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
            if (GUILayout.Button("Swap Player Character...", GUILayout.Height(28))) GddCharacterSwapTool.Open();
            if (anim != null && anim.GetComponent<GddCharacterSwap>() != null && GUILayout.Button("Revert to Original Character")) GddCharacterSwapTool.Revert(anim);
        }
    }
}
