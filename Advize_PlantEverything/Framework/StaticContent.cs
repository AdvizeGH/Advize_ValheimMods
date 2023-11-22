using Advize_PlantEverything.Configuration;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PE = Advize_PlantEverything.PlantEverything;

namespace Advize_PlantEverything.Framework
{
    internal class StaticContent
    {
        private static ModConfig config => PE.Helper.config;

        internal static string[] layersForPieceRemoval = { "item", "piece_nonsolid", "Default_small", "Default" };

        internal static Dictionary<string, string> DefaultLocalizedStrings = new()
        {
            { "AncientSaplingName", "Ancient Sapling" },
            { "AncientSaplingDescription", "" },
            { "YggaSaplingName", "Ygga Sapling" },
            { "YggaSaplingDescription", "" },
            { "AutumnBirchSaplingName", "Autumn Birch Sapling" },
            { "AutumnBirchSaplingDescription", "Plains Variant" },
            { "RaspberryBushName", "Raspberry Bush" },
            { "RaspberryBushDescription", "Plant raspberries to grow raspberry bushes." },
            { "BlueberryBushName", "Blueberry Bush" },
            { "BlueberryBushDescription", "Plant blueberries to grow blueberry bushes." },
            { "CloudberryBushName", "Cloudberry Bush" },
            { "CloudberryBushDescription", "Plant cloudberries to grow cloudberry bushes." },
            { "PickableMushroomName", "Pickable Mushrooms" },
            { "PickableMushroomDescription", "Plant mushrooms to grow more pickable mushrooms." },
            { "PickableYellowMushroomName", "Pickable Yellow Mushrooms" },
            { "PickableYellowMushroomDescription", "Plant yellow mushrooms to grow more pickable yellow mushrooms." },
            { "PickableBlueMushroomName", "Pickable Blue Mushrooms" },
            { "PickableBlueMushroomDescription", "Plant blue mushrooms to grow more pickable blue mushrooms." },
            { "PickableThistleName", "Pickable Thistle" },
            { "PickableThistleDescription", "Plant thistle to grow more pickable thistle." },
            { "PickableDandelionName", "Pickable Dandelion" },
            { "PickableDandelionDescription", "Plant dandelion to grow more pickable dandelion." },
            { "BeechSmallName", "Small Beech Tree" },
            { "BeechSmallDescription", "Plant a small beech tree." },
            { "FirSmallName", "Small Fir Tree" },
            { "FirSmallDescription", "Plant a small fir tree." },
            { "FirSmallDeadName", "Small Dead Fir Tree" },
            { "FirSmallDeadDescription", "Plant a small dead fir tree." },
            { "Bush01Name", "Small Bush 1" },
            { "Bush01Description", "Plant a small bush." },
            { "Bush02Name", "Small Bush 2" },
            { "Bush02Description", "Plant a small bush." },
            { "PlainsBushName", "Small Plains Bush" },
            { "PlainsBushDescription", "Plant a bush native to the plains." },
            { "Shrub01Name", "Small Shrub 1" },
            { "Shrub01Description", "Plant a small shrub." },
            { "Shrub02Name", "Small Shrub 2" },
            { "Shrub02Description", "Plant a small shrub." },
            { "YggaShootName", "Small Ygga Shoot" },
            { "YggaShootDescription", "Plant a small ygga shoot." },
            { "VinesName", "Vines" },
            { "VinesDescription", "Plant vines." },
            { "GlowingMushroomName", "Glowing Mushroom" },
            { "GlowingMushroomDescription", "Plant a large glowing mushroom." },
            { "PickableBranchName", "Pickable Branch" },
            { "PickableBranchDescription", "Plant respawning pickable branches." },
            { "PickableStoneName", "Pickable Stone" },
            { "PickableStoneDescription", "Plant pickable stone." },
            { "PickableFlintName", "Pickable Flint" },
            { "PickableFlintDescription", "Plant respawning pickable flint." }
        };

