namespace Advize_PlantEasily;

using BepInEx.Configuration;
using UnityEngine;
using Attributes = ModConfig.ConfigurationManagerAttributes;

sealed class ModConfig
{
    private readonly ConfigFile Config;

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
    private readonly ConfigEntry<bool> minimizeGridSpacing;
    private readonly ConfigEntry<bool> globallyAlignGridDirections;

    //Performance
    private readonly ConfigEntry<int> maxConcurrentPlacements;
    private readonly ConfigEntry<int> bulkPlantingBatchSize;

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

    private readonly ConfigEntry<KeyboardShortcut> toggleAutoReplantKey;

    //UI
    private readonly ConfigEntry<bool> showCost;
    private readonly ConfigEntry<CostDisplayStyle> costDisplayStyle;
    private readonly ConfigEntry<CostDisplayLocation> costDisplayLocation;
    private readonly ConfigEntry<bool> showHUDKeyHints;
    private readonly ConfigEntry<bool> showHoverKeyHints;
    private readonly ConfigEntry<bool> showHoverReplantHint;

    internal ModConfig(ConfigFile configFile)
    {
        Config = configFile;
        configFile.SaveOnConfigSet = false;

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
        gridSnappingStyle = Config.Bind("General", "GridSnappingStyle", GridSnappingStyle.Intelligent, "Determines grid snapping style. Intelligent will attempt to prevent a new grid from overlapping with an old one. Legacy will allow any orientation of new rows and columns.");
        standardizeGridRotations = Config.Bind("General", "StandardizeGridRotations", true, "When set to true, this setting will prevent the diagonal snapping of new grids to existing grids.");
        minimizeGridSpacing = Config.Bind("General", "MinimizeGridSpacing", false, "Allows for tighter grids, but with varying spacing used between diverse/distinct plants. ");
        globallyAlignGridDirections = Config.Bind("General", "GloballyAlignGridDirections", true, "When set to true, new grid placements will have their column and row directions align with the global grid.");

        //Performance
        maxConcurrentPlacements = Config.Bind("Performance", "MaxConcurrentPlacements", 500, new ConfigDescription("Maximum amount of pieces that can be placed at once with the cultivator.", new AcceptableValueRange<int>(2, 10000)));
        bulkPlantingBatchSize = Config.Bind("Performance", "BulkPlantingBatchSize", 50, new ConfigDescription("This value determines how many concurrent pieces can be placed per frame. Reduce this value if the game hangs when placing too many pieces at once.", new AcceptableValueRange<int>(2, 10000)));

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
        toggleAutoReplantKey = Config.Bind("Controls", "ToggleAutoReplantKey", new KeyboardShortcut(KeyCode.None),
            new ConfigDescription(
                "Key to toggle on/off the [Harvesting]ReplantOnHarvest setting. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Key to toggle on/off the [Harvesting]ReplantOnHarvest setting." }));

        //UI
        showCost = Config.Bind("UI", "ShowCost", true, new ConfigDescription("Update resource cost in build UI.", null, new Attributes { Order = 3 }));
        costDisplayStyle = Config.Bind("UI", "CostDisplayStyle", CostDisplayStyle.TotalCount, "Determines display style of the ShowCost setting. TotalCount shows total number of pieces to be placed. FullCost shows combined resoure cost of all pieces.");
        costDisplayLocation = Config.Bind("UI", "CostDisplayLocation", CostDisplayLocation.RightSide, "Determines whether to prepend or append text to the resource cost in build UI. LeftSide or RightSide will prepend or append respectively.");
        showHUDKeyHints = Config.Bind("UI", "ShowHUDKeyHints", true, "Show KeyHints in build HUD.");
        showHoverKeyHints = Config.Bind("UI", "ShowHoverKeyHints", true, "Show KeyHints in hover text.");
        showHoverReplantHint = Config.Bind("UI", "ShowHoverReplantHint", true, "Show crop to be replanted upon harvest in hover text.");

        configFile.Save();
        configFile.SaveOnConfigSet = true;

