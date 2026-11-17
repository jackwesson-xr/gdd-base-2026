# Change the character everywhere

1. Import a character FBX **with skin** into Assets.
2. Select the FBX. In **Rig**, set **Animation Type → Humanoid**, **Avatar Definition → Create From This Model**, and leave **Optimize Game Objects** off so pickup sockets can find the bones. Click **Apply**.
3. Open **Tools → GDD 110 → Global Character**. Drag the FBX into **Character** in the Inspector.
4. Save, then enter Play Mode from Scene 0 to check the title idle, intro walk, and Playground locomotion. Equip a hat or hand pickup to check the socket.

The title, standalone intro preview, Playground player, Block Showcase player, and Win use this shared choice. The course animation controller supplies the default animations. Clear **Character** to restore the robot. If a valid model faces the wrong direction or needs resizing, try **Extra Yaw** or **Height Override**; zero uses the automatic fit.

A player can intentionally use a local skin via **Hierarchy → GDD 110 → Apply Humanoid Skin**. For optional title loading artwork, assign a Sprite to **Loading Image** on the scene's opening component.

## Download and import from Mixamo

1. Select a character in Mixamo or upload your own humanoid mesh and complete the auto-rigger. Download the character in a **T-Pose**, using **FBX for Unity** and **With Skin** when offered. Keep its textures with it, and copy the FBX into a new art folder under **Assets**.
2. Select the FBX and check **Inspector → Rig**. The course recognizes many Mixamo FBXs on first import. If needed, right-click the character FBX and choose **GDD 110 → Make Humanoid (Mixamo)**. Click **Configure** on the Rig tab if Unity reports an invalid avatar, and check the bone mapping and T-Pose.
3. Check the model preview for missing textures. Use **Extract Textures** in the FBX **Materials** tab when the file contains embedded textures. For Painter exports, follow [Create Unity materials from Substance Painter](06-Substance-Materials.md) and remap the material slots on the FBX before choosing it as the global character.
4. For extra Mixamo motions, download each in **FBX for Unity → Without Skin** using the same character. Set each animation FBX to **Humanoid → Copy From Other Avatar** and choose the avatar from the character FBX. Click **Apply**, then preview the clip in the **Animation** tab. Add the clip to a state in your own Animator Controller or place it on a Timeline Animation Track bound to a character Animator. The course controller already supplies movement clips for the global skin swap.

## Assemble a playable scene

1. Start by editing a duplicate of the Playground or create an empty scene with a floor and its collider. In a new scene, right-click the Hierarchy and use **GDD 110 → Create** to add one **Player Rig**. The rig supplies the gameplay player and camera. Use **Global Character** to change the course-wide skin, or **Apply Humanoid Skin** on one rig to give that instance a different look.
2. Drag props into the Hierarchy and position them with the Move, Rotate, and Scale tools. Add colliders where the player should make contact. Drag assembled reusable objects from the Hierarchy to a Project folder to save a prefab.
3. Add a switch, pickup, or trigger from **GDD 110 → Create**, wire it to a target in its event list, and press Play. Test movement, camera, interaction, and the exit before building for WebGL. Add any new playable scene to the Build Profiles scene list and update the transition to point to it.