        internal static readonly List<string> VanillaPrefabRefs = new()
        {
            { "Bush02_en" },
            { "Bush01_heath" },
            { "Bush01" },
            { "GlowingMushroom" },
            { "Pinetree_01" },
            { "FirTree" },
            { "YggaShoot_small1" },
            { "Beech_small1" },
            { "FirTree_small_dead" },
            { "FirTree_small" },
            { "Pickable_Dandelion" },
            { "Sap" },
            { "CloudberryBush" },
            { "vines" },
            { "Cultivator" },
            { "SwampTree1" },
            { "YggaShoot1" },
            { "Beech1" },
            { "Birch2" },
            { "Oak1" },
            { "Birch2_aut" },
            { "Birch1_aut" },
            { "Birch1" },
            { "Pickable_Thistle" },
            { "Pickable_Flint" },
            { "Pickable_Stone" },
            { "FirCone" },
            { "PineCone" },
            { "shrub_2" },
            { "shrub_2_heath" },
            { "BirchSeeds" },
            { "AncientSeed" },
            { "Acorn" },
            { "BeechSeeds" },
            { "Pickable_Branch" },
            { "Pickable_Mushroom" },
            { "BlueberryBush" },
            { "RaspberryBush" },
            { "Pickable_Mushroom_blue" },
            { "Pickable_Mushroom_yellow" },
            { "sapling_seedonion" },
            { "Beech_Sapling" },
            { "PineTree_Sapling" },
            { "FirTree_Sapling" },
            { "sapling_onion" },
            { "sapling_turnip" },
            { "Oak_Sapling" },
            { "sapling_barley" },
            { "sapling_jotunpuffs" },
            { "Birch_Sapling" },
            { "sapling_carrot" },
            { "sapling_seedcarrot" },
            { "sapling_flax" },
            { "sapling_magecap" },
            { "sapling_seedturnip" },
            { "vfx_Place_wood_pole" },
            { "sfx_build_cultivator" },
            { "YggaShoot3" },
            { "YggaShoot2" }
        };

        internal static readonly List<string> CustomPrefabRefs = new()
        {
            { "Ancient_Sapling" },
            { "Ygga_Sapling" },
            { "Autumn_Birch_Sapling" },
            { "Pickable_Dandelion_Picked" },
            { "Pickable_Thistle_Picked" },
            { "Pickable_Mushroom_Picked" },
            { "Pickable_Mushroom_yellow_Picked" },
            { "Pickable_Mushroom_blue_Picked" }
        };

        private static Dictionary<GameObject, GameObject> seedDropsByTarget;

        internal static Dictionary<GameObject, GameObject> GetSeedDropsByTarget => seedDropsByTarget ??= new()
        {
            { PE.prefabRefs["Birch1"], PE.prefabRefs["BirchSeeds"] },
            { PE.prefabRefs["Birch2"], PE.prefabRefs["BirchSeeds"] },
            { PE.prefabRefs["Birch2_aut"], PE.prefabRefs["BirchSeeds"] },
            { PE.prefabRefs["Birch1_aut"], PE.prefabRefs["BirchSeeds"] },
            { PE.prefabRefs["Oak1"], PE.prefabRefs["Acorn"] },
            { PE.prefabRefs["SwampTree1"], PE.prefabRefs["AncientSeed"] },
            { PE.prefabRefs["Beech1"], PE.prefabRefs["BeechSeeds"] },
            { PE.prefabRefs["Pinetree_01"], PE.prefabRefs["PineCone"] },
            { PE.prefabRefs["FirTree"], PE.prefabRefs["FirCone"] }
        };

        internal static List<ExtraResource> GenerateExampleResources()
        {
            return new()
            {
                new ExtraResource
                {
                    prefabName = "PE_FakePrefab1",
                    resourceName = "PretendSeeds1",
                    resourceCost = 1,
                    groundOnly = true,
                    pieceName = "PickableMushrooms",
                    pieceDescription = "Plant this to grow more pickable mushrooms."
                },
                new ExtraResource
                {
                    prefabName = "PE_FakePrefab2",
                    resourceName = "PretendSeeds2",
                    resourceCost = 2,
                    groundOnly = false
                }
            };
        }

