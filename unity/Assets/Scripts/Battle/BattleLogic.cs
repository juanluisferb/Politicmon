using System;
using System.Collections.Generic;
using Politicmon.Core;
using Politicmon.Data;

namespace Politicmon.Battle
{
    public struct DamageResult
    {
        public int damage;
        public float effectiveness;
        public bool critical;
    }

    public struct CaptureResult
    {
        public bool caught;
        public int shakes;
        public float probability;
    }

    /// <summary>
    /// Pure battle formulas (no Unity objects), so they are easy to read and unit test.
    /// </summary>
    public static class BattleLogic
    {
        public const float CritChance = 1f / 16f;
        public const float CritMultiplier = 1.5f;
        public const float Stab = 1.5f;
        public const float ImputadoSkipChance = 0.25f;
        public const float EscandaloFraction = 1f / 8f;
        public const float CampaignSpeedMultiplier = 1.5f;
        public const float StatusCatchBonus = 1.5f;
        public const float TrainerExpMultiplier = 1.5f;
        public const float SharedExpFraction = 0.5f;

        static float Next(Random rng) => (float)rng.NextDouble();

        // ---------------------------------------------------------------- stats

        public static float StageMultiplier(int stage)
        {
            stage = Math.Clamp(stage, -6, 6);
            return stage >= 0 ? (2f + stage) / 2f : 2f / (2f - stage);
        }

        public static float EffectiveStat(Politician p, Stat stat)
        {
            float value = p.GetStat(stat) * StageMultiplier(p.Stage(stat));
            if (stat == Stat.Agilidad && p.campaignTurns > 0)
                value *= CampaignSpeedMultiplier;
            return value;
        }

        /// <summary>Regionalista politicians fighting in their home zone get a bonus.</summary>
        public static bool IsHome(Politician p, string zone) =>
            !string.IsNullOrEmpty(zone) && p.Species?.zone == zone && p.HasType(GameData.RegionalType);

        public static float RegionalFactor(Politician attacker, Politician defender, string zone)
        {
            float f = 1f;
            if (IsHome(attacker, zone))
                f *= GameData.RegionalBonus;
            if (IsHome(defender, zone))
                f /= GameData.RegionalBonus;
            return f;
        }

        // ---------------------------------------------------------------- damage

        /// <param name="forceCrit">Optional override used by tests.</param>
        /// <param name="forceRoll">Optional random roll (0.85..1) used by tests.</param>
        public static DamageResult CalculateDamage(Politician attacker, Politician defender, string moveId, string zone,
            Random rng, bool? forceCrit = null, float? forceRoll = null)
        {
            var result = new DamageResult { effectiveness = 1f };
            var move = GameData.Move(moveId);
            if (move == null || move.IsStatusMove)
                return result;

            bool physical = move.category == "fisico";
            float attack = EffectiveStat(attacker, physical ? Stat.Oratoria : Stat.Dialectica);
            float defense = Math.Max(1f, EffectiveStat(defender, physical ? Stat.Aguante : Stat.Templanza));

            float levelFactor = MathF.Floor(2f * attacker.level / 5f + 2f);
            float baseDamage = MathF.Floor(MathF.Floor(levelFactor * move.power * attack / defense) / 50f) + 2f;

            float effectiveness = GameData.TypeMultiplier(move.type, defender.Types);
            float stab = !string.IsNullOrEmpty(move.type) && attacker.HasType(move.type) ? Stab : 1f;
            bool crit = forceCrit ?? Next(rng) < CritChance;
            float roll = forceRoll ?? 0.85f + Next(rng) * 0.15f;

            float total = baseDamage * stab * effectiveness * (crit ? CritMultiplier : 1f) * roll
                          * RegionalFactor(attacker, defender, zone);

            result.effectiveness = effectiveness;
            result.critical = crit;
            result.damage = effectiveness == 0f ? 0 : Math.Max(1, (int)MathF.Floor(total));
            return result;
        }

        public static bool AccuracyHits(string moveId, Random rng)
        {
            int accuracy = GameData.Move(moveId)?.accuracy ?? 100;
            return accuracy >= 100 || rng.Next(1, 101) <= accuracy;
        }

        // ---------------------------------------------------------------- status

        public static bool ImputadoBlocksTurn(Politician p, Random rng) =>
            p.status == "imputado" && Next(rng) < ImputadoSkipChance;

        public static int EscandaloDamage(Politician p) =>
            p.status == "escandalo" ? Math.Max(1, (int)(p.MaxHp * EscandaloFraction)) : 0;

        public static bool CanInflict(Politician p, string status) =>
            p.status == "" && !p.IsFainted && (status == "escandalo" || status == "imputado");

