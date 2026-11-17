using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// Opens when the player brings the right Key Item. Fires On Unlocked (and any Link on this object),
    /// optionally using the key up. Without the key it fires On Missing Key instead.
    /// </summary>
    [RequireComponent(typeof(BoxCollider)), AddComponentMenu("GDD 110/Functions/Key Lock")]
    public class GddKeyLock : GddEntity
    {
        [Tooltip("Must match the Key Item's Key Name exactly.")]
        public string keyName = "Red Key";
        public GddCollectible.Activation activation = GddCollectible.Activation.PressInteract;
        [Tooltip("Remove the key from the HUD when it opens.")]
        public bool consumeKey = true;
        [Tooltip("Stay open after the first unlock.")]
        public bool unlockOnce = true;
        public UnityEvent onUnlocked = new UnityEvent();
        public UnityEvent onMissingKey = new UnityEvent();

        public bool Unlocked { get; private set; }

        public override string Prompt => GddInventory.HasKey(keyName) ? "Use " + keyName + " on" : "Needs " + keyName + ":";
        public override bool CanInteract(GddActor actor) => activation == GddCollectible.Activation.PressInteract && !(Unlocked && unlockOnce);

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (activation != GddCollectible.Activation.TouchIt) return;
            var actor = other.GetComponentInParent<GddActor>();
            if (actor != null) TryUnlock(actor);
        }

        public override void Interact(GddActor actor) { TryUnlock(actor); }
        public override void Activate() { TryUnlock(LastActor != null ? LastActor : FindAnyObjectByType<GddActor>()); }

        public void TryUnlock(GddActor actor)
        {
            if (Unlocked && unlockOnce) return;
            LastActor = actor;
            if (!GddInventory.HasKey(keyName)) { onMissingKey.Invoke(); return; }
            if (consumeKey) GddInventory.UseKey(keyName);
            Unlocked = true;
            onUnlocked.Invoke();
            RaiseActivated();
        }
    }
}
