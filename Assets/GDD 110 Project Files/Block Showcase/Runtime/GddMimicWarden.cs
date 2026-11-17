using System.Collections;
using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Guards the edge of the map. Wander toward the edge and a warning appears; keep going and the Mimic shows up
    /// with a roar, the camera snaps to it, and it hunts the player down (one touch is fatal). After that first
    /// scare it lurks: prowling the rim, climbing over the level, stopping to stare, and now and then creeping up,
    /// circling the player with a BRAAAM, and running off again.
    /// </summary>
    [AddComponentMenu("GDD 110/Juice/Mimic Warden")]
    public class GddMimicWarden : MonoBehaviour
    {
        [Tooltip("The Mimic in the scene (switched off until it is needed).")]
        public MimicSpace.Mimic mimic;
        public float warnRadius = 61f;
        public float triggerRadius = 67f;
        public float mapRadius = 74f;
        public float chaseSpeed = 8.5f;
        public float lurkSpeed = 3.5f;
        public float bodyHeight = 1.6f;
        public AudioClip roarSound, braaamSound, warningSound;
        public GameObject roarEffect;

        enum State { Asleep, Hunting, Lurking }
        /// <summary>Survives the scene restarting after the Mimic gets the player, so the ambush only happens once per play session.</summary>
        static bool metOnce;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { metOnce = false; }
        State state;
        Transform player;
        GddActor actor;
        Vector3 velocity, goal;
        float warnCooldown, nextAction, actionUntil, huntStarted;
        string action = "roam";
        float warnAlpha;
        AudioSource voice;

        void Start()
        {
            actor = FindAnyObjectByType<GddActor>();
            player = actor != null ? actor.transform : null;
            if (mimic != null) mimic.gameObject.SetActive(false);
            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 0f; voice.playOnAwake = false;
            BuildEdgeWall();
            if (metOnce && mimic != null)
            {
                Place(RimPoint());
                mimic.gameObject.SetActive(true);
                BeginLurking();
            }
        }

        /// <summary>Invisible ring just inside the ground's edge so nobody walks off into the void.</summary>
        void BuildEdgeWall()
        {
            var ring = new GameObject("Edge Wall").transform;
            ring.SetParent(transform, false);
            int n = 72;
            float seg = 2f * Mathf.PI * mapRadius / n * 1.05f;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var w = new GameObject("Edge " + i).transform;
                w.SetParent(ring, false);
                w.localPosition = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * mapRadius + Vector3.up * 2f;
                w.localRotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
                w.gameObject.AddComponent<BoxCollider>().size = new Vector3(seg, 6f, 0.5f);
            }
        }

        float PlayerRadius => player == null ? 0 : new Vector2(player.position.x - transform.position.x, player.position.z - transform.position.z).magnitude;

        void Update()
        {
            if (player == null) return;
            float r = PlayerRadius;
            // Warning near the edge
            bool warn = r > warnRadius && state != State.Hunting;
            warnAlpha = Mathf.MoveTowards(warnAlpha, warn ? 1f : 0f, Time.deltaTime * 3f);
            if (warnAlpha > 0.01f)
            {
                string text = state == State.Asleep ? "Turn back. The edge isn't safe. Something lives out here." : "It's out here. It's watching.";
                GDD110.GddHud.Get().Subtitle(this, "WARNING", text, 999, warnAlpha, false);
            }
            if (warn && Time.time > warnCooldown) { warnCooldown = Time.time + 6f; if (warningSound != null) voice.PlayOneShot(warningSound, 0.7f); }

            if (state == State.Asleep && r > triggerRadius) StartCoroutine(Ambush());
            if (mimic == null || !mimic.gameObject.activeSelf) return;
            if (state == State.Hunting) Hunt();
            else if (state == State.Lurking) Lurk();
        }

        // ---------- the first scare ----------

        IEnumerator Ambush()
        {
            state = State.Hunting;
            metOnce = true;
            huntStarted = Time.time;
            Vector3 out_ = Flat(player.position - transform.position).normalized;
            Vector3 at = transform.position + out_ * Mathf.Min(PlayerRadius + 7f, mapRadius - 2f) + Vector3.Cross(Vector3.up, out_) * 3f;
            Place(at);
            mimic.gameObject.SetActive(true);
            velocity = Vector3.zero;
            if (roarSound != null) voice.PlayOneShot(roarSound, 1f);
            var d = GddJuiceDirector.Instance;
            if (d != null) { d.Shake(1f); d.SpawnVfx(roarEffect, mimic.transform.position, 2f); }
            GddJuiceCamera.Punch(8f, -0.3f);
            var focus = mimic.gameObject.AddComponent<GDD110.GddFocusMoment>();
            focus.focusTime = 1.8f; focus.pushIn = 2f; focus.blendSeconds = 0.35f; focus.freezePlayer = true;
            focus.enabled = true;
            yield return new WaitForSeconds(2.3f);
        }

        void Hunt()
        {
            if (Time.time - huntStarted < 2.3f) { Move(Vector3.zero, 0); return; }   // roaring
            Vector3 to = Flat(player.position - mimic.transform.position);
            if (to.magnitude < 1.8f) { Catch(); return; }
            Move(to.normalized, chaseSpeed);
            // Gave up: the player made it back toward the middle
            if (PlayerRadius < warnRadius * 0.55f && Time.time - huntStarted > 6f) BeginLurking();
        }

        void Catch()
        {
            var hp = actor != null ? actor.GetComponent<GDD110.GddHealth>() : null;
            if (hp != null) hp.Kill();
            var d = GddJuiceDirector.Instance;
            if (d != null) d.Shake(1f);
            if (roarSound != null) voice.PlayOneShot(roarSound, 0.8f);
            BeginLurking();
            Place(transform.position + Flat(mimic.transform.position - transform.position).normalized * (mapRadius - 6f));
        }

        void BeginLurking()
        {
            state = State.Lurking;
            action = "roam"; actionUntil = Time.time + 8f; nextAction = Time.time + Random.Range(12f, 25f);
            goal = RimPoint();
        }

        // ---------- lurking ----------

        void Lurk()
        {
            Vector3 me = mimic.transform.position;
            Vector3 toPlayer = Flat(player.position - me);
            float dist = toPlayer.magnitude;
            if (Time.time > nextAction && action == "roam") Choose();
            switch (action)
            {
                case "roam":
                    if (Flat(goal - me).magnitude < 3f) goal = Random.value < 0.35f ? InnerPoint() : RimPoint();   // sometimes cuts across the level, climbing over things
                    Move(Flat(goal - me).normalized, lurkSpeed);
                    break;
                case "stare":
                    Move(Vector3.zero, 0);
                    if (Time.time > actionUntil) Resume();
                    break;
                case "approach":
                    Move(toPlayer.normalized, lurkSpeed * 1.4f);
                    if (dist < 7f) { action = "orbit"; actionUntil = Time.time + Random.Range(4f, 6f); if (braaamSound != null) voice.PlayOneShot(braaamSound, 1f); GddJuiceDirector.Instance?.Shake(0.8f); GddJuiceCamera.Punch(6f, -0.2f); }
                    if (Time.time > actionUntil + 12f) Resume();
                    break;
                case "orbit":
                    Vector3 around = Vector3.Cross(Vector3.up, toPlayer.normalized);
                    Vector3 keep = toPlayer.normalized * (dist - 6f) * 0.6f;
                    Move((around + keep).normalized, lurkSpeed * 1.2f);
                    // Walking toward it scares it off sooner
                    var cc = actor != null ? actor.GetComponent<CharacterController>() : null;
                    bool approached = cc != null && Vector3.Dot(Flat(cc.velocity), -toPlayer.normalized) > 1.5f;
                    if (approached || Time.time > actionUntil) Flee();
                    break;
                case "flee":
                    Move(Flat(goal - me).normalized, chaseSpeed);
                    if (Flat(goal - me).magnitude < 4f) Resume();
                    break;
            }
        }

        void Choose()
        {
            float roll = Random.value;
            if (roll < 0.45f) { action = "stare"; actionUntil = Time.time + Random.Range(3f, 6f); }
            else if (roll < 0.75f) { action = "approach"; actionUntil = Time.time; }
            else Resume();
        }

        void Resume() { action = "roam"; goal = RimPoint(); nextAction = Time.time + Random.Range(15f, 35f); }
        void Flee() { action = "flee"; goal = RimPoint(); }

        Vector3 RimPoint()
        {
            float a = Random.Range(0, Mathf.PI * 2f);
            return transform.position + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * Random.Range(mapRadius - 14f, mapRadius - 4f);
        }

        Vector3 InnerPoint()
        {
            float a = Random.Range(0, Mathf.PI * 2f);
            return transform.position + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * Random.Range(22f, 45f);
        }

        // ---------- movement ----------

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void Move(Vector3 dir, float speed)
        {
            velocity = Vector3.Lerp(velocity, dir * speed, Time.deltaTime * 3f);
            mimic.velocity = velocity;
            var t = mimic.transform;
            Vector3 p = t.position + velocity * Time.deltaTime;
            // Ride over whatever is underneath, so it climbs over walls and buildings.
            float y = transform.position.y;
            if (Physics.Raycast(new Vector3(p.x, p.y + 6f, p.z), Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(t) && (player == null || !hit.transform.IsChildOf(player))) y = hit.point.y;
            p.y = Mathf.Lerp(t.position.y, y + bodyHeight, Time.deltaTime * 4f);
            t.position = p;
        }

        void Place(Vector3 at)
        {
            if (Physics.Raycast(at + Vector3.up * 20f, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y;
            mimic.transform.position = at + Vector3.up * bodyHeight;
        }
    }
}
