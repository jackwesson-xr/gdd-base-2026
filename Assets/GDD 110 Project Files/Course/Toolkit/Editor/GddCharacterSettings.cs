using UnityEditor;
using UnityEngine;

namespace GDD110.Editor
{
    public static class GddCharacterSettings
    {
        const string ProfilePath = "Assets/GDD 110 Project Files/Course/Toolkit/Global Character.asset";

        [MenuItem("Tools/GDD 110/Global Character", false, 1)]
        public static void Open()
        {
            var profile=AssetDatabase.LoadAssetAtPath<GddCharacterProfile>(ProfilePath);
            if(profile==null) return;
            Selection.activeObject=profile;
            EditorGUIUtility.PingObject(profile);
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
    }
}
