using System.Collections.Generic;
using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Game-view version of the GddLink gizmo: a soft arc from every link to each of its targets.
    /// When a link fires, a bright pulse runs along the arc and the target gets a small flash on arrival.
    /// </summary>
    [AddComponentMenu("")]
    public class GddLinkVisualizer : MonoBehaviour
    {
        class Wire
        {
            public GddLink link;
            public GddEntity target;
            public LineRenderer line;
            public Color color;
            public float pulseStart = -10f;
            public bool arrived = true;
        }

        const int Points = 28;
        const float PulseTime = 0.4f;
        readonly List<Wire> wires = new List<Wire>();
        readonly Vector3[] buffer = new Vector3[Points];
        GddJuiceDirector director;
        Gradient gradient = new Gradient();
        readonly GradientColorKey[] colorKeys = new GradientColorKey[3];
        readonly GradientAlphaKey[] alphaKeys = new GradientAlphaKey[5];

        public void Init(GddJuiceDirector owner)
        {
            director = owner;
            foreach (var link in FindObjectsByType<GddLink>(FindObjectsInactive.Include))
                foreach (var t in link.targets)
                {
                    if (t == null || t.target == null) continue;
                    var go = new GameObject("Link " + link.name + " -> " + t.target.name);
                    go.transform.SetParent(transform, false);
                    var lr = go.AddComponent<LineRenderer>();
                    lr.positionCount = Points;
                    lr.useWorldSpace = true;
                    lr.widthCurve = new AnimationCurve(new Keyframe(0, 0.16f), new Keyframe(0.5f, 0.11f), new Keyframe(1, 0.18f));
                    lr.material = owner.Settings.lineMaterial;
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lr.numCapVertices = 4;
                    var action = t.whenActivated == GddLinkAction.Nothing ? t.whenExited : t.whenActivated;
                    wires.Add(new Wire { link = link, target = t.target, line = lr, color = GddLink.ActionColor(action) });
                }
            GddLink.Fired += OnFired;
        }

        void OnDestroy() { GddLink.Fired -= OnFired; }

        void OnFired(GddLink link, GddEntity target, GddLinkAction action)
        {
            foreach (var w in wires)
                if (w.link == link && w.target == target) { w.pulseStart = Time.unscaledTime; w.arrived = false; }
        }

        void LateUpdate()
        {
            float now = Time.unscaledTime;
            foreach (var w in wires)
            {
                bool alive = w.link != null && w.target != null && w.link.isActiveAndEnabled;
                if (w.line.enabled != alive) w.line.enabled = alive;
                if (!alive) continue;

                Vector3 a = w.link.transform.position + Vector3.up * 0.2f;
                Vector3 b = w.target.transform.position + Vector3.up * 0.2f;
                for (int i = 0; i < Points; i++) buffer[i] = GddLink.ArcPoint(a, b, i / (Points - 1f));
                w.line.SetPositions(buffer);

                float p = (now - w.pulseStart) / PulseTime;
                float idle = 0.55f + 0.12f * Mathf.Sin(now * 2.5f);
                colorKeys[0] = new GradientColorKey(w.color, 0);
                colorKeys[2] = new GradientColorKey(w.color, 1);
                if (p >= 0 && p <= 1.15f)
                {
                    float k1 = Mathf.Clamp(p - 0.14f, 0.001f, 0.995f);
                    float k2 = Mathf.Clamp(p, k1 + 0.001f, 0.997f);
                    float k3 = Mathf.Clamp(p + 0.14f, k2 + 0.001f, 0.999f);
                    colorKeys[1] = new GradientColorKey(Color.Lerp(w.color, Color.white, 0.8f), k2);
                    alphaKeys[0] = new GradientAlphaKey(idle, 0);
                    alphaKeys[1] = new GradientAlphaKey(idle, k1);
                    alphaKeys[2] = new GradientAlphaKey(1f, k2);
                    alphaKeys[3] = new GradientAlphaKey(idle, k3);
                    alphaKeys[4] = new GradientAlphaKey(idle, 1);
                }
                else
                {
                    colorKeys[1] = new GradientColorKey(w.color, 0.5f);
                    for (int i = 0; i < 5; i++) alphaKeys[i] = new GradientAlphaKey(idle, i / 4f);
                }
                gradient.SetKeys(colorKeys, alphaKeys);
                w.line.colorGradient = gradient;

                if (!w.arrived && p >= 1f)
                {
                    w.arrived = true;
                    if (director != null) director.SpawnVfx(director.Settings.linkPulseVfx, b, 0.35f);
                }
            }
        }
    }
}
