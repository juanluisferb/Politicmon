using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && POLITICMON_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Politicmon.Core
{
    public enum GameButton
    {
        Up,
        Down,
        Left,
        Right,
        A,
        B,
        Start,
    }

    /// <summary>
    /// Unified input: keyboard / gamepad plus the on-screen touch controls.
    /// Works with both the new Input System package and the legacy Input Manager.
    /// <see cref="Update"/> is called once per frame by <see cref="GameBootstrap"/>.
    /// </summary>
    public static class GameInput
    {
        const int ButtonCount = 7;
        const float RepeatDelay = 0.35f;
        const float RepeatInterval = 0.12f;

        static readonly bool[] held = new bool[ButtonCount];
        static readonly bool[] previous = new bool[ButtonCount];
        static readonly float[] heldTime = new float[ButtonCount];
        static readonly float[] nextRepeat = new float[ButtonCount];
        static readonly bool[] injected = new bool[ButtonCount];
        static readonly List<Vector2> pointers = new();
        static bool tapThisFrame;
        static Vector2 tapPosition;

        /// <summary>Buttons held through the touch controls this frame (set by TouchControls).</summary>
        public static readonly bool[] VirtualHeld = new bool[ButtonCount];

        public static void Update(float deltaTime)
        {
            ReadPointers();
            for (int i = 0; i < ButtonCount; i++)
            {
                previous[i] = held[i];
                held[i] = KeyboardHeld((GameButton)i) || VirtualHeld[i] || injected[i];
                heldTime[i] = held[i] ? heldTime[i] + deltaTime : 0f;
            }
        }

        public static bool Held(GameButton b) => held[(int)b];

        /// <summary>True only on the frame the button goes down.</summary>
        public static bool Pressed(GameButton b) => held[(int)b] && !previous[(int)b];

        /// <summary>Like Pressed, but repeats while held (menu navigation).</summary>
        public static bool Repeated(GameButton b)
        {
            int i = (int)b;
            if (Pressed(b))
            {
                nextRepeat[i] = RepeatDelay;
                return true;
            }
            if (held[i] && heldTime[i] >= nextRepeat[i])
            {
                nextRepeat[i] += RepeatInterval;
                return true;
            }
            return false;
        }

        /// <summary>A tap/click that started this frame, in logical pixels (top-left origin).</summary>
        public static bool Tapped(out Vector2 logicalPosition)
        {
            logicalPosition = tapPosition;
            return tapThisFrame;
        }

        /// <summary>All pointers currently down, in logical pixels.</summary>
        public static IReadOnlyList<Vector2> Pointers => pointers;

        /// <summary>Simulates a held button (used by debug autopilot and tests).</summary>
        public static void Inject(GameButton b, bool down) => injected[(int)b] = down;

        // ---------------------------------------------------------------- backends

#if ENABLE_INPUT_SYSTEM && POLITICMON_INPUT_SYSTEM
        static bool KeyboardHeld(GameButton b)
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool key = k != null && b switch
            {
                GameButton.Up => k.upArrowKey.isPressed || k.wKey.isPressed,
                GameButton.Down => k.downArrowKey.isPressed || k.sKey.isPressed,
                GameButton.Left => k.leftArrowKey.isPressed || k.aKey.isPressed,
                GameButton.Right => k.rightArrowKey.isPressed || k.dKey.isPressed,
                GameButton.A => k.zKey.isPressed || k.enterKey.isPressed || k.spaceKey.isPressed || k.jKey.isPressed,
                GameButton.B => k.xKey.isPressed || k.backspaceKey.isPressed || k.kKey.isPressed || k.leftShiftKey.isPressed,
                _ => k.escapeKey.isPressed || k.mKey.isPressed || k.tabKey.isPressed,
            };
            bool pad = g != null && b switch
            {
                GameButton.Up => g.dpad.up.isPressed,
                GameButton.Down => g.dpad.down.isPressed,
                GameButton.Left => g.dpad.left.isPressed,
                GameButton.Right => g.dpad.right.isPressed,
                GameButton.A => g.buttonSouth.isPressed,
                GameButton.B => g.buttonEast.isPressed,
                _ => g.startButton.isPressed,
            };
            return key || pad;
        }

        static void ReadPointers()
        {
            pointers.Clear();
            tapThisFrame = false;
            var touch = Touchscreen.current;
            if (touch != null)
            {
                foreach (var t in touch.touches)
                {
                    if (!t.press.isPressed)
                        continue;
                    var pos = ScreenScaler.ScreenToLogical(t.position.ReadValue());
                    pointers.Add(pos);
                    if (t.press.wasPressedThisFrame && !tapThisFrame)
                    {
                        tapThisFrame = true;
                        tapPosition = pos;
                    }
                }
            }
            var mouse = Mouse.current;
            if (pointers.Count == 0 && mouse != null && mouse.leftButton.isPressed)
            {
                var pos = ScreenScaler.ScreenToLogical(mouse.position.ReadValue());
                pointers.Add(pos);
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    tapThisFrame = true;
                    tapPosition = pos;
                }
            }
        }
#else
        static bool KeyboardHeld(GameButton b) => b switch
        {
            GameButton.Up => Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W),
            GameButton.Down => Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S),
            GameButton.Left => Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A),
            GameButton.Right => Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D),
            GameButton.A => Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J),
            GameButton.B => Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.Backspace) || Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.LeftShift),
            _ => Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.M) || Input.GetKey(KeyCode.Tab),
        };

        static void ReadPointers()
        {
            pointers.Clear();
            tapThisFrame = false;
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                        continue;
                    var pos = ScreenScaler.ScreenToLogical(t.position);
                    pointers.Add(pos);
                    if (t.phase == TouchPhase.Began && !tapThisFrame)
                    {
                        tapThisFrame = true;
                        tapPosition = pos;
                    }
                }
                return;
            }
            if (Input.GetMouseButton(0))
            {
                var pos = ScreenScaler.ScreenToLogical(Input.mousePosition);
                pointers.Add(pos);
                if (Input.GetMouseButtonDown(0))
                {
                    tapThisFrame = true;
                    tapPosition = pos;
                }
            }
        }
#endif
    }
}
