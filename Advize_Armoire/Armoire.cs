namespace Advize_Armoire;

using System.Reflection;
using BepInEx;
using HarmonyLib;
using SoftReferenceableAssets;
using UnityEngine;
using static StaticMembers;

[BepInPlugin(PluginID, PluginName, Version)]
public sealed class Armoire : BaseUnityPlugin
{
    public const string PluginID = "advize.Armoire";
    public const string PluginName = "Armoire";
    public const string Version = "1.0.0";

    internal void Awake()
    {
        Runtime.MakeAllAssetsLoadable();
        BepInEx.Logging.Logger.Sources.Add(ModLogger);
        config = new(Config);
        assetBundle = LoadAssetBundle("armoire");
        guiPrefab = assetBundle.LoadAsset<GameObject>("ArmoireGUI");
        armoireSlot = assetBundle.LoadAsset<GameObject>("ArmoireSlot");
        armoirePiecePrefab = assetBundle.LoadAsset<GameObject>("ArmoirePiece");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginID);
    }
}
