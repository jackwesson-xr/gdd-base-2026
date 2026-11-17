using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// Protection while worn. Put it on a Snap Collectible (or a carried Pickup). While the player has it on,
    /// enemies can't hurt them: they bounce off with a shield effect instead, and Armor Gates let them through.
    /// </summary>
    [AddComponentMenu("GDD 110/Functions/Armor")]
    public class GddArmor : GddEntity
    {
        [Tooltip("How hard enemies get knocked back.")]
        [Range(0,1)] public float damageMultiplier;
        public float bounceForce = 7f;
        [Tooltip("Effect shown on the player when something bounces off.")]
        public GameObject shieldEffect;
        public AudioClip shieldSound;
        public UnityEvent onBlocked = new UnityEvent();

        static readonly List<GddArmor> all = new List<GddArmor>();
        GddCollectible snap;
        GddPickup pickup;
        float nextEffect;

        void Awake() { snap = GetComponent<GddCollectible>(); pickup = GetComponent<GddPickup>(); }
        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        GddActor Wearer => snap != null ? snap.Wearer : pickup != null ? pickup.Holder : null;

        /// <summary>The armor this player is wearing, or null.</summary>
        public static GddArmor On(GddActor actor)
        {
            if (actor == null) return null;
            GddArmor best = null;
            foreach (var a in all)
                if (a && a.Wearer == actor && (!best || a.damageMultiplier < best.damageMultiplier)) best = a;
            return best;
        }

        public static bool Protects(GddActor actor) => On(actor) != null;

        /// <summary>Knock an attacker away from the wearer and show the shield. Returns true if the hit was blocked.</summary>
        public static bool Block(GddActor actor, GameObject attacker)
        {
            var armor = On(actor);
            if (armor == null || armor.damageMultiplier > 0f) return false;
            armor.Deflect(actor, attacker);
            return true;
        }

        void Deflect(GddActor actor, GameObject attacker)
        {
            Vector3 away = attacker.transform.position - actor.transform.position;
            away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = actor.transform.forward;
            away = away.normalized;
            var body = attacker.GetComponentInParent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.AddForce((away + Vector3.up * 0.6f) * bounceForce, ForceMode.Impulse);
            }
            else attacker.transform.position += away * 1.2f;
            if (Time.time < nextEffect) return;
            nextEffect = Time.time + 0.25f;
            if (shieldEffect != null)
            {
                Vector3 at = actor.transform.position + Vector3.up * 1f + away * 0.4f;
                var fx = Instantiate(shieldEffect, at, Quaternion.LookRotation(away));
                fx.transform.localScale *= 0.6f;
                Destroy(fx, 2f);
            }
            if (shieldSound != null) AudioSource.PlayClipAtPoint(shieldSound, actor.transform.position, 0.8f);
            LastActor = actor;
            onBlocked.Invoke();
            RaiseActivated();
        }
    }

    /// <summary>A wall or door the player can only walk through while wearing Armor. Everyone else bumps into it.</summary>
    [AddComponentMenu("GDD 110/Functions/Armor Gate")]
    public class GddArmorGate : MonoBehaviour
    {
        Collider[] walls;
        GddActor actor;
        bool open;

        void Start() { walls = GetComponentsInChildren<Collider>(); actor = FindAnyObjectByType<GddActor>(); }

        void Update()
        {
            if (actor == null) { actor = FindAnyObjectByType<GddActor>(); if (actor == null) return; }
            bool should = GddArmor.Protects(actor);
            if (should == open) return;
            open = should;
            foreach (var mine in actor.GetComponentsInChildren<Collider>())
                foreach (var w in walls) if (w != null && !w.isTrigger) Physics.IgnoreCollision(mine, w, open);
        }
    }
}
