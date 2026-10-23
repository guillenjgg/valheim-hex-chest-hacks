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
                    continue;
                }

                DefaultCapacities[prefabName] = new Vector2Int(width, height);
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

            if (!IsValidCapacity(columns, rows))
            {
                return false;
            }

            return InventoryAccessor.GetItems(inventory).Count > columns * rows;
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
                return;
            }

            GetTargetCapacity(container, defaultWidth, defaultHeight, out var columns, out var rows);

            TryResizeStorage(container, rows, columns);
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
                return false;
            }

            var currentWidth = InventoryAccessor.GetWidth(inventory);
            var currentHeight = InventoryAccessor.GetHeight(inventory);

            if (currentWidth == columns && currentHeight == rows)
            {
                SaveCapacity(zdo, columns, rows);
                return true;
            }

            var items = InventoryAccessor.GetItems(inventory);

            if (items.Count > columns * rows)
            {
                return false;
            }

            if (!TryRepositionItems(inventory, columns, rows, out var repositioned))
            {
                return false;
            }

            SetCapacity(container, inventory, columns, rows);
            SaveCapacity(zdo, columns, rows);

            InventoryAccessor.Changed(inventory);

            return true;
        }

        internal static void RestoreSavedDimensions(Container container)
        {
            if (!Constants.IsSupportedChest(container.name))
            {
                return;
            }

            var nview = container.m_rootObjectOverride
                ? container.m_rootObjectOverride.GetComponent<ZNetView>()
                : container.GetComponent<ZNetView>();

            var zdo = nview.GetZDO();

            if (zdo == null)
            {
                return;
            }

            var width = zdo.GetInt(WidthKey, 0);
            var height = zdo.GetInt(HeightKey, 0);

            if (!IsValidCapacity(width, height))
            {
                return;
            }

            ContainerAccessor.SetWidth(container, width);
            ContainerAccessor.SetHeight(container, height);
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

            columns = isWardrobe ? HexChestHacksConfigurationSetup.WardrobeColumns.Value : HexChestHacksConfigurationSetup.Columns.Value;
            rows = isWardrobe ? HexChestHacksConfigurationSetup.WardrobeRows.Value : HexChestHacksConfigurationSetup.Rows.Value;
        }

        private static bool IsCustomCapacityEnabled(Container container)
        {
            var isWardrobe = NormalizePrefabName(container.name) == WardrobePrefabName;

            return isWardrobe
                ? HexChestHacksConfigurationSetup.EnableCustomWardrobeCapacity.Value
                : HexChestHacksConfigurationSetup.EnableCustomChestCapacity.Value;
        }

        private static bool AreItemPositionsValid(Inventory inventory, int width, int height)
        {
            if (!IsValidCapacity(width, height))
            {
                return false;
            }

            var occupied = new bool[width, height];

            foreach (var item in InventoryAccessor.GetItems(inventory))
            {
                var position = item.m_gridPos;

                if (position.x < 0 || position.x >= width ||
                    position.y < 0 || position.y >= height ||
                    occupied[position.x, position.y])
                {
                    return false;
                }

                occupied[position.x, position.y] = true;
            }

            return true;
        }

        private static bool TryRepositionItems(Inventory inventory, int columns, int rows, out bool repositioned)
        {
            repositioned = false;

            var items = InventoryAccessor.GetItems(inventory);

            if (items.Count > columns * rows)
            {
                return false;
            }

            var occupied = new bool[columns, rows];
            var displaced = new List<ItemDrop.ItemData>();

            foreach (var item in items)
            {
                var position = item.m_gridPos;

                if (position.x >= 0 && position.x < columns &&
                    position.y >= 0 && position.y < rows &&
                    !occupied[position.x, position.y])
                {
                    occupied[position.x, position.y] = true;
                }
                else
                {
                    displaced.Add(item);
                }
            }

            if (displaced.Count == 0)
            {
                return true;
            }

            var available = new List<Vector2i>();

            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    if (!occupied[x, y])
                    {
                        available.Add(new Vector2i(x, y));
                    }
                }
            }

            if (available.Count < displaced.Count)
            {
                return false;
            }

            for (var i = 0; i < displaced.Count; i++)
            {
                displaced[i].m_gridPos = available[i];
            }

            repositioned = true;

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