namespace Advize_PlantEverything;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SoftReferenceableAssets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static PluginUtils;
using static StaticContent;

[BepInPlugin(PluginID, PluginName, Version)]
public sealed class PlantEverything : BaseUnityPlugin
{
    public const string PluginID = "advize.PlantEverything";
    public const string PluginName = "PlantEverything";
    public const string Version = "1.17.3";

    internal static ManualLogSource PELogger = new($" {PluginName}");

    internal static readonly Dictionary<string, GameObject> prefabRefs = [];
    private static List<PieceDB> pieceRefs = [];
    internal static List<SaplingDB> saplingRefs = [];
    internal static List<ExtraResource> deserializedExtraResources = [];
    internal static List<CustomPlantDB> customPlantRefs = [];

    private static bool piecesInitialized = false;
    private static bool saplingsInitialized = false;
    private static bool vinesInitialized = false;
    internal static bool resolveMissingReferences = false;

    internal static AssetBundle assetBundle;
    internal static readonly Dictionary<string, Texture2D> cachedTextures = [];
    internal static readonly Dictionary<Texture2D, Sprite> cachedSprites = [];

    internal static ModConfig config;

    public void Awake()
    {
        Runtime.MakeAllAssetsLoadable();
        BepInEx.Logging.Logger.Sources.Add(PELogger);
        assetBundle = LoadAssetBundle("planteverything");
        config = new(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "1.17.3" });
        SetupWatcher();
        if (config.EnableExtraResources)
            ExtraResourcesFileOrSettingChanged(null, null);
        if (config.EnableLocalization)
            LoadLocalizedStrings();
        Harmony harmony = new(PluginID);
        harmony.PatchAll();
        Game.isModded = true;
        Dbgl("PlantEverything has loaded. Set [General]EnableDebugMessages to false to disable these messages.", level: LogLevel.Message);
    }

    private static string ModConfigDirectory()
    {
        string path = Path.Combine(Paths.ConfigPath, PluginName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    private void SetupWatcher()
    {
        FileSystemWatcher watcher = new(ModConfigDirectory(), $"{PluginName}_ExtraResources.cfg");
        watcher.Changed += ExtraResourcesFileOrSettingChanged;
        watcher.Created += ExtraResourcesFileOrSettingChanged;
        watcher.Renamed += ExtraResourcesFileOrSettingChanged;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
    }
    //Tidy up these events related to extra resources in followup updates, code flow is getting ridiculous. Clean up all the log messages too while you're at it.
    internal static void ExtraResourcesFileOrSettingChanged(object sender, EventArgs e)
    {
        Dbgl($"ExtraResources file or setting has changed");
        if (config.IsSourceOfTruth)
        {
            if (config.EnableExtraResources)
            {
                Dbgl("IsSourceOfTruth: true, loading extra resources from disk");
                LoadExtraResources();
            }
            else
            {
                config.SyncedExtraResources.AssignLocalValue([]);
            }
        }
        else
        {
            Dbgl("IsSourceOfTruth: false, extra resources will not be loaded from disk");
            // Currently if a client changes their local ExtraResources.cfg while on a server, their new data won't be loaded.
            // If they then leave server and join a single player game their originally loaded ExtraResources.cfg data is used, not the updated file.
        }
    }

    private static string SerializeExtraResource(ExtraResource extraResource, bool prettyPrint = true) => JsonUtility.ToJson(extraResource, prettyPrint);

    private static ExtraResource DeserializeExtraResource(string extraResource) => JsonUtility.FromJson<ExtraResource>(extraResource);

    private static void SaveExtraResources()
    {
        string filePath = Path.Combine(ModConfigDirectory(), $"{PluginName}_ExtraResources.cfg");
        Dbgl($"deserializedExtraResources.Count is {deserializedExtraResources.Count}");

        string fullContent = "";
        //foreach (ExtraResource test in deserializedExtraResources)
        //{
        //    fullContent += SerializeExtraResource(test) + ";\n";
        //}
        fullContent += SerializeExtraResource(deserializedExtraResources[0]) + ";\n\n";
        fullContent += SerializeExtraResource(deserializedExtraResources[1], false) + ";\n";

        File.WriteAllText(filePath, fullContent);
        Dbgl($"Serialized extraResources to {filePath}", true);
    }

    private static void LoadExtraResources()
    {
        Dbgl("LoadExtraResources");
        deserializedExtraResources.Clear();
        string fileName = $"{PluginName}_ExtraResources.cfg";
        string filePath = Path.Combine(ModConfigDirectory(), fileName);

        try
        {
            string jsonText = File.ReadAllText(filePath);
            string[] split = jsonText.Split(';');

            foreach (string value in split)
            {
                if (value.IsNullOrWhiteSpace()) continue;
                ExtraResource er = DeserializeExtraResource(value);
                if (er.IsValid())
                {
                    deserializedExtraResources.Add(er);
                    //Dbgl($"er1 {er.prefabName}, {er.resourceName}, {er.resourceCost}, {er.groundOnly}, {er.pieceName}, {er.pieceDescription}");
                }
                else
                {
                    if (er.prefabName.StartsWith("PE_Fake")) continue;
                    Dbgl($"Invalid resource, {er.prefabName}, configured in {fileName}, skipping entry", true, LogLevel.Warning);
                }
            }

            Dbgl($"Loaded extra resources from {filePath}", true);
            //Dbgl($"deserializedExtraResources.Count is {deserializedExtraResources.Count}");
            Dbgl($"Assigning local value from deserializedExtraResources");

            List<string> resourcesToSync = [];
            deserializedExtraResources.ForEach(er => resourcesToSync.Add(SerializeExtraResource(er)));
            config.SyncedExtraResources.AssignLocalValue(resourcesToSync);
            return;
        }
        catch (Exception e)
        {
            //Dbgl(e.GetType().FullName, true, LogLevel.Error);
            if (e is FileNotFoundException)
            {
                Dbgl($"Error loading data from {fileName}. Generating new file with example values", true, LogLevel.Warning);
                deserializedExtraResources = GenerateExampleResources();
                SaveExtraResources();
            }
            else
            {
                Dbgl($"Error loading data from {fileName}. Additional resources have not been added", level: LogLevel.Warning);
                deserializedExtraResources.Clear();
            }
        }
    }
    //CustomSyncedValue value changed event handler
    internal static void ExtraResourcesChanged()
    {
        Dbgl("ExtraResourcesChanged");
        //Dbgl($"deserializedExtraResources.Count is currently {deserializedExtraResources.Count}");
        //Dbgl($"config.SyncedExtraResources.Count is currently {config.SyncedExtraResources.Value.Count}");

        deserializedExtraResources.Clear();
        foreach (string s in config.SyncedExtraResources.Value)
        {
            ExtraResource er = DeserializeExtraResource(s);
            deserializedExtraResources.Add(er);
            //Dbgl($"er2 {er.prefabName}, {er.resourceName}, {er.resourceCost}, {er.groundOnly}");
        }

        //Dbgl($"deserializedExtraResources.Count is now {deserializedExtraResources.Count}");

        //Dbgl("Attempting to call InitExtraResources");
        if (ZNetScene.s_instance)
        {
            //Dbgl("Calling InitExtraResources");
            InitExtraResourceRefs(ZNetScene.s_instance);
            PieceSettingChanged(null, null);
        }
    }

    private void LoadLocalizedStrings()
    {
        string fileName = $"{config.Language}_{PluginName}.json";
        string filePath = Path.Combine(ModConfigDirectory(), fileName);

        try
        {
            string jsonText = File.ReadAllText(filePath);
            ModLocalization ml = JsonUtility.FromJson<ModLocalization>(jsonText);

            foreach (string value in ml.LocalizedStrings)
            {
                string[] split = value.Split(':');
                DefaultLocalizedStrings.Remove(split[0]);
                DefaultLocalizedStrings.Add(split[0], split[1]);
            }

            Dbgl($"Loaded localized strings from {filePath}");
            return;
        }
        catch
        {
            Dbgl("EnableLocalization is true but unable to load localized text file, generating new one from default English values", true);
        }
        SerializeDict();
    }

    private void SerializeDict()
    {
        string filePath = Path.Combine(ModConfigDirectory(), $"english_{PluginName}.json");

        ModLocalization ml = new();
        foreach (KeyValuePair<string, string> kvp in DefaultLocalizedStrings)
        {
            ml.LocalizedStrings.Add($"{kvp.Key}:{kvp.Value}");
        }

        File.WriteAllText(filePath, JsonUtility.ToJson(ml, true));

        Dbgl($"Saved english localized strings to {filePath}");
    }

    internal static void Dbgl(string message, bool forceLog = false, LogLevel level = LogLevel.Info)
    {
        if (forceLog || config.EnableDebugMessages)
        {
            switch (level)
            {
                case LogLevel.Error:
                    PELogger.LogError(message);
                    break;
                case LogLevel.Warning:
                    PELogger.LogWarning(message);
                    break;
                case LogLevel.Info:
                    PELogger.LogInfo(message);
                    break;
                case LogLevel.Message:
                    PELogger.LogMessage(message);
                    break;
                case LogLevel.Debug:
                    PELogger.LogDebug(message);
                    break;
                case LogLevel.Fatal:
                    PELogger.LogFatal(message);
                    break;
            }
        }
    }

    internal static void InitPrefabRefs()
    {
        Dbgl("InitPrefabRefs");
        if (prefabRefs.Count > 0) return;

        Dictionary<string, AssetID> assetIds = Runtime.GetAllAssetPathsInBundleMappedToAssetID();
        bool foundAllRefs = false;

        VanillaPrefabRefs.ForEach(s => prefabRefs.Add(s, null));
        VanillaPrefabRefs.Clear();

        deserializedExtraResources.ForEach(er => prefabRefs[er.prefabName] = null);

        foreach (string key in assetIds.Keys)
        {
            if (!key.EndsWith(".prefab", StringComparison.Ordinal)) continue;

            string prefabName = key.Split('/').Last().Replace(".prefab", "");

            if (!prefabRefs.ContainsKey(prefabName)) continue;

            SoftReference<GameObject> prefab = new(assetIds[key]);
            prefab.Load();
            prefabRefs[prefabName] = prefab.Asset;

            if (!prefabRefs.Any(key => !key.Value))
            {
                Dbgl("Found all prefab references");
                foundAllRefs = true;
                break;
            }
        }

        if (!foundAllRefs)
        {
            Dbgl("Could not find all prefab references");
            List<string> nullKeys = prefabRefs.Where(key => !key.Value).Select(kvp => kvp.Key).ToList();

            if (!nullKeys.Any(key => !deserializedExtraResources.Select(x => x.prefabName).ToList().Contains(key)))
            {
                Dbgl("All missing prefab references are configured as ExtraResources. Attempting alternate prefab detection method.");
                UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(GameObject));
                for (int i = 0; i < array.Length; i++)
                {
                    GameObject gameObject = (GameObject)array[i];

                    if (!nullKeys.Contains(gameObject.name)) continue;

                    prefabRefs[gameObject.name] = gameObject;
                    nullKeys.Remove(gameObject.name);

                    if (!nullKeys.Any())
                    {
                        Dbgl("Found all prefab references");
                        break;
                    }
                }
            }

            foreach (string s in nullKeys)
            {
                Dbgl($"prefabRefs[{s}] value is null, removing key and value pair.");
                prefabRefs.Remove(s);
            }
        }

        CustomPrefabRefs.ForEach(s => prefabRefs.Add(s, CreatePrefab(s)));
        CustomPrefabRefs.Clear();
    }

    internal static bool InitExtraResourceRefs(ZNetScene instance, bool logErrors = false)
    {
        Dbgl("InitExtraResourceRefs");
        bool addedExtraResources = false;

        foreach (ExtraResource er in deserializedExtraResources)
        {
            //Dbgl($"er3 {er.prefabName}, {er.resourceName}, {er.resourceCost}, {er.groundOnly}");
            if (!prefabRefs.ContainsKey(er.prefabName) || !prefabRefs[er.prefabName])
            {
                GameObject targetPrefab = instance.GetPrefab(er.prefabName);
                if (targetPrefab)
                {
                    prefabRefs[er.prefabName] = targetPrefab;
                    Dbgl($"Added {er.prefabName} to prefabRefs");
                    addedExtraResources = true;
                }
                else
                {
                    Dbgl($"Could not find prefab reference for {er.prefabName}, skipping entry", logErrors || config.EnableDebugMessages, LogLevel.Warning);
                }
            }
        }

        return addedExtraResources;
    }

    private static void InitPieceRefs()
    {
        Dbgl("InitPieceRefs");

        if (pieceRefs.Count > 0)
        {
            RemoveFromCultivator(pieceRefs.ConvertAll(x => (PrefabDB)x));
            pieceRefs.Clear();
        }

        pieceRefs = GeneratePieceRefs();
    }

    private static void InitPieces()
    {
        Dbgl("InitPieces");

        foreach (PieceDB pdb in pieceRefs)
        {
            //_ = pdb.Piece; <-- can use discard to init piece reference, still not happy with this
            if (config.DisabledResourceNames.Contains(pdb.key))
            {
                Dbgl($"Resource disabled: {pdb.key}, skipping");
                pdb.enabled = false;
            }

            ItemDrop resource = ObjectDB.instance.GetItemPrefab(pdb.Resource.Key).GetComponent<ItemDrop>();

            if (pdb.Resources.Count > 0)
            {
                List<Piece.Requirement> resources = [];
                foreach (string item in pdb.Resources.Keys)
                {
                    resources.Add(new Piece.Requirement
                    {
                        m_resItem = ObjectDB.instance.GetItemPrefab(item).GetComponent<ItemDrop>(),
                        m_amount = pdb.Resources[item],
                        m_recover = pdb.recover
                    });
                }
                pdb.Piece.m_resources = [.. resources];
            }
            else
            {
                pdb.Piece.m_resources =
                [
                        new()
                        {
                            m_resItem = resource,
                            m_amount = pdb.ResourceCost,
                            m_recover = pdb.recover
                        }
                ];
            }

            pdb.Piece.m_icon = pdb.icon ? CreateSprite($"{pdb.key}PieceIcon.png", new Rect(0, 0, 64, 64)) : resource.m_itemData.GetIcon();

            pdb.Piece.m_placeEffect.m_effectPrefabs =
            [
                    new()
                    {
                        m_prefab = prefabRefs["vfx_Place_wood_pole"],
                        m_enabled = true
                    },
                    new()
                    {
                        m_prefab = prefabRefs["sfx_build_cultivator"],
                        m_enabled = true
                    }
            ];

            if (pdb.snapPoints != null)
            {
                Transform sp = pdb.Prefab.transform.Find("_snappoint");
                if (config.SnappableVines)
                {
                    if (!sp)
                    {
                        foreach (Vector3 point in pdb.snapPoints)
                        {
                            GameObject snapPoint = new("_snappoint") { tag = "snappoint" };
                            snapPoint.transform.position = point;
                            snapPoint.transform.SetParent(pdb.Prefab.transform);
                            snapPoint.SetActive(false);
                        }
                    }
                }
                else
                {
                    while (sp)
                    {
                        DestroyImmediate(sp.gameObject);
                        sp = pdb.Prefab.transform.Find("_snappoint");
                    }
                }
            }

            Pickable pickable = pdb.Prefab.GetComponent<Pickable>();
            if (pickable && !deserializedExtraResources.Any(x => x.prefabName == pdb.key))
            {
                pickable.m_respawnTimeMinutes = pdb.respawnTime;
                pickable.m_amount = pdb.resourceReturn;
                pdb.Piece.m_onlyInBiome = pdb.biome;

                Transform vanillaVisualChild = pdb.Prefab.transform.Find("visual");

                if (!vanillaVisualChild) continue;

                Transform moddedPickedChild = prefabRefs[pdb.key + "_Picked"].transform.Find("PE_Picked");

                if (moddedPickedChild)
                {
                    if (config.ShowPickableSpawners)
                    {
                        moddedPickedChild.SetParent(pdb.Prefab.transform);
                    }

                    if (!piecesInitialized)
                    {
                        MeshRenderer target = moddedPickedChild.GetComponent<MeshRenderer>();
                        MeshRenderer source = vanillaVisualChild.GetComponent<MeshRenderer>();

                        target.sharedMaterials = pdb.key == "Pickable_Thistle" ?
                        vanillaVisualChild.Find("default").GetComponent<MeshRenderer>().sharedMaterials :
                        source.sharedMaterials;

                        if (pdb.key.Contains("Dandelion"))
                        {
                            Material m = source.sharedMaterials[0];
                            target.sharedMaterials = [m, m];
                        }
                    }
                }
                else
                {
                    Transform vanillaPickedChild = prefabRefs[pdb.key].transform.Find("PE_Picked");
                    if (!config.ShowPickableSpawners && vanillaPickedChild)
                    {
                        vanillaPickedChild.SetParent(prefabRefs[pdb.key + "_Picked"].transform);
                    }
                }

                if (pdb.extraDrops)
                {
                    pickable.m_extraDrops.m_drops.Clear();
                }
            }
        }

        piecesInitialized = true;
    }

    private static void InitSaplingRefs()
    {
        Dbgl("InitSaplingRefs");

        if (saplingRefs.Count > 0)
        {
            RemoveFromCultivator(saplingRefs.ConvertAll(x => (PrefabDB)x));
            saplingRefs.Clear();
        }

        saplingRefs = GenerateCustomSaplingRefs();
    }

    private static void InitSaplings()
    {
        Dbgl("InitSaplings");

        ModifyTreeDrops();

        foreach (SaplingDB sdb in Enumerable.Concat(GenerateVanillaSaplingRefs(), saplingRefs))
        {
            Plant plant = sdb.Prefab.GetComponent<Plant>();
            Piece piece = sdb.Prefab.GetComponent<Piece>();

            plant.m_growTime = plant.m_growTimeMax = sdb.growTime;
            plant.m_growRadius = sdb.growRadius;
            plant.m_minScale = sdb.minScale;
            plant.m_maxScale = sdb.maxScale;

            piece.m_onlyInBiome = plant.m_biome = sdb.biome;
            plant.m_tolerateCold = sdb.tolerateCold || !config.PlantsRequireShielding;
            plant.m_tolerateHeat = sdb.tolerateHeat || !config.PlantsRequireShielding;
            plant.m_destroyIfCantGrow = piece.m_groundOnly = !config.PlaceAnywhere;

            if (!saplingRefs.Contains(sdb)) continue;

            plant.m_grownPrefabs = sdb.grownPrefabs;

            List<Piece.Requirement> resources = [];
            foreach (string item in sdb.Resources.Keys)
            {
                resources.Add(new Piece.Requirement
                {
                    m_resItem = ObjectDB.instance.GetItemPrefab(item).GetComponent<ItemDrop>(),
                    m_amount = sdb.Resources[item],
                    m_recover = false
                });
            }
            piece.m_resources = [.. resources];

            if (config.DisabledResourceNames.Contains(sdb.key))
            {
                Dbgl($"Resource disabled: {sdb.key}");
                sdb.enabled = false;
            }

            if (saplingsInitialized) continue;

            string[] p = ["healthy", "unhealthy"];
            Transform t = prefabRefs["Birch_Sapling"].transform.Find(p[0]);

            AssetID.TryParse("f6de4704e075b4095ae641aed283b641", out AssetID id);
            SoftReference<Shader> pieceShader = new(id);
            pieceShader.Load();

            if (sdb.source != "AshlandsTree3")
            {
                foreach (string parent in p)
                    sdb.Prefab.transform.Find(parent).GetComponent<MeshFilter>().mesh = t.Find("Birch_Sapling").GetComponent<MeshFilter>().mesh;
            }

            switch (sdb.source) // Cases are in {} code blocks to re-use variable names and contain them within a local scope. Why did I do this? -> Don't know, just stop trying to delete them
            {
                case "YggaShoot_small1":
                    {
                        string[] foliage = ["birchleafs002", "birchleafs003", "birchleafs008", "birchleafs009", "birchleafs010", "birchleafs011"];
                        Material[] m = [prefabRefs[sdb.source].transform.Find("beech").GetComponent<MeshRenderer>().sharedMaterials[0]];
                        Material[] m2 = [prefabRefs[sdb.source].transform.Find("beech").GetComponent<MeshRenderer>().sharedMaterials[1]];

                        foreach (string parent in p)
                            sdb.Prefab.transform.Find(parent).GetComponent<MeshRenderer>().sharedMaterials = m2;

                        foreach (string child in foliage)
                        {
                            foreach (string parent in p)
                            {
                                sdb.Prefab.transform.Find(parent).Find(child).GetComponent<MeshFilter>().mesh = t.Find(child).GetComponent<MeshFilter>().mesh;
                                sdb.Prefab.transform.Find(parent).Find(child).GetComponent<MeshRenderer>().sharedMaterials = m;
                            }
                        }
                    }
                    break;

                case "SwampTree1":
                    {
                        Material[] m = [prefabRefs[sdb.source].transform.Find("swamptree1").GetComponent<MeshRenderer>().sharedMaterials[0]];
                        m[0].shader = pieceShader.Asset;// Shader.Find("Custom/Piece");

                        foreach (string parent in p)
                            sdb.Prefab.transform.Find(parent).GetComponent<MeshRenderer>().sharedMaterials = m;
                    }
                    break;

                case "Birch1_aut":
                    {
                        string[] foliage = ["birchleafs002", "birchleafs003", "birchleafs008", "birchleafs009", "birchleafs010", "birchleafs011"];
                        Material[] m = [prefabRefs[sdb.source].transform.Find("Lod0").GetComponent<MeshRenderer>().sharedMaterials[0]];
                        Material[] m2 = [t.Find("Birch_Sapling").GetComponent<MeshRenderer>().sharedMaterials[0]];

                        foreach (string parent in p)
                            sdb.Prefab.transform.Find(parent).GetComponent<MeshRenderer>().sharedMaterials = m2;

                        foreach (string child in foliage)
                        {
                            foreach (string parent in p)
                            {
                                sdb.Prefab.transform.Find(parent).Find(child).GetComponent<MeshFilter>().mesh = t.Find(child).GetComponent<MeshFilter>().mesh;
                                sdb.Prefab.transform.Find(parent).Find(child).GetComponent<MeshRenderer>().sharedMaterials = m;
                            }
                        }
                    }
                    break;

                case "AshlandsTree3":
                    {
                        Transform t2 = prefabRefs[sdb.source].transform.Find("default");
                        MeshRenderer renderer = t2.GetComponent<MeshRenderer>();

                        Material[] m = [renderer.sharedMaterials[0]];
                        m[0].shader = pieceShader.Asset;
                        m[0].SetTexture("_EmissionMap", renderer.sharedMaterials[0].GetTexture("_EmissiveTex"));

                        foreach (string parent in p)
                        {
                            sdb.Prefab.transform.Find(parent).GetComponent<MeshFilter>().mesh = t2.GetComponent<MeshFilter>().mesh;
                            sdb.Prefab.transform.Find(parent).GetComponent<MeshRenderer>().sharedMaterials = m;
                        }
                    }
                    break;
            }

            piece.m_icon = sdb.icon ? CreateSprite($"{sdb.key}PieceIcon.png", new Rect(0, 0, 64, 64)) : piece.m_resources[0].m_resItem.m_itemData.GetIcon();

            piece.m_placeEffect.m_effectPrefabs[0].m_prefab = prefabRefs["vfx_Place_wood_pole"];
            piece.m_placeEffect.m_effectPrefabs[1].m_prefab = prefabRefs["sfx_build_cultivator"];

            sdb.Prefab.GetComponent<Destructible>().m_hitEffect.m_effectPrefabs = prefabRefs["Birch_Sapling"].GetComponent<Destructible>().m_hitEffect.m_effectPrefabs;
        }

        saplingsInitialized = true;
    }

    private static void ModifyTreeDrops()
    {
        if (!config.EnableSeedOverrides) return;

        foreach (KeyValuePair<GameObject, GameObject> kvp in TreesToSeeds)
        {
            TreeBase tree = kvp.Key.GetComponent<TreeBase>();
            DropTable.DropData itemDrop = default;
            bool dropExists = false;

            foreach (DropTable.DropData drop in tree.m_dropWhenDestroyed.m_drops)
            {
                if (drop.m_item.Equals(kvp.Value))
                {
                    dropExists = true;
                    itemDrop = drop;
                    break;
                }
            }

            if (dropExists) tree.m_dropWhenDestroyed.m_drops.Remove(itemDrop);

            itemDrop.m_item = kvp.Value;
            itemDrop.m_stackMin = config.SeedDropMin;
            itemDrop.m_stackMax = config.SeedDropMax;
            itemDrop.m_weight = 1;
            tree.m_dropWhenDestroyed.m_dropMin = config.TreeDropMin;
            tree.m_dropWhenDestroyed.m_dropMax = config.TreeDropMax;
            tree.m_dropWhenDestroyed.m_drops.Add(itemDrop);
            tree.m_dropWhenDestroyed.m_dropChance = Mathf.Clamp(config.DropChance, 0f, 1f);
            tree.m_dropWhenDestroyed.m_oneOfEach = config.OneOfEach;
        }
    }

    private static void InitCrops()
    {
        Dbgl("InitCrops");

        bool overridesEnabled = config.EnableCropOverrides;

        foreach (PrefabDB pdb in GenerateCropRefs())
        {
            Piece piece = pdb.Prefab.GetComponent<Piece>();
            Plant plant = pdb.Prefab.GetComponent<Plant>();
            Pickable pickable = plant.m_grownPrefabs[0].GetComponent<Pickable>();

            piece.m_resources[0].m_amount = pdb.resourceCost;
            piece.m_primaryTarget = piece.m_randomTarget = config.EnemiesTargetCrops;

            plant.m_biome = pdb.biome;
            plant.m_tolerateCold = pdb.tolerateCold || !config.PlantsRequireShielding;
            plant.m_tolerateHeat = pdb.tolerateHeat || !config.PlantsRequireShielding;

            plant.m_minScale = overridesEnabled ? config.CropMinScale : 0.9f;
            plant.m_maxScale = overridesEnabled ? config.CropMaxScale : 1.1f;
            plant.m_growTime = overridesEnabled ? config.CropGrowTimeMin : 4000f;
            plant.m_growTimeMax = overridesEnabled ? config.CropGrowTimeMax : 5000f;
            plant.m_growRadius = overridesEnabled ? config.CropGrowRadius : 0.5f;
            plant.m_needCultivatedGround = piece.m_cultivatedGroundOnly = !overridesEnabled || config.CropRequireCultivation;

            pickable.m_amount = pdb.resourceReturn;

            //For jotun puffs and magecap
            pickable.m_extraDrops.m_drops.Clear();
            if (pdb.extraDrops & !overridesEnabled)
            {
                pickable.m_extraDrops.m_drops.Add(new DropTable.DropData { m_item = pickable.m_itemPrefab, m_stackMin = 1, m_stackMax = 1, m_weight = 0 });
            }
        }

        if (customPlantRefs.Count == 0) return;

        overridesEnabled = overridesEnabled && config.OverrideModdedCrops;

        foreach (CustomPlantDB cdb in customPlantRefs)
        {
            Piece piece = cdb.Prefab.GetComponent<Piece>();
            Plant plant = cdb.Prefab.GetComponent<Plant>();
            //Pickable pickable = plant.m_grownPrefabs[0].GetComponent<Pickable>();

            //piece.m_resources[0].m_amount = overridesEnabled ? 1 : cdb.resourceCost;
            piece.m_primaryTarget = piece.m_randomTarget = config.EnemiesTargetCrops;

            plant.m_biome = overridesEnabled && !config.EnforceBiomesVanilla ? AllBiomes : cdb.biome;
            plant.m_tolerateCold = cdb.tolerateCold || !config.PlantsRequireShielding;
            plant.m_tolerateHeat = cdb.tolerateHeat || !config.PlantsRequireShielding;

            plant.m_minScale = overridesEnabled ? config.CropMinScale : cdb.minScale;
            plant.m_maxScale = overridesEnabled ? config.CropMaxScale : cdb.maxScale;
            plant.m_growTime = overridesEnabled ? config.CropGrowTimeMin : cdb.growTime;
            plant.m_growTimeMax = overridesEnabled ? config.CropGrowTimeMax : cdb.growTimeMax;
            plant.m_growRadius = overridesEnabled ? config.CropGrowRadius : cdb.growRadius;
            plant.m_needCultivatedGround = piece.m_cultivatedGroundOnly = !overridesEnabled || config.CropRequireCultivation;

            //pickable.m_amount = overridesEnabled ? 3 : cdb.resourceReturn;

            //pickable.m_extraDrops.m_drops.Clear();
            //if (!overridesEnabled)
            //{
            //    pickable.m_extraDrops = cdb.extraDrops;
            //}
        }
    }

    //TODO: This is mostly temporary garbage that can be refactored to piggyback off existing code. Need to decide what settings to expose as well.
    private static void InitVines()
    {
        Dbgl("InitVines");

        if (!vinesInitialized)
        {
            GameObject cloneContainer = new("PE_VineAsh_sapling");
            cloneContainer.SetActive(false);
            DontDestroyOnLoad(cloneContainer);

            GameObject VineAsh_saplingClone = Instantiate(prefabRefs["VineAsh_sapling"], cloneContainer.transform);
            VineAsh_saplingClone.name = cloneContainer.name;

            Piece piece = VineAsh_saplingClone.GetComponent<Piece>();
            piece.m_name = "$peVineAshSaplingName";
            piece.m_description = "$peVineAshSaplingDescription";

            Texture2D pieceIcon = DuplicateTexture(piece.m_icon);
            cachedTextures.Add("PE_VineAsh_saplingPieceIcon.png", pieceIcon);
            VineAsh_saplingClone.GetComponent<Plant>().m_name = "$peVineAshSaplingName";
            prefabRefs.Add("PE_VineAsh_sapling", VineAsh_saplingClone);

            //Dbgl("Adding initial VineColor component to VineAsh & VineAsh_sapling prefabs");
            prefabRefs["VineAsh"].AddComponent<VineColor>();
            prefabRefs["VineAsh_sapling"].AddComponent<VineColor>();

            vinesInitialized = true;
        }

        PieceTable pieceTable = prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
        if (!config.EnableCustomVinePiece && pieceTable.m_pieces.Contains(prefabRefs["PE_VineAsh_sapling"]))
        {
            if (HoldingCultivator) SheatheCultivator();
            pieceTable.m_pieces.Remove(prefabRefs["PE_VineAsh_sapling"]);
        }

        if (config.EnableCustomVinePiece && !pieceTable.m_pieces.Contains(prefabRefs["PE_VineAsh_sapling"]))
        {
            if (HoldingCultivator) SheatheCultivator();
            int index = pieceTable.m_pieces.IndexOf(prefabRefs["VineAsh_sapling"]);
            pieceTable.m_pieces.Insert(index + 1, prefabRefs["PE_VineAsh_sapling"]);
        }

        Plant plant = prefabRefs["VineAsh_sapling"].GetComponent<Plant>();
        Plant plant2 = prefabRefs["PE_VineAsh_sapling"].GetComponent<Plant>();

        Pickable pickable = prefabRefs["VineAsh"].GetComponent<Pickable>();
        //Vine vine = prefabRefs["VineAsh"].GetComponent<Vine>();

        plant.m_biome = plant2.m_biome = config.EnforceBiomesVanilla ? /*All but mountain and deep north*/(Heightmap.Biome)827 : (Heightmap.Biome)895;
        plant.m_needCultivatedGround = plant2.m_needCultivatedGround = prefabRefs["VineAsh_sapling"].GetComponent<Piece>().m_cultivatedGroundOnly = !config.EnableCropOverrides || config.CropRequireCultivation;
        plant.m_growTime = config.EnableVineOverrides ? config.VinesGrowthTime : 200f;
        plant.m_growTimeMax = config.EnableVineOverrides ? config.VinesGrowthTime : 300f;
        plant.m_attachDistance = config.EnableVineOverrides ? config.VinesAttachDistance : 1.8f;
        plant.m_growRadiusVines = config.EnableVineOverrides ? config.VineGrowRadius : 1.8f;
        plant.m_tolerateCold = plant2.m_tolerateCold = !config.PlantsRequireShielding;

        pickable.m_amount = config.EnableVineOverrides ? config.VineBerryReturn : 3;
        pickable.m_respawnTimeInitMax = config.EnableVineOverrides ? 0 : 150;
        pickable.m_respawnTimeMinutes = config.EnableVineOverrides ? config.VineBerryRespawnTime : 200;
        //vine.m_growTime = vine.m_growTimePerBranch = vine.m_growCheckTime = 15;

        //Update colors on existing vines
        VineColor.UpdateColors();

        //Update custom piece icon
        Texture2D baseSpriteTexture = cachedTextures["PE_VineAsh_saplingPieceIcon.png"];
        prefabRefs["PE_VineAsh_sapling"].GetComponent<Piece>().m_icon = ModifyTextureColor(baseSpriteTexture, 64, 64, VineColorFromConfig);
    }

    private static void InitCultivator()
    {
        Dbgl("InitCultivator");

        PieceTable pieceTable = prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;

        for (int i = 0; i < saplingRefs.Count; i++)
        {
            if (!saplingRefs[i].enabled)
                continue;
            if (!pieceTable.m_pieces.Contains(saplingRefs[i].Prefab))
                pieceTable.m_pieces.Insert(16, saplingRefs[i].Prefab);
        }
        for (int i = 0; i < pieceRefs.Count; i++)
        {
            if (!pieceRefs[i].enabled)
                continue;
            if (!pieceTable.m_pieces.Contains(pieceRefs[i].Prefab))
                pieceTable.m_pieces.Add(pieceRefs[i].Prefab);
        }

        pieceTable.m_canRemovePieces = true;
    }

    private static void RemoveFromCultivator(List<PrefabDB> prefabs)
    {
        if (HoldingCultivator) SheatheCultivator();

        PieceTable pieceTable = prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
        prefabs.ForEach(x => pieceTable.m_pieces.Remove(x.Prefab));
    }

    internal static bool HoldingCultivator => Player.m_localPlayer?.GetRightItem()?.m_shared.m_name == "$item_cultivator";

    private static void SheatheCultivator()
    {
        PELogger.LogWarning("Cultivator updated through config change, unequipping cultivator");
        Player.m_localPlayer.HideHandItems();
    }

    internal static void FinalInit(ZNetScene instance)
    {
        Dbgl("Performing final mod initialization");
        InitExtraResourceRefs(instance);
        InitPieceRefs();
        InitPieces();
        InitSaplingRefs();
        InitSaplings();
        InitCrops();
        InitVines();
        InitCultivator();

        if (DefaultLocalizedStrings.Count > 0)
            InitLocalization();

        for (int i = 0; i < saplingRefs.Count; i++)
        {
            GameObject go = saplingRefs[i].Prefab;

            if (!instance.m_prefabs.Contains(go))
            {
                instance.m_prefabs.Add(go);
                instance.m_namedPrefabs.Add(instance.GetPrefabHash(go), go);
            }
        }
    }

    internal static void CoreSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing mod");
        InitPieceRefs();
        InitPieces();
        InitSaplingRefs();
        InitSaplings();
        InitCrops();
        InitVines();
        InitCultivator();
    }

    internal static void PieceSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing pieces");
        InitPieceRefs();
        InitPieces();
        InitCultivator();
    }

    internal static void SaplingSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing saplings");
        InitSaplingRefs();
        InitSaplings();
        InitCultivator();
    }

    internal static void SeedSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, modifying TreeBase drop tables");
        ModifyTreeDrops();
    }

    internal static void CropSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing crops");
        InitCrops();
    }

    internal static void VineSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing vines");
        InitVines();
    }

    public static void InitLocalization()
    {
        Dbgl("InitLocalization");

        foreach (KeyValuePair<string, string> kvp in DefaultLocalizedStrings)
        {
            Localization.instance.AddWord($"pe{kvp.Key}", kvp.Value);
        }

        DefaultLocalizedStrings.Clear();
    }
}
