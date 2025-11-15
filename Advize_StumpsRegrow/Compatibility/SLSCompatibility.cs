namespace Advize_StumpsRegrow;

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using static StumpsRegrow;

static class SLSCompatibility
{
    private const string GUID = "MidnightsFX.StarLevelSystem";

    private static bool _isInitialized;
    private static bool _enableTreeScaling = false;
    private static float _treeSizeScalePerLevel = 0f;

    internal static bool TreeScalingEnabled
    {
        get
        {
            if (!_isInitialized)
                Initialize();

            return _enableTreeScaling && _treeSizeScalePerLevel != 0;
        }
    }

    private static void Initialize()
    {
        _isInitialized = true;

        if (!Chainloader.PluginInfos.TryGetValue(GUID, out PluginInfo StarLevelSystem)) return;
        
        ConfigFile SLSConfig = StarLevelSystem.Instance.Config;

        if (SLSConfig.TryGetEntry(new("LevelSystem", "EnableTreeScaling"), out ConfigEntry<bool> enableTreeScaling))
        {
            _enableTreeScaling = enableTreeScaling.Value;

            enableTreeScaling.SettingChanged += (_, __) =>
            {
                Dbgl($"SLS changed EnableTreeScaling to {enableTreeScaling.Value}");
                _enableTreeScaling = enableTreeScaling.Value;
            };
        }

        if (SLSConfig.TryGetEntry(new("LevelSystem", "TreeSizeScalePerLevel"), out ConfigEntry<float> treeSizeScalePerLevel))
        {
            _treeSizeScalePerLevel = treeSizeScalePerLevel.Value;

            treeSizeScalePerLevel.SettingChanged += (_, __) =>
            {
                Dbgl($"SLS changed TreeSizeScalePerLevel to {treeSizeScalePerLevel.Value}");
                _treeSizeScalePerLevel = treeSizeScalePerLevel.Value;
            };
        }
    }
    internal static float GetScaleFactor(int level)
    {
        return 1f + _treeSizeScalePerLevel * level;
    }
}
