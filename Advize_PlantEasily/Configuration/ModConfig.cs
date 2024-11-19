namespace Advize_PlantEasily;

using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;
using Attributes = ModConfig.ConfigurationManagerAttributes;

sealed class ModConfig
{
    private readonly ConfigFile Config;

    //Controls
    private readonly ConfigEntry<KeyboardShortcut> enableModKey;
    private readonly ConfigEntry<KeyboardShortcut> enableSnappingKey;
    private readonly ConfigEntry<KeyboardShortcut> toggleAutoReplantKey;

    private readonly ConfigEntry<KeyboardShortcut> increaseXKey;
    private readonly ConfigEntry<KeyboardShortcut> increaseYKey;
    private readonly ConfigEntry<KeyboardShortcut> decreaseXKey;
    private readonly ConfigEntry<KeyboardShortcut> decreaseYKey;

    private readonly ConfigEntry<KeyboardShortcut> keyboardModifierKey;
    private readonly ConfigEntry<KeyboardShortcut> gamepadModifierKey;
    private readonly ConfigEntry<KeyboardShortcut> keyboardHarvestModifierKey;

    //Difficulty
    private readonly ConfigEntry<bool> preventPartialPlanting;
    private readonly ConfigEntry<bool> preventInvalidPlanting;
    private readonly ConfigEntry<bool> useStamina;
    private readonly ConfigEntry<bool> useDurability;

    //General
    private readonly ConfigEntry<bool> modActive;
    private readonly ConfigEntry<bool> snapActive;
    private readonly ConfigEntry<int> rows;
    private readonly ConfigEntry<int> columns;
    private readonly ConfigEntry<bool> randomizeRotation;
    private readonly ConfigEntry<bool> enableDebugMessages;

    //Grid
    private readonly ConfigEntry<bool> globallyAlignGridDirections;
    private readonly ConfigEntry<bool> minimizeGridSpacing;
    private readonly ConfigEntry<GridSnappingStyle> gridSnappingStyle;
    private readonly ConfigEntry<bool> forceAltPlacement;
    //private readonly ConfigEntry<bool> offsetOddRows; // Re-add later if you can work out the last bit of snapping jankyness
    private readonly ConfigEntry<float> extraCropSpacing;
    private readonly ConfigEntry<float> extraSaplingSpacing;

    //Harvesting
    private readonly ConfigEntry<bool> enableBulkHarvest;
    private readonly ConfigEntry<HarvestStyle> harvestStyle;
    private readonly ConfigEntry<float> harvestRadius;
    private readonly ConfigEntry<bool> replantOnHarvest;

    //Performance
    private readonly ConfigEntry<int> maxConcurrentPlacements;
    private readonly ConfigEntry<int> bulkPlantingBatchSize;

    //Pickables
    private readonly ConfigEntry<float> defaultGridSpacing;
    private readonly ConfigEntry<bool> preventOverlappingPlacements;

    //UI
    private readonly ConfigEntry<bool> showCost;
    private readonly ConfigEntry<CostDisplayStyle> costDisplayStyle;
    private readonly ConfigEntry<CostDisplayLocation> costDisplayLocation;
    private readonly ConfigEntry<bool> showHUDKeyHints;
    private readonly ConfigEntry<bool> showHoverKeyHints;
    private readonly ConfigEntry<bool> showHoverReplantHint;
    private readonly ConfigEntry<bool> showGhostsDuringPlacement;
    private readonly ConfigEntry<bool> showGridDirections;
    private readonly ConfigEntry<bool> highlightRootPlacementGhost;
    private readonly ConfigEntry<Color> rootGhostHighlightColor;
    private readonly ConfigEntry<Color> rowStartColor;
    private readonly ConfigEntry<Color> rowEndColor;
    private readonly ConfigEntry<Color> columnStartColor;
    private readonly ConfigEntry<Color> columnEndColor;

