using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>Common base for course entities and their drop-in functions.</summary>
    public class GddEntity : MonoBehaviour
    {
        [Tooltip("Student-facing name. Leave empty to use the GameObject name.")]
        public string friendlyName;
        [Tooltip("Connect these to functions on other objects. No code is required.")]
        public UnityEvent onActivated = new UnityEvent();
        public UnityEvent<GameObject> onActivatedBy = new UnityEvent<GameObject>();
        public GddActor LastActor { get; protected set; }
        public string DisplayName => string.IsNullOrWhiteSpace(friendlyName) ? gameObject.name : friendlyName;
        public virtual bool CanInteract(GddActor actor) => false;
        public virtual string Prompt => "Interact";
        public virtual void Interact(GddActor actor) { LastActor = actor; Activate(); }
        public virtual void Activate() { RaiseActivated(); }
        protected void RaiseActivated()
        {
            var actorObject = LastActor != null ? LastActor.gameObject : gameObject;
            onActivated.Invoke();
            onActivatedBy.Invoke(actorObject);
        }
        public void EnableObject() => gameObject.SetActive(true);
        public void DisableObject() => gameObject.SetActive(false);
        public void DestroyObject() => Destroy(gameObject);
        public void ToggleObject() => gameObject.SetActive(!gameObject.activeSelf);
    }
}
