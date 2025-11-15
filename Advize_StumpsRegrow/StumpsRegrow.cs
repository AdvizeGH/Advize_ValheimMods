namespace Advize_StumpsRegrow;

using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

[BepInPlugin(PluginID, PluginName, Version)]
public sealed class StumpsRegrow : BaseUnityPlugin
{
    public const string PluginID = "advize.StumpsRegrow";
    public const string PluginName = "StumpsRegrow";
    public const string Version = "1.0.5";

    internal static ManualLogSource ModLogger = new($" {PluginName}");
    internal static ModConfig config;

    internal static readonly Dictionary<string, List<GameObject>> TreesPerStump = [];
    internal static readonly int HashedZDOName = "sr_TreeBaseName".GetStableHashCode();

    internal static readonly Dictionary<LogLevel, Action<string>> logActions = new()
        {
            { LogLevel.Fatal, ModLogger.LogFatal },
            { LogLevel.Error, ModLogger.LogError },
            { LogLevel.Warning, ModLogger.LogWarning },
            { LogLevel.Message, ModLogger.LogMessage },
            { LogLevel.Info, ModLogger.LogInfo },
            { LogLevel.Debug, ModLogger.LogDebug }
        };

    internal void Awake()
    {
        BepInEx.Logging.Logger.Sources.Add(ModLogger);
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(StumpGrower).TypeHandle);
        config = new(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "1.0.5", ModRequired = true });
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginID);
    }

    internal static void Dbgl(string message, LogLevel level = LogLevel.Info) => logActions[level](message);
}
