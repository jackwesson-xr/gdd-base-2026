using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GDD110
{
    public static class GddHudFonts
    {
        static TMP_FontAsset body, title;
        public static TMP_FontAsset Title => title ? title : title = Resources.Load<TMP_FontAsset>("GddHud/Papercuts SDF");
        public static TMP_FontAsset Body => body ? body : body = Resources.Load<TMP_FontAsset>("GddHud/Very Popular SDF");

        public static void AddPanelShadow(Graphic graphic)
        {
            if (!graphic || graphic.TryGetComponent<Shadow>(out _)) return;
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0,0,0,.66f);
            shadow.effectDistance = new Vector2(3,-4);
            shadow.useGraphicAlpha = true;
        }

        public static void Apply(TMP_Text label, TMP_FontAsset font)
        {
            if (!label || !font) return;
            label.font = font;
            label.fontSharedMaterial = font.material;
        }
    }
}
