# GDD110_Juice

Optional, instructor-only feel layer for the GDD 110 project. It depends on Feel (More Mountains) and VFX_Klaus, so it stays out of the student starter project. No course scene was edited to use it.

## How it works
- `GddJuiceDirector` boots into every scene at play time. It adds the post look, camera juice on every Cinemachine camera, the button-prompt bar, and juice on every block it recognizes.
- Toolkit blocks get `GddJuiceResponder`. The original course scripts in Scene 0-3 (TriggerActivatedInteraction, Enemy, CompanionCreature, Destructible, PickupManager, Spawner) get `GddLegacyHook`.
- A moment combines Feel (freeze frame, slow motion, flash, URP chromatic aberration and vignette), sound, VFX, Cinemachine impulse, FOV punch and dolly, squash, and floating text. Moments fade with distance from the player.
- Portals and teleporters build up on approach. New scenes flash in. Collect sounds climb in pitch during combos. Dying plays a slow-motion K.O. before the restart (the player rig's Scene Actions now waits 1.2 s).
- `GddActionPrompts`: bottom-of-screen key prompts for what you can do right now (E pick up, Click fire, Q drop, dropped item countdown). Controller names when a gamepad is connected.
- `GddLinkVisualizer`: game-view arcs for every Link, with a pulse that runs to the target when it fires (showcase only, or everywhere via settings).
- `GddCreatureMood`: hearts and happy hops for friendly creatures, a throbbing anger mark, red flashes and grunts for enemies.
- `GddCreaturePen`: keeps creatures inside a box whatever their movement style, lets blaster shots through the fence, respawns defeated ones.

## Toolkit additions (in GDD110_Course/Toolkit, no Feel dependency, usable by students)
- `GddLink` (Add Function > Link To Objects): drag any GDD object in; choose Activate / Turn On / Turn Off / Toggle / Reset for activation and for exit, plus a delay. Scene-view gizmo arcs show every connection with its action.
- `GddTriggerable` base, plus Toggle Objects, Mover (door, lift), Play Timeline (forward, hold, backward, reset and replay), Camera Switch (Cinemachine cut with optional security feed).
- `GddDropRule` asset + `GddPickupReturn`: stay on ground, return right away, or wait N seconds then dissolve home (default 6).
- `GddContactEffect.repeatEvery`: hazards and heal zones tick while you stand in them.
- `GddScenes`: Next Scene wraps to the first scene from the last one, and scenes outside the build list can still restart in the editor.

## Editing
- `Blocks/*.asset` (GddBlockInfo): tooltip text, category, accent, sound, VFX, freeze, FOV punch, shake, flash, slow-mo, squash, idle effect, build-up.
- `Resources/GddJuiceSettings.asset`: master on/off, tooltips everywhere, links everywhere, prompt bar on/off, shared sounds, creature icons.
- `Tools > GDD 110 > Juice`: Rebuild Juice Assets, Build Block Showcase Scene, Toggle Juice, Toggle Tooltips In Every Scene. Rebuilding overwrites block assets from `Editor/GddJuiceSetup.cs` and the scene from `Editor/GddShowcaseBuilder.cs`.
- `Shaders/GddTrainingFloor.shader`: world-space training floor (checker tiles, bevels, grid and glowing accent lines) used by all showcase surfaces.

## Block Showcase
`Scenes/Block Showcase.unity`: a plaza with six districts, one per way a block gets activated: On entry, On touch, On a timer, Press a key, Pick up and use, Creatures. Triggerables sit beside what switches them, joined by visible links. Guide cards spring open as you walk up (two at a time).
