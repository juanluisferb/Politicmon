using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.UI
{
    /// <summary>
    /// Helpers to build pixel-art UI from code. Every element uses a top-left pivot and is
    /// positioned in logical pixels: (x, y) grows to the right and downwards, like a Game Boy screen.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color TextColor = Hex("#101820");
        public static readonly Color TextLight = Hex("#586878");
        public static readonly Color Paper = Hex("#f8f8f0");
        public static readonly Color Danger = Hex("#e04030");

        static readonly Dictionary<string, Sprite> spriteCache = new();

        public static Color Hex(string html) => ColorUtility.TryParseHtmlString(html, out var c) ? c : Color.magenta;

        /// <summary>Loads a texture from Resources as a UI sprite (optionally 9-sliced).</summary>
        public static Sprite LoadSprite(string path, int border = 0)
        {
            string key = path + "#" + border;
            if (spriteCache.TryGetValue(key, out var sprite))
                return sprite;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning("Missing texture " + path);
                return null;
            }
            tex.filterMode = FilterMode.Point;
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 1f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = tex.name;
            spriteCache[key] = sprite;
            return sprite;
        }

        public static RectTransform Create(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return rt;
        }

        public static void Place(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }

        public static void Move(RectTransform rt, float x, float y) => rt.anchoredPosition = new Vector2(x, -y);

        public static Vector2 PositionOf(RectTransform rt) => new(rt.anchoredPosition.x, -rt.anchoredPosition.y);

        public static RectTransform Container(Transform parent, string name, float x, float y, float width, float height)
        {
            var rt = Create(name, parent);
            Place(rt, x, y, width, height);
            return rt;
        }

        /// <summary>The Game Boy Color style framed box (white fill, dark double border).</summary>
        public static Image Panel(Transform parent, float x, float y, float width, float height)
        {
            var image = Create("Panel", parent).gameObject.AddComponent<Image>();
            image.sprite = LoadSprite("Sprites/UI/frame", 8);
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            Place(image.rectTransform, x, y, width, height);
            return image;
        }

        public static Image Solid(Transform parent, Color color, float x, float y, float width, float height)
        {
            var image = Create("Rect", parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            Place(image.rectTransform, x, y, width, height);
            return image;
        }

        public static Image Picture(Transform parent, string resourcePath, float x, float y, float width = 0, float height = 0)
        {
            var image = Create("Picture", parent).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            SetPicture(image, resourcePath);
            var size = image.sprite != null ? image.sprite.rect.size : Vector2.zero;
            Place(image.rectTransform, x, y, width > 0 ? width : size.x, height > 0 ? height : size.y);
            return image;
        }

        public static void SetPicture(Image image, string resourcePath)
        {
            image.sprite = string.IsNullOrEmpty(resourcePath) ? null : LoadSprite(resourcePath);
            image.enabled = image.sprite != null;
        }

        public static PixelText Label(Transform parent, string text, float x, float y, Color? color = null, float width = 0)
        {
            var label = Create("Text", parent).gameObject.AddComponent<PixelText>();
            label.raycastTarget = false;
            label.color = color ?? TextColor;
            label.Text = text;
            Place(label.rectTransform, x, y, width > 0 ? width : Mathf.Max(1, PixelFont.Measure(text)), PixelFont.LineHeight);
            return label;
        }

        /// <summary>A label aligned to the right edge of a box of the given width.</summary>
        public static PixelText RightLabel(Transform parent, string text, float y, float boxWidth, float rightPadding, Color? color = null)
        {
            var label = Label(parent, text, 0, y, color, boxWidth - rightPadding);
            label.Alignment = PixelText.Align.Right;
            return label;
        }
    }
}
