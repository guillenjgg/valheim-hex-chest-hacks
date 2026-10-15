using HarmonyLib;
using HexChestHacks.Core.ReflectionAccessors;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HexChestHacks.Core
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
    internal static class ContainerInventoryUiPatch
    {
        private const float VerticalPadding = 85f;
        private const float MinimumHeight = 150f;

        private static readonly string[] HeaderButtonNames =
        {
            "TakeAll",
            "StackAll",
            "com.hex.quickstackstorage.ContainerSortButton"
        };

        private static InventoryGui _cachedGui;
        private static InventoryGrid _cachedGrid;

        private static RectTransform _containerPanel;
        private static RectTransform _gridViewport;

        private static ScrollRect _scrollRect;
        private static Scrollbar _scrollbar;

        private static float _originalPanelHeight;
        private static float _originalViewportHeight;

        private static Vector2 _originalPanelPosition;
        private static Vector2 _originalPanelPivot;

        private static bool _originalScrollEnabled;
        private static bool _originalScrollbarActive;

        private static readonly Dictionary<RectTransform, Vector2> ButtonPositions =
            new Dictionary<RectTransform, Vector2>();

        private static bool _layoutModified;
        private static bool _scrollbarModified;
        private static bool _lastRemoveScrollbar;
        private static int _lastRows = -1;

        [HarmonyPostfix]
        private static void Postfix(InventoryGui __instance)
        {
            var grid = InventoryGuiAccessor.GetContainerGrid(__instance);

            if (grid == null)
            {
                return;
            }

            Initialize(__instance, grid);

            if (_containerPanel == null || _gridViewport == null)
            {
                return;
            }

            var currentContainer = InventoryGuiAccessor.GetCurrentContainer(__instance);

            if (currentContainer == null)
            {
                RestoreLayout();
                return;
            }

            var inventory = InventoryGridAccessor.GetInventory(grid);

            if (inventory == null)
            {
                return;
            }

            var rows = InventoryAccessor.GetHeight(inventory);

            if (rows <= 0)
            {
                return;
            }

            var removeScrollbar = BepInExConfigurationSetup.RemoveChestScrollBars.Value;

            var elementSpace = InventoryGridAccessor.GetElementSpace(grid);
            var contentHeight = rows * elementSpace;

            var desiredPanelHeight = Mathf.Max(
                MinimumHeight,
                contentHeight + VerticalPadding);

            if (!_layoutModified)
            {
                CacheOriginalLayout();
            }

            _containerPanel.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                desiredPanelHeight);

            _gridViewport.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(0f, desiredPanelHeight - VerticalPadding));

            UpdateButtonPositions(desiredPanelHeight);

            UpdateScrollbar(removeScrollbar);

            if (!_layoutModified ||
                _lastRows != rows ||
                _lastRemoveScrollbar != removeScrollbar)
            {
                grid.ResetView();
            }

            _lastRows = rows;
            _lastRemoveScrollbar = removeScrollbar;
            _layoutModified = true;
        }

        private static void Initialize(InventoryGui gui, InventoryGrid grid)
        {
            if (_cachedGui == gui &&
                _cachedGrid == grid &&
                _containerPanel != null &&
                _gridViewport != null)
            {
                return;
            }

            RestoreLayout();

            _cachedGui = gui;
            _cachedGrid = grid;

            _containerPanel = InventoryGuiAccessor.GetContainer(gui);
            _gridViewport = null;
            _scrollRect = null;
            _scrollbar = null;

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

            CacheOriginalLayout();

            _layoutModified = false;
            _scrollbarModified = false;
            _lastRemoveScrollbar = false;
            _lastRows = -1;
        }

        private static void CacheOriginalLayout()
        {
            _originalPanelHeight = _containerPanel.rect.height;
            _originalViewportHeight = _gridViewport.rect.height;

            _originalPanelPivot = _containerPanel.pivot;
            _originalPanelPosition = _containerPanel.anchoredPosition;

            if (!_scrollbarModified)
            {
                _originalScrollEnabled = _scrollRect != null && _scrollRect.vertical;
                _originalScrollbarActive = _scrollbar != null &&
                    _scrollbar.gameObject.activeSelf;
            }

            ButtonPositions.Clear();

            foreach (var buttonName in HeaderButtonNames)
            {
                var button = _containerPanel.Find(buttonName) as RectTransform;

                if (button != null)
                {
                    ButtonPositions[button] = button.anchoredPosition;
                }
            }
        }

        private static void UpdateButtonPositions(float panelHeight)
        {
            var heightDifference = panelHeight - _originalPanelHeight;

            foreach (var entry in ButtonPositions)
            {
                var rect = entry.Key;

                if (rect == null)
                {
                    continue;
                }

                var originalPosition = entry.Value;
                var anchorY = (rect.anchorMin.y + rect.anchorMax.y) * 0.5f;

                var offset = heightDifference * (1f - anchorY);

                rect.anchoredPosition = new Vector2(
                    originalPosition.x,
                    originalPosition.y + offset);
            }
        }

        private static void UpdateScrollbar(bool removeScrollbar)
        {
            if (removeScrollbar)
            {
                if (_scrollRect != null)
                {
                    _scrollRect.vertical = false;
                }

                if (_scrollbar != null)
                {
                    _scrollbar.gameObject.SetActive(false);
                }

                _scrollbarModified = true;
                return;
            }

            if (_scrollRect != null)
            {
                _scrollRect.vertical = _originalScrollEnabled;
            }

            if (_scrollbar != null)
            {
                _scrollbar.gameObject.SetActive(_originalScrollbarActive);
            }

            _scrollbarModified = false;
        }

        private static void RestoreLayout()
        {
            if (!_layoutModified && !_scrollbarModified)
            {
                return;
            }

            if (_containerPanel != null)
            {
                _containerPanel.pivot = _originalPanelPivot;
                _containerPanel.anchoredPosition = _originalPanelPosition;

                _containerPanel.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    _originalPanelHeight);
            }

            if (_gridViewport != null)
            {
                _gridViewport.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    _originalViewportHeight);
            }

            foreach (var entry in ButtonPositions)
            {
                if (entry.Key != null)
                {
                    entry.Key.anchoredPosition = entry.Value;
                }
            }

            if (_scrollRect != null)
            {
                _scrollRect.vertical = _originalScrollEnabled;
            }

            if (_scrollbar != null)
            {
                _scrollbar.gameObject.SetActive(_originalScrollbarActive);
            }

            _layoutModified = false;
            _scrollbarModified = false;
            _lastRemoveScrollbar = false;
            _lastRows = -1;
        }
    }
}
