using System.Collections.Generic;
using System.Linq;
using Politicmon.Audio;
using Politicmon.Battle;
using Politicmon.Core;
using Politicmon.Data;
using Politicmon.UI;
using UnityEngine;

namespace Politicmon.Overworld
{
    /// <summary>
    /// Story and map events: the intro with the starter choice, NPC talk, trainers that spot you,
    /// gym leaders, wild encounters, the party HQ (heal, shop, storage), items and the legendary.
    /// </summary>
    public class OverworldEvents
    {
        static readonly int[] Starters = { 1, 10, 15 };
        static readonly Dictionary<string, (string item, int count)> LeaderGifts = new()
        {
            { "leader_madrid", ("carnet_premium", 3) },
            { "leader_andalucia", ("puesto_lista", 2) },
            { "leader_galicia", ("puesto_lista", 3) },
        };
        const int LegendLevel = 36;

        readonly OverworldScreen world;

        public OverworldEvents(OverworldScreen screen) => world = screen;

        public async Awaitable OnMapLoaded()
        {
            if (world.MapId == "madrid_garaje" && !GameState.HasFlag("has_starter"))
            {
                world.Busy = true;
                await StarterIntro();
                world.Busy = false;
            }
        }

        // ---------------------------------------------------------------- talking

        public async Awaitable TalkTo(Character npc)
        {
            var data = npc.Npc;
            if (data.eventType != "heal" && data.eventType != "shop")
                npc.Face(Character.Opposite(world.Player.Facing));

            switch (data.eventType)
            {
                case "trainer":
                    if (GameState.TrainerDefeated(data.trainer))
                    {
                        var t = GameData.Trainer(data.trainer);
                        await UIManager.Say($"{t.name}: «{t.afterLine}»");
                    }
                    else
                    {
                        await TrainerBattle(npc);
                    }
                    break;
                case "starter":
                    if (GameState.HasFlag("has_starter"))
                        await UIManager.SayAll(new[]
                        {
                            "Profesora Urna: «Recuerda: gana los tres Escaños (Madrid, Andalucía y Galicia) y el país será tuyo.»",
                            "«Si un afiliado tiene poco Carisma o está en un Escándalo, es más fácil que se una al partido.»",
                        });
                    else
                        await StarterIntro();
                    break;
                case "heal":
                    await Heal();
                    break;
                case "shop":
                    await Shop(data.stock);
                    break;
                case "legend":
                    await Legend();
                    break;
                default:
                    await UIManager.SayAll(data.lines.Count > 0 ? data.lines : new List<string> { "..." });
                    break;
            }
        }

        // ---------------------------------------------------------------- starter

        async Awaitable StarterIntro()
        {
            string name = GameState.Progress.playerName;
            await UIManager.SayAll(new[]
            {
                $"Profesora Urna: «¡{name}! ¡Por fin despiertas! Bienvenida, bienvenido al Partido Nuevo.»",
                "«Lo fundaste anoche en este garaje, entre dos cajas de folletos y una urna de cartón.»",
                "«Un partido sin afiliados es solo un logo. Por suerte, tengo tres fichas de afiliación sobre la mesa.»",
            });

            while (true)
            {
                var preview = UIFactory.Container(UIManager.Instance.Root, "StarterPreview", 4, 8, 64, 64);
                UIFactory.Panel(preview, 0, 0, 64, 64);
                var picture = UIFactory.Picture(preview, null, 4, 4, 56, 56);
                var options = Starters.Select(id =>
                {
                    var s = GameData.Species(id);
                    return new MenuOption(s.name, GameData.TypeName(s.types[0]));
                }).ToList();
                await UIManager.Say("«¿Con quién quieres empezar tu carrera política?»", false);
                int choice = await UIManager.Choose(options, new MenuSettings
                {
                    AllowCancel = false,
                    OnHover = i => UIFactory.SetPicture(picture, GameData.PoliticianSpritePath(Starters[i], "front")),
                });
                Object.Destroy(preview.gameObject);
                UIManager.CloseDialog();

                var species = GameData.Species(Starters[choice]);
                await UIManager.Say(species.description);
                if (await UIManager.YesNo($"¿Afilias a {species.name}, de tipo {GameData.TypeName(species.types[0])}?"))
                {
                    GameState.AddPolitician(Politician.Create(species.id, 5));
                    GameState.SetFlag("has_starter");
                    AudioManager.Sfx("capture");
                    await UIManager.Say($"¡{species.name} se une al Partido Nuevo!");
                    break;
                }
            }

            await UIManager.SayAll(new[]
            {
                "Profesora Urna: «¡Estupendo! Toma también estos 5 Carnets de afiliación y 3 Mítines para el camino.»",
                "«En la hierba alta hay políticos sin partido. Debilítalos en un debate y ofréceles un Carnet.»",
                "«Gana los Escaños de los tres Parlamentos: Madrid, Andalucía y Galicia. Empieza por la Asamblea, al norte de la Gran Vía.»",
                $"«Si tus afiliados dimiten de cansancio, la Sede del Partido los pone a punto. ¡Suerte, {name}!»",
            });
            if (world.Npcs.TryGetValue("mentor", out var mentor))
                mentor.Face("down");
        }

