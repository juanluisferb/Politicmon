using Politicmon.Audio;
using Politicmon.Battle;
using Politicmon.Core;
using UnityEngine;

namespace Politicmon.UI
{
    /// <summary>"¡X está cambiando de cargo!": the old and new sprites flash faster and faster.</summary>
    public static class EvolutionScreen
    {
        public static async Awaitable Play(Politician p)
        {
            int target = p.EvolutionTarget;
            if (target <= 0)
                return;

            var go = new GameObject("EvolutionScreen");
            UIManager.CreateCanvas(go, 6, out _);
            var full = (Vector2)ScreenScaler.LogicalSize;
            var safe = ScreenScaler.SafeArea;
            var root = UIFactory.Container(go.transform, "Root", 0, 0, full.x, full.y);
            UIFactory.Solid(root, UIFactory.Paper, 0, 0, full.x, full.y);
            string oldPath = GameData.PoliticianSpritePath(p.speciesId, "front");
            string newPath = GameData.PoliticianSpritePath(target, "front");
            float y = safe.yMin + (safe.height - DialogBox.BoxHeight) / 2f - 56;
            var sprite = UIFactory.Picture(root, oldPath, Mathf.Round(full.x / 2f - 56), Mathf.Round(y), 112, 112);

            await UIManager.FadeIn(0.2f);
            string oldName = p.Name;
            AudioManager.PlayMusic("evolution");
            await UIManager.Say($"¿Qué? ¡{oldName} está cambiando de cargo!");
            float delay = 0.3f;
            for (int i = 0; i < 14; i++)
            {
                bool showNew = i % 2 == 0;
                UIFactory.SetPicture(sprite, showNew ? newPath : oldPath);
                sprite.color = showNew ? new Color(0.2f, 0.2f, 0.2f) : Color.white;
                await Async.Seconds(delay);
                delay = Mathf.Max(0.06f, delay * 0.82f);
            }
            UIFactory.SetPicture(sprite, newPath);
            sprite.color = Color.white;
            await UIManager.Flash(Color.white, 2);
            p.EvolveTo(target);
            GameState.MarkCaught(target);
            AudioManager.Sfx("levelup");
            await UIManager.Say($"¡Enhorabuena! ¡{oldName} ha ascendido a {p.Name}!");
            await UIManager.FadeOut(0.2f);
            Object.Destroy(go);
        }
    }
}
