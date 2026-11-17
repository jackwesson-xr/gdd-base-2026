using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GDD110
{
    /// <summary>
    /// Which music plays in which level. One entry per scene in the build list (use the Fill button),
    /// plus a default, a boss track that fades in near enemies, and the mixer groups everything plays through.
    /// </summary>
    [CreateAssetMenu(menuName = "GDD 110/Music Library", fileName = "Music Library")]
    public class GddMusicLibrary : ScriptableObject
    {
        [System.Serializable]
        public class SceneTrack
        {
            [Tooltip("Scene name exactly as in the build list.")]
            public string scene;
            public AudioClip track;
            public bool silence;
            public bool loop = true;
        }

        [Header("Mixer")]
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup;
        [Tooltip("Every sound effect without its own group is routed here. Effects duck the music automatically.")]
        public AudioMixerGroup effectsGroup;
        public AudioMixerGroup bossGroup;

        [Header("Level music")]
        [Tooltip("Plays in any level without an entry below (or whose entry has no track).")]
        public AudioClip defaultTrack;
        public List<SceneTrack> sceneTracks = new List<SceneTrack>();
        [Min(0)] public float crossfadeSeconds = 1.5f;

        [Header("Boss music")]
        [Tooltip("Fades in as an enemy gets close and fully ducks the level music.")]
        public AudioClip bossTrack;
        [Tooltip("Boss music starts at this distance from the nearest enemy (meters).")]
        public float bossStartDistance = 15f;
        [Tooltip("Meters over which it rises to full volume. 15 start with 5 falloff = full at 10 m.")]
        [Min(0.01f)] public float bossFalloff = 5f;

        public SceneTrack Find(string sceneName)
        {
            foreach (var t in sceneTracks) if (t != null && t.scene == sceneName) return t;
            return null;
        }
    }
}
