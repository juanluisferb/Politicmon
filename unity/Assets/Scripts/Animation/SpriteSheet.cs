using System.Collections.Generic;
using UnityEngine;

namespace Politicmon.Animation
{
    /// <summary>Utilities to cut textures into sprites at runtime (pixel-art friendly).</summary>
    public static class SpriteSheet
    {
        static readonly Dictionary<string, Sprite[]> characterCache = new();

        /// <summary>
        /// Overworld character frames from Resources/Sprites/Characters/&lt;name&gt;.
        /// Layout of every sheet (32x64): columns = stand/step, rows = down, up, left, right.
        /// Returns frames indexed as [direction * 2 + frame]. Uses the sliced sprites of the
        /// importer when available, otherwise slices the texture in code.
        /// </summary>
        public static Sprite[] CharacterFrames(string name)
        {
            if (characterCache.TryGetValue(name, out var cached))
                return cached;
            var frames = new Sprite[8];
            var imported = Resources.LoadAll<Sprite>("Sprites/Characters/" + name);
            string[] directions = { "down", "up", "left", "right" };
            if (imported.Length >= 8)
            {
                for (int d = 0; d < 4; d++)
                for (int f = 0; f < 2; f++)
                    frames[d * 2 + f] = System.Array.Find(imported, s => s.name == $"{name}_{directions[d]}_{f}");
            }
            if (System.Array.Exists(frames, s => s == null))
            {
                var tex = Resources.Load<Texture2D>("Sprites/Characters/" + name);
                tex.filterMode = FilterMode.Point;
                for (int d = 0; d < 4; d++)
                for (int f = 0; f < 2; f++)
                {
                    var rect = new Rect(f * 16, tex.height - 16 * (d + 1), 16, 16);
                    frames[d * 2 + f] = Sprite.Create(tex, rect, new Vector2(0.5f, 0f), 16f);
                    frames[d * 2 + f].name = $"{name}_{directions[d]}_{f}";
                }
            }
            characterCache[name] = frames;
            return frames;
        }

        /// <summary>A sub-rectangle of a texture (top-left origin in pixels) as a world sprite.</summary>
        public static Sprite Cut(Texture2D tex, int x, int yFromTop, int width, int height, Vector2 pivot, float pixelsPerUnit = 16f)
        {
            tex.filterMode = FilterMode.Point;
            var rect = new Rect(x, tex.height - yFromTop - height, width, height);
            return Sprite.Create(tex, rect, pivot, pixelsPerUnit);
        }
    }
}
