using TMPro;
using UnityEngine;

namespace GDD110
{
    [DisallowMultipleComponent]
    public sealed class GddWorldCue : MonoBehaviour
    {
        public string caption;
        public Color color = new Color(.7f, .98f, .84f);
        public float height = .75f;
        public bool pulse;
        public GddEntity action;
        public GddActor player;

        public bool Highlighted { get; set; }
        public Vector3 FocusPoint => transform.position + Vector3.up * height;
        public bool Available => action switch
        {
            GddCollectible item => !item.Collected,
            GddPickup item => !item.Holder && item.CanInteract(player),
            _ => !action || action.CanInteract(player)
        };

        TextMeshPro label;
        Camera view;

        void Start()
        {
            var marker = new GameObject("World cue", typeof(RectTransform), typeof(TextMeshPro));
            marker.transform.SetParent(transform, false);
            label = marker.GetComponent<TextMeshPro>();
            GddHudFonts.Apply(label, GddHudFonts.Body);
            label.rectTransform.sizeDelta = new Vector2(30, 4);
            label.fontSize = 5;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.richText = false;
            label.outlineWidth = .3f;
            label.outlineColor = new Color32(12, 24, 21, 255);
            label.text = caption;
            label.color = color;
        }

        void LateUpdate()
        {
            if (!label || !player) return;
            float distance = (player.transform.position - transform.position).sqrMagnitude;
            label.enabled = Available && distance < 144f;
            if (!label.enabled) return;
            if (!view || !view.isActiveAndEnabled) view = Camera.main;
            if (!view) return;
            string text = pulse && !Highlighted && player.Interactor.Candidate != action &&
                !(action is GddCollectible touchItem && touchItem.activation == GddCollectible.Activation.TouchIt && distance < 8f)
                ? "+" : caption;
            if (label.text != text) label.text = text;
            float bob = pulse ? Mathf.Sin(Time.time * 3.3f) * .065f : 0f;
            label.transform.SetPositionAndRotation(FocusPoint + Vector3.up * bob, view.transform.rotation);
            label.color = Highlighted ? Color.Lerp(color, Color.white, .55f) : color;
            float markerScale = text == "+" ? .31f : (Highlighted ? .17f : .135f);
            Vector3 scale = transform.lossyScale;
            label.transform.localScale = new Vector3(markerScale/Mathf.Max(.001f,Mathf.Abs(scale.x)),
                markerScale/Mathf.Max(.001f,Mathf.Abs(scale.y)),markerScale/Mathf.Max(.001f,Mathf.Abs(scale.z)));
        }
    }
}
