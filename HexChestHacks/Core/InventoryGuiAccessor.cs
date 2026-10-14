using HarmonyLib;
using UnityEngine;

namespace HexChestHacks.Core
{
    internal static class InventoryGuiAccessor
    {
        private static readonly AccessTools.FieldRef<InventoryGui, InventoryGrid> ContainerGrid =
            AccessTools.FieldRefAccess<InventoryGui, InventoryGrid>("m_containerGrid");

        private static readonly AccessTools.FieldRef<InventoryGui, RectTransform> Container =
            AccessTools.FieldRefAccess<InventoryGui, RectTransform>("m_container");

        internal static InventoryGrid GetContainerGrid(InventoryGui gui) => ContainerGrid(gui);

        internal static RectTransform GetContainer(InventoryGui gui) => Container(gui);
    }
}
