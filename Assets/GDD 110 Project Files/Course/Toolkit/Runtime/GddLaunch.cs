using UnityEngine;
namespace GDD110
{
    /// <summary>
    /// Pushes a character through the air. Upward speed goes into the player controller's own jump arc (so gravity
    /// and landing feel normal); sideways speed is applied here and fades out.
    /// </summary>
    [AddComponentMenu("")]
    public class GddLaunch : GddEntity
    {
        Vector3 velocity;
        CharacterController controller;
        StarterAssets.ThirdPersonController motor;

        public void Launch(Vector3 impulse)
        {
            controller = GetComponent<CharacterController>();
            motor = GetComponent<StarterAssets.ThirdPersonController>();
            if (motor != null && motor.enabled) { motor.Launch(impulse.y); impulse.y = 0; }
            velocity = impulse;
        }

        void Update()
        {
            if (controller == null || !controller.enabled || velocity.sqrMagnitude < 0.01f) return;
            controller.Move(velocity * Time.deltaTime);
            float decay = motor != null ? 7f : 15f;
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, decay * Time.deltaTime);
            if (controller.isGrounded && velocity.y < 0) velocity.y = 0;
        }
    }
}
