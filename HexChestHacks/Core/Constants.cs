using System.Collections.Generic;

namespace HexChestHacks.Core
{
    internal static class Constants
    {
        internal const string TreasureChestPrefab = "piece_chest_treasure";
        internal const string CartPrefab = "Cart";

        internal static string GetPrefabName(Container container)
        {
            if (container == null) return string.Empty;
            var root = container.m_rootObjectOverride != null
                ? container.m_rootObjectOverride.gameObject : container.gameObject;
            return Utils.GetPrefabName(root);
        }

        internal static bool IsSupportedContainer(Container container) =>
            IsSupportedChest(GetPrefabName(container));

        internal static readonly HashSet<string> SupportedPrefabs = new HashSet<string>
        {
            "piece_chest_wood",
            "piece_chest",
            "piece_chest_blackmetal",
            "piece_chest_grausten",
            "piece_chest_private",
            "piece_chest_barrel",
            "piece_chest_warderobe",
            CartPrefab
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
