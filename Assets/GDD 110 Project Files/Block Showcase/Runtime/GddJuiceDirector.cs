using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GDD110.Juice
{
    /// <summary>
    /// Boots itself into any scene that has a camera (no scene edits needed), then:
    /// layers a post-processing look on top, wires camera juice into every Cinemachine camera,
    /// attaches a GddJuiceResponder to every toolkit block it recognizes, adds guide tooltips in the showcase,
    /// and drives the approach build-up for portals and teleporters.
    /// Feel (MMF_Player) handles freeze frames, slow motion, screen flashes and URP vignette / chromatic aberration.
    /// Turn it off with Tools > GDD 110 > Juice > Toggle Juice.
    /// </summary>
    [AddComponentMenu("")]
    [DefaultExecutionOrder(-40)]
    public class GddJuiceDirector : MonoBehaviour
    {
        public static GddJuiceDirector Instance { get; private set; }
        public GddJuiceSettings Settings { get; private set; }
        public Transform Player => actor != null ? actor.transform : null;
        public bool IsShowcase { get; private set; }

        static bool hooked;
        bool arrived;
        GddActor actor;
        GddHealth playerHealth;
        float lastHealth;
        int lastScore;
        MMTimeManager timeManager;
        Volume buildVolume;
        CinemachineImpulseSource impulse;
        AudioSource[] voices;
        int nextVoice;
        AudioSource hum;
        AudioSource[] worldVoices;
        int nextWorldVoice;
        /// <summary>World sounds are fully audible within this distance...</summary>
        public const float WorldNear = 2f;
        /// <summary>...and silent beyond this one.</summary>
        public const float WorldFar = 8f;
        float buildLevel, appliedNormalTime = 1f;
        readonly Dictionary<GddBlockInfo, MMF_Player> moments = new Dictionary<GddBlockInfo, MMF_Player>();
        readonly List<IGddJuiceTarget> targets = new List<IGddJuiceTarget>();
        MMF_Player hurtPlayer, healPlayer, arrivePlayer, deathPlayer;
        float comboUntil;
        int combo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!hooked) { SceneManager.sceneLoaded += OnSceneLoaded; hooked = true; }
            GddJuiceCamera.Reset();
            Spawn(SceneManager.GetActiveScene(), false);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) Spawn(scene, true);
        }

        static void Spawn(Scene scene, bool arrivedFromAnotherScene)
        {
            var settings = GddJuiceSettings.Load();
            if (settings == null || !settings.juiceEnabled || !Application.isPlaying) return;
            if (Instance != null && Instance.gameObject.scene == scene) return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent<GddJuiceDirector>(out var existing))
                {
                    Instance = existing;
                    return;
                }
            var go = new GameObject("[GDD Juice]");
            SceneManager.MoveGameObjectToScene(go, scene);
            var d = go.AddComponent<GddJuiceDirector>();
            d.Settings = settings;
            d.arrived = arrivedFromAnotherScene;
        }

        void Awake() { Instance = this; }
        void OnDestroy() { GddEvents.Spawned -= OnSpawned; if (Instance == this) Instance = null; GddJuiceCamera.Reset(); }

        /// <summary>Dropped and spawned items pop in with a puff and get their own juice.</summary>
        void OnSpawned(GameObject go)
        {
            if (go == null) return;
            Attach(go);
            SpawnVfx(Settings.spawnPopVfx, go.transform.position, 0.8f);
            var squash = go.GetComponent<GddSquash>();
            if (squash == null) squash = go.AddComponent<GddSquash>();
            squash.PopIn();
        }

        void Start()
        {
            if (Settings == null) Settings = GddJuiceSettings.Load();
            IsShowcase = FindAnyObjectByType<GddJuiceShowcase>() != null;
            timeManager = gameObject.AddComponent<MMTimeManager>();
            GddGameRuntime.ExternalTimeControl = true;   // slow-motion cheat goes through Feel's time manager
            BuildPost();
            BuildFlash();
            BuildAudio();
            HookCameras();

            actor = FindAnyObjectByType<GddActor>();
            if (actor != null)
            {
                playerHealth = actor.GetComponent<GddHealth>();
                if (playerHealth != null)
                {
                    lastHealth = playerHealth.Current;
                    playerHealth.onHealthChanged.AddListener(OnPlayerHealth);
                    playerHealth.onEmpty.AddListener(OnPlayerDied);
                }
                actor.onScoreChanged.AddListener(OnScore);
            }

            hurtPlayer = BuildPlayer("Player Hurt", 0.07f, 0.35f, new Color(1f, 0.2f, 0.15f), 0.6f, 0.45f, new Color(0.6f, 0f, 0f), 1f, 0f);
            healPlayer = BuildPlayer("Player Heal", 0f, 0.2f, new Color(0.4f, 1f, 0.6f), 0.15f, 0f, Color.black, 1f, 0f);
            arrivePlayer = BuildPlayer("Arrive", 0f, 0.9f, Color.white, 0.5f, 0f, Color.black, 0.35f, 0.5f);
            deathPlayer = BuildPlayer("Player Down", 0.15f, 0.6f, new Color(1f, 0.15f, 0.1f), 0.9f, 0.6f, new Color(0.5f, 0f, 0f), 0.15f, 1.2f);

            AttachBlocks();
            AttachLegacy();
            GddEvents.Spawned += OnSpawned;
            if (IsShowcase || Settings.showLinksEverywhere)
                new GameObject("Juice Links").AddComponent<GddLinkVisualizer>().Init(this);
            if (Settings.showActionPrompts && actor != null && actor.Interactor != null)
            {
                var hud = new GameObject("Juice Prompts");
                hud.transform.SetParent(transform, false);
                hud.AddComponent<GddActionPrompts>().Init(this, actor.Interactor);
            }
            if (arrived && actor != null) StartCoroutine(PlayArrival());
        }

        IEnumerator PlayArrival()
        {
            // The flash component caches its canvas references in Start.
            yield return null;
            Arrive();
        }

        // ---------- setup ----------

        void BuildPost()
        {
            if (Settings.postProfile != null)
            {
                var pgo = new GameObject("Juice Post");
                pgo.transform.SetParent(transform, false);
                var v = pgo.AddComponent<Volume>();
                v.isGlobal = true;
                v.priority = 40;
                v.profile = Instantiate(Settings.postProfile);
                pgo.AddComponent<MMVignetteShaker_URP>();
                pgo.AddComponent<MMChromaticAberrationShaker_URP>();
            }
            if (Settings.buildUpProfile != null)
            {
                var bgo = new GameObject("Juice Build-up");
                bgo.transform.SetParent(transform, false);
                buildVolume = bgo.AddComponent<Volume>();
                buildVolume.isGlobal = true;
                buildVolume.priority = 41;
                buildVolume.weight = 0;
                buildVolume.profile = Instantiate(Settings.buildUpProfile);
            }
        }

        void BuildFlash()
        {
            var cgo = new GameObject("Juice Flash", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var fgo = new GameObject("Flash", typeof(RectTransform));
            fgo.transform.SetParent(cgo.transform, false);
            var rt = (RectTransform)fgo.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var image = fgo.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            var group = fgo.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
            fgo.AddComponent<MMFlash>();
        }

        void BuildAudio()
        {
            var ago = new GameObject("Juice Audio");
            ago.transform.SetParent(transform, false);
            voices = new AudioSource[10];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = ago.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
                voices[i].ignoreListenerPause = true;
            }
            worldVoices = new AudioSource[12];
            for (int i = 0; i < worldVoices.Length; i++)
            {
                var wgo = new GameObject("World Voice " + i);
                wgo.transform.SetParent(ago.transform, false);
                var w = wgo.AddComponent<AudioSource>();
                w.playOnAwake = false;
                w.spatialBlend = 1f;
                w.rolloffMode = AudioRolloffMode.Linear;
                w.minDistance = WorldNear;
                w.maxDistance = WorldFar;
                w.dopplerLevel = 0f;
                worldVoices[i] = w;
            }
            hum = ago.AddComponent<AudioSource>();
            hum.loop = true; hum.playOnAwake = false; hum.volume = 0; hum.spatialBlend = 0;
        }

        void HookCameras()
        {
            var vcams = FindObjectsByType<CinemachineVirtualCameraBase>(FindObjectsInactive.Include);
            foreach (var vc in vcams)
            {
                if (vc.GetComponent<GddJuiceCameraExtension>() == null) vc.gameObject.AddComponent<GddJuiceCameraExtension>();
                if (vc.GetComponent<CinemachineImpulseListener>() == null) vc.gameObject.AddComponent<CinemachineImpulseListener>();
            }
            var main = Camera.main;
            if (main != null && main.GetComponent<CinemachineBrain>() == null && main.GetComponent<GddJuicePlainCamera>() == null)
                main.gameObject.AddComponent<GddJuicePlainCamera>();

            impulse = gameObject.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseDuration = 0.22f;
        }

        readonly HashSet<GddBlockInfo> tipped = new HashSet<GddBlockInfo>();

        /// <summary>Objects stored as respawn templates by a creature pen. Never juiced directly.</summary>
        static bool IsTemplate(GameObject go)
        {
            for (var t = go.transform; t != null; t = t.parent) if (t.name == "[Templates]") return true;
            return false;
        }

        void AttachBlocks()
        {
            var seen = new HashSet<GameObject>();
            foreach (var e in FindObjectsByType<GddEntity>(FindObjectsInactive.Include))
                if (seen.Add(e.gameObject)) AttachBlock(e.gameObject);
        }

        void AttachBlock(GameObject go)
        {
            if (go.GetComponent<GddJuiceResponder>() != null || IsTemplate(go)) return;
            if (go.GetComponentInParent<GddActor>(true) != null) return;
            var info = Settings.Find(GddBlockClassifier.Classify(go));
            if (info == null) return;
            var r = go.AddComponent<GddJuiceResponder>();
            r.Init(info, this);
            targets.Add(r);
            var follow = go.GetComponent<GddFollow>();
            if (follow != null) go.AddComponent<GddCreatureMood>().Init(this, follow.contactDamage > 0);
            Tooltip(info, go.transform);
        }

        void Tooltip(GddBlockInfo info, Transform target)
        {
            if (!(IsShowcase || Settings.tooltipsEverywhere) || !tipped.Add(info)) return;
            var tgo = new GameObject("Guide");
            SceneManager.MoveGameObjectToScene(tgo, gameObject.scene);
            tgo.AddComponent<GddBlockTooltip>().Init(info, target, Settings, Settings.blocks.IndexOf(info) + 1);
        }

        /// <summary>Juice an object created after the scene started (respawned creatures, spawned blocks).</summary>
        public void Attach(GameObject go)
        {
            if (go == null) return;
            if (go.GetComponent<GddEntity>() != null) AttachBlock(go);
            var enemy = go.GetComponent<Enemy>();
            if (enemy != null) HookLegacy(go, GddLegacyHook.Kind.Enemy, EnemyBlock(enemy));
            if (go.GetComponent<CompanionCreature>() != null) HookLegacy(go, GddLegacyHook.Kind.Friend, "Hopper Friend");
        }

        static string EnemyBlock(Enemy e)
        {
            switch (e.movementType)
            {
                case Enemy.MovementType.Jump: return "Hopper Enemy";
                case Enemy.MovementType.GhostChase: return "Ghost Enemy";
                default: return "Roller Enemy";
            }
        }

        void HookLegacy(GameObject go, GddLegacyHook.Kind kind, string block)
        {
            if (IsTemplate(go)) return;
            var info = Settings.Find(block);
            if (info == null && kind == GddLegacyHook.Kind.Enemy) info = Settings.Find("Enemy");
            if (info == null || go.GetComponent<GddLegacyHook>() != null || go.GetComponent<GddJuiceResponder>() != null) return;
            var h = go.AddComponent<GddLegacyHook>();
            h.Init(kind, info, this);
            targets.Add(h);
            if (kind == GddLegacyHook.Kind.Enemy) go.AddComponent<GddCreatureMood>().Init(this, true);
            if (kind == GddLegacyHook.Kind.Friend) go.AddComponent<GddCreatureMood>().Init(this, false);
            if (kind == GddLegacyHook.Kind.Enemy || kind == GddLegacyHook.Kind.Friend) Tooltip(info, go.transform);
        }

        void AttachLegacy()
        {
            foreach (var t in FindObjectsByType<TriggerActivatedInteraction>(FindObjectsInactive.Include))
            {
                var kind = GddLegacyHook.ClassifyEvent(t.whenPlayerEntersTrigger, t.gameObject);
                string block = kind == GddLegacyHook.Kind.Portal ? "Next Scene Portal" : kind == GddLegacyHook.Kind.Collect ? "Score Collectible" : kind == GddLegacyHook.Kind.Hazard ? "Hazard" : "Event Trigger";
                HookLegacy(t.gameObject, kind, block);
            }
            foreach (var e in FindObjectsByType<Enemy>(FindObjectsInactive.Include)) HookLegacy(e.gameObject, GddLegacyHook.Kind.Enemy, EnemyBlock(e));
            foreach (var c in FindObjectsByType<CompanionCreature>(FindObjectsInactive.Include)) HookLegacy(c.gameObject, GddLegacyHook.Kind.Friend, "Hopper Friend");
            foreach (var d in FindObjectsByType<Destructible>(FindObjectsInactive.Include)) HookLegacy(d.gameObject, GddLegacyHook.Kind.Destructible, "Score Collectible");
            foreach (var p in FindObjectsByType<PickupManager>(FindObjectsInactive.Include)) HookLegacy(p.gameObject, GddLegacyHook.Kind.Pickup, "Hand Pickup");
            foreach (var s in FindObjectsByType<Spawner>(FindObjectsInactive.Include)) HookLegacy(s.gameObject, GddLegacyHook.Kind.Spawner, "Spawner");
        }

        void OnPlayerDied()
        {
            deathPlayer.PlayFeedbacks();
            PlaySfx(Settings.deathSound, 0.9f, 0.9f, 0f);
            Shake(1f);
            GddJuiceCamera.Punch(-10f, 0.8f);
            if (actor != null)
            {
                SpawnVfx(Settings.deathVfx, actor.transform.position + Vector3.up, 1.3f);
                GddFloatingText.Spawn("K.O.", actor.transform.position + Vector3.up * 2.4f, new Color(1f, 0.3f, 0.25f), 7f);
            }
        }

        MMF_Player BuildPlayer(string label, float freeze, float flash, Color flashColor, float chroma, float vignette, Color vignetteColor, float slowMo, float slowDuration)
        {
            var go = new GameObject("Moment - " + label);
            go.transform.SetParent(transform, false);
            var p = go.AddComponent<MMF_Player>();
            p.ForceTimescaleMode = true;
            p.ForcedTimescaleMode = TimescaleModes.Unscaled;
            p.CanPlayWhileAlreadyPlaying = true;
            if (freeze > 0)
            {
                var f = (MMF_FreezeFrame)p.AddFeedback(typeof(MMF_FreezeFrame), true);
                f.FreezeFrameDuration = freeze;
            }
            if (slowMo < 0.99f)
            {
                var t = (MMF_TimescaleModifier)p.AddFeedback(typeof(MMF_TimescaleModifier), true);
                t.Mode = MMF_TimescaleModifier.Modes.Shake;
                t.TimeScale = slowMo;
                t.TimeScaleDuration = slowDuration;
                t.TimeScaleLerp = true;
                t.TimeScaleLerpSpeed = 8f;
                if (t.Timing != null) t.Timing.InitialDelay = freeze;
            }
            if (flash > 0)
            {
                var f = (MMF_Flash)p.AddFeedback(typeof(MMF_Flash), true);
                f.FlashColor = flashColor;
                f.FlashAlpha = flash;
                f.FlashDuration = 0.16f;
            }
            if (chroma > 0)
            {
                var c = (MMF_ChromaticAberration_URP)p.AddFeedback(typeof(MMF_ChromaticAberration_URP), true);
                c.Duration = 0.4f;
                c.RemapIntensityOne = chroma;
            }
            if (vignette > 0)
            {
                var v = (MMF_Vignette_URP)p.AddFeedback(typeof(MMF_Vignette_URP), true);
                v.Duration = 0.5f;
                v.RemapIntensityOne = vignette;
                v.InterpolateColor = true;
                v.TargetColor = vignetteColor;
            }
            p.Initialization(true);
            return p;
        }

        // ---------- moments ----------

        /// <summary>The big beat for a block: Feel feedbacks, sound, VFX, camera punch and shake, floating text.</summary>
        public void PlayMoment(GddBlockInfo info, Vector3 position, float intensity = 1f)
        {
            if (info == null) return;
            // Moments fade with distance: full strength up close, sound and sparks only past ~12 m, nothing past ~28 m.
            float near = actor != null ? Mathf.Clamp01(1f - (Vector3.Distance(actor.transform.position, position) - 7f) / 21f) : 1f;
            if (near <= 0f) return;
            bool big = near > 0.75f;
            if (big)
            {
                if (!moments.TryGetValue(info, out var p))
                {
                    p = BuildPlayer(info.blockName, info.freezeFrame, info.flash, Color.Lerp(Color.white, info.accent, 0.35f), info.chromatic, 0f, Color.black, info.slowMo, info.slowMoDuration);
                    moments[info] = p;
                }
                p.PlayFeedbacks(position, intensity);
                GddJuiceCamera.Punch(info.fovPunch * intensity, info.cameraPush * intensity);
                Shake(info.shake * intensity);
            }
            PlaySfxAt(info.sound, info.volume * Mathf.Clamp01(intensity + 0.2f), Random.Range(info.pitchRange.x, info.pitchRange.y), position);
            SpawnVfx(info.burstVfx, position, info.burstScale * Mathf.Lerp(0.7f, 1f, intensity));
            if (!big) return;
            if (!string.IsNullOrEmpty(info.floatingText))
                GddFloatingText.Spawn(info.floatingText, position + Vector3.up * 1.1f, Color.Lerp(info.accent, Color.white, 0.3f));
        }

        public void Shake(float amount)
        {
            if (impulse == null || amount <= 0.001f) return;
            var dir = Random.insideUnitSphere; dir.y = Mathf.Abs(dir.y) + 0.4f;
            impulse.GenerateImpulseWithVelocity(dir.normalized * amount * 0.45f);
        }

        public void PlaySfx(AudioClip clip, float volume, float pitch, float spatial)
        {
            if (clip == null || voices == null) return;
            var v = voices[nextVoice];
            if (v == null || !v.isActiveAndEnabled) return;   // juice audio switched off
            nextVoice = (nextVoice + 1) % voices.Length;
            v.pitch = pitch;
            v.spatialBlend = spatial;
            v.PlayOneShot(clip, volume);
        }

        /// <summary>A tightly contained 3D sound at a world position: only heard when standing next to it.</summary>
        public void PlaySfxAt(AudioClip clip, float volume, float pitch, Vector3 position)
        {
            if (clip == null || worldVoices == null) return;
            var v = worldVoices[nextWorldVoice];
            nextWorldVoice = (nextWorldVoice + 1) % worldVoices.Length;
            if (v == null || !v.isActiveAndEnabled) return;
            v.transform.position = position;
            v.pitch = pitch;
            v.PlayOneShot(clip, volume);
        }

        public GameObject SpawnVfx(GameObject prefab, Vector3 position, float scale = 1f)
        {
            if (prefab == null) return null;
            var g = Instantiate(prefab, position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(g, gameObject.scene);
            g.transform.localScale *= scale;
            float life = 0.5f;
            foreach (var ps in g.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.loop = false;
                life = Mathf.Max(life, main.duration + main.startLifetime.constantMax);
            }
            if (g.GetComponentInChildren<UnityEngine.VFX.VisualEffect>() != null) life = Mathf.Max(life, 3f);   // VFX Graph effects (LevelUp pack)
            Destroy(g, Mathf.Clamp(life, 1f, 5f));
            return g;
        }

        void Arrive()
        {
            arrivePlayer.PlayFeedbacks();
            GddJuiceCamera.Punch(14f, -0.6f);
            PlaySfx(Settings.arriveSound, 0.7f, 1f, 0f);
            if (actor != null) SpawnVfx(Settings.arriveVfx, actor.transform.position, 1.2f);
        }

        void OnPlayerHealth(float value)
        {
            if (value < lastHealth - 0.01f)
            {
                HurtPlayerMoment(actor != null ? actor.transform.position : transform.position);
                if (actor != null)
                {
                    GddFloatingText.Spawn("-" + Mathf.RoundToInt(lastHealth - value), actor.transform.position + Vector3.up * 2.1f, new Color(1f, 0.35f, 0.3f));
                }
            }
            else if (value > lastHealth + 0.01f)
            {
                healPlayer.PlayFeedbacks();
                PlaySfx(Settings.healSound, 0.7f, 1.1f, 0f);
                if (actor != null) GddFloatingText.Spawn("+" + Mathf.RoundToInt(value - lastHealth) + " HP", actor.transform.position + Vector3.up * 2.1f, new Color(0.45f, 1f, 0.55f));
            }
            lastHealth = value;
        }

        /// <summary>Red flash, vignette, freeze, shake and a punch sound. Used for hazards and enemy contact.</summary>
        public void HurtPlayerMoment(Vector3 position)
        {
            hurtPlayer.PlayFeedbacks();
            PlaySfx(Settings.hurtSound, 0.8f, Random.Range(0.9f, 1.05f), 0f);
            Shake(0.7f);
            GddJuiceCamera.Punch(5f, -0.35f);
            var at = actor != null ? actor.transform.position : position;
            SpawnVfx(Settings.hurtVfx, at + Vector3.up, 0.8f);
        }

        /// <summary>Collect moment plus the rising combo coin sound.</summary>
        public void PlayCollect(GddBlockInfo info, Vector3 position)
        {
            PlayMoment(info, position);
            ComboSound();
        }

        void ComboSound()
        {
            combo = Time.unscaledTime < comboUntil ? combo + 1 : 0;
            comboUntil = Time.unscaledTime + 1.6f;
            float pitch = Mathf.Pow(2f, Mathf.Min(combo, 12) / 12f);
            PlaySfx(Settings.scoreSound, 0.7f, pitch, 0f);
            GddJuiceCamera.Punch(-2.5f - combo * 0.3f, 0.12f);
        }

        void OnScore(int total)
        {
            int delta = total - lastScore;
            lastScore = total;
            if (delta <= 0) return;
            ComboSound();
            if (actor != null)
                GddFloatingText.Spawn("+" + delta + (combo > 0 ? "  x" + (combo + 1) : ""), actor.transform.position + Vector3.up * 2.2f, new Color(1f, 0.86f, 0.25f), 3.6f + combo * 0.25f);
        }

        // ---------- build-up ----------

        void Update()
        {
            float best = 0f;
            GddBlockInfo bestInfo = null;
            if (actor != null)
            {
                Vector3 p = actor.transform.position;
                for (int i = targets.Count - 1; i >= 0; i--)
                {
                    var r = targets[i];
                    if (r == null || (r as Object) == null) { targets.RemoveAt(i); continue; }
                    if (!r.Info.buildUpOnApproach || !r.isActiveAndEnabled) continue;
                    float t = 1f - Vector3.Distance(p, r.transform.position) / Mathf.Max(0.5f, r.Info.buildUpRadius);
                    if (t > best) { best = t; bestInfo = r.Info; }
                }
            }
            buildLevel = Mathf.MoveTowards(buildLevel, Mathf.Clamp01(best), Time.unscaledDeltaTime * 2.2f);
            GddJuiceCamera.BuildUp = buildLevel;
            if (buildVolume != null) buildVolume.weight = buildLevel;

            float normal = Mathf.Lerp(1f, 0.7f, buildLevel * buildLevel) * GddGameRuntime.TimeScaleMultiplier;
            if (timeManager != null && Mathf.Abs(normal - appliedNormalTime) > 0.01f)
            {
                timeManager.SetTimeScaleTo(normal);
                appliedNormalTime = normal;
            }

            if (bestInfo != null && bestInfo.buildUpLoop != null && hum.clip != bestInfo.buildUpLoop) { hum.clip = bestInfo.buildUpLoop; hum.Play(); }
            hum.volume = buildLevel * 0.6f;
            hum.pitch = 0.7f + buildLevel * 0.7f;
            if (buildLevel <= 0.001f && hum.isPlaying) hum.Stop();
            else if (buildLevel > 0.001f && hum.clip != null && !hum.isPlaying) hum.Play();
        }

        void OnDisable()
        {
            if (timeManager != null && appliedNormalTime != 1f) timeManager.SetTimeScaleTo(1f);
        }
    }
}
