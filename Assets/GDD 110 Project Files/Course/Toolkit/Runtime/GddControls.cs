using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GDD110
{
    public static class GddControls
    {
        static bool Down(ButtonControl button) => button != null && button.wasPressedThisFrame;
        static bool Up(ButtonControl button) => button != null && button.wasReleasedThisFrame;
        public static bool Interact => Down(Keyboard.current?.eKey) || Down(Gamepad.current?.buttonWest);
        public static bool Use => Down(Mouse.current?.leftButton) || Down(Gamepad.current?.rightTrigger);
        public static bool AltUse => Down(Mouse.current?.rightButton) || Down(Gamepad.current?.leftTrigger);
        public static bool Aim => Mouse.current?.rightButton.isPressed == true ||
            Gamepad.current?.leftTrigger.isPressed == true;
        public static bool Drop => Down(Keyboard.current?.qKey) || Down(Gamepad.current?.buttonEast);
        public static bool Restart => Down(Keyboard.current?.rKey) || Down(Gamepad.current?.selectButton);
        public static bool HasGamepad => Gamepad.current != null;
        public static string InteractHint => HasGamepad ? "X / Square" : "E";
        public static string UseHint => HasGamepad ? "RT / R2" : "Left click";
        public static string AltUseHint => HasGamepad ? "LT / L2" : "Right click";
        public static string DropHint => HasGamepad ? "B / Circle" : "Q";

        // Adapter for existing KeyCode UnityEvent examples. Keyboard settings remain unchanged.
        public static bool LegacyPressed(KeyCode key, bool released = false)
        {
            bool keyboard = released ? Input.GetKeyUp(key) : Input.GetKeyDown(key);
            var pad = Gamepad.current;
            if (pad == null) return keyboard;
            ButtonControl control = null;
            switch (key)
            {
                case KeyCode.E: control = pad.buttonWest; break;
                case KeyCode.Mouse0: control = pad.rightTrigger; break;
                case KeyCode.Space: control = pad.buttonSouth; break;
                case KeyCode.Tab: control = pad.buttonNorth; break;
                case KeyCode.UpArrow: control = pad.dpad.up; break;
                case KeyCode.DownArrow: control = pad.dpad.down; break;
                case KeyCode.R: control = pad.selectButton; break;
            }
            return keyboard || (released ? Up(control) : Down(control));
        }
    }
}
