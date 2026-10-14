using System.Collections.Generic;
using Politicmon.Audio;
using Politicmon.Battle;
using Politicmon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.UI
{
    /// <summary>
    /// The party list (icon, name, level, PS bar, status). Returns the chosen index or -1.
    /// It can stay open while the caller validates the choice (see Reopen).
    /// </summary>
    public class PartyScreen : Modal
    {
        const int RowHeight = 30;

        List<Politician> members;
        string prompt;
        bool allowCancel;
        int index;
        RectTransform rowsRoot;
        Image cursor;
        PixelText promptLabel;
        Vector2 area;

        public int SwapFrom { get; set; } = -1;

        public void Setup(List<Politician> list, string text = "Elige un afiliado.", bool cancel = true, int start = 0)
        {
            members = list;
            prompt = text;
            allowCancel = cancel;
            index = Mathf.Clamp(start, 0, Mathf.Max(0, list.Count - 1));
        }

        public override void Build(Vector2 size)
        {
            area = size;
            UIFactory.Solid(transform, UIFactory.Paper, 0, 0, area.x, area.y);
            rowsRoot = UIFactory.Container(transform, "Rows", 0, 0, area.x, area.y);
            BuildRows();
            cursor = UIFactory.Picture(transform, "Sprites/UI/cursor", 6, 0);
            UIFactory.Panel(transform, 0, area.y - 30, area.x, 30);
            promptLabel = UIFactory.Label(transform, prompt, 8, area.y - 21, null, area.x - 16);
            RefreshCursor();
        }

        void BuildRows()
        {
            foreach (Transform child in rowsRoot)
                Destroy(child.gameObject);
            for (int i = 0; i < members.Count; i++)
            {
                var p = members[i];
                var row = UIFactory.Container(rowsRoot, "Row" + i, 0, 4 + i * RowHeight, area.x, RowHeight);
                var panel = UIFactory.Panel(row, 2, 0, area.x - 4, RowHeight - 2);
                if (i == SwapFrom)
                    panel.color = new Color(0.8f, 0.8f, 1f);
                UIFactory.Picture(row, GameData.PoliticianSpritePath(p.speciesId, "icon"), 14, 6);
                UIFactory.Label(row, p.Name, 34, 5);
                UIFactory.RightLabel(row, "Nv" + p.level, 5, area.x, 10);
                var bar = HpBar.Create(row, 34, 18, area.x - 100);
                bar.SetRatio(p.hp / (float)p.MaxHp);
                UIFactory.RightLabel(row, $"{p.hp}/{p.MaxHp}", 15, area.x, 10);
                string tag = p.IsFainted ? "DIM" : StatusTag(p.status);
                if (tag != "")
                    UIFactory.RightLabel(row, tag, 5, area.x, 34, UIFactory.Danger);
            }
        }

        public static string StatusTag(string status) => status switch
        {
            "escandalo" => "ESC",
            "imputado" => "IMP",
            _ => "",
        };

        /// <summary>Creates the screen and keeps it open; call Reopen() for each choice and Close() at the end.</summary>
        public static PartyScreen Present(List<Politician> list, string text = "Elige un afiliado.", bool cancel = true, int start = 0)
        {
            var screen = UIManager.Create<PartyScreen>("PartyScreen");
            screen.Setup(list, text, cancel, start);
            UIFactory.Place(screen.Rect, 0, 0, UIManager.Area.x, UIManager.Area.y);
            screen.Build(UIManager.Area);
            UIManager.Push(screen);
            return screen;
        }

        public void Close()
        {
            UIManager.Pop(this);
            Destroy(gameObject);
        }

        /// <summary>Rebuilds the rows (after reordering or healing) and waits for a new choice.</summary>
        public Awaitable<int> Reopen(List<Politician> list = null, string newPrompt = null)
        {
            if (list != null)
                members = list;
            BuildRows();
            if (newPrompt != null)
                SetPrompt(newPrompt);
            RefreshCursor();
            return Open();
        }

        public void SetPrompt(string text)
        {
            prompt = text;
            if (promptLabel != null)
                promptLabel.Text = text;
        }

        void RefreshCursor()
        {
            cursor.enabled = members.Count > 0;
            UIFactory.Move(cursor.rectTransform, 6, 4 + index * RowHeight + 10);
        }

        protected override void HandleInput()
        {
            if (members.Count > 0 && Repeated(GameButton.Up))
            {
                index = (index - 1 + members.Count) % members.Count;
                AudioManager.Sfx("cursor");
                RefreshCursor();
            }
            else if (members.Count > 0 && Repeated(GameButton.Down))
            {
                index = (index + 1) % members.Count;
                AudioManager.Sfx("cursor");
                RefreshCursor();
            }
            else if (members.Count > 0 && Pressed(GameButton.A))
            {
                AudioManager.Sfx("select");
                Finish(index);
            }
            else if (allowCancel && Pressed(GameButton.B))
            {
                AudioManager.Sfx("select");
                Finish(-1);
            }
            else if (Tapped(out var local))
            {
                int i = Mathf.FloorToInt((local.y - 4) / RowHeight);
                if (local.y >= 4 && i >= 0 && i < members.Count)
                {
                    index = i;
                    RefreshCursor();
                    AudioManager.Sfx("select");
                    Finish(i);
                }
                else if (allowCancel && local.y > area.y - 30)
                {
                    Finish(-1);
                }
            }
        }
    }
}