        // ---------------------------------------------------------------- trainers

        /// <summary>Checks the line of sight of every undefeated trainer. Returns true if a battle happened.</summary>
        public async Awaitable<bool> CheckTrainers()
        {
            foreach (var npc in world.Npcs.Values.ToList())
            {
                var data = npc.Npc;
                if (data.eventType != "trainer" || data.sight <= 0 || GameState.TrainerDefeated(data.trainer))
                    continue;
                var step = Character.DirectionVector(npc.Facing);
                for (int i = 1; i <= data.sight; i++)
                {
                    var cell = npc.Cell + step * i;
                    if (cell == world.Player.Cell)
                    {
                        await Spotted(npc);
                        return true;
                    }
                    if ((world.Map.TileAt(cell)?.solid ?? true) || world.NpcAt(cell) != null)
                        break;
                }
            }
            return false;
        }

        async Awaitable Spotted(Character npc)
        {
            world.Busy = true;
            world.Player.StopWalking();
            var bubble = npc.ShowExclamation();
            AudioManager.PlayMusic("encounter");
            AudioManager.Sfx("exclaim");
            await Async.Seconds(0.6f);
            Object.Destroy(bubble);
            while ((npc.Cell - world.Player.Cell).sqrMagnitude > 1)
                await npc.StepTo(npc.Cell + Character.DirectionVector(npc.Facing), 0.18f);
            npc.StopWalking();
            world.Player.Face(Character.Opposite(npc.Facing));
            await TrainerBattle(npc);
            world.Busy = false;
        }

        async Awaitable TrainerBattle(Character npc)
        {
            var trainer = GameData.Trainer(npc.Npc.trainer);
            var team = trainer.team.Select(m => Politician.Create(m.species, m.level)).ToList();
            if (!trainer.isLeader)
                AudioManager.PlayMusic("encounter");
            await UIManager.Say($"{trainer.name}: «{trainer.intro}»");
            var result = await GameFlow.StartBattle(new BattleConfig
            {
                Enemies = team,
                TrainerId = trainer.id,
                Zone = world.Map.Data.zone,
                Music = trainer.isLeader ? "gym" : "battle",
            });
            await AfterBattle(result);
            if (result.Outcome != BattleOutcome.Win)
                return;
            GameState.MarkTrainerDefeated(trainer.id);
            if (trainer.isLeader)
                await LeaderReward(trainer);
        }

        async Awaitable LeaderReward(TrainerData trainer)
        {
            GameState.AddBadge(trainer.badge);
            AudioManager.PlayMusic("badge");
            await UIManager.Say($"¡{trainer.name} te entrega el {trainer.badgeName}!");
            if (LeaderGifts.TryGetValue(trainer.id, out var gift))
            {
                GameState.AddItem(gift.item, gift.count);
                await UIManager.Say($"También recibes {gift.count} x {GameData.Item(gift.item).name}.");
            }
            await UIManager.Say($"{trainer.name}: «{trainer.afterLine}»");
            if (GameState.BadgeCount >= 3)
                await UIManager.Say("¡Tienes los tres Escaños! En Madrid, las puertas del Hemiciclo Nacional ya están abiertas...");
            AudioManager.PlayMusic(world.Map.Data.music);
        }

