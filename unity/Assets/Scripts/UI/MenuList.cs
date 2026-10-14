using System;
using System.Collections.Generic;
using Politicmon.Audio;
using Politicmon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.UI
{
    public class MenuOption
    {
        public string Label;
        public string Right;
        public bool Enabled = true;

        public MenuOption(string label, string right = "", bool enabled = true)
        {
            Label = label;
            Right = right;
            Enabled = enabled;
        }

        public static implicit operator MenuOption(string label) => new(label);
    }

    public enum MenuAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center,
    }

    public class MenuSettings
    {
        public MenuAnchor Anchor = MenuAnchor.BottomRight;
        public Vector2? Point;          // defaults to the bottom-right corner above the dialog box
        public bool AllowCancel = true;
        public int Start;
        public int MaxRows;            // 0 = show all rows
        public int MinWidth;
        public string Title = "";
        public Action<int> OnHover;
    }

    /// <summary>
    /// Vertical list with a ► cursor. Up/Down (with repeat), A to choose, B to cancel (-1).
    /// Rows can also be tapped; tapping outside the menu cancels.
    /// </summary>
    public class MenuList : Modal
    {
        const int RowHeight = 12;
        const int Pad = 8;

        IList<MenuOption> options;
        MenuSettings settings;
        readonly List<PixelText> rows = new();
        readonly List<PixelText> rights = new();
        Image cursor;
        PixelText upArrow;
        PixelText downArrow;
        int index;
        int scroll;
        int titleHeight;

        public void Setup(IList<MenuOption> menuOptions, MenuSettings menuSettings)
        {
            options = menuOptions;
            settings = menuSettings ?? new MenuSettings();
            index = Mathf.Clamp(settings.Start, 0, Mathf.Max(0, options.Count - 1));
        }

        public override void Build(Vector2 area)
        {
            int visibleRows = settings.MaxRows > 0 ? Mathf.Min(settings.MaxRows, options.Count) : options.Count;
            float width = settings.MinWidth;
            foreach (var o in options)
            {
                float w = PixelFont.Measure(o.Label);
                if (!string.IsNullOrEmpty(o.Right))
                    w += PixelFont.Measure(o.Right) + 10;
                width = Mathf.Max(width, w);
            }
            titleHeight = string.IsNullOrEmpty(settings.Title) ? 0 : 12;
            var size = new Vector2(Mathf.Min(Mathf.Ceil(width) + Pad * 2 + 8, area.x), visibleRows * RowHeight + 14 + titleHeight);

            Vector2 point = settings.Point ?? new Vector2(area.x, area.y - (UIManager.DialogVisible ? DialogBox.BoxHeight : 0));
            Vector2 pos = settings.Anchor switch
            {
                MenuAnchor.TopLeft => point,
                MenuAnchor.TopRight => point - new Vector2(size.x, 0),
                MenuAnchor.BottomLeft => point - new Vector2(0, size.y),
                MenuAnchor.BottomRight => point - size,
                _ => point - size / 2f,
            };
            pos.x = Mathf.Clamp(Mathf.Round(pos.x), 0, Mathf.Max(0, area.x - size.x));
            pos.y = Mathf.Clamp(Mathf.Round(pos.y), 0, Mathf.Max(0, area.y - size.y));
            UIFactory.Place(Rect, pos.x, pos.y, size.x, size.y);

            UIFactory.Panel(transform, 0, 0, size.x, size.y);
            if (titleHeight > 0)
                UIFactory.Label(transform, settings.Title, Pad, 6, UIFactory.TextLight);
            for (int i = 0; i < visibleRows; i++)
            {
                float y = 8 + titleHeight + i * RowHeight;
                rows.Add(UIFactory.Label(transform, "", Pad + 8, y, null, size.x - Pad - 10));
                rights.Add(UIFactory.RightLabel(transform, "", y, size.x, Pad + 2));
            }
            cursor = UIFactory.Picture(transform, "Sprites/UI/cursor", Pad, 0);
            upArrow = UIFactory.Label(transform, "▲", size.x / 2 - 3, 1);
            downArrow = UIFactory.Label(transform, "▼", size.x / 2 - 3, size.y - 9);
            Refresh();
        }

        void Refresh()
        {
            if (index < scroll)
                scroll = index;
            if (index >= scroll + rows.Count)
                scroll = index - rows.Count + 1;
            for (int i = 0; i < rows.Count; i++)
            {
                int optionIndex = scroll + i;
                var o = optionIndex < options.Count ? options[optionIndex] : null;
                rows[i].Text = o?.Label ?? "";
                rights[i].Text = o?.Right ?? "";
                rows[i].color = o == null || o.Enabled ? UIFactory.TextColor : UIFactory.TextLight;
            }
            if (rows.Count > 0)
                UIFactory.Move(cursor.rectTransform, Pad, UIFactory.PositionOf(rows[index - scroll].rectTransform).y + 1);
            upArrow.enabled = scroll > 0;
            downArrow.enabled = scroll + rows.Count < options.Count;
            settings.OnHover?.Invoke(index);
        }

        void MoveCursor(int delta)
        {
            if (options.Count == 0)
                return;
            index = (index + delta % options.Count + options.Count) % options.Count;
            AudioManager.Sfx("cursor");
            Refresh();
        }

        void Choose(int i)
        {
            if (!options[i].Enabled)
            {
                AudioManager.Sfx("error");
                return;
            }
            AudioManager.Sfx("select");
            Finish(i);
        }

        protected override void HandleInput()
        {
            if (Repeated(GameButton.Up))
                MoveCursor(-1);
            else if (Repeated(GameButton.Down))
                MoveCursor(1);
            else if (Repeated(GameButton.Left) && settings.MaxRows > 0)
            {
                index = Mathf.Max(0, index - rows.Count);
                Refresh();
            }
            else if (Repeated(GameButton.Right) && settings.MaxRows > 0)
            {
                index = Mathf.Min(options.Count - 1, index + rows.Count);
                Refresh();
            }
            else if (Pressed(GameButton.A))
                Choose(index);
            else if (Pressed(GameButton.B) && settings.AllowCancel)
            {
                AudioManager.Sfx("select");
                Finish(-1);
            }
            else if (Tapped(out var local))
                HandleTap(local);
        }

        void HandleTap(Vector2 local)
        {
            var size = Rect.sizeDelta;
            bool inside = local.x >= 0 && local.y >= 0 && local.x < size.x && local.y < size.y;
            if (!inside)
            {
                if (settings.AllowCancel)
                    Finish(-1);
                return;
            }
            if (downArrow.enabled && local.y > size.y - 10)
            {
                MoveCursor(1);
                return;
            }
            if (upArrow.enabled && local.y < 8)
            {
                MoveCursor(-1);
                return;
            }
            for (int i = 0; i < rows.Count; i++)
            {
                float rowY = UIFactory.PositionOf(rows[i].rectTransform).y;
                if (local.y >= rowY - 2 && local.y < rowY + RowHeight - 2 && scroll + i < options.Count)
                {
                    index = scroll + i;
                    Refresh();
                    Choose(index);
                    return;
                }
            }
        }
    }
}