        internal static List<PieceDB> GeneratePieceRefs()
        {
            bool enforceBiomes = config.EnforceBiomes;

            List<PieceDB> newList = new()
            {
                new PieceDB
                {
                    key = "RaspberryBush",
                    ResourceCost = config.RaspberryCost,
                    resourceReturn = config.RaspberryReturn,
                    respawnTime = config.RaspberryRespawnTime,
                    biome = enforceBiomes ? (int)Heightmap.Biome.Meadows : 0,
                    icon = true,
                    recover = config.RecoverResources
                },
                new PieceDB
                {
                    key = "BlueberryBush",
                    ResourceCost = config.BlueberryCost,
                    resourceReturn = config.BlueberryReturn,
                    respawnTime = config.BlueberryRespawnTime,
                    biome = enforceBiomes ? (int)Heightmap.Biome.BlackForest : 0,
                    icon = true,
                    recover = config.RecoverResources
                },
                new PieceDB
                {
                    key = "CloudberryBush",
                    ResourceCost = config.CloudberryCost,
                    resourceReturn = config.CloudberryReturn,
                    respawnTime = config.CloudberryRespawnTime,
                    biome = enforceBiomes ? (int)Heightmap.Biome.Plains : 0,
                    icon = true,
                    recover = config.RecoverResources
                },
                new PieceDB
                {
                    key = "Pickable_Mushroom",
                    ResourceCost = config.MushroomCost,
                    resourceReturn = config.MushroomReturn,
                    respawnTime = config.MushroomRespawnTime,
                    recover = config.RecoverResources,
                    Name = "PickableMushroom",
                    isGrounded = true
                },
                new PieceDB
                {
                    key = "Pickable_Mushroom_yellow",
                    ResourceCost = config.YellowMushroomCost,
                    resourceReturn = config.YellowMushroomReturn,
                    respawnTime = config.YellowMushroomRespawnTime,
                    recover = config.RecoverResources,
                    Name = "PickableYellowMushroom",
                    isGrounded = true
                },
                new PieceDB
                {
                    key = "Pickable_Mushroom_blue",
                    ResourceCost = config.BlueMushroomCost,
                    resourceReturn = config.BlueMushroomReturn,
                    respawnTime = config.BlueMushroomRespawnTime,
                    recover = config.RecoverResources,
                    Name = "PickableBlueMushroom",
                    isGrounded = true
                },
                new PieceDB
                {
                    key = "Pickable_Thistle",
                    ResourceCost = config.ThistleCost,
                    resourceReturn = config.ThistleReturn,
                    respawnTime = config.ThistleRespawnTime,
                    biome = enforceBiomes ? (int)Heightmap.Biome.BlackForest : 0,
                    recover = config.RecoverResources,
                    Name = "PickableThistle",
                    isGrounded = true
                },
                new PieceDB
                {
                    key = "Pickable_Dandelion",
                    ResourceCost = config.DandelionCost,
                    resourceReturn = config.DandelionReturn,
                    respawnTime = config.DandelionRespawnTime,
                    biome = enforceBiomes ? (int)Heightmap.Biome.Meadows : 0,
                    recover = config.RecoverResources,
                    Name = "PickableDandelion",
                    isGrounded = true
                }
            };

            if (config.EnableMiscFlora)
            {
                newList.AddRange(new List<PieceDB>()
                {
                    new PieceDB
                    {
                        key = "Beech_small1",
                        Resource = new KeyValuePair<string, int>("BeechSeeds", 1),
                        icon = true,
                        Name = "BeechSmall",
                        canBeRemoved = false
                    },
                    new PieceDB
                    {
                        key = "FirTree_small",
                        Resource = new KeyValuePair<string, int>("FirCone", 1),
                        icon = true,
                        Name = "FirSmall",
                        canBeRemoved = false
                    },
                    new PieceDB
                    {
                        key = "FirTree_small_dead",
                        Resource = new KeyValuePair<string, int>("FirCone", 1),
                        icon = true,
                        Name = "FirSmallDead",
                        canBeRemoved = false
                    },
                    new PieceDB
                    {
                        key = "Bush01",
                        Resource = new KeyValuePair<string, int>("Wood", 2),
                        icon = true,
                        canBeRemoved = false
                    },
                    new PieceDB
                    {
                        key = "Bush01_heath",
                        Resource = new KeyValuePair<string, int>("Wood", 2),
                        icon = true,
                        Name = "Bush02",
                        canBeRemoved = false
                    },
                    new PieceDB
                    {
                        key = "Bush02_en",
                        Resource = new KeyValuePair<string, int>("Wood", 3),
                        icon = true,
                        Name = "PlainsBush",
                        canBeRemoved = false
                    },
                    new PieceDB
                    {
                        key = "shrub_2",
                        Resource = new KeyValuePair<string, int>("Wood", 2),
                        icon = true,
                        Name = "Shrub01",
                        canBeRemoved = false
                    },
                    new PieceDB
                    {
                        key = "shrub_2_heath",
                        Resource = new KeyValuePair<string, int>("Wood", 2),
                        icon = true,
                        Name = "Shrub02",
                        canBeRemoved = false
                    },
                    new PieceDB
                    {
                        key = "YggaShoot_small1",
                        Resources = new Dictionary<string, int>() { { "YggdrasilWood", 1 }, { "Wood", 2 } },
                        icon = true,
                        Name = "YggaShoot",
                        canBeRemoved = false
                    },
                    new PieceDB
                    {
                        key = "vines",
                        Resource = new KeyValuePair<string, int>("Wood", 2),
                        icon = true,
                        recover = true,
                        Name = "Vines",
                        isGrounded = false,
                        points = new()
                        {
                            { new Vector3(1f, 0.5f, 0) },
                            { new Vector3(-1f, 0.5f, 0) },
                            { new Vector3(1f, -1f, 0) },
                            { new Vector3(-1f, -1f, 0) }
                        }
                    },
                    new PieceDB
                    {
                        key = "GlowingMushroom",
                        Resources = new Dictionary<string, int>() { { "MushroomYellow", 3 }, { "BoneFragments", 1 }, { "Ooze", 1 } },
                        icon = true,
                        recover = true,
                        isGrounded = true
                    },
                    new PieceDB
                    {
                        key = "Pickable_Branch",
                        ResourceCost = config.PickableBranchCost,
                        resourceReturn = config.PickableBranchReturn,
                        respawnTime = 240,
                        recover = config.RecoverResources,
                        Name = "PickableBranch",
                        isGrounded = true
                    },
                    new PieceDB
                    {
                        key = "Pickable_Stone",
                        ResourceCost = config.PickableStoneCost,
                        resourceReturn = config.PickableStoneReturn,
                        respawnTime = 0,
                        recover = config.RecoverResources,
                        Name = "PickableStone",
                        isGrounded = true
                    },
                    new PieceDB
                    {
                        key = "Pickable_Flint",
                        ResourceCost = config.PickableFlintCost,
                        resourceReturn = config.PickableFlintReturn,
                        respawnTime = 240,
                        recover = config.RecoverResources,
                        Name = "PickableFlint",
                        isGrounded = true
                    }
                });
            }
            // vvvvvvvv Move lots of this to an AddExtraResource() PluginHelper method
            if (config.EnableExtraResources)
            {
                List<string> potentialNewLayers = layersForPieceRemoval.ToList();

                foreach (ExtraResource er in PE.deserializedExtraResources)
                {
                    if (!PE.prefabRefs.ContainsKey(er.prefabName) || !PE.prefabRefs[er.prefabName])
                    {
                        PE.Dbgl($"{er.prefabName} is not in dictionary of prefab references or has a null value", true, LogLevel.Warning);
                        continue;
                    }

                    if (!ObjectDB.instance?.GetItemPrefab(er.resourceName)?.GetComponent<ItemDrop>()/* == null*/)
                    {
                        PE.Dbgl($"{er.prefabName}'s required resource {er.resourceName} not found", true, LogLevel.Warning);
                        continue;
                    }

                    newList.Add(new PieceDB()
                    {
                        key = er.prefabName,
                        Resource = new KeyValuePair<string, int>(er.resourceName, er.resourceCost),
                        isGrounded = er.groundOnly,
                        extraResource = true,
                        pieceName = er.pieceName == "" ? er.prefabName : er.pieceName,
                        pieceDescription = er.pieceDescription
                    });

                    foreach (Collider c in PE.prefabRefs[er.prefabName].GetComponentsInChildren<Collider>())
                    {
                        string layer = LayerMask.LayerToName(c.gameObject.layer);
                        //Dbgl($"Layer to potentially add is {layer}");
                        potentialNewLayers.Add(layer);
                    }
                }

                layersForPieceRemoval = potentialNewLayers.Distinct().ToArray();
            }

            return newList;
        }

