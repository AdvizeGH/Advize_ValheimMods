namespace Advize_PlantEverything;

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using SoftReferenceableAssets;
using UnityEngine;
using static PluginUtils;
using static StaticContent;
using static StaticMembers;

static class PluginInitUtils
{
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
                Dbgl("Found all prefab references.");
                foundAllRefs = true;
                break;
            }
        }

        if (!foundAllRefs)
        {
            Dbgl("Could not find all prefab references, attempting alternate prefab detection method.");
            List<string> nullKeys = prefabRefs.Where(key => !key.Value).Select(kvp => kvp.Key).ToList();

            UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(GameObject));

            for (int i = 0; i < array.Length; i++)
            {
                GameObject gameObject = (GameObject)array[i];

                if (!nullKeys.Contains(gameObject.name)) continue;

                prefabRefs[gameObject.name] = gameObject;
                nullKeys.Remove(gameObject.name);

                if (!nullKeys.Any())
                {
                    Dbgl("Found all prefab references on second attempt.");
                    break;
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
            if (!prefabRefs.TryGetValue(er.prefabName, out GameObject targetPrefab))
            {
                targetPrefab = instance.GetPrefab(er.prefabName);
                if (targetPrefab)
                {
                    prefabRefs[er.prefabName] = targetPrefab;
                    Dbgl($"Added {er.prefabName} to prefabRefs");
                    addedExtraResources = true;
                }
                else
                {
                    Dbgl($"Could not find prefab reference for {er.prefabName}, skipping entry.", logErrors || config.EnableDebugMessages, LogLevel.Warning);
                }
            }
        }

        return addedExtraResources;
    }

    internal static void InitPieceRefs()
    {
        Dbgl("InitPieceRefs");

        if (pieceRefs.Count > 0)
        {
            RemoveFromCultivator(pieceRefs.ConvertAll(x => (PrefabDB)x));
            pieceRefs.Clear();
        }

        pieceRefs = GeneratePieceRefs();
    }

    internal static void InitPieces()
    {
        Dbgl("InitPieces");

        foreach (PieceDB pdb in pieceRefs)
        {
            //_ = pdb.Piece; <-- can use discard to init piece reference, still not happy with this
            if (config.DisabledResourceNames.Contains(pdb.key))
            {
                Dbgl($"Resource disabled: {pdb.key}, skipping.");
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
                        UnityEngine.Object.DestroyImmediate(sp.gameObject);
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

                if (pdb.hideWhenPicked)
                {
                    pickable.m_hideWhenPicked = pdb.respawnTime > 0 ? pickable.gameObject : null;
                }

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

    internal static void InitSaplingRefs()
    {
        Dbgl("InitSaplingRefs");

        if (saplingRefs.Count > 0)
        {
            RemoveFromCultivator(saplingRefs.ConvertAll(x => (PrefabDB)x));
            saplingRefs.Clear();
        }

        saplingRefs = GenerateCustomSaplingRefs();
    }

    internal static void InitSaplings()
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

            Shader pieceShader;
            if (!isDedicatedServer)
            {
                AssetID.TryParse("f6de4704e075b4095ae641aed283b641", out AssetID id);
                SoftReference<Shader> shader = new(id);
                shader.Load();
                pieceShader = shader.Asset;
            }
            else
            {
                pieceShader = Shader.Find("Custom/Piece");
            }

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
                        m[0].shader = pieceShader;

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
                        m[0].shader = pieceShader;
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

        if (moddedSaplingRefs.Count == 0) return;

        bool overridesEnabled = config.OverrideModdedSaplings;

        foreach (ModdedPlantDB sdb in moddedSaplingRefs)
        {
            if (!sdb.Prefab)
            {
                Dbgl($"{sdb.key} reference is null, skipping application of modded sapling override settings", true, LogLevel.Warning);
                continue;
            }

            Piece piece = sdb.Prefab.GetComponent<Piece>();
            Plant plant = sdb.Prefab.GetComponent<Plant>();

            plant.m_growTime = overridesEnabled ? config.ModdedSaplingGrowthTime : sdb.growTime;
            plant.m_growTimeMax = overridesEnabled ? config.ModdedSaplingGrowthTime : sdb.growTimeMax;
            plant.m_growRadius = overridesEnabled ? config.ModdedSaplingGrowRadius : sdb.growRadius;
            plant.m_minScale = overridesEnabled ? config.ModdedSaplingMinScale : sdb.minScale;
            plant.m_maxScale = overridesEnabled ? config.ModdedSaplingMaxScale : sdb.maxScale;
            piece.m_onlyInBiome = plant.m_biome = overridesEnabled && !config.EnforceBiomes ? AllBiomes : sdb.biome;
            plant.m_tolerateCold = sdb.tolerateCold || !config.PlantsRequireShielding;
            plant.m_tolerateHeat = sdb.tolerateHeat || !config.PlantsRequireShielding;
            plant.m_destroyIfCantGrow = piece.m_groundOnly = !config.PlaceAnywhere;
        }
    }

    internal static void ModifyTreeDrops()
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

    internal static void InitCrops()
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

        if (moddedCropRefs.Count == 0) return;

        overridesEnabled = overridesEnabled && config.OverrideModdedCrops;

        foreach (ModdedPlantDB cdb in moddedCropRefs)
        {
            if (!cdb.Prefab)
            {
                Dbgl($"{cdb.key} reference is null, skipping application of crop override settings", true, LogLevel.Warning);
                continue;
            }

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

    internal static void InitVines()
    {
        Dbgl("InitVines");

        Plant plant = prefabRefs["VineAsh_sapling"].GetComponent<Plant>();
        Pickable pickable = prefabRefs["VineAsh"].GetComponent<Pickable>();

        plant.m_biome = config.EnforceBiomesVanilla ? /*All but mountain and deep north*/(Heightmap.Biome)827 : (Heightmap.Biome)895;
        plant.m_needCultivatedGround = prefabRefs["VineAsh_sapling"].GetComponent<Piece>().m_cultivatedGroundOnly = !config.EnableCropOverrides || config.CropRequireCultivation;
        plant.m_growTime = config.EnableVineOverrides ? config.VinesGrowthTime : 200f;
        plant.m_growTimeMax = config.EnableVineOverrides ? config.VinesGrowthTime : 300f;
        plant.m_attachDistance = config.EnableVineOverrides ? config.VinesAttachDistance : 1.8f;
        plant.m_growRadiusVines = config.EnableVineOverrides ? config.VineGrowRadius : 1.8f;
        plant.m_tolerateCold = !config.PlantsRequireShielding;

        pickable.m_amount = config.EnableVineOverrides ? config.VineBerryReturn : 3;
        pickable.m_respawnTimeInitMax = config.EnableVineOverrides ? 0 : 150;
        pickable.m_respawnTimeMinutes = config.EnableVineOverrides ? config.VineBerryRespawnTime : 200;
        //vine.m_growTime = vine.m_growTimePerBranch = vine.m_growCheckTime = 15;
    }

    internal static void InitCultivator()
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

    public static void InitLocalization()
    {
        Dbgl("InitLocalization");

        foreach (KeyValuePair<string, string> kvp in DefaultLocalizedStrings)
        {
            Localization.instance.AddWord($"pe{kvp.Key}", kvp.Value);
        }

        DefaultLocalizedStrings.Clear();
    }

    internal static void FullInit(ZNetScene instance)
    {
        Dbgl("Performing full mod initialization");
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
}
