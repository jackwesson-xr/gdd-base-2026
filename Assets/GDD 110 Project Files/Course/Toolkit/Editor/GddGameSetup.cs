using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace GDD110.Editor
{
    /// <summary>
    /// Creates (or updates) the game systems assets: the settings asset in Toolkit/Resources, the audio mixer
    /// (Master > BGM, SFX, Boss, with effects ducking the music) and the music library.
    /// </summary>
    public static class GddGameSetup
    {
        const string SettingsPath = GddCourseMenus.Root + "/Resources/GddGameSettings.asset";
        const string MixerPath = "Assets/GDD 110 Project Files/Wesson Assets/Audio/Mixer/GDD110 Mixer.mixer";
        const string LibraryPath = "Assets/GDD 110 Project Files/Wesson Assets/Audio/Music/GDD110 Music Library.asset";

        [MenuItem("Tools/GDD 110/Game Systems/Set Up or Update", false, 100)]
        public static GddGameSettings SetUp()
        {
            GddCourseMenus.EnsureFolder(GddCourseMenus.Root + "/Resources");
            GddCourseMenus.EnsureFolder("Assets/GDD 110 Project Files/Wesson Assets/Audio/Mixer");
            GddCourseMenus.EnsureFolder("Assets/GDD 110 Project Files/Wesson Assets/Audio/Music");

            var settings = AssetDatabase.LoadAssetAtPath<GddGameSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<GddGameSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            settings.panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            settings.knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            settings.checkSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

            settings.fillShader = Shader.Find("GDD/UI/Fill Icons");
            settings.healthIcon = HeartSprite("Heart Icon", false);
            settings.healthFrame = HeartSprite("Heart Frame", true);
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(GddCourseMenus.Root + "/Prefabs/Course Player Rig.prefab");
            var rigHealth = rig != null ? rig.GetComponentInChildren<GddHealth>(true) : null;
            if (rigHealth != null && rigHealth.chunks == 0) { rigHealth.chunks = 5; PrefabUtility.SavePrefabAsset(rig); }

            var lib = AssetDatabase.LoadAssetAtPath<GddMusicLibrary>(LibraryPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<GddMusicLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); DefaultTracks(lib); }
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) ?? CreateMixer(MixerPath);
            lib.mixer = mixer;
            if (mixer != null)
            {
                lib.musicGroup = Group(mixer, "BGM");
                lib.effectsGroup = Group(mixer, "SFX");
                lib.bossGroup = Group(mixer, "Boss");
            }
            FillScenes(lib);
            settings.music = lib;
            EditorUtility.SetDirty(lib);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[GDD 110] Game systems ready: " + SettingsPath);
            return settings;
        }

        /// <summary>
        /// Default heart art, drawn on a pure white background on purpose: the fill shader treats #FFFFFF as clear,
        /// which is what lets students use plain JPGs.
        /// </summary>
        static Sprite HeartSprite(string name, bool frame)
        {
            const string dir = "Assets/GDD 110 Project Files/Wesson Assets/UI/Sprites/Health";
            GddCourseMenus.EnsureFolder(dir);
            string path = dir + "/" + name + ".png";
            const int size = 128;
            var t = new Texture2D(size, size, TextureFormat.RGB24, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2.6f - 1.3f, v = (y + 0.5f) / size * 2.6f - 1.05f;
                    float a = u * u + v * v - 1f;
                    float f = a * a * a - u * u * v * v * v;
                    Color c = Color.white;
                    if (frame) { if (f < 0 && f > -0.18f) c = new Color(0.12f, 0.08f, 0.14f); }
                    else if (f < 0) c = Color.Lerp(new Color(0.85f, 0.1f, 0.25f), new Color(1f, 0.42f, 0.5f), Mathf.Clamp01(v * 0.6f + 0.5f));
                    t.SetPixel(x, y, c);
                }
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), path), t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static AudioMixerGroup Group(AudioMixer mixer, string name)
        {
            var g = mixer.FindMatchingGroups("Master/" + name);
            return g.Length > 0 ? g[0] : null;
        }

        /// <summary>Adds an entry for every scene in the build list that doesn't have one yet.</summary>
        public static void FillScenes(GddMusicLibrary lib)
        {
            foreach (var s in EditorBuildSettings.scenes)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(s.path);
                if (lib.Find(name) == null) lib.sceneTracks.Add(new GddMusicLibrary.SceneTrack { scene = name });
            }
            EditorUtility.SetDirty(lib);
        }

        static AudioClip Clip(string name)
        {
            foreach (var g in AssetDatabase.FindAssets(name + " t:AudioClip"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            }
            return null;
        }

        /// <summary>Placeholder music from Feel's demos (licensed for use in games). Swap freely.</summary>
        static void DefaultTracks(GddMusicLibrary lib)
        {
            lib.defaultTrack = Clip("FeelStrikeBgMusic");
            lib.bossTrack = Clip("FeelSnakeBackgroundDrums");
            void Add(string scene, string clip) { lib.sceneTracks.Add(new GddMusicLibrary.SceneTrack { scene = scene, track = Clip(clip) }); }
            Add("Title Screen", "MMFeedbacksDemoTrack");
            Add("The Fourth Bell", "FeelStrikeBgMusic");
            Add("Crossing Complete", "FeelBrassBackgroundMusic");
            Add("Block Showcase", "FeelBrassBackgroundMusic");
        }

        // ---------- mixer (built through Unity's editor mixer API) ----------

        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        static AudioMixer CreateMixer(string path)
        {
            try
            {
                var asm = typeof(UnityEditor.Editor).Assembly;
                var mcT = asm.GetType("UnityEditor.Audio.AudioMixerController");
                var gcT = asm.GetType("UnityEditor.Audio.AudioMixerGroupController");
                var ecT = asm.GetType("UnityEditor.Audio.AudioMixerEffectController");
                var pathT = asm.GetType("UnityEditor.Audio.AudioGroupParameterPath");

                var mc = mcT.GetMethod("CreateMixerControllerAtPath", All).Invoke(null, new object[] { path });
                var master = mcT.GetProperty("masterGroup", All).GetValue(mc);
                object NewGroup(string name)
                {
                    var g = mcT.GetMethod("CreateNewGroup", All).Invoke(mc, new object[] { name, false });
                    mcT.GetMethod("AddChildToParent", All).Invoke(mc, new[] { g, master });
                    return g;
                }
                var bgm = NewGroup("BGM");
                var sfx = NewGroup("SFX");
                var boss = NewGroup("Boss");

                object Effect(string name, object group)
                {
                    var e = Activator.CreateInstance(ecT, All, null, new object[] { name }, null);
                    ecT.GetMethod("PreallocateGUIDs", All).Invoke(e, null);
                    gcT.GetMethod("InsertEffect", All).Invoke(group, new object[] { e, 1 });
                    AssetDatabase.AddObjectToAsset((UnityEngine.Object)e, (UnityEngine.Object)mc);
                    return e;
                }
                var snapshot = mcT.GetProperty("TargetSnapshot", All).GetValue(mc);
                var duck = Effect("Duck Volume", bgm);
                var send = Effect("Send", sfx);
                ecT.GetProperty("sendTarget", All).SetValue(send, duck);
                ecT.GetMethod("SetValueForMixLevel", All).Invoke(send, new object[] { mc, snapshot, 0f });
                var setParam = ecT.GetMethod("SetValueForParameter", All);
                setParam.Invoke(duck, new object[] { mc, snapshot, "Threshold", -30f });
                setParam.Invoke(duck, new object[] { mc, snapshot, "Ratio", 300f });
                setParam.Invoke(duck, new object[] { mc, snapshot, "Attack Time", 60f });
                setParam.Invoke(duck, new object[] { mc, snapshot, "Release Time", 500f });

                void Expose(object group, string name)
                {
                    var guid = gcT.GetMethod("GetGUIDForVolume", All).Invoke(group, null);
                    var p = Activator.CreateInstance(pathT, All, null, new object[] { group, guid }, null);
                    mcT.GetMethod("AddExposedParameter", All).Invoke(mc, new[] { p });
                    var prop = mcT.GetProperty("exposedParameters", All);
                    var arr = (Array)prop.GetValue(mc);
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var item = arr.GetValue(i);
                        var gf = item.GetType().GetField("guid", All);
                        if (!gf.GetValue(item).Equals(guid)) continue;
                        item.GetType().GetField("name", All).SetValue(item, name);
                        arr.SetValue(item, i);
                    }
                    prop.SetValue(mc, arr);
                }
                Expose(master, "MasterVolume");
                Expose(bgm, "BGMVolume");
                Expose(sfx, "SFXVolume");
                Expose(boss, "BossVolume");

                EditorUtility.SetDirty((UnityEngine.Object)mc);
                AssetDatabase.SaveAssets();
                return AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            }
            catch (Exception e)
            {
                Debug.LogError("[GDD 110] Could not build the audio mixer automatically: " + e);
                return AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            }
        }
    }

    [CustomEditor(typeof(GddMusicLibrary))]
    class GddMusicLibraryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("Add an entry for every scene in the build list"))
                GddGameSetup.FillScenes((GddMusicLibrary)target);
        }
    }
}
