using Politicmon.Core;
using Politicmon.UI;
using UnityEngine;

namespace Politicmon.Battle
{
    /// <summary>Name, level, status tag and PS bar of one side (the player's box also shows numbers and EXP).</summary>
    public class BattleHud : MonoBehaviour
    {
        public const float Width = 98;

        Politician mon;
        bool isPlayer;
        PixelText nameLabel;
        PixelText levelLabel;
        PixelText statusLabel;
        PixelText hpLabel;
        HpBar hpBar;
        HpBar expBar;
        float shownHp;

        public float Height => isPlayer ? 34 : 26;

        public static BattleHud Create(Transform parent, bool playerSide)
        {
            float height = playerSide ? 34 : 26;
            var root = UIFactory.Container(parent, playerSide ? "PlayerHud" : "EnemyHud", 0, 0, Width, height);
            var hud = root.gameObject.AddComponent<BattleHud>();
            hud.isPlayer = playerSide;
            UIFactory.Panel(root, 0, 0, Width, height);
            hud.nameLabel = UIFactory.Label(root, "", 6, 5);
            hud.levelLabel = UIFactory.RightLabel(root, "", 5, Width, 6);
            hud.statusLabel = UIFactory.Label(root, "", 6, 5, UIFactory.Danger, 30);
            UIFactory.Label(root, "PS", 6, 14, UIFactory.TextLight);
            hud.hpBar = HpBar.Create(root, 19, 16, Width - 26);
            if (playerSide)
            {
                hud.hpLabel = UIFactory.RightLabel(root, "", 21, Width, 8);
                UIFactory.Label(root, "EX", 6, 26, UIFactory.TextLight);
                hud.expBar = HpBar.Create(root, 19, 28, Width - 26, true);
            }
            return hud;
        }

        public void Bind(Politician p)
        {
            mon = p;
            shownHp = p.hp;
            Refresh();
        }

        public void Refresh()
        {
            if (mon == null)
                return;
            string name = mon.Name;
            while (PixelFont.Measure(name + ".") > Width - 34 && name.Length > 3)
                name = name[..^1];
            if (name != mon.Name)
                name += ".";
            nameLabel.Text = name;
            nameLabel.rectTransform.sizeDelta = new Vector2(PixelFont.Measure(name), PixelFont.LineHeight);
            levelLabel.Text = "Nv" + mon.level;
            string tag = PartyScreen.StatusTag(mon.status);
            if (tag == "" && mon.campaignTurns > 0)
                tag = "CAMP";
            statusLabel.Text = tag;
            UIFactory.Move(statusLabel.rectTransform, 6 + PixelFont.Measure(name) + 4, 5);
            hpBar.SetRatio(shownHp / mon.MaxHp);
            if (hpLabel != null)
            {
                hpLabel.Text = $"{Mathf.RoundToInt(shownHp)}/{mon.MaxHp}";
                expBar.SetRatio(mon.ExpProgress);
            }
        }

        public async Awaitable AnimateHp(int target)
        {
            float start = shownHp;
            float duration = Mathf.Max(0.15f, 0.75f * Mathf.Abs(target - start) / Mathf.Max(1f, mon.MaxHp));
            await Async.Tween(duration, t =>
            {
                shownHp = Mathf.Lerp(start, target, t);
                Refresh();
            });
        }

        public void SetShownHp(int value)
        {
            shownHp = value;
            Refresh();
        }

        public async Awaitable AnimateExp(float from, float to)
        {
            if (expBar != null)
                await Async.Tween(0.4f, t => expBar.SetRatio(Mathf.Lerp(from, to, t)));
        }
    }
}
