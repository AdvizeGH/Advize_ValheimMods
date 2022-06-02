using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Advize_PlantEverything.Configuration;

namespace Advize_PlantEverything
{
    [BepInPlugin(PluginID, PluginName, Version)]
    public partial class PlantEverything : BaseUnityPlugin
    {
        public const string PluginID = "advize.PlantEverything";
        public const string PluginName = "PlantEverything";
        public const string Version = "1.11.5";

        private readonly Harmony harmony = new(PluginID);
        public static ManualLogSource PELogger = new($" {PluginName}");

        private static readonly Dictionary<string, GameObject> prefabRefs = new();
        private static List<PieceDB> pieceRefs = new();
        private static List<SaplingDB> saplingRefs = new();

        private static bool isInitialized = false;

        private static readonly string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly AssetBundle assetBundle = LoadAssetBundle("planteverything");
        private static readonly Dictionary<string, Texture2D> cachedTextures = new();

        private static ModConfig config;

        private static readonly Dictionary<string, string> stringDictionary = new() {
            { "AncientSapling", "Ancient Sapling" },
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

        private void Awake()
        {
            BepInEx.Logging.Logger.Sources.Add(PELogger);
            config = new ModConfig(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "1.9.0" });
            if (config.EnableLocalization)
                LoadLocalizedStrings();
            harmony.PatchAll();
        }

