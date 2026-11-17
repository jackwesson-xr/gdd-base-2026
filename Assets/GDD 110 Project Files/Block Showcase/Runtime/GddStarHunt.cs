using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>
    /// Easter egg: once the player has found every hidden star, the reward (switched off until then) appears with
    /// a burst, a sound and a camera visit.
    /// </summary>
    [AddComponentMenu("GDD 110/Juice/Star Hunt")]
    public class GddStarHunt : MonoBehaviour
    {
        public string counterName = "Secret Stars";
        public int needed = 4;
        public GameObject reward;
        public GameObject revealEffect;
        public AudioClip revealSound;
        bool done;

        void Start() { if (reward != null) reward.SetActive(false); GDD110.GddInventory.CountChanged += OnCount; }
        void OnDestroy() { GDD110.GddInventory.CountChanged -= OnCount; }

        void OnCount(string id, int count)
        {
            if (done || id != counterName || count < needed || reward == null) return;
            done = true;
            Invoke(nameof(Reveal), 1.2f);   // let the last star land in the HUD first
        }

        void Reveal()
        {
            reward.SetActive(true);
            var d = GddJuiceDirector.Instance;
            if (d != null) { d.SpawnVfx(revealEffect, reward.transform.position, 2f); d.PlaySfx(revealSound, 1f, 1f, 0f); d.Shake(0.6f); }
            GddFloatingText.Spawn("ALL STARS FOUND!", reward.transform.position + Vector3.up * 2.5f, new Color(1f, 0.85f, 0.3f));
            var focus = reward.AddComponent<GDD110.GddFocusMoment>();
            focus.focusTime = 2.5f; focus.pushIn = 4f;
            focus.enabled = true;
        }
    }
}
