using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Power-up feel for foot items: a charge-up moment when they go on (slow-mo, burst, camera kick), glowing
    /// trails on both feet, and while worn either speed lines with a wider, looser camera (Speed) or a blast
    /// under the feet on every jump (Jump).
    /// </summary>
    [AddComponentMenu("GDD 110/Juice/Boots Juice"), RequireComponent(typeof(GddPickup))]
    public class GddBootsJuice : MonoBehaviour
    {
        public enum Style { Speed, Jump }
        public Style style;
        public Color color = new Color(0.3f, 0.8f, 1f);
        public GameObject chargeEffect, motionEffect;
        public AudioClip chargeSound, motionSound;
        [Tooltip("Speed: extra FOV at full speed.")]
        public float speedFov = 12f;

        GddPickup pickup;
        GddActor actor;
        CharacterController cc;
        TrailRenderer[] trails = new TrailRenderer[0];
        CinemachineThirdPersonFollow follow;
        Vector3 savedDamping;
        bool wasGrounded = true;
        float nextBurst;
        Material trailMat;

        void Awake()
        {
            pickup = GetComponent<GddPickup>();
            pickup.onEquipped.AddListener(OnEquip);
            pickup.onDropped.AddListener(OnOff);
        }

        void OnEquip() { StartCoroutine(Equip()); }

        IEnumerator Equip()
        {
            yield return null;   // let the mirrored pair appear
            actor = pickup.Holder;
            if (actor == null) yield break;
            cc = actor.GetComponent<CharacterController>();
            var d = GddJuiceDirector.Instance;
            // Charge-up: a beat of slow motion while the power floods in.
            Vector3 feet = actor.transform.position;
            if (d != null) { d.SpawnVfx(chargeEffect, feet, 1.2f); d.PlaySfx(chargeSound, 0.9f, style == Style.Speed ? 1.2f : 0.95f, 0f); d.Shake(0.5f); }
            GddJuiceCamera.Punch(-8f, 0.5f);
            Time.timeScale = 0.25f;
            yield return new WaitForSecondsRealtime(0.35f);
            Time.timeScale = 1f;
            GddJuiceCamera.Punch(16f, -0.5f);
            if (d != null) d.SpawnVfx(motionEffect, feet + Vector3.up * 0.3f, 1f);
            MakeTrails();
            if (style == Style.Speed)
            {
                follow = FindAnyObjectByType<CinemachineThirdPersonFollow>();
                if (follow != null) { savedDamping = follow.Damping; follow.Damping = new Vector3(0.9f, 0.5f, 0.9f); }
            }
        }

        void MakeTrails()
        {
            ClearTrails();
            if (trailMat == null)
            {
                trailMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                trailMat.SetFloat("_Surface", 1); trailMat.SetFloat("_Blend", 2);   // additive
                trailMat.SetColor("_BaseColor", color);
            }
            var feet = new System.Collections.Generic.List<Transform> { transform };
            if (pickup.Pair != null) feet.Add(pickup.Pair);
            trails = new TrailRenderer[feet.Count];
            for (int i = 0; i < feet.Count; i++)
            {
                var go = new GameObject("Boot Trail");
                go.transform.SetParent(feet[i], false);
                var t = go.AddComponent<TrailRenderer>();
                t.time = style == Style.Speed ? 0.35f : 0.5f;
                t.minVertexDistance = 0.05f;
                t.widthCurve = new AnimationCurve(new Keyframe(0, 0.16f), new Keyframe(1, 0));
                t.colorGradient = new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(color, 0.3f), new GradientColorKey(color, 1) }, alphaKeys = new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0, 1) } };
                t.sharedMaterial = trailMat;
                t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trails[i] = t;
            }
        }

        void ClearTrails() { foreach (var t in trails) if (t != null) Destroy(t.gameObject); trails = new TrailRenderer[0]; }

        void OnOff()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            ClearTrails();
            if (follow != null) { follow.Damping = savedDamping; follow = null; }
            actor = null;
        }

        void Update()
        {
            if (actor == null || cc == null) return;
            var d = GddJuiceDirector.Instance;
            Vector3 v = cc.velocity;
            float flat = new Vector2(v.x, v.z).magnitude;
            foreach (var t in trails) if (t != null) t.emitting = style == Style.Jump ? !cc.isGrounded || flat > 2f : flat > 1.5f;
            if (style == Style.Speed)
            {
                float k = Mathf.InverseLerp(4f, 12f, flat);
                GddJuiceCamera.HoldFov = Mathf.Max(GddJuiceCamera.HoldFov, speedFov * k);
                if (k > 0.5f && Time.time > nextBurst && d != null)
                {
                    nextBurst = Time.time + 0.45f;
                    var fx = d.SpawnVfx(motionEffect, actor.transform.position + Vector3.up * 0.9f, 0.7f);
                    if (fx != null) fx.transform.rotation = Quaternion.LookRotation(new Vector3(v.x, 0, v.z));
                }
            }
            else
            {
                bool grounded = cc.isGrounded;
                if (wasGrounded && !grounded && v.y > 2f && d != null)
                {
                    d.SpawnVfx(chargeEffect, actor.transform.position, 0.8f);
                    d.PlaySfx(motionSound, 0.8f, Random.Range(1.1f, 1.3f), 0f);
                    d.Shake(0.35f);
                    GddJuiceCamera.Punch(10f, -0.3f);
                }
                if (!wasGrounded && grounded && d != null) { d.Shake(0.25f); GddJuiceCamera.Punch(-4f, 0.15f); }
                wasGrounded = grounded;
            }
        }

        void OnDisable() { if (actor != null) OnOff(); }
    }
}
