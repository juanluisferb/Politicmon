using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.UI
{
    /// <summary>
    /// uGUI graphic that draws text with <see cref="PixelFont"/>, one quad per glyph,
    /// pixel-perfect at any integer scale. Supports a typewriter effect via VisibleCharacters.
    /// The RectTransform pivot is expected at the top-left corner.
    /// </summary>
    public class PixelText : MaskableGraphic
    {
        public enum Align
        {
            Left,
            Right,
            Center,
        }

        [SerializeField, TextArea] string text = "";
        [SerializeField] int visibleCharacters = -1;
        [SerializeField] Align alignment = Align.Left;

        public override Texture mainTexture => PixelFont.Texture;

        public string Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;
                text = value ?? "";
                SetVerticesDirty();
            }
        }

        /// <summary>-1 shows everything.</summary>
        public int VisibleCharacters
        {
            get => visibleCharacters;
            set
            {
                if (visibleCharacters == value)
                    return;
                visibleCharacters = value;
                SetVerticesDirty();
            }
        }

        public Align Alignment
        {
            get => alignment;
            set
            {
                alignment = value;
                SetVerticesDirty();
            }
        }

        public int PreferredWidth => PixelFont.Measure(text);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var tex = PixelFont.Texture;
            if (tex == null || string.IsNullOrEmpty(text))
                return;

            float texW = tex.width;
            float texH = tex.height;
            var lines = text.Split('\n');
            int shown = 0;
            int limit = visibleCharacters < 0 ? int.MaxValue : visibleCharacters;
            float boxWidth = rectTransform.rect.width;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                float x = alignment switch
                {
                    Align.Right => boxWidth - PixelFont.Measure(line),
                    Align.Center => Mathf.Floor((boxWidth - PixelFont.Measure(line)) / 2f),
                    _ => 0f,
                };
                float top = -lineIndex * PixelFont.LineHeight;
                foreach (char c in line)
                {
                    if (shown++ >= limit)
                        return;
                    if (!PixelFont.TryGetGlyph(c, out var g))
                    {
                        x += 3;
                        continue;
                    }
                    if (g.width > 0 && c != ' ')
                    {
                        float u0 = g.x / texW;
                        float u1 = (g.x + g.width) / texW;
                        float v1 = 1f - g.y / texH;
                        float v0 = 1f - (g.y + PixelFont.GlyphHeight) / texH;
                        AddQuad(vh, new Vector2(x, top - PixelFont.GlyphHeight), new Vector2(x + g.width, top),
                            new Vector2(u0, v0), new Vector2(u1, v1));
                    }
                    x += g.advance;
                }
                shown++;
            }
        }

        void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Vector2 uvMin, Vector2 uvMax)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y), color, new Vector2(uvMin.x, uvMin.y));
            vh.AddVert(new Vector3(min.x, max.y), color, new Vector2(uvMin.x, uvMax.y));
            vh.AddVert(new Vector3(max.x, max.y), color, new Vector2(uvMax.x, uvMax.y));
            vh.AddVert(new Vector3(max.x, min.y), color, new Vector2(uvMax.x, uvMin.y));
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
