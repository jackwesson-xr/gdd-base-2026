using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// A weapon swing on the Use button (left click / RT) while this item is held or snapped to the player:
    /// plays a slash effect in front of them, swings the item, and hits what is in front.
    /// </summary>
    [AddComponentMenu("GDD 110/Functions/Slash Attack")]
    public class GddSlashAttack : GddEntity, IGddUsable
    {
        public GameObject slashEffect;
        public Material waveMaterial;
        public AudioClip slashSound;
        public float damage = 25f;
        public float reach = 2.2f;
        [Min(0.05f)] public float cooldown = 0.45f;
        [Tooltip("Size of the slash effect.")]
        public float effectScale = 1f;
        public Vector3 effectRotation;
        [Min(0)] public float shockwaveDistance = 5f;
        [Min(.1f)] public float shockwaveSpeed = 11f;
        public UnityEvent onSlash = new UnityEvent();

        GddCollectible snap;
        GddPickup pickup;
        float ready;
        bool swinging;

        void Awake() { snap = GetComponent<GddCollectible>(); pickup = GetComponent<GddPickup>(); }

        GddActor Wielder => snap != null && snap.Wearer != null ? snap.Wearer : pickup != null ? pickup.Holder : null;

        public void Use(GddActor actor)
        {
            if (actor && Wielder == actor && Time.time >= ready) Slash(actor);
        }

        public void Slash(GddActor actor)
        {
            ready = Time.time + cooldown;
            LastActor = actor;
            var root = actor.transform;
            Vector3 at = root.position + Vector3.up * 1.1f + root.forward * 0.8f;
            GddCombatAnimation.Play(actor, "Keeper Slash");
            if (slashSound) AudioSource.PlayClipAtPoint(slashSound, at, 0.9f);
            var struck = new HashSet<Component>();
            foreach (var hit in Physics.OverlapSphere(at + root.forward * .35f, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 delta = hit.bounds.center - root.position;
                if (Vector3.Dot(delta, root.forward) <= 0f) continue;
                bool blocked=false;
                Vector3 path=hit.bounds.center-at;
                foreach (var cover in Physics.RaycastAll(at,path.normalized,path.magnitude,~0,QueryTriggerInteraction.Ignore))
                {
                    if (cover.transform.IsChildOf(root) || cover.collider==hit ||
                        cover.collider.GetComponentInParent<GddHealth>()) continue;
                    blocked=true;
                    break;
                }
                if (!blocked) Hit(hit, root, root.forward, struck);
            }
            var fx = slashEffect ? Instantiate(slashEffect, at,
                Quaternion.LookRotation(root.forward, Vector3.up) * Quaternion.Euler(effectRotation)) : null;
            if (fx) fx.transform.localScale *= effectScale;
            if (shockwaveDistance > 0f)
            {
                GameObject wave = MakeWave(root, at);
                StartCoroutine(Shockwave(root, at, root.forward, wave ? wave : fx, struck));
                if (wave && fx) Destroy(fx, 1f);
            }
            else if (fx) Destroy(fx, 2f);
            if (!swinging) StartCoroutine(Swing());
            onSlash.Invoke();
            RaiseActivated();
        }

        GameObject MakeWave(Transform root, Vector3 at)
        {
            if (!waveMaterial) return null;
            var wave = new GameObject("Keeper blade wave", typeof(LineRenderer));
            wave.transform.SetPositionAndRotation(at, root.rotation);
            var line = wave.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = waveMaterial;
            line.widthCurve = new AnimationCurve(new Keyframe(0,0),new Keyframe(.5f,1),new Keyframe(1,0));
            line.widthMultiplier = .115f;
            line.numCapVertices = 4;
            line.positionCount = 17;
            for (int i=0; i<line.positionCount; i++)
            {
                float t=i/16f;
                line.SetPosition(i,new Vector3((t-.5f)*1.75f,(t-.5f)*.65f,Mathf.Sin(t*Mathf.PI)*.48f));
            }
            return wave;
        }

        void Hit(Collider hit, Transform owner, Vector3 direction, HashSet<Component> struck)
        {
            if (hit.transform.IsChildOf(owner) || hit.transform.IsChildOf(transform)) return;
            var health = hit.GetComponentInParent<GddHealth>();
            if (health && struck.Add(health)) health.Damage(damage);
            var enemy = hit.GetComponentInParent<Enemy>();
            if (enemy && struck.Add(enemy)) enemy.Slay();
            var body = hit.attachedRigidbody;
            if (body && !body.isKinematic && struck.Add(body))
                body.AddForce((direction + Vector3.up * .4f) * 6f, ForceMode.Impulse);
        }

        IEnumerator Shockwave(Transform owner, Vector3 point, Vector3 direction, GameObject effect,
            HashSet<Component> struck)
        {
            if (effect) Destroy(effect, Mathf.Max(1.1f, shockwaveDistance / shockwaveSpeed + .3f));
            float remaining = shockwaveDistance;
            if (Physics.Raycast(point, direction, out var wall, remaining, ~0, QueryTriggerInteraction.Ignore) &&
                !wall.transform.IsChildOf(owner) && !wall.collider.GetComponentInParent<GddHealth>() &&
                !wall.collider.GetComponentInParent<Enemy>())
                remaining = wall.distance;

            while (remaining > 0f)
            {
                yield return null;
                float step = Mathf.Min(remaining, shockwaveSpeed * Time.deltaTime);
                foreach (var hit in Physics.OverlapCapsule(point, point + direction * step, .56f, ~0,
                             QueryTriggerInteraction.Ignore))
                    if (Vector3.Dot(hit.bounds.center - owner.position, direction) > 0f)
                        Hit(hit, owner, direction, struck);
                remaining -= step;
                point += direction * step;
                if (effect) effect.transform.position = point;
            }
        }

        IEnumerator Swing()
        {
            swinging = true;
            Quaternion rest = transform.localRotation;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.28f)
            {
                float a = Mathf.Sin(t * Mathf.PI) * 75f;
                transform.localRotation = rest * Quaternion.Euler(0, 0, -a);
                yield return null;
            }
            transform.localRotation = rest;
            swinging = false;
        }
    }
}
