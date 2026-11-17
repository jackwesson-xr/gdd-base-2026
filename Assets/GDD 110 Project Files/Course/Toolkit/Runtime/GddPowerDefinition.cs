using UnityEngine;

namespace GDD110
{
    public enum GddPowerKind { EventsOnly, Projectile, Heal, SpeedBoost, JumpBoost, Push, Score }
    [CreateAssetMenu(menuName="GDD 110/Power Definition",fileName="New Power")]
    public class GddPowerDefinition : ScriptableObject
    {
        public GddPowerKind kind;
        [TextArea] public string studentInstructions="Choose a power and change its numbers. Put this asset on a pickup's Power function.";
        [Min(0)] public float cooldown=0.3f;
        [Min(0)] public float amount=10;
        [Min(0.1f)] public float multiplier=2;
        [Min(0.1f)] public float duration=5;
        public GameObject projectilePrefab;
        [Min(0.1f)] public float projectileSpeed=18;
        [Min(0.1f)] public float projectileLifetime=5;
        [Min(0.1f)] public float range=10;
    }
}
