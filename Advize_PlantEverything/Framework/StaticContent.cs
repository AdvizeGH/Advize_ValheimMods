using Advize_PlantEverything.Configuration;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PE = Advize_PlantEverything.PlantEverything;

namespace Advize_PlantEverything.Framework
{
	static class StaticContent
	{
		private static ModConfig Config => PE.config;

		private const Heightmap.Biome TemperateBiomes = Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest | Heightmap.Biome.Plains;
		internal const Heightmap.Biome AllBiomes = (Heightmap.Biome)895/*GetBiomeMask((Heightmap.Biome[])System.Enum.GetValues(typeof(Heightmap.Biome)))*/;

		//private static Heightmap.Biome GetBiomeMask(Heightmap.Biome[] biomes)
		//{
		//	Heightmap.Biome biomeMask = 0;

		//	foreach (Heightmap.Biome biome in biomes)
		//	{
		//		biomeMask |= biome;
		//	}

		//	return biomeMask;
		//}

		internal static string[] layersForPieceRemoval = { "item", "piece_nonsolid", "Default_small", "Default" };

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
			{ "GlowingMushroomName", "Glowing Mushroom" },
			{ "GlowingMushroomDescription", "Plant a large glowing mushroom." },
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
			{ "Beech_small1" },
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
			{ "GlowingMushroom" },
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
			bool enforceBiomes = Config.EnforceBiomes;

			List<PieceDB> newList = new()
			{
				new PieceDB
				{
					key = "RaspberryBush",
					ResourceCost = Config.RaspberryCost,
					resourceReturn = Config.RaspberryReturn,
					respawnTime = Config.RaspberryRespawnTime,
					biome = enforceBiomes ? Heightmap.Biome.Meadows : 0,
					icon = true,
					recover = Config.RecoverResources
				},
				new PieceDB
				{
					key = "BlueberryBush",
					ResourceCost = Config.BlueberryCost,
					resourceReturn = Config.BlueberryReturn,
					respawnTime = Config.BlueberryRespawnTime,
					biome = enforceBiomes ? Heightmap.Biome.BlackForest : 0,
					icon = true,
					recover = Config.RecoverResources
				},
				new PieceDB
				{
					key = "CloudberryBush",
					ResourceCost = Config.CloudberryCost,
					resourceReturn = Config.CloudberryReturn,
					respawnTime = Config.CloudberryRespawnTime,
					biome = enforceBiomes ? Heightmap.Biome.Plains : 0,
					icon = true,
					recover = Config.RecoverResources
				},
				new PieceDB
				{
					key = "Pickable_Mushroom",
					ResourceCost = Config.MushroomCost,
					resourceReturn = Config.MushroomReturn,
					respawnTime = Config.MushroomRespawnTime,
					recover = Config.RecoverResources,
					Name = "PickableMushroom",
					isGrounded = true
				},
				new PieceDB
				{
					key = "Pickable_Mushroom_yellow",
					ResourceCost = Config.YellowMushroomCost,
					resourceReturn = Config.YellowMushroomReturn,
					respawnTime = Config.YellowMushroomRespawnTime,
					recover = Config.RecoverResources,
					Name = "PickableYellowMushroom",
					isGrounded = true
				},
				new PieceDB
				{
					key = "Pickable_Mushroom_blue",
					ResourceCost = Config.BlueMushroomCost,
					resourceReturn = Config.BlueMushroomReturn,
					respawnTime = Config.BlueMushroomRespawnTime,
					recover = Config.RecoverResources,
					Name = "PickableBlueMushroom",
					isGrounded = true
				},
				new PieceDB
				{
					key = "Pickable_Thistle",
					ResourceCost = Config.ThistleCost,
					resourceReturn = Config.ThistleReturn,
					respawnTime = Config.ThistleRespawnTime,
					biome = enforceBiomes ? Heightmap.Biome.BlackForest : 0,
					recover = Config.RecoverResources,
					Name = "PickableThistle",
					isGrounded = true
				},
				new PieceDB
				{
					key = "Pickable_Dandelion",
					ResourceCost = Config.DandelionCost,
					resourceReturn = Config.DandelionReturn,
					respawnTime = Config.DandelionRespawnTime,
					biome = enforceBiomes ? Heightmap.Biome.Meadows : 0,
					recover = Config.RecoverResources,
					Name = "PickableDandelion",
					isGrounded = true
				},
				new PieceDB
				{
					key = "Pickable_SmokePuff",
					ResourceCost = Config.SmokePuffCost,
					resourceReturn = Config.SmokePuffReturn,
					respawnTime = Config.SmokePuffRespawnTime,
					recover = Config.RecoverResources,
					Name = "PickableSmokePuff",
					isGrounded = true,
					extraDrops = true
				},
				new PieceDB
				{
					key = "Pickable_Fiddlehead",
					ResourceCost = Config.FiddleheadCost,
					resourceReturn = Config.FiddleheadReturn,
					respawnTime = Config.FiddleheadRespawnTime,
					icon = true,
					recover = Config.RecoverResources,
					Name = "PickableFiddlehead",
					isGrounded = true,
					extraDrops = true
				}
			};

