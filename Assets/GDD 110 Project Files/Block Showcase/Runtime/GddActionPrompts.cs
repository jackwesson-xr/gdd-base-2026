using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GDD110.Juice
{
    /// <summary>
    /// Bottom-of-screen button prompts that show only what you can do right now:
    /// [E] Pick up Blaster, [Click] Fire, [Q] Drop Blaster, a countdown for dropped items heading home.
    /// Keys switch to controller names when a gamepad is connected. Prompts spring in and out.
    /// Replaces the toolkit's top-left help text while active.
    /// </summary>
    [AddComponentMenu("")]
    public class GddActionPrompts : MonoBehaviour
    {
        class Chip
        {
            public string id;
            public RectTransform root;
            public CanvasGroup group;
            public TextMeshProUGUI key, label;
            public GameObject keyCap;
            public float open, vel;
            public bool wanted;
            public string lastText;
        }

        struct Want { public string id, key, label; public Color tint; }

        [SerializeField, HideInInspector] GddInteractor interactor;
        [SerializeField, HideInInspector] Sprite panel;
        RectTransform row;
        readonly Dictionary<string, Chip> chips = new Dictionary<string, Chip>();
        readonly List<Want> wants = new List<Want>();
        GddPickupReturn[] returners;
        float nextScan;
        float startTime;

        public void Init(GddJuiceDirector owner, GddInteractor who)
        {
            interactor = who;
            panel = owner.Settings.panelSprite;
            Rebuild();
        }

        void OnEnable()
        {
            if (interactor) Rebuild();
        }

        void Rebuild()
        {
            foreach (var other in FindObjectsByType<GddActionPrompts>())
            {
                if (other == this || other.gameObject.scene != gameObject.scene) continue;
                other.gameObject.SetActive(false);
                Destroy(other.gameObject);
            }
            foreach (Transform child in transform)
            {
                if (child.name != "Action Prompts") continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            chips.Clear();
            startTime = Time.unscaledTime;
            if (interactor.promptText != null) interactor.promptText.gameObject.SetActive(false);

            var cgo = new GameObject("Action Prompts", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var rgo = new GameObject("Row", typeof(RectTransform));
            rgo.transform.SetParent(cgo.transform, false);
            row = (RectTransform)rgo.transform;
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0, 54);
            var h = rgo.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 18;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            var fit = rgo.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        Chip Make(string id)
        {
            var c = new Chip { id = id };
            var go = new GameObject("Prompt " + id, typeof(RectTransform));
            go.transform.SetParent(row, false);
            c.root = (RectTransform)go.transform;
            c.group = go.AddComponent<CanvasGroup>();
            var bg = go.AddComponent<Image>();
            bg.sprite = panel; bg.type = Image.Type.Sliced;
            bg.color = new Color(.045f,.075f,.061f,.96f);
            GddHudFonts.AddPanelShadow(bg);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 22, 10, 10);
            h.spacing = 14;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            c.keyCap = new GameObject("Key", typeof(RectTransform));
            c.keyCap.transform.SetParent(go.transform, false);
            var capImage = c.keyCap.AddComponent<Image>();
            capImage.sprite = panel; capImage.type = Image.Type.Sliced;
            capImage.color = new Color(0.96f, 0.97f, 1f);
            var capLayout = c.keyCap.AddComponent<HorizontalLayoutGroup>();
            capLayout.padding = new RectOffset(14, 14, 6, 8);
            capLayout.childAlignment = TextAnchor.MiddleCenter;
            capLayout.childControlWidth = true; capLayout.childControlHeight = true;
            var capSize = c.keyCap.AddComponent<LayoutElement>();
            capSize.minWidth = 46; capSize.minHeight = 46;
            c.key = Text(c.keyCap.transform, 26, new Color(0.08f, 0.09f, 0.12f), FontStyles.Bold);

            c.label = Text(go.transform, 24, Color.white, FontStyles.Bold);
            c.label.overflowMode = TextOverflowModes.Truncate;
            c.label.enableAutoSizing = true;
            c.label.fontSizeMin = 16;
            c.label.fontSizeMax = 24;
            var labelLayout = c.label.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 0;
            labelLayout.minWidth = 0;
            labelLayout.preferredHeight = 46;
            c.root.localScale = Vector3.zero;
            chips[id] = c;
            return c;
        }

        static TextMeshProUGUI Text(Transform parent, float size, Color color, FontStyles style)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.color = color; t.fontStyle = style;
            GddHudFonts.Apply(t, GddHudFonts.Body);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.alignment = TextAlignmentOptions.Center;
            return t;
        }

        void Gather()
        {
            wants.Clear();
            bool pad = GddControls.HasGamepad;
            var candidate = interactor.Candidate;
            var held = interactor.Equipped;
            if (candidate != null)
                wants.Add(new Want { id = "interact", key = pad ? "X" : "E", label = candidate.Prompt + " " + candidate.DisplayName });
            if (held != null)
            {
                string use = null;
                foreach (var power in held.GetComponentsInChildren<GddPower>(true))
                {
                    if (power.activation != GddPower.Activation.UseButton) continue;
                    var kind = power.definition != null ? power.definition.kind : GddPowerKind.EventsOnly;
                    use = kind == GddPowerKind.Projectile ? "Fire" : kind == GddPowerKind.Push ? "Push" : kind == GddPowerKind.Heal ? "Heal" : "Use";
                }
                if (use == "Fire") wants.Add(new Want { id = "aim", key = pad ? "LT" : "RMB", label = "Aim" });
                if (held.GetComponentInChildren<GddSlashAttack>())
                    wants.Add(new Want { id = "slash", key = pad ? "RT" : "LMB", label = "Slash" });
                if (held.GetComponentInChildren<GddPortalGun>()) use = "Place portal";
                if (use != null) wants.Add(new Want { id = "use", key = pad ? "RT" : "LMB", label = use });
                wants.Add(new Want { id = "drop", key = pad ? "B" : "Q", label = "Drop " + held.DisplayName });
            }

            if (!held && GddCollectible.AnyWorn(interactor.GetComponent<GddActor>()))
                wants.Add(new Want { id = "remove", key = pad ? "B" : "Q", label = "Remove gear" });

            if (Time.unscaledTime >= nextScan) { returners = FindObjectsByType<GddPickupReturn>(); nextScan = Time.unscaledTime + 1f; }
            if (returners != null)
                foreach (var r in returners)
                    if (r != null && r.SecondsLeft > 0)
                    {
                        wants.Add(new Want { id = "return", key = null, label = r.GetComponent<GddPickup>().DisplayName + " returns in " + Mathf.CeilToInt(r.SecondsLeft) });
                        break;
                    }

            if (wants.Count == 0 && Time.unscaledTime - startTime < 10f)
            {
                wants.Add(new Want { id = "move", key = pad ? "L" : "WASD", label = "Move" });
                wants.Add(new Want { id = "jump", key = pad ? "A" : "Space", label = "Jump" });
            }
        }

        void Update()
        {
            if (interactor == null || row == null) return;
            Gather();
            foreach (var c in chips.Values) c.wanted = false;
            int order = 0;
            foreach (var w in wants)
            {
                if (!chips.TryGetValue(w.id, out var c)) c = Make(w.id);
                c.wanted = true;
                c.root.SetSiblingIndex(order++);
                c.keyCap.SetActive(!string.IsNullOrEmpty(w.key));
                if (c.key.text != w.key) c.key.text = w.key;
                if (c.lastText != w.label)
                {
                    if (c.lastText != null && c.open > 0.5f) c.vel += 6f;  // text changed: little bump
                    c.label.text = w.label;
                    c.label.GetComponent<LayoutElement>().preferredWidth = Mathf.Min(315, c.label.GetPreferredValues(w.label).x + 12);
                    c.lastText = w.label;
                }
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            foreach (var c in chips.Values)
            {
                float goal = c.wanted ? 1f : 0f;
                c.vel += (-(c.wanted ? 260f : 180f) * (c.open - goal) - 17f * c.vel) * dt;
                c.open += c.vel * dt;
                if (!c.wanted && c.open < 0.01f && Mathf.Abs(c.vel) < 0.1f) { c.open = 0; c.vel = 0; }
                bool show = c.open > 0.005f || c.wanted;
                if (c.root.gameObject.activeSelf != show) c.root.gameObject.SetActive(show);
                c.root.localScale = Vector3.one * Mathf.Max(0f, c.open);
                c.group.alpha = Mathf.Clamp01(c.open * 1.4f);
            }
        }
    }
}
