using System.Collections;
using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Added at runtime to each recognized toolkit block. Listens to the block's own UnityEvents
    /// (activated, collected, equipped, health changed...) and turns them into moments through the director.
    /// The toolkit never references this class, so the student project compiles without it.
    /// </summary>
    [AddComponentMenu("")]
    public class GddJuiceResponder : MonoBehaviour, IGddJuiceTarget
    {
        public GddBlockInfo Info { get; private set; }
        GddJuiceDirector director;
        GddSquash squash;
        GddPickup pickup;
        GddContactEffect contact;
        GddHealth health;
        GddSpawn spawner;
        float lastHp;
        GameObject idleFx;

        public void Init(GddBlockInfo info, GddJuiceDirector owner)
        {
            Info = info;
            director = owner;
            squash = gameObject.AddComponent<GddSquash>();
            pickup = GetComponent<GddPickup>();
            contact = GetComponent<GddContactEffect>();
            health = GetComponent<GddHealth>();
            spawner = GetComponent<GddSpawn>();

            if (info.idleFloat && pickup != null && GetComponent<GddMotion>() == null) gameObject.AddComponent<GddIdleFloat>();
            if (info.idleVfx != null)
            {
                idleFx = Instantiate(info.idleVfx, transform.position, Quaternion.identity);
                idleFx.transform.localScale *= info.idleVfxScale;
                idleFx.AddComponent<GddJuiceFollower>().target = transform;
            }

            var switchable = GetComponent<GddTriggerable>();
            var returner = GetComponent<GddPickupReturn>();
            if (returner != null)
            {
                returner.onDissolve.AddListener(() =>
                {
                    director.SpawnVfx(director.Settings.poofVfx, transform.position, 0.7f);
                    director.PlaySfx(director.Settings.tooltipClose, 0.5f, 0.7f, 0f);
                });
                returner.onReturned.AddListener(() =>
                {
                    director.SpawnVfx(director.Settings.spawnPopVfx, transform.position, 0.9f);
                    director.PlaySfx(director.Settings.tooltipOpen, 0.55f, 1.25f, 0f);
                    GddFloatingText.Spawn("back!", transform.position + Vector3.up * 0.8f, Color.Lerp(Info.accent, Color.white, 0.3f), 2.4f);
                });
            }

            if (switchable != null)
            {
                switchable.onTurnedOn.AddListener(() => { director.PlayMoment(Info, Center, 1f); squash.Kick(Info.squash); });
                switchable.onTurnedOff.AddListener(() => { director.PlayMoment(Info, Center, 0.55f); squash.Kick(Info.squash * 0.6f); });
            }
            else if (pickup != null)
            {
                pickup.onCollected.AddListener(OnCollected);
                pickup.onEquipped.AddListener(OnEquipped);
                pickup.onDropped.AddListener(OnDropped);
                foreach (var power in GetComponentsInChildren<GddPower>(true))
                {
                    var p = power;
                    p.onActivated.AddListener(() => OnPowerUsed(p));
                }
            }
            else if (health != null && GetComponent<GddFollow>() != null)
            {
                lastHp = health.Current;
                health.onHealthChanged.AddListener(OnCreatureHealth);
                health.onEmpty.AddListener(OnCreatureEmpty);
                GetComponent<GddFollow>().onActivated.AddListener(() => squash.Kick(0.55f));
            }
            else
            {
                foreach (var e in GetComponents<GddEntity>()) e.onActivated.AddListener(OnActivated);
            }
        }

        Vector3 Center => transform.position + Vector3.up * 0.3f;

        void OnActivated()
        {
            if (director == null) return;
            switch (Info.style)
            {
                case GddJuiceStyle.Teleport:
                    director.PlayMoment(Info, Center);
                    if (contact != null && contact.destination != null)
                        director.StartCoroutine(ArriveAt(contact.destination.position));
                    break;
                case GddJuiceStyle.Spawn:
                    var point = spawner != null && spawner.spawnPoint != null ? spawner.spawnPoint.position : transform.position;
                    director.PlayMoment(Info, point + Vector3.up);
                    director.StartCoroutine(PopSpawned(point + Vector3.up));
                    break;
                case GddJuiceStyle.Timer:
                    director.PlayMoment(Info, Center, 0.7f);
                    break;
                default:
                    director.PlayMoment(Info, Center);
                    break;
            }
            squash.Kick(Info.squash);
        }

        IEnumerator ArriveAt(Vector3 destination)
        {
            yield return null;
            director.SpawnVfx(Info.burstVfx, destination, Info.burstScale);
            director.SpawnVfx(director.Settings.arriveVfx, destination, 1f);
            director.PlaySfx(director.Settings.arriveSound, 0.6f, 1.15f, 0f);
            GddJuiceCamera.Punch(10f, -0.5f);
        }

        IEnumerator PopSpawned(Vector3 at)
        {
            yield return null;
            foreach (var c in Physics.OverlapSphere(at, 0.9f))
            {
                var rb = c.attachedRigidbody;
                if (rb == null || rb.gameObject == gameObject || rb.GetComponent<GddSquash>() != null) continue;
                rb.gameObject.AddComponent<GddSquash>().PopIn();
            }
        }

        void OnCollected()
        {
            if (director == null) return;
            director.PlayMoment(Info, Center);
            if (director.IsShowcase) director.StartCoroutine(RespawnCollectible());
        }

        IEnumerator RespawnCollectible()
        {
            yield return new WaitForSeconds(director.Settings.showcaseRespawnSeconds);
            if (this == null) yield break;
            var field = typeof(GddPickup).GetField("collecting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(pickup, false);
            gameObject.SetActive(true);
            squash.PopIn();
            director.SpawnVfx(director.Settings.spawnPopVfx, transform.position, 0.8f);
            director.PlaySfx(director.Settings.tooltipOpen, 0.4f, 1.3f, 0f);
        }

        void OnEquipped()
        {
            if (director == null) return;
            director.PlayMoment(Info, Center);
        }

        void OnDropped()
        {
            var returner = GetComponent<GddPickupReturn>();
            bool goingHome = returner != null && returner.rule != null && returner.rule.mode == GddDropRule.Mode.ReturnRightAway;
            if (!goingHome) squash.Kick(0.45f);
            if (director != null) director.PlaySfx(Info.sound, 0.35f, 0.8f, 0f);
        }

        void OnPowerUsed(GddPower power)
        {
            if (director == null || (pickup != null && pickup.mode == GddPickup.PickupMode.Collect)) return;
            if (pickup != null && pickup.Holder == null) return;
            var muzzle = power.muzzle != null ? power.muzzle.position : transform.position + transform.forward * 0.4f;
            director.PlaySfx(Info.useSound != null ? Info.useSound : Info.sound, Info.volume * 0.8f, Random.Range(1.0f, 1.15f), 0f);
            director.SpawnVfx(Info.useVfx != null ? Info.useVfx : Info.burstVfx, muzzle, Info.burstScale * 0.6f);
            GddJuiceCamera.Punch(1.8f, -0.12f);
            director.Shake(0.12f);
            squash.Kick(0.3f);
        }

        void OnCreatureHealth(float value)
        {
            if (director != null && value < lastHp - 0.01f)
            {
                squash.Kick(0.8f);
                director.SpawnVfx(director.Settings.hurtVfx, Center + Vector3.up * 0.3f, 0.6f);
                director.PlaySfx(director.Settings.hurtSound, 0.5f, 1.35f, 0f);
                GddFloatingText.Spawn("-" + Mathf.RoundToInt(lastHp - value), Center + Vector3.up * 0.9f, new Color(1f, 0.5f, 0.35f), 2.6f);
            }
            lastHp = value;
        }

        void OnCreatureEmpty()
        {
            if (director == null) return;
            director.PlayMoment(Info, Center, 1f);
            director.SpawnVfx(director.Settings.poofVfx, Center, 1.2f);
        }

        void OnDestroy()
        {
            if (idleFx != null) Destroy(idleFx);
        }
    }

    /// <summary>Keeps an unparented effect on its block (so the block's scale never distorts it) and hides it while the block is off.</summary>
    [AddComponentMenu("")]
    public class GddJuiceFollower : MonoBehaviour
    {
        public Transform target;
        Renderer[] renderers;
        bool visible = true;

        void Start() { renderers = GetComponentsInChildren<Renderer>(); }

        void LateUpdate()
        {
            if (target == null) { Destroy(gameObject); return; }
            bool on = target.gameObject.activeInHierarchy;
            if (on != visible)
            {
                visible = on;
                foreach (var r in renderers) if (r != null) r.enabled = on;
            }
            transform.position = target.position;
        }
    }
}
