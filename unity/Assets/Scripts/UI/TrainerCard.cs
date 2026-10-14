using Politicmon.Core;
using UnityEngine;

namespace Politicmon.UI
{
    /// <summary>"Ficha" from the pause menu: name, money, Politidex count, time and badges (Escaños).</summary>
    public class TrainerCard : Modal
    {
        static readonly (string zone, string label)[] Badges = { ("madrid", "MAD"), ("andalucia", "SUR"), ("galicia", "NOR") };

        public override void Build(Vector2 area)
        {
            UIFactory.Solid(transform, UIFactory.Paper, 0, 0, area.x, area.y);
            UIFactory.Panel(transform, 2, 4, area.x - 4, 150);
            UIFactory.Label(transform, "CARNET DE LÍDER", 10, 12, UIFactory.TextLight);
            UIFactory.Label(transform, GameState.Progress.playerName, 10, 26);
            UIFactory.Label(transform, "Partido Nuevo", 10, 38);
            UIFactory.Label(transform, $"Fondos: {GameState.Money}€", 10, 54);
            UIFactory.Label(transform, $"Politidex: {GameState.Progress.caught.Count}", 10, 66);
            UIFactory.Label(transform, "Tiempo: " + GameState.PlayTimeText, 10, 78);
            UIFactory.Picture(transform, "Sprites/Trainers/player", area.x - 62, 18);
            UIFactory.Label(transform, "ESCAÑOS", 10, 100, UIFactory.TextLight);
            float x = 10;
            foreach (var (zone, label) in Badges)
            {
                var icon = UIFactory.Picture(transform, "Sprites/Items/escano_" + zone, x, 112);
                if (!GameState.HasBadge(zone))
                    icon.color = new Color(0, 0, 0, 0.25f);
                UIFactory.Label(transform, label, x + 1, 130, UIFactory.TextLight);
                x += 30;
            }
        }

        protected override void HandleInput()
        {
            if (Pressed(GameButton.A) || Pressed(GameButton.B) || GameInput.Tapped(out _))
                Finish(0);
        }
    }
}
