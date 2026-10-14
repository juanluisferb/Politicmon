using System;
using System.Collections.Generic;
using System.Linq;
using Politicmon.Core;
using Politicmon.Data;

namespace Politicmon.Battle
{
    [Serializable]
    public class KnownMove
    {
        public string id;
        public int pp;

        public KnownMove()
        {
        }

        public KnownMove(string id, int pp)
        {
            this.id = id;
            this.pp = pp;
        }
    }

    /// <summary>
    /// A politician instance (a member of a party or a wild/trainer opponent).
    /// Serializable so it can be stored as-is in the save file.
    /// </summary>
    [Serializable]
    public class Politician
    {
        public const int MaxLevel = 100;

        public int speciesId = 1;
        public int level = 5;
        public int experience;
        public int hp = 1;
        public string status = "";   // "", escandalo, imputado
        public List<KnownMove> moves = new();

        // Battle-only state (not saved).
        [NonSerialized] public Dictionary<Stat, int> stages = new();
        [NonSerialized] public int campaignTurns;

        public static Politician Create(int speciesId, int level)
        {
            var p = new Politician { speciesId = speciesId, level = Math.Clamp(level, 1, MaxLevel) };
            p.experience = BattleLogic.ExpForLevel(p.level);
            foreach (var moveId in p.LearnsetUpTo(p.level))
                p.moves.Add(new KnownMove(moveId, GameData.Move(moveId)?.pp ?? 10));
            p.hp = p.MaxHp;
            return p;
        }

        public PoliticianData Species => GameData.Species(speciesId);
        public string Name => Species?.name ?? "???";
        public IList<string> Types => Species?.types ?? new List<string>();
        public bool HasType(string type) => Types.Contains(type);
        public bool IsFainted => hp <= 0;
        public int MaxHp => GetStat(Stat.Carisma);

        public int GetStat(Stat stat)
        {
            int b = Species?.baseStats.Get(stat) ?? 50;
            int scaled = (int)(2f * b * level / 100f);
            return stat == Stat.Carisma ? scaled + level + 10 : scaled + 5;
        }

        public int Stage(Stat stat)
        {
            stages ??= new Dictionary<Stat, int>();
            return stages.TryGetValue(stat, out var s) ? s : 0;
        }

        public void ResetBattleState()
        {
            stages = new Dictionary<Stat, int>();
            campaignTurns = 0;
        }

        public void HealFully()
        {
            hp = MaxHp;
            status = "";
            foreach (var m in moves)
                m.pp = GameData.Move(m.id)?.pp ?? m.pp;
        }

        // ---------------------------------------------------------------- moves

        /// <summary>The (up to) four most recent moves learned at or below the given level.</summary>
        public List<string> LearnsetUpTo(int lvl)
        {
            var known = new List<string>();
            foreach (var entry in Species?.learnset ?? new List<LearnsetEntry>())
                if (entry.level <= lvl && !known.Contains(entry.move))
                    known.Add(entry.move);
            return known.Skip(Math.Max(0, known.Count - 4)).ToList();
        }

        public List<string> MovesLearnedAt(int lvl) =>
            (Species?.learnset ?? new List<LearnsetEntry>()).Where(e => e.level == lvl).Select(e => e.move).ToList();

        public bool Knows(string moveId) => moves.Any(m => m.id == moveId);

        public bool TryAddMove(string moveId)
        {
            if (Knows(moveId) || moves.Count >= 4)
                return false;
            moves.Add(new KnownMove(moveId, GameData.Move(moveId)?.pp ?? 10));
            return true;
        }

        public void ReplaceMove(int slot, string moveId) =>
            moves[slot] = new KnownMove(moveId, GameData.Move(moveId)?.pp ?? 10);

        public bool HasUsableMoves => moves.Any(m => m.pp > 0);

        // ---------------------------------------------------------------- experience

        public int ExpToNextLevel => level >= MaxLevel ? 0 : BattleLogic.ExpForLevel(level + 1) - experience;

        public float ExpProgress
        {
            get
            {
                if (level >= MaxLevel)
                    return 1f;
                int lo = BattleLogic.ExpForLevel(level);
                int hi = BattleLogic.ExpForLevel(level + 1);
                return Math.Clamp((experience - lo) / (float)(hi - lo), 0f, 1f);
            }
        }

        /// <summary>Adds experience and returns the list of levels reached.</summary>
        public List<int> GainExperience(int amount)
        {
            var gained = new List<int>();
            experience += amount;
            while (level < MaxLevel && experience >= BattleLogic.ExpForLevel(level + 1))
            {
                int oldMax = MaxHp;
                level++;
                hp += MaxHp - oldMax;
                gained.Add(level);
            }
            return gained;
        }

        // ---------------------------------------------------------------- evolution

        public int EvolutionTarget
        {
            get
            {
                var s = Species;
                return s != null && s.evolvesTo > 0 && level >= s.evolveLevel ? s.evolvesTo : 0;
            }
        }

        public void EvolveTo(int newSpecies)
        {
            int oldMax = MaxHp;
            speciesId = newSpecies;
            hp = Math.Clamp(hp + MaxHp - oldMax, 1, MaxHp);
            var newMoves = LearnsetUpTo(level);
            foreach (var moveId in newMoves)
            {
                if (Knows(moveId))
                    continue;
                if (!TryAddMove(moveId))
                {
                    int slot = moves.FindIndex(m => !newMoves.Contains(m.id));
                    if (slot >= 0)
                        ReplaceMove(slot, moveId);
                }
            }
        }

        public Politician Clone()
        {
            var copy = (Politician)MemberwiseClone();
            copy.moves = moves.Select(m => new KnownMove(m.id, m.pp)).ToList();
            copy.ResetBattleState();
            return copy;
        }
    }
}
