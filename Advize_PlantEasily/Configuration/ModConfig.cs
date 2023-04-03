using BepInEx.Configuration;
using UnityEngine;

namespace Advize_PlantEasily.Configuration
{
    class ModConfig
    {
        private ConfigFile Config;

        private readonly ConfigEntry<bool> enableDebugMessages;

        private readonly ConfigEntry<int> rows;
        private readonly ConfigEntry<int> columns;
        private readonly ConfigEntry<bool> modActive;
        private readonly ConfigEntry<bool> snapActive;

        //Pickables
        private readonly ConfigEntry<float> pickableSnapRadius;
        private readonly ConfigEntry<float> berryBushSnapRadius;
        private readonly ConfigEntry<float> mushroomSnapRadius;
        private readonly ConfigEntry<float> flowerSnapRadius;

        private readonly ConfigEntry<bool> preventPartialPlanting;
        private readonly ConfigEntry<bool> preventInvalidPlanting;
        private readonly ConfigEntry<bool> randomizeRotation;
        //private readonly ConfigEntry<bool> useStamina; // not yet implemented
        private readonly ConfigEntry<bool> useDurability;

        //Controls
        private readonly ConfigEntry<KeyboardShortcut> enableModKey;
        private readonly ConfigEntry<KeyboardShortcut> enableSnappingKey;

        private readonly ConfigEntry<KeyboardShortcut> increaseXKey;
        private readonly ConfigEntry<KeyboardShortcut> increaseYKey;
        private readonly ConfigEntry<KeyboardShortcut> decreaseXKey;
        private readonly ConfigEntry<KeyboardShortcut> decreaseYKey;

        private readonly ConfigEntry<KeyboardShortcut> keyboardModifierKey;
        private readonly ConfigEntry<KeyboardShortcut> gamepadModifierKey;

