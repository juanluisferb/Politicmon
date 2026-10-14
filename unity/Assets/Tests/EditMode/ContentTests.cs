using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Politicmon.Core;
using Politicmon.Data;
using UnityEngine;

namespace Politicmon.Tests
{
    /// <summary>Validates the game data: politicians, moves, trainers and every map.</summary>
    public class ContentTests
    {
        static readonly string[] Maps =
        {
            "madrid", "andalucia", "galicia", "madrid_garaje", "hemiciclo", "sede_madrid", "sede_andalucia",
            "sede_galicia", "gym_madrid", "gym_andalucia", "gym_galicia",
        };

        [Test]
        public void TwentyFivePoliticiansWithValidData()
        {
            Assert.AreEqual(25, GameData.SpeciesCount);
            foreach (var s in GameData.AllSpecies)
            {
                Assert.That(s.types.Count, Is.InRange(1, 2), s.name);
                Assert.AreEqual(4, s.learnset.Count, s.name);
                foreach (var entry in s.learnset)
                    Assert.IsNotNull(GameData.Move(entry.move), entry.move);
                Assert.Greater(s.description.Length, 20, s.name);
                if (s.evolvesTo > 0)
                    Assert.IsNotNull(GameData.Species(s.evolvesTo));
            }
        }

        [Test]
        public void AtLeastThirtyMoves() => Assert.GreaterOrEqual(GameData.MoveCount, 30);

        [Test]
        public void TrainersHaveOneToThreeMembers()
        {
            foreach (var t in GameData.AllTrainers)
            {
                Assert.That(t.team.Count, Is.InRange(1, 3), t.id);
                Assert.IsTrue(t.team.All(m => GameData.Species(m.species) != null), t.id);
            }
        }

        static bool Passable(MapData map, Vector2Int c, HashSet<Vector2Int> warps)
        {
            if (c.x < 0 || c.y < 0 || c.x >= map.width || c.y >= map.height)
                return false;
            string tileName = map.legend.First(l => l.symbol[0] == map.rows[c.y][c.x]).tile;
            var tile = GameData.Tile(tileName);
            if (tile.door)
                return warps.Contains(c);
            return !tile.solid && !tile.water;
        }

        [Test]
        public void MapsAreWellFormed()
        {
            foreach (var id in Maps)
            {
                var map = GameData.Map(id);
                Assert.IsNotNull(map, id);
                Assert.AreEqual(map.height, map.rows.Count, id);
                foreach (var row in map.rows)
                {
                    Assert.AreEqual(map.width, row.Length, id);
                    foreach (char ch in row)
                        Assert.IsNotNull(GameData.Tile(map.legend.First(l => l.symbol[0] == ch).tile), $"{id} '{ch}'");
                }
            }
        }

        [Test]
        public void WarpsArriveOnWalkableCells()
        {
            foreach (var id in Maps)
            foreach (var w in GameData.Map(id).warps)
            {
                var dest = GameData.Map(w.toMap);
                Assert.IsNotNull(dest, $"{id} -> {w.toMap}");
                var cell = new Vector2Int(w.toX, w.toY);
                Assert.IsTrue(Passable(dest, cell, new HashSet<Vector2Int>()), $"{id} -> {w.toMap} {cell}");
                Assert.IsFalse(dest.warps.Any(o => o.x == cell.x && o.y == cell.y), "arrival is not another warp");
            }
        }

        [Test]
        public void ThreeRivalsAndOneLeaderPerZone()
        {
            var rivals = new Dictionary<string, int>();
            var leaders = new Dictionary<string, int>();
            foreach (var id in Maps)
            {
                var map = GameData.Map(id);
                foreach (var npc in map.npcs.Where(n => n.eventType == "trainer"))
                {
                    var counter = GameData.Trainer(npc.trainer).isLeader ? leaders : rivals;
                    counter[map.zone] = counter.GetValueOrDefault(map.zone) + 1;
                }
            }
            foreach (var zone in new[] { "madrid", "andalucia", "galicia" })
            {
                Assert.AreEqual(3, rivals[zone], zone);
                Assert.AreEqual(1, leaders[zone], zone);
            }
        }

        [Test]
        public void WildPoliticiansBelongToTheirZone()
        {
            foreach (var id in new[] { "madrid", "andalucia", "galicia" })
            {
                var map = GameData.Map(id);
                Assert.GreaterOrEqual(map.encounters.Count, 5);
                foreach (var e in map.encounters)
                    Assert.AreEqual(id, GameData.Species(e.species).zone);
            }
        }

        [Test]
        public void EveryWarpIsReachable()
        {
            foreach (var id in Maps)
            {
                var map = GameData.Map(id);
                var warps = new HashSet<Vector2Int>(map.warps.Select(w => new Vector2Int(w.x, w.y)));
                var start = new Vector2Int(map.warps[0].x, map.warps[0].y);
                var seen = new HashSet<Vector2Int> { start };
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue();
                    if (warps.Contains(c) && c != start)
                        continue;
                    foreach (var d in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                    {
                        var n = c + d;
                        if (!seen.Contains(n) && Passable(map, n, warps))
                        {
                            seen.Add(n);
                            queue.Enqueue(n);
                        }
                    }
                }
                foreach (var w in warps)
                    Assert.IsTrue(seen.Contains(w), $"{id} warp {w} reachable");
            }
        }
    }
}
