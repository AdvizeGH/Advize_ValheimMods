namespace Advize_PlantEverything;

using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using static StaticMembers;

//Class full of predefined data. Would eventually like to expose these things externally for users to control.
static class StaticContent
{
    internal static Dictionary<string, string> DefaultLocalizedStrings = new()
    {
        { "AncientSaplingName", "Ancient Sapling" },
        { "AncientSaplingDescription", "" },
        { "YggaSaplingName", "Ygga Sapling" },
        { "YggaSaplingDescription", "" },
        { "AutumnBirchSaplingName", "Autumn Birch Sapling" },
        { "AutumnBirchSaplingDescription", "Plains Variant" },
        { "AshwoodSaplingName", "Ashwood Sapling" },
        { "AshwoodSaplingDescription", "" },
        { "VineAshSaplingName", "Custom Ashvine" },
        { "VineAshSaplingDescription", "Plants an Ashvine sapling with the colours defined in the mod config." },
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
        { "AshlandsFernName", "Ashlands Fern" },
        { "AshlandsFernDescription", "Plant a fern native to the ashlands." },
        { "PickableBranchName", "Pickable Branch" },
        { "PickableBranchDescription", "Plant respawning pickable branches." },
        { "PickableStoneName", "Pickable Stone" },
        { "PickableStoneDescription", "Plant pickable stone." },
        { "PickableFlintName", "Pickable Flint" },
        { "PickableFlintDescription", "Plant respawning pickable flint." },
        { "PickableSmokePuffName", "Pickable Smoke Puff" },
        { "PickableSmokePuffDescription", "Plant smoke puffs to grow more pickable smoke puffs." },
        { "PickableFiddleheadName", "Pickable Fiddlehead" },
        { "PickableFiddleheadDescription", "Plant fiddlehead to grow more pickable fiddlehead." },
        { "GrowthTimeHoverPrefix", "Ready in"}
    };

    internal static readonly List<string> VanillaPrefabRefs = new()
    {
        { "Acorn" },
        { "AncientSeed" },
        { "BeechSeeds" },
        { "BirchSeeds" },
        { "FirCone" },
        { "PineCone" },
        { "Sap" },
        { "Pickable_Branch" },
        { "Pickable_Dandelion" },
        { "Pickable_Fiddlehead" },
        { "Pickable_Flint" },
        { "Pickable_Mushroom" },
        { "Pickable_Mushroom_blue" },
        { "Pickable_Mushroom_yellow" },
        { "Pickable_SmokePuff" },
        { "Pickable_Stone" },
        { "Pickable_Thistle" },
        { "Cultivator" },
        { "sfx_build_cultivator" },
        { "vfx_Place_wood_pole" },
        { "sapling_barley" },
        { "sapling_carrot" },
        { "sapling_flax" },
        { "sapling_jotunpuffs" },
        { "sapling_magecap" },
        { "sapling_onion" },
        { "sapling_seedcarrot" },
        { "sapling_seedonion" },
        { "sapling_seedturnip" },
        { "sapling_turnip" },
        { "FernAshlands" },
        { "AshlandsTree3" },
        { "AshlandsTree4" },
        { "AshlandsTree5" },
        { "AshlandsTree6_big" },
        { "Beech1" },
        { "Beech_Sapling" },
        { "Beech_small1" }, // There is apparently a Beech_small2 that I never added. How did that happen?
        { "Birch1" },
        { "Birch1_aut" },
        { "Birch2" },
        { "Birch2_aut" },
        { "Birch_Sapling" },
        { "BlueberryBush" },
        { "Bush01" },
        { "Bush01_heath" },
        { "Bush02_en" },
        { "RaspberryBush" },
        { "CloudberryBush" },
        { "FirTree" },
        { "FirTree_Sapling" },
        { "FirTree_small" },
        { "FirTree_small_dead" },
        { "PineTree_Sapling" },
        { "Pinetree_01" },
        { "YggaShoot1" },
        { "YggaShoot2" },
        { "YggaShoot3" },
        { "YggaShoot_small1" },
        { "shrub_2" },
        { "shrub_2_heath" },
        { "SwampTree1" },
        { "vines" },
        { "VineAsh" },
        { "VineAsh_sapling" },
        { "Oak1" },
        { "Oak_Sapling" }
    };

