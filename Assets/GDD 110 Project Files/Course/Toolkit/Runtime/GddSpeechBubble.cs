using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GDD110
{
    /// <summary>
    /// The bubble a GddDialog shows: white comic panel with a thick black outline, billboarded toward the camera,
    /// with a pointed tail (speech) or a trail of circles (thought) aimed at the speaker's mouth.
    /// Built at runtime; not placed by hand.
    /// </summary>
    [AddComponentMenu("")]
    public class GddSpeechBubble : MonoBehaviour
    {
        const float PixelsPerMeter = 150f;
        const float MaxWidth = 440f;
        const float Border = 6f;

        GddDialog owner;
        RectTransform root, outer, inner;
        TextMeshProUGUI label;
        GddBubbleTail tail;
        CanvasGroup group;
        string full = "";
        float typed, cps, open, openVel, blipTimer, hudBlend;
        bool wanted, thought;
        AudioClip blip;
        float blipVolume;
        AudioSource blipSource;

        public bool Typing => wanted && label != null && label.maxVisibleCharacters < full.Length;

        public void Init(GddDialog dialog)
        {
            owner = dialog;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;
            group = gameObject.AddComponent<CanvasGroup>();
            root = (RectTransform)transform;
            root.sizeDelta = new Vector2(10, 10);
            transform.localScale = Vector3.one / PixelsPerMeter;

            var s = GddGameSettings.Load();
            Sprite panel = s != null ? s.panelSprite : null;
            outer = Panel("Outline", root, Color.black, panel);
            inner = Panel("Fill", outer, Color.white, panel);
            inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(Border, Border); inner.offsetMax = new Vector2(-Border, -Border);

            var tgo = new GameObject("Tail", typeof(RectTransform), typeof(CanvasRenderer));
            tgo.transform.SetParent(root, false);
            tail = tgo.AddComponent<GddBubbleTail>();
            tail.raycastTarget = false;

            var lgo = new GameObject("Text", typeof(RectTransform));
            lgo.transform.SetParent(root, false);
            label = lgo.AddComponent<TextMeshProUGUI>();
            GddHudFonts.Apply(label, GddHudFonts.Body);
            label.fontSize = 34;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.08f, 0.08f, 0.1f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;

            blipSource = gameObject.AddComponent<AudioSource>();
            blipSource.playOnAwake = false;
            blipSource.spatialBlend = 0.7f;
            blipSource.maxDistance = 18f;
            gameObject.SetActive(false);
        }

        static RectTransform Panel(string name, Transform parent, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false;
            img.pixelsPerUnitMultiplier = 0.35f;   // rounder corners
            return (RectTransform)go.transform;
        }

        public void Show(string text, bool thinking, float charactersPerSecond, AudioClip talkBlip, float volume)
        {
            gameObject.SetActive(true);
            full = text;
            thought = thinking;
            cps = charactersPerSecond;
            blip = talkBlip;
            blipVolume = volume;
            label.text = text;
            label.fontStyle = thinking ? FontStyles.Bold | FontStyles.Italic : FontStyles.Bold;
            Vector2 size = label.GetPreferredValues(text, MaxWidth, 0);
            size.x = Mathf.Min(Mathf.Max(size.x, 90f), MaxWidth);
            size = label.GetPreferredValues(text, size.x, 0);
            Vector2 box = new Vector2(Mathf.Max(size.x, 90f) + 56f, size.y + 40f);
            outer.sizeDelta = box;
            label.rectTransform.sizeDelta = new Vector2(box.x - 48f, box.y - 30f);
            typed = cps > 0 ? 0 : full.Length;
            label.maxVisibleCharacters = Mathf.FloorToInt(typed);
            if (!wanted) { open = 0f; openVel = 0f; }
            else openVel += 4f;   // little bounce on each new line
            wanted = true;
        }

        public void FinishTyping() { typed = full.Length; label.maxVisibleCharacters = full.Length; }

        public void Hide() { wanted = false; }

        void LateUpdate()
        {
            if (owner == null) { Destroy(gameObject); return; }
            var cam = Camera.main;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            float goal = wanted ? 1f : 0f;
            openVel += (-(wanted ? 220f : 160f) * (open - goal) - 15f * openVel) * dt;
            open += openVel * dt;
            if (!wanted && open < 0.02f && Mathf.Abs(openVel) < 0.1f) { gameObject.SetActive(false); return; }

            // Speaker off screen (or the camera is busy looking elsewhere): move the line to the bottom of the HUD.
            bool offScreen = true;
            if (cam != null)
            {
                Vector3 vp = cam.WorldToViewportPoint(owner.MouthWorld + Vector3.up * owner.bubbleOffset.y);
                offScreen = vp.z < 0.2f || vp.x < 0.04f || vp.x > 0.96f || vp.y < 0.04f || vp.y > 0.96f
                         || vp.z > 14f;   // too far away to read

            }
            hudBlend = Mathf.MoveTowards(hudBlend, offScreen ? 1f : 0f, dt * 4f);
            float visible = Mathf.Clamp01(open * 1.5f);
            group.alpha = visible * (1f - hudBlend);
            if (hudBlend > 0.01f) GddHud.Get().Subtitle(this, owner.DisplayName, full, label.maxVisibleCharacters, visible * hudBlend, thought);

            if (wanted && typed < full.Length)
            {
                typed += dt * cps;
                int now = Mathf.Min(full.Length, Mathf.FloorToInt(typed));
                if (now != label.maxVisibleCharacters)
                {
                    label.maxVisibleCharacters = now;
                    blipTimer -= 1f;
                    if (blip != null && blipTimer <= 0f && now > 0 && !char.IsWhiteSpace(full[now - 1]))
                    {
                        blipTimer = 2f;
                        blipSource.pitch = Random.Range(0.9f, 1.25f);
                        blipSource.PlayOneShot(blip, blipVolume);
                    }
                }
            }

            Vector3 mouth = owner.MouthWorld;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            float scale = Mathf.Max(0f, open);
            Vector3 center = mouth + right * owner.bubbleOffset.x + Vector3.up * (owner.bubbleOffset.y + outer.sizeDelta.y * 0.5f / PixelsPerMeter);
            transform.position = Vector3.Lerp(mouth, center, Mathf.Clamp01(scale));
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
            transform.localScale = Vector3.one * (scale / PixelsPerMeter);

            // Tail: from the bubble's bottom edge to the mouth, in the bubble's own flat space.
            Vector3 local = root.InverseTransformPoint(mouth);
            Vector2 tip = new Vector2(local.x, local.y);
            float halfW = outer.sizeDelta.x * 0.5f, halfH = outer.sizeDelta.y * 0.5f;
            float baseX = Mathf.Clamp(tip.x * 0.35f, -halfW + 40f, halfW - 40f);
            tail.Set(new Vector2(baseX, -halfH), tip, thought, Border);
        }
    }

    /// <summary>Draws the bubble's tail: a black-outlined white wedge (speech) or shrinking circles (thought).</summary>
    [AddComponentMenu("")]
    public class GddBubbleTail : MaskableGraphic
    {
        Vector2 baseCenter, tip;
        bool thought;
        float border;

        public void Set(Vector2 baseAt, Vector2 tipAt, bool isThought, float outline)
        {
            if (baseAt == baseCenter && tipAt == tip && isThought == thought) return;
            baseCenter = baseAt; tip = tipAt; thought = isThought; border = outline;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 dir = tip - baseCenter;
            float len = dir.magnitude;
            if (len < 1f) return;
            dir /= len;
            Vector2 side = new Vector2(-dir.y, dir.x);
            if (Vector2.Dot(side, Vector2.right) < 0) side = -side;

            if (!thought)
            {
                float half = Mathf.Clamp(len * 0.22f, 18f, 34f);
                // outline wedge, base on the panel edge
                Tri(vh, baseCenter - Vector2.right * (half + border * 1.6f), baseCenter + Vector2.right * (half + border * 1.6f), tip + dir * border * 2f, Color.black);
                // white wedge, base tucked inside the panel so it covers the panel's outline at the join
                Vector2 b = baseCenter + Vector2.up * (border + 3f);
                Tri(vh, b - Vector2.right * half, b + Vector2.right * half, tip - dir * border * 0.4f, Color.white);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    float t = 0.25f + i * 0.3f;
                    float r = Mathf.Lerp(20f, 8f, i / 2f);
                    Vector2 c = Vector2.Lerp(baseCenter, tip, t);
                    Circle(vh, c, r + border, Color.black);
                    Circle(vh, c, r, Color.white);
                }
            }
        }

        static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, col, Vector2.zero);
            vh.AddVert(b, col, Vector2.zero);
            vh.AddVert(c, col, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }

        static void Circle(VertexHelper vh, Vector2 c, float r, Color col)
        {
            const int Segments = 20;
            int start = vh.currentVertCount;
            vh.AddVert(c, col, Vector2.zero);
            for (int s = 0; s <= Segments; s++)
            {
                float a = s / (float)Segments * Mathf.PI * 2f;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col, Vector2.zero);
            }
            for (int s = 1; s <= Segments; s++) vh.AddTriangle(start, start + s, start + s + 1);
        }
    }
}