        rows.SettingChanged += PlantEasily.GridSizeChanged;
        columns.SettingChanged += PlantEasily.GridSizeChanged;
        maxConcurrentPlacements.SettingChanged += PlantEasily.GridSizeChanged;
        increaseXKey.SettingChanged += PlantEasily.KeybindsChanged;
        increaseYKey.SettingChanged += PlantEasily.KeybindsChanged;
        decreaseXKey.SettingChanged += PlantEasily.KeybindsChanged;
        decreaseYKey.SettingChanged += PlantEasily.KeybindsChanged;
        keyboardHarvestModifierKey.SettingChanged += PlantEasily.KeybindsChanged;
        keyboardModifierKey.SettingChanged += PlantEasily.KeybindsChanged;
        gamepadModifierKey.SettingChanged += PlantEasily.KeybindsChanged;
    }

    internal bool EnableDebugMessages => enableDebugMessages.Value;
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
    internal float PickableSnapRadius => pickableSnapRadius.Value;
    internal float BerryBushSnapRadius => berryBushSnapRadius.Value;
    internal float MushroomSnapRadius => mushroomSnapRadius.Value;
    internal float FlowerSnapRadius => flowerSnapRadius.Value;
    internal bool PreventOverlappingPlacements => preventOverlappingPlacements.Value;
    internal bool PreventPartialPlanting => preventPartialPlanting.Value;
    internal bool PreventInvalidPlanting => preventInvalidPlanting.Value;
    internal bool RandomizeRotation => randomizeRotation.Value;
    internal bool UseStamina => useStamina.Value;
    internal bool UseDurability => useDurability.Value;
    internal float ExtraCropSpacing => extraCropSpacing.Value;
    internal float ExtraSaplingSpacing => extraSaplingSpacing.Value;
    internal GridSnappingStyle GridSnappingStyle => gridSnappingStyle.Value;
    internal bool StandardizeGridRotations => standardizeGridRotations.Value;
    internal bool MinimizeGridSpacing => minimizeGridSpacing.Value;
    internal bool GloballyAlignGridDirections => globallyAlignGridDirections.Value;
    internal int MaxConcurrentPlacements => maxConcurrentPlacements.Value;
    internal int BulkPlantingBatchSize => bulkPlantingBatchSize.Value;
    internal bool EnableBulkHarvest => enableBulkHarvest.Value;
    internal HarvestStyle HarvestStyle => harvestStyle.Value;
    internal float HarvestRadius => harvestRadius.Value;
    internal bool ReplantOnHarvest
    {
        get { return replantOnHarvest.Value; }
        set { replantOnHarvest.BoxedValue = value; }
    }
    internal KeyboardShortcut EnableModKey => enableModKey.Value;
    internal KeyboardShortcut EnableSnappingKey => enableSnappingKey.Value;
    internal KeyCode IncreaseXKey => increaseXKey.Value.MainKey;
    internal KeyCode IncreaseYKey => increaseYKey.Value.MainKey;
    internal KeyCode DecreaseXKey => decreaseXKey.Value.MainKey;
    internal KeyCode DecreaseYKey => decreaseYKey.Value.MainKey;
    internal KeyCode KeyboardModifierKey => keyboardModifierKey.Value.MainKey;
    internal KeyCode GamepadModifierKey => gamepadModifierKey.Value.MainKey;
    internal KeyCode KeyboardHarvestModifierKey => keyboardHarvestModifierKey.Value.MainKey;
    internal KeyboardShortcut ToggleAutoReplantKey => toggleAutoReplantKey.Value;
    internal bool ShowCost => showCost.Value;
    internal CostDisplayStyle CostDisplayStyle => costDisplayStyle.Value;
    internal CostDisplayLocation CostDisplayLocation => costDisplayLocation.Value;
    internal bool ShowHUDKeyHints => showHUDKeyHints.Value;
    internal bool ShowHoverKeyHints => showHoverKeyHints.Value;
    internal bool ShowHoverReplantHint => showHoverReplantHint.Value;

#nullable enable
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