        // ---------------------------------------------------------------- wild encounters

        public async Awaitable WildBattle(MapData map)
        {
            int total = map.encounters.Sum(e => e.weight);
            int roll = GameState.Rng.Next(1, total + 1);
            var pick = map.encounters[0];
            foreach (var e in map.encounters)
            {
                roll -= e.weight;
                if (roll <= 0)
                {
                    pick = e;
                    break;
                }
            }
            int level = GameState.Rng.Next(pick.minLevel, pick.maxLevel + 1);
            world.Busy = true;
            var result = await GameFlow.StartBattle(new BattleConfig
            {
                Enemies = new List<Politician> { Politician.Create(pick.species, level) },
                Zone = map.zone,
                Music = "battle",
            });
            await AfterBattle(result);
            world.Busy = false;
        }

        async Awaitable AfterBattle(BattleResult result)
        {
            if (result.Outcome == BattleOutcome.Lose)
            {
                await Respawn();
                return;
            }
            UIManager.SetFade(1f);
            await UIManager.FadeIn(0.25f);
        }

        async Awaitable Respawn()
        {
            GameState.HealParty();
            var p = GameState.Progress;
            world.LoadMap(p.respawnMap, new Vector2Int(p.respawnX, p.respawnY), "up");
            await UIManager.FadeIn(0.3f);
            await UIManager.Say("Tus afiliados se recuperan en la Sede. «Una derrota es solo una encuesta con mala suerte.»");
        }

        // ---------------------------------------------------------------- party HQ (Sede del Partido)

        async Awaitable Heal()
        {
            await UIManager.Say("¡Bienvenida, bienvenido a la Sede del Partido! Aquí ponemos a punto a tus afiliados.");
            if (!await UIManager.YesNo("¿Quieres que tus afiliados descansen?"))
            {
                await UIManager.Say("¡Vuelve cuando quieras!");
                return;
            }
            await UIManager.FadeOut(0.2f);
            AudioManager.Sfx("heal");
            GameState.HealParty();
            await Async.Seconds(0.6f);
            await UIManager.FadeIn(0.2f);
            GameState.SetRespawn(world.MapId, 4, 4);
            await UIManager.Say("¡Listos para el próximo mitin! Solo una foto para redes... ¡Patata! Vuelve cuando quieras.");
        }

        async Awaitable Shop(List<string> stock)
        {
            await UIManager.Say("¡Hola! Tenemos todo lo necesario para una campaña. ¿Qué necesitas?");
            while (true)
            {
                var options = stock.Select(id => new MenuOption(GameData.Item(id).name, GameData.Item(id).price + "€")).ToList();
                int choice = await UIManager.Choose(options, new MenuSettings
                {
                    Title = $"FONDOS {GameState.Money}€",
                    OnHover = i => UIManager.SetDialogText(GameData.Item(stock[i]).description),
                });
                UIManager.CloseDialog();
                if (choice < 0)
                    break;

                var item = GameData.Item(stock[choice]);
                int max = Mathf.Min(9, GameState.Money / Mathf.Max(1, item.price));
                if (max <= 0)
                {
                    AudioManager.Sfx("error");
                    await UIManager.Say("No te llegan los fondos. Prueba con un crowdfunding.");
                    continue;
                }
                var amounts = Enumerable.Range(1, max).Select(q => new MenuOption("x" + q, q * item.price + "€")).ToList();
                await UIManager.Say($"¿Cuántos {item.name} quieres?", false);
                int quantity = await UIManager.Choose(amounts, new MenuSettings { MaxRows = 5 });
                UIManager.CloseDialog();
                if (quantity < 0)
                    continue;
                int count = quantity + 1;
                GameState.Money -= count * item.price;
                GameState.AddItem(item.id, count);
                AudioManager.Sfx("buy");
                await UIManager.Say($"¡Aquí tienes {count} x {item.name}! Gracias por tu donación.");
            }
            await UIManager.Say("¡Vuelve pronto! Y vota con cabeza.");
        }

