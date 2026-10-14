using System.Collections.Generic;
using System.Linq;
using Politicmon.Audio;
using Politicmon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.UI
{
    /// <summary>Politidex: scrollable list of the 25 politicians with a preview of the selected one.</summary>
    public class DexScreen : Modal
    {
        const int RowHeight = 12;

        readonly List<PixelText> rows = new();
        readonly List<Image> icons = new();
        Image cursor;
        Image preview;
        RectTransform detail;
        int index;
        int scroll;
        int visibleRows;
        float listTop;

        public override void Build(Vector2 area)
        {
            UIFactory.Solid(transform, UIFactory.Paper, 0, 0, area.x, area.y);
            UIFactory.Panel(transform, 0, 0, area.x, 26);
            UIFactory.Label(transform, "POLITIDEX", 8, 9);
            int seen = GameData.AllSpecies.Count(s => GameState.IsSeen(s.id));
            int caught = GameData.AllSpecies.Count(s => GameState.IsCaught(s.id));
            UIFactory.RightLabel(transform, $"Vistos {seen}  Afiliados {caught}", 9, area.x, 8);

            listTop = 28;
            const float previewHeight = 72;
            visibleRows = Mathf.Max(3, (int)((area.y - listTop - previewHeight - 16) / RowHeight));
            UIFactory.Panel(transform, 0, listTop, area.x, visibleRows * RowHeight + 14);
            for (int i = 0; i < visibleRows; i++)
            {
                var icon = UIFactory.Picture(transform, null, 12, listTop + 4 + i * RowHeight, 12, 12);
                icons.Add(icon);
                rows.Add(UIFactory.Label(transform, "", 28, listTop + 7 + i * RowHeight, null, area.x - 32));
            }
            cursor = UIFactory.Picture(transform, "Sprites/UI/cursor", 4, 0);
            float py = listTop + visibleRows * RowHeight + 16;
            UIFactory.Panel(transform, 0, py, area.x, area.y - py);
            preview = UIFactory.Picture(transform, null, 6, py + 6, 56, 56);
            detail = UIFactory.Container(transform, "Detail", 66, py + 8, area.x - 70, 60);
            Refresh();
        }

        void Refresh()
        {
            int total = GameData.SpeciesCount;
            if (index < scroll)
                scroll = index;
            if (index >= scroll + visibleRows)
                scroll = index - visibleRows + 1;
            for (int i = 0; i < visibleRows; i++)
            {
                int id = scroll + i + 1;
                if (id > total)
                {
                    rows[i].Text = "";
                    icons[i].enabled = false;
                    continue;
                }
                rows[i].Text = $"{id:000} {(GameState.IsSeen(id) ? GameData.Species(id).name : "-----")}";
                UIFactory.SetPicture(icons[i], GameState.IsCaught(id) ? GameData.PoliticianSpritePath(id, "icon") : null);
            }
            UIFactory.Move(cursor.rectTransform, 4, UIFactory.PositionOf(rows[index - scroll].rectTransform).y + 1);

            foreach (Transform child in detail)
                Destroy(child.gameObject);
            int sid = index + 1;
            if (!GameState.IsSeen(sid))
            {
                UIFactory.SetPicture(preview, null);
                UIFactory.Label(detail, "???", 0, 0);
                return;
            }
            var s = GameData.Species(sid);
            UIFactory.SetPicture(preview, GameData.PoliticianSpritePath(sid, "front"));
            preview.color = GameState.IsCaught(sid) ? Color.white : new Color(0.1f, 0.1f, 0.15f);
            UIFactory.Label(detail, s.name, 0, 0);
            UIFactory.Label(detail, string.Join("/", s.types.Select(GameData.TypeName)), 0, 10, UIFactory.TextLight);
            UIFactory.Label(detail, "Zona: " + GameData.ZoneName(s.zone), 0, 20, UIFactory.TextLight);
            UIFactory.Label(detail, GameState.IsCaught(sid) ? "A: ver ficha" : "Sin afiliar", 0, 34, UIFactory.TextLight);
        }

        protected override void HandleInput()
        {
            int total = GameData.SpeciesCount;
            if (Repeated(GameButton.Up))
                Select((index - 1 + total) % total);
            else if (Repeated(GameButton.Down))
                Select((index + 1) % total);
            else if (Repeated(GameButton.Left))
                Select(Mathf.Max(0, index - visibleRows));
            else if (Repeated(GameButton.Right))
                Select(Mathf.Min(total - 1, index + visibleRows));
            else if (Pressed(GameButton.A))
                OpenEntry();
            else if (Pressed(GameButton.B))
            {
                AudioManager.Sfx("select");
                Finish(-1);
            }
            else if (Tapped(out var local))
            {
                for (int i = 0; i < visibleRows; i++)
                {
                    float rowY = UIFactory.PositionOf(rows[i].rectTransform).y;
                    if (Mathf.Abs(local.y - rowY - 4) <= RowHeight / 2f && scroll + i < total)
                    {
                        if (index == scroll + i)
                            OpenEntry();
                        else
                            Select(scroll + i);
                        return;
                    }
                }
                if (local.y < 26)
                    Finish(-1);
            }
        }

        void Select(int i)
        {
            index = i;
            AudioManager.Sfx("cursor");
            Refresh();
        }

        void OpenEntry()
        {
            int sid = index + 1;
            if (!GameState.IsCaught(sid))
            {
                AudioManager.Sfx("error");
                return;
            }
            AudioManager.Sfx("select");
            var entry = UIManager.Create<DexEntry>("DexEntry");
            entry.SpeciesId = sid;
            Async.Run(async () => await UIManager.Show(entry));
        }
    }

    /// <summary>Full Politidex entry: front and back sprites, base stats and description.</summary>
    public class DexEntry : Modal
    {
        public int SpeciesId { get; set; }

        public override void Build(Vector2 area)
        {
            var s = GameData.Species(SpeciesId);
            UIFactory.Solid(transform, UIFactory.Paper, 0, 0, area.x, area.y);
            UIFactory.Panel(transform, 2, 4, 64, 64);
            UIFactory.Picture(transform, GameData.PoliticianSpritePath(SpeciesId, "front"), 6, 8);
            UIFactory.Label(transform, $"Nº{SpeciesId:000}", 72, 8, UIFactory.TextLight);
            UIFactory.Label(transform, s.name, 72, 20);
            UIFactory.Label(transform, string.Join("/", s.types.Select(GameData.TypeName)), 72, 32);
            UIFactory.Picture(transform, GameData.PoliticianSpritePath(SpeciesId, "back"), area.x - 38, 36);

            float y = 74;
            UIFactory.Panel(transform, 2, y, area.x - 4, 74);
            for (int i = 0; i < SummaryScreen.StatLabels.Length; i++)
            {
                var (stat, label) = SummaryScreen.StatLabels[i];
                int value = s.baseStats.Get(stat);
                UIFactory.Label(transform, label, 8, y + 6 + i * 10);
                HpBar.Create(transform, 76, y + 9 + i * 10, area.x - 100, true).SetRatio(value / 120f);
                UIFactory.RightLabel(transform, value.ToString(), y + 6 + i * 10, area.x, 10);
            }
            y += 78;
            var lines = PixelFont.Wrap(s.description, area.x - 16);
            UIFactory.Panel(transform, 2, y, area.x - 4, lines.Count * PixelFont.LineHeight + 14);
            var text = UIFactory.Label(transform, string.Join("\n", lines), 8, y + 7, null, area.x - 16);
            text.rectTransform.sizeDelta = new Vector2(area.x - 16, lines.Count * PixelFont.LineHeight);
            AudioManager.Sfx("cry");
        }

        protected override void HandleInput()
        {
            if (Pressed(GameButton.A) || Pressed(GameButton.B) || GameInput.Tapped(out _))
            {
                AudioManager.Sfx("select");
                Finish(0);
            }
        }
    }
}
