using UnityEngine;

namespace GDD110
{
    /// <summary>
    /// Swaps the level music for this track while On, then back when Off. Link it to a trigger zone for
    /// area music, or turn it on from any UnityEvent for a story beat.
    /// </summary>
    [AddComponentMenu("GDD 110/Triggerables/Music Override")]
    public class GddMusicOverride : GddTriggerable
    {
        public AudioClip track;
        public bool loop = true;

        protected override void ApplyState(bool on, bool instant)
        {
            if (on) GddMusicPlayer.Push(this);
            else GddMusicPlayer.Pop(this);
        }

        void OnDisable() { GddMusicPlayer.Pop(this); }
    }
}
