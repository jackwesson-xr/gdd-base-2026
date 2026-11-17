using UnityEngine;
using UnityEngine.SceneManagement;

namespace GDD110
{
    /// <summary>
    /// Scene loading shared by the toolkit. Every change goes through the fade transition when the game systems
    /// are running. "Next" from the last scene in the build list wraps to the first one, "Previous" from the first
    /// wraps to the last, and a scene that is not in the build list can still restart itself in the editor.
    /// </summary>
    public static class GddScenes
    {
        public static void Next() => Step(1);
        public static void Previous() => Step(-1);

        static void Step(int dir)
        {
            var current = SceneManager.GetActiveScene();
            int count = SceneManager.sceneCountInBuildSettings;
            if (count == 0) { Debug.LogWarning("[GDD 110] No scenes in the build list. Add scenes in File > Build Profiles."); return; }
            int index = current.buildIndex < 0 ? (dir > 0 ? 0 : count - 1) : (current.buildIndex + dir + count) % count;
            if (dir > 0 && current.buildIndex == count - 1)
                Debug.Log("[GDD 110] '" + current.name + "' is the last scene in the build list, so Next Scene goes back to the first one.");
            Load(index);
        }

        public static void Load(int buildIndex) => Run(() => SceneManager.LoadScene(buildIndex));

        public static void Restart()
        {
            var current = SceneManager.GetActiveScene();
            if (current.buildIndex >= 0) { Load(current.buildIndex); return; }
            string path = current.path;
#if UNITY_EDITOR
            Run(() => UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single)));
#else
            string name = current.name;
            Run(() => SceneManager.LoadScene(name));
#endif
        }

        public static void First()
        {
            if (SceneManager.sceneCountInBuildSettings > 0) Load(0);
        }

        static void Run(System.Action load)
        {
            if (GddGameRuntime.Instance != null) GddGameRuntime.Instance.Transition(load);
            else load();
        }
    }
}
