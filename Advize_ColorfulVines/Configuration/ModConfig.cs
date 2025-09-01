namespace Advize_ColorfulVines;

using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using static ConfigEventHandlers;

sealed class ModConfig
{
    private readonly ConfigFile ConfigFile;
    private List<ConfigEntry<Color>> _BerryColors;

    //[Vines]
    private readonly ConfigEntry<bool> enableCustomVinePiece;
    private readonly ConfigEntry<string> customPieceName;
    private readonly ConfigEntry<string> customPieceDescription;
    private readonly ConfigEntry<AshVineStyle> ashVineStyle;
    private readonly ConfigEntry<VineBerryStyle> vineBerryStyle;

    private readonly ConfigEntry<Color> ashVineCustomColor;
    private readonly ConfigEntry<Color> leftBerryColor;
    private readonly ConfigEntry<Color> centerBerryColor;
    private readonly ConfigEntry<Color> rightBerryColor;

    internal ModConfig(ConfigFile configFile)
    {
        ConfigFile = configFile;
        configFile.SaveOnConfigSet = false;

        //[Vines]
        enableCustomVinePiece = BindAndOrder(
            "Vines",
            "EnableCustomVinePiece",
            true, "Adds/Removes the color customizable vine piece from the cultivator.", 9);
        customPieceName = BindAndOrder(
            "Vines",
            "CustomPieceName",
            "Custom Ashvine",
            "Sets the piece name for the custom ashvine sapling added to the cultivator.", 8);
        customPieceDescription = BindAndOrder(
            "Vines",
            "CustomPieceDescription",
            "Plants an Ashvine sapling with the colours defined in the mod config.",
            "Sets the piece description for the custom ashvine sapling added to the cultivator.", 7);
        ashVineStyle = BindAndOrder(
            "Vines",
            "AshVineStyle",
            AshVineStyle.MeadowsGreen,
            "Defines how the color customizable vines will appear for you, and also what colors are saved on the vines when a sapling is placed. Select custom to display the colors selected at the time of placement.",
            6);
        vineBerryStyle = BindAndOrder(
            "Vines",
            "VineBerryStyle",
            VineBerryStyle.VanillaGreen,
            "Defines how the color customizable vine berries will appear for you, and also what colors are saved on the vines when a sapling is placed. Select custom to display the colors selected at the time of placement.",
            4);
        ashVineCustomColor = BindAndOrder(
            "Vines",
            "AshVineCustomColor",
            new Color(0.867f, 0, 0.278f, 1),
            "The customizable color for the leaf portion of color customizable vines.", 5);
        leftBerryColor = BindAndOrder(
            "Vines",
            "LeftBerryColor",
            new Color(1, 1, 1, 1),
            "The customizable color for the left-most vine berry cluster on color customizable vines.", 3);
        centerBerryColor = BindAndOrder(
            "Vines",
            "CenterBerryColor",
            new Color(1, 1, 1, 1),
            "The customizable color for the center vine berry cluster on color customizable vines.", 2);
        rightBerryColor = BindAndOrder(
            "Vines",
            "RightBerryColor",
            new Color(1, 1, 1, 1),
            "The customizable color for the right-most vine berry cluster on color customizable vines.", 1);

        configFile.Save();
        configFile.SaveOnConfigSet = true;

        //[Vines]
        enableCustomVinePiece.SettingChanged += ApplyVineConfigSettings;
        customPieceName.SettingChanged += UpdateLocalization;
        customPieceDescription.SettingChanged += UpdateLocalization;
        ashVineStyle.SettingChanged += ApplyVineConfigSettings;
        vineBerryStyle.SettingChanged += ApplyVineConfigSettings;

        ashVineCustomColor.SettingChanged += ApplyVineConfigSettings;
        leftBerryColor.SettingChanged += ApplyVineConfigSettings;
        centerBerryColor.SettingChanged += ApplyVineConfigSettings;
        rightBerryColor.SettingChanged += ApplyVineConfigSettings;
    }

    internal bool EnableCustomVinePiece => enableCustomVinePiece.Value;
    internal string CustomPieceName => customPieceName.Value;
    internal string CustomPieceDescription => customPieceDescription.Value;
    internal AshVineStyle AshVineStyle => ashVineStyle.Value;
    internal VineBerryStyle VineBerryStyle => vineBerryStyle.Value;
    internal Color VinesColor => ashVineCustomColor.Value;
    internal List<ConfigEntry<Color>> BerryColors => _BerryColors ??= [rightBerryColor, centerBerryColor, leftBerryColor];

    private ConfigEntry<T> BindAndOrder<T>(string group, string name, T value, string description, int order = 0)
    {
        return ConfigFile.Bind(group, name, value, new ConfigDescription(description, null, new ConfigurationManagerAttributes { Order = order }));
    }

    internal class ConfigurationManagerAttributes { public int? Order; }
}
