using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// Makes a carried Pickup throwable: press Use (left click / RT) to lob it where the camera points.
    /// With Explode On Contact it goes off on the first thing it hits: damage, a push for nearby physics objects,
    /// an effect and a sound. Afterwards it reappears where it started, ready to pick up again.
    /// </summary>
    [RequireComponent(typeof(GddPickup)), AddComponentMenu("GDD 110/Functions/Throwable")]
    public class GddThrowable : GddEntity, IGddUsable
    {
        public float throwSpeed = 14f;
        [Tooltip("Extra upward speed so it arcs.")]
        public float lift = 4f;
        public bool explodeOnContact = true;
        public float blastRadius = 3f;
        public float damage = 40f;
        public float pushForce = 9f;
        public GameObject explosionEffect;
        public AudioClip explosionSound;
        [Tooltip("Seconds after exploding before it reappears where it started. 0 = never.")]
        [Min(0)] public float resetAfter = 2.5f;
        public UnityEvent onThrown = new UnityEvent();
        public UnityEvent onExploded = new UnityEvent();

        GddPickup pickup;
        Rigidbody body;
        GddActor thrower;
        bool armed;
        float armedAt;
        Transform homeParent;
        Vector3 homePos, homeScale;
        Quaternion homeRot;

        void Awake()
        {
            pickup = GetComponent<GddPickup>();
            body = GetComponent<Rigidbody>();
            homeParent = transform.parent; homePos = transform.localPosition; homeRot = transform.localRotation; homeScale = transform.localScale;
        }

        public void Use(GddActor holder)
        {
            if (holder == null || armed) return;
            var cam = Camera.main;
            Vector3 aim = cam != null ? cam.transform.forward : holder.transform.forward;
            Vector3 from = transform.position;
            pickup.Drop();
            transform.position = from;
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = false; body.useGravity = true; body.detectCollisions = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = aim * throwSpeed + Vector3.up * lift;
            body.angularVelocity = Random.insideUnitSphere * 8f;
            foreach (var mine in GetComponentsInChildren<Collider>()) foreach (var theirs in holder.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(mine, theirs, true);
            thrower = holder; LastActor = holder;
            armed = explodeOnContact; armedAt = Time.time;
            onThrown.Invoke();
        }

        void OnCollisionEnter(Collision c)
        {
            if (!armed || Time.time - armedAt < 0.05f) return;
            if (thrower != null && c.transform.IsChildOf(thrower.transform)) return;
            Explode();
        }

        /// <summary>Blow up right now (also works from a Link or UnityEvent).</summary>
        public void Explode()
        {
            armed = false;
            Vector3 at = transform.position;
            if (explosionEffect != null) { var fx = Instantiate(explosionEffect, at, Quaternion.identity); Destroy(fx, 5f); }
            if (explosionSound != null) AudioSource.PlayClipAtPoint(explosionSound, at, 1f);
            foreach (var hit in Physics.OverlapSphere(at, blastRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform)) continue;
                bool isThrower = thrower != null && hit.transform.IsChildOf(thrower.transform);
                if (!isThrower) hit.GetComponentInParent<GddHealth>()?.Damage(damage);
                var enemy = hit.GetComponentInParent<Enemy>();
                if (enemy != null) enemy.Slay();
                if (hit.attachedRigidbody != null && !hit.attachedRigidbody.isKinematic) hit.attachedRigidbody.AddExplosionForce(pushForce, at, blastRadius, 0.6f, ForceMode.Impulse);
            }
            RaiseActivated();
            onExploded.Invoke();
            StartCoroutine(ComeBack());
        }

        IEnumerator ComeBack()
        {
            var shown = new System.Collections.Generic.List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>()) if (r.enabled) { shown.Add(r); r.enabled = false; }
            var solid = new System.Collections.Generic.List<Collider>();
            foreach (var c in GetComponentsInChildren<Collider>()) if (c.enabled) { solid.Add(c); c.enabled = false; }
            if (body != null) { body.linearVelocity = Vector3.zero; body.isKinematic = true; }
            if (resetAfter <= 0) yield break;
            yield return new WaitForSeconds(resetAfter);
            if (thrower != null) foreach (var mine in GetComponentsInChildren<Collider>(true)) foreach (var theirs in thrower.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(mine, theirs, false);
            transform.SetParent(homeParent, false);
            transform.localPosition = homePos; transform.localRotation = homeRot; transform.localScale = homeScale;
            foreach (var r in shown) if (r != null) r.enabled = true;
            foreach (var c in solid) if (c != null) c.enabled = true;
            // Rest on its pedestal until picked up again
            if (body != null) { body.isKinematic = true; body.useGravity = false; }
            thrower = null;
        }
    }
}
