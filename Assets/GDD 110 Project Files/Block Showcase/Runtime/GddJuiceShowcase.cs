using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>Marks a scene as the Block Showcase: guide tooltips appear and collectibles come back after being taken.</summary>
    [AddComponentMenu("GDD 110/Juice/Block Showcase Marker")]
    public class GddJuiceShowcase : MonoBehaviour
    {
        [TextArea] public string note = "Walk up to any block to see what it does. Juice and tooltips are added at runtime by GddJuiceDirector (Assets/GDD 110 Project Files/Block Showcase).";
    }
}
