using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GDD110
{
    /// <summary>Makes objects appear (On) or disappear (Off), with a quick grow or shrink. Bridges, walls, rewards, lights.</summary>
    [AddComponentMenu("GDD 110/Triggerables/Toggle Objects")]
    public class GddToggleObjects : GddTriggerable
    {
        [Tooltip("Shown while On, hidden while Off. Tick 'Invert' to do the opposite.")]
        public List<GameObject> objects = new List<GameObject>();
        public bool invert;
        [Tooltip("Grow and shrink instead of popping instantly.")]
        public bool animate = true;
        [Min(0.01f)] public float animationTime = 0.3f;
        [Tooltip("Seconds between each object when there are several, for a ripple.")]
        [Min(0)] public float stagger = 0.08f;

        readonly Dictionary<GameObject, Vector3> scales = new Dictionary<GameObject, Vector3>();

        void Awake()
        {
            foreach (var o in objects) if (o != null && !scales.ContainsKey(o)) scales[o] = o.transform.localScale;
        }

        protected override void ApplyState(bool on, bool instant)
        {
            bool show = on != invert;
            StopAllCoroutines();
            for (int i = 0; i < objects.Count; i++)
            {
                var o = objects[i];
                if (o == null) continue;
                if (instant || !animate || !isActiveAndEnabled)
                {
                    o.SetActive(show);
                    if (scales.TryGetValue(o, out var s)) o.transform.localScale = s;
                }
                else StartCoroutine(Animate(o, show, i * stagger));
            }
        }

        IEnumerator Animate(GameObject o, bool show, float wait)
        {
            if (wait > 0) yield return new WaitForSeconds(wait);
            if (!scales.TryGetValue(o, out var full)) full = scales[o] = o.transform.localScale;
            if (show && !o.activeSelf) { o.transform.localScale = Vector3.zero; o.SetActive(true); }
            if (!show && !o.activeSelf) yield break;
            float t = 0;
            Vector3 from = o.transform.localScale, to = show ? full : Vector3.zero;
            while (t < 1)
            {
                t += Time.deltaTime / animationTime;
                float k = Mathf.Clamp01(t);
                float ease = show ? 1 + 2.2f * Mathf.Pow(k - 1, 3) + 1.2f * Mathf.Pow(k - 1, 2) : k * k; // overshoot on grow
                o.transform.localScale = Vector3.LerpUnclamped(from, to, ease);
                yield return null;
            }
            o.transform.localScale = to;
            if (!show) { o.SetActive(false); o.transform.localScale = full; }
        }
    }
}
