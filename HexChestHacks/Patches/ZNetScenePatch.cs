using HarmonyLib;
using HexChestHacks.Core.Services;

namespace HexChestHacks.Patches
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class ZNetScenePatch
    {
        private static void Postfix(ZNetScene __instance)
        {
            ContainerService.CacheDefaultCapacities(__instance);
            SnappointService.AddSnappoints(__instance);
        }
    }
}
