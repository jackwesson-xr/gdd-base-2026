# After the Last Bell

This document records the original story and staging concepts. For the current playable layout and test steps, see the [playtest guide](05-Transfer-and-Resume.md).

## Playable case in GDD 6.6

Scene 2 follows three objectives. Power the service lift in West Yard, find the return key in the Culvert, and ring the bell in Signal Orchard. The notices tell the short version in play. The longer material below supplies dialogue and staging references for future revisions. [Playtest and resume guide](05-Transfer-and-Resume.md).

## Case 01 — The Fourth Bell

At 3:17 p.m. the last bell at Briar Glen High rings four times. On the fourth ring, every locker in the west hall opens six inches, the hallway clock runs backward, and a wet footprint appears on the ceiling. By the time anyone looks up, the intercom is blaming marching-band practice.

The only student who hears someone inside the bell is the orange-hoodied kid who already has to explain his ears on picture day. He records the sound on a pocket cassette recorder. Playback catches two knocks, a chirp, and a breath between them. Something was answering. When he sees the same three-beat rhythm blinking on the maintenance lift, he follows it.

The protagonist is a teenage werewolf and an amateur field investigator. His amber eyes and sharp blue teeth make him look more confident than he feels. He takes meticulous notes, lies badly about getting home on time, and always tries to establish whether a creature is dangerous, frightened, or simply annoyed. The cold open uses his school-day perspective. The game starts when he actively decides to follow the clue.

**Player promise.** Follow an odd clue, make a small deduction, and use a physical block to prove it. The game progresses from late-afternoon school grounds to impossible infrastructure to a piece of another world. It plays as a compact platforming case with a beginning, a reversal, and an ending that opens up future investigations.

## What happened

The school was built over a municipal waystation for creatures that cross between worlds. The first three rings mark the end of class. The fourth was the waystation's old all-clear. Each crossing uses a matched call and reply, like a simple two-way radio. The school clock was replaced during a renovation and now keeps repeating the outbound call. The waystation's automated Custodians are following a quarantine rule and will keep the exits shut until they hear both signals.

An adolescent cryptid the records call a **Bell Moth** came through when the signal was incomplete. It borrows sounds to navigate. It has imitated the bell, pulled loose a brass return key to draw attention, and left a trail of damp prints from the school yard down into the culvert. The key looked like stolen loot. It was the only object the moth knew a human might follow.

The mystery moves through three questions. What did the fourth ring open? What was the creature trying to tell us? How do we get it home? Answer each by doing something the player can see and repeat.

## Opening and ending

On the title screen the wolf sits on the edge of a service ramp with his recorder. For the first animation pass he can stand and listen using the shared Humanoid idle. A fluorescent lamp hums. Every few cycles his attention goes to the lift. Start game clears the title UI while the same character and shot are still on screen. The recorder plays the fourth ring. A light switches on behind the service gate and he moves toward it. The existing title-to-intro handoff can stage this beat, with the first playable frame pointing at the first clue.

The end plays in the existing Win scene. The recorder has captured the moth's return call. He labels the original tape “Case 01 — assisted crossing,” then gets a text from a classmate asking if he borrowed the marching band's tuba. Under the text somebody has drawn a map to a laundromat with a coastline. The moth's case is resolved. The town still has more crossings.

## Route and encounter design

### 1. West Yard

**Objective:** Follow the prints to the service lift and bring its power back.

The starting yard reads as a familiar place at an unfamiliar hour. A school facade, bus, wood bench, cones, crates, and a maintenance fence give the player a sense of scale. From the spawn, a lit notice board and the yellow service lift are visible. Footprints lead across short, generous steps. Missing the practice jump lands the player on the same pavement.

An interactable copy of the detention slip introduces reading and closing a card. A low crate route introduces jumping. At the first switch, the player follows the power conduit to the lift with their eyes. Its last section disappears around a corner, so the switch triggers a short focus on the lift light and the opening gate, then hands control back. Inside the lift, hold briefly on the stuttering floor indicator. Fade and transfer to the Culvert. The first checkpoint arrival faces the route.

