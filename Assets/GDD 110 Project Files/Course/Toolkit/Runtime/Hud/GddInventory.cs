using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GDD110
{
    /// <summary>
    /// What the player is carrying in this level: counts by name ("Gems" x 12) and key items ("Red Key").
    /// Cleared when a level loads. Collectibles add to it, Key Locks take keys out of it.
    /// </summary>
    public static class GddInventory
    {
        static readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        static readonly List<string> keys = new List<string>();
        public static event Action<string, int> CountChanged;
        public static event Action<string, bool> KeyChanged;   // key, added?

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            counts.Clear(); keys.Clear();
            SceneManager.sceneLoaded -= Clear;
            SceneManager.sceneLoaded += Clear;
        }

        static void Clear(Scene s, LoadSceneMode m) { if (m == LoadSceneMode.Single) { counts.Clear(); keys.Clear(); } }

        public static int Count(string id) => counts.TryGetValue(id ?? "", out var n) ? n : 0;

        public static void Add(string id, int amount)
        {
            id = id ?? "";
            counts[id] = Mathf.Max(0, Count(id) + amount);
            CountChanged?.Invoke(id, counts[id]);
        }

        /// <summary>Spend some of a count. Returns false (and spends nothing) if there aren't enough.</summary>
        public static bool Spend(string id, int amount)
        {
            if (Count(id) < amount) return false;
            Add(id, -amount);
            return true;
        }

        public static bool HasKey(string id) => keys.Contains(id);

        public static void AddKey(string id) { keys.Add(id); KeyChanged?.Invoke(id, true); }

        public static bool UseKey(string id)
        {
            if (!keys.Remove(id)) return false;
            KeyChanged?.Invoke(id, false);
            return true;
        }
    }
}
