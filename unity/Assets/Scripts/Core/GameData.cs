using System.Collections.Generic;
using Politicmon.Data;
using UnityEngine;

namespace Politicmon.Core
{
    /// <summary>
    /// Read-only game database loaded from the JSON files in Resources/Data.
    /// Edit those files (or regenerate them with tools/export_unity.py) to add content.
    /// </summary>
    public static class GameData
    {
        public static float RegionalBonus { get; private set; } = 1.3f;
        public static string RegionalType { get; private set; } = "regionalista";

        static readonly Dictionary<string, TypeInfo> types = new();
        static readonly Dictionary<string, Dictionary<string, float>> typeChart = new();
        static readonly Dictionary<string, MoveData> moves = new();
        static readonly Dictionary<int, PoliticianData> politicians = new();
        static readonly Dictionary<string, ItemData> items = new();
        static readonly List<string> itemOrder = new();
        static readonly Dictionary<string, PartyData> parties = new();
        static readonly Dictionary<string, TrainerData> trainers = new();
        static readonly Dictionary<string, TileData> tiles = new();
        static readonly Dictionary<string, MapData> maps = new();

        public static int TileColumns { get; private set; } = 16;
        public static int TileCount { get; private set; }
        public static bool IsLoaded { get; private set; }

        public static void Load()
        {
            if (IsLoaded)
                return;
            LoadFrom(name => Resources.Load<TextAsset>("Data/" + name)?.text);
        }

        /// <summary>Loads everything through a text provider (used by tests to read the files directly).</summary>
        public static void LoadFrom(System.Func<string, string> readText)
        {
            types.Clear(); typeChart.Clear(); moves.Clear(); politicians.Clear(); items.Clear();
            itemOrder.Clear(); parties.Clear(); trainers.Clear(); tiles.Clear(); maps.Clear();

            var chart = JsonUtility.FromJson<TypeChartData>(readText("types"));
            RegionalBonus = chart.regionalBonus;
            RegionalType = chart.regionalType;
            foreach (var t in chart.types)
                types[t.id] = t;
            foreach (var m in chart.chart)
            {
                if (!typeChart.TryGetValue(m.attacker, out var row))
                    typeChart[m.attacker] = row = new Dictionary<string, float>();
                row[m.defender] = m.multiplier;
            }

            foreach (var m in JsonUtility.FromJson<MoveList>(readText("moves")).moves)
                moves[m.id] = m;
            foreach (var p in JsonUtility.FromJson<PoliticianList>(readText("politicians")).politicians)
                politicians[p.id] = p;
            foreach (var i in JsonUtility.FromJson<ItemList>(readText("items")).items)
            {
                items[i.id] = i;
                itemOrder.Add(i.id);
            }
            foreach (var p in JsonUtility.FromJson<PartyList>(readText("parties")).parties)
                parties[p.id] = p;
            foreach (var t in JsonUtility.FromJson<TrainerList>(readText("trainers")).trainers)
                trainers[t.id] = t;

            var tileList = JsonUtility.FromJson<TileList>(readText("tiles"));
            TileColumns = tileList.columns;
            foreach (var t in tileList.tiles)
            {
                tiles[t.name] = t;
                TileCount = Mathf.Max(TileCount, t.index + 1);
            }

            mapReader = name => readText("Maps/" + name);
            IsLoaded = true;
        }

        static System.Func<string, string> mapReader;

        // ---------------------------------------------------------------- types

        public static float TypeMultiplier(string attackType, IList<string> defenderTypes)
        {
            float result = 1f;
            if (string.IsNullOrEmpty(attackType) || !typeChart.TryGetValue(attackType, out var row))
                return result;
            foreach (var defender in defenderTypes)
                if (row.TryGetValue(defender, out var m))
                    result *= m;
            return result;
        }

        public static IEnumerable<string> TypeIds => types.Keys;
        public static int TypeCount => types.Count;
        public static IEnumerable<KeyValuePair<string, Dictionary<string, float>>> TypeChartRows => typeChart;

        public static string TypeName(string id) => types.TryGetValue(id, out var t) ? t.name : id;

        public static Color TypeColor(string id)
        {
            if (types.TryGetValue(id, out var t) && ColorUtility.TryParseHtmlString(t.color, out var c))
                return c;
            return Color.gray;
        }

        // ---------------------------------------------------------------- lookups

        public static MoveData Move(string id) => moves.TryGetValue(id, out var m) ? m : null;
        public static int MoveCount => moves.Count;
        public static IEnumerable<MoveData> AllMoves => moves.Values;

        public static PoliticianData Species(int id) => politicians.TryGetValue(id, out var p) ? p : null;
        public static int SpeciesCount => politicians.Count;
        public static IEnumerable<PoliticianData> AllSpecies => politicians.Values;

        public static ItemData Item(string id) => items.TryGetValue(id, out var i) ? i : null;
        public static IReadOnlyList<string> ItemIds => itemOrder;

        public static PartyData Party(string id) => parties.TryGetValue(id, out var p) ? p : null;
        public static TrainerData Trainer(string id) => trainers.TryGetValue(id, out var t) ? t : null;
        public static IEnumerable<TrainerData> AllTrainers => trainers.Values;
        public static TileData Tile(string name) => name != null && tiles.TryGetValue(name, out var t) ? t : null;

        public static MapData Map(string id)
        {
            if (maps.TryGetValue(id, out var cached))
                return cached;
            var text = mapReader?.Invoke(id);
            if (string.IsNullOrEmpty(text))
                return null;
            var map = JsonUtility.FromJson<MapData>(text);
            maps[id] = map;
            return map;
        }

        // ---------------------------------------------------------------- resource paths

        public static string PoliticianSpritePath(int id, string kind)
        {
            var s = Species(id);
            return $"Sprites/Politicians/{id:00}_{s?.key ?? "x"}_{kind}";
        }

        public static string ZoneName(string zone) => zone switch
        {
            "madrid" => "Madrid",
            "andalucia" => "Andalucía",
            "galicia" => "Galicia",
            _ => "???",
        };
    }
}
