using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GDD110.Editor
{
    /// <summary>
    /// Mixamo characters, ready to use:
    /// - Dropping a new Mixamo FBX into the project sets it up as Humanoid automatically.
    /// - Right-click any model > GDD 110 > Make Humanoid does the same for older files (and pulls out textures).
    /// - Right-click > GDD 110 > Add Wearable Body To Level drops it into the open level, ready to press E and wear.
    /// </summary>
    public class GddMixamoImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            var importer = (ModelImporter)assetImporter;
            if (!importer.importSettingsMissing) return;   // only the first import; never override later choices
            if (!LooksLikeMixamo(assetPath)) return;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            Debug.Log("[GDD 110] " + Path.GetFileName(assetPath) + " looks like a Mixamo character: set up as Humanoid.");
        }

        /// <summary>Mixamo skeletons name every bone "mixamorig...". Reads the start of the file to check.</summary>
        static bool LooksLikeMixamo(string path)
        {
            try
            {
                string full = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
                using (var f = File.OpenRead(full))
                {
                    var buf = new byte[Mathf.Min((int)f.Length, 4 * 1024 * 1024)];
                    int n = f.Read(buf, 0, buf.Length);
                    return Encoding.ASCII.GetString(buf, 0, n).Contains("mixamorig");
                }
            }
            catch { return false; }
        }

        static bool SelectedModel(out string path)
        {
            path = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            return path != null && AssetImporter.GetAtPath(path) is ModelImporter;
        }

        [MenuItem("Assets/GDD 110/Make Humanoid (Mixamo)", false, 1999)]
        static void MakeHumanoid()
        {
            foreach (var o in Selection.objects)
            {
                var p = AssetDatabase.GetAssetPath(o);
                var imp = AssetImporter.GetAtPath(p) as ModelImporter;
                if (imp == null) continue;
                if (GddCharacterSwapTool.PrepareImport(imp, p)) Debug.Log("[GDD 110] " + Path.GetFileName(p) + " is ready: Humanoid avatar, textures extracted.");
            }
        }
        [MenuItem("Assets/GDD 110/Make Humanoid (Mixamo)", true)]
        static bool MakeHumanoidValid() => SelectedModel(out _);

        [MenuItem("Assets/GDD 110/Add Wearable Body To Level", false, 2001)]
        static void AddWearable()
        {
            if (!SelectedModel(out var p)) return;
            var imp = (ModelImporter)AssetImporter.GetAtPath(p);
            if (!GddCharacterSwapTool.PrepareImport(imp, p)) return;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            Undo.RegisterCreatedObjectUndo(go, "Add Wearable Body");
            var view = SceneView.lastActiveSceneView;
            if (view != null) go.transform.position = view.pivot;
            var player = Object.FindAnyObjectByType<GddActor>();
            if (player != null && view == null) go.transform.position = player.transform.position + player.transform.forward * 3f;
            var w = Undo.AddComponent<GddWearableBody>(go);
            w.friendlyName = model.name;
            Selection.activeGameObject = go;
        }
        [MenuItem("Assets/GDD 110/Add Wearable Body To Level", true)]
        static bool AddWearableValid() => SelectedModel(out _);
    }
}
