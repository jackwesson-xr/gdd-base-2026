using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using StarterAssets;

namespace GDD110.Editor
{
    public sealed class GddHumanoidSkin : EditorWindow
    {
        const string MountName = "GDD Humanoid Skin";
        ThirdPersonController player;
        GameObject model;
        float yaw;
        float height;

        [MenuItem("GameObject/GDD 110/Apply Humanoid Skin…", false, 81)]
        static void Open(MenuCommand command)
        {
            var selected = GddCourseMenus.Target(command);
            var window = GetWindow<GddHumanoidSkin>("Apply Humanoid Skin");
            if (selected != null) window.player = selected.GetComponentInParent<ThirdPersonController>()
                ?? selected.GetComponentInChildren<ThirdPersonController>(true);
            window.minSize = new Vector2(360, 255);
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("In the FBX importer, set Rig → Animation Type → Humanoid, Avatar Definition → Create From This Model, then Apply. Drag that FBX here.", MessageType.Info);
            player = (ThirdPersonController)EditorGUILayout.ObjectField("Course Player", player, typeof(ThirdPersonController), true);
            model = (GameObject)EditorGUILayout.ObjectField("Humanoid FBX", model, typeof(GameObject), false);
            height = Mathf.Max(0, EditorGUILayout.FloatField("Height override (m)", height));
            yaw = EditorGUILayout.Slider("Extra yaw", yaw, -180, 180);
            EditorGUILayout.LabelField("Leave height at 0 to fit the player capsule.", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(player == null || model == null || EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Apply Skin"))
                    try { Apply(player, model, height, yaw); }
                    catch (Exception e) { EditorUtility.DisplayDialog("Could not apply skin", e.Message, "OK"); }
        }

        public static void Apply(ThirdPersonController player, GameObject asset, float height = 0, float yaw = 0)
        {
            var original = player.GetComponent<Animator>();
            var current = player.CharacterAnimator != null ? player.CharacterAnimator : original;
            var avatar = asset.GetComponent<Animator>();
            if (!EditorUtility.IsPersistent(asset) || avatar == null || avatar.avatar == null || !avatar.avatar.isValid || !avatar.avatar.isHuman)
                throw new ArgumentException("Choose a model asset with a valid Humanoid Avatar. Set its Rig to Humanoid and click Apply in the importer.");
            if (current == null || current.runtimeAnimatorController == null)
                throw new InvalidOperationException("Use a course player with the Starter Assets locomotion controller.");
            var controller = current.runtimeAnimatorController;
            var capsule = player.GetComponent<CharacterController>();
            var rig = player.GetComponent<GddRig>();
            if (capsule == null || rig == null) throw new InvalidOperationException("Use the Course Player Rig or set up player interactions first.");
            var oldMount = current != original && current.transform.parent != null &&
                current.transform.parent.parent == player.transform && current.transform.parent.name == MountName
                ? current.transform.parent : null;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Humanoid Skin");
            try
            {
                var mount = new GameObject(MountName);
                Undo.RegisterCreatedObjectUndo(mount, "Create skin mount");
                Undo.SetTransformParent(mount.transform, player.transform, "Mount skin");
                mount.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                mount.transform.localScale = Vector3.one;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, mount.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Add humanoid model");
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = instance.GetComponent<Animator>();
                GddHumanoidFit.Fit(mount.transform, animator, player.transform,
                    capsule.height, capsule.center.y - capsule.height * .5f, capsule.center, height, yaw);

                Undo.RecordObject(animator, "Set locomotion controller");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                Undo.AddComponent<GddSkinAnimationEvents>(instance).motor = player;

                if (oldMount != null) Undo.DestroyObjectImmediate(oldMount.gameObject);
                foreach (var r in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (!r.transform.IsChildOf(mount.transform) && r.enabled)
                    {
                        Undo.RecordObject(r, "Hide previous skin"); r.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    }
                if (original != null)
                {
                    Undo.RecordObject(original, "Disable original animation");
                    original.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(original);
                }
                Undo.RecordObject(player, "Set skin Animator");
                player.CharacterAnimator = animator;
                PrefabUtility.RecordPrefabInstancePropertyModifications(player);
                Undo.RecordObject(rig, "Remap skin attachment points");
                rig.MapNewSkin(animator);
                foreach (var socket in mount.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("GDD ") && t.name.EndsWith(" Socket")))
                    Undo.RegisterCreatedObjectUndo(socket.gameObject, "Map skin attachment point");
                PrefabUtility.RecordPrefabInstancePropertyModifications(rig);
                EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
                Selection.activeGameObject = mount;
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

    }
}
