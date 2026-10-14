using System;
using NUnit.Framework;
using Politicmon.Battle;
using Politicmon.Data;

namespace Politicmon.Tests
{
    public class DamageTests
    {
        Random rng;

        [SetUp]
        public void SetUp() => rng = new Random(1234);

        int Damage(Politician a, Politician d, string move, string zone = "madrid", bool crit = false) =>
            BattleLogic.CalculateDamage(a, d, move, zone, rng, crit, 1f).damage;

        [Test]
        public void KnownValue()
        {
            // L10: floor(2*10/5+2)=6; oratoria 13 vs aguante 14 -> floor(floor(6*45*13/14)/50)+2 = 7; STAB -> 10
            Assert.AreEqual(10, Damage(Politician.Create(1, 10), Politician.Create(10, 10), "rueda_de_prensa"));
        }

        [Test]
        public void EffectivenessIsReported()
        {
            var pop = Politician.Create(13, 20);
            Assert.AreEqual(2f, BattleLogic.CalculateDamage(pop, Politician.Create(6, 20), "promesa_imposible", "", rng, false, 1f).effectiveness);
            Assert.AreEqual(1f, BattleLogic.CalculateDamage(pop, Politician.Create(15, 20), "promesa_imposible", "", rng, false, 1f).effectiveness);
        }

        [Test]
        public void CriticalHitsDoMoreDamage()
        {
            var a = Politician.Create(13, 30);
            var d = Politician.Create(15, 30);
            int normal = Damage(a, d, "promesa_imposible", "");
            int crit = Damage(a, d, "promesa_imposible", "", true);
            Assert.Greater(crit, normal);
            Assert.AreEqual(1.5, crit / (double)normal, 0.1);
        }

        [Test]
        public void StatusMovesDoNoDamage() =>
            Assert.AreEqual(0, Damage(Politician.Create(9, 20), Politician.Create(1, 20), "vuelva_usted_manana"));

        [Test]
        public void AlwaysAtLeastOneDamage()
        {
            int d = Damage(Politician.Create(1, 2), Politician.Create(22, 60), "rueda_de_prensa");
            Assert.That(d, Is.InRange(1, 3));
        }

        [Test]
        public void RegionalBonusAtHome()
        {
            var r = Politician.Create(20, 30);
            var d = Politician.Create(15, 30);
            int away = Damage(r, d, "hecho_diferencial", "madrid");
            int home = Damage(r, d, "hecho_diferencial", "galicia");
            Assert.Greater(home, away);
            Assert.AreEqual(1.3, home / (double)away, 0.06);
            Assert.Less(Damage(d, r, "recogida_de_firmas", "galicia"), Damage(d, r, "recogida_de_firmas", "madrid"));
        }

        [Test]
        public void RegionalBonusOnlyForRegionalistas()
        {
            var a = Politician.Create(23, 30);
            var d = Politician.Create(15, 30);
            Assert.AreEqual(Damage(a, d, "pancarta", "madrid"), Damage(a, d, "pancarta", "galicia"));
        }

        [Test]
        public void StageMultipliers()
        {
            Assert.AreEqual(1f, BattleLogic.StageMultiplier(0));
            Assert.AreEqual(2f, BattleLogic.StageMultiplier(2));
            Assert.AreEqual(0.5f, BattleLogic.StageMultiplier(-2));
            Assert.AreEqual(4f, BattleLogic.StageMultiplier(9));
        }

        [Test]
        public void CampaignBoostsSpeed()
        {
            var p = Politician.Create(13, 20);
            float before = BattleLogic.EffectiveStat(p, Stat.Agilidad);
            p.campaignTurns = 3;
            Assert.AreEqual(before * 1.5f, BattleLogic.EffectiveStat(p, Stat.Agilidad), 0.001f);
        }

        [Test]
        public void EscandaloAndImputado()
        {
            var p = Politician.Create(1, 20);
            Assert.AreEqual(0, BattleLogic.EscandaloDamage(p));
            p.status = "escandalo";
            Assert.AreEqual((int)(p.MaxHp / 8f), BattleLogic.EscandaloDamage(p));
            p.status = "imputado";
            int blocked = 0;
            for (int i = 0; i < 4000; i++)
                if (BattleLogic.ImputadoBlocksTurn(p, rng))
                    blocked++;
            Assert.AreEqual(0.25, blocked / 4000.0, 0.03);
            Assert.IsFalse(BattleLogic.CanInflict(p, "escandalo"), "only one status at a time");
        }
    }
}
