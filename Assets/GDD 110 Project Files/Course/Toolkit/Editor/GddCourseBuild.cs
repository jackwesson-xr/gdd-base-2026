using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace GDD110.Editor
{
    public static class GddCourseBuild
    {
        [MenuItem("Tools/GDD 110/Prepare Course Project")]
        public static void PrepareCourseProject()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save your scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                GddCourseMenus.GenerateLibrary();
                var entry=EditorBuildSettings.scenes.First(s=>s.path.Contains("The Fourth Bell"));
                var scene=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Single);
                var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StarterAssets.ThirdPersonController>(true)).First().gameObject;
                GddCourseMenus.PreparePlayer(player);
                var actions=GddCourseMenus.Add<GddSceneActions>(player);var health=player.GetComponent<GddHealth>();
                if(health.onEmpty.GetPersistentEventCount()==0)UnityEventTools.AddPersistentListener(health.onEmpty,actions.RestartScene);
                var group=player.transform;while(group.parent!=null)group=group.parent;
                PrefabUtility.SaveAsPrefabAsset(group.gameObject,GddCourseMenus.Root+"/Prefabs/Course Player Rig.prefab");
                GddCourseMenus.CleanPrefabEvents(GddCourseMenus.Root+"/Prefabs/Course Player Rig.prefab");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                foreach(var configured in EditorBuildSettings.scenes)
                {
                    var uiScene=EditorSceneManager.OpenScene(configured.path,OpenSceneMode.Single);
                    var system=uiScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).FirstOrDefault();
                    if(system!=null)
                    {
                        var navigation=GddCourseMenus.Add<GddUiNavigation>(system.gameObject);
                        var buttons=uiScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.UI.Button>(true)).Where(b=>b.gameObject.activeInHierarchy).ToArray();
                        navigation.firstSelection=buttons.FirstOrDefault(b=>b.name.Contains("Start"))??buttons.FirstOrDefault();
                        EditorSceneManager.MarkSceneDirty(uiScene);EditorSceneManager.SaveScene(uiScene);
                    }
                }
                ConfigureWebGL();AuditMaterials();AssetDatabase.SaveAssets();
            }
            finally{if(setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path,OpenSceneMode.Single);}
            Debug.Log("GDD110_PREPARED");
        }
        [MenuItem("Tools/GDD 110/WebGL/Configure URP and Unity Play Settings")]
        public static void ConfigureWebGL()
        {
            if(GraphicsSettings.defaultRenderPipeline==null)
            {
                var fallback=QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                if(fallback==null)fallback=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/GDD 110 Project Files/Unity Template/Settings/Mobile_RPAsset.asset");
                if(fallback!=null)GraphicsSettings.defaultRenderPipeline=fallback;
            }
            if(!(GraphicsSettings.defaultRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset))throw new Exception("The course must use URP.");
            for(int i=0;i<QualitySettings.names.Length;i++){var p=QualitySettings.GetRenderPipelineAssetAt(i);if(p!=null&&!(p is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset))throw new Exception("A quality level does not use URP.");}
            int webQuality=Array.FindIndex(QualitySettings.names,n=>n=="Mobile");
            if(webQuality<0)webQuality=0;
            for(int i=0;i<QualitySettings.names.Length;i++)
            {
                Exception error;
                bool ok=i==webQuality?QualitySettings.TryIncludePlatformAt("WebGL",i,out error):QualitySettings.TryExcludePlatformAt("WebGL",i,out error);
                if(!ok&&error!=null)throw error;
            }
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL,ManagedStrippingLevel.Minimal);
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.threadsSupport=false;
            PlayerSettings.WebGL.initialMemorySize=256;PlayerSettings.WebGL.maximumMemorySize=2048;
            PlayerSettings.WebGL.template="APPLICATION:Default";
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.runInBackground=true;
            PlayerSettings.defaultWebScreenWidth=1280;PlayerSettings.defaultWebScreenHeight=720;
        }
        public static void AuditMaterials()
        {
            var rows=new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/GDD 110 Project Files/Course"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(m!=null)rows.Add(path+"\t"+(m.shader!=null?m.shader.name:"MISSING")+"\t"+m.GetTag("RenderPipeline",false,""));
            }
            File.WriteAllLines(Path.GetFullPath("../toolkit-material-audit.tsv"),rows);
        }
        [MenuItem("Tools/GDD 110/WebGL/Build for Unity Play...")]
        public static void BuildForUnityPlay()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("WebGL Build Support needed",
                    "In Unity Hub, open Installs, find Unity " + Application.unityVersion +
                    ", and choose Add modules → WebGL Build Support. Restart Unity, then try again.", "OK");
                return;
            }

            string folder = EditorUtility.SaveFolderPanel("Choose a WebGL output folder outside Assets", "", "GDD110_WebGL");
            if (string.IsNullOrEmpty(folder)) return;
            string relative = Path.GetRelativePath(Application.dataPath, folder);
            if (!Path.IsPathRooted(relative) && relative != ".." &&
                !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("Choose a different folder",
                    "Put the WebGL output outside Assets so Unity doesn't import the build files.", "OK");
                return;
            }
            Build(folder);
        }
        public static void BuildWebGLBatch()=>Build(Path.GetFullPath("../GDD110_WebGL"));
        public static void Build(string output)
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new Exception("Install WebGL Build Support for this editor in Unity Hub, then try again.");
            ConfigureWebGL();
            int oldQuality=QualitySettings.GetQualityLevel();string oldPipelinePath=AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
            int webQuality=QualitySettings.GetActiveQualityLevelsForPlatform("WebGL")[0];
            try
            {
                QualitySettings.SetQualityLevel(webQuality,false);
                GraphicsSettings.defaultRenderPipeline=QualitySettings.GetRenderPipelineAssetAt(webQuality);
                AssetDatabase.SaveAssets();
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None});
                File.WriteAllText(Path.GetFullPath("../webgl-build-result.txt"),report.summary.result+"\nBytes: "+report.summary.totalSize+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nDuration: "+report.summary.totalTime);
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed. See the Unity Console and build log.");
                MakeResponsive(output);
                if (!Application.isBatchMode) GddStartHere.MarkDone(GddStartHere.WebBuilt);
                Debug.Log("GDD110_WEBGL_BUILD_SUCCEEDED "+output);
            }
            finally{GraphicsSettings.defaultRenderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(oldPipelinePath);QualitySettings.SetQualityLevel(oldQuality,false);AssetDatabase.SaveAssets();}
        }
        public static void MakeResponsive(string output)
        {
            string path=Path.Combine(output,"index.html");if(!File.Exists(path))return;
            string html=File.ReadAllText(path);
            if(!html.Contains("gdd-responsive"))File.WriteAllText(path,html.Replace("</head>",@"<style id='gdd-responsive'>
html,body{width:100%;height:100%;margin:0;overflow:hidden}
#unity-container{position:fixed!important;inset:0!important;width:100%!important;height:100%!important;transform:none!important}
#unity-fullscreen-container{width:100%!important;height:calc(100% - 38px)!important}
#unity-fullscreen-container:fullscreen{height:100%!important}
#unity-canvas{display:block;width:100%!important;height:100%!important;outline:none}
#unity-footer{height:38px;background:white}
</style></head>"));
        }
        [MenuItem("Tools/GDD 110/Student Quick Start")]
        public static void Guide(){var file=AssetDatabase.LoadAssetAtPath<TextAsset>(GddCourseMenus.Root+"/STUDENT_GUIDE.txt");if(file!=null)AssetDatabase.OpenAsset(file);}
    }
}
