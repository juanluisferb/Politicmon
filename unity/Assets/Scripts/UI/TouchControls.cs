using Politicmon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.UI
{
    /// <summary>
    /// Semi-transparent on-screen D-pad (left), A/B (right) and START.
    /// Hidden while menus, dialogs or battles are on screen (there you tap the options directly).
    /// Writes the held buttons into <see cref="GameInput.VirtualHeld"/>.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls Instance { get; private set; }

        /// <summary>Can be switched off from the options menu.</summary>
        public bool Enabled = true;

        readonly Image[] buttons = new Image[7];
        RectTransform root;

        void Awake()
        {
            Instance = this;
            UIManager.CreateCanvas(gameObject, 30, out _);
            root = UIFactory.Create("Controls", transform);
            string[] names = { "up", "down", "left", "right", "a", "b", "start" };
            for (int i = 0; i < names.Length; i++)
            {
                buttons[i] = UIFactory.Picture(root, "Sprites/UI/touch_" + names[i], 0, 0);
                buttons[i].color = new Color(1, 1, 1, 0.55f);
            }
            ScreenScaler.LayoutChanged += Layout;
            Layout();
        }

        void OnDestroy() => ScreenScaler.LayoutChanged -= Layout;

        void Layout()
        {
            var safe = ScreenScaler.SafeArea;
            UIFactory.Place(root, 0, 0, ScreenScaler.LogicalSize.x, ScreenScaler.LogicalSize.y);
            float bottom = safe.yMax;
            var center = new Vector2(safe.xMin + 30, bottom - 36);
            Put(GameButton.Up, center + new Vector2(-8, -24));
            Put(GameButton.Down, center + new Vector2(-8, 8));
            Put(GameButton.Left, center + new Vector2(-24, -8));
            Put(GameButton.Right, center + new Vector2(8, -8));
            Put(GameButton.A, new Vector2(safe.xMax - 26, bottom - 58));
            Put(GameButton.B, new Vector2(safe.xMax - 50, bottom - 38));
            Put(GameButton.Start, new Vector2(safe.xMin + safe.width / 2 - 12, bottom - 16));
        }

        void Put(GameButton b, Vector2 topLeft) => UIFactory.Move(buttons[(int)b].rectTransform, topLeft.x, topLeft.y);

        void Update()
        {
            bool show = Enabled && !UIManager.IsBusy && GameFlow.TouchControlsAllowed;
            root.gameObject.SetActive(show);
            for (int i = 0; i < buttons.Length; i++)
                GameInput.VirtualHeld[i] = show && IsTouched(buttons[i].rectTransform);
        }

        /// <summary>Hit test with a 4 px margin so the small pixel buttons are easy to press.</summary>
        static bool IsTouched(RectTransform button)
        {
            var pos = UIFactory.PositionOf(button);
            var rect = new Rect(pos.x - 4, pos.y - 4, button.sizeDelta.x + 8, button.sizeDelta.y + 8);
            foreach (var p in GameInput.Pointers)
                if (rect.Contains(p))
                    return true;
            return false;
        }
    }
}
