using UnityEngine;
using StarterAssets;

namespace GDD110
{
    [RequireComponent(typeof(ThirdPersonController))]
    [DisallowMultipleComponent]
    public sealed class GddSharedPlayer : MonoBehaviour
    {
        public GddCharacterProfile profile;
        void Awake()
        {
            var motor=GetComponent<ThirdPersonController>();
            // A scene-local skin deliberately overrides the course-wide setting.
            if(profile!=null && motor.CharacterAnimator==null) profile.ApplyToPlayer(motor);
        }
    }
}
