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
        private const string WardrobePrefabName = "piece_chest_warderobe";
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
        private static Container _lastContainer;

        private static RectTransform _containerPanel;
        private static RectTransform _gridViewport;
        private static Scrollbar _scrollbar;
        private static Graphic[] _scrollbarGraphics;

        private static float _originalPanelHeight;
        private static float _originalViewportHeight;

        private static Vector2 _originalPanelPosition;
        private static Vector2 _originalPanelPivot;

        private static readonly Dictionary<RectTransform, Vector2> ButtonPositions =
            new Dictionary<RectTransform, Vector2>();

        private static bool _layoutModified;
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
                return;
            }

            var prefabName = Utils.GetPrefabName(currentContainer.gameObject);

            if (!Constants.IsSupportedChest(prefabName))
            {
                RestoreLayout();
                RestoreScrollbar();

                _lastContainer = null;
                _lastRows = -1;
                return;
            }

            var isWardrobe = prefabName == WardrobePrefabName;

            var customCapacityEnabled = isWardrobe
                ? HexChestHacksConfigurationSetup.EnableCustomWardrobeCapacity.Value
                : HexChestHacksConfigurationSetup.EnableCustomChestCapacity.Value;

            var removeScrollbar = HexChestHacksConfigurationSetup.RemoveChestScrollBars.Value;

            if (customCapacityEnabled || removeScrollbar)
            {
                ResizePanel(grid);
            }
            else
            {
                RestoreLayout();
            }

            UpdateScrollbar(removeScrollbar);

            _lastContainer = currentContainer;
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
            RestoreScrollbar();

            _cachedGui = gui;
            _cachedGrid = grid;

            _containerPanel = InventoryGuiAccessor.GetContainer(gui);
            _gridViewport = null;
            _scrollbar = null;
            _scrollbarGraphics = null;

            if (_containerPanel == null)
            {
                return;
            }

            var scrollRect = grid.GetComponentInParent<ScrollRect>();

            if (scrollRect != null)
            {
                _gridViewport = scrollRect.viewport != null
                    ? scrollRect.viewport
                    : scrollRect.transform as RectTransform;
            }
            else
            {
                _gridViewport = grid.transform as RectTransform;
            }

            _scrollbar = InventoryGridAccessor.GetScrollbar(grid);

            if (_scrollbar != null)
            {
                _scrollbarGraphics = _scrollbar.GetComponentsInChildren<Graphic>(true);
            }

            if (_gridViewport == null)
            {
                return;
            }

            CacheOriginalLayout();

            _layoutModified = false;
            _lastContainer = null;
            _lastRows = -1;
        }

        private static void CacheOriginalLayout()
        {
            _originalPanelHeight = _containerPanel.rect.height;
            _originalViewportHeight = _gridViewport.rect.height;

            _originalPanelPivot = _containerPanel.pivot;
            _originalPanelPosition = _containerPanel.anchoredPosition;

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

        private static void ResizePanel(InventoryGrid grid)
        {
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

            var elementSpace = InventoryGridAccessor.GetElementSpace(grid);
            var contentHeight = rows * elementSpace;

            var desiredPanelHeight = Mathf.Max(
                MinimumHeight,
                contentHeight + VerticalPadding);

            _containerPanel.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                desiredPanelHeight);

            _gridViewport.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(0f, desiredPanelHeight - VerticalPadding));

            UpdateButtonPositions(desiredPanelHeight);

            if (!_layoutModified ||
                _lastContainer != InventoryGuiAccessor.GetCurrentContainer(_cachedGui) ||
                _lastRows != rows)
            {
                grid.ResetView();
            }

            _lastRows = rows;
            _layoutModified = true;
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
            if (_scrollbar == null || _scrollbarGraphics == null)
            {
                return;
            }

            foreach (var graphic in _scrollbarGraphics)
            {
                if (graphic != null && graphic.enabled == removeScrollbar)
                {
                    graphic.enabled = !removeScrollbar;
                }
            }
        }

        private static void RestoreScrollbar()
        {
            UpdateScrollbar(false);
        }

        private static void RestoreLayout()
        {
            if (!_layoutModified)
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

            _layoutModified = false;
            _lastRows = -1;
        }
    }
}
