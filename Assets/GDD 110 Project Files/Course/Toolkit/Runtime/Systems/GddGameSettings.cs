using UnityEngine;
using UnityEngine.InputSystem;

namespace GDD110
{
    /// <summary>
    /// One place for the game-wide controls and options: hotkeys, scene transitions, the game library link,
    /// default player options and the music setup. Lives at Toolkit/Resources/GddGameSettings.
    /// Created by Tools > GDD 110 > Game Systems > Set Up or Update.
    /// </summary>
    [CreateAssetMenu(menuName = "GDD 110/Game Settings", fileName = "GddGameSettings")]
    public class GddGameSettings : ScriptableObject
    {
        [Header("Keys")]
        [Tooltip("Opens the settings window. Hold Shift with it for the developer window.")]
        public Key settingsKey = Key.Backquote;
        public Key quickSaveKey = Key.Digit0;
        public Key quickLoadKey = Key.Digit8;
        public Key restartLevelKey = Key.Digit7;
        [Tooltip("Hold to go back one level.")]
        public Key previousLevelKey = Key.Comma;
        [Tooltip("Hold to go forward one level.")]
        public Key nextLevelKey = Key.Period;
        [Min(0.1f)] public float levelHoldSeconds = 1.5f;

        [Header("Game library")]
        [Tooltip("Tap once for a prompt, again (or Enter) to go. Holding it goes straight there.")]
        public Key libraryKey = Key.Slash;
        public string libraryUrl = "https://wesson.zone/gamelibrary/110";
        [Tooltip("On: open the library in a new browser tab (this tab closes itself when the browser allows). Off: replace this tab.")]
        public bool openLibraryInNewTab = true;
        [Min(0.2f)] public float libraryHoldSeconds = 1f;

        [Header("Level transitions")]
        [Min(0)] public float fadeOutSeconds = 0.5f;
        [Min(0)] public float fadeInSeconds = 0.5f;
        public Color fadeColor = Color.black;

        [Header("Defaults (players can change these in the settings window)")]
        [Range(0.1f, 3f)] public float mouseSensitivity = 1f;
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.7f;
        [Range(0f, 1f)] public float effectsVolume = 1f;

        [Header("Health bar")]
        [Tooltip("Show the player's health in the top-left of every level (unless the level has its own Health Bar).")]
        public bool showHealthBar = true;
        [Tooltip("One heart is enough: it repeats once per chunk.")]
        public Sprite healthIcon;
        [Tooltip("Optional overlay that never empties.")]
        public Sprite healthFrame;
        [Min(1)] public int healthChunks = 5;
        public GddFillLook healthLook = GddFillLook.Grey;
        public Shader fillShader;

        [Header("Music")]
        public GddMusicLibrary music;

        [Header("Look (filled in by the setup menu)")]
        public Sprite panelSprite;
        public Sprite knobSprite;
        public Sprite checkSprite;

        static GddGameSettings cached;
        public static GddGameSettings Load()
        {
            if (cached == null) cached = Resources.Load<GddGameSettings>("GddGameSettings");
            return cached;
        }
    }
}
