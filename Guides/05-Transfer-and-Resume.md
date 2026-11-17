# Transfer GDD 6.6 to another computer

Extract the transfer archive and open the extracted **GDD 6.6** folder in Unity Hub with editor **6000.6.0f1**. Unity rebuilds its Library on first open and may download the packages listed in the checked-in manifest and lock file. Keep the Unity `.meta` files with their assets.

Open **Tools → GDD 110 → Start Here → Play from Title**. **Start game** plays the wolf intro and enters the rebuilt Playground, **The Fourth Bell**. **Block Showcase** opens the original interactive library.

## Where to resume

| Item | Location |
|---|---|
| Design, dialogue, block selection, music cues, and secrets | `Guides/04-After-the-Last-Bell.md` |
| Editable Blender rig in a standing rest pose | `Character Source/Wolf - Humanoid.blend` |
| Rig fitted to the manually revised crouch | `Character Source/Wolf - Pose-fit Rig.blend` |
| Wolf prefab, secondary-motion component, FBX and atlas | `Assets/GDD 110 Project Files/The Fourth Bell/Wolf` |
| Low Poly Ultimate Pack and Epic Toon FX selections | `Assets/GDD 110 Project Files/Third-Party Art/Campaign Art Packs` |
| Shared player appearance | **Tools → GDD 110 → Global Character** |
| Production example scene to replace | `Assets/GDD 110 Project Files/Course/Scenes/The Fourth Bell.unity` |
| Interactive block library | `Assets/GDD 110 Project Files/Block Showcase/Scenes/Block Showcase.unity` |

Scene 2 holds three editable area roots. Each area groups the blockout, platforms, solid props, decoration, lighting, hazards, pickups, and interactions in the Hierarchy. Mint labels identify pickups, with their action shown above the item. Warm labels mark hazards. NPCs and audio have their own groups.

The palette uses pine and slate for roughly 70% of each environment, copper and its darker shades for 20%, and pale mint for 10% of focal lights and hardware. Props and cliff silhouettes were scattered with fixed seeds around hand-placed stairs, landmarks, and interaction routes.

The music stems already live in `Assets/GDD 110 Project Files/Wesson Assets/Audio/Music`. The Showcase also supplies the radio sync behavior, Mimic encounter, portal-gun behavior, and BRAAAM. The case reuses them along the route. Green Synthwave loops at the title and Victory Overdramatized plays once on Win.

## Editing the wolf

Open Wolf - Pose-fit Rig.blend in Blender 5.3 to pose the revised crouched character. The prepared trousers and sneaker were mirrored across X. Wolf - Humanoid.blend is the independent standing calibration mesh for export. Both include the packed atlas. The FBX contains skinned meshes and the deform skeleton. The global profile points to the Wolf prefab variant. Its Wolf Secondary Motion component exposes the neck lean and the spring strength, damping, and angle limits for the sleeves, ears, three mane sections, and hood. These settings live on the wolf prefab. Unity retargets the course's animation controller with the imported Humanoid Avatar. Unity Play Mode checks confirmed the visible full-size mesh, locomotion, shield attachment, checkpoints, both jumps, and the route to the Win music. Keep the current FBX filename and its Unity meta file when regenerating the export. Export the armature and meshes with **-Z Forward**, **Y Up**, no leaf bones, and no baked actions. Set Blender’s scene units to Metric at scale 1.0 and FBX **Apply Scalings → FBX Units Scale**. Unity’s Humanoid evaluation can shrink the rendered mesh if the FBX stores the unit conversion on the mesh objects. Recheck the Avatar mapping and material in Unity after re-exporting. The trousers and sweater hem share hip motion, and the sleeve caps blend into the chest weights. The game uses Papercuts for display headings and Very Popular for body text and action prompts. Field Gems and the Return Key update both the collection display and score. The wolf prefab owns the equipment sockets and gear pose offsets.

## Playtest the case

1. **Assembly Hall** — Check the stairs and mezzanine from both sides. Test mantling the practice block and physically entering the powered service lift.
2. **The Culvert** — Try the elevated west route, the central spike crossing, and the east bank. Collect the key, unlock the orchard lift, and test **R** after the west checkpoint.
3. **Signal Orchard** — Try both side banks and the sunken grove. Test reaching the bell crest and entering the crossing.
4. **Movement tuning** — Select the shared player armature in Scene 2. In **Third Person Controller**, edit **Jump Feel** for coyote time, jump buffering, apex gravity, and mantle height.


## GDD Base 2026

The campaign and Block Showcase are installed in this Unity Version Control workspace. The build scene list starts with Game Start and includes the intro, Playground, Win, and Block Showcase. Feel's loading templates and save/load example live under Assets/GDD 110 Project Files/Plugins/Feel/Support Scenes. The campaign uses the course scene transition and does not need those sample scenes in the build list. The vendor scene basenames are retained for their name-based loaders.

The campaign objective sits below the stitched hearts. Press **H** or **D-pad Up** to show or hide the next step. Hold right mouse or LT to aim the pulse tool and fire with left mouse or RT. Left mouse or RT also swings the Keeper blade and sends out its wave. The shield blocks Custodian attacks and the helmet halves their damage. The Portal Linker connects the shot location to the Science Fair landing and creates a return portal. Ringing the return bell reveals the green EXIT portal.