    internal static readonly List<string> CustomPrefabRefs = new()
    {
        { "Ancient_Sapling" },
        { "Ygga_Sapling" },
        { "Autumn_Birch_Sapling" },
        { "Ashwood_Sapling" },
        { "Pickable_Dandelion_Picked" },
        { "Pickable_Thistle_Picked" },
        { "Pickable_Mushroom_Picked" },
        { "Pickable_Mushroom_yellow_Picked" },
        { "Pickable_Mushroom_blue_Picked" },
        { "Pickable_SmokePuff_Picked" },
        { "Pickable_Fiddlehead_Picked" }
    };

    private static Dictionary<GameObject, GameObject> treesToSeeds;

    internal static Dictionary<GameObject, GameObject> TreesToSeeds => treesToSeeds ??= new()
    {
        { prefabRefs["Birch1"], prefabRefs["BirchSeeds"] },
        { prefabRefs["Birch2"], prefabRefs["BirchSeeds"] },
        { prefabRefs["Birch2_aut"], prefabRefs["BirchSeeds"] },
        { prefabRefs["Birch1_aut"], prefabRefs["BirchSeeds"] },
        { prefabRefs["Oak1"], prefabRefs["Acorn"] },
        { prefabRefs["SwampTree1"], prefabRefs["AncientSeed"] },
        { prefabRefs["Beech1"], prefabRefs["BeechSeeds"] },
        { prefabRefs["Pinetree_01"], prefabRefs["PineCone"] },
        { prefabRefs["FirTree"], prefabRefs["FirCone"] }
    };

    internal static List<ExtraResource> GenerateExampleResources()
    {
        return
        [
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
        ];
    }

