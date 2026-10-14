using System;
using System.Collections.Generic;
using Politicmon.Battle;

namespace Politicmon.Core
{
    [Serializable]
    public class ItemStack
    {
        public string id;
        public int count;
    }

    /// <summary>
    /// Everything that is written to the save file. JsonUtility-friendly (lists only).
    /// </summary>
    [Serializable]
    public class GameProgress
    {
        public int version = 1;
        public string playerName = "ALEX";
        public List<Politician> party = new();
        public List<Politician> box = new();
        public List<ItemStack> items = new();
        public int money = GameState.StartMoney;
        public List<string> badges = new();
        public List<string> flags = new();
        public List<string> defeatedTrainers = new();
        public List<int> seen = new();
        public List<int> caught = new();
        public string mapId = "madrid_garaje";
        public int x = 4;
        public int y = 5;
        public string facing = "up";
        public string respawnMap = "madrid_garaje";
        public int respawnX = 4;
        public int respawnY = 5;
        public float playTime;
        public string savedAt;
    }
}
