using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GDD110
{
    /// <summary>
    /// Game-wide systems that follow the player through every level (created automatically, never placed by hand):
    /// settings window (`), developer window (Shift + `), quick save / load / restart (0 / 8 / 7),
    /// hold &lt; or &gt; to change level, fade transitions, the / key back to the game library, and the music player.
    /// Everything is configured in Toolkit/Resources/GddGameSettings.
    /// </summary>
    [AddComponentMenu("")]
    [DefaultExecutionOrder(-200)]
    public class GddGameRuntime : MonoBehaviour
    {
        public static GddGameRuntime Instance { get; private set; }
        public GddGameSettings Settings { get; private set; }

        // Developer options. All start off.
        public static bool Fly, Unkillable, SlowMoAllowed, SlowMoActive;
        public static int Jumps = 1;
        /// <summary>1, or 0.2 while caps-lock slow motion is on. The juice layer multiplies its own time scale by this.</summary>
        public static float TimeScaleMultiplier => SlowMoActive ? 0.2f : 1f;
        /// <summary>Set by the juice layer when it owns Time.timeScale.</summary>
        public static bool ExternalTimeControl;
        public static bool WindowOpen => Instance != null && Instance.openWindow != null;

        float sensitivity, master, music, effects;
        GddMusicPlayer musicPlayer;

        Canvas canvas;
        Image fade;
        TextMeshProUGUI toast;
        CanvasGroup toastGroup;
        float toastUntil;
        RectTransform holdPanel, holdFill;
        TextMeshProUGUI holdLabel;
        GameObject libraryPrompt;
        GameObject settingsWindow, cheatsWindow, openWindow;
        TextMeshProUGUI jumpsLabel;
        CursorLockMode savedLock;
        bool savedVisible;

        bool loading, firstLoad = true;
        float holdTime;
        int holdDir;
        bool holdDone;
        float libraryHeld = -1f;
        bool libraryHoldFired;
        bool appliedSlowMo;
        bool flying;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void GddOpenLibrary(string url, int newTab);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var s = GddGameSettings.Load();
            if (s == null || Instance != null) return;
            Fly = Unkillable = SlowMoAllowed = SlowMoActive = false;
            Jumps = 1;
            var go = new GameObject("[GDD Game Systems]");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<GddGameRuntime>();
            r.Settings = s;
            r.Setup();
        }

        void Setup()
        {
            Instance = this;
            sensitivity = PlayerPrefs.GetFloat("gdd110.sensitivity", Settings.mouseSensitivity);
            master = PlayerPrefs.GetFloat("gdd110.master", Settings.masterVolume);
            music = PlayerPrefs.GetFloat("gdd110.music", Settings.musicVolume);
            effects = PlayerPrefs.GetFloat("gdd110.effects", Settings.effectsVolume);
            BuildUi();
            if (Settings.music != null) { musicPlayer = gameObject.AddComponent<GddMusicPlayer>(); musicPlayer.Init(Settings.music); }
            ApplyAudio();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SetFade(1f);
        }

        void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            // The first level can finish loading before this object hears about it, so fade in on our first frame.
            yield return null;
            EnsureHealthBar();
            if (firstLoad) { firstLoad = false; loading = false; yield return FadeTo(0f, Settings.fadeInSeconds); }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            flying = false;
            EnsureHealthBar();
            if (loading || firstLoad) { loading = false; firstLoad = false; StartCoroutine(FadeTo(0f, Settings.fadeInSeconds)); }
        }

        /// <summary>Give the player a health bar unless the level already has one.</summary>
        void EnsureHealthBar()
        {
            if (!Settings.showHealthBar || FindAnyObjectByType<GddHealthHud>() != null || FindAnyObjectByType<GddFourthBell>() != null) return;
            var actor = FindAnyObjectByType<GddActor>();
            if (actor == null || actor.GetComponent<GddHealth>() == null) return;
            var go = new GameObject("Health Bar", typeof(RectTransform));
            go.transform.SetParent(GddHud.Get().HealthArea, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            go.AddComponent<GddHealthHud>().Configure(Settings);
        }

        // ---------- scene changes ----------

        /// <summary>Fade out, run the load, and fade back in when the new level arrives.</summary>
        public void Transition(Action load)
        {
            if (loading) return;
            StartCoroutine(FadeAndLoad(load));
        }

        IEnumerator FadeAndLoad(Action load)
        {
            loading = true;
            CloseWindow();
            yield return FadeTo(1f, Settings.fadeOutSeconds);
            load();
        }

        IEnumerator FadeTo(float target, float seconds)
        {
            fade.raycastTarget = true;
            float start = fade.color.a;
            for (float t = 0; t < 1f && seconds > 0f; t += Time.unscaledDeltaTime / seconds)
            {
                SetFade(Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t)));
                yield return null;
            }
            SetFade(target);
            fade.raycastTarget = target > 0.5f;
        }

        void SetFade(float a) { var c = Settings.fadeColor; c.a = a; fade.color = c; }

        // ---------- input ----------

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && !loading) HandleKeys(kb);
            UpdateToast();
            ApplyCheats(kb);
            StarterAssets.ThirdPersonController.LookSensitivity = openWindow != null ? 0f : sensitivity;
            if (openWindow != null) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        void HandleKeys(Keyboard kb)
        {
            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            if (kb[Settings.settingsKey].wasPressedThisFrame)
            {
                var want = shift ? cheatsWindow : settingsWindow;
                if (openWindow == want) CloseWindow(); else OpenWindow(want);
                return;
            }
            if (openWindow != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) CloseWindow();
                return;
            }

            if (HandleLibrary(kb)) return;

            if (kb[Settings.quickSaveKey].wasPressedThisFrame) QuickSave();
            if (kb[Settings.quickLoadKey].wasPressedThisFrame) QuickLoad();
            if (kb[Settings.restartLevelKey].wasPressedThisFrame) { Toast("Restarting level"); GddScenes.Restart(); }

            int dir = kb[Settings.nextLevelKey].isPressed ? 1 : kb[Settings.previousLevelKey].isPressed ? -1 : 0;
            if (dir != holdDir) { holdDir = dir; holdTime = 0f; holdDone = false; }
            if (dir != 0 && !holdDone)
            {
                holdTime += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(holdTime / Settings.levelHoldSeconds);
                holdPanel.gameObject.SetActive(true);
                holdFill.anchorMax = new Vector2(k, 1f);
                holdLabel.text = dir > 0 ? "Next level  >" : "<  Previous level";
                if (k >= 1f) { holdDone = true; if (dir > 0) GddScenes.Next(); else GddScenes.Previous(); }
            }
            else holdPanel.gameObject.SetActive(false);
        }

        /// <summary>/ tap: ask. / again or Enter: go. Any other key: stay. Holding /: go straight there.</summary>
        bool HandleLibrary(Keyboard kb)
        {
            // In WebGL the browser tab opens on the next key-up, so "go" is decided on key-down.
            var key = kb[Settings.libraryKey];
            if (key.wasPressedThisFrame)
            {
                if (libraryPrompt.activeSelf) { OpenLibrary(); libraryHoldFired = true; libraryHeld = -1f; return true; }
                libraryHeld = 0f;
                libraryHoldFired = false;
            }
            if (key.isPressed && libraryHeld >= 0f && !libraryHoldFired)
            {
                libraryHeld += Time.unscaledDeltaTime;
                if (libraryHeld >= Settings.libraryHoldSeconds) { libraryHoldFired = true; OpenLibrary(); return true; }
            }
            if (key.wasReleasedThisFrame)
            {
                if (!libraryHoldFired) libraryPrompt.SetActive(true);
                libraryHeld = -1f;
                return true;
            }
            if (libraryPrompt.activeSelf && kb.anyKey.wasPressedThisFrame)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) OpenLibrary();
                else libraryPrompt.SetActive(false);
                return true;
            }
            return key.isPressed;
        }

        public void OpenLibrary()
        {
            libraryPrompt.SetActive(false);
            Toast("Opening the 110 Game Library");
#if UNITY_WEBGL && !UNITY_EDITOR
            GddOpenLibrary(Settings.libraryUrl, Settings.openLibraryInNewTab ? 1 : 0);
#else
            Application.OpenURL(Settings.libraryUrl);
#if !UNITY_EDITOR
            Application.Quit();
#endif
#endif
        }

        // ---------- save / load position ----------

        string SaveKey => "gdd110.save." + SceneManager.GetActiveScene().path;

        public void QuickSave()
        {
            var actor = FindAnyObjectByType<GddActor>();
            if (actor == null) { Toast("No player in this level"); return; }
            var t = actor.transform;
            var c = CultureInfo.InvariantCulture;
            PlayerPrefs.SetString(SaveKey, string.Join(";", t.position.x.ToString(c), t.position.y.ToString(c), t.position.z.ToString(c), t.eulerAngles.y.ToString(c)));
            PlayerPrefs.Save();
            Toast("Position saved");
        }

        public void QuickLoad()
        {
            var actor = FindAnyObjectByType<GddActor>();
            if (actor == null) { Toast("No player in this level"); return; }
            if (!PlayerPrefs.HasKey(SaveKey)) { Toast("No saved position in this level yet"); return; }
            var p = PlayerPrefs.GetString(SaveKey).Split(';');
            var c = CultureInfo.InvariantCulture;
            var pos = new Vector3(float.Parse(p[0], c), float.Parse(p[1], c), float.Parse(p[2], c));
            float yaw = float.Parse(p[3], c);
            var cc = actor.GetComponent<CharacterController>();
            bool was = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            actor.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            if (cc != null) cc.enabled = was;
            var tpc = actor.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null) tpc.SetLook(yaw, 10f);
            Toast("Position loaded");
        }

        // ---------- developer options ----------

        void ApplyCheats(Keyboard kb)
        {
            if (kb != null && SlowMoAllowed && kb.capsLockKey.wasPressedThisFrame) { SlowMoActive = !SlowMoActive; Toast(SlowMoActive ? "Slow motion 0.2x" : "Normal speed"); }
            if (!SlowMoAllowed) SlowMoActive = false;
            if (SlowMoActive != appliedSlowMo)
            {
                appliedSlowMo = SlowMoActive;
                if (!ExternalTimeControl) Time.timeScale = TimeScaleMultiplier;
                Time.fixedDeltaTime = 0.02f * TimeScaleMultiplier;
            }
            StarterAssets.ThirdPersonController.AirJumps = Mathf.Max(0, Jumps - 1);
            GddHealth.PlayerInvulnerable = Unkillable;

            var actor = FindAnyObjectByType<GddActor>();
            if (actor == null) return;
            var tpc = actor.GetComponent<StarterAssets.ThirdPersonController>();
            var cc = actor.GetComponent<CharacterController>();
            if (Fly != flying)
            {
                flying = Fly;
                if (tpc != null) tpc.enabled = !Fly;
                if (cc != null) cc.enabled = !Fly;
            }
            if (!Fly || kb == null || openWindow != null) return;
            var cam = Camera.main != null ? Camera.main.transform : actor.transform;
            Vector3 fwd = cam.forward, right = cam.right;
            Vector3 move = Vector3.zero;
            if (kb.wKey.isPressed) move += fwd;
            if (kb.sKey.isPressed) move -= fwd;
            if (kb.dKey.isPressed) move += right;
            if (kb.aKey.isPressed) move -= right;
            if (kb.spaceKey.isPressed || kb.eKey.isPressed) move += Vector3.up;
            if (kb.leftCtrlKey.isPressed || kb.qKey.isPressed) move -= Vector3.up;
            float speed = kb.leftShiftKey.isPressed ? 22f : 9f;
            actor.transform.position += move.normalized * speed * Time.unscaledDeltaTime;
            Vector3 flat = Vector3.ProjectOnPlane(fwd, Vector3.up);
            if (flat.sqrMagnitude > 0.01f) actor.transform.rotation = Quaternion.LookRotation(flat);
        }

        // ---------- windows ----------

        void OpenWindow(GameObject w)
        {
            if (openWindow == null) { savedLock = Cursor.lockState; savedVisible = Cursor.visible; }
            if (openWindow != null) openWindow.SetActive(false);
            openWindow = w;
            w.SetActive(true);
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(null);
            }
            libraryPrompt.SetActive(false);
        }

        public void CloseWindow()
        {
            if (openWindow == null) return;
            openWindow.SetActive(false);
            openWindow = null;
            Cursor.lockState = savedLock;
            Cursor.visible = savedVisible;
        }

        void ApplyAudio()
        {
            if (musicPlayer != null) musicPlayer.SetVolumes(master, music, effects);
            else AudioListener.volume = master;
        }

        // ---------- toast ----------

        public void Toast(string text)
        {
            toast.text = text;
            toastUntil = Time.unscaledTime + 1.8f;
        }

        void UpdateToast()
        {
            float left = toastUntil - Time.unscaledTime;
            toastGroup.alpha = Mathf.Clamp01(left / 0.3f);
        }

        // ---------- UI construction ----------

        static readonly Color Glass = new Color(0.07f, 0.08f, 0.11f, 0.94f);
        static readonly Color Accent = new Color(0.25f, 0.75f, 1f);

        void BuildUi()
        {
            var cgo = new GameObject("Game Systems UI", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();

            // Toast (top center)
            var tgo = Rect("Toast", cgo.transform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(900, 60));
            toastGroup = tgo.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0;
            toastGroup.blocksRaycasts = false;
            toast = Label(tgo, "", 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            toast.outlineWidth = 0.18f;
            toast.outlineColor = new Color32(0, 0, 0, 180);

            // Hold-to-change-level bar (lower right)
            holdPanel = Panel("Level Hold", cgo.transform, new Vector2(1f, 0f), new Vector2(-40, 40), new Vector2(380, 70), Glass);
            holdPanel.pivot = new Vector2(1f, 0f);
            var track = Rect("Track", holdPanel, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(340, 10));
            track.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.15f);
            holdFill = Rect("Fill", track, new Vector2(0, 0), Vector2.zero, Vector2.zero);
            holdFill.anchorMin = Vector2.zero; holdFill.anchorMax = new Vector2(0, 1); holdFill.offsetMin = holdFill.offsetMax = Vector2.zero;
            holdFill.gameObject.AddComponent<Image>().color = Accent;
            var hl = Rect("Label", holdPanel, new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(340, 34));
            holdLabel = Label(hl, "", 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            holdPanel.gameObject.SetActive(false);

            // Library prompt (bottom center, above the action prompts)
            var lp = Panel("Library Prompt", cgo.transform, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(980, 96), Glass);
            var lpt = Rect("Text", lp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 86));
            Label(lpt, "<b>Back to the 110 Game Library?</b>\n<size=80%>Press <b>/</b> or <b>Enter</b> to go. Any other key to stay.</size>", 28, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            libraryPrompt = lp.gameObject;
            libraryPrompt.SetActive(false);

            settingsWindow = BuildSettings(cgo.transform);
            cheatsWindow = BuildCheats(cgo.transform);

            // Fade (on top of everything)
            var f = Rect("Fade", cgo.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            f.anchorMin = Vector2.zero; f.anchorMax = Vector2.one; f.offsetMin = f.offsetMax = Vector2.zero;
            fade = f.gameObject.AddComponent<Image>();
            fade.raycastTarget = false;
        }

        GameObject BuildSettings(Transform parent)
        {
            var w = Window(parent, "Settings", out var body);
            SliderRow(body, "Mouse sensitivity", 0.1f, 3f, sensitivity, v => { sensitivity = v; PlayerPrefs.SetFloat("gdd110.sensitivity", v); });
            SliderRow(body, "Master volume", 0f, 1f, master, v => { master = v; PlayerPrefs.SetFloat("gdd110.master", v); ApplyAudio(); });
            SliderRow(body, "Music volume", 0f, 1f, music, v => { music = v; PlayerPrefs.SetFloat("gdd110.music", v); ApplyAudio(); });
            SliderRow(body, "Effects volume", 0f, 1f, effects, v => { effects = v; PlayerPrefs.SetFloat("gdd110.effects", v); ApplyAudio(); });
            var r1 = Row(body);
            Button(r1, "Save position  (0)", () => { QuickSave(); });
            Button(r1, "Load position  (8)", () => { QuickLoad(); CloseWindow(); });
            var r2 = Row(body);
            Button(r2, "Restart level  (7)", () => { CloseWindow(); GddScenes.Restart(); });
            Button(r2, "Game library  (/)", () => { CloseWindow(); OpenLibrary(); });
            var hint = Rect("Hint", body, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 60));
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            Label(hint, "Hold <b><noparse><</noparse></b> or <b><noparse>></noparse></b> to change level.   <b>`</b> or <b>Esc</b> closes.", 20, FontStyles.Normal, new Color(0.7f, 0.75f, 0.85f), TextAlignmentOptions.Center);
            var r3 = Row(body);
            Button(r3, "Show / hide next step", GddFourthBell.ToggleNextStep);
            Button(r3, "Close", CloseWindow);
            w.SetActive(false);
            return w;
        }

        GameObject BuildCheats(Transform parent)
        {
            var w = Window(parent, "Developer", out var body);
            ToggleRow(body, "Fly / no-clip  (WASD, Space up, Ctrl down, Shift fast)", false, v => { Fly = v; });
            ToggleRow(body, "Unkillable", false, v => { Unkillable = v; });
            ToggleRow(body, "Caps Lock toggles 0.2x slow motion", false, v => { SlowMoAllowed = v; });
            var r = Row(body);
            r.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var lbl = Rect("Jumps", r, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 56));
            lbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 300;
            jumpsLabel = Label(lbl, "Jumps: 1", 26, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            Button(r, "-", () => { Jumps = Mathf.Max(1, Jumps - 1); jumpsLabel.text = "Jumps: " + Jumps; }, 80);
            Button(r, "+", () => { Jumps = Mathf.Min(9, Jumps + 1); jumpsLabel.text = "Jumps: " + Jumps; }, 80);
            var r2 = Row(body);
            Button(r2, "Close", CloseWindow);
            w.SetActive(false);
            return w;
        }

        GameObject Window(Transform parent, string title, out RectTransform body)
        {
            var win = Panel(title + " Window", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 10), Glass);
            var v = win.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 32, 32);
            v.spacing = 14;
            v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            win.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = Rect("Title", win, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 60));
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            Label(t, title, 40, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            body = win;
            return win.gameObject;
        }

        RectTransform Row(RectTransform parent)
        {
            var r = Rect("Row", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 56));
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            r.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            return r;
        }

        void SliderRow(RectTransform parent, string label, float min, float max, float value, Action<float> onChange)
        {
            var r = Row(parent);
            var l = Rect("Label", r, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 56));
            l.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;
            Label(l, label, 24, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft);
            var res = new DefaultControls.Resources { standard = Settings.panelSprite, background = Settings.panelSprite, knob = Settings.knobSprite };
            var sgo = DefaultControls.CreateSlider(res);
            sgo.transform.SetParent(r, false);
            sgo.AddComponent<LayoutElement>().preferredWidth = 360;
            var s = sgo.GetComponent<Slider>();
            s.minValue = min; s.maxValue = max; s.value = value;
            foreach (var img in sgo.GetComponentsInChildren<Image>())
            {
                if (img.name == "Background") img.color = new Color(1, 1, 1, 0.15f);
                else if (img.name == "Fill") img.color = Accent;
                else if (img.name == "Handle") img.color = Color.white;
            }
            s.onValueChanged.AddListener(v => onChange(v));
        }

        void ToggleRow(RectTransform parent, string label, bool value, Action<bool> onChange)
        {
            var r = Row(parent);
            r.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var box = Rect("Box", r, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
            var le = box.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 44; le.flexibleWidth = 0;
            var bg = box.gameObject.AddComponent<Image>();
            bg.sprite = Settings.panelSprite; bg.type = Image.Type.Sliced; bg.color = new Color(1, 1, 1, 0.15f);
            var check = Rect("Check", box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));
            var ci = check.gameObject.AddComponent<Image>();
            ci.sprite = Settings.checkSprite; ci.color = Accent;
            var toggle = box.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bg; toggle.graphic = ci; toggle.isOn = value;
            toggle.onValueChanged.AddListener(v => onChange(v));
            var l = Rect("Label", r, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 56));
            l.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Label(l, label, 24, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft);
        }

        void Button(RectTransform parent, string text, Action onClick, float width = 0)
        {
            var b = Rect("Button " + text, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 56));
            var img = b.gameObject.AddComponent<Image>();
            img.sprite = Settings.panelSprite; img.type = Image.Type.Sliced; img.color = new Color(1, 1, 1, 0.12f);
            var btn = b.gameObject.AddComponent<UnityEngine.UI.Button>();
            var colors = btn.colors; colors.highlightedColor = new Color(0.6f, 0.85f, 1f, 1f); colors.pressedColor = Accent; btn.colors = colors;
            btn.onClick.AddListener(() => onClick());
            if (width > 0) { var le = b.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = width; le.flexibleWidth = 0; }
            Label(b, text, 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        }

        RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var r = Rect(name, parent, anchor, pos, size);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = Settings.panelSprite; img.type = Image.Type.Sliced; img.color = color;
            return r;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        static TextMeshProUGUI Label(RectTransform parent, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            GddHudFonts.Apply(t, GddHudFonts.Body);
            t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }
    }
}
