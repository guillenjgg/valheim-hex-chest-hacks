using HarmonyLib;
using HexChestHacks.Core.ReflectionAccessors;
using HexChestHacks.Core.Services;

namespace HexChestHacks.Core
{
    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    internal static class ContainerPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Container __instance)
        {
            ContainerService.RestoreSavedDimensions(__instance);
        }

        [HarmonyPostfix]
        private static void Postfix(Container __instance)
        {
            if (!Constants.IsSupportedChest(__instance.name))
            {
                return;
            }

            LoadedContainerService.Register(__instance);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Load))]
    internal static class ContainerLoadPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Container __instance)
        {
            if (!Constants.IsSupportedChest(__instance.name))
            {
                return;
            }

            var nview = ContainerAccessor.GetNetView(__instance);

            if (nview.GetZDO().DataRevision == ContainerAccessor.GetLastRevision(__instance) || ContainerAccessor.GetInUse(__instance))
            {
                return;
            }

            ContainerService.RestoreSavedCapacity(__instance);
        }

        [HarmonyPostfix]
        private static void Postfix(Container __instance, bool __result)
        {
            if (!Constants.IsSupportedChest(__instance.name) || !__result)
            {
                return;
            }

            LoadedContainerService.Register(__instance);

            if (!ContainerService.HasValidItemPositions(__instance))
            {
                return;
            }

            var nview = ContainerAccessor.GetNetView(__instance);

            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            ContainerService.InitializeSavedCapacity(__instance);
            ContainerService.ApplyConfiguredCapacity(__instance);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.OnContainerChanged))]
    internal static class ContainerChangedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Container __instance)
        {
            if (!Constants.IsSupportedChest(__instance.name))
            {
                return;
            }

            var nview = ContainerAccessor.GetNetView(__instance);

            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            if (ContainerService.HasPendingShrink(__instance))
            {
                ContainerService.ApplyConfiguredCapacity(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    internal static class ContainerInteractPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Container __instance, bool hold)
        {
            if (hold || !Constants.IsSupportedChest(__instance.name))
            {
                return;
            }

            if (!ContainerService.HasPendingShrink(__instance))
            {
                return;
            }

            MessageHud.instance?.ShowMessage(
                MessageHud.MessageType.Center,
                "Chest cannot shrink until enough items are removed.");
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
    internal static class ContainerOnDestroyedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Container __instance)
        {
            LoadedContainerService.Unregister(__instance);
        }
    }
}