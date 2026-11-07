using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;

namespace HexChestHacks.Core.ReflectionAccessors
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

        private static readonly AccessTools.FieldRef<InventoryGrid, RectTransform> GridRoot =
            AccessTools.FieldRefAccess<InventoryGrid, RectTransform>("m_gridRoot");

        internal static RectTransform GetGridRoot(InventoryGrid grid) => GridRoot(grid);

        private static readonly AccessTools.FieldRef<InventoryGrid, int> Width =
            AccessTools.FieldRefAccess<InventoryGrid, int>("m_width");

        internal static void RebuildLayout(InventoryGrid grid) => Width(grid) = -1;
    }
}
