using System.Collections.Generic;
using NUnit.Framework;
using Politicmon.Battle;

namespace Politicmon.Tests
{
    public class ExperienceTests
    {
        [Test]
        public void ExperienceCurve()
        {
            Assert.AreEqual(0, BattleLogic.ExpForLevel(1));
            Assert.AreEqual(100, BattleLogic.ExpForLevel(5));
            Assert.AreEqual(800, BattleLogic.ExpForLevel(10));
            for (int n = 2; n < 60; n++)
            {
                Assert.Greater(BattleLogic.ExpForLevel(n + 1), BattleLogic.ExpForLevel(n));
                Assert.AreEqual(n, BattleLogic.LevelForExp(BattleLogic.ExpForLevel(n)));
            }
        }

        [Test]
        public void ExperienceYield()
        {
            var wild = Politician.Create(1, 10);
            Assert.AreEqual(137, BattleLogic.ExpYield(wild, false));
            Assert.AreEqual(206, BattleLogic.ExpYield(wild, true));
        }

        [Test]
        public void LevelUpRaisesStatsAndKeepsDamage()
        {
            var p = Politician.Create(10, 5);
            int hpBefore = p.MaxHp;
            p.hp = p.MaxHp - 3;
            var levels = p.GainExperience(BattleLogic.ExpForLevel(8) - p.experience);
            CollectionAssert.AreEqual(new List<int> { 6, 7, 8 }, levels);
            Assert.AreEqual(8, p.level);
            Assert.Greater(p.MaxHp, hpBefore);
            Assert.AreEqual(p.MaxHp - 3, p.hp);
        }

        [Test]
        public void StarterKnowsTwoMovesAtLevelFive()
        {
            var p = Politician.Create(1, 5);
            Assert.AreEqual(2, p.moves.Count);
            Assert.AreEqual(19, p.MaxHp);
            Assert.AreEqual(9, p.GetStat(Data.Stat.Oratoria));
            Assert.AreEqual(4, Politician.Create(3, 40).moves.Count);
        }

        [Test]
        public void LearnsMovesByLevel()
        {
            var p = Politician.Create(15, 3);
            Assert.AreEqual(1, p.moves.Count);
            CollectionAssert.AreEqual(new List<string> { "sentada" }, p.MovesLearnedAt(4));
        }

        [Test]
        public void EvolvesByLevel()
        {
            var p = Politician.Create(1, 13);
            Assert.AreEqual(0, p.EvolutionTarget);
            p.GainExperience(BattleLogic.ExpForLevel(14) - p.experience);
            Assert.AreEqual(2, p.EvolutionTarget);
            p.EvolveTo(2);
            Assert.AreEqual("Asesor Áulico", p.Name);
            Assert.LessOrEqual(p.moves.Count, 4);
            Assert.IsTrue(p.Knows("titular_viral"));
        }
    }
}
