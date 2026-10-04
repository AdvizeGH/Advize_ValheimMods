namespace Advize_PlantEasily;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using static ModContext;

[BepInPlugin(PluginID, PluginName, Version)]
[BepInDependency(ConditionalConfigSyncAPI.ConfigSync.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class PlantEasily : BaseUnityPlugin
{
    public const string PluginID = "advize.PlantEasily";
    public const string PluginName = "PlantEasily";
    public const string Version = "2.3.0";
    public const string MinimumRequiredVersion = "2.3.0";

    public void Awake()
    {
        BepInEx.Logging.Logger.Sources.Add(ModLogger);
        config = new ModConfig(Config);
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Dbgl("\n\tThis mod does not apply any server‑side patches." +
                "\n\tIt is optional on dedicated servers and only needed if you want ConditionalConfigSync support." +
                "\n\tPlugin patches will not be applied.", true, LogLevel.Warning);
            return;
        }
        new Harmony(PluginID).PatchAll();
    }
}
