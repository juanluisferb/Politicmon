using Politicmon.Audio;
using Politicmon.Battle;
using Politicmon.Overworld;
using Politicmon.Save;
using Politicmon.UI;
using UnityEngine;

namespace Politicmon.Core
{
    /// <summary>
    /// Switches between the big screens of the game: title, overworld, battle, evolution and ending.
    /// </summary>
    public static class GameFlow
    {
        static Transform root;
        static GameObject currentScreen;

        public static bool InBattle { get; private set; }
        public static bool TouchControlsAllowed => OverworldScreen.Instance != null && !InBattle;

        public static void Initialize(Transform persistentRoot) => root = persistentRoot;

        static void SetScreen(GameObject screen)
        {
            if (currentScreen != null)
                Object.Destroy(currentScreen);
            currentScreen = screen;
        }

        public static void ShowTitle()
        {
            GameState.GameActive = false;
            UIManager.SetFade(0f);
            SetScreen(TitleScreen.Create().gameObject);
        }

        public static void StartNewGame(string playerName)
        {
            GameState.NewGame(playerName);
            StartOverworld("madrid_garaje", new Vector2Int(4, 5), "up");
        }

        public static bool ContinueGame()
        {
            if (!SaveSystem.Load())
                return false;
            var p = GameState.Progress;
            StartOverworld(p.mapId, new Vector2Int(p.x, p.y), p.facing);
            return true;
        }

        public static void StartOverworld(string mapId, Vector2Int cell, string facing)
        {
            SetScreen(OverworldScreen.Create(mapId, cell, facing).gameObject);
        }

        /// <summary>Hides the overworld, runs a battle, plays pending evolutions and comes back.</summary>
        public static async Awaitable<BattleResult> StartBattle(BattleConfig config)
        {
            InBattle = true;
            UIManager.PushBusy();
            string musicBefore = AudioManager.Instance != null ? AudioManager.Instance.CurrentMusic : "";
            var world = OverworldScreen.Instance;
            if (world != null)
                world.gameObject.SetActive(false);
            UIManager.SetFade(0f);

            var battle = BattleScreen.Create(config);
            BattleResult result;
            try
            {
                result = await battle.Run();
            }
            finally
            {
                Object.Destroy(battle.gameObject);
            }

            foreach (var p in result.Evolutions)
                await EvolutionScreen.Play(p);

            if (world != null)
                world.gameObject.SetActive(true);
            UIManager.PopBusy();
            InBattle = false;
            if (result.Outcome != BattleOutcome.Lose && !string.IsNullOrEmpty(musicBefore))
                AudioManager.PlayMusic(musicBefore);
            return result;
        }

        public static async Awaitable ShowVictory()
        {
            await UIManager.FadeOut(0.6f);
            SetScreen(VictoryScreen.Create().gameObject);
            await UIManager.FadeIn(0.6f);
        }
    }
}
