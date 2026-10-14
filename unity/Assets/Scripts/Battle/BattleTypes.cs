using System.Collections.Generic;

namespace Politicmon.Battle
{
    public class BattleConfig
    {
        public List<Politician> Enemies = new();
        public string TrainerId = "";
        public string Zone = "";
        public string Music = "battle";
        public bool CanRun = true;

        public bool IsTrainerBattle => !string.IsNullOrEmpty(TrainerId);
    }

    public enum BattleOutcome
    {
        None,
        Win,
        Lose,
        Run,
        Caught,
    }

    public class BattleResult
    {
        public BattleOutcome Outcome;
        public List<Politician> Evolutions = new();
        public string TrainerId = "";
    }

    /// <summary>What the player decided to do this turn.</summary>
    public struct PlayerAction
    {
        public enum Kind
        {
            Fight,
            Switch,
            UseItem,
            ThrowCarnet,
            Run,
        }

        public Kind Type;
        public int Index;       // move slot, party slot or item target
        public string ItemId;

        public static PlayerAction Fight(int slot) => new() { Type = Kind.Fight, Index = slot };
        public static PlayerAction SwitchTo(int slot) => new() { Type = Kind.Switch, Index = slot };
        public static PlayerAction Use(string item, int target) => new() { Type = Kind.UseItem, ItemId = item, Index = target };
        public static PlayerAction Carnet(string item) => new() { Type = Kind.ThrowCarnet, ItemId = item };
        public static PlayerAction RunAway() => new() { Type = Kind.Run };
    }
}
