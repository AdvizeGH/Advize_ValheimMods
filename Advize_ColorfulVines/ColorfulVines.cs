namespace Advize_ColorfulVines;

using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using static StaticMembers;

[BepInPlugin(PluginID, PluginName, Version)]
public sealed class ColorfulVines : BaseUnityPlugin
{
    public const string PluginID = "advize.ColorfulVines";
    public const string PluginName = "ColorfulVines";
    public const string Version = "1.0.0";

    internal void Awake()
    {
        BepInEx.Logging.Logger.Sources.Add(ModLogger);
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(VineColor).TypeHandle);
        if (UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Dbgl("This mod is client-side only and is not needed on a dedicated server. Plugin patches will not be applied.", LogLevel.Warning);
            return;
        }
        config = new ModConfig(Config);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginID);
    }
}
