using System.Collections.Generic;
using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Keeps the creatures placed under it inside a box so they can come right up to the fence without ever reaching
    /// the player. Works for every movement style (physics hops and rolls, sliding followers, ghosts that ignore walls).
    /// Player projectiles pass through the fence. Defeated creatures come back after a few seconds.
    /// </summary>
    [AddComponentMenu("GDD 110/Juice/Creature Pen")]
    public class GddCreaturePen : MonoBehaviour
    {
        [Tooltip("Inner size of the pen in local space. The pen's position is the floor center.")]
        public Vector3 size = new Vector3(8, 3, 8);
        [Tooltip("Creatures stay this far from the fence.")]
        public float margin = 0.45f;
        [Tooltip("Round pen: creatures stay inside a circle with a diameter of Size X.")]
        public bool round;
        [Tooltip("Flying chasers (ghosts, bats) never sink below this height above the pen floor.")]
        public float flyerHeight = 1.1f;
        public float respawnSeconds = 5f;
        [Tooltip("Fence colliders. Projectiles are allowed through these.")]
        public List<Collider> fence = new List<Collider>();

        class Slot { public Transform creature; public GameObject template; public Vector3 localPos; public float deadSince = -1; }
        readonly List<Slot> slots = new List<Slot>();
        readonly HashSet<GddProjectile> handled = new HashSet<GddProjectile>();
        Transform templates;
        float nextScan;

        void Awake()
        {
            templates = new GameObject("[Templates]").transform;
            templates.SetParent(transform, false);
            templates.gameObject.SetActive(false);
            foreach (Transform child in transform)
            {
                if (child == templates || child.GetComponentInChildren<Rigidbody>() == null && child.GetComponent<GddEntity>() == null && child.GetComponent<Enemy>() == null) continue;
                if (fence.Exists(f => f != null && f.transform == child)) continue;
                bool wasActive = child.gameObject.activeSelf;
                child.gameObject.SetActive(false);
                var copy = Instantiate(child.gameObject, templates);
                copy.name = child.name;
                child.gameObject.SetActive(wasActive);
                slots.Add(new Slot { creature = child, template = copy, localPos = child.localPosition });
            }
        }

        void FixedUpdate() { Clamp(); }
        void LateUpdate() { Clamp(); }

        void Clamp()
        {
            Vector3 half = size * 0.5f - new Vector3(margin, 0, margin);
            foreach (var s in slots)
            {
                if (s.creature == null) continue;
                Vector3 local = transform.InverseTransformPoint(s.creature.position);
                var chaser = s.creature.GetComponent<Enemy>();
                float floor = chaser != null && chaser.movementType == Enemy.MovementType.GhostChase ? flyerHeight : 0.05f;
                Vector3 c = new Vector3(Mathf.Clamp(local.x, -half.x, half.x), Mathf.Clamp(local.y, floor, size.y), Mathf.Clamp(local.z, -half.z, half.z));
                if (round)
                {
                    var flat = new Vector2(local.x, local.z);
                    float r = size.x * 0.5f - margin;
                    if (flat.magnitude > r) flat = flat.normalized * r;
                    c.x = flat.x; c.z = flat.y;
                }
                if (c == local) continue;
                var body = s.creature.GetComponent<Rigidbody>();
                Vector3 world = transform.TransformPoint(c);
                if (body != null && !body.isKinematic)
                {
                    body.position = world;
                    Vector3 v = transform.InverseTransformDirection(body.linearVelocity);
                    if (c.x != local.x) v.x = 0;
                    if (c.z != local.z) v.z = 0;
                    if (c.y != local.y && v.y > 0) v.y = 0;
                    body.linearVelocity = transform.TransformDirection(v);
                }
                s.creature.position = world;
            }
        }

        void Update()
        {
            if (Time.time >= nextScan)
            {
                nextScan = Time.time + 0.15f;
                foreach (var p in FindObjectsByType<GddProjectile>())
                {
                    if (!handled.Add(p)) continue;
                    foreach (var pc in p.GetComponentsInChildren<Collider>())
                        foreach (var f in fence) if (f != null) Physics.IgnoreCollision(pc, f);
                }
                handled.RemoveWhere(p => p == null);
            }

            foreach (var s in slots)
            {
                if (s.creature != null) continue;
                if (s.deadSince < 0) { s.deadSince = Time.time; continue; }
                if (Time.time - s.deadSince < respawnSeconds) continue;
                var go = Instantiate(s.template, transform);
                go.name = s.template.name;
                go.transform.localPosition = s.localPos;
                go.SetActive(true);
                s.creature = go.transform;
                s.deadSince = -1;
                var d = GddJuiceDirector.Instance;
                if (d != null)
                {
                    d.Attach(go);
                    d.SpawnVfx(d.Settings.spawnPopVfx, go.transform.position, 1f);
                }
                var squash = go.GetComponent<GddSquash>();
                if (squash == null) squash = go.AddComponent<GddSquash>();
                squash.PopIn();
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.6f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (round) for (int i = 0; i < 32; i++) { float a = i / 32f * Mathf.PI * 2, b = (i + 1) / 32f * Mathf.PI * 2, r = size.x * 0.5f; Gizmos.DrawLine(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), new Vector3(Mathf.Cos(b) * r, 0, Mathf.Sin(b) * r)); }
            else Gizmos.DrawWireCube(new Vector3(0, size.y * 0.5f, 0), size);
        }
    }
}
