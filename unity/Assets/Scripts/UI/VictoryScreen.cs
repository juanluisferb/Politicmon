using Politicmon.Audio;
using Politicmon.Core;
using Politicmon.Save;
using UnityEngine;

namespace Politicmon.UI
{
    /// <summary>Ending: scrolling credits, play stats and the final party. Saves the completed game.</summary>
    public class VictoryScreen : MonoBehaviour
    {
        static readonly string[] Credits =
        {
            "¡HAS GANADO LAS ELECCIONES!", "",
            "El Partido Nuevo, fundado en un garaje,", "gobierna ahora con mayoría absoluta.", "",
            "Escaños conseguidos: 3 de 3", "{stats}", "",
            "— CRÉDITOS —", "",
            "Diseño, código y datos", "generados para Politicmon", "",
            "Pixel art Game Boy Color", "dibujado píxel a píxel por script", "",
            "Música chiptune", "cuadradas y ruido, por script", "",
            "Motor: Unity 6", "",
            "Todos los personajes y partidos", "son ficticios. Ninguno representa", "a personas ni partidos reales.", "",
            "Gracias por jugar.", "Y recuerda: vota siempre.",
        };

        RectTransform scroller;
        PixelText endLabel;
        float scrollHeight;
        float time;
        bool done;

        public static VictoryScreen Create()
        {
            var go = new GameObject("VictoryScreen");
            UIManager.CreateCanvas(go, 2, out _);
            var screen = go.AddComponent<VictoryScreen>();
            screen.Build();
            return screen;
        }

        void Build()
        {
            var full = (Vector2)ScreenScaler.LogicalSize;
            var root = UIFactory.Container(transform, "Root", 0, 0, full.x, full.y);
            UIFactory.Solid(root, UIFactory.Hex("#101820"), 0, 0, full.x, full.y);
            scroller = UIFactory.Container(root, "Credits", 0, full.y, full.x, 10);
            var light = UIFactory.Hex("#f8f0d0");
            float y = 0;
            string stats = $"Afiliados en la Politidex: {GameState.Progress.caught.Count} · Tiempo: {GameState.PlayTimeText}";
            foreach (var raw in Credits)
            {
                string line = raw == "{stats}" ? stats : raw;
                foreach (var sub in PixelFont.Wrap(line, full.x - 16))
                {
                    UIFactory.Label(scroller, sub, Mathf.Floor((full.x - PixelFont.Measure(sub)) / 2f), y, light);
                    y += 12;
                }
            }
            y += 12;
            float x = 8;
            foreach (var p in GameState.Party)
            {
                UIFactory.Picture(scroller, GameData.PoliticianSpritePath(p.speciesId, "front"), x, y);
                x += 58;
                if (x > full.x - 56)
                {
                    x = 8;
                    y += 58;
                }
            }
            scrollHeight = y + 70;
            endLabel = UIFactory.Label(root, "FIN · Pulsa A", 0, full.y - ScreenScaler.SafeArea.yMin - 20, UIFactory.Hex("#f8e8a0"));
            UIFactory.Move(endLabel.rectTransform, Mathf.Floor((full.x - PixelFont.Measure(endLabel.Text)) / 2f),
                ScreenScaler.SafeArea.yMax - 20);
            endLabel.enabled = false;
            AudioManager.PlayMusic("ending");
            GameState.SetFlag("game_completed");
            SaveSystem.Save();
        }

        void Update()
        {
            time += Time.deltaTime;
            float full = ScreenScaler.LogicalSize.y;
            float target = Mathf.Min(full * 0.1f, full - scrollHeight);
            float y = Mathf.Max(target, full - time * 18f);
            UIFactory.Move(scroller, 0, Mathf.Round(y));
            done = y <= target;
            endLabel.enabled = done && time % 1f < 0.7f;

            if (GameInput.Pressed(GameButton.A) || GameInput.Tapped(out _))
            {
                if (done)
                    GameFlow.ShowTitle();
                else
                    time += 4f;
            }
        }
    }
}
