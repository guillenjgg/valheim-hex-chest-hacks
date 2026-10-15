using HexChestHacks.Core.ReflectionAccessors;
using System.Collections.Generic;
using UnityEngine;

namespace HexChestHacks.Core.Services
{
    internal static class ContainerService
    {
        private const string WidthKey = "HexChestHacks_Width";
        private const string HeightKey = "HexChestHacks_Height";
        private const string WardrobePrefabName = "piece_chest_warderobe";

        private const int MinimumCapacity = 1;
        private const int MaximumCapacity = 10;

        private static readonly Dictionary<string, Vector2Int> DefaultCapacities =
            new Dictionary<string, Vector2Int>();

        internal static void CacheDefaultCapacities(ZNetScene scene)
        {
            if (scene == null)
            {
                return;
            }

            foreach (var prefab in scene.m_prefabs)
            {
                if (prefab == null || !Constants.IsSupportedChest(prefab.name))
                {
                    continue;
                }

                var container = prefab.GetComponent<Container>();

                if (container == null)
                {
                    continue;
                }

                var prefabName = NormalizePrefabName(prefab.name);

                if (DefaultCapacities.ContainsKey(prefabName))
                {
                    continue;
                }

                var width = ContainerAccessor.GetWidth(container);
                var height = ContainerAccessor.GetHeight(container);

                if (!IsValidCapacity(width, height))
                {
                    Plugin.Log.LogWarning($"Invalid default capacity for {prefabName}: {width}x{height}");
                    continue;
                }

                DefaultCapacities[prefabName] = new Vector2Int(width, height);

                Plugin.Log.LogInfo($"Cached default chest capacity: {prefabName} | {width}x{height}");
            }
        }

        internal static bool TryGetDefaultCapacity(Container container, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (container == null || !Constants.IsSupportedChest(container.name))
            {
                return false;
            }

            var prefabName = NormalizePrefabName(container.name);

            if (!DefaultCapacities.TryGetValue(prefabName, out var capacity))
            {
                return false;
            }

            width = capacity.x;
            height = capacity.y;

            return true;
        }

        internal static bool HasValidItemPositions(Container container)
        {
            if (container == null)
            {
                return false;
            }

            var inventory = ContainerAccessor.GetInventory(container);

            if (inventory == null)
            {
                return false;
            }

            return AreItemPositionsValid(
                inventory,
                InventoryAccessor.GetWidth(inventory),
                InventoryAccessor.GetHeight(inventory));
        }

        internal static bool HasPendingShrink(Container container)
        {
            if (container == null ||
                !TryGetDefaultCapacity(container, out var defaultWidth, out var defaultHeight))
            {
                return false;
            }

            var inventory = ContainerAccessor.GetInventory(container);

            if (inventory == null)
            {
                return false;
            }

            GetTargetCapacity(container, defaultWidth, defaultHeight, out var columns, out var rows);

            var currentWidth = InventoryAccessor.GetWidth(inventory);
            var currentHeight = InventoryAccessor.GetHeight(inventory);

            var shrinking = columns < currentWidth || rows < currentHeight;

            return shrinking && InventoryAccessor.GetItems(inventory).Count > 0;
        }

        internal static bool RestoreSavedCapacity(Container container)
        {
            if (container == null || !Constants.IsSupportedChest(container.name))
            {
                return false;
            }

            var inventory = ContainerAccessor.GetInventory(container);
            var nview = ContainerAccessor.GetNetView(container);

            if (inventory == null || nview == null)
            {
                return false;
            }

            var zdo = nview.GetZDO();

            if (zdo == null)
            {
                return false;
            }

            var width = zdo.GetInt(WidthKey, 0);
            var height = zdo.GetInt(HeightKey, 0);

            if (!IsValidCapacity(width, height))
            {
                return false;
            }

            if (!AreItemPositionsValid(inventory, width, height))
            {
                Plugin.Log.LogWarning($"Saved chest capacity contains invalid item positions: {container.name}");
                return false;
            }

            SetCapacity(container, inventory, width, height);

            return true;
        }

