using Politicmon.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && POLITICMON_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Politicmon.UI
{
    /// <summary>Free text name entry (max 8 chars). Uses the on-screen keyboard on mobile.</summary>
    public class NameEntry : Modal
    {
        const int MaxLength = 8;

        public string Value { get; private set; } = "";

        PixelText field;
        TouchScreenKeyboard keyboard;

        public override void Build(Vector2 area)
        {
            UIFactory.Panel(transform, 8, 40, area.x - 16, 56);
            UIFactory.Label(transform, "Escribe tu nombre (máx. 8):", 16, 48);
            UIFactory.Solid(transform, UIFactory.Hex("#e8f0e8"), 16, 60, area.x - 32, 12);
            field = UIFactory.Label(transform, "_", 19, 62, null, area.x - 38);
            UIFactory.Label(transform, "A / Intro: aceptar", 16, 82, UIFactory.TextLight);
            if (TouchScreenKeyboard.isSupported)
                keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, "", MaxLength);
#if ENABLE_INPUT_SYSTEM && POLITICMON_INPUT_SYSTEM
            if (Keyboard.current != null)
                Keyboard.current.onTextInput += OnTextInput;
#endif
        }

        void OnDestroy()
        {
#if ENABLE_INPUT_SYSTEM && POLITICMON_INPUT_SYSTEM
            if (Keyboard.current != null)
                Keyboard.current.onTextInput -= OnTextInput;
#endif
        }

        void OnTextInput(char c)
        {
            if (c == '\b')
            {
                if (Value.Length > 0)
                    Value = Value[..^1];
            }
            else if (!char.IsControl(c) && Value.Length < MaxLength && PixelFont.TryGetGlyph(char.ToUpperInvariant(c), out _))
            {
                Value += char.ToUpperInvariant(c);
            }
            field.Text = Value + "_";
        }

        protected override void HandleInput()
        {
            if (keyboard != null)
            {
                Value = (keyboard.text ?? "").ToUpperInvariant();
                if (Value.Length > MaxLength)
                    Value = Value[..MaxLength];
                field.Text = Value + "_";
                if (keyboard.status == TouchScreenKeyboard.Status.Done)
                    Accept();
                return;
            }
#if !(ENABLE_INPUT_SYSTEM && POLITICMON_INPUT_SYSTEM)
            foreach (char c in Input.inputString)
                if (c != '\n' && c != '\r')
                    OnTextInput(c);
            if (Input.GetKeyDown(KeyCode.Return))
                Accept();
#else
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                Accept();
#endif
        }

        void Accept()
        {
            if (string.IsNullOrWhiteSpace(Value))
                Value = "ALEX";
            Finish(0);
        }
    }
}
