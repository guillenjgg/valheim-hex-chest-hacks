using BepInEx.Configuration;
using HexChestHacks.Core.Services;
using System;
using UnityEngine;

namespace HexChestHacks.Core
{
    internal static class HexChestHacksConfigurationSetup
    {
        private const string GeneralSection = "1 - General";
        private const string ChestSection = "2 - Standard Chest Inventory";
        private const string WardrobeSection = "3 - Wardrobe Inventory";

        internal static ConfigEntry<bool> EnableCustomChestCapacity;
        internal static ConfigEntry<bool> EnableCustomWardrobeCapacity;
        internal static ConfigEntry<bool> RemoveChestScrollBars;
        internal static ConfigEntry<bool> EnableTopFirstChestPlacement;

        internal static ConfigEntry<int> Columns;
        internal static ConfigEntry<int> Rows;
        internal static ConfigEntry<int> WardrobeColumns;
        internal static ConfigEntry<int> WardrobeRows;

        internal static void Init(ConfigFile config)
        {
            var oldChestEnabled = ReadOldValue(config, "Standard Chest Inventory", "EnableCustomChestCapacity", false);
            var oldChestColumns = ReadOldValue(config, "Standard Chest Inventory", "Columns", 5);
            var oldChestRows = ReadOldValue(config, "Standard Chest Inventory", "Rows", 6);

            var oldWardrobeEnabled = ReadOldValue(config, "Wardrobe Inventory", "EnableCustomWardrobeCapacity", false);
            var oldWardrobeColumns = ReadOldValue(config, "Wardrobe Inventory", "WardrobeColumns", 5);
            var oldWardrobeRows = ReadOldValue(config, "Wardrobe Inventory", "WardrobeRows", 10);

            var oldScrollBars = ReadOldValue(config, "Chest Inventory", "RemoveChestScrollBars", false);
            var oldTopFirst = ReadOldValue(config, "Chest Inventory", "EnableTopFirstChestPlacement", true);

            EnableCustomChestCapacity = config.Bind(
                GeneralSection,
                "EnableCustomChestCapacity",
                oldChestEnabled,
                new ConfigDescription(
                    "Enable custom chest capacity.",
                    null,
                    new ConfigurationManagerAttributes { Order = 5 }));

            EnableCustomWardrobeCapacity = config.Bind(
                GeneralSection,
                "EnableCustomWardrobeCapacity",
                oldWardrobeEnabled,
                new ConfigDescription(
                    "Enable custom wardrobe capacity.",
                    null,
                    new ConfigurationManagerAttributes { Order = 4 }));

            RemoveChestScrollBars = config.Bind(
                GeneralSection,
                "RemoveChestScrollBars",
                oldScrollBars,
                new ConfigDescription(
                    "Remove scrollbars from supported chest and wardrobe inventories.",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        Order = 3,
                        HideDefaultButton = true
                    }));

            EnableTopFirstChestPlacement = config.Bind(
                GeneralSection,
                "EnableTopFirstChestPlacement",
                oldTopFirst,
                new ConfigDescription(
                    "Insert items at the top of chest inventories instead of the bottom.",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        Order = 2,
                        HideDefaultButton = true
                    }));

            config.Bind(
                GeneralSection,
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

            Columns = config.Bind(
                ChestSection,
                "Columns",
                oldChestColumns,
                new ConfigDescription(
                    "Number of chest inventory columns.",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes
                    {
                        Order = 2,
                        HideDefaultButton = true
                    }));

            Rows = config.Bind(
                ChestSection,
                "Rows",
                oldChestRows,
                new ConfigDescription(
                    "Number of chest inventory rows.",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes
                    {
                        Order = 1,
                        HideDefaultButton = true
                    }));

            WardrobeColumns = config.Bind(
                WardrobeSection,
                "WardrobeColumns",
                oldWardrobeColumns,
                new ConfigDescription(
                    "Number of wardrobe inventory columns.",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes
                    {
                        Order = 2,
                        HideDefaultButton = true
                    }));

            WardrobeRows = config.Bind(
                WardrobeSection,
                "WardrobeRows",
                oldWardrobeRows,
                new ConfigDescription(
                    "Number of wardrobe inventory rows.",
                    new AcceptableValueRange<int>(1, 10),
                    new ConfigurationManagerAttributes
                    {
                        Order = 1,
                        HideDefaultButton = true
                    }));

            RemoveOldEntries(config);
            config.Save();

            EnableCustomChestCapacity.SettingChanged += ConfigChange;
            EnableCustomWardrobeCapacity.SettingChanged += ConfigChange;
            Columns.SettingChanged += ConfigChange;
            Rows.SettingChanged += ConfigChange;
            WardrobeColumns.SettingChanged += ConfigChange;
            WardrobeRows.SettingChanged += ConfigChange;
        }

        internal static void Dispose()
        {
            EnableCustomChestCapacity.SettingChanged -= ConfigChange;
            EnableCustomWardrobeCapacity.SettingChanged -= ConfigChange;
            Columns.SettingChanged -= ConfigChange;
            Rows.SettingChanged -= ConfigChange;
            WardrobeColumns.SettingChanged -= ConfigChange;
            WardrobeRows.SettingChanged -= ConfigChange;
        }

        private static T ReadOldValue<T>(ConfigFile config, string section, string key, T defaultValue)
        {
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                return existingEntry.Value;
            }

            return config.Bind(section, key, defaultValue).Value;
        }

        private static void RemoveOldEntries(ConfigFile config)
        {
            RemoveOldEntry(config, "Standard Chest Inventory", "EnableCustomChestCapacity");
            RemoveOldEntry(config, "Standard Chest Inventory", "Columns");
            RemoveOldEntry(config, "Standard Chest Inventory", "Rows");

            RemoveOldEntry(config, "Wardrobe Inventory", "EnableCustomWardrobeCapacity");
            RemoveOldEntry(config, "Wardrobe Inventory", "WardrobeColumns");
            RemoveOldEntry(config, "Wardrobe Inventory", "WardrobeRows");

            RemoveOldEntry(config, "Chest Inventory", "RemoveChestScrollBars");
            RemoveOldEntry(config, "Chest Inventory", "EnableTopFirstChestPlacement");
            RemoveOldEntry(config, "Chest Inventory", "RestoreVanillaChestSizes");
        }

        private static void RemoveOldEntry(ConfigFile config, string section, string key)
        {
            config.Remove(new ConfigDefinition(section, key));
        }

        private static void ConfigChange(object sender, EventArgs e)
        {
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
                LoadedContainerService.ApplyConfiguration();
            }
        }
    }

    internal sealed class ConfigurationManagerAttributes
    {
        public int? Order;
        public bool? HideDefaultButton;
        public Action<ConfigEntryBase> CustomDrawer;
    }
}