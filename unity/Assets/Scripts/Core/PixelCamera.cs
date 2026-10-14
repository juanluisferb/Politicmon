using UnityEngine;

namespace Politicmon.Core
{
    /// <summary>
    /// Orthographic camera where 1 world unit = 16 logical pixels = 16 * Scale screen pixels,
    /// so tiles and sprites are always drawn at an integer scale.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PixelCamera : MonoBehaviour
    {
        Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.1f);
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
            ScreenScaler.LayoutChanged += Apply;
            Apply();
        }

        void OnDestroy() => ScreenScaler.LayoutChanged -= Apply;

        void Apply() => cam.orthographicSize = Screen.height / (2f * ScreenScaler.Scale * ScreenScaler.PixelsPerUnit);
    }
}
