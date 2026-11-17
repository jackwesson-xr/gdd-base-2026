using System;
using UnityEngine;

namespace GDD110
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10150)]
    [AddComponentMenu("GDD 110/Character/Wolf Secondary Motion")]
    public sealed class GddWolfSecondaryMotion : MonoBehaviour
    {
        [Serializable]
        public sealed class Spring
        {
            public Transform bone;
            [Min(.1f)] public float frequency = 3;
            [Range(.15f, 1.5f)] public float damping = .55f;
            [Range(0, 30)] public float limit = 10;
            [Min(0)] public float response = 1;

            internal Quaternion animated;
            internal Vector3 position, velocity, angles, angularVelocity;
            internal bool ready, applied;
        }

        public Transform neck;
        [Range(0, 15)] public float runningLean = 4.5f;
        [Range(0, 20)] public float neckLimit = 9;
        public Spring[] clothAndFrills = Array.Empty<Spring>();
        readonly Spring neckSpring = new Spring { frequency = 2.25f, damping = .78f };
        Transform facing;
        Animator animator;

        StarterAssets.ThirdPersonController motor;
        Transform leftFoot, rightFoot;
        Vector3 leftSole, rightSole, leftUp, rightUp;
        float leftContact, rightContact;
        bool footPlacementReady;
        readonly RaycastHit[] footHits = new RaycastHit[12];

        public void CalibrateFootPlacement(Animator fittedAnimator, Transform root)
        {
            animator = fittedAnimator;
            facing = root;
            motor = root.GetComponent<StarterAssets.ThirdPersonController>();
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (!leftFoot || !rightFoot) return;

            // Heel-to-toe midpoint and sole plane in the authored sneaker model.
            Vector3 sole = root.forward * .065f - root.up * .124f;
            leftSole = leftFoot.InverseTransformVector(sole * animator.transform.lossyScale.y);
            rightSole = rightFoot.InverseTransformVector(sole * animator.transform.lossyScale.y);
            leftUp = leftFoot.InverseTransformDirection(root.up);
            rightUp = rightFoot.InverseTransformDirection(root.up);
            footPlacementReady = true;
        }

        void OnAnimatorIK(int layer)
        {
            if (!footPlacementReady || layer != 0) return;
            bool supported = !motor || motor.Grounded;
            float speed = animator.GetFloat("Speed");
            float stationary = 1f - Mathf.InverseLerp(.08f, .65f, speed);
            PlaceFoot(AvatarIKGoal.LeftFoot, leftFoot, leftSole, leftUp, supported, stationary, ref leftContact);
            PlaceFoot(AvatarIKGoal.RightFoot, rightFoot, rightSole, rightUp, supported, stationary, ref rightContact);
        }

        void PlaceFoot(AvatarIKGoal goal, Transform foot, Vector3 sole, Vector3 shoeUp, bool supported,
            float stationary, ref float contact)
        {
            Vector3 ankle = animator.GetIKPosition(goal);
            Quaternion animatedRotation = animator.GetIKRotation(goal);
            var scale = animator.transform.lossyScale.y;
            Vector3 offset = foot.rotation * (sole * scale);
            Vector3 projectedSole = ankle + offset;
            Vector3 up = facing.up;
            int mask = motor ? motor.GroundLayers.value : Physics.DefaultRaycastLayers;
            int count = supported ? Physics.RaycastNonAlloc(projectedSole + up * .44f, -up,
                footHits, .84f, mask, QueryTriggerInteraction.Ignore) : 0;
            int best = -1;
            for (int i = 0; i < count; i++)
            {
                var hit = footHits[i];
                if (hit.collider.transform.IsChildOf(facing) || Vector3.Dot(hit.normal, up) < .7f) continue;
                if (best < 0 || hit.distance < footHits[best].distance) best = i;
            }

            float weight = 0f;
            if (best >= 0)
            {
                var ground = footHits[best];
                float clearance = Vector3.Dot(projectedSole - ground.point, up);
                weight = Mathf.Max(stationary, 1f - Mathf.InverseLerp(.015f, .12f, clearance));
                // Unity's Humanoid goal has a different rotation basis than the shoe bone.
                Quaternion level = Quaternion.FromToRotation(foot.rotation * shoeUp, ground.normal);
                Quaternion correction = Quaternion.Slerp(Quaternion.identity, level, Mathf.Max(stationary, .7f * weight));
                Vector3 target = ground.point + ground.normal * .006f - correction * offset;
                animator.SetIKPosition(goal, target);
                animator.SetIKRotation(goal, correction * animatedRotation);
            }
            float dt = animator.updateMode == AnimatorUpdateMode.UnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            contact = best >= 0 ? Mathf.MoveTowards(contact, weight, 18f * dt) : 0f;
            animator.SetIKPositionWeight(goal, contact);
            animator.SetIKRotationWeight(goal, contact);
        }

        void Awake()
        {
            animator = GetComponent<Animator>();
            var actor = GetComponentInParent<GddActor>();
            facing = actor ? actor.transform : transform;
            neckSpring.bone = neck;
        }

        void Update() => RestorePose();

        void OnDisable()
        {
            RestorePose();
            neckSpring.ready = false;
            foreach (var spring in clothAndFrills) spring.ready = false;
        }

        void RestorePose()
        {
            Restore(neckSpring);
            foreach (var spring in clothAndFrills) Restore(spring);
        }

        static void Restore(Spring spring)
        {
            if (!spring.applied || !spring.bone) return;
            spring.bone.localRotation = spring.animated;
            spring.applied = false;
        }

        void LateUpdate()
        {
            float dt = animator && animator.updateMode == AnimatorUpdateMode.UnscaledTime
                ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= .0001f) return;
            // Record the animated pose before adding any parent offsets.
            Sample(neckSpring, dt, true);
            foreach (var spring in clothAndFrills) Sample(spring, dt, false);
            Apply(neckSpring);
            foreach (var spring in clothAndFrills) Apply(spring);
        }

        void Sample(Spring spring, float dt, bool isNeck)
        {
            if (!spring.bone || !spring.bone.parent) return;
            spring.animated = spring.bone.localRotation;
            Vector3 position = spring.bone.position;
            Vector3 delta = position - spring.position;
            spring.position = position;
            if (!spring.ready || delta.sqrMagnitude > 2.25f || dt > .2f)
            {
                spring.ready = true;
                spring.velocity = spring.angularVelocity = spring.angles = Vector3.zero;
                return;
            }

            Vector3 velocity = delta / dt;
            Vector3 force = Vector3.ClampMagnitude((velocity - spring.velocity) / dt, 40);
            spring.velocity = velocity;
            Vector3 longitudinalUp = facing.up;
            Vector3 tipLag = Vector3.Cross(force, longitudinalUp);
            Vector3 aim = tipLag * spring.response;
            float limit = spring.limit;
            if (isNeck)
            {
                float forwardSpeed = Vector3.Dot(velocity, facing.forward);
                float forwardForce = Vector3.Dot(force, facing.forward);
                float pitch = runningLean * Mathf.Clamp(forwardSpeed / 5f, -.4f, 1f) - forwardForce * .34f;
                float roll = Vector3.Dot(force, facing.right) * .17f;
                aim = facing.right * pitch + facing.forward * roll;
                limit = neckLimit;
            }

            Vector3 target = spring.bone.parent.InverseTransformDirection(Vector3.ClampMagnitude(aim, limit));
            float omega = Mathf.PI * 2 * spring.frequency;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt * 120));
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                spring.angularVelocity += ((target - spring.angles) * (omega * omega)
                    - spring.angularVelocity * (2 * spring.damping * omega)) * h;
                spring.angles = Vector3.ClampMagnitude(spring.angles + spring.angularVelocity * h, limit);
            }
        }

        static void Apply(Spring spring)
        {
            if (!spring.ready || !spring.bone) return;
            spring.bone.localRotation = Quaternion.Euler(spring.angles) * spring.animated;
            spring.applied = true;
        }
    }
}
