using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Shows how a creature feels about the player. Friendly: a floating heart, heart bursts and happy hops when you
    /// come close. Angry: a throbbing red anger mark, red flashes and shaking that get stronger as you approach.
    /// </summary>
    [AddComponentMenu("")]
    public class GddCreatureMood : MonoBehaviour
    {
        public bool angry;
        GddJuiceDirector director;
        Transform icon;
        Material iconMat;
        Renderer[] bodies;
        MaterialPropertyBlock block;
        float height, nextBurst, phase, shakeSeed;
        GddSquash squash;
        Vector3 iconBase;
        Transform shakeTarget;

        public void Init(GddJuiceDirector owner, bool isAngry)
        {
            director = owner;
            angry = isAngry;
            phase = Random.value * 10f;
            shakeSeed = Random.value * 100f;
            squash = GetComponent<GddSquash>();
            if (squash == null) squash = gameObject.AddComponent<GddSquash>();
            bodies = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            var b = new Bounds(transform.position, Vector3.zero);
            foreach (var r in bodies) b.Encapsulate(r.bounds);
            height = b.max.y - transform.position.y + 0.35f;

            var tex = angry ? owner.Settings.angerIcon : owner.Settings.heartIcon;
            if (tex == null) return;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = angry ? "Anger Mark" : "Heart";
            icon = q.transform;
            iconMat = new Material(owner.Settings.lineMaterial) { mainTexture = tex };
            iconMat.color = Color.white;
            q.GetComponent<Renderer>().sharedMaterial = iconMat;
            q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float s = Mathf.Clamp(b.size.x, 0.35f, 0.8f);
            icon.localScale = Vector3.one * s;
        }

        void LateUpdate()
        {
            if (director == null || icon == null) return;
            var player = director.Player;
            float dist = player != null ? Vector3.Distance(player.position, transform.position) : 99f;
            float near = Mathf.Clamp01(1f - (dist - 2f) / 7f);   // 0 far, 1 within ~2m
            float t = Time.time + phase;

            Vector3 side = angry ? new Vector3(0.28f, 0, 0) : Vector3.zero;
            var cam = Camera.main;
            Vector3 pos = transform.position + Vector3.up * (height + Mathf.Sin(t * 2.2f) * 0.06f);
            if (cam != null) pos += cam.transform.right * side.x;
            icon.position = pos;
            if (cam != null) icon.rotation = Quaternion.LookRotation(icon.position - cam.transform.position);

            if (angry)
            {
                float rate = Mathf.Lerp(3f, 11f, near);
                float throb = 1f + 0.25f * Mathf.Max(0, Mathf.Sin(t * rate));
                icon.localScale = Vector3.one * 0.55f * throb;
                iconMat.color = Color.Lerp(new Color(1f, 0.35f, 0.3f, Mathf.Lerp(0.6f, 1f, near)), Color.white, 0.2f * Mathf.Sin(t * rate));
                float flash = near * Mathf.Max(0, Mathf.Sin(t * rate * 0.5f));
                foreach (var r in bodies)
                {
                    if (r == null || r.transform == icon) continue;
                    r.GetPropertyBlock(block);
                    block.SetColor("_EmissionColor", new Color(1f, 0.08f, 0.05f) * flash * 1.5f);
                    r.SetPropertyBlock(block);
                }
                if (near > 0.5f && Time.time >= nextBurst)
                {
                    nextBurst = Time.time + Random.Range(0.9f, 1.6f);
                    squash.Kick(0.35f);
                    director.PlaySfxAt(director.Settings.angrySound, 0.35f, Random.Range(0.9f, 1.15f), transform.position);
                }
            }
            else
            {
                icon.localScale = Vector3.one * 0.45f * (1f + 0.12f * Mathf.Sin(t * 3f));
                iconMat.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.5f, 1f, near));
                if (near > 0.45f && Time.time >= nextBurst)
                {
                    nextBurst = Time.time + Random.Range(1.1f, 1.8f);
                    director.SpawnVfx(director.Settings.heartBurstVfx, transform.position + Vector3.up * height, 0.45f);
                    director.PlaySfxAt(director.Settings.happySound, 0.35f, Random.Range(1.1f, 1.35f), transform.position);
                    squash.Kick(-0.4f);
                }
            }
        }

        void OnDisable() { if (icon != null) icon.gameObject.SetActive(false); }
        void OnEnable() { if (icon != null) icon.gameObject.SetActive(true); }
        void OnDestroy() { if (icon != null) Destroy(icon.gameObject); if (iconMat != null) Destroy(iconMat); }
    }
}
