using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;

namespace GDD110.Editor
{
    /// <summary>GameObject menu entries for links, triggerables and drop rules.</summary>
    public static class GddTriggerableMenus
    {
        const string Rules = GddCourseMenus.Root + "/Drop Rules";

        public static GddDropRule Rule(GddDropRule.Mode mode, string name)
        {
            GddCourseMenus.EnsureFolder(Rules);
            string path = Rules + "/" + name + ".asset";
            var r = AssetDatabase.LoadAssetAtPath<GddDropRule>(path);
            if (r != null) return r;
            r = ScriptableObject.CreateInstance<GddDropRule>();
            r.mode = mode;
            AssetDatabase.CreateAsset(r, path);
            return r;
        }

        [MenuItem("Tools/GDD 110/Create Default Drop Rules")]
        public static void DefaultRules()
        {
            Rule(GddDropRule.Mode.StayOnGround, "Stay On Ground");
            Rule(GddDropRule.Mode.ReturnRightAway, "Return Right Away");
            Rule(GddDropRule.Mode.WaitThenReturn, "Wait 6s Then Return");
            AssetDatabase.SaveAssets();
        }

        static GameObject Make(string name, MenuCommand c, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            GameObjectUtility.SetParentAndAlign(go, c?.context as GameObject);
            if (c?.context == null && SceneView.lastActiveSceneView != null) go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            return go;
        }

        [MenuItem("GameObject/GDD 110/Triggerables/Toggle Objects", false, 40)]
        static void Toggle(MenuCommand c) => Make("Toggle Objects", c, typeof(GddToggleObjects));

        [MenuItem("GameObject/GDD 110/Triggerables/Mover (Door, Lift)", false, 41)]
        static void Mover(MenuCommand c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Mover";
            go.transform.localScale = new Vector3(2, 2, 0.3f);
            go.AddComponent<GddMover>();
            GameObjectUtility.SetParentAndAlign(go, c?.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Mover");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/GDD 110/Triggerables/Play Timeline", false, 42)]
        static void Timeline(MenuCommand c) => Make("Play Timeline", c, typeof(PlayableDirector), typeof(GddPlayTimeline));

        [MenuItem("GameObject/GDD 110/Triggerables/Camera Switch", false, 43)]
        static void CameraSwitch(MenuCommand c)
        {
            var go = Make("Camera Switch", c, typeof(GddCameraSwitch));
            var cam = new GameObject("Switch Camera", typeof(CinemachineCamera));
            cam.transform.SetParent(go.transform, false);
            cam.transform.localPosition = new Vector3(0, 3, -3);
            cam.transform.localRotation = Quaternion.Euler(35, 0, 0);
            cam.SetActive(false);
            go.GetComponent<GddCameraSwitch>().cameraToUse = cam.GetComponent<CinemachineCamera>();
        }

        [MenuItem("GameObject/GDD 110/Add Function/Link To Objects", false, 60)]
        static void Link(MenuCommand c)
        {
            var go = GddCourseMenus.Target(c);
            if (go == null) return;
            if (go.GetComponent<GddLink>() == null) Undo.AddComponent<GddLink>(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        [MenuItem("GameObject/GDD 110/Add Function/Pickup Return", false, 61)]
        static void Return(MenuCommand c)
        {
            var go = GddCourseMenus.Target(c);
            if (go == null || go.GetComponent<GddPickup>() == null) { EditorUtility.DisplayDialog("Pickup Return", "Select an object with a Pickup function first.", "OK"); return; }
            var r = go.GetComponent<GddPickupReturn>();
            if (r == null) r = Undo.AddComponent<GddPickupReturn>(go);
            if (r.rule == null) r.rule = Rule(GddDropRule.Mode.WaitThenReturn, "Wait 6s Then Return");
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
