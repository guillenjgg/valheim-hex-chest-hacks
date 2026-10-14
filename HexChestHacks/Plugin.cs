
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HexChestHacks.Core;
using System;
using System.Reflection;

namespace HexChestHacks
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string PluginGuid = "com.hex.chesthacks";
        private const string PluginName = "HexChestHacks";
        private const string PluginVersion = "1.1.0";

        private Harmony _harmonyInstance;

        private static int _configurationRevision;
        private static int _lastWarningRevision = -1;

        internal static ConfigEntry<bool> EnableCustomChestCapacity;
        internal static ConfigEntry<int> Rows;
        internal static ConfigEntry<int> Columns;

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            EnableCustomChestCapacity = Config.Bind(
                "Chest Inventory",
                "EnableCustomChestCapacity",
                false,
                new ConfigDescription(
                    "Enable custom chest capacity.",
                    null,
                    new ConfigurationManagerAttributes { Order = 3 }));

            Columns = Config.Bind(
                "Chest Inventory",
                "Columns",
                5,
                new ConfigDescription(
                    "Number of chest inventory columns.",
                    new AcceptableValueRange<int>(1, 8),
                    new ConfigurationManagerAttributes { Order = 2 }));

            Rows = Config.Bind(
                "Chest Inventory",
                "Rows",
                2,
                new ConfigDescription(
                    "Number of chest inventory rows.",
                    new AcceptableValueRange<int>(1, 8),
                    new ConfigurationManagerAttributes { Order = 1 }));

            EnableCustomChestCapacity.SettingChanged += ConfigChange;
            Rows.SettingChanged += ConfigChange;
            Columns.SettingChanged += ConfigChange;

            _harmonyInstance = new Harmony(PluginGuid);
            _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            EnableCustomChestCapacity.SettingChanged -= ConfigChange;
            Rows.SettingChanged -= ConfigChange;
            Columns.SettingChanged -= ConfigChange;

            _harmonyInstance?.UnpatchSelf();
            _harmonyInstance = null;

            LoadedContainerService.Clear();

            Log.LogInfo($"{PluginName} v{PluginVersion} unloaded.");

            Instance = null;
            Log = null;
        }

        private static void ConfigChange(object sender, EventArgs e)
        {
            _configurationRevision++;

            Log.LogInfo($"Chest capacity configuration changed. Enabled: {EnableCustomChestCapacity.Value}, Rows: {Rows.Value}, Columns: {Columns.Value}");

            LoadedContainerService.ApplyConfiguration();
        }

        internal static void NotifyChestShrinkRejected()
        {
            if (_configurationRevision == 0 ||
                _lastWarningRevision == _configurationRevision)
            {
                return;
            }

            _lastWarningRevision = _configurationRevision;

            MessageHud.instance?.ShowMessage(
                MessageHud.MessageType.Center,
                "Some chests could not shrink because they contain items.");
        }
    }

    internal sealed class ConfigurationManagerAttributes
    {
        public int? Order;
    }
}