**Billboard:** “Move to the yellow landing. Jump the gap. Try the service switch.” Display the current input binding alongside the relevant verb.

**Prefab use:** Showcase interact button, trigger zone, dialog or popup, mover or toggle, focus moment, and a Level Block transition. Low Poly Ultimate Pack candidates include `building-school`, `bus-school`, `bench-wood`, and `crate-box`.

### 2. The Culvert

**Objective:** Cross the maintenance traps and recover the brass return key.

From the lift, a safe observation platform shows one entire spike cycle. The next run uses the actual spikes from the Block Showcase Hazard Run, with readable warning and withdrawal intervals. A stable landing divides two jumps. The player can cross the same gap with a well-timed launch pad after following the optional safer bridge route to see where it lands. A moving service platform combines timing and route reading. Put a checkpoint between the spike run and the moving platform.

A small light under a grating is the first glimpse of the Bell Moth. It copies the player's recorder chirp and points toward a second pipe. Its footprints continue behind the hazard line. A maintenance log reveals the missing reply. The player retrieves a large brass key, carries it to a socket, and sees a gate drain or retract in a short focus reveal. Walk through the unlocked lock chamber to take the service lift into the next world.

**Billboard:** “Watch one full cycle. Cross when the spikes retract. Land on the marked islands.”

**Prefab use:** Actual Showcase spike hazard and its timing assembly, timer, bounce pad, mover, health pickup, key collectible, key lock, popup message, linked gate, focus moment, and level transition. Source pipes, railings, bridge spans, and utility pieces from the Low Poly pack. A cooldown or checkpoint reset must prevent repeat damage from stacking every frame.

### 3. The Signal Orchard

**Objective:** Reach the return bell and answer the moth's call.

Beyond the lift is a twilight pocket world where the stone branches hold bells and pieces of the school are stuck in the rock. A distant lit bell tower sets the heading. The nearest shelter presents equipment beside safe target practice. A pack helmet or shield is visible when equipped. The compact melee option uses a recognizable pack sword as a borrowed keeper's blade. A ranged field tool uses a pack model picked for a clear held silhouette. Any placeholder functional prefab should receive the mesh through its visual child and keep the Showcase interaction and socket behavior.

A Custodian shows a clear wind-up and melee arc. A second signals before firing a slow, legible projectile, with room between cover pieces. An optional clue hints at a safer service route. Past the fight, the player connects the return key and uses the bell switch; the focus shot shows the answer light at the far end before it shows the moth crossing. That clears the final gate to the Win portal. The friendly moth's cues, targeting layers, and staging should make fighting it unnecessary and visually implausible.

**Billboard:** “Equip gear at the shelter. Use cover when a Custodian charges. Reach the return bell.”

**Prefab use:** Showcase armor, sword or slash, shooter, projectile, health, dialogue, enemy and companion behaviors, linked bell control, camera focus, and a Next Scene Portal with Win explicitly assigned. Low Poly Ultimate Pack includes `shield-medieval`, `sword`, `helmet-us`, `bridge-medieval-stone`, and `chest`. Pick the armor silhouette that reads clearly on the wolf after fit testing. Use Epic Toon FX for player muzzle flash, projectile trail, hit burst, and enemy charge and impact. Give friendly and hostile effects distinct colors and shapes. Keep the reticle, landing edges, and attack silhouettes legible during a hit.

## Interactable field notes

Use short cards on real objects. Give the player the verb and reward in the interaction prompt. Required instructions live at decision points. Optional sightings reward curiosity with a joke, a hint, or corroboration. Hearing-impaired players can get the full deduction from icon patterns and text cards.

