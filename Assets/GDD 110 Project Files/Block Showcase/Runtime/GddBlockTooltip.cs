using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GDD110.Juice
{
    /// <summary>
    /// Floating guide card for one block. Springs open when the player walks close, drifts around the block on the
    /// side facing the camera, types its description out, and tethers itself to the block with a glowing line.
    /// Content comes from the block's GddBlockInfo asset.
    /// </summary>
    [AddComponentMenu("")]
    public class GddBlockTooltip : MonoBehaviour
    {
        GddBlockInfo info;
        Transform target;
        GddJuiceSettings settings;
        Canvas canvas;
        CanvasGroup group;
        RectTransform card;
        TextMeshProUGUI body;
        LineRenderer tether;
        Light glow;
        float open, openVel, typed, phase;
        bool wanted, shown;
        Vector3 smoothPos;
        float topOffset = 1f, sideOffset = 1.2f;

        static readonly System.Collections.Generic.List<GddBlockTooltip> all = new System.Collections.Generic.List<GddBlockTooltip>();
        const int MaxOpen = 2;
        float lastDistance = float.MaxValue;

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        /// <summary>Only the nearest few cards open at once, so crowded spots stay readable.</summary>
        bool AmongNearest(float dist)
        {
            int closer = 0;
            foreach (var t in all)
                if (t != this && t.lastDistance < dist && t.lastDistance < settings.tooltipRadius && ++closer >= MaxOpen) return false;
            return true;
        }

        public void Init(GddBlockInfo blockInfo, Transform blockTarget, GddJuiceSettings juiceSettings, int number)
        {
            info = blockInfo; target = blockTarget; settings = juiceSettings;
            phase = number * 1.37f;
            gameObject.name = "Guide - " + info.blockName;
            BuildCard(number);
            MeasureTarget();
            smoothPos = TargetPosition();
            SetVisible(false);
        }

        void BuildCard(int number)
        {
            var cgo = new GameObject("Card", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            group = cgo.AddComponent<CanvasGroup>();
            card = (RectTransform)cgo.transform;
            card.sizeDelta = new Vector2(560, 10);
            card.pivot = new Vector2(0.5f, 0f);
            card.localScale = Vector3.one * 0.0026f;

            var panel = cgo.AddComponent<Image>();
            panel.sprite = settings.panelSprite;
            panel.type = Image.Type.Sliced;
            panel.color = new Color(0.07f, 0.08f, 0.11f, 0.9f);
            var layout = cgo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 28, 22, 26);
            layout.spacing = 8;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            var fitter = cgo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bar = new GameObject("Accent", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            bar.transform.SetParent(cgo.transform, false);
            bar.GetComponent<LayoutElement>().ignoreLayout = true;
            var br = (RectTransform)bar.transform;
            br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(0, 1);
            br.pivot = new Vector2(0, 0.5f);
            br.sizeDelta = new Vector2(12, -24);
            br.anchoredPosition = new Vector2(14, 0);
            bar.GetComponent<Image>().color = info.accent;

            if (!string.IsNullOrEmpty(info.category))
                Label(cgo.transform, "<color=#" + ColorUtility.ToHtmlStringRGB(info.accent) + ">" + info.category.ToUpperInvariant() + "</color>", 17, FontStyles.Bold, Color.white).characterSpacing = 6;
            var heading = Label(cgo.transform, info.blockName, 40, FontStyles.Bold, Color.white);
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            if (!string.IsNullOrEmpty(info.menuPath)) Label(cgo.transform, info.menuPath, 17, FontStyles.Normal, new Color(0.62f, 0.66f, 0.74f));
            body = Label(cgo.transform, info.whatItDoes, 24, FontStyles.Normal, new Color(0.93f, 0.95f, 1f));
            if (!string.IsNullOrEmpty(info.howToUse))
                Label(cgo.transform, "<b>Try it:</b> " + info.howToUse, 20, FontStyles.Italic, Color.Lerp(info.accent, Color.white, 0.35f));

            tether = gameObject.AddComponent<LineRenderer>();
            tether.positionCount = 2;
            tether.useWorldSpace = true;
            tether.widthCurve = new AnimationCurve(new Keyframe(0, 0.035f), new Keyframe(1, 0.008f));
            tether.material = settings.lineMaterial;
            tether.startColor = info.accent;
            tether.endColor = new Color(info.accent.r, info.accent.g, info.accent.b, 0.2f);
            tether.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var lgo = new GameObject("Glow");
            lgo.transform.SetParent(transform, false);
            glow = lgo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = info.accent;
            glow.range = 3.5f;
            glow.intensity = 0;
            glow.shadows = LightShadows.None;
        }

        TextMeshProUGUI Label(Transform parent, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            GddHudFonts.Apply(t, GddHudFonts.Body);
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            return t;
        }

        void MeasureTarget()
        {
            if (target == null) return;
            bool any = false;
            var b = new Bounds(target.position, Vector3.zero);
            foreach (var r in target.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || !r.enabled) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) foreach (var c in target.GetComponentsInChildren<Collider>()) { if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds); }
            topOffset = b.max.y - target.position.y;
            sideOffset = Mathf.Max(b.extents.x, b.extents.z) + 0.7f;
        }

        Vector3 TargetPosition()
        {
            var cam = Camera.main;
            Vector3 toCam = cam != null ? cam.transform.position - target.position : Vector3.back;
            toCam.y = 0;
            if (toCam.sqrMagnitude < 0.01f) toCam = Vector3.back;
            float drift = Mathf.Sin(Time.unscaledTime * 0.45f + phase) * 14f;
            Vector3 side = Quaternion.Euler(0, 48f + drift, 0) * toCam.normalized;
            float bob = Mathf.Sin(Time.unscaledTime * 1.4f + phase) * 0.07f;
            return target.position + side * sideOffset + Vector3.up * (topOffset + 0.25f + bob);
        }

        void Update()
        {
            if (target == null) { Destroy(gameObject); return; }
            var director = GddJuiceDirector.Instance;
            bool targetVisible = target.gameObject.activeInHierarchy && !HeldByPlayer(director);
            float dist = director != null && director.Player != null ? Vector3.Distance(director.Player.position, target.position) : float.MaxValue;
            lastDistance = targetVisible ? dist : float.MaxValue;
            bool want = targetVisible && dist < settings.tooltipRadius && AmongNearest(dist);

            if (want != wanted)
            {
                wanted = want;
                if (want) { MeasureTarget(); openVel += 9f; typed = 0; if (director != null) director.PlaySfx(settings.tooltipOpen, 0.55f, 1.05f + Random.value * 0.1f, 0f); }
                else if (director != null) director.PlaySfx(settings.tooltipClose, 0.35f, 1f, 0f);
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float goal = wanted ? 1f : 0f;
            openVel += (-(wanted ? 190f : 120f) * (open - goal) - 13f * openVel) * dt;
            open += openVel * dt;
            if (!wanted && open < 0.02f && Mathf.Abs(openVel) < 0.1f) { open = 0; openVel = 0; }

            bool visible = open > 0.01f;
            if (visible != shown) SetVisible(visible);
            glow.intensity = Mathf.Lerp(glow.intensity, wanted ? 2.2f : 0f, dt * 6f);
            glow.enabled = glow.intensity > 0.02f;
            glow.transform.position = target.position + Vector3.up * 0.4f;
            if (!visible) return;

            smoothPos = Vector3.Lerp(smoothPos, TargetPosition(), 1f - Mathf.Exp(-7f * dt));
            card.position = smoothPos;
            card.localScale = Vector3.one * 0.0026f * Mathf.Max(0f, open);
            group.alpha = Mathf.Clamp01(open * 1.6f);
            var cam = Camera.main;
            if (cam != null) card.rotation = Quaternion.LookRotation(card.position - cam.transform.position, Vector3.up);

            if (wanted && body != null)
            {
                typed += dt * 110f;
                body.maxVisibleCharacters = Mathf.FloorToInt(typed);
            }

            tether.SetPosition(0, card.position + Vector3.up * 0.02f);
            tether.SetPosition(1, target.position + Vector3.up * Mathf.Max(0.1f, topOffset * 0.85f));
            var c = info.accent; c.a = group.alpha;
            tether.startColor = c;
        }

        /// <summary>Worn or carried by the player: the guide closes instead of riding along on their head or hand.</summary>
        bool HeldByPlayer(GddJuiceDirector director)
        {
            var player = director != null ? director.Player : null;
            if (player != null && target.IsChildOf(player.root)) return true;
            var snap = target.GetComponent<GddCollectible>();
            if (snap != null && snap.Wearer != null) return true;
            var pickup = target.GetComponent<GddPickup>();
            return pickup != null && pickup.Holder != null;
        }

        void SetVisible(bool v)
        {
            shown = v;
            canvas.enabled = v;
            tether.enabled = v;
            if (!v && body != null) body.maxVisibleCharacters = 0;
        }
    }
}