        private void LoadLocalizedStrings()
        {
            string fileName = $"{config.Language}_{PluginName}.json";
            string filePath = Path.Combine(modDirectory, fileName);

            try
            {
                string jsonText = File.ReadAllText(filePath);
                LocalizedStrings localizedStrings = JsonUtility.FromJson<LocalizedStrings>(jsonText);

                foreach (string value in localizedStrings.localizedStrings)
                {
                    string[] split = value.Split(':');
                    stringDictionary.Remove(split[0]);
                    stringDictionary.Add(split[0], split[1]);
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
            string filePath = Path.Combine(modDirectory, $"english_{PluginName}.json");

            LocalizedStrings localizedStrings = new();
            foreach (KeyValuePair<string, string> kvp in stringDictionary)
            {
                localizedStrings.localizedStrings.Add($"{kvp.Key}:{kvp.Value}");
            }

            File.WriteAllText(filePath, JsonUtility.ToJson(localizedStrings, true));

            Dbgl($"Saved english localized strings to {filePath}");
        }

        internal static void Dbgl(string message, bool forceLog = false, bool logError = false)
        {
            if (forceLog || config.EnableDebugMessages)
            {
                if (logError)
                {
                    PELogger.LogError(message);
                }
                else
                {
                    PELogger.LogInfo(message);
                }
            }
        }

        private static AssetBundle LoadAssetBundle(string fileName)
        {
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Advize_{PluginName}.Assets.{fileName}");
            return AssetBundle.LoadFromStream(manifestResourceStream);
        }

        private static GameObject CreatePrefab(string name)
        {
            GameObject loadedPrefab = assetBundle.LoadAsset<GameObject>(name);
            loadedPrefab.SetActive(true);

            return loadedPrefab;
        }

        private static Sprite CreateSprite(string fileName, Rect spriteSection)
        {
            try
            {
                Texture2D texture = LoadTexture(fileName);
                return Sprite.Create(texture, spriteSection, Vector2.zero);
            }
            catch
            {
                Dbgl("Unable to load texture", true, true);
            }

            return null;
        }

        private static Texture2D LoadTexture(string fileName)
        {
            Texture2D result;
            if (cachedTextures.ContainsKey(fileName))
            {
                result = cachedTextures[fileName];
            }
            else
            {
                Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Advize_{PluginName}.Assets.{fileName}");
                byte[] array = new byte[manifestResourceStream.Length];
                manifestResourceStream.Read(array, 0, array.Length);
                Texture2D texture = new(0, 0);
                ImageConversion.LoadImage(texture, array);
                result = texture;
            }

            return result;
        }

        private static Piece CreatePiece(string key, Piece component, bool? isGrounded = null, bool canBeRemoved = true)
        {
            component.m_name = $"$pe{key}Name";
            component.m_description = $"$pe{key}Description";
            component.m_category = Piece.PieceCategory.Misc;
            component.m_cultivatedGroundOnly = (key.Contains("berryBush") || key.Contains("Pickable")) && config.RequireCultivation;
            component.m_groundOnly = component.m_groundPiece = isGrounded ?? !config.PlaceAnywhere;
            component.m_canBeRemoved = canBeRemoved;
            component.m_targetNonPlayerBuilt = false;
            return component;
        }

        private static void InitPrefabRefs()
        {
            Dbgl("InitPrefabRefs");
            if (prefabRefs.Count > 0)
            {
                return;
            }
            prefabRefs.Add("Bush02_en", null);
            prefabRefs.Add("Bush01_heath", null);
            prefabRefs.Add("Bush01", null);
            prefabRefs.Add("GlowingMushroom", null);
            prefabRefs.Add("Pinetree_01", null);
            prefabRefs.Add("FirTree", null);
            prefabRefs.Add("Beech_small1", null);
            prefabRefs.Add("FirTree_small_dead", null);
            prefabRefs.Add("FirTree_small", null);
            prefabRefs.Add("Pickable_Dandelion", null);
            prefabRefs.Add("CloudberryBush", null);
            prefabRefs.Add("vines", null);
            prefabRefs.Add("Cultivator", null);
            prefabRefs.Add("SwampTree1", null);
            prefabRefs.Add("Beech1", null);
            prefabRefs.Add("Birch2", null);
            prefabRefs.Add("Oak1", null);
            prefabRefs.Add("Birch2_aut", null);
            prefabRefs.Add("Birch1_aut", null);
            prefabRefs.Add("Birch1", null);
            prefabRefs.Add("Pickable_Thistle", null);
            prefabRefs.Add("Pickable_Flint", null);
            prefabRefs.Add("Pickable_Stone", null);
            prefabRefs.Add("FirCone", null);
            prefabRefs.Add("PineCone", null);
            prefabRefs.Add("shrub_2", null);
            prefabRefs.Add("shrub_2_heath", null);
            prefabRefs.Add("BirchSeeds", null);
            prefabRefs.Add("AncientSeed", null);
            prefabRefs.Add("Acorn", null);
            prefabRefs.Add("BeechSeeds", null);
            prefabRefs.Add("Pickable_Branch", null);
            prefabRefs.Add("Pickable_Mushroom", null);
            prefabRefs.Add("BlueberryBush", null);
            prefabRefs.Add("RaspberryBush", null);
            prefabRefs.Add("Pickable_Mushroom_blue", null);
            prefabRefs.Add("Pickable_Mushroom_yellow", null);
            prefabRefs.Add("sapling_seedonion", null);
            prefabRefs.Add("Beech_Sapling", null);
            prefabRefs.Add("PineTree_Sapling", null);
            prefabRefs.Add("FirTree_Sapling", null);
            prefabRefs.Add("sapling_onion", null);
            prefabRefs.Add("sapling_turnip", null);
            prefabRefs.Add("Oak_Sapling", null);
            prefabRefs.Add("sapling_barley", null);
            prefabRefs.Add("Birch_Sapling", null);
            prefabRefs.Add("sapling_carrot", null);
            prefabRefs.Add("sapling_seedcarrot", null);
            prefabRefs.Add("sapling_flax", null);
            prefabRefs.Add("sapling_seedturnip", null);
            prefabRefs.Add("vfx_Place_wood_pole", null);
            prefabRefs.Add("sfx_build_cultivator", null);

            Object[] array = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            for (int i = 0; i < array.Length; i++)
            {
                GameObject gameObject = (GameObject)array[i];

                if (!prefabRefs.ContainsKey(gameObject.name))
                {
                    continue;
                }

                if (gameObject.name.Equals("FirTree_small"))
                {
                    Component[] components = gameObject.GetComponents(typeof(Component));
                    if (components.Length < 2)
                    {
                        continue;
                    }
                }

                prefabRefs[gameObject.name] = gameObject;

                bool nullValue = false;
                foreach (KeyValuePair<string, GameObject> kvp in prefabRefs)
                {
                    if (kvp.Value == null)
                        nullValue = true;
                }
                if (!nullValue)
                {
                    Dbgl("Found all prefab references");
                    break;
                }
            }

            prefabRefs.Add("Ancient_Sapling", CreatePrefab("Ancient_Sapling"));
            prefabRefs.Add("Pickable_Dandelion_Picked", CreatePrefab("Pickable_Dandelion_Picked"));
            prefabRefs.Add("Pickable_Thistle_Picked", CreatePrefab("Pickable_Thistle_Picked"));
            prefabRefs.Add("Pickable_Mushroom_Picked", CreatePrefab("Pickable_Mushroom_Picked"));
            prefabRefs.Add("Pickable_Mushroom_yellow_Picked", CreatePrefab("Pickable_Mushroom_yellow_Picked"));
            prefabRefs.Add("Pickable_Mushroom_blue_Picked", CreatePrefab("Pickable_Mushroom_blue_Picked"));
        }

        private static void InitPieceRefs()
        {
            Dbgl($"InitPieceRefs");

            if (pieceRefs.Count > 0)
            {
                foreach (PieceDB pdb in pieceRefs)
                {
                    if (prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Contains(pdb.Prefab))
                    {
                        prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(pdb.Prefab);
                    }
                    //Used to prevent null ref if someone is using cultivator
                    if (Player.m_localPlayer != null && Player.m_localPlayer.GetRightItem() != null && Player.m_localPlayer.GetRightItem().m_shared.m_name == "$item_cultivator")
                    {
                        PELogger.LogWarning("Cultivator updated through config change, unequipping cultivator");
                        Player.m_localPlayer.HideHandItems();
                    }
                    DestroyImmediate(pdb.Prefab.GetComponent<Piece>());
                }
                pieceRefs.Clear();
            }
            pieceRefs = GeneratePieceRefs();
        }

        private static Piece GetOrAddPieceComponent(GameObject go) => go.GetComponent<Piece>() ?? go.AddComponent<Piece>();

        private static List<PieceDB> GeneratePieceRefs()
        {
            List<PieceDB> newList = new()
            {
                new PieceDB
                {
                    key = "RaspberryBush",
                    ResourceCost = config.RaspberryCost,
                    resourceReturn = config.RaspberryReturn,
                    respawnTime = config.RaspberryRespawnTime,
                    icon = true,
                    recover = config.RecoverResources,
                    piece = CreatePiece("RaspberryBush", GetOrAddPieceComponent(prefabRefs["RaspberryBush"]))
                },
                new PieceDB
                {
                    key = "BlueberryBush",
                    ResourceCost = config.BlueberryCost,
                    resourceReturn = config.BlueberryReturn,
                    respawnTime = config.BlueberryRespawnTime,
                    biome = (int)Heightmap.Biome.BlackForest,
                    icon = true,
                    recover = config.RecoverResources,
                    piece = CreatePiece("BlueberryBush", GetOrAddPieceComponent(prefabRefs["BlueberryBush"]))
                },
                new PieceDB
                {
                    key = "CloudberryBush",
                    ResourceCost = config.CloudberryCost,
                    resourceReturn = config.CloudberryReturn,
                    respawnTime = config.CloudberryRespawnTime,
                    biome = (int)Heightmap.Biome.Plains,
                    icon = true,
                    recover = config.RecoverResources,
                    piece = CreatePiece("CloudberryBush", GetOrAddPieceComponent(prefabRefs["CloudberryBush"]))
                },
                new PieceDB
                {
                    key = "Pickable_Mushroom",
                    ResourceCost = config.MushroomCost,
                    resourceReturn = config.MushroomReturn,
                    respawnTime = config.MushroomRespawnTime,
                    biome = 9,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableMushroom", GetOrAddPieceComponent(prefabRefs["Pickable_Mushroom"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Mushroom_yellow",
                    ResourceCost = config.YellowMushroomCost,
                    resourceReturn = config.YellowMushroomReturn,
                    respawnTime = config.YellowMushroomRespawnTime,
                    biome = 10,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableYellowMushroom", GetOrAddPieceComponent(prefabRefs["Pickable_Mushroom_yellow"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Mushroom_blue",
                    ResourceCost = config.BlueMushroomCost,
                    resourceReturn = config.BlueMushroomReturn,
                    respawnTime = config.BlueMushroomRespawnTime,
                    biome = 10,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableBlueMushroom", GetOrAddPieceComponent(prefabRefs["Pickable_Mushroom_blue"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Thistle",
                    ResourceCost = config.ThistleCost,
                    resourceReturn = config.ThistleReturn,
                    respawnTime = config.ThistleRespawnTime,
                    biome = 10,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableThistle", GetOrAddPieceComponent(prefabRefs["Pickable_Thistle"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Dandelion",
                    ResourceCost = config.DandelionCost,
                    resourceReturn = config.DandelionReturn,
                    respawnTime = config.DandelionRespawnTime,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableDandelion", GetOrAddPieceComponent(prefabRefs["Pickable_Dandelion"]), isGrounded: true)
                }
            };

            if (!config.EnableMiscFlora) return newList;

            newList.AddRange(new List<PieceDB>()
            {
                new PieceDB
                {
                    key = "Beech_small1",
                    Resource = new KeyValuePair<string, int>("BeechSeeds", 1),
                    icon = true,
                    piece = CreatePiece("BeechSmall", GetOrAddPieceComponent(prefabRefs["Beech_small1"]), canBeRemoved: false)
                },
                new PieceDB
                {
                    key = "FirTree_small",
                    Resource = new KeyValuePair<string, int>("FirCone", 1),
                    icon = true,
                    piece = CreatePiece("FirSmall", GetOrAddPieceComponent(prefabRefs["FirTree_small"]), canBeRemoved: false)
                },
                new PieceDB
                {
                    key = "FirTree_small_dead",
                    Resource = new KeyValuePair<string, int>("FirCone", 1),
                    icon = true,
                    piece = CreatePiece("FirSmallDead", GetOrAddPieceComponent(prefabRefs["FirTree_small_dead"]), canBeRemoved: false)
                },
                new PieceDB
                {
                    key = "Bush01",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    piece = CreatePiece("Bush01", GetOrAddPieceComponent(prefabRefs["Bush01"]), canBeRemoved: false)
                },
                new PieceDB
                {
                    key = "Bush01_heath",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    piece = CreatePiece("Bush02", GetOrAddPieceComponent(prefabRefs["Bush01_heath"]), canBeRemoved: false)
                },
                new PieceDB
                {
                    key = "Bush02_en",
                    Resource = new KeyValuePair<string, int>("Wood", 3),
                    icon = true,
                    piece = CreatePiece("PlainsBush", GetOrAddPieceComponent(prefabRefs["Bush02_en"]), canBeRemoved: false)
                },
                new PieceDB
                {
                    key = "shrub_2",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    piece = CreatePiece("Shrub01", GetOrAddPieceComponent(prefabRefs["shrub_2"]), canBeRemoved: false)
                },
                new PieceDB
                {
                    key = "shrub_2_heath",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    piece = CreatePiece("Shrub02", GetOrAddPieceComponent(prefabRefs["shrub_2_heath"]), canBeRemoved: false)
                },
                new PieceDB
                {
                    key = "vines",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    icon = true,
                    recover = true,
                    piece = CreatePiece("Vines", GetOrAddPieceComponent(prefabRefs["vines"]), isGrounded: false),
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
                    piece = CreatePiece("GlowingMushroom", GetOrAddPieceComponent(prefabRefs["GlowingMushroom"]), isGrounded: true, canBeRemoved: true)
                },
                new PieceDB
                {
                    key = "Pickable_Branch",
                    ResourceCost = config.PickableBranchCost,
                    resourceReturn = config.PickableBranchReturn,
                    respawnTime = 240,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableBranch", GetOrAddPieceComponent(prefabRefs["Pickable_Branch"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Stone",
                    ResourceCost = config.PickableStoneCost,
                    resourceReturn = config.PickableStoneReturn,
                    respawnTime = 0,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableStone", GetOrAddPieceComponent(prefabRefs["Pickable_Stone"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Flint",
                    ResourceCost = config.PickableFlintCost,
                    resourceReturn = config.PickableFlintReturn,
                    respawnTime = 240,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableFlint", GetOrAddPieceComponent(prefabRefs["Pickable_Flint"]), isGrounded: true)
                }
            });
            return newList;
        }

        private static void InitPieces()
        {
            Dbgl("InitPieces");

            foreach (PieceDB pdb in pieceRefs)
            {
                ItemDrop resource = ObjectDB.instance.GetItemPrefab(pdb.Resource.Key).GetComponent<ItemDrop>();

                if (pdb.Resources.Count > 0)
                {
                    List<Piece.Requirement> resources = new();
                    foreach (string item in pdb.Resources.Keys)
                    {
                        resources.Add(new Piece.Requirement
                        {
                            m_resItem = ObjectDB.instance.GetItemPrefab(item).GetComponent<ItemDrop>(),
                            m_amount = pdb.Resources[item],
                            m_recover = pdb.recover
                        });
                    }
                    pdb.piece.m_resources = resources.ToArray();
                }
                else
                {
                    pdb.piece.m_resources = new Piece.Requirement[]
                    {
                        new Piece.Requirement
                        {
                            m_resItem = resource,
                            m_amount = pdb.ResourceCost,
                            m_recover = pdb.recover
                        }
                    };
                }

                pdb.piece.m_placeEffect.m_effectPrefabs = new EffectList.EffectData[]
                {
                    new EffectList.EffectData
                    {
                        m_prefab = prefabRefs["vfx_Place_wood_pole"],
                        m_enabled = true
                    },
                    new EffectList.EffectData
                    {
                        m_prefab = prefabRefs["sfx_build_cultivator"],
                        m_enabled = true
                    }
                };

                Pickable pickable = pdb.Prefab.GetComponent<Pickable>();
                if (pickable != null)
                {
                    pickable.m_respawnTimeMinutes = pdb.respawnTime;
                    pickable.m_amount = pdb.resourceReturn;

                    if (pdb.Prefab.transform.Find("visual") != null)
                    {
                        if (config.ShowPickableSpawners)
                        {
                            Transform t = prefabRefs[pdb.key + "_Picked"].transform.Find("PE_Picked");
                            if (t)
                            {
                                t.SetParent(pdb.Prefab.transform);
                                t.gameObject.GetComponent<MeshRenderer>().sharedMaterials = pdb.key.Equals("Pickable_Thistle") ?
                                    pdb.Prefab.transform.Find("visual").Find("default").GetComponent<MeshRenderer>().sharedMaterials :
                                    pdb.Prefab.transform.Find("visual").GetComponent<MeshRenderer>().sharedMaterials;
                                if (pdb.key.Contains("Dandelion"))
                                {
                                    Material m = pdb.Prefab.transform.Find("visual").GetComponent<MeshRenderer>().sharedMaterials[0];
                                    t.gameObject.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { m, m };
                                }
                            }
                        }
                        else
                        {
                            Transform t = prefabRefs[pdb.key].transform.Find("PE_Picked");
                            if (t)
                            {
                                t.SetParent(prefabRefs[pdb.key + "_Picked"].transform);
                            }
                        }
                    }
                }

                if (pdb.points != null)
                {
                    Transform sp = pdb.Prefab.transform.Find("_snappoint");
                    if (config.SnappableVines)
                    {
                        if (!sp)
                        {
                            foreach (Vector3 point in pdb.points)
                            {
                                GameObject snapPoint = new("_snappoint");
                                snapPoint.tag = "snappoint";
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

                if (config.EnforceBiomes)
                {
                    pdb.piece.m_onlyInBiome = (Heightmap.Biome)pdb.biome;
                }

                pdb.piece.m_icon = pdb.icon ? CreateSprite($"{pdb.key}PieceIcon.png", new Rect(0, 0, 64, 64)) : resource.m_itemData.GetIcon();
            }
        }

        private static void InitSaplingRefs()
        {
            Dbgl("InitSaplingRefs");

            if (saplingRefs.Count > 0)
                saplingRefs.Clear();

            saplingRefs = GenerateSaplingRefs();
        }

        private static List<SaplingDB> GenerateSaplingRefs()
        {
            List<SaplingDB> newList = new()
            {
                new SaplingDB
                {
                    key = "Ancient_Sapling",
                    source = "PineTree_Sapling",
                    resource = "AncientSeed",
                    resourceCost = 1,
                    biome = (int)Heightmap.Biome.Swamp,
                    growTime = config.AncientGrowthTime,
                    growRadius = config.AncientGrowRadius,
                    minScale = config.AncientMinScale,
                    maxScale = config.AncientMaxScale,
                    grownPrefabs = new GameObject[] { prefabRefs["SwampTree1"] }
                }
            };

            return newList;
        }

        private static void InitSaplings()
        {
            Dbgl("InitSaplings");

            ModifyTreeDrops();

            List<SaplingDB> vanillaSaplings = new()
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

            foreach (SaplingDB sdb in vanillaSaplings)
            {
                Plant plant = sdb.Prefab.GetComponent<Plant>();
                plant.m_growTime = plant.m_growTimeMax = sdb.growTime;
                plant.m_growRadius = sdb.growRadius;
                plant.m_minScale = sdb.minScale;
                plant.m_maxScale = sdb.maxScale;
                plant.m_destroyIfCantGrow = sdb.Prefab.GetComponent<Piece>().m_groundOnly = !config.PlaceAnywhere;
            }

            foreach (SaplingDB sdb in saplingRefs)
            {
                Plant plant = sdb.Prefab.GetComponent<Plant>();
                Piece piece = sdb.Prefab.GetComponent<Piece>();

                plant.m_growTime = plant.m_growTimeMax = sdb.growTime;
                plant.m_grownPrefabs = sdb.grownPrefabs;
                plant.m_minScale = sdb.minScale;
                plant.m_maxScale = sdb.maxScale;
                plant.m_growRadius = sdb.growRadius;

                piece.m_resources[0].m_resItem = prefabRefs[sdb.resource].GetComponent<ItemDrop>();
                piece.m_resources[0].m_amount = sdb.resourceCost;

                if (config.EnforceBiomes)
                {
                    piece.m_onlyInBiome = plant.m_biome = (Heightmap.Biome)sdb.biome;
                }
                plant.m_destroyIfCantGrow = piece.m_groundOnly = !config.PlaceAnywhere;

                if (isInitialized) continue;

                sdb.Prefab.transform.Find("healthy").gameObject.GetComponent<MeshFilter>().mesh = prefabRefs[sdb.source].transform.Find("healthy").gameObject.GetComponent<MeshFilter>().mesh;
                sdb.Prefab.transform.Find("healthy").gameObject.GetComponent<MeshRenderer>().sharedMaterials = prefabRefs[sdb.source].transform.Find("healthy").gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                sdb.Prefab.transform.Find("unhealthy").gameObject.GetComponent<MeshFilter>().mesh = prefabRefs[sdb.source].transform.Find("unhealthy").gameObject.GetComponent<MeshFilter>().mesh;
                sdb.Prefab.transform.Find("unhealthy").gameObject.GetComponent<MeshRenderer>().sharedMaterials = prefabRefs[sdb.source].transform.Find("unhealthy").gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                //sdb.Prefab.GetComponent<Piece>().m_icon = source.GetComponent<Piece>().m_icon;
                piece.m_icon = piece.m_resources[0].m_resItem.m_itemData.GetIcon();
                piece.m_placeEffect.m_effectPrefabs[0].m_prefab = prefabRefs["vfx_Place_wood_pole"];
                piece.m_placeEffect.m_effectPrefabs[1].m_prefab = prefabRefs["sfx_build_cultivator"];
                sdb.Prefab.GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[0].m_prefab = prefabRefs[sdb.source].GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[0].m_prefab;
                sdb.Prefab.GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[1].m_prefab = prefabRefs[sdb.source].GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[1].m_prefab;
            }

            isInitialized = true;
        }

        private static void InitCrops()
        {
            Dbgl("InitCrops");

            bool enableCropOverrides = config.EnableCropOverrides;

            List<PrefabDB> crops = new()
            {
                new PrefabDB
                {
                    key = "sapling_barley",
                    resourceCost = enableCropOverrides ? config.BarleyCost : 1,
                    resourceReturn = enableCropOverrides ? config.BarleyReturn : 2
                },
                new PrefabDB
                {
                    key = "sapling_carrot",
                    resourceCost = enableCropOverrides ? config.CarrotCost : 1,
                    resourceReturn = enableCropOverrides ? config.CarrotReturn : 1
                },
                new PrefabDB
                {
                    key = "sapling_flax",
                    resourceCost = enableCropOverrides ? config.FlaxCost : 1,
                    resourceReturn = enableCropOverrides ? config.FlaxReturn : 2
                },
                new PrefabDB
                {
                    key = "sapling_onion",
                    resourceCost = enableCropOverrides ? config.OnionCost : 1,
                    resourceReturn = enableCropOverrides ? config.OnionReturn : 1
                },
                new PrefabDB
                {
                    key = "sapling_seedcarrot",
                    resourceCost = enableCropOverrides ? config.SeedCarrotCost : 1,
                    resourceReturn = enableCropOverrides ? config.SeedCarrotReturn : 3
                },
                new PrefabDB
                {
                    key = "sapling_seedonion",
                    resourceCost = enableCropOverrides ? config.SeedOnionCost : 1,
                    resourceReturn = enableCropOverrides ? config.SeedOnionReturn : 3
                },
                new PrefabDB
                {
                    key = "sapling_seedturnip",
                    resourceCost = enableCropOverrides ? config.SeedTurnipCost : 1,
                    resourceReturn = enableCropOverrides ? config.SeedTurnipReturn : 3
                },
                new PrefabDB
                {
                    key = "sapling_turnip",
                    resourceCost = enableCropOverrides ? config.TurnipCost : 1,
                    resourceReturn = enableCropOverrides ? config.TurnipReturn : 1
                }
            };

            foreach (PrefabDB pdb in crops)
            {
                Piece piece = pdb.Prefab.GetComponent<Piece>();
                Plant plant = pdb.Prefab.GetComponent<Plant>();
                Pickable pickable = plant.m_grownPrefabs[0].GetComponent<Pickable>();

                piece.m_resources[0].m_amount = pdb.resourceCost;

                plant.m_destroyIfCantGrow = pdb.Prefab.GetComponent<Piece>().m_groundOnly = !config.PlaceAnywhere;

                if (!config.EnforceBiomesVanilla)
                    plant.m_biome = (Heightmap.Biome)895;

                plant.m_minScale = enableCropOverrides ? config.CropMinScale : 0.9f;
                plant.m_maxScale = enableCropOverrides ? config.CropMaxScale : 1.1f;
                plant.m_growTime = enableCropOverrides ? config.CropGrowTimeMin : 4000f;
                plant.m_growTimeMax = enableCropOverrides ? config.CropGrowTimeMax : 5000f;
                plant.m_growRadius = enableCropOverrides ? config.CropGrowRadius : 0.5f;
                plant.m_needCultivatedGround = piece.m_cultivatedGroundOnly = !enableCropOverrides || config.CropRequireCultivation;

                pickable.m_amount = pdb.resourceReturn;
            }
        }

        private static void InitCultivator()
        {
            Dbgl("InitCultivator");

            ItemDrop cultivator = prefabRefs["Cultivator"].GetComponent<ItemDrop>();

            for (int i = 0; i < saplingRefs.Count; i++)
            {
                if (!cultivator.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(saplingRefs[i].Prefab))
                    cultivator.m_itemData.m_shared.m_buildPieces.m_pieces.Add(saplingRefs[i].Prefab);
            }
            for (int i = 0; i < pieceRefs.Count; i++)
            {
                if (!pieceRefs[i].enabled)
                    continue;
                if (!cultivator.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(pieceRefs[i].Prefab))
                    cultivator.m_itemData.m_shared.m_buildPieces.m_pieces.Add(pieceRefs[i].Prefab);
            }

            cultivator.m_itemData.m_shared.m_buildPieces.m_canRemovePieces = true;
        }

        private static void FinalInit(ZNetScene __instance)
        {
            InitPieceRefs();
            InitPieces();
            InitSaplingRefs();
            InitSaplings();
            InitCrops();
            InitCultivator();

            if (stringDictionary.Count > 0)
                InitLocalization();

            if (!__instance.m_prefabs.Contains(prefabRefs["Ancient_Sapling"]))
                __instance.m_prefabs.Add(prefabRefs["Ancient_Sapling"]);
        }

        private static void ModifyTreeDrops()
        {
            if (!config.EnableSeedOverrides) return;

            Dictionary<GameObject, GameObject> dropsByTarget = new()
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

            foreach (KeyValuePair<GameObject, GameObject> kvp in dropsByTarget)
            {
                TreeBase target = kvp.Key.GetComponent<TreeBase>();
                DropTable.DropData itemDrop = default;
                bool dropExists = false;

                foreach (DropTable.DropData drop in target.m_dropWhenDestroyed.m_drops)
                {
                    if (drop.m_item.Equals(kvp.Value))
                    {
                        dropExists = true;
                        itemDrop = drop;
                        break;
                    }
                }

                if (dropExists) target.m_dropWhenDestroyed.m_drops.Remove(itemDrop);

                itemDrop.m_item = kvp.Value;
                itemDrop.m_stackMin = config.SeedDropMin;
                itemDrop.m_stackMax = config.SeedDropMax;
                target.m_dropWhenDestroyed.m_dropMin = 1;
                target.m_dropWhenDestroyed.m_dropMax = 3;
                target.m_dropWhenDestroyed.m_drops.Add(itemDrop);
                target.m_dropWhenDestroyed.m_dropChance = Mathf.Clamp(config.DropChance, 0f, 1f);
                target.m_dropWhenDestroyed.m_oneOfEach = config.OneOfEach;
            }
        }

        internal static void CoreSettingChanged(object o, System.EventArgs e)
        {
            Dbgl($"Config setting changed, re-initializing mod");
            InitPieceRefs();
            InitPieces();
            InitSaplingRefs();
            InitSaplings();
            InitCrops();
            InitCultivator();
        }

        internal static void PickableSettingChanged(object o, System.EventArgs e)
        {
            Dbgl($"Config setting changed, re-initializing pieces");
            InitPieceRefs();
            InitPieces();
            InitCultivator();
        }

        internal static void SaplingSettingChanged(object o, System.EventArgs e)
        {
            Dbgl($"Config setting changed, re-initializing saplings");
            InitSaplingRefs();
            InitSaplings();
        }

        internal static void SeedSettingChanged(object o, System.EventArgs e)
        {
            Dbgl($"Config setting changed, modifying TreeBase drop tables");
            ModifyTreeDrops();
        }

        internal static void CropSettingChanged(object o, System.EventArgs e)
        {
            Dbgl($"Config setting changed, re-initializing crops");
            InitCrops();
        }

        public static void InitLocalization()
        {
            Dbgl("InitLocalization");
            foreach (KeyValuePair<string, string> kvp in stringDictionary)
            {
                Traverse.Create(Localization.instance).Method("AddWord", $"pe{kvp.Key}", kvp.Value).GetValue($"pe{kvp.Key}", kvp.Value);
            }
            stringDictionary.Clear();
        }

        internal class LocalizedStrings
        {
            public List<string> localizedStrings = new();
        }

        internal class PrefabDB
        {
            internal string key;
            internal int biome = (int)Heightmap.Biome.Meadows;
            internal int resourceCost;
            internal int resourceReturn;

            internal GameObject Prefab
            {
                get { return prefabRefs[key]; }
            }
        }

        private class PieceDB : PrefabDB
        {
            private Dictionary<string, int> resources;
            internal int respawnTime;
            internal bool icon;
            internal bool recover;
            internal bool enabled;
            internal Piece piece;
            internal List<Vector3> points;

            internal KeyValuePair<string, int> Resource
            {
                get { return Resources.Count > 0 ? Resources.First() : new KeyValuePair<string, int>(Prefab.GetComponent<Pickable>().m_itemPrefab.name, resourceCost); }
                set { if (resources == null) { resources = new Dictionary<string, int>(); } if (!resources.ContainsKey(value.Key)) resources.Add(value.Key, value.Value); enabled = true; }
            }

            internal Dictionary<string, int> Resources
            {
                get { return resources ?? new Dictionary<string, int>(); }
                set { resources = value; enabled = true; }
            }

            internal int ResourceCost
            {
                get { return resourceCost; }
                set { resourceCost = value; enabled = value != 0; }
            }
        }

        private class SaplingDB : PrefabDB
        {
            internal string source;
            internal string resource;
            internal float growTime;
            internal float growRadius;
            internal float minScale;
            internal float maxScale;
            internal GameObject[] grownPrefabs;
        }

        /* TODO: * = Incomplete, ** = Partly Complete, *** = Complete, *~ = Skipped for now
         ** Double check config options and descriptions (including old ones that have been reorganized). Normalize them all.
         *~ Update all localization files to remove ancient seeds
         *~ Double check seed drop related fields
         ** Clean up PrefabDB usage, remove any duplicate code
         * */
    }
}