        internal static List<SaplingDB> GenerateSaplingRefs()
        {
            return new()
            {
                new SaplingDB
                {
                    key = "Ygga_Sapling",
                    source = "YggaShoot_small1",
                    resource = "Sap",
                    resourceCost = 1,
                    biome = config.EnforceBiomes ? (int)Heightmap.Biome.Mistlands : 895,
                    icon = true,
                    growTime = config.YggaGrowthTime,
                    growRadius = config.YggaGrowRadius,
                    minScale = config.YggaMinScale,
                    maxScale = config.YggaMaxScale,
                    grownPrefabs = new GameObject[] { PE.prefabRefs["YggaShoot1"], PE.prefabRefs["YggaShoot2"], PE.prefabRefs["YggaShoot3"] }
                },
                new SaplingDB
                {
                    key = "Ancient_Sapling",
                    source = "SwampTree1",
                    resource = "AncientSeed",
                    resourceCost = 1,
                    biome = config.EnforceBiomes ? (int)Heightmap.Biome.Swamp : 895,
                    icon = true,
                    growTime = config.AncientGrowthTime,
                    growRadius = config.AncientGrowRadius,
                    minScale = config.AncientMinScale,
                    maxScale = config.AncientMaxScale,
                    grownPrefabs = new GameObject[] { PE.prefabRefs["SwampTree1"] }
                },
                new SaplingDB
                {
                    key = "Autumn_Birch_Sapling",
                    source = "Birch1_aut",
                    resource = "BirchSeeds",
                    resourceCost = 1,
                    biome = config.EnforceBiomes ? (int)Heightmap.Biome.Plains : 895,
                    icon = true,
                    growTime = config.AutumnBirchGrowthTime,
                    growRadius = config.AutumnBirchGrowRadius,
                    minScale = config.AutumnBirchMinScale,
                    maxScale = config.AutumnBirchMaxScale,
                    grownPrefabs = new GameObject[] { PE.prefabRefs["Birch1_aut"], PE.prefabRefs["Birch2_aut"] }
                }
            };
        }

