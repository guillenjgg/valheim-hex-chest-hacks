
using System.Collections.Generic;

namespace HexChestHacks.Core
{
    internal static class ContainerService
    {
        private const string WidthKey = "HexChestHacks_Width";
        private const string HeightKey = "HexChestHacks_Height";

        private const int MinimumCapacity = 1;
        private const int MaximumCapacity = 8;

        private static readonly Dictionary<string, (int Width, int Height)> DefaultCapacities =
            new Dictionary<string, (int Width, int Height)>();

        internal static void RegisterDefaultCapacity(Container container)
        {
            if (container == null || !Constants.IsSupportedChest(container.name))
            {
                return;
            }

            var prefabName = container.name.Replace("(Clone)", "").Trim();

            if (!DefaultCapacities.ContainsKey(prefabName))
            {
                DefaultCapacities[prefabName] = (
                    ContainerAccessor.GetWidth(container),
                    ContainerAccessor.GetHeight(container));
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

            var prefabName = container.name.Replace("(Clone)", "").Trim();

            if (!DefaultCapacities.TryGetValue(prefabName, out var capacity))
            {
                return false;
            }

            width = capacity.Width;
            height = capacity.Height;

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
                return;
            }

            var columns = Plugin.EnableCustomChestCapacity.Value ? Plugin.Columns.Value : defaultWidth;
            var rows = Plugin.EnableCustomChestCapacity.Value ? Plugin.Rows.Value : defaultHeight;

            TryResizeStorage(container, rows, columns);
        }

        internal static bool TryResizeStorage(Container container, int rows, int columns)
        {
            if (container == null || !Constants.IsSupportedChest(container.name))
            {
                return false;
            }

            if (!IsValidCapacity(columns, rows))
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
                Plugin.Log.LogInfo($"Cannot shrink {container.name}: chest is not empty.");
                Plugin.NotifyChestShrinkRejected();

                return false;
            }

            SetCapacity(container, inventory, columns, rows);
            SaveCapacity(zdo, columns, rows);

            return true;
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
            return width >= MinimumCapacity && width <= MaximumCapacity &&
                   height >= MinimumCapacity && height <= MaximumCapacity;
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
