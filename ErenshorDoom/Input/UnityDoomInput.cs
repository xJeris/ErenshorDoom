using System;
using System.Collections.Generic;
using ManagedDoom;
using ManagedDoom.UserInput;
using UnityEngine;

namespace ErenshorDoom.Input
{
    public class UnityDoomInput : IUserInput
    {
        private Config config;
        private DoomRunner runner;

        private bool[] weaponKeys;
        private int turnHeld;

        // Mouse state
        private bool mouseGrabbed;
        private float mouseDeltaX;
        private float mouseDeltaY;

        // Key event buffer for Doom's event system
        private HashSet<DoomKey> pressedKeys;
        private HashSet<DoomKey> prevPressedKeys;

        // Controller axes
        private float controllerMoveX;
        private float controllerMoveY;
        private float controllerLookX;

        // Whether the Doom window has focus (mouse is over it)
        private bool focused;

        public UnityDoomInput(Config config, DoomRunner runner)
        {
            this.config = config;
            this.runner = runner;

            weaponKeys = new bool[7];
            turnHeld = 0;
            mouseGrabbed = false;
            focused = false;

            pressedKeys = new HashSet<DoomKey>();
            prevPressedKeys = new HashSet<DoomKey>();
        }

        public void SetFocused(bool value)
        {
            focused = value;
        }

        public void ProcessInput()
        {
            // Swap key sets
            var temp = prevPressedKeys;
            prevPressedKeys = pressedKeys;
            pressedKeys = temp;
            pressedKeys.Clear();

            // Only capture input when the Doom window has focus
            if (!focused)
            {
                // Still generate key-up events for any keys that were pressed
                foreach (var key in prevPressedKeys)
                {
                    runner.PostDoomEvent(new DoomEvent(ManagedDoom.EventType.KeyUp, key));
                }
                mouseDeltaX = 0;
                mouseDeltaY = 0;
                controllerMoveX = 0;
                controllerMoveY = 0;
                controllerLookX = 0;
                return;
            }

            // Read keyboard
            foreach (var mapping in KeyMappings)
            {
                if (UnityEngine.Input.GetKey(mapping.Key))
                {
                    pressedKeys.Add(mapping.Value);
                }
            }

            // Read mouse buttons
            if (mouseGrabbed)
            {
                if (UnityEngine.Input.GetMouseButton(0))
                    pressedKeys.Add(DoomKey.LControl); // Fire
                if (UnityEngine.Input.GetMouseButton(1))
                    pressedKeys.Add(DoomKey.Space); // Use

                mouseDeltaX = UnityEngine.Input.GetAxis("Mouse X") * 15f;
                mouseDeltaY = UnityEngine.Input.GetAxis("Mouse Y") * 15f;
            }
            else
            {
                mouseDeltaX = 0;
                mouseDeltaY = 0;
            }

            // Read controller
            float deadzone = PluginConfig.ControllerDeadzone.Value;
            controllerMoveX = ApplyDeadzone(UnityEngine.Input.GetAxis("Horizontal"), deadzone);
            controllerMoveY = ApplyDeadzone(UnityEngine.Input.GetAxis("Vertical"), deadzone);

            // Right stick - try common axis names
            controllerLookX = 0;
            try
            {
                float rx = UnityEngine.Input.GetAxis("RightStickHorizontal");
                controllerLookX = ApplyDeadzone(rx, deadzone);
            }
            catch
            {
                // Axis not defined in Input Manager, ignore
            }

            // Controller buttons
            if (GetControllerButton("Fire1") || GetControllerButton("joystick button 7")) // RT / R2
                pressedKeys.Add(DoomKey.LControl);
            if (GetControllerButton("Fire2") || GetControllerButton("joystick button 0")) // A / Cross
                pressedKeys.Add(DoomKey.Space);
            if (GetControllerButton("Fire3") || GetControllerButton("joystick button 4")) // LB / L1
                pressedKeys.Add(DoomKey.LShift); // Run
            if (GetControllerButton("joystick button 6") || GetControllerButton("Submit")) // Start
                pressedKeys.Add(DoomKey.Enter);
            if (GetControllerButton("Cancel") || GetControllerButton("joystick button 1")) // B / Circle
                pressedKeys.Add(DoomKey.Escape);

            // Generate key events for Doom
            foreach (var key in pressedKeys)
            {
                if (!prevPressedKeys.Contains(key))
                {
                    runner.PostDoomEvent(new DoomEvent(ManagedDoom.EventType.KeyDown, key));
                }
            }
            foreach (var key in prevPressedKeys)
            {
                if (!pressedKeys.Contains(key))
                {
                    runner.PostDoomEvent(new DoomEvent(ManagedDoom.EventType.KeyUp, key));
                }
            }
        }

