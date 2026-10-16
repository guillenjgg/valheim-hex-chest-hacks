using HarmonyLib;
using HexChestHacks.Core;
using HexChestHacks.Core.Services;

namespace HexChestHacks.Patches
{
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.TopFirst))]
    internal static class InventoryTopFirstPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!HexChestHacksConfigurationSetup.EnableTopFirstChestPlacement.Value)
            {
                return;
            }

            if (!LoadedContainerService.TryGetContainer(__instance, out var container))
            {
                return;
            }

            var vanillaTopFirst = __result;

            __result = true;
        }
    }
}
