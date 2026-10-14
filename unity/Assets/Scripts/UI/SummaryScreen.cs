using Politicmon.Audio;
using Politicmon.Battle;
using Politicmon.Core;
using Politicmon.Data;
using UnityEngine;

namespace Politicmon.UI
{
    /// <summary>Politician card: sprite, types, stats, moves, experience and Politidex text.
    /// With isScan=true it is the "Encuesta" view of an opponent.</summary>
    public class SummaryScreen : Modal
    {
        public static readonly (Stat stat, string label)[] StatLabels =
        {
            (Stat.Carisma, "CARISMA"), (Stat.Oratoria, "ORATORIA"), (Stat.Aguante, "AGUANTE"),
            (Stat.Dialectica, "DIALÉCTICA"), (Stat.Templanza, "TEMPLANZA"), (Stat.Agilidad, "AGILIDAD"),
        };

        Politician mon;
        bool scan;

        public void Setup(Politician p, bool isScan = false)
        {
            mon = p;
            scan = isScan;
        }

        public override void Build(Vector2 area)
        {
            UIFactory.Solid(transform, UIFactory.Paper, 0, 0, area.x, area.y);
            float top = 4;
            UIFactory.Panel(transform, 2, top, 64, 64);
            UIFactory.Picture(transform, GameData.PoliticianSpritePath(mon.speciesId, "front"), 6, top + 4);
            UIFactory.Label(transform, $"Nº{mon.speciesId:000}", 70, top + 4, UIFactory.TextLight);
            UIFactory.Label(transform, mon.Name, 70, top + 14);
            UIFactory.Label(transform, "Nv" + mon.level, 70, top + 24);
            float tx = 70;
            foreach (var t in mon.Types)
                tx = TypeTag(transform, t, tx, top + 36) + 3;
            string status = mon.IsFainted ? "Dimisión" : mon.status switch
            {
                "escandalo" => "Escándalo",
                "imputado" => "Imputado",
                _ => "OK",
            };
            UIFactory.Label(transform, "Estado: " + status, 70, top + 48, UIFactory.TextLight);

            float y = top + 70;
            UIFactory.Panel(transform, 2, y, area.x - 4, 74);
            for (int i = 0; i < StatLabels.Length; i++)
            {
                var (stat, label) = StatLabels[i];
                string value = stat == Stat.Carisma ? $"{mon.hp}/{mon.MaxHp}" : mon.GetStat(stat).ToString();
                UIFactory.Label(transform, label, 8, y + 6 + i * 10);
                UIFactory.RightLabel(transform, value, y + 6 + i * 10, area.x, 10);
            }

            y += 76;
            int moveRows = Mathf.Max(1, mon.moves.Count);
            UIFactory.Panel(transform, 2, y, area.x - 4, 14 + 10 * moveRows);
            for (int j = 0; j < mon.moves.Count; j++)
            {
                var move = GameData.Move(mon.moves[j].id);
                UIFactory.Label(transform, move?.name ?? "?", 8, y + 6 + j * 10);
                UIFactory.RightLabel(transform, $"{mon.moves[j].pp}/{move?.pp ?? 0}", y + 6 + j * 10, area.x, 10);
            }
            y += 16 + 10 * moveRows;

            if (scan)
            {
                UIFactory.Label(transform, "Encuesta: margen de error ±3%", 6, y, UIFactory.TextLight);
                return;
            }
            UIFactory.Label(transform, $"EXP {mon.experience}  Siguiente nivel: {mon.ExpToNextLevel}", 6, y, UIFactory.TextLight);
            var desc = UIFactory.Label(transform, string.Join("\n", PixelFont.Wrap(mon.Species?.description, area.x - 12)), 6, y + 12);
            desc.rectTransform.sizeDelta = new Vector2(area.x - 12, 60);
        }

        /// <summary>Colored type badge; returns the x where the next badge can go.</summary>
        public static float TypeTag(Transform parent, string type, float x, float y)
        {
            string name = GameData.TypeName(type).ToUpperInvariant();
            float w = PixelFont.Measure(name) + 6;
            UIFactory.Solid(parent, GameData.TypeColor(type), x, y, w, 10);
            UIFactory.Label(parent, name, x + 3, y + 1, Color.white);
            return x + w;
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
