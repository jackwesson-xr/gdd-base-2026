using System;
using System.Collections.Generic;
using GDD110;
using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GDD110.Editor
{
    public sealed class GddStartHere : EditorWindow
    {
        const string Opening = "Assets/GDD 110 Project Files/Course/Scenes/Title Screen.unity";
        const string Playground = "Assets/GDD 110 Project Files/Course/Scenes/The Fourth Bell.unity";
        const string Showcase = "Assets/GDD 110 Project Files/Block Showcase/Scenes/Block Showcase.unity";
        const string Images = "Assets/GDD 110 Project Files/Course/Toolkit/Editor/Tutorial Art/";
        const string PainterExamplePath = "Assets/GDD 110 Project Files/Course/Inherited/Character/Textures/Armature_Body_";
        const string PlayTitle = "play-title";
        const string Jump = "jump";
        const string Lift = "lift";
        internal const string MaterialCreated = "material";
        internal const string WebBuilt = "web-build";

        static readonly string[] Tabs = { "Play", "Explore", "Make it yours", "Build" };
        static readonly Color Cream = new Color(.91f,.95f,.91f);
        static readonly Color Mist = new Color(.73f,.80f,.77f);
        static readonly Color Mint = new Color(.69f,.95f,.83f);
        static readonly Color Panel = new Color(.12f,.17f,.15f);
        static readonly Color Border = new Color(.25f,.35f,.30f);
        static GddFourthBell bell;
        static ThirdPersonController motor;
        static double nextObservation;
        static bool launchedFromGround;

        readonly List<(Label label, string text, Func<bool> done)> checks = new();
        VisualElement page;
        Button[] tabs;
        int step;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (Application.isBatchMode) return;
            EditorApplication.update += ObservePlay;
            EditorApplication.delayCall += () =>
            {
                const string sessionKey = "GDD.Bell.Tutorial.Seen";
                if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(sessionKey,false)) return;
                SessionState.SetBool(sessionKey,true);
                Open();
            };
        }

        static void ObservePlay()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            {
                bell = null;
                motor = null;
                launchedFromGround = false;
                return;
            }

            if (SceneManager.GetActiveScene().path == Opening)
                MarkDone(PlayTitle);
            if (!bell)
            {
                double now = EditorApplication.timeSinceStartup;
                if (now < nextObservation) return;
                nextObservation = now + .3;
                bell = FindAnyObjectByType<GddFourthBell>();
                if (bell) motor = bell.player.GetComponent<ThirdPersonController>();
            }
            if (!motor) return;
            if (motor.Grounded) launchedFromGround = true;
            if (launchedFromGround && motor.VerticalSpeed > 3f) MarkDone(Jump);
            if (bell.CurrentLevel > 0) MarkDone(Lift);
        }

        static string Key(string task) => "GDD.Bell.Tutorial." + PlayerSettings.productGUID + "." + task;
        static bool Done(string task) => EditorPrefs.GetBool(Key(task),false);
        internal static void MarkDone(string task)
        {
            if (!Done(task)) EditorPrefs.SetBool(Key(task),true);
        }

        [MenuItem("Tools/GDD 110/Start Here", false, 0)]
        static void Open()
        {
            var window = GetWindow<GddStartHere>("GDD · Start Here");
            window.minSize = new Vector2(340, 390);
            window.Show();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            var bodyFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/GDD 110 Project Files/The Fourth Bell/Resources/GddHud/Very Popular.ttf");
            if (bodyFont) root.style.unityFont=bodyFont;
            root.style.backgroundColor = new Color(.075f,.105f,.092f);
            root.style.color = Cream;
            root.style.paddingLeft = root.style.paddingRight = 16;
            root.style.paddingTop = 12;
            var heading = Text("Game Design 110", 21, Cream, true);
            var titleFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/GDD 110 Project Files/The Fourth Bell/Resources/GddHud/papercuts-2.ttf");
            if (titleFont) heading.style.unityFont=titleFont;
            root.Add(heading);
            var navigation = new VisualElement();
            navigation.style.flexDirection = FlexDirection.Row;
            navigation.style.marginTop = 9;
            navigation.style.marginBottom = 8;
            root.Add(navigation);
            tabs = new Button[Tabs.Length];
            for (int i=0; i<Tabs.Length; i++)
            {
                int target = i;
                tabs[i] = new Button(() => ShowPage(target)) { text = Tabs[i] };
                tabs[i].style.flexGrow = 1;
                tabs[i].style.paddingTop = tabs[i].style.paddingBottom = 6;
                navigation.Add(tabs[i]);
            }
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            root.Add(scroll);
            page = scroll.contentContainer;
            page.style.paddingRight = 4;
            page.style.paddingBottom = 16;
            ShowPage(Mathf.Clamp(SessionState.GetInt("GDD.Bell.Tutorial.Page",0),0,Tabs.Length-1));
            root.schedule.Execute(RefreshChecks).Every(240);
        }

        void ShowPage(int index)
        {
            step = index;
            SessionState.SetInt("GDD.Bell.Tutorial.Page",step);
            page.Clear();
            checks.Clear();
            for (int i=0;i<tabs.Length;i++)
            {
                tabs[i].style.backgroundColor = i==step ? Mint : Panel;
                tabs[i].style.color = i==step ? Panel : Cream;
            }
            switch (step)
            {
                case 0:
                    Banner("Play The Fourth Bell", "Title.png");
                    Card("Start the game", "Open the title in Play Mode. Choose Start Game.", "Play from Title",
                        () => OpenScene(Opening,true), () => Done(PlayTitle), "Title running in Play Mode");
                    Card("Block Showcase", "Try the ready-made objects one at a time.", "Open Block Showcase",
                        () => OpenScene(Showcase,false));
                    break;
                case 1:
                    Banner("Explore the school hall", "Assembly Hall.png");
                    Card("Jump and climb", "Use Space or gamepad south to jump. Move toward a low ledge and press Jump to mantle.",
                        "Play from the Hall", () => OpenScene(Playground,true), () => Done(Jump), "Jump performed in Play Mode");
                    Card("Run the service lift", "Restore power and enter the lift. H or D-pad Up hides or shows the next step.", null, null,
                        () => Done(Lift), "Reached the culvert", "Service Lift.png");
                    Card("Read the markers", "Mint marks gear and pickups. Orange marks enemies and hazards. Left click or RT uses the equipped tool.", null, null);
                    break;
                case 2:
                    Banner("Change the character and art", "Signal Orchard.png");
                    Card("Swap the character", "Import a skinned Mixamo FBX as Humanoid. Assign it in Global Character.",
                        "Open Global Character", GddCharacterSettings.Open);
                    Card("Create Painter materials", "Export using URP Metallic Standard. Choose the texture folder inside Assets.",
                        "Choose Texture Folder", GddSubstanceMaterials.ChooseFolder, () => Done(MaterialCreated), "Materials generated");
                    AddPainterExample();
                    break;
                case 3:
                    Banner("Build for the browser", "The Culvert.png");
                    Card("Build for Unity Play", "Choose a folder outside Assets. ZIP the output with index.html at the top level.",
                        "Build WebGL", GddCourseBuild.BuildForUnityPlay, () => Done(WebBuilt), "WebGL build completed");
                    Card("Author a tutorial", "In the Project window, use Create → Tutorials → Tutorial.", null, null);
                    break;
            }

            if (step < Tabs.Length-1)
                page.Add(ActionButton("Next  >", () => ShowPage(step+1), false));
            RefreshChecks();
        }

        void Banner(string heading, string image)
        {
            var title = Text(heading, 24, Cream, true);
            title.style.marginTop = 5;
            title.style.marginBottom = 9;
            page.Add(title);
            AddPicture(page,image,164);
        }

        static void AddPicture(VisualElement parent,string image,float height)
        {
            var picture = AssetDatabase.LoadAssetAtPath<Texture2D>(Images+image);
            if (!picture) return;
            var banner = new Image { image = picture, scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
            banner.style.height = height;
            banner.style.marginBottom = 10;
            parent.Add(banner);
        }

        void Card(string title, string copy, string actionText, Action action, Func<bool> done = null, string checkText = null, string image = null)
        {
            var card = new VisualElement();
            card.style.backgroundColor = Panel;
            card.style.borderLeftColor = Border;
            card.style.borderLeftWidth = 2;
            card.style.marginBottom = 9;
            card.style.paddingLeft = card.style.paddingRight = 11;
            card.style.paddingTop = card.style.paddingBottom = 8;
            card.Add(Text(title, 14, Cream, true));
            var instruction = Text(copy, 13, Mist);
            instruction.style.marginTop = 4;
            card.Add(instruction);
            if (image != null) AddPicture(card,image,126);
            if (action != null) card.Add(ActionButton(actionText,action,true));
            if (done != null)
            {
                var check = Text(checkText, 12, Mist);
                check.style.marginTop = 6;
                card.Add(check);
                checks.Add((check,checkText,done));
            }
            page.Add(card);
        }

        void AddPainterExample()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 10;
            foreach (var (file,caption) in new[]
            {
                ("AlbedoTransparency","Base color"), ("Normal","Normal"), ("MetallicSmoothness","Metal + smoothness")
            })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PainterExamplePath+file+".tif");
                if (!texture) continue;
                var tile = new VisualElement();
                tile.style.flexBasis = 0;
                tile.style.flexGrow = 1;
                tile.style.marginRight = 5;
                var preview = new Image { image=texture, scaleMode=ScaleMode.ScaleAndCrop, pickingMode=PickingMode.Ignore };
                preview.style.height = 60;
                tile.Add(preview);
                tile.Add(Text(caption,11,Mist));
                row.Add(tile);
            }
            page.Add(row);
        }

        void RefreshChecks()
        {
            foreach (var (label, text, done) in checks)
            {
                bool completed = done();
                label.text = (completed ? "Done  " : "To do  ") + text;
                label.style.color = completed ? Mint : Mist;
            }
        }

        static Label Text(string value, int size, Color color, bool bold = false)
        {
            var label = new Label(value);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = size;
            label.style.color = color;
            if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        static Button ActionButton(string caption, Action action, bool requireEditMode)
        {
            var button = new Button(action.Invoke) { text = caption };
            button.style.alignSelf = Align.FlexStart;
            button.style.marginTop = 8;
            button.style.paddingLeft = button.style.paddingRight = 10;
            button.style.paddingTop = button.style.paddingBottom = 4;
            button.style.backgroundColor = Mint;
            button.style.color = Panel;
            if (requireEditMode)
            {
                button.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                button.schedule.Execute(() => button.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode)).Every(250);
            }
            return button;
        }

        static void OpenScene(string path, bool play)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path);
            if (play) EditorApplication.EnterPlaymode();
        }
    }
}
