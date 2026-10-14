using System;
using Politicmon.Core;

namespace Politicmon.Battle
{
    /// <summary>Healing and curing items usable from the bag (in and out of battle).</summary>
    public static class ItemEffects
    {
        public static bool CanUse(string itemId, Politician p)
        {
            var item = GameData.Item(itemId);
            if (item == null || p.IsFainted)
                return false;
            return item.kind switch
            {
                "heal" => p.hp < p.MaxHp,
                "cure" => p.status == item.curesStatus,
                _ => false,
            };
        }

        /// <summary>Applies the item and returns the message to show.</summary>
        public static string Apply(string itemId, Politician p)
        {
            var item = GameData.Item(itemId);
            switch (item?.kind)
            {
                case "heal":
                    int before = p.hp;
                    p.hp = Math.Min(p.MaxHp, p.hp + item.amount);
                    return $"¡{p.Name} recupera {p.hp - before} PS de Carisma!";
                case "cure":
                    p.status = "";
                    return $"¡Bulo desmentido! {p.Name} ya no está en un Escándalo.";
                default:
                    return "No ha tenido efecto.";
            }
        }
    }
}
