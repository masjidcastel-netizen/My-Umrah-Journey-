using UnityEngine;
using UnityEngine.InputSystem;

namespace MyUmrah
{
    /// <summary>Aggregates on-screen touch controls, keyboard/mouse (editor testing) and gamepads (Bluetooth controllers).</summary>
    public static class GameInput
    {
        public static Vector2 TouchMove;
        public static bool TouchSprint, TouchBrake, TouchAccel;

        static Vector2 touchLook;
        static bool jump, interact, cameraSwitch, menu;

        public static void AddLook(Vector2 delta) { touchLook += delta; }
        public static void PressJump() { jump = true; }
        public static void PressInteract() { interact = true; }
        public static void PressCamera() { cameraSwitch = true; }
        public static void PressMenu() { menu = true; }

        public static void ClearOneShots()
        {
            jump = interact = cameraSwitch = menu = false;
            touchLook = Vector2.zero;
        }

        public static Vector2 Move
        {
            get
            {
                Vector2 v = TouchMove;
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
                }
                var g = Gamepad.current;
                if (g != null) v += g.leftStick.ReadValue();
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public static bool Sprint
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return TouchSprint || (k != null && k.leftShiftKey.isPressed) || (g != null && g.leftStickButton.isPressed);
            }
        }

        public static bool Accel
        {
            get { var g = Gamepad.current; return TouchAccel || (g != null && g.rightTrigger.isPressed); }
        }

        public static bool Brake
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return TouchBrake || (k != null && k.spaceKey.isPressed) || (g != null && g.buttonEast.isPressed);
            }
        }

        public static bool ConsumeJump()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            bool r = jump || (k != null && k.spaceKey.wasPressedThisFrame) || (g != null && g.buttonSouth.wasPressedThisFrame);
            jump = false;
            return r;
        }

        public static bool ConsumeInteract()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            bool r = interact || (k != null && k.eKey.wasPressedThisFrame) || (g != null && g.buttonWest.wasPressedThisFrame);
            interact = false;
            return r;
        }

        public static bool ConsumeCamera()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            bool r = cameraSwitch || (k != null && k.cKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame);
            cameraSwitch = false;
            return r;
        }

        public static bool ConsumeMenu()
        {
            var k = Keyboard.current; var g = Gamepad.current;
            bool r = menu || (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame);
            menu = false;
            return r;
        }

        /// <summary>Accumulated look delta in pixels since the last call.</summary>
        public static Vector2 ConsumeLook()
        {
            Vector2 l = touchLook;
            touchLook = Vector2.zero;
            var m = Mouse.current;
            if (m != null && m.rightButton.isPressed) l += m.delta.ReadValue();
            var g = Gamepad.current;
            if (g != null) l += g.rightStick.ReadValue() * 1200f * Time.unscaledDeltaTime;
            return l;
        }
    }
}
