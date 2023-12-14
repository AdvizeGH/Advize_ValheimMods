using BepInEx.Configuration;
using ServerSync;
using UnityEngine;

namespace Advize_Spyglass.Configuration
{
    class ModConfig
    {
        private readonly ConfigFile ConfigFile;
        private readonly ConfigSync ConfigSync;

        //General
        private readonly ConfigEntry<bool> serverConfigLocked;
        private readonly ConfigEntry<bool> enableLocalization;
        //Spyglass
        private readonly ConfigEntry<float> fovReductionFactor;
        private readonly ConfigEntry<float> zoomMultiplier;

        private readonly ConfigEntry<bool> enableZoomVignetteEffect;
        private readonly ConfigEntry<bool> vignetteRounded;
        private readonly ConfigEntry<float> vignetteIntensity;
        private readonly ConfigEntry<float> vignetteSmoothness;
        private readonly ConfigEntry<float> vignetteRoundness;
        private readonly ConfigEntry<Color> vignetteColor;
        private readonly ConfigEntry<Vector2> vignetteCenter;

        //Controls
        private readonly ConfigEntry<KeyboardShortcut> increaseZoomKey;
        private readonly ConfigEntry<KeyboardShortcut> decreaseZoomKey;
        private readonly ConfigEntry<KeyboardShortcut> removeZoomKey;
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
            enableLocalization = Config(
                "General",
                "EnableLocalization",
                false,
                "Enable this to attempt to load localized text.",
                false);
            fovReductionFactor = Config(
                "Spyglass",
                "FovReductionFactor",
                5f,
                "Influences field of view when zoomed, recommended range is 0 (disabled) to 5.");
            zoomMultiplier = Config(
                "Spyglass",
                "ZoomMultiplier",
                5f,
                "Increase/Decrease camera zoom distance.");
            enableZoomVignetteEffect = Config(
                "Spyglass",
                "EnableZoomVignetteEffect",
                true,
                "Enable vignette postprocessing effect when zooming with the spyglass.");
            vignetteRounded = Config(
                "Spyglass",
                "VignetteRounded",
                true,
                "Enable this to make the vignette perfectly round. When disabled, the vignette effect is dependent on the current aspect ratio.");
            vignetteIntensity = Config(
                "Spyglass",
                "VignetteIntensity",
                0.65f,
                "Set the amount of vignetting on screen.");
            vignetteSmoothness = Config(
                "Spyglass",
                "VignetteSmoothness",
                0.2f,
                "Set the smoothness of the vignette borders.");
            vignetteRoundness = Config(
                "Spyglass",
                "VignetteRoundness",
                1f,
                "Set the value to round the vignette. Lower values will make a more squared vignette.");
            vignetteColor = Config(
                "Spyglass",
                "VignetteColor",
                new Color(0, 0, 0, 1),
                "Set the color of the vignette.");
            vignetteCenter = Config(
                "Spyglass",
                "VignetteCenter",
                new Vector2(0.5f, 0.5f),
                "Set the vignette center point (screen center is [0.5, 0.5]).");
            increaseZoomKey = Config(
                "Controls",
                "IncreaseZoomKey",
                new KeyboardShortcut(KeyCode.Mouse1),
                new ConfigDescription(
                    "Keyboard shortcut to increase zoom level. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Keyboard shortcut to increase zoom level." }),
                false);
            decreaseZoomKey = Config(
                "Controls",
                "DecreaseZoomKey",
                new KeyboardShortcut(KeyCode.Mouse1, KeyCode.LeftShift),
                new ConfigDescription(
                    "Keyboard shortcut to decrease zoom level. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Keyboard shortcut to decrease zoom level." }),
                false);
            removeZoomKey = Config(
                "Controls",
                "RemoveZoomKey",
                new KeyboardShortcut(),
                new ConfigDescription(
                    "Optional keyboard shortcut to fully zoom out. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Optional keyboard shortcut to fully zoom out." }),
                false);
            enableDebugMessages = Config(
                "Troubleshooting",
                "EnableDebugMessages",
                false,
                "Enable mod debug messages in console.",
                false);

            configFile.Save();
            configFile.SaveOnConfigSet = true;

            configSync.AddLockingConfigEntry(serverConfigLocked);

            vignetteRounded.SettingChanged += Spyglass.VignetteSettingChanged;
            vignetteIntensity.SettingChanged += Spyglass.VignetteSettingChanged;
            vignetteSmoothness.SettingChanged += Spyglass.VignetteSettingChanged;
            vignetteRoundness.SettingChanged += Spyglass.VignetteSettingChanged;
            vignetteColor.SettingChanged += Spyglass.VignetteSettingChanged;
            vignetteCenter.SettingChanged += Spyglass.VignetteSettingChanged;
        }

        internal bool EnableLocalization => enableLocalization.Value;
        internal bool EnableDebugMessages => enableDebugMessages.Value;
        internal float FovReductionFactor => fovReductionFactor.Value;
        internal float ZoomMultiplier => zoomMultiplier.Value;
        internal bool EnableZoomVignetteEffect => enableZoomVignetteEffect.Value;
        internal bool VignetteRounded => vignetteRounded.Value;
        internal float VignetteIntensity => vignetteIntensity.Value;
        internal float VignetteSmoothness => vignetteSmoothness.Value;
        internal float VignetteRoundness => vignetteRoundness.Value;
        internal Color VignetteColor => vignetteColor.Value;
        internal Vector2 VignetteCenter => vignetteCenter.Value;
        internal KeyboardShortcut IncreaseZoomKey => increaseZoomKey.Value;
        internal KeyboardShortcut DecreaseZoomKey => decreaseZoomKey.Value;
        internal KeyboardShortcut RemoveZoomKey => removeZoomKey.Value;
#nullable enable
        internal class ConfigurationManagerAttributes
        {
            public string? Description;
        }
#nullable disable
    }
}
