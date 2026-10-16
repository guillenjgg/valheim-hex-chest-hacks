using HexChestHacks.Core.ReflectionAccessors;
using System.Collections.Generic;

namespace HexChestHacks.Core.Services
{
    internal static class LoadedContainerService
    {
        private static readonly HashSet<Container> Containers = new HashSet<Container>();

        private static readonly Dictionary<Inventory, Container> InventoryContainers =
            new Dictionary<Inventory, Container>();

        internal static void Register(Container container)
        {
            if (container == null || !Constants.IsSupportedChest(container.name))
            {
                return;
            }

            Containers.Add(container);

            var inventory = ContainerAccessor.GetInventory(container);

            if (inventory != null)
            {
                InventoryContainers[inventory] = container;
            }
        }

        internal static void Unregister(Container container)
        {
            if (container == null)
            {
                return;
            }

            Containers.Remove(container);

            var inventory = ContainerAccessor.GetInventory(container);

            if (inventory != null &&
                InventoryContainers.TryGetValue(inventory, out var registeredContainer) &&
                registeredContainer == container)
            {
                InventoryContainers.Remove(inventory);
            }
        }

        internal static bool TryGetContainer(Inventory inventory, out Container container)
        {
            container = null;

            if (inventory == null)
            {
                return false;
            }

            if (!InventoryContainers.TryGetValue(inventory, out container))
            {
                container = null;
                return false;
            }

            if (container == null || !Constants.IsSupportedChest(container.name))
            {
                InventoryContainers.Remove(inventory);
                container = null;
                return false;
            }

            return true;
        }

        internal static void ApplyConfiguration()
        {
            Containers.RemoveWhere(container => container == null);

            foreach (var container in Containers)
            {
                var nview = ContainerAccessor.GetNetView(container);

                if (nview == null || !nview.IsValid() || !nview.IsOwner())
                {
                    continue;
                }

                ContainerService.ApplyConfiguredCapacity(container);
            }
        }

        internal static void Clear()
        {
            Containers.Clear();
            InventoryContainers.Clear();
        }
    }
}