        private bool GetControllerButton(string name)
        {
            try
            {
                return UnityEngine.Input.GetButton(name);
            }
            catch
            {
                return false;
            }
        }

        public void BuildTicCmd(TicCmd cmd)
        {
            var keyForward = IsPressed(config.key_forward);
            var keyBackward = IsPressed(config.key_backward);
            var keyStrafeLeft = IsPressed(config.key_strafeleft);
            var keyStrafeRight = IsPressed(config.key_straferight);
            var keyTurnLeft = IsPressed(config.key_turnleft);
            var keyTurnRight = IsPressed(config.key_turnright);
            var keyFire = IsPressed(config.key_fire);
            var keyUse = IsPressed(config.key_use);
            var keyRun = IsPressed(config.key_run);
            var keyStrafe = IsPressed(config.key_strafe);

            weaponKeys[0] = UnityEngine.Input.GetKey(KeyCode.Alpha1);
            weaponKeys[1] = UnityEngine.Input.GetKey(KeyCode.Alpha2);
            weaponKeys[2] = UnityEngine.Input.GetKey(KeyCode.Alpha3);
            weaponKeys[3] = UnityEngine.Input.GetKey(KeyCode.Alpha4);
            weaponKeys[4] = UnityEngine.Input.GetKey(KeyCode.Alpha5);
            weaponKeys[5] = UnityEngine.Input.GetKey(KeyCode.Alpha6);
            weaponKeys[6] = UnityEngine.Input.GetKey(KeyCode.Alpha7);

            cmd.Clear();

            var strafe = keyStrafe;
            var speed = keyRun ? 1 : 0;
            var forward = 0;
            var side = 0;

            if (config.game_alwaysrun)
            {
                speed = 1 - speed;
            }

            if (keyTurnLeft || keyTurnRight)
            {
                turnHeld++;
            }
            else
            {
                turnHeld = 0;
            }

            int turnSpeed;
            if (turnHeld < PlayerBehavior.SlowTurnTics)
            {
                turnSpeed = 2;
            }
            else
            {
                turnSpeed = speed;
            }

            if (strafe)
            {
                if (keyTurnRight) side += PlayerBehavior.SideMove[speed];
                if (keyTurnLeft) side -= PlayerBehavior.SideMove[speed];
            }
            else
            {
                if (keyTurnRight) cmd.AngleTurn -= (short)PlayerBehavior.AngleTurn[turnSpeed];
                if (keyTurnLeft) cmd.AngleTurn += (short)PlayerBehavior.AngleTurn[turnSpeed];
            }

            if (keyForward) forward += PlayerBehavior.ForwardMove[speed];
            if (keyBackward) forward -= PlayerBehavior.ForwardMove[speed];
            if (keyStrafeLeft) side -= PlayerBehavior.SideMove[speed];
            if (keyStrafeRight) side += PlayerBehavior.SideMove[speed];

            if (keyFire) cmd.Buttons |= TicCmdButtons.Attack;
            if (keyUse) cmd.Buttons |= TicCmdButtons.Use;

            // Weapon keys
            for (var i = 0; i < weaponKeys.Length; i++)
            {
                if (weaponKeys[i])
                {
                    cmd.Buttons |= TicCmdButtons.Change;
                    cmd.Buttons |= (byte)(i << TicCmdButtons.WeaponShift);
                    break;
                }
            }

            // Mouse turning
            if (mouseGrabbed)
            {
                var ms = 0.5f * config.mouse_sensitivity;
                var mx = (int)(ms * mouseDeltaX);
                var my = (int)(ms * -mouseDeltaY);
                forward += my;
                if (strafe)
                {
                    side += mx * 2;
                }
                else
                {
                    cmd.AngleTurn -= (short)(mx * 0x8);
                }
            }

            // Controller input
            float sensitivity = PluginConfig.ControllerTurnSensitivity.Value;
            if (Math.Abs(controllerMoveY) > 0.01f)
            {
                forward += (int)(controllerMoveY * PlayerBehavior.ForwardMove[speed]);
            }
            if (Math.Abs(controllerMoveX) > 0.01f)
            {
                side += (int)(controllerMoveX * PlayerBehavior.SideMove[speed]);
            }
            if (Math.Abs(controllerLookX) > 0.01f)
            {
                cmd.AngleTurn -= (short)(controllerLookX * sensitivity * PlayerBehavior.AngleTurn[turnSpeed]);
            }

            // Clamp values
            if (forward > PlayerBehavior.MaxMove) forward = PlayerBehavior.MaxMove;
            else if (forward < -PlayerBehavior.MaxMove) forward = -PlayerBehavior.MaxMove;
            if (side > PlayerBehavior.MaxMove) side = PlayerBehavior.MaxMove;
            else if (side < -PlayerBehavior.MaxMove) side = -PlayerBehavior.MaxMove;

            cmd.ForwardMove += (sbyte)forward;
            cmd.SideMove += (sbyte)side;
        }

