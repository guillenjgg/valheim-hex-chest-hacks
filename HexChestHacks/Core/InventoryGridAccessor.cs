
using HarmonyLib;
using UnityEngine.UI;

namespace HexChestHacks.Core
{
    internal static class InventoryGridAccessor
    {
        private static readonly AccessTools.FieldRef<InventoryGrid, Inventory> Inventory =
            AccessTools.FieldRefAccess<InventoryGrid, Inventory>("m_inventory");

        private static readonly AccessTools.FieldRef<InventoryGrid, float> ElementSpace =
            AccessTools.FieldRefAccess<InventoryGrid, float>("m_elementSpace");

        private static readonly AccessTools.FieldRef<InventoryGrid, Scrollbar> Scrollbar =
            AccessTools.FieldRefAccess<InventoryGrid, Scrollbar>("m_scrollbar");

        internal static Inventory GetInventory(InventoryGrid grid) => Inventory(grid);

        internal static float GetElementSpace(InventoryGrid grid) => ElementSpace(grid);

        internal static Scrollbar GetScrollbar(InventoryGrid grid) => Scrollbar(grid);
    }
}
