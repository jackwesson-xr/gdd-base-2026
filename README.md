# GDD 6.6 — GDD 110 course and no-code toolkit

Open this folder in Unity Hub using Unity 6000.6.0f1. Required Unity packages download on first import. Coplay MCP is an optional local instructor tool. See the handoff instructions below when making an editable archive.

- [Student quickstart and first no-code exercise](Guides/01-Student-Quickstart.md)
- [Global character and Mixamo setup](Guides/02-Global-Character.md)
- [WebGL builds and classroom sharing](Guides/03-WebGL-and-Sharing.md)
- [Generate URP materials from a Substance Painter export](Guides/06-Substance-Materials.md)
- [Instructor setup, tutorial authoring, and local MCP](Guides/07-Instructor-Setup.md)

## Where files live

Open `Assets/GDD 110 Project Files` in Unity's Project window.

- **Course** holds the playable scenes, teaching toolkit, shared input, and starter assets.
- **The Fourth Bell** holds the campaign's character, environment, combat, and runtime assets.
- **Block Showcase** holds the interactive teaching examples and their editor tools.
- **Wesson Assets**, **Plugins**, and **Third-Party Art** keep the shared art library, integrations, and imported art in separate folders.
- **Unity Template** and **Experiments** hold template samples and exploratory work.

The editable Blender source and written guides live beside `Assets` in **Character Source** and **Guides**.

Open **Tools → GDD 110 → Start Here**. Choose **Play from Title** for the full experience or **Explore → Play from the Hall** to start in the school. The tutorial has short steps and checks for playing, jumping, reaching the culvert, making materials, and building.

The Fourth Bell starts inside the School Assembly Hall and opens into the Culvert and Signal Orchard. **R** returns to the last checkpoint. The three Level Block roots hold the stage scenery and interactions, with one player and camera across all stages. [Playtest and resume guide](Guides/05-Transfer-and-Resume.md).

Open **Block Showcase** from the title or **Tools → GDD 110 → Start Here → Play**. The five sections cover touchables, interactions, automations, NPCs, and pickups. Select the blocks in the editor to inspect their components and links.

Right-click the Hierarchy → **GDD 110 → Create** for ready-made interactive objects and the course Player Rig. Right-click an existing object → **GDD 110 → Add Function** to add pickups, powers, triggers, collision events, timers, health, motion, and other functions. All new custom functions share the `GddEntity` base. The project includes the lesson scripts and assets required by the scenes and toolkit.

Open **Tools → GDD 110 → Student Quick Start**, or read `Assets/GDD 110 Project Files/Course/Toolkit/STUDENT_GUIDE.txt`. It includes a no-code switch example, body attachments, global power presets, and controller instructions.

**Swap the character everywhere:** Import a character FBX with skin as Humanoid, then select **Tools → GDD 110 → Global Character** and assign it to **Character**. Clear the field to restore the default robot. The shared default animations, movement, and pickup sockets carry over. See the Start Here guide for the full steps.

**Keyboard:** WASD move, mouse look, Space jump, E interact, left click use, Q drop. To mantle a low ledge, move toward it and press Jump. **Controller:** left stick move, right stick look, A/Cross jump/select, X/Square interact, right trigger use, B/Circle drop. Press a controller button after focusing a browser game so the browser can discover it.

Build with **Start Here → Build → Build WebGL**. The builder uses URP, the browser quality level, WebGL 2, IL2CPP, Gzip with decompression fallback, and no worker-thread requirement. ZIP the build contents with `index.html` at the root for Unity Play.
