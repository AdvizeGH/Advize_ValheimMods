namespace Advize_PlantEverything;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SoftReferenceableAssets;
using UnityEngine;
using static PluginUtils;
using static StaticMembers;

[BepInPlugin(PluginID, PluginName, Version)]
public sealed class PlantEverything : BaseUnityPlugin
{
    public const string PluginID = "advize.PlantEverything";
    public const string PluginName = "PlantEverything";
    public const string Version = "1.20.0";

    public void Awake()
    {
        Runtime.MakeAllAssetsLoadable();
        BepInEx.Logging.Logger.Sources.Add(ModLogger);
        assetBundle = LoadAssetBundle("planteverything");
        config = new(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "1.20.0" });

        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Dbgl("Dedicated Server Detected");
            isDedicatedServer = true;
        }

        if (config.EnableExtraResources)
            ConfigEventHandlers.ExtraResourcesFileOrSettingChanged(null, null);
        if (config.EnableLocalization)
            LoadLocalizedStrings();

        new Harmony(PluginID).PatchAll();

        Dbgl("PlantEverything has loaded. Set [General]EnableDebugMessages to false to disable these messages.", level: LogLevel.Message);
    }
}
