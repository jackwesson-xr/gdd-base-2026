using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>Plays a named Animator state (idle), and a second one while the object is moving.</summary>
    [AddComponentMenu("GDD 110/Juice/Animation State")]
    public class GddAnimState : MonoBehaviour
    {
        public string idleState = "Idle";
        public string movingState;
        public float movingSpeed = 0.4f;
        [Tooltip("Turn to face the way it is moving.")]
        public bool faceMotion = true;
        Animator anim;
        Vector3 last;
        bool moving, started;

        void Start() { anim = GetComponentInChildren<Animator>(); last = transform.position; Play(idleState); started = true; }

        void Play(string state)
        {
            if (anim == null || string.IsNullOrEmpty(state)) return;
            int hash = Animator.StringToHash(state);
            if (anim.HasState(0, hash)) anim.CrossFade(hash, started ? 0.15f : 0f);
        }

        void Update()
        {
            if (Time.deltaTime <= 0) return;
            Vector3 v = (transform.position - last) / Time.deltaTime; last = transform.position;
            v.y = 0;
            if (faceMotion && v.sqrMagnitude > 0.05f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(v.normalized), Time.deltaTime * 8f);
            bool m = v.magnitude > movingSpeed;
            if (m == moving || anim == null || string.IsNullOrEmpty(movingState)) return;
            moving = m;
            Play(moving ? movingState : idleState);
        }
    }
}
