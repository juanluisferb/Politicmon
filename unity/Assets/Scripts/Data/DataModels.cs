using System;
using System.Collections.Generic;

namespace Politicmon.Data
{
    // Plain serializable classes that mirror the JSON files in Resources/Data.
    // They are loaded with JsonUtility, so every field is public and lists are used instead of dictionaries.

    [Serializable]
    public class TypeInfo
    {
        public string id;
        public string name;
        public string color;
    }

    [Serializable]
    public class TypeMatchup
    {
        public string attacker;
        public string defender;
        public float multiplier;
    }

    [Serializable]
    public class TypeChartData
    {
        public float regionalBonus = 1.3f;
        public string regionalType = "regionalista";
        public List<TypeInfo> types = new();
        public List<TypeMatchup> chart = new();
    }

    [Serializable]
    public class MoveEffect
    {
        public string kind;      // "", status, stat, heal, campaign, drain, recoil
        public string status;    // escandalo | imputado
        public int chance = 100;
        public string target;    // self | enemy
        public string stat;
        public int stages;
        public float fraction;
        public int turns;
    }

    [Serializable]
    public class MoveData
    {
        public string id;
        public string name;
        public string type;
        public string category;  // fisico | especial | estado
        public int power;
        public int accuracy;
        public int pp;
        public string description;
        public MoveEffect effect = new();

        public bool IsStatusMove => category == "estado" || power <= 0;
    }

    [Serializable]
    public class MoveList
    {
        public List<MoveData> moves = new();
    }

    [Serializable]
    public class BaseStats
    {
        public int carisma;
        public int oratoria;
        public int aguante;
        public int dialectica;
        public int templanza;
        public int agilidad;

        public int Get(Stat stat) => stat switch
        {
            Stat.Carisma => carisma,
            Stat.Oratoria => oratoria,
            Stat.Aguante => aguante,
            Stat.Dialectica => dialectica,
            Stat.Templanza => templanza,
            _ => agilidad,
        };
    }

    [Serializable]
    public class LearnsetEntry
    {
        public string move;
        public int level;
    }

    [Serializable]
    public class PoliticianData
    {
        public int id;
        public string key;
        public string name;
        public List<string> types = new();
        public string zone;
        public BaseStats baseStats = new();
        public int catchRate;
        public int baseExp;
        public List<LearnsetEntry> learnset = new();
        public string description;
        public int evolvesTo;
        public int evolveLevel;
    }

    [Serializable]
    public class PoliticianList
    {
        public List<PoliticianData> politicians = new();
    }

    [Serializable]
    public class ItemData
    {
        public string id;
        public string name;
        public string kind;      // carnet | heal | cure | scan
        public float catchMultiplier;
        public int amount;
        public string curesStatus;
        public int price;
        public string usage;     // battle | both
        public string icon;
        public string description;

        public bool UsableInBattle => usage == "battle" || usage == "both";
        public bool UsableInField => usage == "both";
    }

    [Serializable]
    public class ItemList
    {
        public List<ItemData> items = new();
    }

    [Serializable]
    public class PartyData
    {
        public string id;
        public string name;
        public string shortName;
        public string motto;
    }

    [Serializable]
    public class PartyList
    {
        public List<PartyData> parties = new();
    }

    [Serializable]
    public class TeamMember
    {
        public int species;
        public int level;
    }

    [Serializable]
    public class TrainerData
    {
        public string id;
        public string name;
        public string party;
        public string sprite;
        public List<TeamMember> team = new();
        public int reward;
        public string intro;
        public string defeatLine;
        public string afterLine;
        public bool isLeader;
        public string badge;
        public string badgeName;
    }

    [Serializable]
    public class TrainerList
    {
        public List<TrainerData> trainers = new();
    }

    [Serializable]
    public class TileData
    {
        public string name;
        public int index;
        public bool solid;
        public bool encounter;
        public bool door;
        public bool water;
    }

    [Serializable]
    public class TileList
    {
        public int columns = 16;
        public List<TileData> tiles = new();
    }

    [Serializable]
    public class LegendEntry
    {
        public string symbol;
        public string tile;
    }

    [Serializable]
    public class WarpData
    {
        public int x;
        public int y;
        public string toMap;
        public int toX;
        public int toY;
        public string facing;
        public int requiredBadges;
        public string requiredFlag;
        public string deniedText;
    }

    [Serializable]
    public class NpcData
    {
        public string id;
        public int x;
        public int y;
        public string sprite;
        public string facing;
        public string eventType;  // talk, trainer, starter, heal, shop, legend
        public List<string> lines = new();
        public string trainer;
        public int sight;
        public string hideIf;     // "badge:madrid", "flag:legend_done"
        public List<string> stock = new();
    }

    [Serializable]
    public class SignData
    {
        public int x;
        public int y;
        public string text;
    }

    [Serializable]
    public class MapItemData
    {
        public string id;
        public int x;
        public int y;
        public string item;
        public int count;
    }

    [Serializable]
    public class AreaData
    {
        public string name;
        public int x;
        public int y;
        public int width;
        public int height;

        public bool Contains(int px, int py) => px >= x && py >= y && px < x + width && py < y + height;
    }

    [Serializable]
    public class EncounterEntry
    {
        public int species;
        public int minLevel;
        public int maxLevel;
        public int weight;
    }

    [Serializable]
    public class MapData
    {
        public string id;
        public string name;
        public string zone;
        public string music;
        public int width;
        public int height;
        public string border;
        public bool interior;
        public List<LegendEntry> legend = new();
        public List<string> rows = new();
        public List<WarpData> warps = new();
        public List<NpcData> npcs = new();
        public List<SignData> signs = new();
        public List<MapItemData> items = new();
        public List<AreaData> areas = new();
        public float encounterRate;
        public List<EncounterEntry> encounters = new();
    }

    [Serializable]
    public class GlyphData
    {
        public int code;
        public int x;
        public int y;
        public int width;
        public int advance;
    }

    [Serializable]
    public class FontData
    {
        public int lineHeight = 10;
        public int glyphHeight = 8;
        public List<GlyphData> glyphs = new();
    }

    public enum Stat
    {
        Carisma,
        Oratoria,
        Aguante,
        Dialectica,
        Templanza,
        Agilidad,
    }
}
