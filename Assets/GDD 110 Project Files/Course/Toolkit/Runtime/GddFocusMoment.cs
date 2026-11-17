using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// Points the camera at this object for a few seconds, then eases back to normal.
    /// Starts switched off. Switch it on from any UnityEvent (tick the component's enabled box, or call Play)
    /// and the moment runs. With Loop off it runs once and removes itself; with Loop on it switches itself
    /// off afterwards and runs again every time it is switched back on.
    /// </summary>
    [AddComponentMenu("GDD 110/Camera/Focus Moment")]
    public class GddFocusMoment : MonoBehaviour
    {
        [Tooltip("Seconds the camera stays on this object.")]
        [Min(0.1f)] public float focusTime = 2.5f;
        [Tooltip("Off: happens once, then this component removes itself. On: can happen again whenever it is re-enabled.")]
        public bool loop;
        [Tooltip("Seconds to swing over and to swing back.")]
        [Min(0)] public float blendSeconds = 0.8f;
        [Tooltip("Move the camera this far toward the object while looking (0 = just turn to look).")]
        [Min(0)] public float pushIn = 1.5f;
        [Tooltip("Stop the player moving during the moment.")]
        public bool freezePlayer;
        public UnityEvent onFocusStart = new UnityEvent();
        public UnityEvent onFocusEnd = new UnityEvent();

        bool running;

        void Reset() { enabled = false; }

        /// <summary>Start the moment from a UnityEvent (same as switching the component on).</summary>
        public void Play() { if (enabled && !running) StartCoroutine(Run()); else enabled = true; }

        void OnEnable() { if (Application.isPlaying && !running) StartCoroutine(Run()); }

        IEnumerator Run()
        {
            running = true;
            var main = Camera.main;
            if (main == null) { Finish(); yield break; }
            var brain = main.GetComponent<CinemachineBrain>();

            var go = new GameObject("Focus Moment Camera (" + name + ")");
            go.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            Vector3 toTarget = transform.position - main.transform.position;
            if (pushIn > 0 && toTarget.magnitude > pushIn + 1f) go.transform.position += toTarget.normalized * pushIn;
            var cam = go.AddComponent<CinemachineCamera>();
            var lens = cam.Lens; lens.FieldOfView = main.fieldOfView; cam.Lens = lens;
            cam.LookAt = transform;
            go.AddComponent<CinemachineRotationComposer>();
            cam.Priority = 2000;

            CinemachineBlendDefinition saved = default;
            if (brain != null) { saved = brain.DefaultBlend; brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendSeconds); }
            cam.Prioritize();

            var actor = freezePlayer ? FindAnyObjectByType<GddActor>() : null;
            var mover = actor != null ? actor.GetComponent<MonoBehaviour>() : null;
            var tpc = actor != null ? actor.GetComponent("ThirdPersonController") as Behaviour : null;
            if (tpc != null) tpc.enabled = false;

            onFocusStart.Invoke();
            yield return new WaitForSeconds(blendSeconds + focusTime);
            Destroy(go);
            if (tpc != null) tpc.enabled = true;
            onFocusEnd.Invoke();
            yield return new WaitForSeconds(blendSeconds);
            if (brain != null) brain.DefaultBlend = saved;
            Finish();
        }

        void Finish()
        {
            running = false;
            if (loop) enabled = false;
            else Destroy(this);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.2f);
        }
    }
}
