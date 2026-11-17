using Unity.Cinemachine;
using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Shared camera juice: FOV punches and dolly pushes driven by springs, plus a sustained build-up offset.
    /// Applied to every Cinemachine camera through GddJuiceCameraExtension, or straight to a plain Camera.
    /// Runs on unscaled time so it keeps moving through freeze frames and slow motion.
    /// </summary>
    public static class GddJuiceCamera
    {
        public static float FovOffset { get; private set; }
        public static float Push { get; private set; }
        public static float BuildUp;   // 0-1, set by the director while approaching portals
        /// <summary>Extra FOV held for as long as someone keeps setting it (speed boots); eases back to 0 when they stop.</summary>
        public static float HoldFov;
        static float held;

        static float fov, fovVel, push, pushVel;
        static int lastFrame = -1;

        public static void Punch(float fovDegrees, float pushMeters)
        {
            fovVel += fovDegrees * 18f;
            pushVel += pushMeters * 18f;
        }

        public static void Reset() { fov = fovVel = push = pushVel = 0; BuildUp = 0; FovOffset = 0; Push = 0; HoldFov = held = 0; }

        public static void Tick()
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Spring(ref fov, ref fovVel, 160f, 11f, dt);
            Spring(ref push, ref pushVel, 160f, 12f, dt);
            float b = BuildUp * BuildUp;
            held = Mathf.Lerp(held, HoldFov, 1f - Mathf.Exp(-4f * dt));
            HoldFov = Mathf.MoveTowards(HoldFov, 0, dt * 20f);
            FovOffset = fov - 9f * b + held;
            Push = push + 0.9f * b;
        }

        static void Spring(ref float x, ref float v, float k, float c, float dt)
        {
            v += (-k * x - c * v) * dt;
            x += v * dt;
        }
    }

    /// <summary>Adds the shared FOV and push offsets to whatever this Cinemachine camera computes.</summary>
    [AddComponentMenu("")]
    public class GddJuiceCameraExtension : CinemachineExtension
    {
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize) return;
            GddJuiceCamera.Tick();
            var lens = state.Lens;
            if (!lens.Orthographic) lens.FieldOfView = Mathf.Clamp(lens.FieldOfView + GddJuiceCamera.FovOffset, 8f, 130f);
            state.Lens = lens;
            if (Mathf.Abs(GddJuiceCamera.Push) > 0.0001f)
                state.PositionCorrection += state.GetFinalOrientation() * Vector3.forward * GddJuiceCamera.Push;
        }
    }

    /// <summary>Fallback for scenes whose camera is not driven by Cinemachine.</summary>
    [AddComponentMenu("")]
    public class GddJuicePlainCamera : MonoBehaviour
    {
        Camera cam;
        float baseFov;
        Vector3 lastPush;

        void Awake() { cam = GetComponent<Camera>(); baseFov = cam.fieldOfView; }

        void LateUpdate()
        {
            GddJuiceCamera.Tick();
            transform.position -= lastPush;
            if (!cam.orthographic) cam.fieldOfView = Mathf.Clamp(baseFov + GddJuiceCamera.FovOffset, 8f, 130f);
            lastPush = transform.forward * GddJuiceCamera.Push;
            transform.position += lastPush;
        }
    }
}
