using HarmonyLib;
using HexChestHacks.Core;

namespace HexChestHacks.Patches
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class ZNetScenePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ZNetScene __instance)
        {
            SnappointService.AddSnappoints(__instance);
        }
    }
}
