using UnityEngine;

namespace GDD110
{
    /// <summary>
    /// A shared setting for what dropped items do. Make one asset and put it on as many pickups as you like
    /// (Pickup Return function). Change the asset and every item that uses it follows.
    /// </summary>
    [CreateAssetMenu(menuName = "GDD 110/Drop Rule", fileName = "New Drop Rule")]
    public class GddDropRule : ScriptableObject
    {
        public enum Mode { StayOnGround, ReturnRightAway, WaitThenReturn }
        [Tooltip("Stay On Ground: it stays where it lands.\nReturn Right Away: it dissolves and reappears where it was picked up.\nWait Then Return: it lies on the ground, then dissolves and reappears.")]
        public Mode mode = Mode.WaitThenReturn;
        [Tooltip("Wait Then Return only: seconds on the ground before it goes home.")]
        [Min(0)] public float waitSeconds = 6f;
        [Tooltip("How long the dissolve and reappear take.")]
        [Min(0.05f)] public float dissolveSeconds = 0.5f;
    }
}
