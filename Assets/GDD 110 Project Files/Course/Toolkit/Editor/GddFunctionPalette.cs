using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace GDD110.Editor
{
    public class GddFunctionPalette:EditorWindow
    {
        GameObject destination;
        string search="";
        Vector2 scroll;
        public static void Open(GameObject target){var w=GetWindow<GddFunctionPalette>("GDD 110 Functions");w.destination=target;w.minSize=new Vector2(360,420);w.Show();}
        void OnGUI()
        {
            destination=(GameObject)EditorGUILayout.ObjectField("Add to",destination,typeof(GameObject),true);
            EditorGUILayout.HelpBox("Choose a function. Use its Inspector UnityEvents to connect objects. Trigger zones create a child so the object's solid collider stays intact.",MessageType.Info);
            search=EditorGUILayout.TextField("Search",search);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            foreach(var type in TypeCache.GetTypesDerivedFrom<GddEntity>().Where(t=>!t.IsAbstract&&!t.ContainsGenericParameters&&t!=typeof(GddLaunch)&&t!=typeof(GddActor)&&t!=typeof(GddRig)&&t!=typeof(GddInteractor)&&t!=typeof(GddLegacyInputBridge)&&t!=typeof(GddUiNavigation)&&t!=typeof(GddProjectile)).OrderBy(t=>t.Namespace=="GDD110"?0:1).ThenBy(t=>t.Name))
            {
                string name=ObjectNames.NicifyVariableName(type.Name.StartsWith("Gdd")?type.Name.Substring(3):type.Name);
                if(!name.ToLowerInvariant().Contains(search.ToLowerInvariant()))continue;
                using(new EditorGUI.DisabledScope(destination==null))if(GUILayout.Button(name,GUILayout.Height(28)))GddCourseMenus.AddFunction(destination,type);
            }
            EditorGUILayout.EndScrollView();
        }
    }
    [CustomEditor(typeof(GddEntity),true),CanEditMultipleObjects]
    public class GddEntityInspector:UnityEditor.Editor
    {
        bool events=true;
        public override bool RequiresConstantRepaint()=>Application.isPlaying;
        public override void OnInspectorGUI()
        {
            var entity=(GddEntity)target;
            EditorGUILayout.LabelField("GDD 110 · "+ObjectNames.NicifyVariableName(entity.GetType().Name.Replace("Gdd","")),EditorStyles.boldLabel);
            if(entity is GddPickup)EditorGUILayout.HelpBox("Choose a body attachment. The player finds its bones automatically. Add a Power to the same item; E / X picks it up, click / RT uses it, Q / B drops it.",MessageType.Info);
            else if(entity is GddTrigger)EditorGUILayout.HelpBox("An Actor entering the outlined zone runs On Activated. Drag a target into the event and choose its function. Use Once for one-time actions.",MessageType.Info);
            else if(entity is GddPower)EditorGUILayout.HelpBox("Drag in a shared Power Definition. Duplicate the preset to create your own. Use While Equipped for wearable speed/jump powers; Use Button for tools.",MessageType.Info);
            else if(entity is GddInteractable)EditorGUILayout.HelpBox("The player sees an E / controller X prompt nearby. Connect On Activated to the object or action you want to run.",MessageType.Info);
            if(Application.isPlaying)
            {
                if(entity is GddHealth health)EditorGUILayout.LabelField("Current health",health.Current.ToString("0.##")+" / "+health.maximum.ToString("0.##"));
                if(entity is GddActor actor)EditorGUILayout.LabelField("Score",actor.Score.ToString());
                if(entity is GddPickup pickup)EditorGUILayout.LabelField("Held by",pickup.Holder!=null?pickup.Holder.name:"Not held");
            }
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject,"m_Script","onActivated","onActivatedBy");
            events=EditorGUILayout.Foldout(events,"When activated — connect functions here",true);
            if(events){EditorGUILayout.PropertyField(serializedObject.FindProperty("onActivated"));EditorGUILayout.PropertyField(serializedObject.FindProperty("onActivatedBy"));}
            serializedObject.ApplyModifiedProperties();
            if(entity is GddRig rig&&GUILayout.Button("Auto Map Hands / Head / Chest / Feet")){Undo.RegisterFullObjectHierarchyUndo(rig.gameObject,"Map body sockets");rig.AutoMap();EditorUtility.SetDirty(rig);}
            using(new EditorGUI.DisabledScope(!Application.isPlaying))if(GUILayout.Button("Test Activate (Play mode)"))foreach(var obj in targets)((GddEntity)obj).Activate();
            if(GUILayout.Button("Add Another Function..."))GddFunctionPalette.Open(entity.gameObject);
        }
    }
}
