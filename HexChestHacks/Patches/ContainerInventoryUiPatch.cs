
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace HexChestHacks.Core
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
    internal static class ContainerInventoryUiPatch
    {
        private const float VerticalPadding = 85f;
        private const float MinimumHeight = 150f;
        private const float ScreenPadding = 40f;

        private static RectTransform _containerPanel;
        private static RectTransform _gridViewport;
        private static ScrollRect _scrollRect;
        private static Scrollbar _scrollbar;

        [HarmonyPostfix]
        private static void Postfix(InventoryGui __instance)
        {
            var grid = InventoryGuiAccessor.GetContainerGrid(__instance);

            if (grid == null)
            {
                return;
            }

            var inventory = InventoryGridAccessor.GetInventory(grid);

            if (inventory == null)
            {
                return;
            }

            Initialize(__instance, grid);

            if (_containerPanel == null || _gridViewport == null)
            {
                return;
            }

            var rows = InventoryAccessor.GetHeight(inventory);

            if (rows <= 0)
            {
                return;
            }

            var elementSpace = InventoryGridAccessor.GetElementSpace(grid);
            var contentHeight = rows * elementSpace;

            var desiredPanelHeight = Mathf.Max(
                MinimumHeight,
                contentHeight + VerticalPadding);

            var canvas = _containerPanel.GetComponentInParent<Canvas>();
            var scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            var availableHeight = Screen.height / scaleFactor - ScreenPadding;

            var panelHeight = Mathf.Min(desiredPanelHeight, availableHeight);
            var viewportHeight = Mathf.Max(0f, panelHeight - VerticalPadding);

            _containerPanel.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                panelHeight);

            _gridViewport.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                viewportHeight);

            var needsScrolling = contentHeight > viewportHeight + 1f;

            if (_scrollRect != null)
            {
                _scrollRect.vertical = needsScrolling;
            }

            if (_scrollbar != null)
            {
                _scrollbar.gameObject.SetActive(needsScrolling);
            }

            if (!needsScrolling)
            {
                grid.ResetView();
            }
        }

        private static void Initialize(InventoryGui gui, InventoryGrid grid)
        {
            if (_containerPanel != null && _gridViewport != null)
            {
                return;
            }

            _containerPanel = InventoryGuiAccessor.GetContainer(gui);

            if (_containerPanel == null)
            {
                return;
            }

            _scrollRect = grid.GetComponentInParent<ScrollRect>();

            if (_scrollRect != null)
            {
                _gridViewport = _scrollRect.viewport != null
                    ? _scrollRect.viewport
                    : _scrollRect.transform as RectTransform;
            }
            else
            {
                _gridViewport = grid.transform as RectTransform;
            }

            _scrollbar = InventoryGridAccessor.GetScrollbar(grid);
        }
    }
}
