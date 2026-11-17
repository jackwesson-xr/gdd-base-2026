using StarterAssets;
using UnityEngine;

namespace GDD110
{
    internal static class GddCombatAnimation
    {
        public static void Play(GddActor actor, string state)
        {
            var motor = actor.GetComponent<ThirdPersonController>();
            var animator = motor ? motor.CharacterAnimator : null;
            int hash = Animator.StringToHash("Base Layer." + state);
            if (animator && animator.HasState(0, hash))
                animator.CrossFadeInFixedTime(hash, .08f, 0, 0);
        }

        public static void SetGunReady(GddActor actor, bool ready)
        {
            var motor = actor.GetComponent<ThirdPersonController>();
            var animator = motor ? motor.CharacterAnimator : null;
            if (animator && animator.HasState(0, Animator.StringToHash("Base Layer.Gun Ready")))
                animator.SetBool("Gun Ready", ready);
        }
    }
}
