using System;
using UnityEngine;

namespace Politicmon.Core
{
    /// <summary>
    /// Pixel-perfect presentation: picks the biggest integer scale that still shows at least
    /// 160x256 logical pixels, and widens the visible area to the device aspect ratio
    /// (more tiles instead of black bars). All UI works in logical pixels.
    /// </summary>
    public static class ScreenScaler
    {
        public const int MinLogicalWidth = 160;
        public const int MinLogicalHeight = 256;
        public const int PixelsPerUnit = 16;

        public static int Scale { get; private set; } = 1;
        public static Vector2Int LogicalSize { get; private set; } = new(180, 320);

        /// <summary>Safe area in logical pixels, origin at the top-left corner.</summary>
        public static Rect SafeArea { get; private set; } = new(0, 0, 180, 320);

        public static event Action LayoutChanged;

        static Vector2Int lastScreen;
        static Rect lastSafe;

        public static int ComputeScale(int width, int height) =>
            Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(width / (float)MinLogicalWidth, height / (float)MinLogicalHeight)));

        public static Vector2Int ComputeLogicalSize(int width, int height, int scale) => new(width / scale, height / scale);

        /// <summary>Call every frame; recomputes only when the screen size or safe area changes.</summary>
        public static void Refresh(bool force = false)
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            var safe = Screen.safeArea;
            if (!force && screen == lastScreen && safe == lastSafe)
                return;
            lastScreen = screen;
            lastSafe = safe;

            Scale = ComputeScale(screen.x, screen.y);
            LogicalSize = ComputeLogicalSize(screen.x, screen.y, Scale);
            float left = Mathf.Ceil(safe.xMin / Scale);
            float top = Mathf.Ceil((screen.y - safe.yMax) / Scale);
            float right = Mathf.Ceil((screen.x - safe.xMax) / Scale);
            float bottom = Mathf.Ceil(safe.yMin / Scale);
            SafeArea = new Rect(left, top, LogicalSize.x - left - right, LogicalSize.y - top - bottom);
            LayoutChanged?.Invoke();
        }

        /// <summary>Converts a screen position (pixels, bottom-left origin) to logical pixels (top-left origin).</summary>
        public static Vector2 ScreenToLogical(Vector2 screenPos) =>
            new(screenPos.x / Scale, (Screen.height - screenPos.y) / Scale);
    }
}
