using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HexChestHacks.Core;
using HexChestHacks.Core.Services;
using System.Reflection;

namespace HexChestHacks
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string PluginGuid = "com.hex.chesthacks";
        private const string PluginName = "HexChestHacks";
        private const string PluginVersion = "1.1.2";

        private Harmony _harmonyInstance;

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            HexChestHacksConfigurationSetup.Init(Config);

            _harmonyInstance = new Harmony(PluginGuid);
            _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            Log.LogInfo($"{PluginName} v{PluginVersion} unloaded.");

            HexChestHacksConfigurationSetup.Dispose();

            _harmonyInstance?.UnpatchSelf();
            _harmonyInstance = null;

            LoadedContainerService.Clear();

            Instance = null;
            Log = null;
        }
    }
}
