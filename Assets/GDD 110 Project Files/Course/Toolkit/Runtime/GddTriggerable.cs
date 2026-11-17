using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// Base for anything a trigger can switch: toggles, movers, timelines, cameras.
    /// It is a GddEntity, so it can be dragged into any Link and still exposes UnityEvents.
    /// Activate() toggles it. A Link can also turn it on, off, or reset it.
    /// </summary>
    public abstract class GddTriggerable : GddEntity
    {
        [Tooltip("State when the scene starts.")]
        public bool startOn;
        public UnityEvent onTurnedOn = new UnityEvent();
        public UnityEvent onTurnedOff = new UnityEvent();
        public bool IsOn { get; private set; }
        bool started;

        protected virtual void Start()
        {
            if (started) return;
            started = true;
            IsOn = startOn;
            ApplyState(IsOn, true);
        }

        public override void Activate() => Toggle();
        public void Toggle() { if (IsOn) TurnOff(); else TurnOn(); }

        public void TurnOn()
        {
            Start();
            if (IsOn) { OnRetrigger(); return; }
            IsOn = true;
            ApplyState(true, false);
            onTurnedOn.Invoke();
            RaiseActivated();
        }

        public void TurnOff()
        {
            Start();
            if (!IsOn) return;
            IsOn = false;
            ApplyState(false, false);
            onTurnedOff.Invoke();
            RaiseActivated();
        }

        /// <summary>Back to the starting state, instantly.</summary>
        public virtual void ResetState()
        {
            Start();
            IsOn = startOn;
            ApplyState(IsOn, true);
        }

        /// <summary>Show the on or off state. instant = snap with no animation.</summary>
        protected abstract void ApplyState(bool on, bool instant);
        /// <summary>Called when TurnOn arrives while already on.</summary>
        protected virtual void OnRetrigger() { }
    }
}