        internal static List<SaplingDB> GenerateVanillaSaplingRefs()
        {
            return new()
            {
                new SaplingDB
                {
                    key = "Beech_Sapling",
                    growTime = config.BeechGrowthTime,
                    growRadius = config.BeechGrowRadius,
                    minScale = config.BeechMinScale,
                    maxScale = config.BeechMaxScale
                },
                new SaplingDB
                {
                    key = "PineTree_Sapling",
                    growTime = config.PineGrowthTime,
                    growRadius = config.PineGrowRadius,
                    minScale = config.PineMinScale,
                    maxScale = config.PineMaxScale
                },
                new SaplingDB
                {
                    key = "FirTree_Sapling",
                    growTime = config.FirGrowthTime,
                    growRadius = config.FirGrowRadius,
                    minScale = config.FirMinScale,
                    maxScale = config.FirMaxScale
                },
                new SaplingDB
                {
                    key = "Birch_Sapling",
                    growTime = config.BirchGrowthTime,
                    growRadius = config.BirchGrowRadius,
                    minScale = config.BirchMinScale,
                    maxScale = config.BirchMaxScale
                },
                new SaplingDB
                {
                    key = "Oak_Sapling",
                    growTime = config.OakGrowthTime,
                    growRadius = config.OakGrowRadius,
                    minScale = config.OakMinScale,
                    maxScale = config.OakMaxScale
                }
            };
        }

        internal static List<PrefabDB> GenerateCropRefs()
        {
            bool overridesEnabled = config.EnableCropOverrides;
            return new()
            {
                new PrefabDB
                {
                    key = "sapling_barley",
                    resourceCost = overridesEnabled ? config.BarleyCost : 1,
                    resourceReturn = overridesEnabled ? config.BarleyReturn : 2
                },
                new PrefabDB
                {
                    key = "sapling_carrot",
                    resourceCost = overridesEnabled ? config.CarrotCost : 1,
                    resourceReturn = overridesEnabled ? config.CarrotReturn : 1
                },
                new PrefabDB
                {
                    key = "sapling_flax",
                    resourceCost = overridesEnabled ? config.FlaxCost : 1,
                    resourceReturn = overridesEnabled ? config.FlaxReturn : 2
                },
                new PrefabDB
                {
                    key = "sapling_onion",
                    resourceCost = overridesEnabled ? config.OnionCost : 1,
                    resourceReturn = overridesEnabled ? config.OnionReturn : 1
                },
                new PrefabDB
                {
                    key = "sapling_seedcarrot",
                    resourceCost = overridesEnabled ? config.SeedCarrotCost : 1,
                    resourceReturn = overridesEnabled ? config.SeedCarrotReturn : 3
                },
                new PrefabDB
                {
                    key = "sapling_seedonion",
                    resourceCost = overridesEnabled ? config.SeedOnionCost : 1,
                    resourceReturn = overridesEnabled ? config.SeedOnionReturn : 3
                },
                new PrefabDB
                {
                    key = "sapling_seedturnip",
                    resourceCost = overridesEnabled ? config.SeedTurnipCost : 1,
                    resourceReturn = overridesEnabled ? config.SeedTurnipReturn : 3
                },
                new PrefabDB
                {
                    key = "sapling_turnip",
                    resourceCost = overridesEnabled ? config.TurnipCost : 1,
                    resourceReturn = overridesEnabled ? config.TurnipReturn : 1
                },
                new PrefabDB
                {
                    key = "sapling_magecap",
                    resourceCost = overridesEnabled ? config.MagecapCost : 1,
                    resourceReturn = overridesEnabled ? config.MagecapReturn : 1,
                    extraDrops = true
                },
                new PrefabDB
                {
                    key = "sapling_jotunpuffs",
                    resourceCost = overridesEnabled ? config.JotunPuffsCost : 1,
                    resourceReturn = overridesEnabled ? config.JotunPuffsReturn : 1,
                    extraDrops = true
                }
            };
        }
    }
}