    internal static List<PieceDB> GeneratePieceRefs()
    {
        bool enforceBiomes = config.EnforceBiomes;

        List<PieceDB> newList =
        [
            new PieceDB
            {
                key = "RaspberryBush",
                ResourceCost = config.RaspberryCost,
                resourceReturn = config.RaspberryReturn,
                respawnTime = config.RaspberryRespawnTime,
                biome = enforceBiomes ? Heightmap.Biome.Meadows : 0,
                icon = true,
                isGrounded = true,
                recover = config.RecoverResources
            },
            new PieceDB
            {
                key = "BlueberryBush",
                ResourceCost = config.BlueberryCost,
                resourceReturn = config.BlueberryReturn,
                respawnTime = config.BlueberryRespawnTime,
                biome = enforceBiomes ? Heightmap.Biome.BlackForest : 0,
                icon = true,
                isGrounded = true,
                recover = config.RecoverResources
            },
            new PieceDB
            {
                key = "CloudberryBush",
                ResourceCost = config.CloudberryCost,
                resourceReturn = config.CloudberryReturn,
                respawnTime = config.CloudberryRespawnTime,
                biome = enforceBiomes ? Heightmap.Biome.Plains : 0,
                icon = true,
                isGrounded = true,
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
                biome = enforceBiomes ? Heightmap.Biome.BlackForest : 0,
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
                biome = enforceBiomes ? Heightmap.Biome.Meadows : 0,
                recover = config.RecoverResources,
                Name = "PickableDandelion",
                isGrounded = true
            },
            new PieceDB
            {
                key = "Pickable_SmokePuff",
                ResourceCost = config.SmokePuffCost,
                resourceReturn = config.SmokePuffReturn,
                respawnTime = config.SmokePuffRespawnTime,
                biome = enforceBiomes ? Heightmap.Biome.AshLands : 0,
                recover = config.RecoverResources,
                Name = "PickableSmokePuff",
                isGrounded = true,
            },
            new PieceDB
            {
                key = "Pickable_Fiddlehead",
                ResourceCost = config.FiddleheadCost,
                resourceReturn = config.FiddleheadReturn,
                respawnTime = config.FiddleheadRespawnTime,
                biome = enforceBiomes ? Heightmap.Biome.AshLands : 0,
                icon = true,
                recover = config.RecoverResources,
                Name = "PickableFiddlehead",
                isGrounded = true,
                extraDrops = true
            }
        ];

        if (config.EnableMiscFlora)
        {
            newList.AddRange(
            [
                new()
                {
                    key = "Beech_small1",
                    Resource = new KeyValuePair<string, int>("BeechSeeds", 1),
                    icon = true,
                    Name = "BeechSmall",
                    canBeRemoved = false
                },
                new()
                {
                    key = "FirTree_small",
                    Resource = new KeyValuePair<string, int>("FirCone", 1),
                    icon = true,
                    Name = "FirSmall",
                    canBeRemoved = false
                },
                new()
                {
                    key = "FirTree_small_dead",
                    Resource = new KeyValuePair<string, int>("FirCone", 1),
                    icon = true,
                    Name = "FirSmallDead",
                    canBeRemoved = false
                },
                new()
                {
                    key = "Bush01",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    canBeRemoved = false
                },
                new()
                {
                    key = "Bush01_heath",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    Name = "Bush02",
                    canBeRemoved = false
                },
                new()
                {
                    key = "Bush02_en",
                    Resource = new KeyValuePair<string, int>("Wood", 3),
                    icon = true,
                    Name = "PlainsBush",
                    canBeRemoved = false
                },
                new()
                {
                    key = "shrub_2",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    Name = "Shrub01",
                    canBeRemoved = false
                },
                new()
                {
                    key = "shrub_2_heath",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    Name = "Shrub02",
                    canBeRemoved = false
                },
                new()
                {
                    key = "YggaShoot_small1",
                    Resources = new Dictionary<string, int>() { { "YggdrasilWood", 1 }, { "Wood", 2 } },
                    icon = true,
                    Name = "YggaShoot",
                    canBeRemoved = false
                },
                new()
                {
                    key = "vines",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    recover = true,
                    Name = "Vines",
                    isGrounded = false,
                    snapPoints = new()
                    {
                        { new Vector3(1f, 0.5f, 0) },
                        { new Vector3(-1f, 0.5f, 0) },
                        { new Vector3(1f, -1f, 0) },
                        { new Vector3(-1f, -1f, 0) }
                    }
                },
                new()
                {
                    key = "FernAshlands",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    Name = "AshlandsFern",
                    isGrounded = true,
                    canBeRemoved = false
                }
            ]);
        }

        if (config.EnableDebris)
        {
            newList.AddRange(
            [
                new()
                {
                    key = "Pickable_Branch",
                    ResourceCost = config.PickableBranchCost,
                    resourceReturn = config.PickableBranchReturn,
                    respawnTime = config.PickableBranchRespawnTime,
                    recover = config.RecoverResources,
                    Name = "PickableBranch",
                    isGrounded = true
                },
                new()
                {
                    key = "Pickable_Stone",
                    ResourceCost = config.PickableStoneCost,
                    resourceReturn = config.PickableStoneReturn,
                    respawnTime = config.PickableStoneRespawnTime,
                    recover = config.RecoverResources,
                    Name = "PickableStone",
                    isGrounded = true,
                    hideWhenPicked = true
                },
                new()
                {
                    key = "Pickable_Flint",
                    ResourceCost = config.PickableFlintCost,
                    resourceReturn = config.PickableFlintReturn,
                    respawnTime = config.PickableFlintRespawnTime,
                    recover = config.RecoverResources,
                    Name = "PickableFlint",
                    isGrounded = true
                }
            ]);
        }

        if (config.EnableExtraResources)
        {
            List<string> potentialNewLayers = [.. layersForPieceRemoval];
            bool queueReattempt = false;

            foreach (ExtraResource er in deserializedExtraResources)
            {
                if (!prefabRefs.ContainsKey(er.prefabName) || !prefabRefs[er.prefabName])
                {
                    Dbgl($"{er.prefabName} is not in dictionary of prefab references or has a null value", level: LogLevel.Warning);
                    queueReattempt = true;
                    continue;
                }

                if (!ObjectDB.instance?.GetItemPrefab(er.resourceName)?.GetComponent<ItemDrop>())
                {
                    Dbgl($"{er.prefabName}'s required resource {er.resourceName} not found", level: LogLevel.Warning);
                    queueReattempt = true;
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

                foreach (Collider c in prefabRefs[er.prefabName].GetComponentsInChildren<Collider>())
                {
                    string layer = LayerMask.LayerToName(c.gameObject.layer);
                    //Dbgl($"Layer to potentially add is {layer}");
                    potentialNewLayers.Add(layer);
                }
            }

            layersForPieceRemoval = potentialNewLayers.Distinct().ToArray();
            resolveMissingReferences = queueReattempt;
        }

        return newList;
    }

    internal static List<SaplingDB> GenerateCustomSaplingRefs()
    {
        return
        [
            new SaplingDB
            {
                key = "Ashwood_Sapling",
                biome = config.EnforceBiomes ? TemperateBiomes | Heightmap.Biome.AshLands : AllBiomes,
                source = "AshlandsTree3",
                Resources = new Dictionary<string, int>() { { "BeechSeeds", 1 }, { "SulfurStone", 1 } },
                icon = true,
                growTime = config.AshwoodGrowthTime,
                growRadius = config.AshwoodGrowRadius,
                minScale = config.AshwoodMinScale,
                maxScale = config.AshwoodMaxScale,
                grownPrefabs = [prefabRefs["AshlandsTree3"], prefabRefs["AshlandsTree4"], prefabRefs["AshlandsTree5"], prefabRefs["AshlandsTree6_big"]],
                tolerateHeat = true
            },
            new SaplingDB
            {
                key = "Ygga_Sapling",
                biome = config.EnforceBiomes ? TemperateBiomes | Heightmap.Biome.Mistlands : AllBiomes,
                source = "YggaShoot_small1",
                Resource = new KeyValuePair<string, int>("Sap", 1),
                icon = true,
                growTime = config.YggaGrowthTime,
                growRadius = config.YggaGrowRadius,
                minScale = config.YggaMinScale,
                maxScale = config.YggaMaxScale,
                grownPrefabs = [prefabRefs["YggaShoot1"], prefabRefs["YggaShoot2"], prefabRefs["YggaShoot3"]]
            },
            new SaplingDB
            {
                key = "Ancient_Sapling",
                biome = config.EnforceBiomes ? TemperateBiomes | Heightmap.Biome.Swamp : AllBiomes,
                source = "SwampTree1",
                Resource = new KeyValuePair<string, int>("AncientSeed", 1),
                icon = true,
                growTime = config.AncientGrowthTime,
                growRadius = config.AncientGrowRadius,
                minScale = config.AncientMinScale,
                maxScale = config.AncientMaxScale,
                grownPrefabs = [prefabRefs["SwampTree1"]]
            },
            new SaplingDB
            {
                key = "Autumn_Birch_Sapling",
                biome = config.EnforceBiomes ? TemperateBiomes : AllBiomes,
                source = "Birch1_aut",
                Resource = new KeyValuePair<string, int>("BirchSeeds", 1),
                icon = true,
                growTime = config.AutumnBirchGrowthTime,
                growRadius = config.AutumnBirchGrowRadius,
                minScale = config.AutumnBirchMinScale,
                maxScale = config.AutumnBirchMaxScale,
                grownPrefabs = [prefabRefs["Birch1_aut"], prefabRefs["Birch2_aut"]]
            }
        ];
    }

    internal static List<SaplingDB> GenerateVanillaSaplingRefs()
    {
        return
        [
            new SaplingDB
            {
                key = "Beech_Sapling",
                biome = config.EnforceBiomesVanilla ? TemperateBiomes : AllBiomes,
                growTime = config.BeechGrowthTime,
                growRadius = config.BeechGrowRadius,
                minScale = config.BeechMinScale,
                maxScale = config.BeechMaxScale
            },
            new SaplingDB
            {
                key = "PineTree_Sapling",
                biome = config.EnforceBiomesVanilla ? TemperateBiomes : AllBiomes,
                growTime = config.PineGrowthTime,
                growRadius = config.PineGrowRadius,
                minScale = config.PineMinScale,
                maxScale = config.PineMaxScale
            },
            new SaplingDB
            {
                key = "FirTree_Sapling",
                biome = config.EnforceBiomesVanilla ? TemperateBiomes | Heightmap.Biome.Mountain : AllBiomes,
                growTime = config.FirGrowthTime,
                growRadius = config.FirGrowRadius,
                minScale = config.FirMinScale,
                maxScale = config.FirMaxScale,
                tolerateCold = true
            },
            new SaplingDB
            {
                key = "Birch_Sapling",
                biome = config.EnforceBiomesVanilla ? TemperateBiomes : AllBiomes,
                growTime = config.BirchGrowthTime,
                growRadius = config.BirchGrowRadius,
                minScale = config.BirchMinScale,
                maxScale = config.BirchMaxScale
            },
            new SaplingDB
            {
                key = "Oak_Sapling",
                biome = config.EnforceBiomesVanilla ? TemperateBiomes : AllBiomes,
                growTime = config.OakGrowthTime,
                growRadius = config.OakGrowRadius,
                minScale = config.OakMinScale,
                maxScale = config.OakMaxScale
            }
        ];
    }

    internal static List<PrefabDB> GenerateCropRefs()
    {
        bool overridesEnabled = config.EnableCropOverrides;
        bool enforceBiomesVanilla = config.EnforceBiomesVanilla;

        return
        [
            new PrefabDB
            {
                key = "sapling_barley",
                biome = enforceBiomesVanilla ? Heightmap.Biome.Plains : AllBiomes,
                resourceCost = overridesEnabled ? config.BarleyCost : 1,
                resourceReturn = overridesEnabled ? config.BarleyReturn : 2
            },
            new PrefabDB
            {
                key = "sapling_carrot",
                biome = enforceBiomesVanilla ? TemperateBiomes : AllBiomes,
                resourceCost = overridesEnabled ? config.CarrotCost : 1,
                resourceReturn = overridesEnabled ? config.CarrotReturn : 1
            },
            new PrefabDB
            {
                key = "sapling_flax",
                biome = enforceBiomesVanilla ? Heightmap.Biome.Plains : AllBiomes,
                resourceCost = overridesEnabled ? config.FlaxCost : 1,
                resourceReturn = overridesEnabled ? config.FlaxReturn : 2
            },
            new PrefabDB
            {
                key = "sapling_onion",
                biome = enforceBiomesVanilla ? TemperateBiomes : AllBiomes,
                resourceCost = overridesEnabled ? config.OnionCost : 1,
                resourceReturn = overridesEnabled ? config.OnionReturn : 1
            },
            new PrefabDB
            {
                key = "sapling_seedcarrot",
                biome = enforceBiomesVanilla ? TemperateBiomes : AllBiomes,
                resourceCost = overridesEnabled ? config.SeedCarrotCost : 1,
                resourceReturn = overridesEnabled ? config.SeedCarrotReturn : 3
            },
            new PrefabDB
            {
                key = "sapling_seedonion",
                biome = enforceBiomesVanilla ? TemperateBiomes : AllBiomes,
                resourceCost = overridesEnabled ? config.SeedOnionCost : 1,
                resourceReturn = overridesEnabled ? config.SeedOnionReturn : 3
            },
            new PrefabDB
            {
                key = "sapling_seedturnip",
                biome = enforceBiomesVanilla ? TemperateBiomes | Heightmap.Biome.Swamp | Heightmap.Biome.Mistlands : AllBiomes,
                resourceCost = overridesEnabled ? config.SeedTurnipCost : 1,
                resourceReturn = overridesEnabled ? config.SeedTurnipReturn : 3
            },
            new PrefabDB
            {
                key = "sapling_turnip",
                biome = enforceBiomesVanilla ? TemperateBiomes | Heightmap.Biome.Swamp | Heightmap.Biome.Mistlands : AllBiomes,
                resourceCost = overridesEnabled ? config.TurnipCost : 1,
                resourceReturn = overridesEnabled ? config.TurnipReturn : 1
            },
            new PrefabDB
            {
                key = "sapling_magecap",
                biome = enforceBiomesVanilla ? Heightmap.Biome.Mistlands : AllBiomes,
                resourceCost = overridesEnabled ? config.MagecapCost : 1,
                resourceReturn = overridesEnabled ? config.MagecapReturn : 1,
                extraDrops = true
            },
            new PrefabDB
            {
                key = "sapling_jotunpuffs",
                biome = enforceBiomesVanilla ? Heightmap.Biome.Mistlands : AllBiomes,
                resourceCost = overridesEnabled ? config.JotunPuffsCost : 1,
                resourceReturn = overridesEnabled ? config.JotunPuffsReturn : 1,
                extraDrops = true
            }
        ];
    }

    internal static List<ModdedPlantDB> GenerateCustomPlantRefs(List<GameObject> customPlants)
    {
        List<ModdedPlantDB> newList = [];

        foreach (GameObject customPlant in customPlants)
        {
			//if (!customPlant.TryGetComponent<Plant>(out Plant plant) || !customPlant.TryGetComponent<Piece>(out _) || !plant.m_grownPrefabs[0].TryGetComponent<Pickable>(out _))
			//    continue;
			Plant plant = customPlant.GetComponent<Plant>();

            newList.Add(new ModdedPlantDB
            {
                key = customPlant.name,
                biome = plant.m_biome,
                //resourceCost = piece.m_resources[0].m_amount,
                //resourceReturn = pickable.m_amount,
                tolerateCold = plant.m_tolerateCold,
                tolerateHeat = plant.m_tolerateHeat,
                //needCultivatedGround = plant.m_needCultivatedGround,
                minScale = plant.m_minScale,
                maxScale = plant.m_maxScale,
                growTime = plant.m_growTime,
                growTimeMax = plant.m_growTimeMax,
                growRadius = plant.m_growRadius
                //extraDrops = pickable.m_extraDrops
            });

            prefabRefs[customPlant.name] = customPlant;
        }

        return newList;
    }
}
