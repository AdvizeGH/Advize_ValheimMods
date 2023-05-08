using BepInEx.Configuration;
using UnityEngine;
using Attributes = Advize_PlantEasily.Configuration.ModConfig.ConfigurationManagerAttributes;

namespace Advize_PlantEasily.Configuration
{
    class ModConfig
    {
        private ConfigFile Config;

        //General
        private readonly ConfigEntry<bool> enableDebugMessages;

        private readonly ConfigEntry<int> rows;
        private readonly ConfigEntry<int> columns;
        private readonly ConfigEntry<bool> modActive;
        private readonly ConfigEntry<bool> snapActive;

        private readonly ConfigEntry<bool> preventPartialPlanting;
        private readonly ConfigEntry<bool> preventInvalidPlanting;
        private readonly ConfigEntry<bool> randomizeRotation;
        private readonly ConfigEntry<bool> useStamina;
        private readonly ConfigEntry<bool> useDurability;
        private readonly ConfigEntry<float> extraCropSpacing;
        private readonly ConfigEntry<float> extraSaplingSpacing;
        private readonly ConfigEntry<GridSnappingStyle> gridSnappingStyle;
        private readonly ConfigEntry<bool> standardizeGridRotations;

        //Pickables
        private readonly ConfigEntry<float> pickableSnapRadius;
        private readonly ConfigEntry<float> berryBushSnapRadius;
        private readonly ConfigEntry<float> mushroomSnapRadius;
        private readonly ConfigEntry<float> flowerSnapRadius;
        private readonly ConfigEntry<bool> preventOverlappingPlacements;
        
        //Harvesting
        private readonly ConfigEntry<bool> enableBulkHarvest;
        private readonly ConfigEntry<HarvestStyle> harvestStyle;
        private readonly ConfigEntry<float> harvestRadius;
        private readonly ConfigEntry<bool> replantOnHarvest;

        //Controls
        private readonly ConfigEntry<KeyboardShortcut> enableModKey;
        private readonly ConfigEntry<KeyboardShortcut> enableSnappingKey;

        private readonly ConfigEntry<KeyboardShortcut> increaseXKey;
        private readonly ConfigEntry<KeyboardShortcut> increaseYKey;
        private readonly ConfigEntry<KeyboardShortcut> decreaseXKey;
        private readonly ConfigEntry<KeyboardShortcut> decreaseYKey;

        private readonly ConfigEntry<KeyboardShortcut> keyboardModifierKey;
        private readonly ConfigEntry<KeyboardShortcut> gamepadModifierKey;
        private readonly ConfigEntry<KeyboardShortcut> keyboardHarvestModifierKey;

        //UI
        private readonly ConfigEntry<bool> showCost;
        private readonly ConfigEntry<CostDisplayStyle> costDisplayStyle;
        private readonly ConfigEntry<CostDisplayLocation> costDisplayLocation;

