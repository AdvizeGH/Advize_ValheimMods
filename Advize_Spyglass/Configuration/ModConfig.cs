using BepInEx.Configuration;
//using ServerSync;
using UnityEngine;

namespace Advize_Spyglass.Configuration
{
    class ModConfig
    {
        private readonly ConfigFile ConfigFile;
        //private readonly ConfigSync ConfigSync;

        //General
        //private readonly ConfigEntry<bool> serverConfigLocked;
        private readonly ConfigEntry<bool> enableLocalization;
        //Spyglass
        private readonly ConfigEntry<float> fovReductionFactor;
        private readonly ConfigEntry<float> zoomMultiplier;
        //Controls
        private readonly ConfigEntry<KeyboardShortcut> increaseZoomKey;
        private readonly ConfigEntry<KeyboardShortcut> decreaseZoomModifierKey;
        private readonly ConfigEntry<KeyboardShortcut> removeZoomKey;
        //Troubleshooting
        private readonly ConfigEntry<bool> enableDebugMessages;

        private ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = ConfigFile.Bind(group, name, value, description);

            //SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            //syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> Config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => Config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        internal ModConfig(ConfigFile configFile/*, ConfigSync configSync*/)
        {
            ConfigFile = configFile;/* ConfigSync = configSync;*/

            //serverConfigLocked = Config(
            //    "General",
            //    "Lock Configuration",
            //    false,
            //    "If on, the configuration is locked and can be changed by server admins only.");
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
            increaseZoomKey = Config(
                "Controls",
                "IncreaseZoomKey",
                new KeyboardShortcut(KeyCode.Mouse1),
                new ConfigDescription(
                    "Keyboard shortcut to increase zoom level. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Keyboard shortcut to increase zoom level." }),
                false);
            decreaseZoomModifierKey = Config(
                "Controls",
                "DecreaseZoomModifierKey",
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
                "Enable mod debug messages in console.", false);

            //configSync.AddLockingConfigEntry(serverConfigLocked);
        }

        internal bool EnableLocalization
        {
            get { return enableLocalization.Value; }
        }
        internal bool EnableDebugMessages
        {
            get { return enableDebugMessages.Value; }
        }
        internal float FovReductionFactor
        {
            get { return fovReductionFactor.Value; }
        }
        internal float ZoomMultiplier
        {
            get { return zoomMultiplier.Value; }
        }
        internal KeyboardShortcut IncreaseZoomKey
        {
            get { return increaseZoomKey.Value; }
        }
        internal KeyboardShortcut DecreaseZoomModifierKey
        {
            get { return decreaseZoomModifierKey.Value; }
        }
        internal KeyboardShortcut RemoveZoomKey
        {
            get { return removeZoomKey.Value; }
        }
#nullable enable
        internal class ConfigurationManagerAttributes
        {
            public string? Description;
        }
#nullable disable
    }
}
