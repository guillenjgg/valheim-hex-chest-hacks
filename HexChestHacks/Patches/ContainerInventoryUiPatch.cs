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
        private const float PanelGap = 16f;
        private static readonly Vector3[] PanelCorners = new Vector3[4];

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
        private static RectTransform _scrollbarRect;
        private static Vector2 _originalScrollbarPosition;
        private static Vector2 _originalScrollbarSize;
        private static Vector2 _originalViewportPosition;
        private const float ScrollbarGap = 6f;

        private static float _originalPanelHeight;
        private static float _originalViewportHeight;
        private static float _originalPanelWidth;
        private static float _originalViewportWidth;

        private static Vector2 _originalPanelPosition;
        private static Vector2 _originalPanelPivot;
        private static Vector3 _originalPanelScale;

        private static readonly Dictionary<RectTransform, Vector2> ButtonPositions =
            new Dictionary<RectTransform, Vector2>();

        private static bool _layoutModified;
        private static int _lastRows = -1;
        private static float _lastViewportHeight = -1f;

        // Run after inventory UI mods have resized/repositioned the player panel.
        internal static void RefreshLayout()
        {
            if (_layoutModified && _cachedGui != null && _cachedGrid != null &&
                _containerPanel != null && _containerPanel.gameObject.activeInHierarchy &&
                InventoryGuiAccessor.GetCurrentContainer(_cachedGui) != null)
            {
                ResizePanel(_cachedGrid);
            }
        }

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
            _scrollbarRect = null;

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
                _scrollbarRect = _scrollbar.transform as RectTransform;
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
            _originalPanelWidth = _containerPanel.rect.width;
            _originalViewportWidth = _gridViewport.rect.width;
            _originalViewportPosition = _gridViewport.anchoredPosition;
            if (_scrollbarRect != null)
            {
                _originalScrollbarPosition = _scrollbarRect.anchoredPosition;
                _originalScrollbarSize = _scrollbarRect.sizeDelta;
            }

            _originalPanelPivot = _containerPanel.pivot;
            _originalPanelPosition = _containerPanel.anchoredPosition;
            _originalPanelScale = _containerPanel.localScale;

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
            var columns = InventoryAccessor.GetWidth(inventory);
            var removeScrollbar = HexChestHacksConfigurationSetup.RemoveChestScrollBars.Value;
            var scrollbarGutter = !removeScrollbar && _scrollbarRect != null
                ? _scrollbarRect.rect.width + ScrollbarGap : 0f;
            var horizontalPadding = _originalPanelWidth - _originalViewportWidth;
            var desiredPanelWidth = Mathf.Max(_originalPanelWidth,
                columns * elementSpace + horizontalPadding + 20f + scrollbarGutter);
            var previousViewportWidth = _gridViewport.rect.width;
            var contentHeight = rows * elementSpace;

            var desiredPanelHeight = Mathf.Max(
                MinimumHeight,
                contentHeight + VerticalPadding);

            var parent = _containerPanel.parent as RectTransform;
            var panelBackground = _containerPanel.Find("Bkg") as RectTransform;
            var panelBounds = panelBackground != null ? panelBackground : _containerPanel;
            var backgroundPadding = Mathf.Max(0f, panelBounds.rect.height - _containerPanel.rect.height);
            var backgroundWidthPadding = Mathf.Max(0f, panelBounds.rect.width - _containerPanel.rect.width);
            var playerPanel = InventoryGuiAccessor.GetPlayer(_cachedGui);
            var panelTop = 0f;
            var positionPanel = parent != null && playerPanel != null;
            var panelScale = _originalPanelScale;
            var screenLeft = float.NegativeInfinity;
            var screenRight = float.PositiveInfinity;

            if (positionPanel)
            {
                var playerBottom = GetBottomInParent(playerPanel, parent);
                // ExtraSlots expands Bkg's anchors, not the outer player panel.
                var background = playerPanel.Find("Bkg") as RectTransform;
                if (background != null && background.gameObject.activeInHierarchy)
                {
                    playerBottom = Mathf.Min(playerBottom, GetBottomInParent(background, parent));
                }

                var playerGrid = InventoryGuiAccessor.GetPlayerGrid(_cachedGui);
                if (playerGrid != null)
                {
                    // Valheim sizes m_gridRoot to rows * elementSpace in UpdateGui.
                    // PlayerGrid itself can remain four rows tall with masking disabled.
                    var visibleGrid = InventoryGridAccessor.GetGridRoot(playerGrid);
                    var mask = playerGrid.GetComponent<RectMask2D>();
                    var scrollRect = playerGrid.GetComponent<ScrollRect>();
                    if (mask != null && mask.isActiveAndEnabled)
                    {
                        visibleGrid = scrollRect != null && scrollRect.viewport != null
                            ? scrollRect.viewport
                            : playerGrid.transform as RectTransform;
                    }
                    if (visibleGrid != null)
                    {
                        playerBottom = Mathf.Min(playerBottom, GetBottomInParent(visibleGrid, parent));
                    }
                }
                panelTop = playerBottom - PanelGap;

                // Fit all rows when scrollbars are removed; otherwise retain a scrollable viewport.
                var canvas = _containerPanel.GetComponentInParent<Canvas>();
                var canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
                if (canvasRect != null)
                {
                    canvasRect.GetWorldCorners(PanelCorners);
                    var screenBottom = Mathf.Max(parent.InverseTransformPoint(PanelCorners[0]).y,
                        parent.InverseTransformPoint(PanelCorners[3]).y) + PanelGap;
                    var availableHeight = panelTop - screenBottom;
                    screenLeft = parent.InverseTransformPoint(PanelCorners[0]).x + PanelGap;
                    screenRight = parent.InverseTransformPoint(PanelCorners[3]).x - PanelGap;
                    var widthScale = Mathf.Min(1f, Mathf.Max(1f, screenRight - screenLeft) /
                        ((desiredPanelWidth + backgroundWidthPadding) * Mathf.Max(0.001f, _originalPanelScale.x)));
                    if (HexChestHacksConfigurationSetup.RemoveChestScrollBars.Value)
                    {
                        var fullHeight = (desiredPanelHeight + backgroundPadding) *
                            Mathf.Max(0.001f, _originalPanelScale.y);
                        var fitScale = Mathf.Min(widthScale, Mathf.Max(1f, availableHeight) / fullHeight);
                        panelScale = new Vector3(_originalPanelScale.x * fitScale,
                            _originalPanelScale.y * fitScale, _originalPanelScale.z);
                    }
                    else
                    {
                        panelScale = new Vector3(_originalPanelScale.x * widthScale,
                            _originalPanelScale.y * widthScale, _originalPanelScale.z);
                        desiredPanelHeight = Mathf.Min(desiredPanelHeight,
                            Mathf.Max(VerticalPadding + 1f,
                                availableHeight / Mathf.Max(0.001f, panelScale.y) - backgroundPadding));
                    }
                }
            }

            _containerPanel.localScale = panelScale;
            _containerPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredPanelWidth);
            _containerPanel.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                desiredPanelHeight);

            if (positionPanel)
            {
                // Use parent-space bounds so anchors, pivots and UI scale are respected.
                panelBounds.GetWorldCorners(PanelCorners);
                var currentTop = parent.InverseTransformPoint(PanelCorners[1]).y;
                var position = _containerPanel.localPosition;
                position.y += panelTop - currentTop;
                var left = parent.InverseTransformPoint(PanelCorners[0]).x;
                var right = parent.InverseTransformPoint(PanelCorners[3]).x;
                if (right > screenRight) position.x -= right - screenRight;
                else if (left < screenLeft) position.x += screenLeft - left;
                _containerPanel.localPosition = position;
            }

            _gridViewport.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, desiredPanelWidth - horizontalPadding - scrollbarGutter);
            _gridViewport.anchoredPosition = new Vector2(
                _originalViewportPosition.x - scrollbarGutter * 0.5f, _originalViewportPosition.y);
            _gridViewport.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(0f, desiredPanelHeight - VerticalPadding));

            UpdateButtonPositions(desiredPanelHeight, desiredPanelWidth);
            PositionScrollbar();

            // UpdateGui centers slots only when its cached dimensions change.
            // Rebuild after changing viewport width so edge columns are recentered.
            if (!Mathf.Approximately(previousViewportWidth, _gridViewport.rect.width))
            {
                InventoryGridAccessor.RebuildLayout(grid);
            }

            if (!_layoutModified ||
                _lastContainer != InventoryGuiAccessor.GetCurrentContainer(_cachedGui) ||
                _lastRows != rows ||
                !Mathf.Approximately(_lastViewportHeight, _gridViewport.rect.height))
            {
                grid.ResetView();
            }

            _lastRows = rows;
            _lastViewportHeight = _gridViewport.rect.height;
            _layoutModified = true;
        }

        private static void PositionScrollbar()
        {
            if (_scrollbarRect == null) return;
            var parent = _scrollbarRect.parent as RectTransform;
            if (parent == null) return;

            // The vanilla scrollbar is centered at a fixed X/Y, not anchored to the grid edge.
            _gridViewport.GetWorldCorners(PanelCorners);
            var bottomRight = parent.InverseTransformPoint(PanelCorners[3]);
            var topRight = parent.InverseTransformPoint(PanelCorners[2]);
            var height = topRight.y - bottomRight.y;
            _scrollbarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            var position = _scrollbarRect.localPosition;
            position.x = bottomRight.x + ScrollbarGap + _scrollbarRect.pivot.x * _scrollbarRect.rect.width;
            position.y = bottomRight.y + _scrollbarRect.pivot.y * height;
            _scrollbarRect.localPosition = position;
        }

        private static float GetBottomInParent(RectTransform rect, RectTransform parent)
        {
            rect.GetWorldCorners(PanelCorners);
            return Mathf.Min(parent.InverseTransformPoint(PanelCorners[0]).y,
                parent.InverseTransformPoint(PanelCorners[3]).y);
        }

        private static void UpdateButtonPositions(float panelHeight, float panelWidth)
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
                var anchorX = (rect.anchorMin.x + rect.anchorMax.x) * 0.5f;
                var edge = rect.name == "TakeAll" ? 0f : 1f;

                rect.anchoredPosition = new Vector2(
                    originalPosition.x + (panelWidth - _originalPanelWidth) * (edge - anchorX),
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
                _containerPanel.localScale = _originalPanelScale;
                _containerPanel.anchoredPosition = _originalPanelPosition;

                _containerPanel.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, _originalPanelWidth);
                _containerPanel.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    _originalPanelHeight);
            }

            if (_gridViewport != null)
            {
                _gridViewport.anchoredPosition = _originalViewportPosition;
                _gridViewport.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, _originalViewportWidth);
                InventoryGridAccessor.RebuildLayout(_cachedGrid);
                _gridViewport.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    _originalViewportHeight);
            }

            if (_scrollbarRect != null)
            {
                _scrollbarRect.sizeDelta = _originalScrollbarSize;
                _scrollbarRect.anchoredPosition = _originalScrollbarPosition;
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
            _lastViewportHeight = -1f;
        }
    }
}
