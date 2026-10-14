using System.IO;
using NUnit.Framework;
using Politicmon.Battle;
using Politicmon.Core;
using Politicmon.Save;
using UnityEngine;

namespace Politicmon.Tests
{
    public class GameStateTests
    {
        [SetUp]
        public void SetUp()
        {
            SaveSystem.FilePath = Path.Combine(Path.GetTempPath(), $"politicmon_test_{System.Guid.NewGuid():N}.json");
            GameState.NewGame("TEST");
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.Delete();
            SaveSystem.FilePath = null;
        }

        [Test]
        public void MitinHealsUpToMax()
        {
            var p = Politician.Create(9, 20);
            Assert.IsFalse(ItemEffects.CanUse("mitin", p));
            p.hp = 5;
            Assert.IsTrue(ItemEffects.CanUse("mitin", p));
            ItemEffects.Apply("mitin", p);
            Assert.AreEqual(Mathf.Min(p.MaxHp, 55), p.hp);
            p.hp = 0;
            Assert.IsFalse(ItemEffects.CanUse("mitin", p));
        }

        [Test]
        public void FactCheckOnlyCuresEscandalo()
        {
            var p = Politician.Create(9, 20);
            p.status = "imputado";
            Assert.IsFalse(ItemEffects.CanUse("factcheck", p));
            p.status = "escandalo";
            Assert.IsTrue(ItemEffects.CanUse("factcheck", p));
            ItemEffects.Apply("factcheck", p);
            Assert.AreEqual("", p.status);
        }

        [Test]
        public void Inventory()
        {
            Assert.AreEqual(5, GameState.ItemCount("carnet_normal"));
            GameState.AddItem("encuesta", 2);
            Assert.IsTrue(GameState.RemoveItem("encuesta"));
            Assert.AreEqual(1, GameState.ItemCount("encuesta"));
            Assert.IsTrue(GameState.RemoveItem("encuesta"));
            Assert.IsFalse(GameState.RemoveItem("encuesta"));
        }

        [Test]
        public void FullPartySendsToBox()
        {
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(GameState.AddPolitician(Politician.Create(1 + i, 5)));
            Assert.IsFalse(GameState.AddPolitician(Politician.Create(9, 5)));
            Assert.AreEqual(6, GameState.Party.Count);
            Assert.AreEqual(1, GameState.Box.Count);
            Assert.IsTrue(GameState.IsCaught(9));
        }

        [Test]
        public void SaveRoundTrip()
        {
            var a = Politician.Create(19, 31);
            a.hp = 12;
            a.status = "imputado";
            a.moves[1].pp = 3;
            GameState.AddPolitician(a);
            GameState.AddPolitician(Politician.Create(8, 9));
            GameState.Box.Add(Politician.Create(22, 25));
            GameState.AddItem("puesto_lista", 4);
            GameState.Money = 4321;
            GameState.AddBadge("madrid");
            GameState.AddBadge("andalucia");
            GameState.SetFlag("has_starter");
            GameState.MarkTrainerDefeated("t_mad_1");
            GameState.MarkSeen(5);
            GameState.SetPosition("galicia", new Vector2Int(17, 22), "left");
            GameState.SetRespawn("sede_galicia", 4, 4);
            GameState.Progress.playTime = 1234.5f;
            Assert.IsTrue(SaveSystem.Save());

            GameState.NewGame("OTRO");
            Assert.IsTrue(SaveSystem.Load());
            var p = GameState.Progress;
            Assert.AreEqual("TEST", p.playerName);
            Assert.AreEqual(2, GameState.Party.Count);
            Assert.AreEqual(19, GameState.Party[0].speciesId);
            Assert.AreEqual(31, GameState.Party[0].level);
            Assert.AreEqual(12, GameState.Party[0].hp);
            Assert.AreEqual("imputado", GameState.Party[0].status);
            Assert.AreEqual(3, GameState.Party[0].moves[1].pp);
            Assert.AreEqual(1, GameState.Box.Count);
            Assert.AreEqual(4, GameState.ItemCount("puesto_lista"));
            Assert.AreEqual(5, GameState.ItemCount("carnet_normal"));
            Assert.AreEqual(4321, GameState.Money);
            CollectionAssert.AreEqual(new[] { "madrid", "andalucia" }, p.badges);
            Assert.IsTrue(GameState.HasFlag("has_starter"));
            Assert.IsTrue(GameState.TrainerDefeated("t_mad_1"));
            Assert.IsTrue(GameState.IsCaught(19) && GameState.IsSeen(5) && !GameState.IsCaught(5));
            Assert.AreEqual("galicia", p.mapId);
            Assert.AreEqual(new Vector2Int(17, 22), GameState.Position);
            Assert.AreEqual("left", p.facing);
            Assert.AreEqual(4, p.respawnX);
            Assert.AreEqual(1234.5f, p.playTime, 0.01f);
        }

        [Test]
        public void MissingOrCorruptSave()
        {
            Assert.IsFalse(SaveSystem.HasSave);
            Assert.IsFalse(SaveSystem.Load());
            File.WriteAllText(SaveSystem.FilePath, "{esto no es json");
            Assert.IsFalse(SaveSystem.Load());
        }
    }
}
