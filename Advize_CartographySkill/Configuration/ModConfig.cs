namespace Advize_CartographySkill;

using BepInEx.Configuration;
using ServerSync;

class ModConfig
{
    private readonly ConfigFile ConfigFile;
    private readonly ConfigSync ConfigSync;

    //General
    private readonly ConfigEntry<bool> serverConfigLocked;
    private readonly ConfigEntry<float> exploreRadiusIncrease;
    private readonly ConfigEntry<float> baseExploreRadius;
    //Progression
    private readonly ConfigEntry<bool> enableSkill;
    private readonly ConfigEntry<string> skillName;
    private readonly ConfigEntry<string> skillDescription;
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
        configFile.SaveOnConfigSet = false;

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
        enableSkill = Config(
            "Progression",
            "EnableSkill",
            true,
            "Enables the cartography skill. Disable if you only want to increase base explore radius. !!Warning!! This will reset skill level to 0.");
        skillName = Config(
            "Progression",
            "SkillName",
            "Cartography",
            "Sets the name of the skill in the skill window (requires world reload or language change to take effect).",
            false);
        skillDescription = Config(
            "Progression",
            "SkillDescription",
            "Increases map explore radius.",
            "Sets the description of the skill in the skill window (requires world reload or language change to take effect).",
            false);
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

        skillName.SettingChanged += CartographySkill.UpdateLocalization;
        skillDescription.SettingChanged += CartographySkill.UpdateLocalization;

        configSync.AddLockingConfigEntry(serverConfigLocked);
        configFile.Save();
        configFile.SaveOnConfigSet = true;
    }

    internal bool EnableSkill => enableSkill.Value;
    internal float SkillIncrease => skillIncrease.Value;
    internal int TilesDiscoveredForXPGain => tilesDiscoveredForXPGain.Value;
    internal float ExploreRadiusIncrease => exploreRadiusIncrease.Value;
    internal float BaseExploreRadius => baseExploreRadius.Value;
    internal string SkillName => skillName.Value;
    internal string SkillDescription => skillDescription.Value;
    internal bool EnableDebugMessages => enableDebugMessages.Value;
}