        internal ModConfig(ConfigFile configFile)
        {
            Config = configFile;

            //General
            enableDebugMessages = Config.Bind("General", "EnableDebugMessages", false, "Enable mod debug messages in console.");
            rows = Config.Bind("General", "Rows", 2, new ConfigDescription("Number of rows for planting grid aka height.", null, new Attributes { Order = 8 }));
            columns = Config.Bind("General", "Columns", 2, new ConfigDescription("Number of columns for planting grid aka width.", null, new Attributes { Order = 7 }));
            modActive = Config.Bind("General", "ModActive", true, new ConfigDescription("Enables all mod features.", null, new Attributes { Order = 10 }));
            snapActive = Config.Bind("General", "SnapActive", true, new ConfigDescription("Enables grid snapping feature.", null, new Attributes { Order = 9 }));
            preventPartialPlanting = Config.Bind("General", "PreventPartialPlanting", true, "Prevents placement of resources when any placement ghosts are invalid for any reason.");
            preventInvalidPlanting = Config.Bind("General", "PreventInvalidPlanting", true, "Prevents plants from being placed where they will be unable to grow.");
            randomizeRotation = Config.Bind("General", "RandomizeRotation", true, "Randomizes rotation of pieces once placed.");
            useStamina = Config.Bind("General", "UseStamina", true, "Consume stamina for every piece placed.");
            useDurability = Config.Bind("General", "UseDurability", true, "Decrease durability of cultivator for every piece placed.");
            extraCropSpacing = Config.Bind("General", "ExtraCropSpacing", 0f, "Adds extra spacing between crops. Accepts negative values to decrease spacing (not recommended).");
            extraSaplingSpacing = Config.Bind("General", "ExtraSaplingSpacing", 0f, "Adds extra spacing between saplings. Accepts negative values to decrease spacing (not recommended).");
            gridSnappingStyle = Config.Bind("General", "GridSnappingStyle", GridSnappingStyle.Intelligent, "PLACEHOLDER");
            standardizeGridRotations = Config.Bind("General", "StandardizeGridRotations", true, "When set to true, this setting will prevent the diagonal snapping of new grids to existing grids.");

            //Pickables
            pickableSnapRadius = Config.Bind("Pickables", "PickableSnapRadius", 1.0f, "Determines default distance/spacing between pickable resources when planting.");
            berryBushSnapRadius = Config.Bind("Pickables", "BerryBushSnapRadius", 1.5f, "Determines distance/spacing between berry bushes when planting.");
            mushroomSnapRadius = Config.Bind("Pickables", "MushroomSnapRadius", 0.5f, "Determines distance/spacing between mushrooms when planting.");
            flowerSnapRadius = Config.Bind("Pickables", "FlowerSnapRadius", 1.0f, "Determines distance/spacing between flowers when planting.");
            preventOverlappingPlacements = Config.Bind("Pickables", "PreventOverlappingPlacements", true, new ConfigDescription("Prevents placement of pickable resources on top of colliding obstructions.", null, new Attributes { Order = 5 }));

            //Harvesting
            enableBulkHarvest = Config.Bind("Harvesting", "EnableBulkHarvest", true, "Enables the ability to harvest multiple resources at once.");
            harvestStyle = Config.Bind("Harvesting", "HarvestStyle", HarvestStyle.AllResources, "Determines bulk harvest style. LikeResources only harvests resources of the type you've interacted with. AllResources harvests all eligible resources.");
            harvestRadius = Config.Bind("Harvesting", "HarvestRadius", 3.0f, "Determines radius used to search for resources when bulk harvesting.");
            replantOnHarvest = Config.Bind("Harvesting", "ReplantOnHarvest", false, new ConfigDescription("Enables automatic replanting of crops when harvested, provided you have the resources.", null, new Attributes { Order = 4 }));

            //Controls
            enableModKey = Config.Bind("Controls", "EnableModKey", new KeyboardShortcut(KeyCode.F8),
                new ConfigDescription(
                    "Key to toggle on/off all mod features. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new Attributes { Description = "Key to toggle on/off all mod features." }));
            enableSnappingKey = Config.Bind("Controls", "EnableSnappingKey", new KeyboardShortcut(KeyCode.F10),
                new ConfigDescription(
                    "Key to toggle on/off piece snapping functionality. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new Attributes { Description = "Key to toggle on/off piece snapping functionality." }));
            increaseXKey = Config.Bind("Controls", "IncreaseXKey", new KeyboardShortcut(KeyCode.RightArrow),
                new ConfigDescription(
                    "Key to increase number of grid columns. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new Attributes { Description = "Key to increase number of grid columns." }));
            increaseYKey = Config.Bind("Controls", "IncreaseYKey", new KeyboardShortcut(KeyCode.UpArrow),
                new ConfigDescription(
                    "Key to increase number of grid rows. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new Attributes { Description = "Key to increase number of grid rows." }));
            decreaseXKey = Config.Bind("Controls", "DecreaseXKey", new KeyboardShortcut(KeyCode.LeftArrow),
                new ConfigDescription(
                    "Key to decrease number of grid columns. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new Attributes { Description = "Key to decrease number of grid columns." }));
            decreaseYKey = Config.Bind("Controls", "DecreaseYKey", new KeyboardShortcut(KeyCode.DownArrow),
                new ConfigDescription(
                    "Key to decrease number of grid rows. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new Attributes { Description = "Key to decrease number of grid rows." }));
            keyboardModifierKey = Config.Bind("Controls", "KeyboardModifierKey", new KeyboardShortcut(KeyCode.RightControl),
                new ConfigDescription(
                    "Modifier key when using keyboard controls. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new Attributes { Description = "Modifier key when using keyboard controls." }));
            gamepadModifierKey = Config.Bind("Controls", "GamepadModifierKey", new KeyboardShortcut(KeyCode.JoystickButton4),
                new ConfigDescription(
                    "Modifier key when using gamepad controls. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new Attributes { Description = "Modifier key when using gamepad controls." }));
            keyboardHarvestModifierKey = Config.Bind("Controls", "KeyboardHarvestModifierKey", new KeyboardShortcut(KeyCode.LeftShift),
                new ConfigDescription(
                    "Modifier key to enable bulk harvest when using keyboard controls. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                    null,
                    new Attributes { Description = "Modifier key to enable bulk harvest when using keyboard controls." }));

            //UI
            showCost = Config.Bind("UI", "ShowCost", true, new ConfigDescription("Update resource cost in build UI.", null, new Attributes { Order = 3 }));
            costDisplayStyle = Config.Bind("UI", "CostDisplayStyle", CostDisplayStyle.TotalCount, "Determines display style of the ShowCost setting. TotalCount shows total number of pieces to be placed. FullCost shows combined resoure cost of all pieces.");
            costDisplayLocation = Config.Bind("UI", "CostDisplayLocation", CostDisplayLocation.RightSide, "Determines whether to prepend or append text to the resource cost in build UI. LeftSide or RightSide will prepend or append respectively.");

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
        internal bool PreventOverlappingPlacements
        {
            get { return preventOverlappingPlacements.Value; }
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
        internal bool UseStamina
        {
            get { return useStamina.Value; }
        }
        internal bool UseDurability
        {
            get { return useDurability.Value; }
        }
        internal float ExtraCropSpacing
        {
            get { return extraCropSpacing.Value; }
        }
        internal float ExtraSaplingSpacing
        {
            get { return extraSaplingSpacing.Value; }
        }
        internal GridSnappingStyle GridSnappingStyle
        {
            get { return gridSnappingStyle.Value; }
        }
        internal bool StandardizeGridRotations
        {
            get { return standardizeGridRotations.Value; }
        }
        internal bool EnableBulkHarvest
        {
            get { return enableBulkHarvest.Value; }
        }
        internal HarvestStyle HarvestStyle
        {
            get { return harvestStyle.Value; }
        }
        internal float HarvestRadius
        {
            get { return harvestRadius.Value; }
        }
        internal bool ReplantOnHarvest
        {
            get { return replantOnHarvest.Value; }
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
        internal KeyCode KeyboardHarvestModifierKey
        {
            get { return keyboardHarvestModifierKey.Value.MainKey; }
        }
        internal bool ShowCost
        {
            get { return showCost.Value; }
        }
        internal CostDisplayStyle CostDisplayStyle
        {
            get { return costDisplayStyle.Value; }
        }
        internal CostDisplayLocation CostDisplayLocation
        {
            get { return costDisplayLocation.Value; }
        }

        internal class ConfigurationManagerAttributes
        {
            public string? Description;
            public int? Order;
        }
    }

    internal enum GridSnappingStyle
    {
        Intelligent,
        Legacy
    }
    internal enum HarvestStyle
    {
        LikeResources,
        AllResources
    }
    internal enum CostDisplayStyle
    {
        TotalCount,
        FullCost
    }
    internal enum CostDisplayLocation
    {
        LeftSide,
        RightSide
    }
}
