using System.Collections.Generic;
using Politicmon.Audio;
using Politicmon.Core;
using Politicmon.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.UI
{
    /// <summary>Title: logo, the Eterno Candidato, a parade of politician icons and the main menu.</summary>
    public class TitleScreen : MonoBehaviour
    {
        static readonly int[] Parade = { 1, 4, 6, 8, 10, 13, 15, 17, 18, 20, 21, 24 };
        static readonly string[] Names = { "ALEX", "SAM", "NOA", "DANI", "ARI", "CRIS" };

        RectTransform root;
        Image background;
        Image stripe;
        PixelText logo;
        PixelText subtitle;
        PixelText pressStart;
        PixelText credit;
        Image hero;
        readonly List<Image> parade = new();
        bool waitingForStart = true;
        float time;

        public static TitleScreen Create()
        {
            var go = new GameObject("TitleScreen");
            UIManager.CreateCanvas(go, 1, out _);
            var screen = go.AddComponent<TitleScreen>();
            screen.Build();
            return screen;
        }

        void Build()
        {
            root = UIFactory.Create("Root", transform);
            background = UIFactory.Solid(root, UIFactory.Hex("#f8e8a0"), 0, 0, 1, 1);
            stripe = UIFactory.Solid(root, UIFactory.Hex("#e8c050"), 0, 0, 1, 22);
            logo = UIFactory.Label(root, "POLITICMON", 0, 0);
            logo.rectTransform.localScale = new Vector3(3, 3, 1);
            subtitle = UIFactory.Label(root, "Edición Escaño · Sátira ficticia", 0, 0, UIFactory.TextLight);
            hero = UIFactory.Picture(root, Core.GameData.PoliticianSpritePath(25, "front"), 0, 0, 112, 112);
            foreach (int id in Parade)
                parade.Add(UIFactory.Picture(root, Core.GameData.PoliticianSpritePath(id, "icon"), 0, 0));
            pressStart = UIFactory.Label(root, "PULSA A PARA EMPEZAR", 0, 0);
            credit = UIFactory.Label(root, "Personajes y partidos 100% ficticios", 0, 0, UIFactory.TextLight);
            ScreenScaler.LayoutChanged += Layout;
            Layout();
            AudioManager.PlayMusic("title");
        }

        void OnDestroy() => ScreenScaler.LayoutChanged -= Layout;

        static float Centered(float fullWidth, string text, int scale = 1) => Mathf.Floor((fullWidth - PixelFont.Measure(text) * scale) / 2f);

        void Layout()
        {
            var full = (Vector2)ScreenScaler.LogicalSize;
            var safe = ScreenScaler.SafeArea;
            UIFactory.Place(root, 0, 0, full.x, full.y);
            UIFactory.Place(background.rectTransform, 0, 0, full.x, full.y);
            float top = safe.yMin + Mathf.Max(16f, (safe.height - 250f) / 2f);
            UIFactory.Move(logo.rectTransform, Centered(full.x, logo.Text, 3), top);
            UIFactory.Move(subtitle.rectTransform, Centered(full.x, subtitle.Text), top + 30);
            float heroY = top + 46;
            UIFactory.Move(hero.rectTransform, Mathf.Floor(full.x / 2f - 56), heroY);
            UIFactory.Place(stripe.rectTransform, 0, heroY + 96, full.x, 22);
            UIFactory.Move(pressStart.rectTransform, Centered(full.x, pressStart.Text), heroY + 136);
            UIFactory.Move(credit.rectTransform, Centered(full.x, credit.Text), safe.yMax - 14);
        }

        void Update()
        {
            time += Time.deltaTime;
            pressStart.enabled = waitingForStart && time % 1f < 0.65f;
            float width = ScreenScaler.LogicalSize.x;
            float span = Parade.Length * 24f;
            float stripeY = UIFactory.PositionOf(stripe.rectTransform).y;
            for (int i = 0; i < parade.Count; i++)
            {
                float x = (time * 20f + i * 24f) % span - 24f;
                if (x > width)
                    x -= span;
                UIFactory.Move(parade[i].rectTransform, Mathf.Round(x), stripeY + 3 + ((int)(time * 4 + i) % 2 == 0 ? 2 : 0));
            }

            if (waitingForStart && !UIManager.IsBusy &&
                (GameInput.Pressed(GameButton.A) || GameInput.Pressed(GameButton.Start) || GameInput.Tapped(out _)))
            {
                waitingForStart = false;
                AudioManager.Sfx("select");
                Async.Run(MainMenu);
            }
        }

        async Awaitable MainMenu()
        {
            while (true)
            {
                bool hasSave = SaveSystem.HasSave;
                var options = new List<MenuOption>();
                if (hasSave)
                    options.Add("CONTINUAR");
                options.Add("NUEVA PARTIDA");
                options.Add("CRÉDITOS");
                float menuY = UIFactory.PositionOf(pressStart.rectTransform).y - ScreenScaler.SafeArea.yMin + 4;
                int choice = await UIManager.Choose(options, new MenuSettings
                {
                    Anchor = MenuAnchor.Center, Point = new Vector2(UIManager.Area.x / 2f, menuY), MinWidth = 90,
                });
                if (choice < 0)
                {
                    waitingForStart = true;
                    return;
                }
                switch (options[choice].Label)
                {
                    case "CONTINUAR":
                        await UIManager.FadeOut(0.4f);
                        if (GameFlow.ContinueGame())
                            return;
                        await UIManager.FadeIn(0.2f);
                        await UIManager.Say("La partida guardada está dañada. Culpa de las cloacas.");
                        break;
                    case "NUEVA PARTIDA":
                        if (hasSave && !await UIManager.YesNo("Ya hay una partida guardada. Si guardas la nueva, se sobrescribirá. ¿Seguir?"))
                            break;
                        string name = await ChooseName();
                        if (string.IsNullOrEmpty(name))
                            break;
                        await UIManager.FadeOut(0.5f);
                        GameFlow.StartNewGame(name);
                        return;
                    default:
                        await UIManager.SayAll(new[]
                        {
                            "POLITICMON. Juego, código, gráficos, fuente, música y sonido generados para esta versión.",
                            "Motor: Unity 6. Todos los personajes y partidos son ficticios: cualquier parecido es pura casualidad electoral.",
                        });
                        break;
                }
            }
        }

        static async Awaitable<string> ChooseName()
        {
            await UIManager.Say("¿Cómo se llama quien funda el nuevo partido?", false);
            var options = new List<MenuOption>();
            foreach (var n in Names)
                options.Add(n);
            options.Add("ESCRIBIR...");
            int choice = await UIManager.Choose(options);
            UIManager.CloseDialog();
            if (choice < 0)
                return "";
            if (choice < Names.Length)
                return Names[choice];
            var entry = UIManager.Create<NameEntry>("NameEntry");
            await UIManager.Show(entry, false);
            string value = entry.Value;
            Destroy(entry.gameObject);
            return value;
        }
    }
}
