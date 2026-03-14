namespace Advize_PlantEasily;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using static ModContext;

[BepInPlugin(PluginID, PluginName, Version)]
public sealed class PlantEasily : BaseUnityPlugin
{
    public const string PluginID = "advize.PlantEasily";
    public const string PluginName = "PlantEasily";
    public const string Version = "2.1.0";

    public void Awake()
    {
        BepInEx.Logging.Logger.Sources.Add(ModLogger);
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Dbgl("This mod is client-side only and is not needed on a dedicated server. Plugin patches will not be applied.", true, LogLevel.Warning);
            return;
        }
        config = new ModConfig(Config);
        new Harmony(PluginID).PatchAll();
    }
}