			if (Config.EnableMiscFlora)
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
						snapPoints = new()
						{
							{ new Vector3(1f, 0.5f, 0) },
							{ new Vector3(-1f, 0.5f, 0) },
							{ new Vector3(1f, -1f, 0) },
							{ new Vector3(-1f, -1f, 0) }
						}
					},
					new PieceDB
					{
						key = "FernAshlands",
						Resource = new KeyValuePair<string, int>("Wood", 2),
						icon = true,
						Name = "AshlandsFern",
						canBeRemoved = false
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
						ResourceCost = Config.PickableBranchCost,
						resourceReturn = Config.PickableBranchReturn,
						respawnTime = 240,
						recover = Config.RecoverResources,
						Name = "PickableBranch",
						isGrounded = true
					},
					new PieceDB
					{
						key = "Pickable_Stone",
						ResourceCost = Config.PickableStoneCost,
						resourceReturn = Config.PickableStoneReturn,
						respawnTime = 0,
						recover = Config.RecoverResources,
						Name = "PickableStone",
						isGrounded = true
					},
					new PieceDB
					{
						key = "Pickable_Flint",
						ResourceCost = Config.PickableFlintCost,
						resourceReturn = Config.PickableFlintReturn,
						respawnTime = 240,
						recover = Config.RecoverResources,
						Name = "PickableFlint",
						isGrounded = true
					}
				});
			}
			
			if (Config.EnableExtraResources)
			{
				List<string> potentialNewLayers = layersForPieceRemoval.ToList();
				bool queueReattempt = false;

				foreach (ExtraResource er in PE.deserializedExtraResources)
				{
					if (!PE.prefabRefs.ContainsKey(er.prefabName) || !PE.prefabRefs[er.prefabName])
					{
						PE.Dbgl($"{er.prefabName} is not in dictionary of prefab references or has a null value", level: LogLevel.Warning);
						queueReattempt = true;
						continue;
					}

					if (!ObjectDB.instance?.GetItemPrefab(er.resourceName)?.GetComponent<ItemDrop>())
					{
						PE.Dbgl($"{er.prefabName}'s required resource {er.resourceName} not found", level: LogLevel.Warning);
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

					foreach (Collider c in PE.prefabRefs[er.prefabName].GetComponentsInChildren<Collider>())
					{
						string layer = LayerMask.LayerToName(c.gameObject.layer);
						//Dbgl($"Layer to potentially add is {layer}");
						potentialNewLayers.Add(layer);
					}
				}

				layersForPieceRemoval = potentialNewLayers.Distinct().ToArray();
				PE.resolveMissingReferences = queueReattempt;
			}

			return newList;
		}

		internal static List<SaplingDB> GenerateCustomSaplingRefs()
		{
			return new()
			{
				new SaplingDB
				{
					key = "Ygga_Sapling",
					biome = Config.EnforceBiomes ? TemperateBiomes | Heightmap.Biome.Mistlands : AllBiomes,
					source = "YggaShoot_small1",
					Resource = new KeyValuePair<string, int>("Sap", 1),
					icon = true,
					growTime = Config.YggaGrowthTime,
					growRadius = Config.YggaGrowRadius,
					minScale = Config.YggaMinScale,
					maxScale = Config.YggaMaxScale,
					grownPrefabs = new GameObject[] { PE.prefabRefs["YggaShoot1"], PE.prefabRefs["YggaShoot2"], PE.prefabRefs["YggaShoot3"] }
				},
				new SaplingDB
				{
					key = "Ancient_Sapling",
					biome = Config.EnforceBiomes ? TemperateBiomes | Heightmap.Biome.Swamp : AllBiomes,
					source = "SwampTree1",
					Resource = new KeyValuePair<string, int>("AncientSeed", 1),
					icon = true,
					growTime = Config.AncientGrowthTime,
					growRadius = Config.AncientGrowRadius,
					minScale = Config.AncientMinScale,
					maxScale = Config.AncientMaxScale,
					grownPrefabs = new GameObject[] { PE.prefabRefs["SwampTree1"] }
				},
				new SaplingDB
				{
					key = "Autumn_Birch_Sapling",
					biome = Config.EnforceBiomes ? TemperateBiomes : AllBiomes,
					source = "Birch1_aut",
					Resource = new KeyValuePair<string, int>("BirchSeeds", 1),
					icon = true,
					growTime = Config.AutumnBirchGrowthTime,
					growRadius = Config.AutumnBirchGrowRadius,
					minScale = Config.AutumnBirchMinScale,
					maxScale = Config.AutumnBirchMaxScale,
					grownPrefabs = new GameObject[] { PE.prefabRefs["Birch1_aut"], PE.prefabRefs["Birch2_aut"] }
				},
				new SaplingDB
				{
					key = "Ashwood_Sapling",
					biome = Config.EnforceBiomes ? TemperateBiomes | Heightmap.Biome.AshLands : AllBiomes,
					source = "AshlandsTree3",
					Resources = new Dictionary<string, int>() { { "BeechSeeds", 1 }, { "SulfurStone", 1 } },
					icon = true,
					growTime = Config.AshwoodGrowthTime,
					growRadius = Config.AshwoodGrowRadius,
					minScale = Config.AshwoodMinScale,
					maxScale = Config.AshwoodMaxScale,
					grownPrefabs = new GameObject[] { PE.prefabRefs["AshlandsTree3"], PE.prefabRefs["AshlandsTree4"], PE.prefabRefs["AshlandsTree5"], PE.prefabRefs["AshlandsTree6_big"] }
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
					biome = Config.EnforceBiomesVanilla ? TemperateBiomes : AllBiomes,
					growTime = Config.BeechGrowthTime,
					growRadius = Config.BeechGrowRadius,
					minScale = Config.BeechMinScale,
					maxScale = Config.BeechMaxScale
				},
				new SaplingDB
				{
					key = "PineTree_Sapling",
					biome = Config.EnforceBiomesVanilla ? TemperateBiomes : AllBiomes,
					growTime = Config.PineGrowthTime,
					growRadius = Config.PineGrowRadius,
					minScale = Config.PineMinScale,
					maxScale = Config.PineMaxScale
				},
				new SaplingDB
				{
					key = "FirTree_Sapling",
					biome = Config.EnforceBiomesVanilla ? TemperateBiomes | Heightmap.Biome.Mountain : AllBiomes,
					growTime = Config.FirGrowthTime,
					growRadius = Config.FirGrowRadius,
					minScale = Config.FirMinScale,
					maxScale = Config.FirMaxScale
				},
				new SaplingDB
				{
					key = "Birch_Sapling",
					biome = Config.EnforceBiomesVanilla ? TemperateBiomes : AllBiomes,
					growTime = Config.BirchGrowthTime,
					growRadius = Config.BirchGrowRadius,
					minScale = Config.BirchMinScale,
					maxScale = Config.BirchMaxScale
				},
				new SaplingDB
				{
					key = "Oak_Sapling",
					biome = Config.EnforceBiomesVanilla ? TemperateBiomes : AllBiomes,
					growTime = Config.OakGrowthTime,
					growRadius = Config.OakGrowRadius,
					minScale = Config.OakMinScale,
					maxScale = Config.OakMaxScale
				}
			};
		}

		internal static List<PrefabDB> GenerateCropRefs()
		{
			bool overridesEnabled = Config.EnableCropOverrides;
			bool enforceBiomesVanilla = Config.EnforceBiomesVanilla;

			return new()
			{
				new PrefabDB
				{
					key = "sapling_barley",
					biome = enforceBiomesVanilla ? Heightmap.Biome.Plains : AllBiomes,
					resourceCost = overridesEnabled ? Config.BarleyCost : 1,
					resourceReturn = overridesEnabled ? Config.BarleyReturn : 2
				},
				new PrefabDB
				{
					key = "sapling_carrot",
					biome = enforceBiomesVanilla ? TemperateBiomes : AllBiomes,
					resourceCost = overridesEnabled ? Config.CarrotCost : 1,
					resourceReturn = overridesEnabled ? Config.CarrotReturn : 1
				},
				new PrefabDB
				{
					key = "sapling_flax",
					biome = enforceBiomesVanilla ? Heightmap.Biome.Plains : AllBiomes,
					resourceCost = overridesEnabled ? Config.FlaxCost : 1,
					resourceReturn = overridesEnabled ? Config.FlaxReturn : 2
				},
				new PrefabDB
				{
					key = "sapling_onion",
					biome = enforceBiomesVanilla ? TemperateBiomes : AllBiomes,
					resourceCost = overridesEnabled ? Config.OnionCost : 1,
					resourceReturn = overridesEnabled ? Config.OnionReturn : 1
				},
				new PrefabDB
				{
					key = "sapling_seedcarrot",
					biome = enforceBiomesVanilla ? TemperateBiomes : AllBiomes,
					resourceCost = overridesEnabled ? Config.SeedCarrotCost : 1,
					resourceReturn = overridesEnabled ? Config.SeedCarrotReturn : 3
				},
				new PrefabDB
				{
					key = "sapling_seedonion",
					biome = enforceBiomesVanilla ? TemperateBiomes : AllBiomes,
					resourceCost = overridesEnabled ? Config.SeedOnionCost : 1,
					resourceReturn = overridesEnabled ? Config.SeedOnionReturn : 3
				},
				new PrefabDB
				{
					key = "sapling_seedturnip",
					biome = enforceBiomesVanilla ? TemperateBiomes | Heightmap.Biome.Swamp | Heightmap.Biome.Mistlands : AllBiomes,
					resourceCost = overridesEnabled ? Config.SeedTurnipCost : 1,
					resourceReturn = overridesEnabled ? Config.SeedTurnipReturn : 3
				},
				new PrefabDB
				{
					key = "sapling_turnip",
					biome = enforceBiomesVanilla ? TemperateBiomes | Heightmap.Biome.Swamp | Heightmap.Biome.Mistlands : AllBiomes,
					resourceCost = overridesEnabled ? Config.TurnipCost : 1,
					resourceReturn = overridesEnabled ? Config.TurnipReturn : 1
				},
				new PrefabDB
				{
					key = "sapling_magecap",
					biome = enforceBiomesVanilla ? Heightmap.Biome.Mistlands : AllBiomes,
					resourceCost = overridesEnabled ? Config.MagecapCost : 1,
					resourceReturn = overridesEnabled ? Config.MagecapReturn : 1,
					extraDrops = true
				},
				new PrefabDB
				{
					key = "sapling_jotunpuffs",
					biome = enforceBiomesVanilla ? Heightmap.Biome.Mistlands : AllBiomes,
					resourceCost = overridesEnabled ? Config.JotunPuffsCost : 1,
					resourceReturn = overridesEnabled ? Config.JotunPuffsReturn : 1,
					extraDrops = true
				}
			};
		}
	}
}
