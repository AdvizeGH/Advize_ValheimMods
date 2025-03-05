namespace Advize_StumpsRegrow;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[BepInPlugin(PluginID, PluginName, Version)]
public sealed class StumpsRegrow : BaseUnityPlugin
{
    public const string PluginID = "advize.StumpsRegrow";
    public const string PluginName = "StumpsRegrow";
    public const string Version = "1.0.3";

    internal static ManualLogSource ModLogger = new($" {PluginName}");
    internal static ModConfig config;

    internal static readonly Dictionary<string, List<GameObject>> TreesPerStump = [];
    internal static readonly int HashedZDOName = "sr_TreeBaseName".GetStableHashCode();

    internal void Awake()
    {
        BepInEx.Logging.Logger.Sources.Add(ModLogger);
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(StumpGrower).TypeHandle);
        config = new(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "1.0.3", ModRequired = true });
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginID);
    }

    internal static void Dbgl(string message, LogLevel level = LogLevel.Info)
    {
        switch (level)
        {
            case LogLevel.Error:
                ModLogger.LogError(message);
                break;
            case LogLevel.Warning:
                ModLogger.LogWarning(message);
                break;
            case LogLevel.Info:
                ModLogger.LogInfo(message);
                break;
            case LogLevel.Message:
                ModLogger.LogMessage(message);
                break;
            case LogLevel.Debug:
                ModLogger.LogDebug(message);
                break;
            case LogLevel.Fatal:
                ModLogger.LogFatal(message);
                break;
        }
    }
}