        /// <summary>"Secretaría de Organización": moves politicians between the party and the box.</summary>
        public async Awaitable PartyStorage()
        {
            await UIManager.Say("Secretaría de Organización. Aquí esperan los afiliados que no caben en tu equipo.");
            while (true)
            {
                await UIManager.Say($"¿Qué quieres hacer? (Reserva: {GameState.Box.Count})", false);
                int choice = await UIManager.Choose(new List<MenuOption> { "SACAR AFILIADO", "DEJAR AFILIADO", "SALIR" });
                UIManager.CloseDialog();
                if (choice == 0)
                {
                    if (GameState.Box.Count == 0)
                    {
                        await UIManager.Say("No hay nadie en la reserva.");
                        continue;
                    }
                    if (GameState.Party.Count >= GameState.PartyMax)
                    {
                        await UIManager.Say("Tu equipo está completo. Deja antes a alguien.");
                        continue;
                    }
                    var options = GameState.Box.Select(p => new MenuOption(p.Name, "Nv" + p.level)).ToList();
                    int k = await UIManager.Choose(options, new MenuSettings { MaxRows = 6, Anchor = MenuAnchor.Center, Point = UIManager.Area / 2f });
                    if (k >= 0)
                    {
                        var p = GameState.Box[k];
                        GameState.Box.RemoveAt(k);
                        GameState.Party.Add(p);
                        await UIManager.Say(p.Name + " vuelve a tu equipo.");
                    }
                }
                else if (choice == 1)
                {
                    if (GameState.Party.Count <= 1)
                    {
                        await UIManager.Say("¡No puedes quedarte sin afiliados en el equipo!");
                        continue;
                    }
                    var screen = PartyScreen.Present(GameState.Party, "¿A quién dejas en la reserva?");
                    int k = await screen.Reopen();
                    screen.Close();
                    if (k >= 0)
                    {
                        var p = GameState.Party[k];
                        GameState.Party.RemoveAt(k);
                        GameState.Box.Add(p);
                        await UIManager.Say(p.Name + " se queda en la Sede, repartiendo folletos.");
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public async Awaitable PickUp(Vector2Int cell)
        {
            var item = world.Items[cell].data;
            GameState.AddItem(item.item, item.count);
            GameState.SetFlag("item:" + item.id);
            world.RemoveItem(cell);
            AudioManager.Sfx("item");
            await UIManager.Say($"¡Has encontrado {item.count} x {GameData.Item(item.item).name}!");
        }

        // ---------------------------------------------------------------- legendary

        async Awaitable Legend()
        {
            await UIManager.SayAll(new[]
            {
                "Una figura imponente sonríe desde el estrado. La sonrisa no se mueve. Nunca se ha movido.",
                "El Eterno Candidato: «Llevo en campaña desde que se inventaron las urnas. ¿Vienes a votarme?»",
                "«¿No? Entonces... ¡DEBATE!»",
            });
            var result = await GameFlow.StartBattle(new BattleConfig
            {
                Enemies = new List<Politician> { Politician.Create(25, LegendLevel) },
                Music = "legend",
            });
            await AfterBattle(result);
            switch (result.Outcome)
            {
                case BattleOutcome.Caught:
                    GameState.SetFlag("legend_done");
                    GameState.SetFlag("legend_caught");
                    world.RemoveNpc("eterno");
                    await UIManager.Say("El Eterno Candidato se afilia al Partido Nuevo. Por primera vez, su sonrisa parece sincera.");
                    await GameFlow.ShowVictory();
                    break;
                case BattleOutcome.Win:
                    GameState.SetFlag("legend_done");
                    world.RemoveNpc("eterno");
                    await UIManager.Say("El Eterno Candidato recoge sus carteles. «Volveré... en las próximas.» Y se marcha sonriendo.");
                    await GameFlow.ShowVictory();
                    break;
                case BattleOutcome.Run:
                    await UIManager.Say("El Eterno Candidato sigue ahí, sonriendo. Tiene todo el tiempo del mundo.");
                    break;
            }
        }
    }
}
