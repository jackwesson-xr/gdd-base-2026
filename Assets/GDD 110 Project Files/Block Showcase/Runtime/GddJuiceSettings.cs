using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GDD110.Juice
{
    /// <summary>Global switches and the block catalog. Lives at Resources/GddJuiceSettings.</summary>
    public class GddJuiceSettings : ScriptableObject
    {
        [Tooltip("Turns the whole juice layer on or off in every scene. The student project never contains this folder.")]
        public bool juiceEnabled = true;
        [Tooltip("Show guide tooltips in every scene, not only the Block Showcase.")]
        public bool tooltipsEverywhere;
        public float tooltipRadius = 4.5f;
        [Tooltip("Draw the trigger -> target link lines in every scene, not only the showcase.")]
        public bool showLinksEverywhere;
        [Tooltip("Show the button-prompt bar at the bottom of the screen.")]
        public bool showActionPrompts = true;
        [Tooltip("Collectibles come back after this long in the showcase.")]
        public float showcaseRespawnSeconds = 4f;

        public List<GddBlockInfo> blocks = new List<GddBlockInfo>();

        [Header("Look")]
        public VolumeProfile postProfile;
        public VolumeProfile buildUpProfile;
        public Sprite panelSprite;
        public Material lineMaterial;

        [Header("Shared sounds and VFX")]
        public AudioClip tooltipOpen;
        public AudioClip tooltipClose;
        public AudioClip hurtSound;
        public AudioClip healSound;
        public AudioClip scoreSound;
        public AudioClip arriveSound;
        public GameObject arriveVfx;
        public GameObject hurtVfx;
        public GameObject poofVfx;
        public GameObject spawnPopVfx;
        [Tooltip("Small flash where a Link reaches its target.")]
        public GameObject linkPulseVfx;

        [Header("Creatures")]
        public Texture2D heartIcon;
        public Texture2D angerIcon;
        public GameObject heartBurstVfx;
        public AudioClip happySound;
        public AudioClip angrySound;
        public GameObject deathVfx;
        public AudioClip deathSound;

        static GddJuiceSettings cached;
        public static GddJuiceSettings Load()
        {
            if (cached == null) cached = Resources.Load<GddJuiceSettings>("GddJuiceSettings");
            return cached;
        }

        public GddBlockInfo Find(string blockName)
        {
            if (string.IsNullOrEmpty(blockName)) return null;
            foreach (var b in blocks) if (b != null && b.blockName == blockName) return b;
            return null;
        }
    }
}
