// Minimal stand-ins for the UnityEngine API used by Politicmon, so the game scripts can be
// compiled and the pure-logic tests can run with plain .NET (see ../README.md).
// They mirror the real signatures; behaviour is only implemented where tests need it.
#pragma warning disable CS0067, CS0649, CS0414, CS8618, CS8625
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace UnityEngine
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new(0, 0);
        public static Vector2 one => new(1, 1);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new(a.x / d, a.y / d);
        public static bool operator ==(Vector2 a, Vector2 b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * Mathf.Clamp01(t);
        public static implicit operator Vector3(Vector2 v) => new(v.x, v.y, 0);
        public static implicit operator Vector2(Vector3 v) => new(v.x, v.y);
        public static implicit operator Vector4(Vector2 v) => new(v.x, v.y, 0, 0);
        public static implicit operator Vector2(Vector2Int v) => new(v.x, v.y);
        public bool Equals(Vector2 o) => this == o;
        public override bool Equals(object o) => o is Vector2 v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 one => new(1, 1, 1);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => a * d;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * Mathf.Clamp01(t);
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int zero => new(0, 0);
        public static Vector2Int up => new(0, 1);
        public static Vector2Int down => new(0, -1);
        public static Vector2Int left => new(-1, 0);
        public static Vector2Int right => new(1, 0);
        public int sqrMagnitude => x * x + y * y;
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new(a.x - b.x, a.y - b.y);
        public static Vector2Int operator *(Vector2Int a, int d) => new(a.x * d, a.y * d);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public bool Equals(Vector2Int o) => this == o;
        public override bool Equals(object o) => o is Vector2Int v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector3Int
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new(1, 1, 1);
        public static Color black => new(0, 0, 0);
        public static Color gray => new(0.5f, 0.5f, 0.5f);
        public static Color magenta => new(1, 0, 1);
        public static implicit operator Color32(Color c) => new((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(c.a * 255));
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string html, out Color color)
        {
            color = Color.black;
            if (string.IsNullOrEmpty(html) || html[0] != '#' || (html.Length != 7 && html.Length != 9))
                return false;
            int v(int i) => Convert.ToInt32(html.Substring(i, 2), 16);
            color = new Color(v(1) / 255f, v(3) / 255f, v(5) / 255f, html.Length == 9 ? v(7) / 255f : 1f);
            return true;
        }
    }

    public struct Rect : IEquatable<Rect>
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
        public Rect(Vector2 position, Vector2 size) : this(position.x, position.y, size.x, size.y) { }
        public float xMin => x;
        public float yMin => y;
        public float xMax => x + width;
        public float yMax => y + height;
        public Vector2 position => new(x, y);
        public Vector2 size => new(width, height);
        public bool Contains(Vector2 p) => p.x >= xMin && p.x < xMax && p.y >= yMin && p.y < yMax;
        public static bool operator ==(Rect a, Rect b) => a.x == b.x && a.y == b.y && a.width == b.width && a.height == b.height;
        public static bool operator !=(Rect a, Rect b) => !(a == b);
        public bool Equals(Rect o) => this == o;
        public override bool Equals(object o) => o is Rect r && this == r;
        public override int GetHashCode() => HashCode.Combine(x, y, width, height);
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Clamp(float v, float a, float b) => Math.Clamp(v, a, b);
        public static int Clamp(int v, int a, int b) => Math.Clamp(v, a, b);
        public static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
        public static float Floor(float v) => MathF.Floor(v);
        public static int FloorToInt(float v) => (int)MathF.Floor(v);
        public static float Ceil(float v) => MathF.Ceiling(v);
        public static float Round(float v) => MathF.Round(v);
        public static int RoundToInt(float v) => (int)MathF.Round(v);
        public static float Abs(float v) => Math.Abs(v);
        public static int Abs(int v) => Math.Abs(v);
        public static float Sin(float v) => MathF.Sin(v);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
    }

    public static class Debug
    {
        public static void Log(object m) => Console.WriteLine(m);
        public static void LogWarning(object m) => Console.WriteLine("WARN " + m);
        public static void LogError(object m) => Console.WriteLine("ERROR " + m);
        public static void LogException(Exception e) => Console.WriteLine("EXCEPTION " + e);
    }

    public static class Time
    {
        public static float deltaTime => 1f / 60f;
        public static float unscaledDeltaTime => 1f / 60f;
        public static float unscaledTime => 0f;
        public static int frameCount => 0;
    }

    public enum ScreenOrientation { Portrait = 1, PortraitUpsideDown, LandscapeLeft, LandscapeRight, AutoRotation }

    public static class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }

    public static class Screen
    {
        public static int width => 1080;
        public static int height => 1920;
        public static Rect safeArea => new(0, 0, width, height);
        public static ScreenOrientation orientation { get; set; }
        public static int sleepTimeout { get; set; }
    }

    public static class Application
    {
        public static string persistentDataPath => Path.GetTempPath();
        public static int targetFrameRate { get; set; }
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultExecutionOrder : Attribute
    {
        public DefaultExecutionOrder(int order) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireComponent : Attribute
    {
        public RequireComponent(Type t) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class TextAreaAttribute : Attribute { }

    public class Object
    {
        public string name { get; set; }
        public static void Destroy(Object o) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T FindAnyObjectByType<T>() where T : Object => null;
        public static implicit operator bool(Object o) => o is not null;
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new();
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { this.name = name; }
        public GameObject(string name, params Type[] components) { this.name = name; }
        public Transform transform { get; } = new RectTransform();
        public bool activeSelf { get; private set; } = true;
        public string tag { get; set; }
        public void SetActive(bool value) => activeSelf = value;
        public T AddComponent<T>() where T : Component => (T)Activator.CreateInstance(typeof(T), true);
        public T GetComponent<T>() => default;
    }

    public class Component : Object
    {
        public Transform transform => gameObject.transform;
        public GameObject gameObject { get; } = new();
        public string tag { get; set; }
        public T GetComponent<T>() => default;
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; } = true;
    }

    public class MonoBehaviour : Behaviour { }

    public class Transform : Component, IEnumerable
    {
        public Vector3 localPosition { get; set; }
        public Vector3 position { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetAsLastSibling() { }
        public IEnumerator GetEnumerator() { yield break; }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Rect rect => new(0, 0, sizeDelta.x, sizeDelta.y);
    }

    public enum FilterMode { Point, Bilinear, Trilinear }

    public class Texture : Object
    {
        public int width { get; set; }
        public int height { get; set; }
        public FilterMode filterMode { get; set; }
    }

    public class Texture2D : Texture { }

    public class TextAsset : Object
    {
        public string text { get; set; }
    }

    public enum SpriteMeshType { FullRect, Tight }

    public class Sprite : Object
    {
        public Rect rect { get; private set; }
        public float pixelsPerUnit { get; private set; }
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu) => new() { rect = r, pixelsPerUnit = ppu };
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivot, float ppu, uint extrude, SpriteMeshType meshType, Vector4 border) =>
            new() { rect = r, pixelsPerUnit = ppu };
    }

    public class Renderer : Component
    {
        public bool enabled { get; set; } = true;
        public int sortingOrder { get; set; }
    }

    public class SpriteRenderer : Renderer
    {
        public Sprite sprite { get; set; }
        public bool flipX { get; set; }
    }

    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }
    public enum TransparencySortMode { Default, Perspective, Orthographic, CustomAxis }

    public class Camera : Behaviour
    {
        public static Camera main => null;
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public TransparencySortMode transparencySortMode { get; set; }
        public Vector3 transparencySortAxis { get; set; }
    }

    public class AudioClip : Object
    {
        public float length => 1f;
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public bool loop { get; set; }
        public float volume { get; set; }
        public bool isPlaying => false;
        public void Play() { }
        public void Stop() { }
    }

    public enum AnimatorCullingMode { AlwaysAnimate, CullUpdateTransforms, CullCompletely }

    public class Animator : Behaviour
    {
        public AnimatorCullingMode cullingMode { get; set; }
    }

    public class AnimationClip : Object { }

    public class Grid : Behaviour { }

    public static class Resources
    {
        /// <summary>Test root: Assets/Resources folder on disk.</summary>
        public static string Root = "";

        public static T Load<T>(string path) where T : Object
        {
            if (typeof(T) == typeof(TextAsset))
            {
                string file = Path.Combine(Root, path + ".json");
                return File.Exists(file) ? (T)(Object)new TextAsset { text = File.ReadAllText(file), name = Path.GetFileName(path) } : null;
            }
            return null;
        }

        public static T[] LoadAll<T>(string path) where T : Object => Array.Empty<T>();
    }

    public static class JsonUtility
    {
        static readonly System.Text.Json.JsonSerializerOptions Options = new() { IncludeFields = true, WriteIndented = true };

        public static T FromJson<T>(string json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(json, Options);
            }
            catch (System.Text.Json.JsonException e)
            {
                throw new ArgumentException("JSON parse error: " + e.Message);
            }
        }

        public static string ToJson(object obj, bool pretty = false) => System.Text.Json.JsonSerializer.Serialize(obj, obj.GetType(), Options);
    }

    // ---------------------------------------------------------------- legacy input

    public enum KeyCode
    {
        None, Backspace, Tab, Return, Escape, Space, LeftShift,
        UpArrow, DownArrow, LeftArrow, RightArrow,
        A, D, J, K, M, S, W, X, Z,
    }

    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }

    public struct Touch
    {
        public TouchPhase phase { get; set; }
        public Vector2 position { get; set; }
    }

    public static class Input
    {
        public static bool GetKey(KeyCode k) => false;
        public static bool GetKeyDown(KeyCode k) => false;
        public static int touchCount => 0;
        public static Touch GetTouch(int i) => default;
        public static bool GetMouseButton(int b) => false;
        public static bool GetMouseButtonDown(int b) => false;
        public static Vector3 mousePosition => Vector3.zero;
        public static string inputString => "";
    }

    public enum TouchScreenKeyboardType { Default }

    public class TouchScreenKeyboard
    {
        public enum Status { Visible, Done, Canceled, LostFocus }
        public static bool isSupported => false;
        public string text { get; set; }
        public Status status { get; }
        public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType type, bool autocorrection, bool multiline,
            bool secure, bool alert, string textPlaceholder, int characterLimit) => new();
    }

    // ---------------------------------------------------------------- Awaitable (Unity 6)

    [AsyncMethodBuilder(typeof(AwaitableBuilder))]
    public class Awaitable
    {
        internal Task task;
        internal Awaitable(Task t) => task = t;
        public TaskAwaiter GetAwaiter() => task.GetAwaiter();
        public static Awaitable NextFrameAsync() => new(Task.Yield().GetAwaiter().IsCompleted ? Task.CompletedTask : Task.Delay(1));
        public static Awaitable WaitForSecondsAsync(float seconds) => new(Task.Delay((int)(seconds * 1000)));
    }

    [AsyncMethodBuilder(typeof(AwaitableBuilder<>))]
    public class Awaitable<T>
    {
        internal Task<T> task;
        internal Awaitable(Task<T> t) => task = t;
        public TaskAwaiter<T> GetAwaiter() => task.GetAwaiter();
    }

    public class AwaitableCompletionSource<T>
    {
        readonly TaskCompletionSource<T> source = new();
        public Awaitable<T> Awaitable => new(source.Task);
        public void SetResult(T value) => source.TrySetResult(value);
    }

    public struct AwaitableBuilder
    {
        AsyncTaskMethodBuilder inner;
        public static AwaitableBuilder Create() => new() { inner = AsyncTaskMethodBuilder.Create() };
        public Awaitable Task => new(inner.Task);
        public void Start<TStateMachine>(ref TStateMachine sm) where TStateMachine : IAsyncStateMachine => inner.Start(ref sm);
        public void SetStateMachine(IAsyncStateMachine sm) => inner.SetStateMachine(sm);
        public void SetResult() => inner.SetResult();
        public void SetException(Exception e) => inner.SetException(e);
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter a, ref TStateMachine sm)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine => inner.AwaitOnCompleted(ref a, ref sm);
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter a, ref TStateMachine sm)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine => inner.AwaitUnsafeOnCompleted(ref a, ref sm);
    }

    public struct AwaitableBuilder<T>
    {
        AsyncTaskMethodBuilder<T> inner;
        public static AwaitableBuilder<T> Create() => new() { inner = AsyncTaskMethodBuilder<T>.Create() };
        public Awaitable<T> Task => new(inner.Task);
        public void Start<TStateMachine>(ref TStateMachine sm) where TStateMachine : IAsyncStateMachine => inner.Start(ref sm);
        public void SetStateMachine(IAsyncStateMachine sm) => inner.SetStateMachine(sm);
        public void SetResult(T value) => inner.SetResult(value);
        public void SetException(Exception e) => inner.SetException(e);
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter a, ref TStateMachine sm)
            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine => inner.AwaitOnCompleted(ref a, ref sm);
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter a, ref TStateMachine sm)
            where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine => inner.AwaitUnsafeOnCompleted(ref a, ref sm);
    }
}

