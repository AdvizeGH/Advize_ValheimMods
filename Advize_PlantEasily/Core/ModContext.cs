namespace Advize_PlantEasily;

using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

internal static class ModContext
{
    internal static readonly Dictionary<string, GameObject> PrefabRefs = [];

    internal static ManualLogSource ModLogger = new($" {PlantEasily.PluginName}");
    internal static ModConfig config;
    internal static List<PickableDB> PickableRefs = [];

    internal static void Dbgl(string message, bool forceLog = false, LogLevel level = LogLevel.Info)
    {
        if (forceLog || config.EnableDebugMessages)
            ModLogger.Log(level, message);
    }
}
