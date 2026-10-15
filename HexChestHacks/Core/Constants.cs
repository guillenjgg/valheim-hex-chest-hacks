using System.Collections.Generic;

namespace HexChestHacks.Core
{
    internal static class Constants
    {
        internal const string TreasureChestPrefab = "piece_chest_treasure";

        internal static readonly HashSet<string> SupportedPrefabs = new HashSet<string>
        {
            "piece_chest_wood",
            "piece_chest",
            "piece_chest_blackmetal",
            "piece_chest_grausten",
            "piece_chest_private",
            "piece_chest_barrel",
            "piece_chest_warderobe"
        };

        internal static bool IsSupportedChest(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            var name = prefabName.Replace("(Clone)", "").Trim();

            return SupportedPrefabs.Contains(name);
        }
    }
}
