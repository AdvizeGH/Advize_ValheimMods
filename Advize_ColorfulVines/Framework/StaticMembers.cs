namespace Advize_ColorfulVines;

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using static ColorfulVines;

static class StaticMembers
{
    internal static ManualLogSource ModLogger = new($" {PluginName}");
    internal static ModConfig config;
    internal static readonly Dictionary<string, GameObject> prefabRefs = [];

    internal static readonly Dictionary<LogLevel, Action<string>> logActions = new()
    {
        { LogLevel.Fatal, ModLogger.LogFatal },
        { LogLevel.Error, ModLogger.LogError },
        { LogLevel.Warning, ModLogger.LogWarning },
        { LogLevel.Message, ModLogger.LogMessage },
        { LogLevel.Info, ModLogger.LogInfo },
        { LogLevel.Debug, ModLogger.LogDebug }
    };

    internal static readonly int saplingHash = "CV_VineAsh_sapling".GetStableHashCode();
    internal static readonly int ModdedVineHash = "cv_ModdedVine".GetStableHashCode();
    internal static readonly int VineColorHash = "cv_VineColor".GetStableHashCode();
    internal static readonly int BerryColor1Hash = "cv_BerryColor1".GetStableHashCode();
    internal static readonly int BerryColor2Hash = "cv_BerryColor2".GetStableHashCode();
    internal static readonly int BerryColor3Hash = "cv_BerryColor3".GetStableHashCode();
    internal static readonly Vector3 ColorBlackVector3 = new(0.00012345f, 0.00012345f, 0.00012345f);

    internal static readonly Color ColorVineGreen = new(0.729f, 1, 0.525f, 1);
    internal static readonly Color ColorVineRed = new(0.867f, 0, 0.278f, 1);
    internal static readonly Color ColorBerryGreen = new(1, 1, 1, 1);
    internal static readonly Color ColorBerryRed = new(1, 0, 0, 1);

    internal static Color VineColorFromConfig => config.AshVineStyle == AshVineStyle.Custom ?
            config.VinesColor : config.AshVineStyle == AshVineStyle.MeadowsGreen ?
            ColorVineGreen : ColorVineRed;

    internal static List<Color> BerryColorsFromConfig => config.VineBerryStyle == VineBerryStyle.Custom ?
            config.BerryColors.Select(x => x.Value).ToList() : config.VineBerryStyle == VineBerryStyle.RedGrapes ?
            Enumerable.Repeat(ColorBerryRed, 3).ToList() : Enumerable.Repeat(ColorBerryGreen, 3).ToList();

    internal static bool OverrideVines => config.AshVineStyle != AshVineStyle.Custom;
    internal static bool OverrideBerries => config.VineBerryStyle != VineBerryStyle.Custom;

    internal static Vector3 ColorToVector3(Color color) => color == Color.black ? ColorBlackVector3 : new(color.r, color.g, color.b);
    internal static Color Vector3ToColor(Vector3 vector3) => vector3 == ColorBlackVector3 ? Color.black : new(vector3.x, vector3.y, vector3.z);

    internal static void Dbgl(string message, LogLevel level = LogLevel.Info) => logActions[level](message);
}
