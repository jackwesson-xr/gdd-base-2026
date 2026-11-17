using UnityEngine;

namespace GDD110
{
    [DisallowMultipleComponent, AddComponentMenu("GDD 110/Level/Level Block")]
    public sealed class GddLevelBlock : MonoBehaviour
    {
        public string areaName;
        public Transform arrival;
        public Transform departingLift;
        public Transform arrivingLift;
        [Tooltip("Positive rises. Negative descends.")]
        public float liftTravel;
        [TextArea] public string objective;
        public Color ambient = new Color(.55f, .6f, .7f);
        public Color sunlight = Color.white;
        public Color fog = new Color(.37f, .47f, .59f);
    }
}
