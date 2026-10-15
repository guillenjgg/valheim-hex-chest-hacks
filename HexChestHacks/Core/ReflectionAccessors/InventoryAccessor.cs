using HarmonyLib;
using System.Collections.Generic;

namespace HexChestHacks.Core.ReflectionAccessors
{
    internal static class InventoryAccessor
    {
        private static readonly AccessTools.FieldRef<Inventory, int> Width =
            AccessTools.FieldRefAccess<Inventory, int>("m_width");

        private static readonly AccessTools.FieldRef<Inventory, int> Height =
            AccessTools.FieldRefAccess<Inventory, int>("m_height");

        private static readonly AccessTools.FieldRef<Inventory, List<ItemDrop.ItemData>> Items =
            AccessTools.FieldRefAccess<Inventory, List<ItemDrop.ItemData>>("m_inventory");

        internal static int GetWidth(Inventory inventory) => Width(inventory);

        internal static int GetHeight(Inventory inventory) => Height(inventory);

        internal static void SetWidth(Inventory inventory, int width) => Width(inventory) = width;

        internal static void SetHeight(Inventory inventory, int height) => Height(inventory) = height;

        internal static List<ItemDrop.ItemData> GetItems(Inventory inventory) => Items(inventory);
    }
}