using System.Collections.Generic;
using System.Linq;
using Politicmon.Battle;
using UnityEngine;

namespace Politicmon.Core
{
    /// <summary>
    /// The current game in progress: party, bag, badges, flags, position...
    /// All mutable state lives in <see cref="Progress"/> so saving is a single JSON dump.
    /// </summary>
    public static class GameState
    {
        public const int PartyMax = 6;
        public const int StartMoney = 1000;

        public static GameProgress Progress { get; private set; } = new();
        public static System.Random Rng { get; set; } = new();
        public static bool GameActive { get; set; }

        public static List<Politician> Party => Progress.party;
        public static List<Politician> Box => Progress.box;

        public static void NewGame(string playerName)
        {
            Progress = new GameProgress { playerName = playerName };
            AddItem("carnet_normal", 5);
            AddItem("mitin", 3);
            GameActive = true;
        }

        public static void Load(GameProgress progress)
        {
            Progress = progress;
            foreach (var p in Progress.party.Concat(Progress.box))
                p.ResetBattleState();
            GameActive = true;
        }

        public static void Tick(float deltaTime)
        {
            if (GameActive)
                Progress.playTime += deltaTime;
        }

        // ---------------------------------------------------------------- party

        /// <summary>Adds a politician to the party, or to the box if the party is full.</summary>
        public static bool AddPolitician(Politician p)
        {
            MarkCaught(p.speciesId);
            if (Party.Count < PartyMax)
            {
                Party.Add(p);
                return true;
            }
            Box.Add(p);
            return false;
        }

        public static int FirstAbleIndex() => Party.FindIndex(p => !p.IsFainted);
        public static int AbleCount => Party.Count(p => !p.IsFainted);

        public static void HealParty()
        {
            foreach (var p in Party)
                p.HealFully();
        }

        // ---------------------------------------------------------------- items & money

        public static int ItemCount(string id) => Progress.items.FirstOrDefault(i => i.id == id)?.count ?? 0;

        public static void AddItem(string id, int count = 1)
        {
            var stack = Progress.items.FirstOrDefault(i => i.id == id);
            if (stack == null)
                Progress.items.Add(new ItemStack { id = id, count = count });
            else
                stack.count += count;
        }

        public static bool RemoveItem(string id, int count = 1)
        {
            var stack = Progress.items.FirstOrDefault(i => i.id == id);
            if (stack == null || stack.count < count)
                return false;
            stack.count -= count;
            if (stack.count <= 0)
                Progress.items.Remove(stack);
            return true;
        }

        public static int Money
        {
            get => Progress.money;
            set => Progress.money = Mathf.Max(0, value);
        }

        // ---------------------------------------------------------------- badges, flags, trainers

        public static int BadgeCount => Progress.badges.Count;
        public static bool HasBadge(string zone) => Progress.badges.Contains(zone);

        public static void AddBadge(string zone)
        {
            if (!HasBadge(zone))
                Progress.badges.Add(zone);
        }

        public static bool HasFlag(string flag) => Progress.flags.Contains(flag);

        public static void SetFlag(string flag)
        {
            if (!HasFlag(flag))
                Progress.flags.Add(flag);
        }

        public static bool TrainerDefeated(string id) => Progress.defeatedTrainers.Contains(id);

        public static void MarkTrainerDefeated(string id)
        {
            if (!TrainerDefeated(id))
                Progress.defeatedTrainers.Add(id);
        }

        /// <summary>Evaluates map conditions like "badge:madrid", "badges:3" or "flag:legend_done".</summary>
        public static bool CheckCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
                return false;
            var parts = condition.Split(':');
            return parts[0] switch
            {
                "badge" => HasBadge(parts[1]),
                "badges" => BadgeCount >= int.Parse(parts[1]),
                "flag" => HasFlag(parts[1]),
                _ => false,
            };
        }

        // ---------------------------------------------------------------- politidex

        public static void MarkSeen(int id)
        {
            if (!Progress.seen.Contains(id))
                Progress.seen.Add(id);
        }

        public static void MarkCaught(int id)
        {
            MarkSeen(id);
            if (!Progress.caught.Contains(id))
                Progress.caught.Add(id);
        }

        public static bool IsSeen(int id) => Progress.seen.Contains(id);
        public static bool IsCaught(int id) => Progress.caught.Contains(id);

        // ---------------------------------------------------------------- position

        public static void SetPosition(string mapId, Vector2Int cell, string facing)
        {
            Progress.mapId = mapId;
            Progress.x = cell.x;
            Progress.y = cell.y;
            Progress.facing = facing;
        }

        public static Vector2Int Position => new(Progress.x, Progress.y);

        public static void SetRespawn(string mapId, int x, int y)
        {
            Progress.respawnMap = mapId;
            Progress.respawnX = x;
            Progress.respawnY = y;
        }

        public static string PlayTimeText
        {
            get
            {
                int t = (int)Progress.playTime;
                return $"{t / 3600}:{t / 60 % 60:00}";
            }
        }
    }
}
