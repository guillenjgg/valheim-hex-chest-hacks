
using System.Collections.Generic;
using UnityEngine;

namespace HexChestHacks.Core
{
    internal static class LoadedContainerService
    {
        private static readonly HashSet<Container> Containers = new HashSet<Container>();

        internal static void Register(Container container)
        {
            if (container == null || !Constants.IsSupportedChest(container.name))
            {
                return;
            }

            Containers.Add(container);
        }

        internal static void Unregister(Container container)
        {
            Containers.Remove(container);
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
        }
    }
}