        private bool IsPressed(KeyBinding keyBinding)
        {
            foreach (var key in keyBinding.Keys)
            {
                if (pressedKeys.Contains(key))
                    return true;
            }

            if (mouseGrabbed)
            {
                foreach (var mb in keyBinding.MouseButtons)
                {
                    if (mb == DoomMouseButton.Mouse1 && UnityEngine.Input.GetMouseButton(0))
                        return true;
                    if (mb == DoomMouseButton.Mouse2 && UnityEngine.Input.GetMouseButton(1))
                        return true;
                    if (mb == DoomMouseButton.Mouse3 && UnityEngine.Input.GetMouseButton(2))
                        return true;
                }
            }

            return false;
        }

        private static float ApplyDeadzone(float value, float deadzone)
        {
            if (Math.Abs(value) < deadzone)
                return 0f;
            return value;
        }

        public void Reset()
        {
            mouseDeltaX = 0;
            mouseDeltaY = 0;
        }

        public void GrabMouse()
        {
            mouseGrabbed = true;
        }

        public void ReleaseMouse()
        {
            mouseGrabbed = false;
        }

        public int MaxMouseSensitivity => 15;

        public int MouseSensitivity
        {
            get => config.mouse_sensitivity;
            set => config.mouse_sensitivity = value;
        }

