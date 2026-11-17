using UnityEngine;

namespace GDD110
{
    /// <summary>
    /// A small sound zone that plays a clip locked in time with the level music. Give it the vocal stem of a song
    /// whose instrumental is the level music and the singer is heard only near the radio, perfectly in sync.
    /// Radio Sound squeezes it through a small speaker (band-limited and a little crunchy).
    /// </summary>
    [AddComponentMenu("GDD 110/Audio/Synced Radio")]
    public class GddSyncedRadio : MonoBehaviour
    {
        [Tooltip("What the radio plays (for example, the vocals-only version of the level music).")]
        public AudioClip clip;
        [Tooltip("Level music to stay in sync with. Empty = whatever level music is playing.")]
        public AudioClip syncWith;
        [Range(0, 1)] public float volume = 1f;
        [Tooltip("Full volume inside this distance (meters)...")]
        [Min(0.1f)] public float fullVolumeWithin = 2.5f;
        [Tooltip("...fading to silence at this distance.")]
        [Min(0.2f)] public float silentBeyond = 9f;

        [Header("Radio sound")]
        public bool radioSound = true;
        [Range(1000, 8000)] public float highCut = 3400f;
        [Range(100, 1500)] public float lowCut = 550f;
        [Range(0, 0.9f)] public float crunch = 0.3f;

        [Header("Speakers")]
        [Tooltip("Optional parts that pulse with the sound (speaker cones, an antenna).")]
        public Transform[] pulseWithSound;
        [Range(0, 0.5f)] public float pulseAmount = 0.18f;

        AudioSource source;
        Vector3[] baseScales;
        readonly float[] samples = new float[256];
        float level;

        void Start()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = fullVolumeWithin;
            source.maxDistance = silentBeyond;
            source.dopplerLevel = 0f;
            source.volume = volume;
            if (GddMusicPlayer.Instance != null) source.outputAudioMixerGroup = GddMusicPlayer.Instance.MusicGroup;
            if (radioSound)
            {
                gameObject.AddComponent<AudioHighPassFilter>().cutoffFrequency = lowCut;
                gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = highCut;
                if (crunch > 0f) gameObject.AddComponent<AudioDistortionFilter>().distortionLevel = crunch;
            }
            if (pulseWithSound != null)
            {
                baseScales = new Vector3[pulseWithSound.Length];
                for (int i = 0; i < pulseWithSound.Length; i++) if (pulseWithSound[i] != null) baseScales[i] = pulseWithSound[i].localScale;
            }
        }

        void Update()
        {
            if (source == null || clip == null) return;
            var music = GddMusicPlayer.Instance != null ? GddMusicPlayer.Instance.CurrentSource : null;
            source.volume = volume * (GddMusicPlayer.Instance != null ? GddMusicPlayer.Instance.StoryDuck : 1f);
            bool locked = music != null && music.isPlaying && music.clip != null && (syncWith == null || music.clip == syncWith);
            if (locked)
            {
                float target = music.time % clip.length;
                if (!source.isPlaying) { source.time = target; source.Play(); }
                else if (Mathf.Abs(source.time - target) > 0.04f) source.time = target;   // keep within 40 ms
            }
            else if (!source.isPlaying && syncWith == null) source.Play();

            if (pulseWithSound == null || baseScales == null) return;
            source.GetOutputData(samples, 0);
            float sum = 0f;
            foreach (var v in samples) sum += v * v;
            level = Mathf.Lerp(level, Mathf.Sqrt(sum / samples.Length) * 6f, Time.deltaTime * 18f);
            for (int i = 0; i < pulseWithSound.Length; i++)
                if (pulseWithSound[i] != null) pulseWithSound[i].localScale = baseScales[i] * (1f + Mathf.Clamp01(level) * pulseAmount);
        }

        // Keep the near radio's vocal dynamics narrow enough to read as a small loudspeaker.
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!radioSound) return;
            const float threshold = .20f;
            for (int i = 0; i < data.Length; i++)
            {
                float sample = data[i];
                float magnitude = System.Math.Abs(sample);
                if (magnitude > threshold)
                    data[i] = (sample < 0 ? -1f : 1f) * (threshold + (magnitude - threshold) * .28f);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, fullVolumeWithin);
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, silentBeyond);
        }
    }
}
