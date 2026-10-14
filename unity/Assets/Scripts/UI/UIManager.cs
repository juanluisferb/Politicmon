using System.Collections.Generic;
using Politicmon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.UI
{
    /// <summary>
    /// Owns the main UI canvas (inside the safe area) and offers the high level calls used
    /// by the whole game: Say, Choose, YesNo, Show(modal), fades and area banners.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public RectTransform Root { get; private set; }
        public DialogBox Dialog { get; private set; }

        readonly List<Modal> modals = new();
        Canvas canvas;
        CanvasScaler scaler;
        Image fade;
        RectTransform fadeRoot;
        RectTransform banner;
        int busyCount;
        int lastCloseFrame = -10;

        public static Vector2 Area => Instance.Root.sizeDelta;
        public static bool DialogVisible => Instance.Dialog.gameObject.activeSelf;

        /// <summary>True while any dialog/menu/transition owns the input (the overworld must wait).</summary>
        public static bool IsBusy =>
            Instance.modals.Count > 0 || DialogVisible || Instance.busyCount > 0 || Time.frameCount - Instance.lastCloseFrame <= 1;

        public static void PushBusy() => Instance.busyCount++;
        public static void PopBusy() => Instance.busyCount = Mathf.Max(0, Instance.busyCount - 1);

        // ---------------------------------------------------------------- setup

        void Awake()
        {
            Instance = this;
            canvas = CreateCanvas(gameObject, 20, out scaler);
            Root = UIFactory.Create("SafeArea", transform);
            Dialog = UIFactory.Create("DialogBox", Root).gameObject.AddComponent<DialogBox>();
            Dialog.gameObject.SetActive(false);

            var fadeGo = new GameObject("Fade");
            fadeGo.transform.SetParent(transform, false);
            CreateCanvas(fadeGo, 100, out _);
            fadeRoot = UIFactory.Create("Full", fadeGo.transform);
            fade = UIFactory.Solid(fadeRoot, new Color(0, 0, 0, 0), 0, 0, 1, 1);

            ScreenScaler.LayoutChanged += Layout;
            Layout();
        }

        void OnDestroy() => ScreenScaler.LayoutChanged -= Layout;

        /// <summary>Creates a pixel-perfect overlay canvas: 1 UI unit = 1 logical pixel.</summary>
        public static Canvas CreateCanvas(GameObject go, int sortingOrder, out CanvasScaler canvasScaler)
        {
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortingOrder;
            c.pixelPerfect = true;
            canvasScaler = go.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.scaleFactor = ScreenScaler.Scale;
            var s = canvasScaler;
            ScreenScaler.LayoutChanged += () =>
            {
                if (s != null)
                    s.scaleFactor = ScreenScaler.Scale;
            };
            return c;
        }

        void Layout()
        {
            scaler.scaleFactor = ScreenScaler.Scale;
            var safe = ScreenScaler.SafeArea;
            UIFactory.Place(Root, safe.x, safe.y, safe.width, safe.height);
            UIFactory.Place(fadeRoot, 0, 0, ScreenScaler.LogicalSize.x, ScreenScaler.LogicalSize.y);
            UIFactory.Place(fade.rectTransform, 0, 0, ScreenScaler.LogicalSize.x, ScreenScaler.LogicalSize.y);
            Dialog.Layout(safe.size);
        }

        /// <summary>Logical screen position to coordinates inside the safe-area root.</summary>
        public static Vector2 LogicalToRoot(Vector2 logical) => logical - ScreenScaler.SafeArea.position;

        // ---------------------------------------------------------------- modal stack

        public static bool IsTop(Modal m) => Instance.modals.Count > 0 && Instance.modals[^1] == m;

        public static T Create<T>(string name) where T : Modal => UIFactory.Create(name, Instance.Root).gameObject.AddComponent<T>();

        /// <summary>Builds, shows and awaits a modal. The modal is destroyed afterwards.</summary>
        public static async Awaitable<int> Show(Modal modal, bool destroy = true)
        {
            if (modal.Rect.sizeDelta == Vector2.zero)
                UIFactory.Place(modal.Rect, 0, 0, Area.x, Area.y);
            modal.Build(Area);
            var waiter = modal.Open();
            Push(modal);
            int result = await waiter;
            Pop(modal);
            if (destroy)
                Destroy(modal.gameObject);
            return result;
        }

        public static void Push(Modal m)
        {
            m.transform.SetAsLastSibling();
            Instance.modals.Add(m);
        }

        public static void Pop(Modal m)
        {
            Instance.modals.Remove(m);
            Instance.lastCloseFrame = Time.frameCount;
        }

        // ---------------------------------------------------------------- dialogs

        /// <summary>Shows a message. When waitLast is false the text stays on screen (for a following menu).</summary>
        public static async Awaitable Say(string text, bool waitLast = true)
        {
            var dialog = Instance.Dialog;
            dialog.gameObject.SetActive(true);
            dialog.transform.SetAsLastSibling();
            _ = dialog.Open();
            Push(dialog);
            await dialog.Play(text, waitLast);
            Pop(dialog);
            if (waitLast)
                dialog.gameObject.SetActive(false);
        }

        public static async Awaitable SayAll(IEnumerable<string> lines)
        {
            foreach (var line in lines)
                await Say(line);
        }

        /// <summary>Instantly replaces the dialog text (used for hover descriptions in menus).</summary>
        public static void SetDialogText(string text)
        {
            Instance.Dialog.gameObject.SetActive(true);
            Instance.Dialog.ShowInstant(text);
        }

        public static void CloseDialog()
        {
            Instance.Dialog.gameObject.SetActive(false);
            Instance.lastCloseFrame = Time.frameCount;
        }

        public static Awaitable<int> Choose(IList<MenuOption> options, MenuSettings settings = null)
        {
            var menu = Create<MenuList>("Menu");
            menu.Setup(options, settings);
            return Show(menu);
        }

        public static async Awaitable<int> Ask(string text, IList<MenuOption> options, MenuSettings settings = null)
        {
            await Say(text, false);
            int r = await Choose(options, settings);
            CloseDialog();
            return r;
        }

        public static async Awaitable<bool> YesNo(string question)
        {
            int r = await Ask(question, new List<MenuOption> { "SÍ", "NO" });
            return r == 0;
        }

        // ---------------------------------------------------------------- transitions

        public static void SetFade(float alpha, Color? color = null)
        {
            var c = color ?? Color.black;
            Instance.fade.color = new Color(c.r, c.g, c.b, alpha);
        }

        public static async Awaitable FadeOut(float duration = 0.25f) => await FadeTo(1f, duration);

        public static async Awaitable FadeIn(float duration = 0.25f) => await FadeTo(0f, duration);

        static async Awaitable FadeTo(float target, float duration)
        {
            PushBusy();
            var image = Instance.fade;
            float start = image.color.a;
            var rgb = image.color;
            await Async.Tween(duration, t => image.color = new Color(rgb.r, rgb.g, rgb.b, Mathf.Lerp(start, target, t)));
            PopBusy();
        }

        public static async Awaitable Flash(Color color, int times = 2)
        {
            for (int i = 0; i < times; i++)
            {
                SetFade(1f, color);
                await Async.Seconds(0.06f);
                SetFade(0f, color);
                await Async.Seconds(0.06f);
            }
        }

        /// <summary>Area name sign that slides in from the top-left corner.</summary>
        public static void Banner(string text) => Async.Run(() => Instance.ShowBanner(text));

        async Awaitable ShowBanner(string text)
        {
            if (banner != null)
                Destroy(banner.gameObject);
            float width = PixelFont.Measure(text) + 20;
            var b = UIFactory.Container(Root, "Banner", 4, -26, width, 24);
            banner = b;
            UIFactory.Panel(b, 0, 0, width, 24);
            UIFactory.Label(b, text, 10, 8);
            await Async.MoveTo(b, new Vector2(4, -4), 0.2f);
            await Async.Seconds(1.6f);
            if (b == null)
                return;
            await Async.MoveTo(b, new Vector2(4, 26), 0.2f);
            if (b != null)
                Destroy(b.gameObject);
        }
    }
}
