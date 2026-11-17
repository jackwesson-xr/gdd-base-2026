using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GDD110
{
    /// <summary>
    /// Switches the view to another Cinemachine camera while On, then back to the player camera when Off.
    /// With Security Feed ticked, the view gets a grainy black-and-white monitor look with a REC label.
    /// Typical use: a trigger zone in a small room linked with TurnOn on entry and TurnOff on exit.
    /// </summary>
    [AddComponentMenu("GDD 110/Triggerables/Camera Switch")]
    public class GddCameraSwitch : GddTriggerable
    {
        [Tooltip("The camera to look through while On. Leave it disabled in the scene.")]
        public CinemachineVirtualCameraBase cameraToUse;
        [Tooltip("Cut straight to the camera instead of blending.")]
        public bool cut = true;
        [Header("Security feed")]
        public bool securityFeed = true;
        public string feedLabel = "CAM 02";

        CinemachineBrain brain;
        CinemachineBlendDefinition savedBlend;
        GameObject overlay;
        Volume look;
        RawImage noise;
        Texture2D noiseTex;
        TextMeshProUGUI rec, clock;
        float nextNoise;

        protected override void ApplyState(bool on, bool instant)
        {
            if (cameraToUse == null) return;
            if (Application.isPlaying) StarterAssets.ThirdPersonController.NotifyCameraCut();
            if (brain == null) { var main = Camera.main; if (main != null) brain = main.GetComponent<CinemachineBrain>(); }
            if (on)
            {
                if (brain != null && cut) { savedBlend = brain.DefaultBlend; brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0); }
                cameraToUse.Priority = 1000;
                cameraToUse.gameObject.SetActive(true);
                cameraToUse.Prioritize();
                if (securityFeed) ShowFeed(true);
            }
            else
            {
                cameraToUse.gameObject.SetActive(false);
                if (brain != null && cut && !instant) StartCoroutine(RestoreBlend());
                ShowFeed(false);
            }
        }

        System.Collections.IEnumerator RestoreBlend()
        {
            yield return null;
            yield return null;
            if (brain != null) brain.DefaultBlend = savedBlend;
        }

        void ShowFeed(bool show)
        {
            if (show && overlay == null) BuildFeed();
            if (overlay != null) overlay.SetActive(show);
            if (look != null) look.weight = show ? 1 : 0;
        }

        void BuildFeed()
        {
            overlay = new GameObject("Security Feed", typeof(RectTransform));
            var canvas = overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;

            noiseTex = new Texture2D(160, 90, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            noise = Stretch<RawImage>("Static");
            noise.texture = noiseTex;
            noise.raycastTarget = false;

            var lines = Stretch<RawImage>("Scanlines");
            var lineTex = new Texture2D(1, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            lineTex.SetPixels(new[] { new Color(0, 0, 0, 0.35f), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.15f) });
            lineTex.Apply();
            lines.texture = lineTex;
            lines.uvRect = new Rect(0, 0, 1, Screen.height / 3f);
            lines.raycastTarget = false;

            rec = Label("<b>REC</b>  " + feedLabel, new Vector2(0, 1), new Vector2(40, -36), TextAlignmentOptions.TopLeft, new Color(1f, 0.25f, 0.2f));
            clock = Label("", new Vector2(1, 0), new Vector2(-40, 30), TextAlignmentOptions.BottomRight, new Color(0.85f, 1f, 0.85f));

            var vgo = new GameObject("Security Look");
            vgo.transform.SetParent(overlay.transform, false);
            look = vgo.AddComponent<Volume>();
            look.isGlobal = true;
            look.priority = 200;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var col = profile.Add<ColorAdjustments>(true);
            col.saturation.Override(-85f); col.contrast.Override(35f); col.colorFilter.Override(new Color(0.8f, 1f, 0.85f));
            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.Override(0.9f); grain.type.Override(FilmGrainLookup.Large02);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.5f); vig.smoothness.Override(0.5f);
            var lens = profile.Add<LensDistortion>(true);
            lens.intensity.Override(0.35f);
            look.profile = profile;
        }

        T Stretch<T>(string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(overlay.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go.AddComponent<T>();
        }

        TextMeshProUGUI Label(string text, Vector2 anchor, Vector2 offset, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(overlay.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(600, 60);
            var t = go.AddComponent<TextMeshProUGUI>();
            GddHudFonts.Apply(t, GddHudFonts.Body);
            t.text = text; t.fontSize = 34; t.alignment = align; t.color = color; t.fontStyle = FontStyles.Bold;
            return t;
        }

        void Update()
        {
            if (overlay == null || !overlay.activeSelf) return;
            if (Time.unscaledTime >= nextNoise)
            {
                nextNoise = Time.unscaledTime + 0.045f;
                var px = noiseTex.GetPixels32();
                for (int i = 0; i < px.Length; i++)
                {
                    byte v = (byte)Random.Range(0, 255);
                    px[i] = new Color32(v, v, v, (byte)Random.Range(10, 46));
                }
                noiseTex.SetPixels32(px);
                noiseTex.Apply(false);
                noise.uvRect = new Rect(Random.value, Random.value, 1, 1);
            }
            rec.alpha = Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f ? 1f : 0.25f;
            clock.text = System.DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
        }

        void OnDestroy()
        {
            if (overlay != null) Destroy(overlay);
            if (noiseTex != null) Destroy(noiseTex);
        }
    }
}
