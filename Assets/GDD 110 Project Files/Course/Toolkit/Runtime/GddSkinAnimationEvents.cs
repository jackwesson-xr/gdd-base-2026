using UnityEngine;

namespace GDD110
{
    [DisallowMultipleComponent]
    public sealed class GddSkinAnimationEvents : MonoBehaviour
    {
        public StarterAssets.ThirdPersonController motor;
        public void OnFootstep(AnimationEvent evt) { if (motor != null) motor.OnFootstep(evt); }
        public void OnLand(AnimationEvent evt) { if (motor != null) motor.OnLand(evt); }
    }
}
