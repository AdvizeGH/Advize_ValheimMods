using BepInEx.Configuration;
using ServerSync;
using UnityEngine;

namespace Advize_CartographySkill.Configuration
{
    class ModConfig
    {
        private ConfigFile Config;
        private ConfigSync ConfigSync;

        //General
        private readonly ConfigEntry<bool> serverConfigLocked;
        private readonly ConfigEntry<float> exploreRadiusIncrease;
        private readonly ConfigEntry<float> baseExploreRadius;
        private readonly ConfigEntry<int> nexusID;
        //Progression
        private readonly ConfigEntry<bool> enableSkill;
        private readonly ConfigEntry<float> skillIncrease;
        private readonly ConfigEntry<int> tilesDiscoveredForXPGain;
        //Spyglass
        private readonly ConfigEntry<bool> enableSpyglass;
        private readonly ConfigEntry<float> fovReductionFactor;
        private readonly ConfigEntry<float> zoomMultiplier;
        //Controls
        private readonly ConfigEntry<KeyboardShortcut> increaseZoomKey;
        private readonly ConfigEntry<KeyboardShortcut> decreaseZoomModifierKey;
        private readonly ConfigEntry<KeyboardShortcut> removeZoomKey;
        //Troubleshooting
        private readonly ConfigEntry<bool> enableDebugMessages;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        internal ModConfig(ConfigFile configFile, ConfigSync configSync)
        {
            Config = configFile; ConfigSync = configSync;

            serverConfigLocked = config(
                "General",
                "Lock Configuration",
                false,
                "If on, the configuration is locked and can be changed by server admins only.");
            nexusID = config(
                "General",
                "NexusID",
                394,
                "Nexus mod ID for updates.",
                false);
            exploreRadiusIncrease = config(
                "General",
                "RadiusIncreasePerLevel",
                1f,
                "Amount to increase base explore radius by per skill level");
            baseExploreRadius = config(
                "General",
                "BaseExploreRadius",
                100f,
                "BaseExploreRadius (Vanilla value is 100)");
            enableSkill = config(
                "Progression",
                "EnableSkill",
                true,
                "Enables the cartography skill",
                false);
            skillIncrease = config(
                "Progression",
                "LevelingIncrement",
                0.5f,
                "Experience gain when cartography skill XP is awarded");
            tilesDiscoveredForXPGain = config(
                "Progression",
                "TileDiscoveryRequirement",
                100,
                "Amount of map tiles that need to be discovered before XP is awarded (influences BetterUI xp gain spam)");
            enableSpyglass = config(
                "Spyglass",
                "EnableSpyglass",
                true,
                "Enables the spyglass item",
                false);
            fovReductionFactor = config(
                "Spyglass",
                "FovReductionFactor",
                5f,
                "Influences field of view when zoomed, recommended range is 0 (disabled) to 5");
            zoomMultiplier = config(
                "Spyglass",
                "ZoomMultiplier",
                5f,
                "Increase/Decrease camera zoom distance");
            increaseZoomKey = config(
                "Controls",
                "IncreaseZoomKey",
                new KeyboardShortcut(KeyCode.Mouse1),
                "Key to increase zoom level. See https://docs.unity3d.com/Manual/class-InputManager.html",
                false);
            decreaseZoomModifierKey = config(
                "Controls",
                "DecreaseZoomModifierKey",
                new KeyboardShortcut(KeyCode.LeftShift),
                "Hold this key while pressing IncreaseZoomKey to decrease zoom level. See https://docs.unity3d.com/Manual/class-InputManager.html", false);
            removeZoomKey = config(
                "Controls",
                "RemoveZoomKey",
                new KeyboardShortcut(),
                "Optional key to fully zoom out. See https://docs.unity3d.com/Manual/class-InputManager.html", false);
            enableDebugMessages = config(
                "Troubleshooting",
                "EnableDebugMessages",
                false,
                "Enable mod debug messages in console", false);

            configSync.AddLockingConfigEntry(serverConfigLocked);
        }

        internal bool EnableSkill
        {
            get { return enableSkill.Value; }
        }
        internal float SkillIncrease
        {
            get { return skillIncrease.Value; }
        }
        internal int TilesDiscoveredForXPGain
        {
            get { return tilesDiscoveredForXPGain.Value; }
        }
        internal float ExploreRadiusIncrease
        {
            get { return exploreRadiusIncrease.Value; }
        }
        internal float BaseExploreRadius
        {
            get { return baseExploreRadius.Value; }
        }
        internal bool EnableDebugMessages
        {
            get { return enableDebugMessages.Value; }
        }
        internal bool EnableSpyglass
        {
            get { return enableSpyglass.Value; }
        }
        internal float FovReductionFactor
        {
            get { return fovReductionFactor.Value; }
        }
        internal float ZoomMultiplier
        {
            get { return zoomMultiplier.Value; }
        }
        internal KeyCode IncreaseZoomKey
        {
            get { return increaseZoomKey.Value.MainKey; }
        }
        internal KeyCode DecreaseZoomModifierKey
        {
            get { return decreaseZoomModifierKey.Value.MainKey; }
        }
        internal KeyCode RemoveZoomKey
        {
            get { return removeZoomKey.Value.MainKey; }
        }
    }
}
