using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GDD110
{
    public enum GddLinkAction { Activate, TurnOn, TurnOff, Toggle, Reset, Nothing }

    /// <summary>
    /// Drag-and-drop wiring. Put it on anything that activates (trigger zone, interact button, timer, pickup...),
    /// then drag any GDD object into Targets. When this object activates, each target gets its action.
    /// Trigger zones can also send a second action when the player leaves.
    /// The object's own UnityEvents still fire as usual; this is a shortcut for the common cases.
    /// Lines in the Scene view show every connection.
    /// </summary>
    [AddComponentMenu("GDD 110/Functions/Link To Objects"), DisallowMultipleComponent]
    public class GddLink : MonoBehaviour
    {
        [Serializable]
        public class Target
        {
            [Tooltip("Any GDD object: spawner, toggle, mover, timeline, camera switch, another trigger...")]
            public GddEntity target;
            [Tooltip("What to do when this object activates.")]
            public GddLinkAction whenActivated = GddLinkAction.Activate;
            [Tooltip("Trigger zones only: what to do when the player leaves.")]
            public GddLinkAction whenExited = GddLinkAction.Nothing;
            [Tooltip("Seconds to wait before acting. Chain several targets with growing delays for a ripple.")]
            [Min(0)] public float delay;
        }

        public List<Target> targets = new List<Target>();

        /// <summary>Raised for every target that receives an action. The juice layer draws a pulse along the link.</summary>
        public static event Action<GddLink, GddEntity, GddLinkAction> Fired;

        readonly List<GddEntity> sources = new List<GddEntity>();

        void OnEnable()
        {
            sources.Clear();
            foreach (var e in GetComponents<GddEntity>())
            {
                sources.Add(e);
                e.onActivated.AddListener(OnSourceActivated);
                if (e is GddTrigger t) t.onExited.AddListener(OnSourceExited);
            }
        }

        void OnDisable()
        {
            foreach (var e in sources)
            {
                if (e == null) continue;
                e.onActivated.RemoveListener(OnSourceActivated);
                if (e is GddTrigger t) t.onExited.RemoveListener(OnSourceExited);
            }
            sources.Clear();
        }

        /// <summary>Fire every target's "when activated" action. Callable from any UnityEvent.</summary>
        public void FireAll() => OnSourceActivated();
        /// <summary>Fire every target's "when exited" action.</summary>
        public void FireExit() => OnSourceExited();

        void OnSourceActivated() { foreach (var t in targets) Run(t, t.whenActivated); }
        void OnSourceExited() { foreach (var t in targets) Run(t, t.whenExited); }

        void Run(Target t, GddLinkAction action)
        {
            if (t == null || t.target == null || action == GddLinkAction.Nothing) return;
            if (t.target.gameObject == gameObject && action == GddLinkAction.Activate) return; // no feedback loops
            if (t.delay > 0 && isActiveAndEnabled) StartCoroutine(Later(t.target, action, t.delay));
            else Apply(t.target, action);
        }

        IEnumerator Later(GddEntity target, GddLinkAction action, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (target != null) Apply(target, action);
        }

        void Apply(GddEntity target, GddLinkAction action)
        {
            Fired?.Invoke(this, target, action);
            if (target is GddTriggerable switchable)
            {
                switch (action)
                {
                    case GddLinkAction.TurnOn: switchable.TurnOn(); break;
                    case GddLinkAction.TurnOff: switchable.TurnOff(); break;
                    case GddLinkAction.Toggle: switchable.Toggle(); break;
                    case GddLinkAction.Reset: switchable.ResetState(); break;
                    default: switchable.Activate(); break;
                }
                return;
            }
            switch (action)
            {
                case GddLinkAction.TurnOn: target.EnableObject(); break;
                case GddLinkAction.TurnOff: target.DisableObject(); break;
                case GddLinkAction.Toggle: target.ToggleObject(); break;
                case GddLinkAction.Reset: if (target is GddTrigger trigger) trigger.ResetTrigger(); break;
                default: target.Activate(); break;
            }
        }

        public static Color ActionColor(GddLinkAction a)
        {
            switch (a)
            {
                case GddLinkAction.TurnOn: return new Color(0.35f, 1f, 0.5f);
                case GddLinkAction.TurnOff: return new Color(1f, 0.4f, 0.35f);
                case GddLinkAction.Toggle: return new Color(1f, 0.85f, 0.3f);
                case GddLinkAction.Reset: return new Color(0.75f, 0.5f, 1f);
                default: return new Color(0.3f, 0.85f, 1f);
            }
        }

        /// <summary>Point on the arc drawn between a link and its target (0 = here, 1 = target).</summary>
        public static Vector3 ArcPoint(Vector3 a, Vector3 b, float t)
        {
            float lift = Mathf.Clamp(Vector3.Distance(a, b) * 0.3f, 0.5f, 3f);
            Vector3 mid = (a + b) * 0.5f + Vector3.up * lift;
            return (1 - t) * (1 - t) * a + 2 * (1 - t) * t * mid + t * t * b;
        }

        void OnDrawGizmos()
        {
            Vector3 from = transform.position;
            foreach (var t in targets)
            {
                if (t == null || t.target == null) continue;
                Vector3 to = t.target.transform.position;
                Gizmos.color = ActionColor(t.whenActivated == GddLinkAction.Nothing ? t.whenExited : t.whenActivated);
                Vector3 prev = from;
                for (int i = 1; i <= 24; i++)
                {
                    Vector3 p = ArcPoint(from, to, i / 24f);
                    Gizmos.DrawLine(prev, p);
                    prev = p;
                }
                Vector3 dir = (to - ArcPoint(from, to, 0.9f)).normalized;
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * 0.18f;
                Gizmos.DrawLine(to, to - dir * 0.35f + side);
                Gizmos.DrawLine(to, to - dir * 0.35f - side);
                Gizmos.DrawWireSphere(to, 0.12f);
#if UNITY_EDITOR
                string label = t.whenActivated.ToString();
                if (t.whenExited != GddLinkAction.Nothing) label += " / exit: " + t.whenExited;
                if (t.delay > 0) label += " (+" + t.delay.ToString("0.##") + "s)";
                UnityEditor.Handles.color = Gizmos.color;
                UnityEditor.Handles.Label(ArcPoint(from, to, 0.5f), label);
#endif
            }
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.8f);
            Gizmos.DrawWireSphere(from, 0.18f);
        }
    }
}
