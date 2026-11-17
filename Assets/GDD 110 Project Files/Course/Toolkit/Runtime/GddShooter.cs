using System.Collections;
using UnityEngine;

namespace GDD110
{
    /// <summary>
    /// Fires a copy of a projectile (a cannonball, a snowball, anything) out of the muzzle when activated.
    /// Activate it from a Link, a button, a timer, or any UnityEvent (Fire).
    /// </summary>
    [AddComponentMenu("GDD 110/Functions/Shooter")]
    public class GddShooter : GddEntity
    {
        [Tooltip("What gets fired. A Rigidbody and collider are added if it has none.")]
        public GameObject projectile;
        [Tooltip("Where it comes out, pointing the way it flies. Empty = this object.")]
        public Transform muzzle;
        [Min(0)] public float speed = 16f;
        [Tooltip("Aims a little upward so shots arc. 0 = straight along the muzzle.")]
        [Range(0, 1)] public float arc = 0.3f;
        [Tooltip("Seconds before each shot disappears.")]
        [Min(0.1f)] public float lifetime = 4f;
        [Tooltip("Optional part that kicks back when firing (a barrel).")]
        public Transform recoilPart;

        public override void Activate() { Fire(); }

        public void Fire()
        {
            if (projectile == null) return;
            var m = muzzle != null ? muzzle : transform;
            var shot = Instantiate(projectile, m.position, m.rotation);
            shot.name = projectile.name;
            shot.SetActive(true);
            if (shot.GetComponentInChildren<Collider>() == null) shot.AddComponent<SphereCollider>().radius = 0.45f;
            var rb = shot.GetComponent<Rigidbody>();
            if (rb == null) rb = shot.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 2f;
            rb.linearVelocity = (m.forward + Vector3.up * arc).normalized * speed;
            Destroy(shot, lifetime);
            if (recoilPart != null) StartCoroutine(Recoil());
            RaiseActivated();
        }

        IEnumerator Recoil()
        {
            Vector3 home = recoilPart.localPosition;
            Vector3 back = home - recoilPart.parent.InverseTransformDirection((muzzle != null ? muzzle : transform).forward) * 0.25f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.35f)
            {
                recoilPart.localPosition = Vector3.Lerp(back, home, t * t);
                yield return null;
            }
            recoilPart.localPosition = home;
        }

        void OnDrawGizmosSelected()
        {
            var m = muzzle != null ? muzzle : transform;
            Gizmos.color = new Color(1f, 0.5f, 0.2f);
            Gizmos.DrawLine(m.position, m.position + (m.forward + Vector3.up * arc).normalized * 2.5f);
        }
    }
}
