using System.Collections;
using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Press a button linked to this and a hologram copy of a creature appears out in the open, far from the player.
    /// The camera visits it, then it comes at the player the way the real one moves and fizzles out on arrival,
    /// so you can watch the movement style without being hurt.
    /// </summary>
    [AddComponentMenu("GDD 110/Juice/Hologram Preview")]
    public class GddHologramPreview : GddEntity
    {
        public enum Style { Hop, Float, Roll, Walk }
        [Tooltip("The creature to copy (usually the one in the pen).")]
        public GameObject creature;
        public Style style;
        public Material hologramMaterial;
        public float distance = 22f;
        public float speed = 4f;
        public GameObject appearEffect, fizzleEffect;
        public AudioClip appearSound;

        public override void Activate()
        {
            RaiseActivated();
            var d = GddJuiceDirector.Instance;
            var player = d != null ? d.Player : FindAnyObjectByType<GddActor>()?.transform;
            if (player == null || creature == null) return;
            var cam = Camera.main;
            Vector3 dir = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : player.forward;
            if (dir.sqrMagnitude < 0.01f) dir = player.forward;
            dir = Quaternion.Euler(0, Random.Range(-25f, 25f), 0) * dir.normalized;
            Vector3 spot = player.position + dir * distance;
            if (Physics.Raycast(spot + Vector3.up * 30f, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore)) spot = hit.point;

            var holo = Instantiate(creature, spot, Quaternion.LookRotation(-dir));
            holo.name = "Hologram - " + creature.name;
            holo.SetActive(false);
            foreach (var mb in holo.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(mb);
            foreach (var rb in holo.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
            foreach (var c in holo.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var r in holo.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = new Material(hologramMaterial);
                    Texture tex = mats[i] != null ? (mats[i].HasProperty("_BaseMap") ? mats[i].GetTexture("_BaseMap") : mats[i].mainTexture) : null;
                    if (m.HasProperty("_Video")) m.SetTexture("_Video", tex != null ? tex : Texture2D.whiteTexture);
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            holo.SetActive(true);
            var runner = holo.AddComponent<HoloRunner>();
            runner.Init(player, style, speed, fizzleEffect);
            if (d != null) { d.SpawnVfx(appearEffect, spot + Vector3.up * 0.5f, 1.2f); d.PlaySfx(appearSound, 0.8f, 1.3f, 0f); }
            var focus = holo.AddComponent<GddFocusMoment>();
            focus.focusTime = 1.6f; focus.pushIn = 3f; focus.blendSeconds = 0.6f;
            focus.enabled = true;
        }
    }

    /// <summary>Moves a hologram toward the player in a chosen style, then fizzles out when it gets there.</summary>
    [AddComponentMenu("")]
    public class HoloRunner : MonoBehaviour
    {
        Transform target;
        GddHologramPreview.Style style;
        float speed, t, hopTimer;
        GameObject fizzle;
        Vector3 baseScale;
        float groundY;
        bool done;

        public void Init(Transform player, GddHologramPreview.Style s, float spd, GameObject fizzleFx)
        {
            target = player; style = s; speed = spd; fizzle = fizzleFx;
            baseScale = transform.localScale; groundY = transform.position.y;
            StartCoroutine(Appear());
        }

        IEnumerator Appear()
        {
            for (float k = 0; k < 1; k += Time.deltaTime / 0.4f) { transform.localScale = baseScale * Mathf.SmoothStep(0, 1, k); yield return null; }
            transform.localScale = baseScale;
        }

        void Update()
        {
            if (done || target == null) return;
            t += Time.deltaTime;
            if (t < 2.2f) { Flicker(); return; }   // wait while the camera looks at it
            Vector3 to = target.position - transform.position; to.y = 0;
            float dist = to.magnitude;
            if (dist < 1.3f) { StartCoroutine(Fizzle()); return; }
            Vector3 dir = to / Mathf.Max(dist, 0.001f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 6f);
            Vector3 p = transform.position + dir * speed * Time.deltaTime;
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore) && hit.transform.root != transform) groundY = hit.point.y;
            switch (style)
            {
                case GddHologramPreview.Style.Hop:
                    hopTimer += Time.deltaTime * 1.6f;
                    p.y = groundY + Mathf.Abs(Mathf.Sin(hopTimer * Mathf.PI)) * 1.2f;
                    break;
                case GddHologramPreview.Style.Float:
                    p.y = Mathf.Lerp(p.y, groundY + 1.4f + Mathf.Sin(t * 2f) * 0.25f, Time.deltaTime * 3f);
                    break;
                case GddHologramPreview.Style.Roll:
                    p.y = groundY + 0.3f;
                    transform.Rotate(Vector3.right, speed * 120f * Time.deltaTime, Space.Self);
                    break;
                default: p.y = groundY; break;
            }
            transform.position = p;
            Flicker();
        }

        void Flicker()
        {
            float f = Random.value < 0.04f ? 0.94f : 1f;
            transform.localScale = new Vector3(baseScale.x * f, baseScale.y, baseScale.z * f);
        }

        IEnumerator Fizzle()
        {
            done = true;
            var d = GddJuiceDirector.Instance;
            if (d != null) d.SpawnVfx(fizzle, transform.position + Vector3.up * 0.6f, 0.9f);
            for (float k = 0; k < 1; k += Time.deltaTime / 0.35f)
            {
                transform.localScale = new Vector3(baseScale.x * (1 - k), baseScale.y * (1 + k * 1.5f), baseScale.z * (1 - k));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
