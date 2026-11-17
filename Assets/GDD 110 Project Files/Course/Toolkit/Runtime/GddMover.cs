using UnityEngine;

namespace GDD110
{
    /// <summary>
    /// Moves and/or rotates an object to an "on" pose, holds it there, and plays back to the start when turned off.
    /// Doors, drawbridges, lifts, rising cubes. No animation asset needed.
    /// </summary>
    [AddComponentMenu("GDD 110/Triggerables/Mover (Door, Lift)")]
    public class GddMover : GddTriggerable
    {
        [Tooltip("What moves. Leave empty to move this object.")]
        public Transform target;
        [Tooltip("Offset from the start position when On, in the object's parent space.")]
        public Vector3 moveBy = new Vector3(0, 2, 0);
        public Vector3 rotateBy;
        [Min(0.01f)] public float duration = 1f;
        [Tooltip("How the motion speeds up and slows down.")]
        public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("Seconds to hold On before returning by itself. 0 = stay On until told otherwise.")]
        [Min(0)] public float autoReturnAfter;

        Vector3 homePos;
        Quaternion homeRot;
        float progress;
        float holdTimer = -1;
        bool cached;
        Rigidbody body;

        /// <summary>0 = start pose, 1 = on pose.</summary>
        public float Progress => progress;

        void Cache()
        {
            if (cached) return;
            if (target == null) target = transform;
            homePos = target.localPosition;
            homeRot = target.localRotation;
            body = target.GetComponent<Rigidbody>();
            cached = true;
        }

        void Awake() => Cache();

        protected override void ApplyState(bool on, bool instant)
        {
            Cache();
            if (instant) { progress = on ? 1 : 0; Pose(); }
            holdTimer = on && autoReturnAfter > 0 ? autoReturnAfter : -1;
        }

        void Update()
        {
            float goal = IsOn ? 1 : 0;
            if (!Mathf.Approximately(progress, goal))
            {
                progress = Mathf.MoveTowards(progress, goal, Time.deltaTime / duration);
                Pose();
            }
            else if (IsOn && holdTimer > 0)
            {
                holdTimer -= Time.deltaTime;
                if (holdTimer <= 0) TurnOff();
            }
        }

        void Pose()
        {
            float k = ease.Evaluate(progress);
            var pos = homePos + moveBy * k;
            var rot = homeRot * Quaternion.Euler(rotateBy * k);
            if (body != null && body.isKinematic && target.parent == null) { body.MovePosition(pos); body.MoveRotation(rot); }
            else { target.localPosition = pos; target.localRotation = rot; }
        }

        void OnDrawGizmosSelected()
        {
            var t = target != null ? target : transform;
            Vector3 start = Application.isPlaying && cached ? (t.parent != null ? t.parent.TransformPoint(homePos) : homePos) : t.position;
            Vector3 end = start + (t.parent != null ? t.parent.TransformVector(moveBy) : moveBy);
            Gizmos.color = new Color(0.35f, 1f, 0.5f, 0.8f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireCube(end, t.lossyScale);
        }
    }
}
