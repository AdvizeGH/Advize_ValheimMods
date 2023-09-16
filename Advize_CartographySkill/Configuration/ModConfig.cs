using BepInEx.Configuration;
using ServerSync;

namespace Advize_CartographySkill.Configuration
{
    class ModConfig
    {
        private readonly ConfigFile ConfigFile;
        private readonly ConfigSync ConfigSync;

        //General
        private readonly ConfigEntry<bool> serverConfigLocked;
        private readonly ConfigEntry<float> exploreRadiusIncrease;
        private readonly ConfigEntry<float> baseExploreRadius;
        private readonly ConfigEntry<bool> enableLocalization;
        //Progression
        private readonly ConfigEntry<bool> enableSkill;
        private readonly ConfigEntry<float> skillIncrease;
        private readonly ConfigEntry<int> tilesDiscoveredForXPGain;
        //Troubleshooting
        private readonly ConfigEntry<bool> enableDebugMessages;

        private ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = ConfigFile.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> Config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => Config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        internal ModConfig(ConfigFile configFile, ConfigSync configSync)
        {
            ConfigFile = configFile; ConfigSync = configSync;

            serverConfigLocked = Config(
                "General",
                "Lock Configuration",
                true,
                "If on, the configuration is locked and can be changed by server admins only.");
            exploreRadiusIncrease = Config(
                "General",
                "RadiusIncreasePerLevel",
                1f,
                "Amount to increase base explore radius by per skill level.");
            baseExploreRadius = Config(
                "General",
                "BaseExploreRadius",
                100f,
                "BaseExploreRadius (Vanilla value is 100).");
            enableLocalization = Config(
                "General",
                "EnableLocalization",
                false,
                "Enable this to attempt to load localized text.",
                false);
            enableSkill = Config(
                "Progression",
                "EnableSkill",
                true,
                "Enables the cartography skill. Disable if you only want to increase base explore radius. !!Warning!! This will reset skill level to 0.");
            skillIncrease = Config(
                "Progression",
                "LevelingIncrement",
                0.5f,
                "Experience gain when cartography skill XP is awarded.");
            tilesDiscoveredForXPGain = Config(
                "Progression",
                "TileDiscoveryRequirement",
                100,
                "Amount of map tiles that need to be discovered before XP is awarded (influences BetterUI xp gain spam).");
            enableDebugMessages = Config(
                "Troubleshooting",
                "EnableDebugMessages",
                false,
                "Enable mod debug messages in console.", false);

            enableSkill.SettingChanged += (_, _) =>
            {
                if (!EnableSkill)
                {
                    Minimap.instance.m_exploreRadius = BaseExploreRadius;
                    CartographySkill.Dbgl($"enableSkill set to false. Explore Radius is now: {BaseExploreRadius}");
                }
            };

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
        internal bool EnableLocalization
        {
            get { return enableLocalization.Value; }
        }
        internal bool EnableDebugMessages
        {
            get { return enableDebugMessages.Value; }
        }
    }
}