        // Unity KeyCode -> DoomKey mapping table
        private static readonly Dictionary<KeyCode, DoomKey> KeyMappings = new Dictionary<KeyCode, DoomKey>
        {
            { KeyCode.A, DoomKey.A },
            { KeyCode.B, DoomKey.B },
            { KeyCode.C, DoomKey.C },
            { KeyCode.D, DoomKey.D },
            { KeyCode.E, DoomKey.E },
            { KeyCode.F, DoomKey.F },
            { KeyCode.G, DoomKey.G },
            { KeyCode.H, DoomKey.H },
            { KeyCode.I, DoomKey.I },
            { KeyCode.J, DoomKey.J },
            { KeyCode.K, DoomKey.K },
            { KeyCode.L, DoomKey.L },
            { KeyCode.M, DoomKey.M },
            { KeyCode.N, DoomKey.N },
            { KeyCode.O, DoomKey.O },
            { KeyCode.P, DoomKey.P },
            { KeyCode.Q, DoomKey.Q },
            { KeyCode.R, DoomKey.R },
            { KeyCode.S, DoomKey.S },
            { KeyCode.T, DoomKey.T },
            { KeyCode.U, DoomKey.U },
            { KeyCode.V, DoomKey.V },
            { KeyCode.W, DoomKey.W },
            { KeyCode.X, DoomKey.X },
            { KeyCode.Y, DoomKey.Y },
            { KeyCode.Z, DoomKey.Z },
            { KeyCode.Alpha0, DoomKey.Num0 },
            { KeyCode.Alpha1, DoomKey.Num1 },
            { KeyCode.Alpha2, DoomKey.Num2 },
            { KeyCode.Alpha3, DoomKey.Num3 },
            { KeyCode.Alpha4, DoomKey.Num4 },
            { KeyCode.Alpha5, DoomKey.Num5 },
            { KeyCode.Alpha6, DoomKey.Num6 },
            { KeyCode.Alpha7, DoomKey.Num7 },
            { KeyCode.Alpha8, DoomKey.Num8 },
            { KeyCode.Alpha9, DoomKey.Num9 },
            { KeyCode.Escape, DoomKey.Escape },
            { KeyCode.Return, DoomKey.Enter },
            { KeyCode.Tab, DoomKey.Tab },
            { KeyCode.Backspace, DoomKey.Backspace },
            { KeyCode.Space, DoomKey.Space },
            { KeyCode.LeftShift, DoomKey.LShift },
            { KeyCode.RightShift, DoomKey.RShift },
            { KeyCode.LeftControl, DoomKey.LControl },
            { KeyCode.RightControl, DoomKey.RControl },
            { KeyCode.LeftAlt, DoomKey.LAlt },
            { KeyCode.RightAlt, DoomKey.RAlt },
            { KeyCode.UpArrow, DoomKey.Up },
            { KeyCode.DownArrow, DoomKey.Down },
            { KeyCode.LeftArrow, DoomKey.Left },
            { KeyCode.RightArrow, DoomKey.Right },
            { KeyCode.F1, DoomKey.F1 },
            { KeyCode.F2, DoomKey.F2 },
            { KeyCode.F3, DoomKey.F3 },
            { KeyCode.F4, DoomKey.F4 },
            { KeyCode.F5, DoomKey.F5 },
            { KeyCode.F6, DoomKey.F6 },
            { KeyCode.F7, DoomKey.F7 },
            { KeyCode.F8, DoomKey.F8 },
            // F9 is reserved as the toggle key
            { KeyCode.F10, DoomKey.F10 },
            { KeyCode.F11, DoomKey.F11 },
            { KeyCode.F12, DoomKey.F12 },
            { KeyCode.Minus, DoomKey.Subtract },
            { KeyCode.Equals, DoomKey.Equal },
            { KeyCode.LeftBracket, DoomKey.LBracket },
            { KeyCode.RightBracket, DoomKey.RBracket },
            { KeyCode.Semicolon, DoomKey.Semicolon },
            { KeyCode.Comma, DoomKey.Comma },
            { KeyCode.Period, DoomKey.Period },
            { KeyCode.Slash, DoomKey.Slash },
            { KeyCode.Backslash, DoomKey.Backslash },
            { KeyCode.Pause, DoomKey.Pause },
            { KeyCode.Insert, DoomKey.Insert },
            { KeyCode.Delete, DoomKey.Delete },
            { KeyCode.Home, DoomKey.Home },
            { KeyCode.End, DoomKey.End },
            { KeyCode.PageUp, DoomKey.PageUp },
            { KeyCode.PageDown, DoomKey.PageDown },
            { KeyCode.KeypadPlus, DoomKey.Add },
            { KeyCode.KeypadMinus, DoomKey.Subtract },
            { KeyCode.KeypadMultiply, DoomKey.Multiply },
            { KeyCode.KeypadDivide, DoomKey.Divide },
            { KeyCode.KeypadEnter, DoomKey.Enter },
        };
    }
}
