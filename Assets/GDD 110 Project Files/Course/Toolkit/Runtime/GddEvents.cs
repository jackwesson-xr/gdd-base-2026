using System;
using UnityEngine;

namespace GDD110
{
    /// <summary>Toolkit-wide notifications other systems can listen to (the juice layer uses these).</summary>
    public static class GddEvents
    {
        /// <summary>Something new appeared in the level at runtime (a dropped item, a spawned object).</summary>
        public static event Action<GameObject> Spawned;
        public static void RaiseSpawned(GameObject go) { if (go != null) Spawned?.Invoke(go); }
    }
}
