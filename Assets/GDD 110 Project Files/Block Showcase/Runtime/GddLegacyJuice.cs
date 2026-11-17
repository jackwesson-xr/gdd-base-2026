using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110.Juice
{
    /// <summary>Anything the director can build up toward or fire moments from.</summary>
    public interface IGddJuiceTarget
    {
        GddBlockInfo Info { get; }
        Transform transform { get; }
        bool isActiveAndEnabled { get; }
    }

    /// <summary>
    /// Adapters for the original course scripts used in the numbered scenes (TriggerActivatedInteraction, Enemy,
    /// Destructible, PickupManager, Spawner). Added at runtime by the director; the course scripts are not modified.
    /// </summary>
    [AddComponentMenu("")]
    public class GddLegacyHook : MonoBehaviour, IGddJuiceTarget
    {
        public enum Kind { Trigger, Portal, Collect, Hazard, Enemy, Destructible, Pickup, Spawner, Friend }
        public GddBlockInfo Info { get; private set; }
        public Kind kind;
        GddJuiceDirector director;
        GddSquash squash;
        Spawner legacySpawner;
        System.Reflection.FieldInfo spawnedField;
        readonly HashSet<GameObject> known = new HashSet<GameObject>();
        float nextTrigger;

        static readonly string[] PortalMethods = { "SwitchScreen", "NextScene", "BeginningScene", "FirstScene", "LoadNextScene" };
        static readonly string[] HazardMethods = { "RestartLevel", "RestartScene", "Respawn", "RespawnPlayer", "ResetPlayer", "Damage" };
        static readonly string[] CollectMethods = { "TriggerDestruction", "DestroyObject", "Collect" };

        /// <summary>Reads what a UnityEvent is wired to and guesses what kind of moment fits.</summary>
        public static Kind ClassifyEvent(UnityEventBase e, GameObject self)
        {
            if (e == null) return Kind.Trigger;
            bool collect = false, hazard = false;
            for (int i = 0; i < e.GetPersistentEventCount(); i++)
            {
                string m = e.GetPersistentMethodName(i);
                if (System.Array.IndexOf(PortalMethods, m) >= 0) return Kind.Portal;
                if (System.Array.IndexOf(HazardMethods, m) >= 0) hazard = true;
                var target = e.GetPersistentTarget(i);
                if (System.Array.IndexOf(CollectMethods, m) >= 0) collect = true;
                if (m == "SetActive" && target is GameObject g && g == self) collect = true;
            }
            return hazard ? Kind.Hazard : collect ? Kind.Collect : Kind.Trigger;
        }

        public void Init(Kind k, GddBlockInfo info, GddJuiceDirector owner)
        {
            kind = k; Info = info; director = owner;
            squash = gameObject.AddComponent<GddSquash>();
            switch (kind)
            {
                case Kind.Trigger:
                case Kind.Portal:
                case Kind.Collect:
                case Kind.Hazard:
                    var t = GetComponent<TriggerActivatedInteraction>();
                    if (t != null) t.whenPlayerEntersTrigger.AddListener(Fire);
                    break;
                case Kind.Enemy:
                    var enemy = GetComponent<Enemy>();
                    if (enemy != null) enemy.onPlayerCaught.AddListener(() => { if (director != null) director.HurtPlayerMoment(transform.position); squash.Kick(0.6f); });
                    break;
                case Kind.Spawner:
                    legacySpawner = GetComponent<Spawner>();
                    spawnedField = typeof(Spawner).GetField("activeInstances", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    break;
            }
        }

        Vector3 Center
        {
            get
            {
                var c = GetComponent<Collider>();
                return c != null ? c.bounds.center : transform.position;
            }
        }

        void Fire()
        {
            if (director == null || Time.unscaledTime < nextTrigger) return;
            nextTrigger = Time.unscaledTime + 0.25f;
            if (kind == Kind.Collect) director.PlayCollect(Info, Center);
            else director.PlayMoment(Info, Center);
            squash.Kick(Info.squash);
        }

        void OnTriggerEnter(Collider other)
        {
            if (kind != Kind.Pickup || director == null || !other.CompareTag("Player")) return;
            director.PlayMoment(Info, Center);
        }

        void Update()
        {
            if (kind != Kind.Spawner || legacySpawner == null || spawnedField == null || director == null) return;
            if (!(spawnedField.GetValue(legacySpawner) is List<GameObject> list)) return;
            foreach (var g in list)
            {
                if (g == null || !known.Add(g)) continue;
                if (g.GetComponent<GddSquash>() == null) g.AddComponent<GddSquash>().PopIn();
                director.PlayMoment(Info, g.transform.position, 0.5f);
            }
            known.RemoveWhere(g => g == null);
        }

        void OnDestroy()
        {
            // Scene unloads also destroy objects; only celebrate real in-game destruction.
            if (director == null || !gameObject.scene.isLoaded) return;
            if (kind == Kind.Enemy) director.PlayMoment(Info, transform.position);
            else if (kind == Kind.Destructible) director.PlayCollect(Info, transform.position);
        }
    }
}
