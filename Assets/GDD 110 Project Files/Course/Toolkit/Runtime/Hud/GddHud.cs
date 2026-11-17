using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GDD110
{
    /// <summary>
    /// The level's heads-up display, built on demand: health in the top left, key items under it,
    /// pickup counters ("icon x 12") down the top right. Collectibles and the health bar register themselves;
    /// nothing has to be placed by hand.
    /// </summary>
    [AddComponentMenu("")]
    public class GddHud : MonoBehaviour
    {
        static GddHud instance;
        public RectTransform HealthArea { get; private set; }
        public RectTransform KeyRow { get; private set; }
        public RectTransform CounterColumn { get; private set; }

        class Counter
        {
            public RectTransform root;
            public Image icon;
            public TextMeshProUGUI label;
            public float punch;
        }

        class KeySlot
        {
            public string id;
            public RectTransform root;
            public GameObject model;   // flattened 3D icon, if used
        }

        readonly Dictionary<string, Counter> counters = new Dictionary<string, Counter>();
        readonly List<KeySlot> keySlots = new List<KeySlot>();

        public static GddHud Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("[GDD HUD]");
            instance = go.AddComponent<GddHud>();
            instance.Build();
            return instance;
        }

        void OnEnable()
        {
            GddInventory.CountChanged += OnCount;
            GddInventory.KeyChanged += OnKey;
        }

        void OnDisable()
        {
            GddInventory.CountChanged -= OnCount;
            GddInventory.KeyChanged -= OnKey;
            if (instance == this) instance = null;
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            HealthArea = Area("Health", new Vector2(0, 1), new Vector2(48, -44));
            bool caseHud = FindAnyObjectByType<GddFourthBell>() != null;
            KeyRow = Area("Key Items", caseHud ? new Vector2(1, 1) : new Vector2(0, 1),
                caseHud ? new Vector2(-48, -197) : new Vector2(48, -140));
            var h = KeyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14; h.childAlignment = caseHud ? TextAnchor.UpperRight : TextAnchor.UpperLeft; h.childControlWidth = false; h.childControlHeight = false;
            CounterColumn = Area("Counters", new Vector2(1, 1), new Vector2(-48, caseHud ? -119 : -44));
            CounterColumn.pivot = new Vector2(1, 1);
            var v = CounterColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10; v.childAlignment = TextAnchor.UpperRight; v.childControlWidth = false; v.childControlHeight = false;
        }

        RectTransform Area(string name, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = new Vector2(anchor.x, 1);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(600, 90);
            return r;
        }

        // ---------- score ----------

        TextMeshProUGUI scoreLabel;
        float scorePunch;
        GddActor scored;

        /// <summary>Shows the player's score at the top middle of the screen and keeps it up to date.</summary>
        public void ShowScore(GddActor actor)
        {
            if (actor == null || scored == actor) return;
            if (scored != null) scored.onScoreChanged.RemoveListener(OnScore);
            scored = actor;
            if (scoreLabel == null)
            {
                var go = new GameObject("Score", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var r = (RectTransform)go.transform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1);
                r.pivot = new Vector2(0.5f, 1);
                r.anchoredPosition = new Vector2(0, -30);
                if (FindAnyObjectByType<GddFourthBell>() != null)
                {
                    r.anchorMin = r.anchorMax = r.pivot = new Vector2(1, 1);
                    r.anchoredPosition = new Vector2(-50, -28);
                    r.sizeDelta = new Vector2(260, 72);
                }
                if (r.pivot.x != 1) r.sizeDelta = new Vector2(500, 80);
                if (r.pivot.x == 1)
                {
                    var plate = new GameObject("Score backing", typeof(RectTransform), typeof(Image));
                    var backing = (RectTransform)plate.transform;
                    backing.SetParent(transform, false);
                    backing.SetSiblingIndex(r.GetSiblingIndex());
                    backing.anchorMin = r.anchorMin;
                    backing.anchorMax = r.anchorMax;
                    backing.pivot = r.pivot;
                    backing.anchoredPosition = r.anchoredPosition + new Vector2(14, 5);
                    backing.sizeDelta = new Vector2(260,72);
                    var image = plate.GetComponent<Image>();
                    image.color = new Color(.035f,.06f,.049f,.92f);
                    image.raycastTarget = false;
                    GddHudFonts.AddPanelShadow(image);
                }
                scoreLabel = go.AddComponent<TextMeshProUGUI>();
                scoreLabel.fontSize = 44; scoreLabel.textWrappingMode = TextWrappingModes.NoWrap; scoreLabel.fontStyle = FontStyles.Bold;
                GddHudFonts.Apply(scoreLabel, GddHudFonts.Body);
                scoreLabel.alignment = r.pivot.x == 1 ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
                scoreLabel.outlineWidth = 0.22f; scoreLabel.outlineColor = new Color32(0, 0, 0, 180);
                scoreLabel.raycastTarget = false;
            }
            actor.onScoreChanged.AddListener(OnScore);
            OnScore(actor.Score);
            scorePunch = 0;
        }

        void OnScore(int score)
        {
            if (scoreLabel == null) return;
            scoreLabel.text = "<size=65%>Score</size>  " + score;
            scorePunch = 1f;
        }

        void TickScore()
        {
            if (scoreLabel != null)
            {
                scorePunch = Mathf.MoveTowards(scorePunch, 0, Time.unscaledDeltaTime * 3f);
                scoreLabel.rectTransform.localScale = Vector3.one * (1f + 0.25f * scorePunch * scorePunch);
            }
        }

        // ---------- counters ----------

        /// <summary>Make sure a counter exists (it shows greyed out at zero). Offset, rotation and scale nudge the element.</summary>
        public RectTransform EnsureCounter(string id, Sprite icon, Vector2 offset, float rotation, Vector3 scale)
        {
            if (counters.TryGetValue(id, out var existing)) return existing.root;
            var c = new Counter();
            var go = new GameObject("Counter - " + id, typeof(RectTransform));
            go.transform.SetParent(CounterColumn, false);
            c.root = (RectTransform)go.transform;
            c.root.sizeDelta = new Vector2(230, 64);
            var inner = new GameObject("Content", typeof(RectTransform));
            inner.transform.SetParent(c.root, false);
            var ir = (RectTransform)inner.transform;
            ir.anchorMin = ir.anchorMax = new Vector2(1, 0.5f);
            ir.pivot = new Vector2(1, 0.5f);
            ir.sizeDelta = new Vector2(230, 64);
            ir.anchoredPosition = offset;
            ir.localEulerAngles = new Vector3(0, 0, rotation);
            ir.localScale = scale == Vector3.zero ? Vector3.one : scale;
            var counterPlate = ir.gameObject.AddComponent<Image>();
            counterPlate.color = new Color(.035f,.06f,.049f,.88f);
            counterPlate.raycastTarget = false;
            GddHudFonts.AddPanelShadow(counterPlate);

            var ig = new GameObject("Icon", typeof(RectTransform));
            ig.transform.SetParent(ir, false);
            var icr = (RectTransform)ig.transform;
            icr.anchorMin = icr.anchorMax = new Vector2(0, 0.5f);
            icr.pivot = new Vector2(0, 0.5f);
            icr.sizeDelta = new Vector2(60, 60);
            c.icon = ig.AddComponent<Image>();
            c.icon.sprite = icon;
            c.icon.enabled = icon != null;
            c.icon.preserveAspect = true;
            c.icon.raycastTarget = false;

            var lg = new GameObject("Count", typeof(RectTransform));
            lg.transform.SetParent(ir, false);
            var lr = (RectTransform)lg.transform;
            lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(1, 1);
            lr.offsetMin = new Vector2(icon ? 70 : 12, 0); lr.offsetMax = Vector2.zero;
            c.label = lg.AddComponent<TextMeshProUGUI>();
            c.label.fontSize = 34; c.label.fontStyle = FontStyles.Bold;
            c.label.enableAutoSizing = true;
            c.label.fontSizeMin = 20;
            c.label.fontSizeMax = 34;
            c.label.textWrappingMode = TextWrappingModes.NoWrap;
            GddHudFonts.Apply(c.label, GddHudFonts.Body);
            c.label.alignment = TextAlignmentOptions.MidlineLeft;
            c.label.outlineWidth = 0.28f; c.label.outlineColor = new Color32(0, 0, 0, 170);
            c.label.raycastTarget = false;
            counters[id] = c;
            Refresh(id, GddInventory.Count(id));
            return c.root;
        }

        public RectTransform CounterTarget(string id) => counters.TryGetValue(id, out var c) ? (RectTransform)c.icon.transform : CounterColumn;

        void OnCount(string id, int count)
        {
            if (!counters.TryGetValue(id, out var c)) return;
            c.punch = 1f;
            Refresh(id, count);
        }

        void Refresh(string id, int count)
        {
            var c = counters[id];
            bool any = count > 0;
            c.label.text = id.ToUpperInvariant() + "  " + count;
            c.icon.color = any ? Color.white : new Color(0.55f, 0.57f, 0.62f, 0.45f);
            c.label.color = any ? Color.white : new Color(1, 1, 1, 0.45f);
        }

        // ---------- key items ----------

        /// <summary>Reserve a slot for a key item. Returns the RectTransform the item flies to.</summary>
        public RectTransform AddKeySlot(string id, Sprite icon, Vector2 offset, float rotation, Vector3 scale)
        {
            var go = new GameObject("Key - " + id, typeof(RectTransform));
            go.transform.SetParent(KeyRow, false);
            var r = (RectTransform)go.transform;
            r.sizeDelta = new Vector2(76, 76);
            var inner = new GameObject("Icon", typeof(RectTransform));
            inner.transform.SetParent(r, false);
            var ir = (RectTransform)inner.transform;
            ir.sizeDelta = new Vector2(76, 76);
            ir.anchoredPosition = offset;
            ir.localEulerAngles = new Vector3(0, 0, rotation);
            ir.localScale = scale == Vector3.zero ? Vector3.one : scale;
            if (icon != null)
            {
                var img = inner.AddComponent<Image>();
                img.sprite = icon; img.preserveAspect = true; img.raycastTarget = false;
                img.color = new Color(1, 1, 1, 0);   // fades in when the item lands
            }
            keySlots.Add(new KeySlot { id = id, root = r });
            return ir;
        }

        public void ShowKey(RectTransform slot, GameObject flattenedModel)
        {
            var img = slot.GetComponent<Image>();
            if (img != null) StartCoroutine(FadeIn(img));
            foreach (var k in keySlots) if (k.root == slot.parent) k.model = flattenedModel;
            StartCoroutine(Punch(slot));
        }

        IEnumerator FadeIn(Image img)
        {
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.2f) { img.color = new Color(1, 1, 1, t); yield return null; }
            img.color = Color.white;
        }

        void OnKey(string id, bool added)
        {
            if (added) return;
            var slot = keySlots.Find(k => k.id == id);
            if (slot == null) return;
            keySlots.Remove(slot);
            StartCoroutine(RemoveSlot(slot));
        }

        IEnumerator RemoveSlot(KeySlot slot)
        {
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.3f)
            {
                float s = 1f + t * 0.6f;
                slot.root.localScale = Vector3.one * s;
                foreach (var g in slot.root.GetComponentsInChildren<Graphic>()) { var c = g.color; c.a = 1 - t; g.color = c; }
                if (slot.model != null) slot.model.transform.localScale *= 0.85f;
                yield return null;
            }
            if (slot.model != null) Destroy(slot.model);
            Destroy(slot.root.gameObject);
        }

        IEnumerator Punch(RectTransform r)
        {
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.35f)
            {
                r.localScale = Vector3.one * (1f + 0.45f * Mathf.Sin(t * Mathf.PI) * (1 - t));
                yield return null;
            }
            r.localScale = Vector3.one;
        }

        // ---------- dialog subtitle (when the speaker is off screen) ----------

        RectTransform subtitle;
        CanvasGroup subtitleGroup;
        TextMeshProUGUI subtitleText, subtitleName;
        Image subtitleTag;
        object subtitleOwner;
        int subtitleFrame = -10;
        float subtitleAlpha;

        /// <summary>
        /// Show a dialog line along the bottom of the screen. Call every frame while it should show;
        /// it fades out by itself when calls stop. Used by dialog bubbles whose speaker is off screen.
        /// </summary>
        public void Subtitle(object owner, string speaker, string text, int visibleCharacters, float alpha, bool thought)
        {
            if (subtitle == null) BuildSubtitle();
            if (subtitleOwner != owner && Time.frameCount == subtitleFrame && alpha < subtitleAlpha) return;
            subtitleOwner = owner;
            subtitleFrame = Time.frameCount;
            subtitleAlpha = alpha;
            if (subtitleText.text != text) subtitleText.text = text;
            subtitleText.maxVisibleCharacters = visibleCharacters;
            subtitleText.fontStyle = thought ? FontStyles.Bold | FontStyles.Italic : FontStyles.Bold;
            subtitleName.text = thought ? speaker + " (thinking)" : speaker;
            subtitleGroup.alpha = alpha;
            subtitle.anchoredPosition = new Vector2(0, 150 - 30f * (1f - alpha));
        }

        void BuildSubtitle()
        {
            var s = GddGameSettings.Load();
            Sprite panel = s != null ? s.panelSprite : null;
            var go = new GameObject("Dialog Subtitle", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            subtitle = (RectTransform)go.transform;
            subtitle.anchorMin = subtitle.anchorMax = new Vector2(0.5f, 0);
            subtitle.pivot = new Vector2(0.5f, 0);
            subtitle.sizeDelta = new Vector2(1100, 130);
            subtitleGroup = go.AddComponent<CanvasGroup>();
            subtitleGroup.blocksRaycasts = false;
            var outline = go.AddComponent<Image>();
            outline.sprite = panel; outline.type = Image.Type.Sliced; outline.color = Color.black; outline.pixelsPerUnitMultiplier = 0.35f;
            var fill = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            fill.SetParent(subtitle, false);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = new Vector2(6, 6); fill.offsetMax = new Vector2(-6, -6);
            var fi = fill.gameObject.AddComponent<Image>();
            fi.sprite = panel; fi.type = Image.Type.Sliced; fi.color = Color.white; fi.pixelsPerUnitMultiplier = 0.35f;
            var tag = new GameObject("Speaker", typeof(RectTransform)).GetComponent<RectTransform>();
            tag.SetParent(subtitle, false);
            tag.anchorMin = tag.anchorMax = new Vector2(0, 1); tag.pivot = new Vector2(0, 0.5f);
            tag.anchoredPosition = new Vector2(28, 0); tag.sizeDelta = new Vector2(260, 44);
            subtitleTag = tag.gameObject.AddComponent<Image>();
            subtitleTag.sprite = panel; subtitleTag.type = Image.Type.Sliced; subtitleTag.color = Color.black; subtitleTag.pixelsPerUnitMultiplier = 0.35f;
            subtitleName = Text(tag, 24, Color.white, TextAlignmentOptions.Center);
            var body = new GameObject("Line", typeof(RectTransform)).GetComponent<RectTransform>();
            body.SetParent(subtitle, false);
            body.anchorMin = Vector2.zero; body.anchorMax = Vector2.one; body.offsetMin = new Vector2(40, 14); body.offsetMax = new Vector2(-40, -26);
            subtitleText = Text(body, 32, new Color(0.08f, 0.08f, 0.1f), TextAlignmentOptions.MidlineLeft);
            subtitleGroup.alpha = 0;
        }

        static TextMeshProUGUI Text(RectTransform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var t = go.AddComponent<TextMeshProUGUI>();
            GddHudFonts.Apply(t, GddHudFonts.Body);
            t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = FontStyles.Bold;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false;
            return t;
        }

        void Update()
        {
            TickScore();
            if (subtitle != null && Time.frameCount > subtitleFrame + 1 && subtitleGroup.alpha > 0f)
            {
                subtitleAlpha = Mathf.MoveTowards(subtitleGroup.alpha, 0f, Time.unscaledDeltaTime * 5f);
                subtitleGroup.alpha = subtitleAlpha;
            }
            foreach (var c in counters.Values)
            {
                if (c.punch <= 0) continue;
                c.punch = Mathf.Max(0, c.punch - Time.unscaledDeltaTime * 3f);
                float s = 1f + 0.4f * Mathf.Sin(c.punch * Mathf.PI) * c.punch;
                c.icon.rectTransform.localScale = Vector3.one * s;
            }
        }

        /// <summary>World point just in front of the camera at a HUD element, for flying 3D items to it.</summary>
        public static Vector3 WorldAt(RectTransform target, float depth)
        {
            var cam = Camera.main;
            if (cam == null || target == null) return Vector3.zero;
            Vector3 screen = target.position;   // overlay canvas: position is in screen pixels
            return cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        }
    }
}
