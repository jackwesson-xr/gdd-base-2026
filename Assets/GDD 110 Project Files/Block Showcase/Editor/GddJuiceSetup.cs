using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;

namespace GDD110.Juice.EditorTools
{
    /// <summary>Builds the juice catalog, post profiles, icons and the Block Showcase scene. Safe to re-run.</summary>
    public static partial class GddJuiceSetup
    {
        const string Root = "Assets/GDD 110 Project Files/Block Showcase";
        const string Toolkit = "Assets/GDD 110 Project Files/Course/Toolkit/Prefabs";
        // Generated art lives in the shared asset pack, sorted by type and use.
        const string Pack = "Assets/GDD 110 Project Files/Wesson Assets";
        const string MatDir = Pack + "/Materials/Showcase";
        const string TexDir = Pack + "/Textures/Showcase";
        const string AnimDir = Pack + "/Animation/Showcase";
        const string PostDir = Pack + "/Rendering/Post Processing";

        // ---------- lookups ----------

        static AudioClip Sfx(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var g in AssetDatabase.FindAssets(name + " t:AudioClip"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            }
            Debug.LogWarning("[GDD Juice] Sound not found: " + name);
            return null;
        }

        static GameObject Fx(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var g in AssetDatabase.FindAssets(name + " t:Prefab", new[] { "Assets" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            Debug.LogWarning("[GDD Juice] VFX not found: " + name);
            return null;
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        // ---------- block catalog ----------

        class Spec
        {
            public string name, category, what, how, sound, vfx, idleVfx, loop, text, useSound, useVfx;
            public GddJuiceStyle style;
            public Color accent;
            public float vfxScale = 1, idleScale = 1, freeze, fov = -3, push = 0.2f, shake = 0.2f, flash, chroma = 0.2f, slow = 1, slowDur = 0.4f, squash = 0.35f, radius = 6;
            public bool idleFloat, buildUp;
        }

        public const string OnEntry = "Touchables", OnTouch = "Touchables", OnTimer = "Automations", OnKey = "Interactions", PickUp = "Pickups", Creatures = "NPCs", Triggerable = "Triggerable";
        static readonly Color Cyan = new Color(0.2f, 0.75f, 1f), Gold = new Color(1f, 0.74f, 0.2f), Green = new Color(0.3f, 0.9f, 0.45f),
            Red = new Color(1f, 0.3f, 0.26f), Violet = new Color(0.62f, 0.42f, 1f), Orange = new Color(1f, 0.55f, 0.2f), Pink = new Color(1f, 0.42f, 0.7f),
            Mint = new Color(0.2f, 0.88f, 0.7f), Blue = new Color(0.36f, 0.55f, 1f);

        static List<Spec> Specs() => new List<Spec>
        {
            // On entry
            new Spec{ name="Event Trigger", category=OnEntry, accent=Cyan, style=GddJuiceStyle.Trigger,
                what="An invisible zone. When the player walks in it fires On Activated, and On Exited when they leave.",
                how="Add a Link and drag in what it should switch. This one raises the pillars behind it.",
                sound="MMFeedbacksDemoSnap", vfx="HCFX_Flash_01", vfxScale=0.6f, freeze=0.03f, fov=-3, push=0.2f, shake=0.12f, chroma=0.2f, squash=0 },
            new Spec{ name="Teleporter", category=OnEntry, accent=Violet, style=GddJuiceStyle.Teleport,
                what="Instantly moves the player to its Destination point.",
                how="Move the child 'Teleport Destination' to where the player should appear. This one drops you in the plaza.",
                sound="NVTeleport", vfx="HCFX_Appear_01_Start", vfxScale=0.8f, freeze=0.08f, fov=12, push=-0.6f, shake=0.3f, flash=0.6f, chroma=0.7f, slow=0.3f, slowDur=0.35f, squash=0.6f,
                buildUp=true, radius=5f, loop="SFX_Buff_Loop", idleVfx="HCFX_Gathering_Star", idleScale=0.7f },
            new Spec{ name="Next Scene Portal", category=OnEntry, accent=new Color(0.5f,0.38f,1f), style=GddJuiceStyle.Portal,
                what="Loads the next scene in the build list when the player enters. From the last scene it goes back to the first.",
                how="Order scenes in File > Build Profiles. From here it opens Scene 0.",
                sound="SFX_Lightning_Cast", vfx="HCFX_Appear_02_Start", vfxScale=1f, freeze=0.1f, fov=15, push=-0.8f, shake=0.4f, flash=0.9f, chroma=0.8f, squash=0.6f,
                buildUp=true, radius=7f, loop="SFX_Smoke_Bombs_Psychic_L", idleVfx="HCFX_Gathering_Energy", idleScale=0.9f },
            // On touch
            new Spec{ name="Bounce Pad", category=OnTouch, accent=new Color(0.3f,0.85f,1f), style=GddJuiceStyle.Bounce,
                what="Launches the player into the air on contact.",
                how="Change Launch Velocity for higher or angled launches.",
                sound="FeelBounceJump", vfx="HCFX_Smoke_03_Floor", vfxScale=0.9f, freeze=0.05f, fov=11, push=-0.55f, shake=0.4f, chroma=0.35f, squash=1f },
            new Spec{ name="Hazard", category=OnTouch, accent=Red, style=GddJuiceStyle.Hurt,
                what="Hurts the player while they stand on it. When health runs out the scene restarts.",
                how="Amount sets the damage, Repeat Every sets how often it hits while you stay on it. Heal in Pickups.",
                sound="SFX_Debuff_Cast", vfx="HCFX_Hit_04", vfxScale=0.7f, fov=0, push=0, shake=0, chroma=0, squash=0.4f, idleVfx="HCFX_Sign_ExclamationMark_Loop", idleScale=0.6f },
            new Spec{ name="Health Collectible", category=PickUp, accent=Green, style=GddJuiceStyle.Heal,
                what="Touch it to heal. It disappears once collected.",
                how="Change Amount on its Heal power definition to heal more or less.",
                sound="SFX_Heal_Cast", vfx="HCFX_Heart_Get", vfxScale=0.8f, freeze=0.06f, fov=-5, push=0.3f, shake=0.12f, flash=0.15f, chroma=0.2f, squash=0, idleFloat=true },
            new Spec{ name="Score Collectible", category=PickUp, accent=Gold, style=GddJuiceStyle.Collect,
                what="Touch it to add to the score. It disappears once collected.",
                how="Grab several in a row: the pitch climbs with each one in the combo.",
                vfx="HCFX_Orb_01_Get", vfxScale=0.7f, freeze=0.045f, fov=-3, push=0.15f, shake=0.1f, chroma=0.15f, squash=0, idleFloat=true },
            // On a timer
            new Spec{ name="Timer", category=OnTimer, accent=Blue, style=GddJuiceStyle.Timer,
                what="Fires On Activated every few seconds, or once as a countdown.",
                how="Add a Link to switch things on a beat. Untick Repeat for a one-shot countdown.",
                sound="MMSequencingNote2", vfx="HCFX_Flash_03", vfxScale=0.35f, fov=0, push=0, shake=0, chroma=0, squash=0.3f },
            new Spec{ name="Spawner", category=OnTimer, accent=Orange, style=GddJuiceStyle.Spawn,
                what="Creates copies of a prefab, up to a limit, and cleans them up after their lifetime.",
                how="Automatic spawns on its own interval. Turn it off and link a button or trigger to spawn on demand.",
                sound="Pop1", vfx="HCFX_Appear_03", vfxScale=0.6f, fov=-1.5f, push=0.06f, shake=0.06f, chroma=0.1f, squash=0.4f },
            new Spec{ name="Rotating Object", category=OnTimer, accent=new Color(0.7f,0.8f,1f), style=GddJuiceStyle.Ambient,
                what="Spins, and can bob up and down. Pure motion with no events.",
                how="Edit Rotation Per Second. Set Bob Height above 0 to make it float.",
                fov=0, push=0, shake=0, chroma=0, squash=0 },
            // Press a key
            new Spec{ name="Interact Button", category=OnKey, accent=Mint, style=GddJuiceStyle.Interact,
                what="Walk up and press E (controller X) to fire it. The prompt at the bottom of the screen appears by itself.",
                how="Set Action Label to the verb. Add a Link to choose what it switches.",
                sound="SwitchHeavy1", vfx="HCFX_Hit_02", vfxScale=0.6f, freeze=0.05f, fov=-3.5f, push=0.18f, shake=0.2f, chroma=0.2f, squash=0.6f },
            // Pick up and use
            new Spec{ name="Hand Pickup", category=PickUp, accent=Gold, style=GddJuiceStyle.Equip,
                what="Press E to carry it in the right hand. Q drops it. This one uses the 'Stay On Ground' drop rule.",
                how="Add Pickup Return and pick a Drop Rule asset to choose what happens after it's dropped.",
                sound="Pop1", vfx="HCFX_Orb_02_Get", vfxScale=0.8f, freeze=0.05f, fov=-4, push=0.2f, shake=0.15f, flash=0.12f, chroma=0.25f, squash=0.45f, idleFloat=true },
            new Spec{ name="Hat Pickup", category=PickUp, accent=Pink, style=GddJuiceStyle.Equip,
                what="Snaps onto the player's head. Drop it and it pops straight back to its stand ('Return Right Away').",
                how="Swap the cube for any model, but keep the Pickup function on the root object.",
                sound="Award1", vfx="HCFX_Blessed_01_Small", vfxScale=0.8f, freeze=0.06f, fov=-5, push=0.25f, shake=0.15f, flash=0.15f, chroma=0.25f, squash=0.5f, idleFloat=true },
            new Spec{ name="Chest Pickup", category=PickUp, accent=new Color(0.55f,0.8f,1f), style=GddJuiceStyle.Equip,
                what="Worn on the chest, like armor or a badge. Dropped, it waits 6 seconds, then dissolves and returns.",
                how="Adjust Attachment Position to sit it exactly where you want on the body.",
                sound="Velcro1", vfx="HCFX_Blessed_02_Small", vfxScale=0.8f, freeze=0.05f, fov=-4, push=0.2f, shake=0.15f, flash=0.12f, chroma=0.2f, squash=0.45f, idleFloat=true },
            new Spec{ name="Speed Boots", category=PickUp, accent=new Color(0.25f,0.95f,0.9f), style=GddJuiceStyle.Equip,
                what="Wear them to run faster. The boost lasts as long as they're equipped.",
                how="Change Multiplier on the Speed Boost power definition.",
                sound="Boost1", vfx="HCFX_Smoke_01_Floor", vfxScale=0.7f, freeze=0.05f, fov=7, push=-0.35f, shake=0.2f, flash=0.1f, chroma=0.4f, squash=0.45f, idleFloat=true, text="SPEED UP" },
            new Spec{ name="Jump Boots", category=PickUp, accent=new Color(0.5f,0.95f,0.35f), style=GddJuiceStyle.Equip,
                what="Wear them to jump higher.",
                how="Change Multiplier on the Jump Boost power definition.",
                sound="FeelBounceCharge", vfx="HCFX_Smoke_02_Floor", vfxScale=0.6f, freeze=0.05f, fov=5, push=-0.25f, shake=0.2f, flash=0.1f, chroma=0.3f, squash=0.45f, idleFloat=true, text="JUMP UP" },
            new Spec{ name="Blaster Pickup", category=PickUp, accent=new Color(1f,0.5f,0.28f), style=GddJuiceStyle.Blaster,
                what="Carry it, then click to fire toward where the camera looks. Dropped, it waits 6 seconds and returns.",
                how="Shots pass through the creature fences. Edit the power definition for speed and cooldown.",
                sound="PistolLoad1", vfx="HCFX_Orb_03_Get", vfxScale=0.7f, freeze=0.05f, fov=-4, push=0.2f, shake=0.18f, flash=0.1f, chroma=0.25f, squash=0.45f, idleFloat=true,
                useSound="ScifiGunshot1", useVfx="HCFX_Flash_02" },
            new Spec{ name="Snap Collectible", category=PickUp, accent=new Color(0.95f,0.75f,0.3f), style=GddJuiceStyle.Equip,
                what="Touch it and it snaps onto the player: a hand, the head, or any joint you pick. Jiggle makes it wobble as you move.",
                how="Select it in the editor: a ghost body shows exactly how it will be held. Move or rotate the object to adjust.",
                sound="Award1", vfx="vfx_LevelUp_v1_blue", vfxScale=0.6f, freeze=0.06f, fov=-5, push=0.25f, shake=0.15f, flash=0.15f, chroma=0.25f, squash=0 },
            new Spec{ name="Counter Collectible", category=PickUp, accent=new Color(0.35f,0.9f,1f), style=GddJuiceStyle.Collect,
                what="Flies into a HUD counter (icon x 12). Collectibles with the same name share one counter.",
                how="Give it an icon, or press Make Icon From This Object. Grabbed position nudges the HUD icon.",
                sound="Coin1", vfx="HCFX_Orb_01_Get", vfxScale=0.6f, freeze=0.04f, fov=-2.5f, push=0.12f, shake=0.08f, chroma=0.15f, squash=0, text="+1" },
            new Spec{ name="Key Item", category=PickUp, accent=new Color(1f,0.35f,0.35f), style=GddJuiceStyle.Collect,
                what="Flies to the HUD and waits there. A Key Lock with the same key name uses it up.",
                how="HUD icon: a sprite, or a flattened copy of the 3D model parked in front of the camera.",
                sound="Award1", vfx="vfx_LevelUp_v3_orange", vfxScale=0.6f, freeze=0.07f, fov=-5, push=0.3f, shake=0.15f, flash=0.2f, chroma=0.3f, squash=0 },
            new Spec{ name="Key Lock", category=PickUp, accent=new Color(1f,0.55f,0.4f), style=GddJuiceStyle.Interact,
                what="Opens only if you carry its key. Fires On Unlocked and any Link, here the door behind it.",
                how="Key Name must match the Key Item's. Untick Consume Key for keys that open many locks.",
                sound="SwitchHeavy2", vfx="vfx_LevelUp_v2_purple", vfxScale=0.6f, freeze=0.05f, fov=-4, push=0.2f, shake=0.2f, flash=0.15f, chroma=0.2f, squash=0.5f },
            new Spec{ name="Shooter", category=Triggerable, accent=new Color(0.35f,0.6f,1f), style=GddJuiceStyle.Interact,
                what="Fires a copy of a projectile out of its muzzle each time it's activated: a cannonball, a snowball, anything.",
                how="Give it a projectile and a muzzle, then fire it from a Link, a button, a timer or a trigger zone leaving.",
                sound="FeelDuckBoom", vfx="HCFX_Flash_03", vfxScale=0.8f, freeze=0.05f, fov=4, push=-0.3f, shake=0.45f, flash=0.1f, chroma=0.3f, squash=0.6f },
            new Spec{ name="Wearable Body", category=PickUp, accent=new Color(0.75f,0.45f,1f), style=GddJuiceStyle.Equip,
                what="A body you can put on: press E and you become this character, running and jumping with your own moves.",
                how="Right-click any Mixamo FBX > GDD 110 > Add Wearable Body To Level. Press E again to take it off.",
                sound="Award1", vfx="vfx_LevelUp_v4_purple", vfxScale=0.7f, freeze=0.08f, fov=-6, push=0.3f, shake=0.2f, flash=0.3f, chroma=0.4f, squash=0 },
            new Spec{ name="Popup Message", category=Triggerable, accent=new Color(0.3f,0.8f,1f), style=GddJuiceStyle.Pop,
                what="A digital message card that floats above an object or slides in at the top of the screen.",
                how="Shows when you walk close, or switch it On from a Link or event. Set a title, message, color and icon.",
                sound="MMSequencingNote1", fov=0, push=0, shake=0, chroma=0, squash=0 },
            // Creatures
            new Spec{ name="Companion", category=Creatures, accent=Pink, style=GddJuiceStyle.Companion,
                what="Follows the player and never deals damage. Hearts mean it's happy to see you.",
                how="Raise Speed or Stop Distance to change how closely it trails you.",
                fov=0, push=0, shake=0, chroma=0, squash=0.3f },
            new Spec{ name="Dialog NPC", category=Creatures, accent=new Color(0.5f,0.75f,1f), style=GddJuiceStyle.Interact,
                what="Talks in a comic speech or thought bubble that points at its mouth and types the words out.",
                how="Start it by walking close, pressing E, or from an event. Tick Advanced on a line to fire an event or drop an item.",
                sound="MMSequencingNote3", fov=-2f, push=0.1f, shake=0, chroma=0, squash=0.35f },
            new Spec{ name="Hopper Friend", category=Creatures, accent=new Color(1f,0.6f,0.8f), style=GddJuiceStyle.Companion,
                what="An older-style companion that hops around near the camera at random intervals.",
                how="Jump Force and the min/max intervals set how bouncy it is.",
                fov=0, push=0, shake=0, chroma=0, squash=0.3f },
            new Spec{ name="Enemy", category=Creatures, accent=new Color(1f,0.22f,0.3f), style=GddJuiceStyle.Enemy,
                what="Slides toward the player and deals damage on contact. It has Health, so blaster shots defeat it.",
                how="Lower Speed for a friendlier challenge, or raise Contact Damage for a meaner one.",
                sound="SFX_Bomb_Explosion", vfx="HCFX_Explosion_01", vfxScale=0.7f, freeze=0.12f, fov=-6, push=0.45f, shake=0.6f, flash=0.3f, chroma=0.5f, slow=0.25f, slowDur=0.55f, squash=0.6f, text="DEFEATED" },
            new Spec{ name="Hopper Enemy", category=Creatures, accent=new Color(1f,0.4f,0.25f), style=GddJuiceStyle.Enemy,
                what="Enemy script, Jump movement: leaps toward the player in bursts.",
                how="Speed sets both jump strength and how often it jumps.",
                sound="SFX_Bomb_Explosion", vfx="HCFX_Explosion_01", vfxScale=0.5f, freeze=0.1f, fov=-5, push=0.35f, shake=0.5f, flash=0.25f, chroma=0.45f, slow=0.3f, slowDur=0.45f, squash=0.6f, text="DEFEATED" },
            new Spec{ name="Ghost Enemy", category=Creatures, accent=new Color(0.75f,0.4f,1f), style=GddJuiceStyle.Enemy,
                what="Enemy script, Ghost Chase movement: drifts straight at the player and passes through walls.",
                how="Ghosts ignore walls, so this pen holds them in place with its bounds instead.",
                sound="SFX_Bomb_Explosion", vfx="HCFX_Explosion_01", vfxScale=0.5f, freeze=0.1f, fov=-5, push=0.35f, shake=0.5f, flash=0.25f, chroma=0.45f, slow=0.3f, slowDur=0.45f, squash=0.6f, text="DEFEATED" },
            new Spec{ name="Roller Enemy", category=Creatures, accent=new Color(1f,0.6f,0.2f), style=GddJuiceStyle.Enemy,
                what="Enemy script, Roll movement: shoves itself toward the player with physics.",
                how="A heavier Rigidbody rolls slower and hits harder.",
                sound="SFX_Bomb_Explosion", vfx="HCFX_Explosion_01", vfxScale=0.5f, freeze=0.1f, fov=-5, push=0.35f, shake=0.5f, flash=0.25f, chroma=0.45f, slow=0.3f, slowDur=0.45f, squash=0.6f, text="DEFEATED" },
            // Triggerables
            new Spec{ name="Toggle Objects", category=Triggerable, accent=Green, style=GddJuiceStyle.Pop,
                what="Makes objects appear when switched On and vanish when Off, with a quick grow or shrink.",
                how="Drag objects into its list, then drag it into a Link. Bridges, walls, rewards, lamps.",
                sound="MMSequencingNote1", vfx="HCFX_Appear_04", vfxScale=0.8f, freeze=0.03f, fov=-2.5f, push=0.12f, shake=0.12f, chroma=0.15f, squash=0 },
            new Spec{ name="Mover", category=Triggerable, accent=new Color(0.4f,1f,0.6f), style=GddJuiceStyle.Pop,
                what="Slides and turns an object to its On pose, holds it, and plays back to the start when Off.",
                how="Set Move By and Rotate By. Auto Return After makes it go back by itself.",
                sound="SwitchMed1", vfx="HCFX_Smoke_02_Floor", vfxScale=0.4f, fov=-1.5f, push=0.08f, shake=0.12f, chroma=0.1f, squash=0 },
            new Spec{ name="Play Timeline", category=Triggerable, accent=new Color(0.45f,0.7f,1f), style=GddJuiceStyle.Pop,
                what="Plays a Timeline forward and holds the last frame. Off plays it backward. Reset rewinds and runs it again.",
                how="Link with Toggle for a lift, or with Reset for a beat-the-clock gate.",
                sound="SwitchHeavy2", vfx="HCFX_Flash_04", vfxScale=0.5f, freeze=0.04f, fov=-3, push=0.15f, shake=0.18f, chroma=0.2f, squash=0 },
            new Spec{ name="Camera Switch", category=Triggerable, accent=new Color(0.6f,1f,0.6f), style=GddJuiceStyle.Pop,
                what="Cuts to another Cinemachine camera while On, with an optional grainy security-feed look.",
                how="Put a trigger zone in a room and link it: Turn On on entry, Turn Off on exit.",
                sound="CameraShutter1", freeze=0.05f, fov=0, push=0, shake=0.1f, flash=0.5f, chroma=0.4f, squash=0 },
        };

        [MenuItem("Tools/GDD 110/Juice/Rebuild Juice Assets", false, 200)]
        public static GddJuiceSettings BuildAssets()
        {
            Folder(Root + "/Blocks");
            Folder(Root + "/Resources");
            Folder(PostDir);
            Folder(TexDir);
            Folder(MatDir);

            var settings = Asset<GddJuiceSettings>(Root + "/Resources/GddJuiceSettings.asset");
            settings.blocks.Clear();
            foreach (var s in Specs())
            {
                var b = Asset<GddBlockInfo>(Root + "/Blocks/" + s.name + ".asset");
                b.blockName = s.name;
                b.category = s.category;
                b.menuPath = s.category == Creatures && s.name.Contains("Enemy") && s.name != "Enemy" ? "Add Component > Enemy (course script)" :
                             s.name == "Hopper Friend" ? "Add Component > Companion Creature (course script)" :
                             s.category == Triggerable ? "GameObject > GDD 110 > Triggerables > " + s.name :
                             "GameObject > GDD 110 > Create > " + s.name;
                b.accent = s.accent;
                b.whatItDoes = s.what;
                b.howToUse = s.how;
                b.style = s.style;
                b.sound = Sfx(s.sound);
                b.burstVfx = Fx(s.vfx);
                b.burstScale = s.vfxScale;
                b.freezeFrame = s.freeze;
                b.fovPunch = s.fov;
                b.cameraPush = s.push;
                b.shake = s.shake;
                b.flash = s.flash;
                b.chromatic = s.chroma;
                b.slowMo = s.slow;
                b.slowMoDuration = s.slowDur;
                b.squash = s.squash;
                b.floatingText = s.text;
                b.useSound = Sfx(s.useSound);
                b.useVfx = Fx(s.useVfx);
                b.idleFloat = s.idleFloat;
                b.idleVfx = Fx(s.idleVfx);
                b.idleVfxScale = s.idleScale;
                b.buildUpOnApproach = s.buildUp;
                b.buildUpRadius = s.radius;
                b.buildUpLoop = Sfx(s.loop);
                EditorUtility.SetDirty(b);
                settings.blocks.Add(b);
            }

            settings.postProfile = PostProfile(PostDir + "/Juice Post Profile.asset", false);
            settings.buildUpProfile = PostProfile(PostDir + "/Juice Build-up Profile.asset", true);
            settings.panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            settings.lineMaterial = LineMaterial();
            settings.tooltipOpen = Sfx("Pop1");
            settings.tooltipClose = Sfx("Button1");
            settings.hurtSound = Sfx("CombatPunch1");
            settings.healSound = Sfx("SFX_Heal_Cast");
            settings.scoreSound = Sfx("Coin1");
            settings.arriveSound = Sfx("NVTeleport");
            settings.arriveVfx = Fx("vfx_LevelUp_v1_blue");
            settings.hurtVfx = Fx("HCFX_Hit_01");
            settings.poofVfx = Fx("HCFX_Smoke_02_Air");
            settings.spawnPopVfx = Fx("HCFX_Flash_03");
            settings.linkPulseVfx = Fx("HCFX_Flash_03");
            settings.heartIcon = Icon("Heart Icon", HeartPixel);
            settings.angerIcon = Icon("Anger Icon", AngerPixel);
            settings.heartBurstVfx = Fx("HCFX_Heart");
            settings.happySound = Sfx("FeelBlobNote2");
            settings.angrySound = Sfx("FeelBarbariansGrunt3");
            settings.deathVfx = Fx("HCFX_Explosion_02");
            settings.deathSound = Sfx("NVGameOver");
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[GDD Juice] Built " + settings.blocks.Count + " block infos.");
            return settings;
        }

        // ---------- generated icons ----------

        static Color HeartPixel(float x, float y)
        {
            x *= 1.25f; y = y * 1.25f + 0.25f;
            float a = x * x + y * y - 1f;
            float f = a * a * a - x * x * y * y * y;
            if (f > 0) return Color.clear;
            float rim = Mathf.Clamp01(-f * 6f);
            return Color.Lerp(new Color(0.75f, 0.1f, 0.35f), new Color(1f, 0.45f, 0.65f), rim);
        }

        static Color AngerPixel(float x, float y)
        {
            // Four bent strokes meeting toward the middle: the classic "angry vein" mark.
            foreach (var q in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) })
            {
                if (x * q.x < 0.12f || y * q.y < 0.12f) continue;
                float d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), q * 0.95f) - 0.6f);
                if (d < 0.1f) return new Color(1f, 0.2f, 0.18f);
                if (d < 0.15f) return new Color(0.45f, 0f, 0f);
            }
            return Color.clear;
        }

        static Texture2D Icon(string name, System.Func<float, float, Color> pixel)
        {
            string path = TexDir + "/" + name + ".png";
            const int size = 128;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    // 3x3 supersampling for smooth edges
                    Color c = Color.clear;
                    for (int sy = 0; sy < 3; sy++)
                        for (int sx = 0; sx < 3; sx++)
                            c += pixel((px + (sx + 0.5f) / 3f) / size * 2f - 1f, (py + (sy + 0.5f) / 3f) / size * 2f - 1f);
                    t.SetPixel(px, py, c / 9f);
                }
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), path), t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------- look ----------

        static Material LineMaterial()
        {
            string path = MatDir + "/Juice Tether.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Sprites/Default")) { name = "Juice Tether" };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static T Override<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (!p.TryGet<T>(out var c))
            {
                c = p.Add<T>(true);
                c.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(c, p);
            }
            c.active = true;
            return c;
        }

        static VolumeProfile PostProfile(string path, bool buildUp)
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
            var vig = Override<Vignette>(p);
            var ca = Override<ChromaticAberration>(p);
            var col = Override<ColorAdjustments>(p);
            if (!buildUp)
            {
                var bloom = Override<Bloom>(p);
                bloom.intensity.Override(0.55f); bloom.threshold.Override(1.05f); bloom.scatter.Override(0.65f);
                var tone = Override<Tonemapping>(p);
                tone.mode.Override(TonemappingMode.Neutral);
                col.postExposure.Override(-0.1f); col.contrast.Override(12f); col.saturation.Override(14f);
                vig.intensity.Override(0.18f); vig.smoothness.Override(0.5f); vig.color.Override(new Color(0.12f, 0.14f, 0.25f));
                ca.intensity.Override(0.02f);
            }
            else
            {
                var lens = Override<LensDistortion>(p);
                lens.intensity.Override(-0.28f); lens.scale.Override(1f);
                col.saturation.Override(25f); col.colorFilter.Override(new Color(0.9f, 0.85f, 1f));
                vig.intensity.Override(0.48f); vig.smoothness.Override(0.6f); vig.color.Override(new Color(0.12f, 0.02f, 0.25f));
                ca.intensity.Override(0.55f);
            }
            EditorUtility.SetDirty(p);
            return p;
        }
    }
}
