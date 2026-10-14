using System;
using NUnit.Framework;
using Politicmon.Battle;
using Politicmon.Core;

namespace Politicmon.Tests
{
    public class CaptureTests
    {
        Random rng;

        [SetUp]
        public void SetUp() => rng = new Random(99);

        [Test]
        public void LowerHpIsEasier()
        {
            var p = Politician.Create(1, 10);
            float full = BattleLogic.CatchProbability(p, 1f);
            p.hp = 1;
            Assert.Greater(BattleLogic.CatchProbability(p, 1f), full);
            Assert.AreEqual(200f / 3f / 255f, full, 0.001f);
        }

        [Test]
        public void CarnetsAreRanked()
        {
            var p = Politician.Create(12, 30);
            p.hp = p.MaxHp / 2;
            float normal = BattleLogic.CatchProbability(p, GameData.Item("carnet_normal").catchMultiplier);
            float premium = BattleLogic.CatchProbability(p, GameData.Item("carnet_premium").catchMultiplier);
            float lista = BattleLogic.CatchProbability(p, GameData.Item("puesto_lista").catchMultiplier);
            Assert.Less(normal, premium);
            Assert.Less(premium, lista);
        }

        [Test]
        public void StatusGivesBonus()
        {
            var p = Politician.Create(12, 30);
            float a = BattleLogic.CatchProbability(p, 1f);
            p.status = "escandalo";
            Assert.AreEqual(a * 1.5f, BattleLogic.CatchProbability(p, 1f), 0.0001f);
            p.status = "imputado";
            Assert.AreEqual(a * 1.5f, BattleLogic.CatchProbability(p, 1f), 0.0001f);
        }

        [Test]
        public void AttemptsMatchTheProbability()
        {
            var p = Politician.Create(11, 20);
            p.hp = p.MaxHp / 3;
            float probability = BattleLogic.CatchProbability(p, 1.5f);
            int caught = 0;
            const int n = 5000;
            for (int i = 0; i < n; i++)
            {
                var r = BattleLogic.AttemptCapture(p, 1.5f, rng);
                if (r.caught)
                {
                    caught++;
                    Assert.AreEqual(3, r.shakes);
                }
            }
            Assert.AreEqual(probability, caught / (float)n, 0.025f);
        }

        [Test]
        public void LegendaryIsHardButPossible()
        {
            var p = Politician.Create(25, 36);
            p.hp = 1;
            p.status = "imputado";
            Assert.That(BattleLogic.CatchProbability(p, 2.5f), Is.InRange(0.2f, 0.7f));
            p.hp = p.MaxHp;
            p.status = "";
            Assert.Less(BattleLogic.CatchProbability(p, 1f), 0.05f);
        }

        [Test]
        public void ProbabilityIsClamped()
        {
            var p = Politician.Create(1, 5);
            p.hp = 1;
            p.status = "escandalo";
            Assert.AreEqual(1f, BattleLogic.CatchProbability(p, 2.5f), 0.0001f);
        }
    }
}