    internal ModConfig(ConfigFile configFile)
    {
        Config = configFile;
        configFile.SaveOnConfigSet = false;

        //Controls
        enableModKey = Config.Bind("Controls", "EnableModKey", new KeyboardShortcut(KeyCode.F8),
            new ConfigDescription(
                "Key to toggle on/off all mod features. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Key to toggle on/off all mod features.", Order = 10 }));
        enableSnappingKey = Config.Bind("Controls", "EnableSnappingKey", new KeyboardShortcut(KeyCode.F10),
            new ConfigDescription(
                "Key to toggle on/off piece snapping functionality. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Key to toggle on/off piece snapping functionality.", Order = 9 }));
        toggleAutoReplantKey = Config.Bind("Controls", "ToggleAutoReplantKey", new KeyboardShortcut(KeyCode.F6),
            new ConfigDescription(
                "Key to toggle on/off the [Harvesting]ReplantOnHarvest setting. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Key to toggle on/off the [Harvesting]ReplantOnHarvest setting.", Order = 8 }));
        increaseXKey = Config.Bind("Controls", "IncreaseXKey", new KeyboardShortcut(KeyCode.RightArrow),
            new ConfigDescription(
                "Key to increase number of grid columns. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Key to increase number of grid columns.", Order = 7 }));
        increaseYKey = Config.Bind("Controls", "IncreaseYKey", new KeyboardShortcut(KeyCode.UpArrow),
            new ConfigDescription(
                "Key to increase number of grid rows. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Key to increase number of grid rows.", Order = 6 }));
        decreaseXKey = Config.Bind("Controls", "DecreaseXKey", new KeyboardShortcut(KeyCode.LeftArrow),
            new ConfigDescription(
                "Key to decrease number of grid columns. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Key to decrease number of grid columns.", Order = 5 }));
        decreaseYKey = Config.Bind("Controls", "DecreaseYKey", new KeyboardShortcut(KeyCode.DownArrow),
            new ConfigDescription(
                "Key to decrease number of grid rows. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Key to decrease number of grid rows.", Order = 4 }));
        keyboardModifierKey = Config.Bind("Controls", "KeyboardModifierKey", new KeyboardShortcut(KeyCode.RightControl),
            new ConfigDescription(
                "Modifier key when using keyboard controls. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Modifier key when using keyboard controls.", Order = 3 }));
        gamepadModifierKey = Config.Bind("Controls", "GamepadModifierKey", new KeyboardShortcut(KeyCode.JoystickButton4),
            new ConfigDescription(
                "Modifier key when using gamepad controls. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Modifier key when using gamepad controls.", Order = 2 }));
        keyboardHarvestModifierKey = Config.Bind("Controls", "KeyboardHarvestModifierKey", new KeyboardShortcut(KeyCode.LeftShift),
            new ConfigDescription(
                "Modifier key to enable bulk harvest when using keyboard controls. See https://docs.unity3d.com/ScriptReference/KeyCode.html",
                null,
                new Attributes { Description = "Modifier key to enable bulk harvest when using keyboard controls.", Order = 1 }));

        //Difficulty
        preventPartialPlanting = Config.Bind("Difficulty", "PreventPartialPlanting", false, "Prevents placement of resources when any placement ghosts are invalid for any reason.");
        preventInvalidPlanting = Config.Bind("Difficulty", "PreventInvalidPlanting", true, "Prevents plants from being placed where they will be unable to grow.");
        useStamina = Config.Bind("Difficulty", "UseStamina", true, "Consume stamina for every piece placed.");
        useDurability = Config.Bind("Difficulty", "UseDurability", true, "Decrease durability of cultivator for every piece placed.");

        //General
        modActive = Config.Bind("General", "ModActive", true, new ConfigDescription("Enables all mod features.", null, new Attributes { Order = 6 }));
        snapActive = Config.Bind("General", "SnapActive", true, new ConfigDescription("Enables grid snapping feature.", null, new Attributes { Order = 5 }));
        rows = Config.Bind("General", "Rows", 2, new ConfigDescription("Number of rows for planting grid aka height.", null, new Attributes { Order = 4 }));
        columns = Config.Bind("General", "Columns", 2, new ConfigDescription("Number of columns for planting grid aka width.", null, new Attributes { Order = 3 }));
        randomizeRotation = Config.Bind("General", "RandomizeRotation", true, new ConfigDescription("Randomizes rotation of pieces once placed.", null, new Attributes { Order = 2 }));
        enableDebugMessages = Config.Bind("General", "EnableDebugMessages", false, new ConfigDescription("Enable mod debug messages in console.", null, new Attributes { Order = 1 }));

        //Grid
        globallyAlignGridDirections = Config.Bind("Grid", "GloballyAlignGridDirections", true, "When set to true, new grid placements will have their column and row directions align with the global grid.");
        minimizeGridSpacing = Config.Bind("Grid", "MinimizeGridSpacing", false, "Allows for tighter grids, but with varying spacing used between diverse/distinct plants. ");
        gridSnappingStyle = Config.Bind("Grid", "GridSnappingStyle", GridSnappingStyle.Intelligent, "Determines grid snapping style. Intelligent will attempt to prevent a new grid from overlapping with an old one. Legacy will allow any orientation of new rows and columns.");
        forceAltPlacement = Config.Bind("Grid", "ForceAltPlacement", false, "When enabled, alternate placement mode for the cultivator is always used. Alternate placement mode allows free rotation when snapping to a single piece.");
        //offsetOddRows = Config.Bind("Grid", "OffsetOddRows", false, "When enabled, alters grid formation by offsetting each odd row.");
        extraCropSpacing = Config.Bind("Grid", "ExtraCropSpacing", 0f, "Adds extra spacing between crops. Accepts negative values to decrease spacing (not recommended).");
        extraSaplingSpacing = Config.Bind("Grid", "ExtraSaplingSpacing", 0f, "Adds extra spacing between saplings. Accepts negative values to decrease spacing (not recommended).");

        //Harvesting
        enableBulkHarvest = Config.Bind("Harvesting", "EnableBulkHarvest", true, "Enables the ability to harvest multiple resources at once.");
        harvestStyle = Config.Bind("Harvesting", "HarvestStyle", HarvestStyle.AllResources, "Determines bulk harvest style. LikeResources only harvests resources of the type you've interacted with. AllResources harvests all eligible resources.");
        harvestRadius = Config.Bind("Harvesting", "HarvestRadius", 3.0f, "Determines radius used to search for resources when bulk harvesting.");
        replantOnHarvest = Config.Bind("Harvesting", "ReplantOnHarvest", false, new ConfigDescription("Enables automatic replanting of crops when harvested, provided you have the resources.", null, new Attributes { Order = 4 }));
        
        //Performance
        maxConcurrentPlacements = Config.Bind("Performance", "MaxConcurrentPlacements", 500, new ConfigDescription("Maximum amount of pieces that can be placed at once with the cultivator.", new AcceptableValueRange<int>(2, 10000)));
        bulkPlantingBatchSize = Config.Bind("Performance", "BulkPlantingBatchSize", 2, new ConfigDescription("This value determines how many concurrent pieces can be placed per frame. Increase to speed up planting. Reduce this value if the game hangs when placing too many pieces at once.", new AcceptableValueRange<int>(2, 10000)));
        
        //Pickables
        defaultGridSpacing = Config.Bind("Pickables", "DefaultGridSpacing", 1.0f, new ConfigDescription("Determines default distance/spacing between pickable resources when planting.", null, new Attributes { Order = 1 }));
        preventOverlappingPlacements = Config.Bind("Pickables", "PreventOverlappingPlacements", true, new ConfigDescription("Prevents placement of pickable resources on top of colliding obstructions.", null, new Attributes { Order = 2 }));

        //UI
        showCost = Config.Bind("UI", "ShowCost", true, new ConfigDescription("Update resource cost in build UI.", null, new Attributes { Order = 14 }));
        costDisplayStyle = Config.Bind("UI", "CostDisplayStyle", CostDisplayStyle.TotalCount, new ConfigDescription("Determines display style of the ShowCost setting. TotalCount shows total number of pieces to be placed. FullCost shows combined resoure cost of all pieces.", null, new Attributes { Order = 13 }));
        costDisplayLocation = Config.Bind("UI", "CostDisplayLocation", CostDisplayLocation.RightSide, new ConfigDescription("Determines whether to prepend or append text to the resource cost in build UI. LeftSide or RightSide will prepend or append respectively.", null, new Attributes { Order = 12 }));
        showHUDKeyHints = Config.Bind("UI", "ShowHUDKeyHints", true, new ConfigDescription("Show KeyHints in build HUD.", null, new Attributes { Order = 11 }));
        showHoverKeyHints = Config.Bind("UI", "ShowHoverKeyHints", true, new ConfigDescription("Show KeyHints in hover text.", null, new Attributes { Order = 10 }));
        showHoverReplantHint = Config.Bind("UI", "ShowHoverReplantHint", true, new ConfigDescription("Show crop to be replanted upon harvest in hover text.", null, new Attributes { Order = 9 }));
        showGhostsDuringPlacement = Config.Bind("UI", "ShowGhostsDuringPlacement", true, new ConfigDescription("Show silhouettes of placement ghosts during placement.", null, new Attributes { Order = 8 }));
        showGridDirections = Config.Bind("UI", "ShowGridDirections", true, new ConfigDescription("Render lines indicating direction of rows and columns.", null, new Attributes { Order = 7 }));
        highlightRootPlacementGhost = Config.Bind("UI", "HighlightRootGhost", true, new ConfigDescription("Highlight the root placement ghost while bulk planting.", null, new Attributes { Order = 6 }));
        rootGhostHighlightColor = Config.Bind("UI", "RootGhostHighlightColor", Color.green, new ConfigDescription("Highlight color for root placement ghost when [UI]HighlightRootGhost is enabled.", null, new Attributes { Order = 5 }));
        rowStartColor = Config.Bind("UI", "RowStartColor", Color.blue, new ConfigDescription("Starting color for row direction when [UI]ShowGridDirections is enabled.", null, new Attributes { Order = 4 }));
        rowEndColor = Config.Bind("UI", "RowEndColor", Color.cyan, new ConfigDescription("Ending color for row direction when [UI]ShowGridDirections is enabled.", null, new Attributes { Order = 3 }));
        columnStartColor = Config.Bind("UI", "ColumnStartColor", Color.green, new ConfigDescription("Starting color for column direction when [UI]ShowGridDirections is enabled.", null, new Attributes { Order = 2 }));
        columnEndColor = Config.Bind("UI", "ColumnEndColor", Color.yellow, new ConfigDescription("Ending color for column direction when [UI]ShowGridDirections is enabled.", null, new Attributes { Order = 1 }));

        configFile.Save();
        configFile.SaveOnConfigSet = true;

        rows.SettingChanged += PlantEasily.GridSizeChanged;
        columns.SettingChanged += PlantEasily.GridSizeChanged;
        maxConcurrentPlacements.SettingChanged += PlantEasily.GridSizeChanged;
        increaseXKey.SettingChanged += PlantEasily.KeybindsChanged;
        increaseYKey.SettingChanged += PlantEasily.KeybindsChanged;
        decreaseXKey.SettingChanged += PlantEasily.KeybindsChanged;
        decreaseYKey.SettingChanged += PlantEasily.KeybindsChanged;
        keyboardModifierKey.SettingChanged += PlantEasily.KeybindsChanged;
        gamepadModifierKey.SettingChanged += PlantEasily.KeybindsChanged;
        keyboardHarvestModifierKey.SettingChanged += PlantEasily.KeybindsChanged;
        showGridDirections.SettingChanged += (_, _) => PlantEasily.gridRenderer?.SetActive(false);
        rowStartColor.SettingChanged += PlantEasily.GridColorChanged;
        rowEndColor.SettingChanged += PlantEasily.GridColorChanged;
        columnStartColor.SettingChanged += PlantEasily.GridColorChanged;
        columnEndColor.SettingChanged += PlantEasily.GridColorChanged;
    }

    internal void BindPickableSpacingSettings()
    {
        Config.SaveOnConfigSet = false;

        Dictionary<string, float> predefinedSpacingDefaults = new()
        {
            { "Pickable_Dandelion", 0.75f },
            { "Pickable_Fiddlehead", 1.0f },
            { "Pickable_Mushroom", 0.5f },
            { "Pickable_Mushroom_blue", 0.5f },
            { "Pickable_Mushroom_yellow", 0.5f },
            { "Pickable_SmokePuff", 0.75f },
            { "Pickable_Thistle", 0.75f },
            { "BlueberryBush", 1.5f },
            { "RaspberryBush", 1.5f },
            { "CloudberryBush", 1.0f }
        };

        foreach (PickableDB pdb in PlantEasily.pickableRefs)
        {
            float gridSpacing = predefinedSpacingDefaults.TryGetValue(pdb.key, out float spacing) ? spacing : DefaultGridSpacing;
            pdb.itemName = Localization.instance.Localize(pdb.Prefab.GetComponent<Pickable>().m_itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
            pdb.configEntry = Config.Bind("Pickables", $"{pdb.key} GridSpacing", gridSpacing, $"Determines distance/spacing between {pdb.itemName} when planting.");
            pdb.configEntry.SettingChanged += PlantEasily.GridSpacingChanged;
        }

        predefinedSpacingDefaults.Clear();
        Config.Save();
        Config.SaveOnConfigSet = true;
    }

    //Controls
    internal KeyboardShortcut EnableModKey => enableModKey.Value;
    internal KeyboardShortcut EnableSnappingKey => enableSnappingKey.Value;
    internal KeyboardShortcut ToggleAutoReplantKey => toggleAutoReplantKey.Value;
    internal KeyCode IncreaseXKey => increaseXKey.Value.MainKey;
    internal KeyCode IncreaseYKey => increaseYKey.Value.MainKey;
    internal KeyCode DecreaseXKey => decreaseXKey.Value.MainKey;
    internal KeyCode DecreaseYKey => decreaseYKey.Value.MainKey;
    internal KeyCode KeyboardModifierKey => keyboardModifierKey.Value.MainKey;
    internal KeyCode GamepadModifierKey => gamepadModifierKey.Value.MainKey;
    internal KeyCode KeyboardHarvestModifierKey => keyboardHarvestModifierKey.Value.MainKey;
    //Difficulty
    internal bool PreventPartialPlanting => preventPartialPlanting.Value;
    internal bool PreventInvalidPlanting => preventInvalidPlanting.Value;
    internal bool UseStamina => useStamina.Value;
    internal bool UseDurability => useDurability.Value;
    //General    
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
    internal bool RandomizeRotation => randomizeRotation.Value;
    internal bool EnableDebugMessages => enableDebugMessages.Value;
    //Grid
    internal bool GloballyAlignGridDirections => globallyAlignGridDirections.Value;
    internal bool MinimizeGridSpacing => minimizeGridSpacing.Value;
    internal GridSnappingStyle GridSnappingStyle => gridSnappingStyle.Value;
    internal bool ForceAltPlacement => forceAltPlacement.Value;
    //internal bool OffsetOddRows => offsetOddRows.Value; // Maybe re-add in 2.1 if you can resolve remaining jankyness
    internal float ExtraCropSpacing => extraCropSpacing.Value;
    internal float ExtraSaplingSpacing => extraSaplingSpacing.Value;
    //Harvesting
    internal bool EnableBulkHarvest => enableBulkHarvest.Value;
    internal HarvestStyle HarvestStyle => harvestStyle.Value;
    internal float HarvestRadius => harvestRadius.Value;
    internal bool ReplantOnHarvest
    {
        get { return replantOnHarvest.Value; }
        set { replantOnHarvest.BoxedValue = value; }
    }
    //Performance
    internal int MaxConcurrentPlacements => maxConcurrentPlacements.Value;
    internal int BulkPlantingBatchSize => bulkPlantingBatchSize.Value;
    //Pickables
    internal float DefaultGridSpacing => defaultGridSpacing.Value;
    internal bool PreventOverlappingPlacements => preventOverlappingPlacements.Value;
    //UI
    internal bool ShowCost => showCost.Value;
    internal CostDisplayStyle CostDisplayStyle => costDisplayStyle.Value;
    internal CostDisplayLocation CostDisplayLocation => costDisplayLocation.Value;
    internal bool ShowHUDKeyHints => showHUDKeyHints.Value;
    internal bool ShowHoverKeyHints => showHoverKeyHints.Value;
    internal bool ShowHoverReplantHint => showHoverReplantHint.Value;
    internal bool ShowGhostsDuringPlacement => showGhostsDuringPlacement.Value;
    internal bool ShowGridDirections => showGridDirections.Value;
    internal bool HighlightRootPlacementGhost => highlightRootPlacementGhost.Value;
    internal Color RootGhostHighlightColor => rootGhostHighlightColor.Value;
    internal Color RowStartColor => rowStartColor.Value;
    internal Color RowEndColor => rowEndColor.Value;
    internal Color ColumnStartColor => columnStartColor.Value;
    internal Color ColumnEndColor => columnEndColor.Value;
    

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
