using System.Collections.Generic;
using System.Linq;
using Politicmon.Audio;
using Politicmon.Core;
using Politicmon.Data;
using Politicmon.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.Battle
{
    /// <summary>
    /// Full-screen 1 vs 1 turn based battle ("debate"). Build it with <see cref="Create"/> and
    /// await <see cref="Run"/>. Texts and menus go through <see cref="UIManager"/>; formulas
    /// live in <see cref="BattleLogic"/>.
    /// </summary>
    public class BattleScreen : MonoBehaviour
    {
        static readonly string[] FailLines =
        {
            "¡Oh, no! ¡Ha roto el carnet en directo!",
            "¡Casi! Solo quería hacerse una foto contigo.",
            "¡Prefiere seguir siendo independiente!",
            "¡Ha pedido leer la letra pequeña primero!",
        };

        static readonly Dictionary<Stat, string> StatNames = new()
        {
            { Stat.Oratoria, "Oratoria" }, { Stat.Aguante, "Aguante" }, { Stat.Dialectica, "Dialéctica" },
            { Stat.Templanza, "Templanza" }, { Stat.Agilidad, "Agilidad" }, { Stat.Carisma, "Carisma" },
        };

        BattleConfig config;
        TrainerData trainer;
        System.Random rng;
        Politician enemy;
        Politician player;
        int enemyIndex;
        int runAttempts;
        BattleOutcome outcome = BattleOutcome.None;
        readonly List<Politician> participants = new();
        readonly List<Politician> evolutions = new();
        readonly HashSet<Politician> homeAnnounced = new();

        RectTransform root;
        Image skyRect;
        Image groundRect;
        Image enemySprite;
        Image playerSprite;
        Image trainerSprite;
        Image carnetSprite;
        Image enemyPlatform;
        Image playerPlatform;
        BattleHud enemyHud;
        BattleHud playerHud;
        Vector2 enemyHome;
        Vector2 playerHome;

        bool IsTrainer => config.IsTrainerBattle;

        // ---------------------------------------------------------------- creation & layout

        public static BattleScreen Create(BattleConfig config)
        {
            var go = new GameObject("BattleScreen");
            UIManager.CreateCanvas(go, 5, out _);
            var screen = go.AddComponent<BattleScreen>();
            screen.config = config;
            screen.trainer = config.IsTrainerBattle ? GameData.Trainer(config.TrainerId) : null;
            screen.rng = GameState.Rng;
            screen.BuildUI();
            return screen;
        }

        void BuildUI()
        {
            root = UIFactory.Create("Root", transform);
            skyRect = UIFactory.Solid(root, UIFactory.Hex("#f8f8f0"), 0, 0, 1, 1);
            groundRect = UIFactory.Solid(root, UIFactory.Hex("#e0e8d8"), 0, 0, 1, 1);
            enemyPlatform = UIFactory.Picture(root, "Sprites/UI/platform", 0, 0, 64, 12);
            playerPlatform = UIFactory.Picture(root, "Sprites/UI/platform", 0, 0, 96, 18);
            enemySprite = BottomCentered(UIFactory.Picture(root, null, 0, 0, 56, 56));
            trainerSprite = BottomCentered(UIFactory.Picture(root, null, 0, 0, 56, 56));
            playerSprite = BottomCentered(UIFactory.Picture(root, null, 0, 0, 64, 64));
            carnetSprite = UIFactory.Picture(root, null, 0, 0, 16, 16);
            carnetSprite.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            enemyHud = BattleHud.Create(root, false);
            playerHud = BattleHud.Create(root, true);
            enemyHud.gameObject.SetActive(false);
            playerHud.gameObject.SetActive(false);
            ScreenScaler.LayoutChanged += Layout;
            Layout();
        }

        void OnDestroy() => ScreenScaler.LayoutChanged -= Layout;

        /// <summary>Sprites scale and slide from their feet (bottom-center pivot).</summary>
        static Image BottomCentered(Image image)
        {
            image.rectTransform.pivot = new Vector2(0.5f, 0f);
            return image;
        }

        static void PlaceFeet(Image image, Vector2 topLeft)
        {
            var size = image.rectTransform.sizeDelta;
            image.rectTransform.anchoredPosition = new Vector2(topLeft.x + size.x / 2f, -(topLeft.y + size.y));
        }

        void Layout()
        {
            var full = (Vector2)ScreenScaler.LogicalSize;
            var safe = ScreenScaler.SafeArea;
            UIFactory.Place(root, 0, 0, full.x, full.y);
            float stageBottom = safe.yMax - DialogBox.BoxHeight;
            float stageTop = safe.yMin;
            float available = stageBottom - stageTop;
            float split = stageTop + available * 0.5f;
            UIFactory.Place(skyRect.rectTransform, 0, 0, full.x, split);
            UIFactory.Place(groundRect.rectTransform, 0, split, full.x, full.y - split);

            float enemyY = stageTop + Mathf.Clamp(available * 0.2f, 14f, 80f);
            enemyHome = new Vector2(Mathf.Round(safe.xMax - 56 - 8), Mathf.Round(enemyY));
            playerHome = new Vector2(Mathf.Round(safe.xMin + 8), Mathf.Round(stageBottom - 64));
            PlaceFeet(enemySprite, enemyHome);
            PlaceFeet(trainerSprite, enemyHome);
            PlaceFeet(playerSprite, playerHome);
            UIFactory.Move(enemyPlatform.rectTransform, enemyHome.x - 4, enemyHome.y + 48);
            UIFactory.Move(playerPlatform.rectTransform, safe.xMin - 8, stageBottom - 14);
            UIFactory.Move(enemyHud.transform as RectTransform, Mathf.Round(safe.xMin + 4), Mathf.Round(enemyY + 4));
            UIFactory.Move(playerHud.transform as RectTransform, Mathf.Round(safe.xMax - BattleHud.Width - 4),
                Mathf.Round(stageBottom - playerHud.Height - 8));
        }

        // ---------------------------------------------------------------- main loop

        public async Awaitable<BattleResult> Run()
        {
            AudioManager.PlayMusic(config.Music);
            foreach (var p in GameState.Party)
                p.ResetBattleState();
            await Intro();
            while (outcome == BattleOutcome.None)
            {
                var action = await ChooseAction();
                await RunTurn(action);
            }
            await End();
            return new BattleResult { Outcome = outcome, Evolutions = evolutions, TrainerId = config.TrainerId };
        }

        static Awaitable Say(string text) => UIManager.Say(text);

        string EnemyLabel(Politician p = null) => (p ?? enemy).Name + (IsTrainer ? " rival" : " salvaje");
        string NameOf(Politician p) => p == player ? p.Name : EnemyLabel(p);
        BattleHud HudOf(Politician p) => p == player ? playerHud : enemyHud;
        Image SpriteOf(Politician p) => p == player ? playerSprite : enemySprite;

        async Awaitable Intro()
        {
            await UIManager.Flash(Color.white, 3);
            float width = ScreenScaler.LogicalSize.x;
            UIFactory.SetPicture(playerSprite, "Sprites/Trainers/player_back");
            PlaceFeet(playerSprite, playerHome + new Vector2(width, 0));
            var opponent = IsTrainer ? trainerSprite : enemySprite;
            if (IsTrainer)
            {
                UIFactory.SetPicture(trainerSprite, "Sprites/Trainers/" + trainer.sprite);
                enemySprite.enabled = false;
            }
            else
            {
                enemy = config.Enemies[0];
                UIFactory.SetPicture(enemySprite, GameData.PoliticianSpritePath(enemy.speciesId, "front"));
            }
            PlaceFeet(opponent, enemyHome - new Vector2(width, 0));
            var playerFrom = playerSprite.rectTransform.anchoredPosition;
            var opponentFrom = opponent.rectTransform.anchoredPosition;
            await Async.Tween(0.6f, t =>
            {
                playerSprite.rectTransform.anchoredPosition = Vector2.Lerp(playerFrom, playerFrom - new Vector2(width, 0), t);
                opponent.rectTransform.anchoredPosition = Vector2.Lerp(opponentFrom, opponentFrom + new Vector2(width, 0), t);
            });

            if (IsTrainer)
            {
                var party = GameData.Party(trainer.party);
                await Say(party != null
                    ? $"¡{trainer.name}, del {party.name}, te reta a un debate!"
                    : $"¡{trainer.name} te reta a un debate!");
                await Async.MoveTo(trainerSprite.rectTransform, trainerSprite.rectTransform.anchoredPosition + new Vector2(width, 0), 0.35f);
                trainerSprite.enabled = false;
                await SendEnemy(0);
            }
            else
            {
                GameState.MarkSeen(enemy.speciesId);
                enemyHud.Bind(enemy);
                enemyHud.gameObject.SetActive(true);
                AudioManager.Sfx("cry");
                await Say($"¡Un {enemy.Name} salvaje te corta el paso!");
                await AnnounceHome(enemy);
            }

            await Async.MoveTo(playerSprite.rectTransform, playerSprite.rectTransform.anchoredPosition - new Vector2(width, 0), 0.3f);
            await SendPlayer(GameState.FirstAbleIndex());
        }

        async Awaitable SendEnemy(int index)
        {
            enemyIndex = index;
            enemy = config.Enemies[index];
            participants.Clear();
            GameState.MarkSeen(enemy.speciesId);
            UIFactory.SetPicture(enemySprite, GameData.PoliticianSpritePath(enemy.speciesId, "front"));
            PlaceFeet(enemySprite, enemyHome);
            enemySprite.rectTransform.localScale = Vector3.zero;
            await Say($"{trainer.name} saca a {enemy.Name}.");
            AudioManager.Sfx("cry");
            await Async.Tween(0.25f, t => enemySprite.rectTransform.localScale = Vector3.one * t);
            enemyHud.Bind(enemy);
            enemyHud.gameObject.SetActive(true);
            await AnnounceHome(enemy);
        }

        async Awaitable SendPlayer(int index)
        {
            player = GameState.Party[index];
            player.ResetBattleState();
            if (!participants.Contains(player))
                participants.Add(player);
            UIFactory.SetPicture(playerSprite, GameData.PoliticianSpritePath(player.speciesId, "back"));
            PlaceFeet(playerSprite, playerHome);
            playerSprite.rectTransform.localScale = Vector3.zero;
            AudioManager.Sfx("cry");
            await Async.Tween(0.25f, t => playerSprite.rectTransform.localScale = Vector3.one * t);
            playerHud.Bind(player);
            playerHud.gameObject.SetActive(true);
            await Say($"¡Adelante, {player.Name}!");
            await AnnounceHome(player);
        }

        async Awaitable AnnounceHome(Politician p)
        {
            if (BattleLogic.IsHome(p, config.Zone) && homeAnnounced.Add(p))
                await Say($"¡{p.Name} juega en casa! Su Hecho Diferencial le da ventaja.");
        }

        // ---------------------------------------------------------------- player choices

        async Awaitable<PlayerAction> ChooseAction()
        {
            while (true)
            {
                await UIManager.Say($"¿Qué hará {player.Name}?", false);
                int choice = await UIManager.Choose(new List<MenuOption> { "LUCHAR", "EQUIPO", "OBJETOS", "HUIR" },
                    new MenuSettings { AllowCancel = false, MinWidth = 50 });
                switch (choice)
                {
                    case 0:
                        if (!player.HasUsableMoves)
                        {
                            UIManager.CloseDialog();
                            return PlayerAction.Fight(-1);
                        }
                        int move = await ChooseMove();
                        if (move >= 0)
                        {
                            UIManager.CloseDialog();
                            return PlayerAction.Fight(move);
                        }
                        break;
                    case 1:
                        UIManager.CloseDialog();
                        int slot = await ChooseSwitch(false);
                        if (slot >= 0)
                            return PlayerAction.SwitchTo(slot);
                        break;
                    case 2:
                        UIManager.CloseDialog();
                        var itemAction = await ChooseItem();
                        if (itemAction.HasValue)
                            return itemAction.Value;
                        break;
                    default:
                        UIManager.CloseDialog();
                        return PlayerAction.RunAway();
                }
            }
        }

        Awaitable<int> ChooseMove()
        {
            var options = player.moves.Select(m =>
            {
                var data = GameData.Move(m.id);
                return new MenuOption(data?.name ?? "?", $"{m.pp}/{data?.pp ?? 0}", m.pp > 0);
            }).ToList();
            return UIManager.Choose(options, new MenuSettings
            {
                OnHover = i =>
                {
                    var data = GameData.Move(player.moves[i].id);
                    string category = data.category switch { "fisico" => "Físico", "especial" => "Especial", _ => "Estado" };
                    string power = data.power > 0 ? $" · Pot {data.power}" : "";
                    UIManager.SetDialogText($"Tipo {GameData.TypeName(data.type)} · {category}{power}");
                },
            });
        }

        async Awaitable<int> ChooseSwitch(bool forced)
        {
            int start = GameState.Party.FindIndex(p => !p.IsFainted && p != player);
            var screen = PartyScreen.Present(GameState.Party, forced ? "¿A quién sacas?" : "Elige un afiliado.", !forced, Mathf.Max(0, start));
            int result = -1;
            int i = await screen.Reopen();
            while (i >= 0)
            {
                var p = GameState.Party[i];
                if (p.IsFainted)
                {
                    screen.SetPrompt("¡Ha dimitido! No puede salir.");
                }
                else if (p == player && !player.IsFainted)
                {
                    screen.SetPrompt("¡Ya está en el debate!");
                }
                else
                {
                    result = i;
                    break;
                }
                AudioManager.Sfx("error");
                i = await screen.Reopen();
            }
            screen.Close();
            return result;
        }

        async Awaitable<PlayerAction?> ChooseItem()
        {
            var ids = GameData.ItemIds.Where(id => GameState.ItemCount(id) > 0 && GameData.Item(id).UsableInBattle).ToList();
            if (ids.Count == 0)
            {
                await Say("No tienes objetos que usar en un debate.");
                return null;
            }
            while (true)
            {
                var options = ids.Where(id => GameState.ItemCount(id) > 0)
                    .Select(id => new MenuOption(GameData.Item(id).name, "x" + GameState.ItemCount(id))).ToList();
                var shown = ids.Where(id => GameState.ItemCount(id) > 0).ToList();
                if (shown.Count == 0)
                    return null;
                int choice = await UIManager.Choose(options, new MenuSettings
                {
                    Title = "OBJETOS",
                    OnHover = k => UIManager.SetDialogText(GameData.Item(shown[k]).description),
                });
                UIManager.CloseDialog();
                if (choice < 0)
                    return null;
                var item = GameData.Item(shown[choice]);
                switch (item.kind)
                {
                    case "carnet":
                        if (IsTrainer)
                        {
                            await Say("¡No puedes afiliar a militantes de otro partido! Sería transfuguismo.");
                            continue;
                        }
                        return PlayerAction.Carnet(item.id);
                    case "scan":
                        GameState.RemoveItem(item.id);
                        var card = UIManager.Create<SummaryScreen>("Encuesta");
                        card.Setup(enemy, true);
                        await UIManager.Show(card);
                        await Say($"La Encuesta revela los datos de {enemy.Name}.");
                        continue;
                    default:
                        int target = await ChooseItemTarget(item);
                        if (target >= 0)
                            return PlayerAction.Use(item.id, target);
                        continue;
                }
            }
        }

        async Awaitable<int> ChooseItemTarget(ItemData item)
        {
            var screen = PartyScreen.Present(GameState.Party, $"¿Con quién usas {item.name}?");
            int result = -1;
            int i = await screen.Reopen();
            while (i >= 0)
            {
                if (ItemEffects.CanUse(item.id, GameState.Party[i]))
                {
                    result = i;
                    break;
                }
                screen.SetPrompt("No tendría ningún efecto.");
                AudioManager.Sfx("error");
                i = await screen.Reopen();
            }
            screen.Close();
            return result;
        }

        // ---------------------------------------------------------------- turn resolution

        async Awaitable RunTurn(PlayerAction action)
        {
            bool enemyActsAfter = true;
            switch (action.Type)
            {
                case PlayerAction.Kind.Run:
                    if (IsTrainer || !config.CanRun)
                    {
                        await Say("¡No puedes abandonar un debate a medias!");
                        return;
                    }
                    if ((float)rng.NextDouble() < BattleLogic.RunChance(player, enemy, runAttempts++))
                    {
                        AudioManager.Sfx("run");
                        await Say("¡Has escapado sin hacer declaraciones!");
                        outcome = BattleOutcome.Run;
                        return;
                    }
                    await Say("¡No has podido escapar!");
                    break;

                case PlayerAction.Kind.Switch:
                    await Say($"¡Vuelve, {player.Name}!");
                    await WithdrawPlayer();
                    await SendPlayer(action.Index);
                    break;

                case PlayerAction.Kind.UseItem:
                    var target = GameState.Party[action.Index];
                    string message = ItemEffects.Apply(action.ItemId, target);
                    GameState.RemoveItem(action.ItemId);
                    AudioManager.Sfx("heal");
                    if (target == player)
                        await playerHud.AnimateHp(player.hp);
                    await Say(message);
                    break;

                case PlayerAction.Kind.ThrowCarnet:
                    if (await ThrowCarnet(action.ItemId))
                    {
                        outcome = BattleOutcome.Caught;
                        return;
                    }
                    break;

                case PlayerAction.Kind.Fight:
                    enemyActsAfter = false;
                    int enemyMove = BattleLogic.ChooseAiMove(enemy, player, rng, IsTrainer);
                    var order = new List<(Politician user, Politician target, int move)>
                    {
                        (player, enemy, action.Index),
                        (enemy, player, enemyMove),
                    };
                    if (!BattleLogic.GoesFirst(player, enemy, rng))
                        order.Reverse();
                    foreach (var (user, foe, move) in order)
                    {
                        if (user.IsFainted || foe.IsFainted)
                            continue;
                        await UseMove(user, foe, move);
                        if (await CheckFaints())
                            return;
                    }
                    break;
            }

            if (enemyActsAfter && outcome == BattleOutcome.None && !enemy.IsFainted)
            {
                await UseMove(enemy, player, BattleLogic.ChooseAiMove(enemy, player, rng, IsTrainer));
                if (await CheckFaints())
                    return;
            }
            await EndOfTurn();
        }

        async Awaitable EndOfTurn()
        {
            foreach (var p in new[] { player, enemy })
            {
                if (p.IsFainted)
                    continue;
                int damage = BattleLogic.EscandaloDamage(p);
                if (damage > 0)
                {
                    p.hp = Mathf.Max(0, p.hp - damage);
                    AudioManager.Sfx("hit");
                    await HudOf(p).AnimateHp(p.hp);
                    await Say($"¡El Escándalo pasa factura a {NameOf(p)}!");
                }
                if (p.campaignTurns > 0 && --p.campaignTurns == 0)
                {
                    HudOf(p).Refresh();
                    await Say($"La campaña de {NameOf(p)} ha terminado.");
                }
            }
            await CheckFaints();
        }

        async Awaitable UseMove(Politician user, Politician target, int slot)
        {
            if (BattleLogic.ImputadoBlocksTurn(user, rng))
            {
                await Say($"¡{NameOf(user)} está Imputado y no puede actuar!");
                return;
            }
            string moveId = "balbuceo";
            if (slot >= 0 && slot < user.moves.Count)
            {
                moveId = user.moves[slot].id;
                user.moves[slot].pp = Mathf.Max(0, user.moves[slot].pp - 1);
            }
            else if (user == player)
            {
                await Say($"¡{NameOf(user)} se ha quedado sin argumentos!");
            }

            var move = GameData.Move(moveId);
            await Say($"¡{NameOf(user)} usa {move.name}!");
            if (!BattleLogic.AccuracyHits(moveId, rng))
            {
                await Say("¡Pero ha fallado!");
                return;
            }

            var effect = move.effect ?? new MoveEffect();
            bool damaging = move.power > 0;
            if (damaging)
            {
                var result = BattleLogic.CalculateDamage(user, target, moveId, config.Zone, rng);
                await AttackAnimation(user, target);
                target.hp = Mathf.Max(0, target.hp - result.damage);
                await HudOf(target).AnimateHp(target.hp);
                if (result.critical)
                    await Say("¡Un golpe crítico!");
                if (result.effectiveness > 1f)
                    await Say("¡Es muy eficaz!");
                else if (result.effectiveness < 1f)
                    await Say("No es muy eficaz...");

                if (effect.kind == "drain")
                {
                    user.hp = Mathf.Min(user.MaxHp, user.hp + Mathf.Max(1, (int)(result.damage * effect.fraction)));
                    await HudOf(user).AnimateHp(user.hp);
                    await Say($"¡{NameOf(user)} recupera Carisma!");
                }
                else if (effect.kind == "recoil")
                {
                    user.hp = Mathf.Max(0, user.hp - Mathf.Max(1, (int)(result.damage * effect.fraction)));
                    await HudOf(user).AnimateHp(user.hp);
                    await Say($"¡{NameOf(user)} se resiente del esfuerzo!");
                }
            }
            else
            {
                AudioManager.Sfx("status");
                bool onSelf = effect.target == "self" || effect.kind == "heal" || effect.kind == "campaign";
                await StatusAnimation(onSelf ? user : target);
            }
            await ApplyEffect(user, target, effect, damaging);
        }

        async Awaitable ApplyEffect(Politician user, Politician target, MoveEffect effect, bool damaging)
        {
            if (string.IsNullOrEmpty(effect.kind))
                return;
            bool rollOk = effect.chance >= 100 || rng.Next(1, 101) <= effect.chance;
            switch (effect.kind)
            {
                case "status":
                    if (target.IsFainted)
                        return;
                    if (BattleLogic.CanInflict(target, effect.status) && rollOk)
                    {
                        target.status = effect.status;
                        HudOf(target).Refresh();
                        AudioManager.Sfx("status");
                        await Say(effect.status == "escandalo"
                            ? $"¡{NameOf(target)} se ve envuelto en un Escándalo!"
                            : $"¡{NameOf(target)} ha sido Imputado!");
                    }
                    else if (!damaging)
                    {
                        await Say("No ha tenido ningún efecto.");
                    }
                    break;

                case "stat":
                    if (!rollOk)
                        return;
                    var who = effect.target == "self" ? user : target;
                    if (who.IsFainted)
                        return;
                    var stat = BattleLogic.ParseStat(effect.stat);
                    int changed = BattleLogic.ApplyStage(who, stat, effect.stages);
                    string label = StatNames[stat];
                    if (changed == 0)
                        await Say($"¡La {label} de {NameOf(who)} no puede {(effect.stages > 0 ? "subir" : "bajar")} más!");
                    else if (changed > 0)
                        await Say($"¡La {label} de {NameOf(who)} sube{(changed > 1 ? " mucho" : "")}!");
                    else
                        await Say($"¡La {label} de {NameOf(who)} baja{(changed < -1 ? " mucho" : "")}!");
                    break;

                case "heal":
                    if (user.hp >= user.MaxHp)
                    {
                        await Say($"¡{NameOf(user)} ya tiene el Carisma al máximo!");
                        return;
                    }
                    user.hp = Mathf.Min(user.MaxHp, user.hp + (int)(user.MaxHp * effect.fraction));
                    AudioManager.Sfx("heal");
                    await HudOf(user).AnimateHp(user.hp);
                    await Say($"¡{NameOf(user)} recupera Carisma!");
                    break;

                case "campaign":
                    if (user.campaignTurns > 0)
                    {
                        await Say($"¡{NameOf(user)} ya está En Campaña!");
                        return;
                    }
                    user.campaignTurns = effect.turns;
                    HudOf(user).Refresh();
                    await Say($"¡{NameOf(user)} entra En Campaña! Su Agilidad se dispara.");
                    break;
            }
        }

        /// <summary>Handles fainted politicians. Returns true when the battle is over.</summary>
        async Awaitable<bool> CheckFaints()
        {
            if (enemy.IsFainted)
            {
                await Faint(enemy);
                await AwardExperience(enemy);
                if (IsTrainer && enemyIndex + 1 < config.Enemies.Count)
                {
                    await SendEnemy(enemyIndex + 1);
                }
                else
                {
                    outcome = BattleOutcome.Win;
                    return true;
                }
            }
            if (player.IsFainted)
            {
                await Faint(player);
                if (GameState.AbleCount == 0)
                {
                    outcome = BattleOutcome.Lose;
                    return true;
                }
                await SendPlayer(await ChooseSwitch(true));
            }
            return false;
        }

        async Awaitable Faint(Politician p)
        {
            var sprite = SpriteOf(p);
            var start = sprite.rectTransform.anchoredPosition;
            AudioManager.Sfx("faint");
            await Async.Tween(0.35f, t =>
            {
                sprite.rectTransform.anchoredPosition = start - new Vector2(0, 40 * t);
                sprite.color = new Color(1, 1, 1, 1 - t);
            });
            sprite.enabled = false;
            sprite.color = Color.white;
            PlaceFeet(sprite, p == enemy ? enemyHome : playerHome);
            p.status = "";
            p.campaignTurns = 0;
            HudOf(p).gameObject.SetActive(false);
            await Say($"¡{NameOf(p)} presenta su Dimisión!");
        }

        async Awaitable AwardExperience(Politician defeated)
        {
            int baseExp = BattleLogic.ExpYield(defeated, IsTrainer);
            int sharedAmount = 0;
            foreach (var p in GameState.Party)
            {
                if (p.IsFainted)
                    continue;
                int amount = participants.Contains(p) ? baseExp : (int)(baseExp * BattleLogic.SharedExpFraction);
                if (amount <= 0)
                    continue;
                float before = p.ExpProgress;
                var levels = p.GainExperience(amount);
                if (p == player)
                {
                    await Say($"¡{p.Name} gana {amount} puntos de experiencia!");
                    await playerHud.AnimateExp(before, levels.Count > 0 ? 1f : p.ExpProgress);
                }
                else
                {
                    sharedAmount = amount;
                }
                foreach (int level in levels)
                {
                    if (p == player)
                        playerHud.SetShownHp(p.hp);
                    AudioManager.Sfx("levelup");
                    await Say($"¡{p.Name} sube al nivel {level}!");
                    foreach (var moveId in p.MovesLearnedAt(level))
                        await LearnMove(p, moveId);
                }
                if (p.EvolutionTarget > 0 && !evolutions.Contains(p))
                    evolutions.Add(p);
            }
            if (sharedAmount > 0)
                await Say($"El resto del equipo también aprende algo: +{sharedAmount} EXP.");
        }

        async Awaitable LearnMove(Politician p, string moveId)
        {
            if (p.Knows(moveId))
                return;
            string moveName = GameData.Move(moveId)?.name ?? "?";
            if (p.TryAddMove(moveId))
            {
                await Say($"¡{p.Name} aprende {moveName}!");
                return;
            }
            await Say($"{p.Name} quiere aprender {moveName}, pero ya domina cuatro argumentos.");
            if (await UIManager.YesNo($"¿Olvidar un argumento para aprender {moveName}?"))
            {
                var options = p.moves.Select(m => (MenuOption)(GameData.Move(m.id)?.name ?? "?")).ToList();
                int slot = await UIManager.Choose(options, new MenuSettings { Title = "¿CUÁL OLVIDA?" });
                if (slot >= 0)
                {
                    string old = options[slot].Label;
                    p.ReplaceMove(slot, moveId);
                    await Say($"1, 2 y... ¡ya está! {p.Name} olvida {old} y aprende {moveName}.");
                    return;
                }
            }
            await Say($"{p.Name} no aprende {moveName}.");
        }

        async Awaitable WithdrawPlayer()
        {
            await Async.Tween(0.2f, t => playerSprite.rectTransform.localScale = Vector3.one * (1 - t));
            playerHud.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- capture

        async Awaitable<bool> ThrowCarnet(string itemId)
        {
            GameState.RemoveItem(itemId);
            var item = GameData.Item(itemId);
            await Say($"¡Ofreces un {item.name} a {enemy.Name}!");
            UIFactory.SetPicture(carnetSprite, "Sprites/Items/" + item.icon);
            var start = new Vector2(playerHome.x + 40, -(playerHome.y + 10));
            var target = new Vector2(enemyHome.x + 28, -(enemyHome.y + 32));
            AudioManager.Sfx("throw");
            await Async.Tween(0.5f, t =>
                carnetSprite.rectTransform.anchoredPosition = Vector2.Lerp(start, target, t) + new Vector2(0, 50 * Mathf.Sin(Mathf.PI * t)));
            await UIManager.Flash(Color.white, 1);
            await Async.Tween(0.2f, t => enemySprite.rectTransform.localScale = Vector3.one * (1 - t));
            var dropFrom = carnetSprite.rectTransform.anchoredPosition;
            await Async.Tween(0.2f, t => carnetSprite.rectTransform.anchoredPosition = dropFrom - new Vector2(0, 16 * t));

            var capture = BattleLogic.AttemptCapture(enemy, item.catchMultiplier, rng);
            for (int i = 0; i < capture.shakes; i++)
            {
                await Async.Seconds(0.35f);
                AudioManager.Sfx("shake");
                await Async.Tween(0.28f, t => carnetSprite.rectTransform.localEulerAngles = new Vector3(0, 0, 20f * Mathf.Sin(t * Mathf.PI * 2)));
            }
            await Async.Seconds(0.3f);

            if (capture.caught)
            {
                AudioManager.StopMusic();
                await AudioManager.SfxAndWait("capture");
                await Say($"¡Enhorabuena! ¡{enemy.Name} se ha afiliado a tu partido!");
                bool newEntry = !GameState.IsCaught(enemy.speciesId);
                enemy.ResetBattleState();
                bool inParty = GameState.AddPolitician(enemy);
                if (newEntry)
                    await Say($"Los datos de {enemy.Name} se han añadido a la Politidex.");
                if (!inParty)
                    await Say($"Tu equipo está completo: {enemy.Name} espera en la Sede del Partido.");
                carnetSprite.enabled = false;
                return true;
            }

            carnetSprite.enabled = false;
            await Async.Tween(0.2f, t => enemySprite.rectTransform.localScale = Vector3.one * t);
            await Say(FailLines[rng.Next(FailLines.Length)]);
            return false;
        }

        // ---------------------------------------------------------------- animations

        async Awaitable AttackAnimation(Politician user, Politician target)
        {
            var attacker = SpriteOf(user).rectTransform;
            var defender = SpriteOf(target);
            var home = attacker.anchoredPosition;
            var lunge = user == player ? new Vector2(8, 4) : new Vector2(-8, -4);
            await Async.Tween(0.08f, t => attacker.anchoredPosition = home + lunge * t);
            await Async.Tween(0.1f, t => attacker.anchoredPosition = home + lunge * (1 - t));
            AudioManager.Sfx("hit");
            for (int i = 0; i < 3; i++)
            {
                defender.enabled = false;
                await Async.Seconds(0.06f);
                defender.enabled = true;
                await Async.Seconds(0.06f);
            }
        }

        async Awaitable StatusAnimation(Politician p)
        {
            var rt = SpriteOf(p).rectTransform;
            var home = rt.anchoredPosition;
            for (int i = 0; i < 2; i++)
                await Async.Tween(0.14f, t => rt.anchoredPosition = home + new Vector2(0, 4 * Mathf.Sin(t * Mathf.PI)));
        }

        // ---------------------------------------------------------------- end

        async Awaitable End()
        {
            if (outcome == BattleOutcome.Win)
            {
                AudioManager.PlayMusic("victory");
                if (IsTrainer)
                {
                    trainerSprite.enabled = true;
                    PlaceFeet(trainerSprite, enemyHome + new Vector2(ScreenScaler.LogicalSize.x, 0));
                    await Async.MoveTo(trainerSprite.rectTransform, trainerSprite.rectTransform.anchoredPosition - new Vector2(ScreenScaler.LogicalSize.x, 0), 0.4f);
                    await Say($"¡Has ganado el debate contra {trainer.name}!");
                    await Say($"{trainer.name}: «{trainer.defeatLine}»");
                    GameState.Money += trainer.reward;
                    await Say($"¡Recibes {trainer.reward}€ en donaciones!");
                }
            }
            else if (outcome == BattleOutcome.Lose)
            {
                AudioManager.StopMusic();
                await Say("¡No te quedan afiliados en pie!");
                int lost = GameState.Money / 2;
                GameState.Money -= lost;
                await Say($"Pierdes {lost}€ en la campaña y vuelves corriendo a la Sede...");
            }
            foreach (var p in GameState.Party)
                p.ResetBattleState();
            await UIManager.FadeOut(0.3f);
        }
    }
}