        /// <summary>Applies a stat stage change and returns how much it really changed.</summary>
        public static int ApplyStage(Politician p, Stat stat, int delta)
        {
            int before = p.Stage(stat);
            int after = Math.Clamp(before + delta, -6, 6);
            p.stages[stat] = after;
            return after - before;
        }

        public static Stat ParseStat(string name) => name switch
        {
            "carisma" => Stat.Carisma,
            "oratoria" => Stat.Oratoria,
            "aguante" => Stat.Aguante,
            "dialectica" => Stat.Dialectica,
            "templanza" => Stat.Templanza,
            _ => Stat.Agilidad,
        };

        // ---------------------------------------------------------------- capture

        public static float CatchProbability(Politician target, float carnetMultiplier)
        {
            float maxHp = target.MaxHp;
            float hp = Math.Max(target.hp, 1);
            float rate = target.Species?.catchRate ?? 100;
            float bonus = target.status == "escandalo" || target.status == "imputado" ? StatusCatchBonus : 1f;
            float a = (3f * maxHp - 2f * hp) * rate * carnetMultiplier / (3f * maxHp) * bonus;
            return Math.Clamp(a / 255f, 0f, 1f);
        }

        /// <summary>Four checks with probability p^(1/4) each, so the overall chance equals p.</summary>
        public static CaptureResult AttemptCapture(Politician target, float carnetMultiplier, Random rng)
        {
            float p = CatchProbability(target, carnetMultiplier);
            float perCheck = MathF.Pow(p, 0.25f);
            int passes = 0;
            for (int i = 0; i < 4; i++)
            {
                if (Next(rng) < perCheck)
                    passes++;
                else
                    break;
            }
            return new CaptureResult { caught = passes == 4, shakes = Math.Min(passes, 3), probability = p };
        }

        // ---------------------------------------------------------------- experience

        public static int ExpForLevel(int n) => n <= 1 ? 0 : (int)(4f * n * n * n / 5f);

        public static int LevelForExp(int exp)
        {
            int n = 1;
            while (n < Politician.MaxLevel && ExpForLevel(n + 1) <= exp)
                n++;
            return n;
        }

        public static int ExpYield(Politician defeated, bool fromTrainer)
        {
            float value = (defeated.Species?.baseExp ?? 50) * defeated.level / 4f;
            if (fromTrainer)
                value *= TrainerExpMultiplier;
            return Math.Max(1, (int)value);
        }

        // ---------------------------------------------------------------- turn order & escape

        public static float RunChance(Politician runner, Politician enemy, int previousAttempts)
        {
            float ps = EffectiveStat(runner, Stat.Agilidad);
            float es = Math.Max(1f, EffectiveStat(enemy, Stat.Agilidad));
            return Math.Clamp(0.5f + 0.5f * (ps - es) / es + 0.15f * previousAttempts, 0.1f, 1f);
        }

        public static bool GoesFirst(Politician a, Politician b, Random rng)
        {
            float sa = EffectiveStat(a, Stat.Agilidad);
            float sb = EffectiveStat(b, Stat.Agilidad);
            return sa == sb ? Next(rng) < 0.5f : sa > sb;
        }

        // ---------------------------------------------------------------- AI

        public static float MoveScore(Politician attacker, Politician defender, string moveId)
        {
            var move = GameData.Move(moveId);
            if (move == null)
                return 0f;
            if (move.power <= 0)
            {
                var e = move.effect;
                switch (e.kind)
                {
                    case "heal": return attacker.hp < attacker.MaxHp / 2 ? 80f : 5f;
                    case "status": return defender.status == "" ? 45f : 0f;
                    case "campaign": return attacker.campaignTurns == 0 ? 35f : 0f;
                    case "stat":
                        var who = e.target == "self" ? attacker : defender;
                        return Math.Abs(who.Stage(ParseStat(e.stat))) < 2 ? 30f : 2f;
                    default: return 10f;
                }
            }
            float score = move.power * GameData.TypeMultiplier(move.type, defender.Types) * move.accuracy / 100f;
            if (!string.IsNullOrEmpty(move.type) && attacker.HasType(move.type))
                score *= Stab;
            return score;
        }

        /// <summary>Returns the move slot to use, or -1 when no move has PP left.</summary>
        public static int ChooseAiMove(Politician attacker, Politician defender, Random rng, bool smart)
        {
            var usable = new List<int>();
            for (int i = 0; i < attacker.moves.Count; i++)
                if (attacker.moves[i].pp > 0)
                    usable.Add(i);
            if (usable.Count == 0)
                return -1;
            if (!smart || Next(rng) < 0.25f)
                return usable[rng.Next(usable.Count)];

            int best = usable[0];
            float bestScore = -1f;
            foreach (int i in usable)
            {
                float score = MoveScore(attacker, defender, attacker.moves[i].id) * (0.85f + Next(rng) * 0.3f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }
    }
}