        internal ModConfig(ConfigFile configFile)
        {
            Config = configFile;

            //General
            enableDebugMessages = Config.Bind("General", "EnableDebugMessages", false, "Enable mod debug messages in console.");
            rows = Config.Bind("General", "Rows", 2, "Number of rows for planting grid aka height.");
            columns = Config.Bind("General", "Columns", 2, "Number of columns for planting grid aka width.");
            modActive = Config.Bind("General", "ModActive", true, "Enables all mod features.");
            snapActive = Config.Bind("General", "SnapActive", true, "Enables grid snapping feature.");
            preventPartialPlanting = Config.Bind("General", "PreventPartialPlanting", true, "Prevents placement of resources when any placement ghosts are invalid for any reason.");
            preventInvalidPlanting = Config.Bind("General", "PreventInvalidPlanting", true, "Prevents plants from being placed where they will be unable to grow.");
            randomizeRotation = Config.Bind("General", "RandomizeRotation", true, "Randomizes rotation of pieces once placed.");
            //useStamina = Config.Bind("General", "UseStamina", true, "PLACEHOLDER");
            useDurability = Config.Bind("General", "UseDurability", true, "Decrease durability of cultivator for every piece placed.");

            //Pickables
            pickableSnapRadius = Config.Bind("Pickables", "PickableSnapRadius", 1.0f, "Determines default distance/spacing between pickable resources when planting.");
            berryBushSnapRadius = Config.Bind("Pickables", "BerryBushSnapRadius", 1.5f, "Determines distance/spacing between berry bushes when planting.");
            mushroomSnapRadius = Config.Bind("Pickables", "MushroomSnapRadius", 0.5f, "Determines distance/spacing between mushrooms when planting.");
            flowerSnapRadius = Config.Bind("Pickables", "FlowerSnapRadius", 1.0f, "Determines distance/spacing between flowers when planting.");

            //Controls
            enableModKey = Config.Bind("Controls", "EnableModKey", new KeyboardShortcut(KeyCode.F8),
                new ConfigDescription(
                    "Key to toggle on/off all mod features. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Key to toggle on/off all mod features." }));
            enableSnappingKey = Config.Bind("Controls", "EnableSnappingKey", new KeyboardShortcut(KeyCode.F10),
                new ConfigDescription(
                    "Key to toggle on/off piece snapping functionality. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Key to toggle on/off piece snapping functionality." }));
            increaseXKey = Config.Bind("Controls", "IncreaseXKey", new KeyboardShortcut(KeyCode.RightArrow),
                new ConfigDescription(
                    "Key to increase number of grid columns. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Key to increase number of grid columns." }));
            increaseYKey = Config.Bind("Controls", "IncreaseYKey", new KeyboardShortcut(KeyCode.UpArrow),
                new ConfigDescription(
                    "Key to increase number of grid rows. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Key to increase number of grid rows." }));
            decreaseXKey = Config.Bind("Controls", "DecreaseXKey", new KeyboardShortcut(KeyCode.LeftArrow),
                new ConfigDescription(
                    "Key to decrease number of grid columns. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Key to decrease number of grid columns." }));
            decreaseYKey = Config.Bind("Controls", "DecreaseYKey", new KeyboardShortcut(KeyCode.DownArrow),
                new ConfigDescription(
                    "Key to decrease number of grid rows. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Key to decrease number of grid rows." }));
            keyboardModifierKey = Config.Bind("Controls", "KeyboardModifierKey", new KeyboardShortcut(KeyCode.RightControl),
                new ConfigDescription(
                    "Modifier key when using keyboard controls. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Modifier key when using keyboard controls." }));
            gamepadModifierKey = Config.Bind("Controls", "GamepadModifierKey", new KeyboardShortcut(KeyCode.JoystickButton4),
                new ConfigDescription(
                    "Modifier key when using gamepad controls. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new ConfigurationManagerAttributes { Description = "Modifier key when using gamepad controls." }));

            rows.SettingChanged += PlantEasily.GridSizeChanged;
            columns.SettingChanged += PlantEasily.GridSizeChanged;
        }

        internal bool EnableDebugMessages
        {
            get { return enableDebugMessages.Value; }
        }
        internal int Rows
        {
            get { return Mathf.Max(rows.Value, 1); }
            set { rows.BoxedValue = Mathf.Max(value, 1); }
        }
        internal int Columns
        {
            get { return Mathf.Max(columns.Value, 1); }
            set { columns.BoxedValue = Mathf.Max(value, 1); }
        }
        internal bool ModActive
        {
            get { return modActive.Value; }
            set { modActive.BoxedValue = value; }
        }
        internal bool SnapActive
        {
            get { return snapActive.Value; }
            set { snapActive.BoxedValue = value; }
        }
        internal float PickableSnapRadius
        {
            get { return pickableSnapRadius.Value; }
        }
        internal float BerryBushSnapRadius
        {
            get { return berryBushSnapRadius.Value; }
        }
        internal float MushroomSnapRadius
        {
            get { return mushroomSnapRadius.Value; }
        }
        internal float FlowerSnapRadius
        {
            get { return flowerSnapRadius.Value; }
        }
        internal bool PreventPartialPlanting
        {
            get { return preventPartialPlanting.Value; }
        }
        internal bool PreventInvalidPlanting
        {
            get { return preventInvalidPlanting.Value; }
        }
        internal bool RandomizeRotation
        {
            get { return randomizeRotation.Value; }
        }
        //internal bool UseStamina
        //{
        //    get { return useStamina.Value; }
        //}
        internal bool UseDurability
        {
            get { return useDurability.Value; }
        }
        internal KeyCode EnableModKey
        {
            get { return enableModKey.Value.MainKey; }
        }
        internal KeyCode EnableSnappingKey
        {
            get { return enableSnappingKey.Value.MainKey; }
        }
        internal KeyCode IncreaseXKey
        {
            get { return increaseXKey.Value.MainKey; }
        }
        internal KeyCode IncreaseYKey
        {
            get { return increaseYKey.Value.MainKey; }
        }
        internal KeyCode DecreaseXKey
        {
            get { return decreaseXKey.Value.MainKey; }
        }
        internal KeyCode DecreaseYKey
        {
            get { return decreaseYKey.Value.MainKey; }
        }
        internal KeyCode KeyboardModifierKey
        {
            get { return keyboardModifierKey.Value.MainKey; }
        }
        internal KeyCode GamepadModifierKey
        {
            get { return gamepadModifierKey.Value.MainKey; }
        }
    }
}
