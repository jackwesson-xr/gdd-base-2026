using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>Springy squash and stretch on a transform. Only writes scale while the spring is moving.</summary>
    public class GddSquash : MonoBehaviour
    {
        public float stiffness = 260f;
        public float damping = 14f;
        Vector3 baseScale;
        float value, velocity;
        bool moving;

        public void Kick(float amount)
        {
            if (!moving) baseScale = transform.localScale;
            velocity -= amount * 22f;
            moving = true;
        }

        /// <summary>Grow from nothing with an overshoot. Used for spawns and respawns.</summary>
        public void PopIn()
        {
            baseScale = transform.localScale;
            value = -1f;
            velocity = 0f;
            moving = true;
            Apply();
        }

        void Update()
        {
            if (!moving) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            velocity += (-stiffness * value - damping * velocity) * dt;
            value += velocity * dt;
            if (Mathf.Abs(value) < 0.001f && Mathf.Abs(velocity) < 0.01f) { value = 0; velocity = 0; moving = false; }
            Apply();
        }

        void Apply()
        {
            // Negative value squashes (wide and short), positive stretches (tall and thin). -1 is fully collapsed.
            float y = Mathf.Max(0.001f, 1f + value);
            float xz = value < -0.95f ? Mathf.Max(0.001f, 1f + value) : 1f - value * 0.5f;
            transform.localScale = new Vector3(baseScale.x * xz, baseScale.y * y, baseScale.z * xz);
        }

        void OnDisable()
        {
            if (moving) { transform.localScale = baseScale; moving = false; value = 0; velocity = 0; }
        }
    }

    /// <summary>Gentle hover and spin for pickups sitting in the world. Stops when carried or dropped with physics.</summary>
    public class GddIdleFloat : MonoBehaviour
    {
        public float height = 0.15f;
        public float speed = 1.6f;
        public float spin = 50f;
        GddPickup pickup;
        Rigidbody body;
        Transform homeParent;
        Vector3 homePos;
        bool homed;
        float phase;

        void Awake()
        {
            pickup = GetComponent<GddPickup>();
            body = GetComponent<Rigidbody>();
            phase = Random.value * 10f;
        }

        void OnEnable() { homed = false; }

        void Update()
        {
            bool free = (pickup == null || pickup.Holder == null) && (body == null || body.isKinematic);
            if (!free || (homed && transform.parent != homeParent)) { homed = false; return; }
            if (!homed) { homeParent = transform.parent; homePos = transform.localPosition; homed = true; }
            transform.localPosition = homePos + Vector3.up * ((Mathf.Sin((Time.time + phase) * speed) * 0.5f + 0.5f) * height);
            transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
        }

        void OnDisable()
        {
            if (homed) transform.localPosition = homePos;
            homed = false;
        }
    }

    /// <summary>World-space "+1" style text that pops, rises and fades. Runs on unscaled time so freeze frames do not stall it.</summary>
    public class GddFloatingText : MonoBehaviour
    {
        TMPro.TextMeshPro label;
        float age;
        const float Life = 1.1f;
        Vector3 start;
        Color color;

        public static void Spawn(string text, Vector3 position, Color color, float size = 3.2f)
        {
            if (string.IsNullOrEmpty(text)) return;
            var go = new GameObject("Floating Text");
            go.transform.position = position;
            var t = go.AddComponent<GddFloatingText>();
            t.label = go.AddComponent<TMPro.TextMeshPro>();
            t.label.text = text;
            t.label.fontSize = size;
            t.label.fontStyle = TMPro.FontStyles.Bold;
            t.label.alignment = TMPro.TextAlignmentOptions.Center;
            t.label.color = color;
            t.label.outlineWidth = 0.22f;
            t.label.outlineColor = new Color32(0, 0, 0, 200);
            t.label.rectTransform.sizeDelta = new Vector2(6, 2);
            t.start = position;
            t.color = color;
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            float k = age / Life;
            if (k >= 1f) { Destroy(gameObject); return; }
            float rise = 1f - Mathf.Pow(1f - k, 3f);
            transform.position = start + Vector3.up * (1.2f * rise);
            float pop = k < 0.15f ? Mathf.Lerp(0.2f, 1.35f, k / 0.15f) : Mathf.Lerp(1.35f, 1f, Mathf.Clamp01((k - 0.15f) / 0.2f));
            transform.localScale = Vector3.one * pop;
            var c = color; c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            label.color = c;
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
