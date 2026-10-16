using BepInEx.Configuration;
using HexChestHacks.Core.Services;
using System;
using UnityEngine;

namespace HexChestHacks.Core
{
    internal static class BepInExConfigurationSetup
    {
        private static int _configurationRevision;
        private static int _lastWarningRevision = -1;

        internal static ConfigEntry<bool> EnableCustomChestCapacity;
        internal static ConfigEntry<int> Rows;
        internal static ConfigEntry<int> Columns;

        internal static ConfigEntry<bool> EnableCustomWardrobeCapacity;
        internal static ConfigEntry<int> WardrobeRows;
        internal static ConfigEntry<int> WardrobeColumns;

        internal static ConfigEntry<bool> RemoveChestScrollBars;

        internal static void Init(ConfigFile config)
        {
            EnableCustomChestCapacity = config.Bind(
                "Standard Chest Inventory",
                "EnableCustomChestCapacity",
                false,
                new ConfigDescription(
                    "Enable custom chest capacity.",
                    null,
                    new ConfigurationManagerAttributes { Order = 4 }));

            Columns = config.Bind(
                "Standard Chest Inventory",
                "Columns",
                5,
                new ConfigDescription(
                    "Number of chest inventory columns.",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes
                    {
                        Order = 3,
                        HideDefaultButton = true
                    }));

            Rows = config.Bind(
                "Standard Chest Inventory",
                "Rows",
                2,
                new ConfigDescription(
                    "Number of chest inventory rows.",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes
                    {
                        Order = 2,
                        HideDefaultButton = true
                    }));

            EnableCustomWardrobeCapacity = config.Bind(
                "Wardrobe Inventory",
                "EnableCustomWardrobeCapacity",
                false,
                new ConfigDescription(
                    "Enable custom wardrobe capacity.",
                    null,
                    new ConfigurationManagerAttributes { Order = 7 }));

            WardrobeRows = config.Bind(
                "Wardrobe Inventory",
                "WardrobeRows",
                10,
                new ConfigDescription(
                    "Number of wardrobe inventory rows.",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes
                    {
                        Order = 5,
                        HideDefaultButton = true
                    }));

            WardrobeColumns = config.Bind(
                "Wardrobe Inventory",
                "WardrobeColumns",
                5,
                new ConfigDescription(
                    "Number of wardrobe inventory columns.",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes
                    {
                        Order = 6,
                        HideDefaultButton = true
                    }));

            RemoveChestScrollBars = config.Bind(
                "Chest Inventory",
                "RemoveChestScrollBars",
                false,
                new ConfigDescription(
                    "Remove scrollbars from all supported chest and wardrobe inventories.",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        Order = 2,
                        HideDefaultButton = true
                    }));

            config.Bind(
                "Chest Inventory",
                "RestoreVanillaChestSizes",
                false,
                new ConfigDescription(
                    "Restore supported chests and wardrobes to their original vanilla dimensions.",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        Order = 1,
                        HideDefaultButton = true,
                        CustomDrawer = _ =>
                        {
                            if (GUILayout.Button("Restore Vanilla Chest Sizes"))
                            {
                                RestoreVanillaChestSizes();
                            }
                        }
                    }));

            EnableCustomChestCapacity.SettingChanged += ConfigChange;
            Rows.SettingChanged += ConfigChange;
            Columns.SettingChanged += ConfigChange;

            EnableCustomWardrobeCapacity.SettingChanged += ConfigChange;
            WardrobeRows.SettingChanged += ConfigChange;
            WardrobeColumns.SettingChanged += ConfigChange;
        }

        internal static void Dispose()
        {
            EnableCustomChestCapacity.SettingChanged -= ConfigChange;
            Rows.SettingChanged -= ConfigChange;
            Columns.SettingChanged -= ConfigChange;

            EnableCustomWardrobeCapacity.SettingChanged -= ConfigChange;
            WardrobeRows.SettingChanged -= ConfigChange;
            WardrobeColumns.SettingChanged -= ConfigChange;
        }

        private static void ConfigChange(object sender, EventArgs e)
        {
            _configurationRevision++;

            Plugin.Log.LogInfo(
                $"Chest capacity configuration changed. " +
                $"Chest enabled: {EnableCustomChestCapacity.Value}, " +
                $"Rows: {Rows.Value}, Columns: {Columns.Value}, " +
                $"Wardrobe enabled: {EnableCustomWardrobeCapacity.Value}, " +
                $"WardrobeRows: {WardrobeRows.Value}, WardrobeColumns: {WardrobeColumns.Value}");

            LoadedContainerService.ApplyConfiguration();
        }

        private static void RestoreVanillaChestSizes()
        {
            var chestEnabled = EnableCustomChestCapacity.Value;
            var wardrobeEnabled = EnableCustomWardrobeCapacity.Value;

            if (chestEnabled)
            {
                EnableCustomChestCapacity.Value = false;
            }

            if (wardrobeEnabled)
            {
                EnableCustomWardrobeCapacity.Value = false;
            }

            if (!chestEnabled && !wardrobeEnabled)
            {
                _configurationRevision++;
                LoadedContainerService.ApplyConfiguration();
            }

            Plugin.Log.LogInfo("Vanilla chest and wardrobe capacity restoration requested.");
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
        public bool? HideDefaultButton;
        public Action<ConfigEntryBase> CustomDrawer;
    }
}