        internal static void InitializeSavedCapacity(Container container)
        {
            if (container == null || !Constants.IsSupportedChest(container.name))
            {
                return;
            }

            var inventory = ContainerAccessor.GetInventory(container);
            var nview = ContainerAccessor.GetNetView(container);

            if (inventory == null || nview == null || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (zdo == null)
            {
                return;
            }

            var width = zdo.GetInt(WidthKey, 0);
            var height = zdo.GetInt(HeightKey, 0);

            if (IsValidCapacity(width, height))
            {
                return;
            }

            width = InventoryAccessor.GetWidth(inventory);
            height = InventoryAccessor.GetHeight(inventory);

            if (!IsValidCapacity(width, height) || !HasValidItemPositions(container))
            {
                Plugin.Log.LogWarning($"Cannot initialize chest capacity: {container.name}");
                return;
            }

            SaveCapacity(zdo, width, height);
        }

        internal static void ApplyConfiguredCapacity(Container container)
        {
            if (container == null)
            {
                return;
            }

            if (!TryGetDefaultCapacity(container, out var defaultWidth, out var defaultHeight))
            {
                Plugin.Log.LogWarning($"No cached default capacity found for {container.name}");
                return;
            }

            GetTargetCapacity(container, defaultWidth, defaultHeight, out var columns, out var rows);

            var inventory = ContainerAccessor.GetInventory(container);

            if (inventory == null)
            {
                return;
            }

            var customEnabled = IsCustomCapacityEnabled(container);

            Plugin.Log.LogInfo(
                $"Applying chest capacity: {container.name} | " +
                $"Default: {defaultWidth}x{defaultHeight} | " +
                $"Current: {InventoryAccessor.GetWidth(inventory)}x{InventoryAccessor.GetHeight(inventory)} | " +
                $"Target: {columns}x{rows} | " +
                $"Custom enabled: {customEnabled}");

            if (!TryResizeStorage(container, rows, columns))
            {
                Plugin.Log.LogWarning($"Chest capacity change rejected: {container.name}");
            }
        }

        internal static bool TryResizeStorage(Container container, int rows, int columns)
        {
            if (container == null || !Constants.IsSupportedChest(container.name))
            {
                return false;
            }

            var customEnabled = IsCustomCapacityEnabled(container);

            if (!IsValidCapacity(columns, rows) ||
                (customEnabled && !IsValidCustomCapacity(columns, rows)))
            {
                return false;
            }

            var inventory = ContainerAccessor.GetInventory(container);
            var nview = ContainerAccessor.GetNetView(container);

            if (inventory == null || nview == null || !nview.IsOwner())
            {
                return false;
            }

            var zdo = nview.GetZDO();

            if (zdo == null)
            {
                return false;
            }

            if (!HasValidItemPositions(container))
            {
                Plugin.Log.LogWarning($"Cannot resize chest with invalid item positions: {container.name}");
                return false;
            }

            var currentWidth = InventoryAccessor.GetWidth(inventory);
            var currentHeight = InventoryAccessor.GetHeight(inventory);

            if (currentWidth == columns && currentHeight == rows)
            {
                SaveCapacity(zdo, columns, rows);
                return true;
            }

            var shrinking = columns < currentWidth || rows < currentHeight;

            if (shrinking && InventoryAccessor.GetItems(inventory).Count > 0)
            {
                return false;
            }

            SetCapacity(container, inventory, columns, rows);
            SaveCapacity(zdo, columns, rows);

            return true;
        }

        private static void GetTargetCapacity(
            Container container,
            int defaultWidth,
            int defaultHeight,
            out int columns,
            out int rows)
        {
            columns = defaultWidth;
            rows = defaultHeight;

            if (!IsCustomCapacityEnabled(container))
            {
                return;
            }

            var isWardrobe = NormalizePrefabName(container.name) == WardrobePrefabName;

            columns = isWardrobe ? BepInExConfigurationSetup.WardrobeColumns.Value : BepInExConfigurationSetup.Columns.Value;
            rows = isWardrobe ? BepInExConfigurationSetup.WardrobeRows.Value : BepInExConfigurationSetup.Rows.Value;
        }

        private static bool IsCustomCapacityEnabled(Container container)
        {
            var isWardrobe = NormalizePrefabName(container.name) == WardrobePrefabName;

            return isWardrobe
                ? BepInExConfigurationSetup.EnableCustomWardrobeCapacity.Value
                : BepInExConfigurationSetup.EnableCustomChestCapacity.Value;
        }

        private static bool AreItemPositionsValid(Inventory inventory, int width, int height)
        {
            foreach (var item in InventoryAccessor.GetItems(inventory))
            {
                var position = item.m_gridPos;

                if (position.x < 0 || position.x >= width ||
                    position.y < 0 || position.y >= height)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidCapacity(int width, int height)
        {
            return width > 0 && height > 0;
        }

        private static bool IsValidCustomCapacity(int width, int height)
        {
            return width >= MinimumCapacity && width <= MaximumCapacity &&
                   height >= MinimumCapacity && height <= MaximumCapacity;
        }

        private static string NormalizePrefabName(string name)
        {
            return name.Replace("(Clone)", "").Trim();
        }

        private static void SetCapacity(Container container, Inventory inventory, int width, int height)
        {
            ContainerAccessor.SetWidth(container, width);
            ContainerAccessor.SetHeight(container, height);

            InventoryAccessor.SetWidth(inventory, width);
            InventoryAccessor.SetHeight(inventory, height);
        }

        private static void SaveCapacity(ZDO zdo, int width, int height)
        {
            if (zdo.GetInt(WidthKey, 0) != width)
            {
                zdo.Set(WidthKey, width);
            }

            if (zdo.GetInt(HeightKey, 0) != height)
            {
                zdo.Set(HeightKey, height);
            }
        }
    }
}
