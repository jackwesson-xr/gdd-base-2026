using UnityEngine;

namespace GDD110.Juice
{
    /// <summary>How a block's big moment should feel.</summary>
    public enum GddJuiceStyle { Pop, Collect, Heal, Equip, Bounce, Hurt, Teleport, Portal, Spawn, Interact, Trigger, Timer, Ambient, Companion, Enemy, Blaster }

    /// <summary>
    /// One entry per GDD 110 toolkit block: what the showcase tooltip says about it and how its moments look, sound and feel.
    /// Edit these assets to retune the juice without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "GDD 110/Juice/Block Info", fileName = "New Block Info")]
    public class GddBlockInfo : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Must match the toolkit template name, e.g. 'Bounce Pad'.")]
        public string blockName;
        [Tooltip("Showcase district and tooltip label, e.g. 'On entry'.")]
        public string category;
        [Tooltip("Where students find it in the GameObject menu.")]
        public string menuPath;
        public Color accent = new Color(0.2f, 0.8f, 1f);

        [Header("Tooltip")]
        [TextArea(2, 4)] public string whatItDoes;
        [TextArea(2, 4)] public string howToUse;

        [Header("Moment")]
        public GddJuiceStyle style;
        public AudioClip sound;
        [Range(0, 1)] public float volume = 0.8f;
        public Vector2 pitchRange = new Vector2(0.95f, 1.08f);
        public GameObject burstVfx;
        public float burstScale = 1f;
        [Tooltip("Seconds the whole game freezes on the hit. 0.03-0.12 reads as a snap.")]
        [Range(0, 0.3f)] public float freezeFrame;
        [Tooltip("Camera FOV kick in degrees. Negative punches in, positive whips out.")]
        public float fovPunch = -4f;
        [Tooltip("Dolly the camera toward the action (meters, springs back).")]
        public float cameraPush = 0.25f;
        [Range(0, 1)] public float shake = 0.25f;
        [Range(0, 1)] public float flash;
        [Range(0, 1)] public float chromatic = 0.25f;
        [Tooltip("Slow-motion timescale during the moment. 1 = off.")]
        [Range(0.05f, 1)] public float slowMo = 1f;
        public float slowMoDuration = 0.4f;
        [Tooltip("How hard the block itself squashes when it fires.")]
        [Range(0, 1)] public float squash = 0.35f;
        public string floatingText;
        [Tooltip("For powers: sound and effect when the held item is used (e.g. blaster shot).")]
        public AudioClip useSound;
        public GameObject useVfx;

        [Header("Idle")]
        public bool idleFloat;
        public GameObject idleVfx;
        public float idleVfxScale = 1f;

        [Header("Approach build-up")]
        [Tooltip("Camera narrows, time slows and a hum rises as the player closes in.")]
        public bool buildUpOnApproach;
        public float buildUpRadius = 6f;
        public AudioClip buildUpLoop;
    }
}
