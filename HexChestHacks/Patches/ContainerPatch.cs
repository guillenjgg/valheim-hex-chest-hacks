//using HarmonyLib;
//using HexChestHacks;
//using HexChestHacks.Core;

//[HarmonyPatch(typeof(Container), nameof(Container.Awake))]
//internal static class ContainerPatch
//{
//    [HarmonyPrefix]
//    private static void AwakePrefix(Container __instance)
//    {
//        if (!SnappointService.IsSupportedChest(__instance.gameObject.name))
//        {
//            return;
//        }

//        __instance.m_width = Plugin.ChestColumns.Value;
//        __instance.m_height = Plugin.ChestRows.Value;
//    }
//}