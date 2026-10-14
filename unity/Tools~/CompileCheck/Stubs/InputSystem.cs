#if ENABLE_INPUT_SYSTEM
#pragma warning disable CS0067
using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Controls
{
    public class ButtonControl
    {
        public bool isPressed => false;
        public bool wasPressedThisFrame => false;
    }

    public class KeyControl : ButtonControl { }

    public class Vector2Control
    {
        public Vector2 ReadValue() => Vector2.zero;
    }

    public class DpadControl
    {
        public ButtonControl up { get; } = new();
        public ButtonControl down { get; } = new();
        public ButtonControl left { get; } = new();
        public ButtonControl right { get; } = new();
    }

    public class TouchControl
    {
        public ButtonControl press { get; } = new();
        public Vector2Control position { get; } = new();
    }
}

namespace UnityEngine.InputSystem
{
    using Controls;

    public class Keyboard
    {
        public static Keyboard current => null;
        public event Action<char> onTextInput;
        public KeyControl upArrowKey, downArrowKey, leftArrowKey, rightArrowKey, wKey, aKey, sKey, dKey, zKey, xKey, jKey, kKey, mKey,
            enterKey, spaceKey, backspaceKey, leftShiftKey, escapeKey, tabKey;
    }

    public class Gamepad
    {
        public static Gamepad current => null;
        public DpadControl dpad { get; } = new();
        public ButtonControl buttonSouth { get; } = new();
        public ButtonControl buttonEast { get; } = new();
        public ButtonControl startButton { get; } = new();
    }

    public class Mouse
    {
        public static Mouse current => null;
        public ButtonControl leftButton { get; } = new();
        public Vector2Control position { get; } = new();
    }

    public class Touchscreen
    {
        public static Touchscreen current => null;
        public IReadOnlyList<TouchControl> touches { get; } = new List<TouchControl>();
    }
}
#endif
