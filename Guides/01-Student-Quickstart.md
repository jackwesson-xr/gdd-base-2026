# Student quickstart

1. In Unity Hub, open the project in **Unity 6000.6.0f1**.
2. In **Tools → GDD 110 → Start Here**, choose **Play from Title** or **Explore → Play from the Hall**.
3. In Play Mode, move toward a low ledge and press Jump to mantle. Use **R** to return to the current checkpoint.
4. To inspect individual mechanics, stop Play and choose **Play → Open Block Showcase**.

| Action | Keyboard/mouse | Controller |
| --- | --- | --- |
| Move / look | WASD / mouse | Left stick / right stick |
| Jump / sprint | Space / Shift | A or Cross / left trigger |
| Interact or equip | E | X or Square |
| Use active item | Left click | Right trigger |
| Drop active item | Q | B or Circle |

In Block Showcase, try the switch and the trigger floor marker. Both toggle the same target. Pick up a hat or blaster and try Use and Drop. Try the touch effects and spawn switch, then walk into the exit portal to reach Win.

## Build a switch without code

1. In the Hierarchy, right-click empty space → **GDD 110 → Create → Interact Button**.
2. Add a cube to serve as the target. In the button's Inspector, find **On Activated** and add an event.
3. Drag the cube into the event target. Choose **GameObject → SetActive** and leave the checkbox off.
4. Press Play, walk within range, and interact. The cube should disappear. Stop Play and try using a **Trigger Zone** to get the same effect on entry.

Use **GDD 110 → Add Function → Choose Function** to browse available behaviors for an object you already have. See the [full toolkit guide](../Assets/GDD 110 Project Files/Course/Toolkit/STUDENT_GUIDE.txt) for sockets, powers, and events.
