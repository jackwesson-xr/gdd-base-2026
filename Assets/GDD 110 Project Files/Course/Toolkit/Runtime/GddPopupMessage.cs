using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GDD110
{
    /// <summary>
    /// A digital message card: a dark glass panel with a colored stripe, a title and a message.
    /// Shows when the player walks close, or when it is switched On (by a Link, a trigger zone, any UnityEvent).
    /// Float it above the object in the world, or show it as a banner at the top of the screen.
    /// </summary>
    [AddComponentMenu("GDD 110/Triggerables/Popup Message")]
    public class GddPopupMessage : GddTriggerable
    {
        public enum ShowWhen { PlayerIsNear, SwitchedOn }
        public enum Display { FloatingCard, ScreenBanner }

        public string title = "Hello";
        [TextArea(2, 5)] public string message = "Walk close to read me.";
        public ShowWhen showWhen = ShowWhen.PlayerIsNear;
        [Min(0.5f)] public float nearDistance = 3.5f;
        public Display display = Display.FloatingCard;
        public Color accent = new Color(0.25f, 0.75f, 1f);
        [Tooltip("Optional picture on the left of the card.")]
        public Sprite icon;
        [Tooltip("Hide by itself after this many seconds. 0 = stay up while near / switched on.")]
        [Min(0)] public float hideAfterSeconds;
        [Tooltip("Floating card: height above this object (meters).")]
        public float height = 2.2f;

        RectTransform card;
        CanvasGroup group;
        TextMeshProUGUI titleText, bodyText;
        bool wanted;
        float open, vel, hideAt;

        protected override void ApplyState(bool on, bool instant) { if (showWhen == ShowWhen.SwitchedOn) Set(on); }

        /// <summary>Show it now (from any UnityEvent).</summary>
        public void Show() => Set(true);
        /// <summary>Hide it now.</summary>
        public void Hide() => Set(false);
        /// <summary>Change the message text and show it.</summary>
        public void ShowMessage(string text) { message = text; if (bodyText != null) bodyText.text = text; Set(true); }

        void Set(bool show)
        {
            if (show && card == null) Build();
            if (show && !wanted && bodyText != null) { titleText.text = title; bodyText.text = message; }
            wanted = show;
            hideAt = show && hideAfterSeconds > 0 ? Time.time + hideAfterSeconds : 0f;
        }

        void Update()
        {
            if (showWhen == ShowWhen.PlayerIsNear)
            {
                var actor = FindAnyObjectByType<GddActor>();
                bool near = actor != null && Vector3.Distance(actor.transform.position, transform.position) < nearDistance;
                if (near && !wanted && hideAt == 0f) Set(true);
                else if (!near && wanted) { Set(false); hideAt = 0f; }
                else if (!near) hideAt = 0f;
            }
            if (wanted && hideAt > 0f && Time.time >= hideAt) { wanted = false; if (showWhen == ShowWhen.PlayerIsNear) hideAt = -1f; }
            if (card == null) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            vel += (-200f * (open - (wanted ? 1f : 0f)) - 18f * vel) * dt;
            open = Mathf.Max(0f, open + vel * dt);
            group.alpha = Mathf.Clamp01(open * 1.4f);
            card.gameObject.SetActive(open > 0.01f || wanted);

            if (display == Display.FloatingCard)
            {
                var cam = Camera.main;
                var root = card.parent;
                root.position = transform.position + Vector3.up * height;
                if (cam != null) root.rotation = Quaternion.LookRotation(root.position - cam.transform.position, Vector3.up);
                card.localScale = new Vector3(1f, Mathf.Max(0.001f, open), 1f);
            }
            else
            {
                card.anchoredPosition = new Vector2(0, -150 + 40f * (1f - Mathf.Clamp01(open)));
                card.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, Mathf.Clamp01(open));
            }
        }

        void Build()
        {
            Transform parent;
            if (display == Display.FloatingCard)
            {
                var root = new GameObject("Popup - " + DisplayName, typeof(RectTransform));
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 50;
                root.transform.localScale = Vector3.one / 220f;
                parent = root.transform;
            }
            else parent = GddHud.Get().transform;

            var s = GddGameSettings.Load();
            var go = new GameObject("Card", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            card = (RectTransform)go.transform;
            if (display == Display.ScreenBanner) { card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f); card.pivot = new Vector2(0.5f, 1f); }
            card.sizeDelta = new Vector2(620, 10);
            group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var bg = go.AddComponent<Image>();
            if (s != null) bg.sprite = s.panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(icon != null ? 140 : 40, 34, 24, 26);
            v.spacing = 6;
            v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var stripe = new GameObject("Accent", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            stripe.SetParent(card, false);
            stripe.GetComponent<LayoutElement>().ignoreLayout = true;
            stripe.anchorMin = new Vector2(0, 0); stripe.anchorMax = new Vector2(0, 1);
            stripe.pivot = new Vector2(0, 0.5f); stripe.sizeDelta = new Vector2(10, -16); stripe.anchoredPosition = new Vector2(10, 0);
            stripe.gameObject.AddComponent<Image>().color = accent;

            if (icon != null)
            {
                var ic = new GameObject("Icon", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
                ic.SetParent(card, false);
                ic.GetComponent<LayoutElement>().ignoreLayout = true;
                ic.anchorMin = ic.anchorMax = new Vector2(0, 0.5f); ic.pivot = new Vector2(0, 0.5f);
                ic.anchoredPosition = new Vector2(34, 0); ic.sizeDelta = new Vector2(88, 88);
                var img = ic.gameObject.AddComponent<Image>(); img.sprite = icon; img.preserveAspect = true;
            }

            titleText = Label(card, title, 38, FontStyles.Bold, Color.white);
            bodyText = Label(card, message, 26, FontStyles.Normal, new Color(0.9f, 0.93f, 1f));
            titleText.color = Color.Lerp(accent, Color.white, 0.55f);
        }

        static TextMeshProUGUI Label(Transform parent, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            GddHudFonts.Apply(t, GddHudFonts.Body);
            t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false;
            return t;
        }

        void OnDestroy() { if (card != null && display == Display.FloatingCard) Destroy(card.parent.gameObject); }

        void OnDrawGizmosSelected()
        {
            if (showWhen != ShowWhen.PlayerIsNear) return;
            Gizmos.color = new Color(accent.r, accent.g, accent.b, 0.5f);
            Gizmos.DrawWireSphere(transform.position, nearDistance);
        }
    }
}