namespace UnityEngine.Playables
{
    public struct PlayableGraph
    {
        public bool IsValid() => false;
        public void Destroy() { }
    }
}

namespace UnityEngine.Animations
{
    public struct AnimationClipPlayable { }

    public static class AnimationPlayableUtilities
    {
        public static AnimationClipPlayable PlayClip(Animator animator, AnimationClip clip, out Playables.PlayableGraph graph)
        {
            graph = default;
            return default;
        }
    }
}

namespace UnityEngine.Tilemaps
{
    public abstract class TileBase : ScriptableObject { }

    public class Tile : TileBase
    {
        public Sprite sprite { get; set; }
    }

    public class Tilemap : Behaviour
    {
        public void ClearAllTiles() { }
        public void SetTiles(Vector3Int[] positions, TileBase[] tiles) { }
    }

    public class TilemapRenderer : Renderer { }
}

namespace UnityEngine.UI
{
    public class VertexHelper
    {
        public int currentVertCount { get; private set; }
        public void Clear() => currentVertCount = 0;
        public void AddVert(Vector3 position, Color32 color, Vector4 uv0) => currentVertCount++;
        public void AddTriangle(int a, int b, int c) { }
    }

    public abstract class Graphic : MonoBehaviour
    {
        public virtual Color color { get; set; } = Color.white;
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform => (RectTransform)transform;
        public virtual Texture mainTexture => null;
        public virtual void SetVerticesDirty() { }
        protected virtual void OnPopulateMesh(VertexHelper vh) { }
    }

    public abstract class MaskableGraphic : Graphic { }

    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public Sprite sprite { get; set; }
        public Type type { get; set; }
    }

    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public ScaleMode uiScaleMode { get; set; }
        public float scaleFactor { get; set; }
    }
}

namespace UnityEngine
{
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
        public bool pixelPerfect { get; set; }
    }
}
