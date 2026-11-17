using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GDD110
{
    public enum GddFillLook { Clip = 0, Grey = 1, Faded = 2, Dark = 3, DebugChunks = 4 }

    /// <summary>
    /// Health shown as a row of icons that empty right to left (5 hearts = 20% each).
    /// Bring your own icon (one heart is enough with "One Icon Per Chunk") and an optional frame that never empties.
    /// Pure white and transparent pixels are both see-through, so plain JPGs work.
    /// One is created automatically for the player (see Game Settings); add this to your own Canvas to place it yourself.
    /// </summary>
    [AddComponentMenu("GDD 110/HUD/Health Bar")]
    public class GddHealthHud : MonoBehaviour
    {
        [Tooltip("Leave empty to follow the player.")]
        public GddHealth health;
        [Tooltip("The image that empties. With 'One Icon Per Chunk' a single heart is enough.")]
        public Sprite icon;
        [Tooltip("Optional overlay drawn on top that never empties (an outline, a socket, a frame).")]
        public Sprite frame;
        [Tooltip("How many parts the bar is split into. Uses the Health's own Chunks when that is set.")]
        [Min(1)] public int chunks = 5;
        public bool oneIconPerChunk = true;
        public GddFillLook unfilledLook = GddFillLook.Grey;
        public bool pureWhiteIsClear = true;
        [Min(8)] public float iconSize = 64f;
        [Tooltip("Hearts drain smoothly at this speed (fraction of the bar per second).")]
        public float drainSpeed = 2.5f;
        public UnityEvent onDamaged = new UnityEvent();
        public UnityEvent onHealed = new UnityEvent();

        Image image;
        Material mat;
        RectTransform rt;
        Vector2 basePos;
        float shown = 1f, target = 1f, shake, punch, flash;
        GddHealth bound;

        static Shader shader;

        /// <summary>Called by the HUD when it builds the default bar.</summary>
        public void Configure(GddGameSettings s)
        {
            if (icon == null) icon = s.healthIcon;
            if (frame == null) frame = s.healthFrame;
            chunks = Mathf.Max(1, s.healthChunks);
            unfilledLook = s.healthLook;
            shader = s.fillShader;
        }

        void Start()
        {
            rt = transform as RectTransform;
            image = GetComponent<Image>();
            if (image == null) image = gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            if (shader == null) { var s = GddGameSettings.Load(); if (s != null) shader = s.fillShader; }
            if (shader == null) shader = Shader.Find("GDD/UI/Fill Icons");
            mat = new Material(shader);
            image.material = mat;
            Apply();
            basePos = rt.anchoredPosition;
        }

        void Apply()
        {
            int n = health != null && health.chunks > 0 ? health.chunks : chunks;
            image.sprite = icon;
            mat.SetFloat("_Chunks", n);
            mat.SetFloat("_Tile", oneIconPerChunk ? 1 : 0);
            mat.SetFloat("_Mode", (float)unfilledLook);
            mat.SetFloat("_WhiteIsClear", pureWhiteIsClear ? 1 : 0);
            mat.SetFloat("_UseFrame", frame != null ? 1 : 0);
            if (frame != null) mat.SetTexture("_FrameTex", frame.texture);
            float aspect = icon != null ? icon.rect.width / icon.rect.height : 1f;
            rt.sizeDelta = oneIconPerChunk ? new Vector2(iconSize * aspect * n, iconSize) : new Vector2(iconSize * aspect, iconSize);
        }

        void Bind()
        {
            if (health == null)
            {
                var actor = FindAnyObjectByType<GddActor>();
                if (actor != null) health = actor.GetComponent<GddHealth>();
            }
            if (health == null || health == bound) return;
            bound = health;
            shown = target = health.Fraction;
            health.onHealthChanged.AddListener(OnChanged);
            Apply();
        }

        void OnChanged(float value)
        {
            float f = health.Fraction;
            if (f < target - 0.001f) { shake = 1f; onDamaged.Invoke(); }
            else if (f > target + 0.001f) { punch = 1f; flash = 1f; onHealed.Invoke(); }
            target = f;
        }

        void Update()
        {
            if (mat == null) return;
            Bind();
            float dt = Time.unscaledDeltaTime;
            shown = Mathf.MoveTowards(shown, target, dt * drainSpeed);
            mat.SetFloat("_Fill", shown);
            shake = Mathf.Max(0, shake - dt * 3f);
            punch = Mathf.Max(0, punch - dt * 3f);
            flash = Mathf.Max(0, flash - dt * 4f);
            rt.anchoredPosition = basePos + Random.insideUnitCircle * (shake * shake * 14f);
            rt.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(punch * Mathf.PI));
            image.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 1f) * 1.6f, flash);
        }

        void OnDestroy()
        {
            if (bound != null) bound.onHealthChanged.RemoveListener(OnChanged);
            if (mat != null) Destroy(mat);
        }
    }
}
