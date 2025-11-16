namespace Advize_Armoire;

using System.Collections.Generic;
using BepInEx.Configuration;

sealed class ModConfig
{
    private readonly ConfigFile ConfigFile;

    //General
    private readonly ConfigEntry<bool> enableOverrides;
    private readonly ConfigEntry<bool> showAllAppearances;
    private readonly ConfigEntry<string> disabledAppearanceNames;
    private readonly ConfigEntry<bool> excludeDLCItems;
    private readonly ConfigEntry<bool> showUndiscoveredHoverDetails;
    private readonly ConfigEntry<bool> enableDebugMessages;

    internal ModConfig(ConfigFile configFile)
    {
        ConfigFile = configFile;
        configFile.SaveOnConfigSet = false;

        enableOverrides = ConfigFile.Bind(
            "General",
            "EnableOverrides",
            true, "Enables overriding of appearances on local player.");
        showAllAppearances = ConfigFile.Bind(
            "General",
            "ShowAllAppearances",
            true, "Enables display of all unlockable appearances.");
        disabledAppearanceNames = ConfigFile.Bind(
            "General",
            "DisabledAppearanceNames",
            "ShieldKnight,ShieldIronSquare,CapeTest,PickaxeStone, SledgeCheat, SwordCheat",
            "To disable specific appearances from being totalled as locked/unlocked, list their prefab names here separated by a comma. Names are case-sensitive.");
        excludeDLCItems = ConfigFile.Bind(
            "General",
            "ExcludeDLCItems",
            true, "Excludes DLC items (OdinCape, HelmetOdin) from being totalled as locked/unlocked.");
        showUndiscoveredHoverDetails = ConfigFile.Bind(
            "General",
            "ShowUndiscoveredHoverDetails",
            false,
            "Shows/hides additional details in undiscovered appearance tooltips.");
        enableDebugMessages = ConfigFile.Bind(
            "General",
            "EnableDebugMessages",
            false,
            "Enable mod debug messages in console.");

        enableOverrides.SettingChanged += (_, _) => { Player.m_localPlayer?.SetupEquipment(); };

        excludeDLCItems.SettingChanged += (_, _) =>
        {
            if (Player.m_localPlayer is Player player)
                AppearanceCategorizer.RecalculateAppearances(player);

            ArmoireUI armoireUI = ArmoireUIController.ArmoireUIInstance;
            if (ArmoireUIController.IsArmoirePanelActive() && armoireUI.scrollView.activeSelf)
                armoireUI.RebuildScrollableGrid();
        };

        configFile.Save();
        configFile.SaveOnConfigSet = true;
    }

    internal bool EnableOverrides
    {
        get { return enableOverrides.Value; }
        set { enableOverrides.BoxedValue = value; }
    }

    internal bool ShowAllAppearances
    {
        get { return showAllAppearances.Value; }
        set { showAllAppearances.BoxedValue = value; }
    }

    internal HashSet<string> DisabledAppearanceNames => [.. disabledAppearanceNames.Value.Split(',')];

    internal bool ExcludeDLCItems => excludeDLCItems.Value;

    internal bool ShowUndiscoveredHoverDetails => showUndiscoveredHoverDetails.Value;

    internal bool EnableDebugMessages => enableDebugMessages.Value;
}
