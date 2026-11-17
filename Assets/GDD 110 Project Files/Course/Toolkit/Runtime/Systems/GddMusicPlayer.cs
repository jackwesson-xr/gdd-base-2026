using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GDD110
{
    /// <summary>
    /// Plays each level's music from the Music Library, crossfading between levels, with stacked overrides
    /// (Music Override), boss music that fades in near enemies and fully ducks the level music, and automatic
    /// routing of every other sound to the Effects group (which ducks the music in the mixer).
    /// Lives on the persistent [GDD Game Systems] object.
    /// </summary>
    [AddComponentMenu("")]
    public class GddMusicPlayer : MonoBehaviour
    {
        public static GddMusicPlayer Instance { get; private set; }
        public float BossWeight { get; private set; }
        /// <summary>The source playing the level music right now (Synced Radio locks to it).</summary>
        public AudioSource CurrentSource => current;
        public float StoryDuck => storyDuck;
        public UnityEngine.Audio.AudioMixerGroup MusicGroup => lib != null ? lib.musicGroup : null;

        GddMusicLibrary lib;
        GddFourthBell story;
        float storyDuck = 1f;
        AudioSource a, b, boss, current, fading;
        float fadeT = 1f;
        float nextScan, nextRoute, bossTarget;
        static readonly List<GddMusicOverride> overrides = new List<GddMusicOverride>();

        public void Init(GddMusicLibrary library)
        {
            Instance = this;
            lib = library;
            a = Source("Music A", lib.musicGroup);
            b = Source("Music B", lib.musicGroup);
            boss = Source("Boss Music", lib.bossGroup);
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; if (Instance == this) Instance = null; }

        AudioSource Source(string name, UnityEngine.Audio.AudioMixerGroup group)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.outputAudioMixerGroup = group;
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;
            s.volume = 0f;
            s.ignoreListenerPause = true;
            return s;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            overrides.Clear();
            story = FindAnyObjectByType<GddFourthBell>();
            Refresh();
            if (lib.bossTrack != null && boss.clip != lib.bossTrack) { boss.clip = lib.bossTrack; boss.Play(); }
            BossWeight = 0f;
            nextRoute = 0f;
        }

        public static void Push(GddMusicOverride o)
        {
            if (o == null || overrides.Contains(o)) return;
            overrides.Add(o);
            if (Instance != null) Instance.Refresh();
        }

        public static void Pop(GddMusicOverride o)
        {
            if (!overrides.Remove(o)) return;
            if (Instance != null) Instance.Refresh();
        }

        void Refresh()
        {
            AudioClip clip;
            bool loop = true;
            overrides.RemoveAll(o => o == null);
            if (overrides.Count > 0) { var o = overrides[overrides.Count - 1]; clip = o.track; loop = o.loop; }
            else
            {
                var entry = lib.Find(SceneManager.GetActiveScene().name);
                clip = entry != null && entry.silence ? null : entry != null && entry.track != null ? entry.track : lib.defaultTrack;
                if (entry != null) loop = entry.loop;
            }
            if (story && !story.MusicStarted) clip = null;
            MixTo(clip, loop);
        }

        public void FadeOutForIntro() => MixTo(null, false);

        void MixTo(AudioClip clip, bool loop)
        {
            if (current != null && current.clip == clip && (clip == null || current.isPlaying)) return;
            var next = current == a ? b : a;
            next.clip = clip;
            next.loop = loop;
            next.volume = 0f;
            if (clip != null) next.Play(); else next.Stop();
            if (fading != null && fading != current && fading != next) fading.Stop();
            fading = current;
            current = next;
            fadeT = 0f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fadeT = Mathf.Min(1f, fadeT + dt / Mathf.Max(0.01f, lib.crossfadeSeconds));

            if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + 0.25f; bossTarget = BossTarget(); }
            BossWeight = Mathf.MoveTowards(BossWeight, bossTarget, dt * 1.2f);

            float target = story ? Mathf.Min(GddGameRuntime.WindowOpen ? .45f : 1f, 1f - .38f * story.Tension) : 1f;
            storyDuck = Mathf.MoveTowards(storyDuck, target, dt * 2f);
            float duck = story ? storyDuck : 1f - BossWeight;
            if (current != null) current.volume = fadeT * duck;
            if (fading != null)
            {
                fading.volume = (1f - fadeT) * duck;
                if (fadeT >= 1f) { fading.Stop(); fading = null; }
            }
            if (boss.clip != null) boss.volume = story ? BossWeight * storyDuck * .36f : BossWeight;

            if (Time.unscaledTime >= nextRoute) { nextRoute = Time.unscaledTime + 1f; RouteEffects(); }
        }

        float BossTarget()
        {
            if (lib.bossTrack == null) return 0f;
            if (story) return Mathf.SmoothStep(0, 1, story.Tension);
            var player = FindAnyObjectByType<GddActor>();
            if (player == null) return 0f;
            Vector3 p = player.transform.position;
            float nearest = float.MaxValue;
            foreach (var f in FindObjectsByType<GddFollow>())
                if (f.contactDamage > 0 && f.isActiveAndEnabled) nearest = Mathf.Min(nearest, Vector3.Distance(p, f.transform.position));
            foreach (var e in FindObjectsByType<global::Enemy>())
                if (e.isActiveAndEnabled) nearest = Mathf.Min(nearest, Vector3.Distance(p, e.transform.position));
            return Mathf.Clamp01((lib.bossStartDistance - nearest) / lib.bossFalloff);
        }

        /// <summary>Send every sound without a mixer group to Effects, so effects duck the music.</summary>
        void RouteEffects()
        {
            if (lib.effectsGroup == null) return;
            foreach (var s in FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
                if (s.outputAudioMixerGroup == null && s != a && s != b && s != boss) s.outputAudioMixerGroup = lib.effectsGroup;
        }

        public void SetVolumes(float master, float music, float effects)
        {
            AudioListener.volume = master;
            if (lib.mixer == null) return;
            lib.mixer.SetFloat("BGMVolume", ToDb(music));
            lib.mixer.SetFloat("SFXVolume", ToDb(effects));
            lib.mixer.SetFloat("BossVolume", ToDb(music));
        }

        static float ToDb(float v) => Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;
    }
}
