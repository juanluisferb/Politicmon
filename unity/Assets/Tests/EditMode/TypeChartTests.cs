using System.Collections.Generic;
using NUnit.Framework;
using Politicmon.Core;

namespace Politicmon.Tests
{
    public class TypeChartTests
    {
        static readonly string[] All =
            { "populista", "tecnocrata", "mediatico", "burocrata", "activista", "baron", "lobista", "leguleyo", "tuitero", "regionalista" };

        static float M(string attacker, params string[] defender) => GameData.TypeMultiplier(attacker, defender);

        [Test]
        public void TenTypesAreDefined()
        {
            Assert.AreEqual(10, GameData.TypeCount);
            foreach (var t in All)
                Assert.AreNotEqual(t, GameData.TypeName(t), "missing type " + t);
        }

        [Test]
        public void Triangles()
        {
            Assert.AreEqual(2f, M("populista", "tecnocrata"));
            Assert.AreEqual(2f, M("tecnocrata", "mediatico"));
            Assert.AreEqual(2f, M("mediatico", "populista"));
            Assert.AreEqual(2f, M("burocrata", "activista"));
            Assert.AreEqual(2f, M("activista", "baron"));
            Assert.AreEqual(2f, M("baron", "burocrata"));
        }

        [Test]
        public void InverseIsHalf()
        {
            Assert.AreEqual(0.5f, M("tecnocrata", "populista"));
            Assert.AreEqual(0.5f, M("mediatico", "tecnocrata"));
            Assert.AreEqual(0.5f, M("populista", "mediatico"));
            Assert.AreEqual(0.5f, M("activista", "burocrata"));
            Assert.AreEqual(0.5f, M("baron", "activista"));
            Assert.AreEqual(0.5f, M("burocrata", "baron"));
        }

        [Test]
        public void LobistaLeguleyoTuitero()
        {
            Assert.AreEqual(2f, M("lobista", "baron"));
            Assert.AreEqual(2f, M("lobista", "burocrata"));
            Assert.AreEqual(2f, M("leguleyo", "lobista"));
            Assert.AreEqual(2f, M("activista", "lobista"));
            Assert.AreEqual(0.5f, M("lobista", "leguleyo"));
            Assert.AreEqual(0.5f, M("lobista", "activista"));
            Assert.AreEqual(2f, M("leguleyo", "populista"));
            Assert.AreEqual(2f, M("burocrata", "leguleyo"));
            Assert.AreEqual(0.5f, M("leguleyo", "burocrata"));
            Assert.AreEqual(2f, M("tuitero", "mediatico"));
            Assert.AreEqual(2f, M("tuitero", "tecnocrata"));
            Assert.AreEqual(2f, M("leguleyo", "tuitero"));
            Assert.AreEqual(0.5f, M("tuitero", "leguleyo"));
        }

        [Test]
        public void EveryAdvantageHasItsInverse()
        {
            foreach (var row in GameData.TypeChartRows)
            foreach (var cell in row.Value)
                if (cell.Value == 2f)
                    Assert.AreEqual(0.5f, M(cell.Key, row.Key), $"inverse of {row.Key}>{cell.Key}");
        }

        [Test]
        public void RegionalistaIsNeutral()
        {
            foreach (var t in All)
            {
                Assert.AreEqual(1f, M("regionalista", t));
                Assert.AreEqual(1f, M(t, "regionalista"));
            }
        }

        [Test]
        public void DualTypesMultiply()
        {
            Assert.AreEqual(2f, M("leguleyo", "populista", "baron"));
            Assert.AreEqual(4f, M("tuitero", "mediatico", "tecnocrata"));
            Assert.AreEqual(0.25f, M("populista", "mediatico", "leguleyo"));
            Assert.AreEqual(1f, M("populista", "activista"));
            Assert.AreEqual(1f, GameData.TypeMultiplier("", new List<string> { "activista" }));
        }
    }
}
