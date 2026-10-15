using HarmonyLib;
using UnityEngine;

namespace HexChestHacks.Core.ReflectionAccessors
{
    internal static class InventoryGuiAccessor
    {
        private static readonly AccessTools.FieldRef<InventoryGui, InventoryGrid> ContainerGrid =
            AccessTools.FieldRefAccess<InventoryGui, InventoryGrid>("m_containerGrid");

        private static readonly AccessTools.FieldRef<InventoryGui, RectTransform> Container =
            AccessTools.FieldRefAccess<InventoryGui, RectTransform>("m_container");

        private static readonly AccessTools.FieldRef<InventoryGui, Container> CurrentContainer =
            AccessTools.FieldRefAccess<InventoryGui, Container>("m_currentContainer");

        internal static InventoryGrid GetContainerGrid(InventoryGui gui) => ContainerGrid(gui);

        internal static RectTransform GetContainer(InventoryGui gui) => Container(gui);

        internal static Container GetCurrentContainer(InventoryGui gui) => CurrentContainer(gui);

        
    }
}
