namespace Advize_PlantEverything;

using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlantEverything;

static class StaticContent
{
    private const Heightmap.Biome TemperateBiomes = Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest | Heightmap.Biome.Plains;
    private const Heightmap.Biome AllBiomes = (Heightmap.Biome)895/*GetBiomeMask((Heightmap.Biome[])System.Enum.GetValues(typeof(Heightmap.Biome)))*/;

    internal static readonly int ModdedVineHash = "pe_ModdedVine".GetStableHashCode();
    internal static readonly int VineColorHash = "pe_VineColor".GetStableHashCode();
    internal static readonly int BerryColor1Hash = "pe_BerryColor1".GetStableHashCode();
    internal static readonly int BerryColor2Hash = "pe_BerryColor2".GetStableHashCode();
    internal static readonly int BerryColor3Hash = "pe_BerryColor3".GetStableHashCode();
    internal static readonly Vector3 ColorBlackVector3 = new(0.00012345f, 0.00012345f, 0.00012345f);

    internal static readonly Color ColorVineGreen = new(0.729f, 1, 0.525f, 1);
    internal static readonly Color ColorVineRed = new(0.867f, 0, 0.278f, 1);
    internal static readonly Color ColorBerryGreen = new(1, 1, 1, 1);
    internal static readonly Color ColorBerryRed = new(1, 0, 0, 1);

    internal static bool OverrideVines => config.AshVineStyle != AshVineStyle.Custom;
    internal static bool OverrideBerries => config.VineBerryStyle != VineBerryStyle.Custom;

    internal static Color VineColorFromConfig => config.AshVineStyle == AshVineStyle.Custom ?
            config.VinesColor : config.AshVineStyle == AshVineStyle.MeadowsGreen ?
            ColorVineGreen : ColorVineRed;

    internal static List<Color> BerryColorsFromConfig => config.VineBerryStyle == VineBerryStyle.Custom ?
            config.BerryColors.Select(x => x.Value).ToList() : config.VineBerryStyle == VineBerryStyle.RedGrapes ?
            Enumerable.Repeat(ColorBerryRed, 3).ToList() : Enumerable.Repeat(ColorBerryGreen, 3).ToList();

    internal static Vector3 ColorToVector3(Color color) => color == Color.black ? ColorBlackVector3 : new(color.r, color.g, color.b);
    internal static Color Vector3ToColor(Vector3 vector3) => vector3 == ColorBlackVector3 ? Color.black : new(vector3.x, vector3.y, vector3.z);

    //public static bool TryGetVector3(this ZDO zdo, int keyHashCode, out Vector3 value)
    //{
    //	if (ZDOExtraData.s_vec3.TryGetValue(zdo.m_uid, out BinarySearchDictionary<int, Vector3> values)
    //		&& values.TryGetValue(keyHashCode, out value))
    //	{
    //		return true;
    //	}

    //	value = default;
    //	return false;
    //}

    //private static Heightmap.Biome GetBiomeMask(Heightmap.Biome[] biomes)
    //{
    //	Heightmap.Biome biomeMask = 0;

    //	foreach (Heightmap.Biome biome in biomes)
    //	{
    //		biomeMask |= biome;
    //	}

    //	return biomeMask;
    //}

    internal static string[] layersForPieceRemoval = ["item", "piece_nonsolid", "Default_small", "Default"];

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
        { "PickableFiddleheadDescription", "Plant fiddlehead to grow more pickable fiddlehead." }
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
                recover = config.RecoverResources,
                Name = "PickableSmokePuff",
                isGrounded = true,
                extraDrops = true
            },
            new PieceDB
            {
                key = "Pickable_Fiddlehead",
                ResourceCost = config.FiddleheadCost,
                resourceReturn = config.FiddleheadReturn,
                respawnTime = config.FiddleheadRespawnTime,
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
                    canBeRemoved = false
                },
                new()
                {
                    key = "Pickable_Branch",
                    ResourceCost = config.PickableBranchCost,
                    resourceReturn = config.PickableBranchReturn,
                    respawnTime = 240,
                    recover = config.RecoverResources,
                    Name = "PickableBranch",
                    isGrounded = true
                },
                new()
                {
                    key = "Pickable_Stone",
                    ResourceCost = config.PickableStoneCost,
                    resourceReturn = config.PickableStoneReturn,
                    respawnTime = 0,
                    recover = config.RecoverResources,
                    Name = "PickableStone",
                    isGrounded = true
                },
                new()
                {
                    key = "Pickable_Flint",
                    ResourceCost = config.PickableFlintCost,
                    resourceReturn = config.PickableFlintReturn,
                    respawnTime = 240,
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
}