| Object | Text or beat | What the player learns |
|---|---|---|
| Detention slip, pinned to the yard notice board | “Reason for detention: growling at a substitute. Student response: the intercom growled first.” | The protagonist has been investigating since class. Safe first interaction. |
| Recorder, near the first footprint | *Four rings. Two knocks. One chirp. A gap.* “It keeps leaving room for an answer.” | The repeated pattern will matter. Pair sound with a static pattern of marks. |
| Groundskeeper's lift checklist | “If the basement button says SKY, report it to Facilities. If it says HOME, go upstairs and try again tomorrow.” | The lift connects incompatible places. Follow the yellow conduit to the switch. |
| Culvert incident tag, before the spike run | “We heard the bell. We shut the gate. The small one came back twice.” | The safety system is responsible for the trap. Show the timing instruction separately. |
| Lost Property box, after the moving platform | “One brass return key. One permission slip for a field trip dated 1958. One very offended moth.” | Pick up the key. Match its return-bell symbol to the lock. |
| Orchard shelter memo | “Custodians won't cross the yellow line. Borrow a shield. Put it back before it learns your name.” | This is a safe space to practice gear controls. |
| Keeper's return plate, beside the bell | “One call means a door is open. One reply means it is safe to go through.” | Ring the reply. The moth immediately acts on it. |
| Final recorder log | “Case closed. Subject found alive. Suspect still owes me a brass key. I hope he keeps it.” | The protagonist understands he helped a stranded kid like himself. |

## Unity layout and teaching value

Keep the Block Showcase as the editable zoo and Scene 2 as the applied example. Turn the actual Showcase examples used above into prefab assets, preserve their base configuration, and use prefab instances in both places. Put wires to scene objects on instances. Where convenient, keep complete linked assemblies together so a new level author can drag in a working trap and adjust timing or damage. Use prefab variants for differences that should be reused. Keep set dressing distinct from collider boundaries and trigger ownership.

Put West Yard, Culvert, and Signal Orchard under separate Level Block roots. Each holds its arrival marker, checkpoint markers, scenery, lights, encounters, and local goals. The player, camera, transition UI, and level supervisor sit outside those roots. A level transition validates the destination, blocks repeat triggers, activates destination content under cover, transfers the player and their held gear, updates the checkpoint, and deactivates the previous root. A failed transfer leaves the current stage usable. Scene loading happens at the final portal. Respawn should restore a viable local route or preserve completed locks, so losing health after spending a key cannot strand the player.

Use hearts as the Inspector unit for enemy damage. Convert at the existing health-system boundary. Give melee and projectile damage separate editable fields, with a suggested introductory tuning of one heart per melee hit and half a heart per ranged hit. Check the actual HUD can represent half hearts before committing that tuning. Use the existing Juice layer for door cues, pickup feedback, hit response, and arrivals. Reserve full-screen effects and strong camera motion for major moments.

An initial playthrough should confirm that the title hands off to the open yard, every critical step has a visible goal, the wolf can clear all required jumps, fail states restore a playable route, gear travels with the player, both damage sliders work, and the final portal reaches Win. Separately open the Showcase and confirm each promoted block still demonstrates its original behavior.

## Character rig and handoff

Use the orange hoodie, silhouette, and baked illustrated surface from the supplied Blender model. The sculpted pose can become a title or promotional pose after the character has a neutral base. Work in a new Blender file. Inspect whether the current bend lives in evaluated mesh geometry or in rig transforms, whether both shoes and hands exist as full meshes, and whether the hood, cuffs, and trousers retain enough coverage to open out cleanly. Repose in Blender before committing a Humanoid rest pose. Keep the UV mapping and any packed atlas.

Build a Blender deform armature with a hips/spine/chest/neck/head chain and matched upper/lower arms and legs, hands, feet, and toes. Add only the finger chains supported by the mesh. Ears and jaw can have separate bones outside the required Humanoid mapping. Fit both sides to the actual stylized proportions, normalize skin weights, and test shoulder lift, elbow bend, hand grip, crouch, knee bend, and the sneakers at toe-off. Export the mesh and deform bones to FBX, then verify Unity's Humanoid avatar and T-pose. Run the existing idle, walk, run, and jump and confirm the weapon and shield sockets stay in the right place through motion. Set the global character only after the imported avatar and retargeted animation are checked. Rigging and weighting are authored in Blender, with Unity handling the animation retargeting.

## Music direction — one song heard from different places

