using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace GDD110
{
    /// <summary>
    /// Plays a Timeline forward when turned On and holds the last frame. Turned Off, it plays backward to the start.
    /// Reset jumps back to the first frame, and with "Play After Reset" it runs again, which makes "get there before it
    /// finishes" challenges: a gate that closes, a bridge that sinks, a timer you race.
    /// </summary>
    [AddComponentMenu("GDD 110/Triggerables/Play Timeline")]
    public class GddPlayTimeline : GddTriggerable
    {
        [Tooltip("The Playable Director with the Timeline to play. Leave empty to use one on this object.")]
        public PlayableDirector director;
        [Tooltip("1 = normal speed. Playing backward uses the same speed.")]
        [Min(0.01f)] public float speed = 1f;
        [Tooltip("After a Reset, start playing forward straight away.")]
        public bool playAfterReset = true;
        public UnityEvent onReachedEnd = new UnityEvent();
        public UnityEvent onReachedStart = new UnityEvent();

        double time;
        bool ready;

        /// <summary>0 at the first frame, 1 at the last.</summary>
        public float Normalized => director != null && director.duration > 0 ? (float)(time / director.duration) : 0f;

        void Awake()
        {
            if (director == null) director = GetComponent<PlayableDirector>();
            if (director == null) return;
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.Manual;
            director.extrapolationMode = DirectorWrapMode.Hold;
        }

        void EnsureGraph()
        {
            if (ready || director == null) return;
            director.Play();   // builds the graph; Manual mode means it only moves when we Evaluate
            ready = true;
        }

        protected override void ApplyState(bool on, bool instant)
        {
            EnsureGraph();
            if (instant && director != null) { time = on ? director.duration : 0; Evaluate(); }
        }

        public override void ResetState()
        {
            base.ResetState();
            EnsureGraph();
            time = 0;
            Evaluate();
            if (playAfterReset) TurnOn();
        }

        void Update()
        {
            if (director == null) return;
            EnsureGraph();
            double goal = IsOn ? director.duration : 0;
            if (time == goal) return;
            double step = Time.deltaTime * speed;
            time = goal > time ? System.Math.Min(goal, time + step) : System.Math.Max(goal, time - step);
            Evaluate();
            if (time == goal) { if (IsOn) onReachedEnd.Invoke(); else onReachedStart.Invoke(); }
        }

        void Evaluate()
        {
            if (director == null) return;
            director.time = time;
            director.Evaluate();
        }
    }
}
