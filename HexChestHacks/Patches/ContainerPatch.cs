using HarmonyLib;

namespace HexChestHacks.Core
{
    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    internal static class ContainerPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Container __instance)
        {
            ContainerService.RegisterDefaultCapacity(__instance);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Load))]
    internal static class ContainerLoadPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Container __instance, bool __result)
        {
            if (!__result)
            {
                return;
            }

            ContainerService.RestoreSavedCapacity(__instance);

            if (!ContainerService.HasValidItemPositions(__instance))
            {
                Plugin.Log.LogWarning($"Invalid item positions in {__instance.name}. Skipping resize.");
                return;
            }

            var nview = ContainerAccessor.GetNetView(__instance);

            if (nview == null || !nview.IsOwner())
            {
                return;
            }

            ContainerService.InitializeSavedCapacity(__instance);
            ContainerService.ApplyConfiguredCapacity(__instance);
        }
    }
}