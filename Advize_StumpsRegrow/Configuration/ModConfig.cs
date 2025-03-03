namespace Advize_StumpsRegrow;

using BepInEx.Configuration;
using ServerSync;

sealed class ModConfig
{
    private readonly ConfigFile ConfigFile;
    private readonly ConfigSync ConfigSync;

    //[Server]
    private readonly ConfigEntry<bool> lockConfiguration;
    //[General]
    private readonly ConfigEntry<float> stumpGrowthTime;
    //[UI]
    private readonly ConfigEntry<bool> enableStumpTimers; // local
    private readonly ConfigEntry<bool> growthAsPercentage; // local

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

        //[Server]
        lockConfiguration = Config(
            "Server",
            "LockConfiguration",
            true,
            "If on, the mod configuration is locked and can only be changed by server admins.");
        //[General]
        stumpGrowthTime = Config(
            "General",
            "StumpGrowthTime",
            3000f,
            "Number of seconds it takes for a stump to regrow into the tree that spawned it (will take at least 10 seconds after spawning to grow). Default is 3000 seconds (50 minutes).");
        //[UI]
        enableStumpTimers = Config(
            "UI",
            "EnableStumpTimers",
            true,
            "Enables hover text display of growth time remaining on stumps.",
            false);
        growthAsPercentage = Config(
            "UI",
            "GrowthAsPercentage",
            false,
            "Enables display of growth time as a percentage instead of time remaining.",
            false);

        configSync.AddLockingConfigEntry(lockConfiguration);
    }

    internal float StumpGrowthTime => stumpGrowthTime.Value;
    internal bool EnableStumpTimers => enableStumpTimers.Value;
    internal bool GrowthAsPercentage => growthAsPercentage.Value;
}
