namespace Advize_PlantEverything;

using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using static PlantEverything;
using static PluginUtils;

static class StaticMembers
{
    internal static ManualLogSource ModLogger = new($" {PluginName}");
    internal static ModConfig config;

    private static string customConfigPath;
    internal static string CustomConfigPath => customConfigPath ??= SetupConfigDirectory();

    internal static readonly Dictionary<LogLevel, Action<string>> logActions = new()
    {
        { LogLevel.Fatal, ModLogger.LogFatal },
        { LogLevel.Error, ModLogger.LogError },
        { LogLevel.Warning, ModLogger.LogWarning },
        { LogLevel.Message, ModLogger.LogMessage },
        { LogLevel.Info, ModLogger.LogInfo },
        { LogLevel.Debug, ModLogger.LogDebug }
    };

    internal static readonly Dictionary<string, GameObject> prefabRefs = [];
    internal static List<PieceDB> pieceRefs = [];
    internal static List<SaplingDB> saplingRefs = [];
    internal static List<ModdedPlantDB> moddedCropRefs = [];
    internal static List<ModdedPlantDB> moddedSaplingRefs = [];
    internal static List<ExtraResource> deserializedExtraResources = [];

    internal static bool piecesInitialized = false;
    internal static bool saplingsInitialized = false;
    internal static bool resolveMissingReferences = false;
    internal static bool isDedicatedServer = false;

    internal static AssetBundle assetBundle;
    internal static readonly Dictionary<string, Texture2D> cachedTextures = [];
    internal static readonly Dictionary<Texture2D, Sprite> cachedSprites = [];

    internal const Heightmap.Biome TemperateBiomes = Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest | Heightmap.Biome.Plains;
    internal static Heightmap.Biome AllBiomes = /*(Heightmap.Biome)895*/GetBiomeMask((Heightmap.Biome[])System.Enum.GetValues(typeof(Heightmap.Biome)));

    internal static readonly int PlaceAnywhereHash = "pe_placeAnywhere".GetStableHashCode();   

    internal static string[] layersForPieceRemoval = ["item", "piece_nonsolid", "Default_small", "Default"];

    internal static void Dbgl(string message, bool forceLog = false, LogLevel level = LogLevel.Info)
    {
        if (forceLog || config.EnableDebugMessages)
            logActions[level](message);
    }
}
