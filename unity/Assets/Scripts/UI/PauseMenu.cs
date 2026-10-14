using System.Collections.Generic;
using Politicmon.Audio;
using Politicmon.Battle;
using Politicmon.Core;
using Politicmon.Save;
using UnityEngine;

namespace Politicmon.UI
{
    /// <summary>START menu: Equipo, Politidex, Objetos, Ficha, Guardar, Opciones, Salir.</summary>
    public static class PauseMenu
    {
        public static async Awaitable Open()
        {
            int last = 0;
            while (true)
            {
                var options = new List<MenuOption>
                {
                    new("EQUIPO", "", GameState.Party.Count > 0), "POLITIDEX", "OBJETOS", "FICHA", "GUARDAR", "OPCIONES", "SALIR",
                };
                int choice = await UIManager.Choose(options, new MenuSettings
                {
                    Anchor = MenuAnchor.TopRight, Point = new Vector2(UIManager.Area.x, 0), Start = last, MinWidth = 60,
                });
                if (choice < 0 || choice == 6)
                    return;
                last = choice;
                switch (choice)
                {
                    case 0: await PartyMenu(); break;
                    case 1: await UIManager.Show(UIManager.Create<DexScreen>("Politidex")); break;
                    case 2: await BagMenu(); break;
                    case 3: await UIManager.Show(UIManager.Create<TrainerCard>("Ficha")); break;
                    case 4: await SaveGame(); break;
                    case 5: await OptionsMenu(); break;
                }
            }
        }

        static async Awaitable SaveGame()
        {
            if (!await UIManager.YesNo("¿Quieres guardar la partida?"))
                return;
            if (SaveSystem.Save())
            {
                AudioManager.Sfx("save");
                await UIManager.Say($"¡{GameState.Progress.playerName} ha guardado la partida! Promesa cumplida.");
            }
            else
            {
                await UIManager.Say("No se ha podido guardar. Culpa de la oposición.");
            }
        }

        static async Awaitable PartyMenu()
        {
            var screen = PartyScreen.Present(GameState.Party);
            int i = await screen.Reopen();
            while (i >= 0)
            {
                int action = await UIManager.Choose(new List<MenuOption> { "FICHA", "MOVER", "CANCELAR" },
                    new MenuSettings { Point = UIManager.Area - new Vector2(0, 30) });
                if (action == 0)
                {
                    var card = UIManager.Create<SummaryScreen>("Summary");
                    card.Setup(GameState.Party[i]);
                    await UIManager.Show(card);
                }
                else if (action == 1)
                {
                    screen.SwapFrom = i;
                    int j = await screen.Reopen(null, "¿Con quién lo intercambias?");
                    screen.SwapFrom = -1;
                    if (j >= 0 && j != i)
                        (GameState.Party[i], GameState.Party[j]) = (GameState.Party[j], GameState.Party[i]);
                    i = await screen.Reopen(GameState.Party, "Elige un afiliado.");
                    continue;
                }
                i = await screen.Reopen();
            }
            screen.Close();
        }

        static async Awaitable BagMenu()
        {
            while (true)
            {
                var ids = new List<string>();
                var options = new List<MenuOption>();
                foreach (var id in GameData.ItemIds)
                {
                    if (GameState.ItemCount(id) <= 0)
                        continue;
                    ids.Add(id);
                    options.Add(new MenuOption(GameData.Item(id).name, "x" + GameState.ItemCount(id)));
                }
                if (ids.Count == 0)
                {
                    await UIManager.Say($"La mochila está vacía. Escaños: {GameState.BadgeCount}/3   Fondos: {GameState.Money}€");
                    return;
                }
                int choice = await UIManager.Choose(options, new MenuSettings
                {
                    Title = "OBJETOS", Anchor = MenuAnchor.TopRight, Point = new Vector2(UIManager.Area.x, 0), MaxRows = 8,
                    OnHover = k => UIManager.SetDialogText(GameData.Item(ids[k]).description),
                });
                UIManager.CloseDialog();
                if (choice < 0)
                    return;
                var item = GameData.Item(ids[choice]);
                if (!item.UsableInField)
                {
                    await UIManager.Say(item.name + " solo se puede usar en un debate.");
                    continue;
                }
                await UseOnParty(item.id);
            }
        }

        static async Awaitable UseOnParty(string itemId)
        {
            var item = GameData.Item(itemId);
            var screen = PartyScreen.Present(GameState.Party, $"¿Con quién usas {item.name}?");
            while (GameState.ItemCount(itemId) > 0)
            {
                int k = await screen.Reopen(GameState.Party);
                if (k < 0)
                    break;
                var p = GameState.Party[k];
                if (!ItemEffects.CanUse(itemId, p))
                {
                    AudioManager.Sfx("error");
                    screen.SetPrompt("No tendría ningún efecto.");
                    continue;
                }
                string message = ItemEffects.Apply(itemId, p);
                GameState.RemoveItem(itemId);
                AudioManager.Sfx("heal");
                screen.SetPrompt(message);
            }
            if (GameState.ItemCount(itemId) <= 0)
                await Async.Seconds(0.6f);
            screen.Close();
        }

        static async Awaitable OptionsMenu()
        {
            while (true)
            {
                var audio = AudioManager.Instance;
                var touch = TouchControls.Instance;
                var options = new List<MenuOption>
                {
                    new("SONIDO", audio != null && audio.Muted ? "NO" : "SÍ"),
                    new("CONTROLES TÁCTILES", touch == null || touch.Enabled ? "SÍ" : "NO"),
                    "VOLVER",
                };
                int choice = await UIManager.Choose(options, new MenuSettings { Anchor = MenuAnchor.TopRight, Point = new Vector2(UIManager.Area.x, 0) });
                if (choice == 0)
                    AudioManager.SetMuted(!(audio != null && audio.Muted));
                else if (choice == 1 && touch != null)
                    touch.Enabled = !touch.Enabled;
                else
                    return;
            }
        }
    }
}