Use the existing Showcase track **Feel the Beat the City's Waking Up (Remastered)**. Both files are already in the project. The *Without Lead Vocal* mix is the score and *Lead Vocal* is the positional source. The player should be able to stand between two radios and hear one synchronized vocal, with the point of origin changing as they move.

- **Intro to first playable beat.** Let the school bell, recorder, character footsteps, and outside air carry the cutscene. Bring the instrumental in after the player has control and crosses the first yard landmark, ideally as the school opens up visually. Give the entrance a short fade on a musical phrase. The cue should happen once and have a fallback for direct scene entry or players who skip the intro. A checkpoint retry should keep the track running.
- **Radios as world objects.** Use the existing `GddSyncedRadio` and the exact instrumental reference the Showcase uses. Have the vocal roll off in 3D. Give the small radio band-limited EQ, firm compression, and a restrained touch of speaker saturation. Test the imported clips for matching downbeats and loop length. Prefer a shared DSP start and loop phase if the existing radio drifts, clicks, or starts a syllable late. Keep phase while the radio is inaudible or a Level Block is inactive.
- **The show in the West Yard.** Put a small outdoor school showcase behind the gym on the real-world route. It answers the announcement about marching-band practice and gives the player a bright pocket of normal life before they go underground. The show is already in progress. As the player approaches, crossfade from the score to a crisp, close-sounding stage presentation with the same instrumental phase, the clean lead vocal, and light crowd and PA ambience. Walk away and the mix returns to the score. Place the cheap radio outside the stage's main listening area so the two vocal treatments blend only over a short boundary.
- **Pause and tension.** Duck the score and diegetic music partially while the pause menu is open, then ease back on resume while the song stays in phase. As the player approaches a genuine threat, lower the theme slightly before the warning. Leave a clear pocket for the existing `SFX_Braaam`. Give threat stingers a cooldown, and use a slower release than attack so repeatedly crossing a trigger boundary sounds natural.
- **Combat and recovery.** Keep enemy wind-ups and projectiles audible. Raise a rhythmic combat layer or use a short musical override if there is suitable material, keeping the original song's phase alive underneath. Release the combat mix after participants disengage for a short time. Combine menu, proximity, encounter, and one-shot ducking through a single mix controller so two overlapping events do not multiply the track into silence. Test headphones and speakers, near and far stage positions, pause mid-lyric, level transfers, and repeat encounters.

## Optional discoveries

**The Lost Property Mimic.** The obvious Lost Property box contains the required brass key. A maintenance memo says a *second* box was returned because it “claimed ownership of the clerk.” A badly aligned pipe opens a short side passage to the second box. It breathes in time with the warning tone. Interacting makes it sprout its legs, sound one BRAAAM, and give chase around a small loop with a visible way back to safety. Its reward is a confiscated item tag with a scribble from a previous investigator. Keep it off the main path and use the existing Showcase Mimic encounter as the behavioral base.

**The After-School Science Fair portal gun.** At the show, a vented equipment case has an inventory stamp that also appears on a stone planter in the Orchard. Following both marks leads to an abandoned science fair display tucked behind two mismatched pieces of school wall. A note reads, “Project question: Does a door need a wall? Judges declined to enter.” The existing portal gun opens a small paired-surface puzzle that retrieves a recording from a reversed version of the school corridor. The reward is an optional shortcut back to an Orchard checkpoint and a hint about the laundromat. Restrict valid surfaces to the puzzle so the gun cannot skip the return bell or strand the player across inactive Level Blocks.

### Rig status

The Blender source files capture the revised crouched sculpt and its separate standing Humanoid calibration. The crouch rig mirrors the prepared trouser-and-sneaker across the centerline. The Wolf prefab uses a 30-bone armature with 21 mapped Humanoid joints and 19 skinned parts. The trousers share a continuous waistband tucked under the sweater hem. The sleeve caps extend under the shoulders and blend into the chest weights. Its secondary-motion component adds spring response in the sleeves, ears, three mane sections, and hood, plus additive neck lean on acceleration and braking. Play Mode checks verified the visible wolf, retargeted sprint, spring activity, and isolation from the fallback character. See [Transfer and resume](05-Transfer-and-Resume.md) for the handoff.
