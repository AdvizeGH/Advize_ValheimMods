using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using Advize_PlantEverything.Configuration;

namespace Advize_PlantEverything
{
    [BepInPlugin(PluginID, PluginName, Version)]
    public partial class PlantEverything : BaseUnityPlugin
    {
        public const string PluginID = "advize.PlantEverything";
        public const string PluginName = "PlantEverything";
        public const string Version = "1.8.3";

        private readonly Harmony harmony = new(PluginID);
        public static ManualLogSource PELogger = new($" {PluginName}");

        private static readonly Dictionary<string, GameObject> prefabRefs = new();
        private static List<PrefabDB> pieceRefs = new();
        private static List<SaplingDB> saplingRefs = new();

        private static bool isInitialized = false; 

        private static readonly string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly AssetBundle assetBundle = LoadAssetBundle("planteverything");
        private static readonly Dictionary<string, Texture2D> cachedTextures = new();

        private new Config Config
        {
            get { return Config.Instance; }
        }
        internal static ModConfig config;

        private static readonly Dictionary<string, string> stringDictionary = new() {
            //{ "BirchConeName", "Birch Cone" },
            //{ "BirchConeDescription", "Plant it to grow a birch tree." },
            //{ "OakSeedsName", "Oak Seeds" },
            //{ "OakSeedsDescription", "Plant them to grow an oak tree." },
            { "AncientSeedsName", "Ancient Seeds" },
            { "AncientSeedsDescription", "Plant them to grow an ancient tree." },
            //{ "BirchSapling", "Birch Sapling" },
            //{ "OakSapling", "Oak Sapling" },
            { "AncientSapling", "Ancient Sapling" },
            { "RaspberryBushName", "Raspberry Bush" },
            { "RaspberryBushDescription", "Plant raspberries to grow raspberry bushes." },
            { "BlueberryBushName", "Blueberry Bush" },
            { "BlueBerryBushDescription", "Plant blueberries to grow blueberry bushes." },
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
            { "GlowingMushroomName", "Glowing Mushroom"},
            { "GlowingMushroomDescription", "Plant a large glowing mushroom."}/*,
            { "GreydwarfNestName", "Greydwarf Nest" },
            { "GreydwarfNestDescription", "Plant your very own greydwarf nest" }*/
        };

        private void Awake()
        {
            BepInEx.Logging.Logger.Sources.Add(PELogger);
            Config.Init(this, true);
            config = new ModConfig(Config);
            Config.OnConfigReceived.AddListener(new UnityAction(ConfigReceived));
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
            //Directory.CreateDirectory(modDirectory + "/Localization");
            
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
            bool textureLoaded = cachedTextures.ContainsKey(fileName);
            Texture2D result;
            if (textureLoaded)
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

        private static Piece CreatePiece(string name, string description, Piece component, bool? isGrounded = null, bool canBeRemoved = true)
        {
            component.m_name = $"$pe{name}";
            component.m_description = $"$pe{description}";
            component.m_category = Piece.PieceCategory.Misc;
            component.m_cultivatedGroundOnly = (name.Contains("berryBush") || name.Contains("Pickable")) && config.RequireCultivation;
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
            prefabRefs.Add("FirCone", null);
            prefabRefs.Add("PineCone", null);
            prefabRefs.Add("shrub_2", null);
            prefabRefs.Add("shrub_2_heath", null);
            prefabRefs.Add("BirchSeeds", null);
            prefabRefs.Add("Acorn", null);
            prefabRefs.Add("BeechSeeds", null);
            prefabRefs.Add("Pickable_Mushroom", null);
            prefabRefs.Add("BlueberryBush", null);
            prefabRefs.Add("RaspberryBush", null);
            prefabRefs.Add("Pickable_Mushroom_blue", null);
            prefabRefs.Add("Pickable_Mushroom_yellow", null);
            prefabRefs.Add("Beech_Sapling", null);
            prefabRefs.Add("PineTree_Sapling", null);
            prefabRefs.Add("FirTree_Sapling", null);
            prefabRefs.Add("Oak_Sapling", null);
            prefabRefs.Add("Birch_Sapling", null);
            prefabRefs.Add("vfx_Place_wood_pole", null);
            prefabRefs.Add("sfx_build_cultivator", null);
            //prefabRefs.Add("Spawner_GreydwarfNest", null);
            

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

            //prefabRefs.Add("BirchCone", CreatePrefab("BirchCone"));
            //prefabRefs.Add("OakSeeds", CreatePrefab("OakSeeds"));
            prefabRefs.Add("AncientSeeds", CreatePrefab("AncientSeeds"));
            //prefabRefs.Add("Birch_Sapling", CreatePrefab("Birch_Sapling"));
            //prefabRefs.Add("Oak_Sapling", CreatePrefab("Oak_Sapling"));
            prefabRefs.Add("Ancient_Sapling", CreatePrefab("Ancient_Sapling"));
        }

        private static void InitPieceRefs()
        {
            Dbgl("InitPieceRefs");

            if (pieceRefs.Count > 0)
            {
                foreach (PrefabDB pdb in pieceRefs)
                {
                    if (prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Contains(pdb.Prefab))
                    {
                        prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(pdb.Prefab);
                    }
                    Destroy(pdb.Prefab.GetComponent<Piece>());
                }
                pieceRefs.Clear();
            }

            pieceRefs = new List<PrefabDB>()
            {
                new PrefabDB
                {
                    key = "RaspberryBush",
                    resourceCost = config.RaspberryCost,
                    resourceReturn = config.RaspberryReturn,
                    respawnTime = config.RaspberryRespawnTime,
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    recover = config.RecoverResources,
                    piece = CreatePiece("RaspberryBushName", "RaspberryBushDescription", prefabRefs["RaspberryBush"].AddComponent<Piece>())
                },
                new PrefabDB
                {
                    key = "BlueberryBush",
                    resourceCost = config.BlueberryCost,
                    resourceReturn = config.BlueberryReturn,
                    respawnTime = config.BlueberryRespawnTime,
                    biome = (int)Heightmap.Biome.BlackForest,
                    icon = true,
                    recover = config.RecoverResources,
                    piece = CreatePiece("BlueberryBushName", "BlueBerryBushDescription", prefabRefs["BlueberryBush"].AddComponent<Piece>())
                },
                new PrefabDB
                {
                    key = "CloudberryBush",
                    resourceCost = config.CloudberryCost,
                    resourceReturn = config.CloudberryReturn,
                    respawnTime = config.CloudberryRespawnTime,
                    biome = (int)Heightmap.Biome.Plains,
                    icon = true,
                    recover = config.RecoverResources,
                    piece = CreatePiece("CloudberryBushName", "CloudberryBushDescription", prefabRefs["CloudberryBush"].AddComponent<Piece>())
                },
                new PrefabDB
                {
                    key = "Pickable_Mushroom",
                    resourceCost = config.MushroomCost,
                    resourceReturn = config.MushroomReturn,
                    respawnTime = config.MushroomRespawnTime,
                    biome = 9,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableMushroomName", "PickableMushroomDescription", prefabRefs["Pickable_Mushroom"].AddComponent<Piece>(), isGrounded: true)
                },
                new PrefabDB
                {
                    key = "Pickable_Mushroom_yellow",
                    resourceCost = config.YellowMushroomCost,
                    resourceReturn = config.YellowMushroomReturn,
                    respawnTime = config.YellowMushroomRespawnTime,
                    biome = 10,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableYellowMushroomName", "PickableYellowMushroomDescription", prefabRefs["Pickable_Mushroom_yellow"].AddComponent<Piece>(), isGrounded: true)
                },
                new PrefabDB
                {
                    key = "Pickable_Mushroom_blue",
                    resourceCost = config.BlueMushroomCost,
                    resourceReturn = config.BlueMushroomReturn,
                    respawnTime = config.BlueMushroomRespawnTime,
                    biome = 10,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableBlueMushroomName", "PickableBlueMushroomDescription", prefabRefs["Pickable_Mushroom_blue"].AddComponent<Piece>(), isGrounded: true)
                },
                new PrefabDB
                {
                    key = "Pickable_Thistle",
                    resourceCost = config.ThistleCost,
                    resourceReturn = config.ThistleReturn,
                    respawnTime = config.ThistleRespawnTime,
                    biome = 10,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableThistleName", "PickableThistleDescription", prefabRefs["Pickable_Thistle"].AddComponent<Piece>(), isGrounded: true)
                },
                new PrefabDB
                {
                    key = "Pickable_Dandelion",
                    resourceCost = config.DandelionCost,
                    resourceReturn = config.DandelionReturn,
                    respawnTime = config.DandelionRespawnTime,
                    biome = (int)Heightmap.Biome.Meadows,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableDandelionName", "PickableDandelionDescription", prefabRefs["Pickable_Dandelion"].AddComponent<Piece>(), isGrounded: true)
                }
            };

            if (!config.EnableMiscFlora) return;

            pieceRefs.AddRange(new List<PrefabDB>()
            {
                new PrefabDB
                {
                    key = "Beech_small1",
                    Resource = new KeyValuePair<string, int>("BeechSeeds", 1),
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    piece = CreatePiece("BeechSmallName", "BeechSmallDescription", prefabRefs["Beech_small1"].AddComponent<Piece>(), canBeRemoved: false)
                },
                new PrefabDB
                {
                    key = "FirTree_small",
                    Resource = new KeyValuePair<string, int>("FirCone", 1),
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    piece = CreatePiece("FirSmallName", "FirSmallDescription", prefabRefs["FirTree_small"].AddComponent<Piece>(), canBeRemoved: false)
                },
                new PrefabDB
                {
                    key = "FirTree_small_dead",
                    Resource = new KeyValuePair<string, int>("FirCone", 1),
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    piece = CreatePiece("FirSmallDeadName", "FirSmallDeadDescription", prefabRefs["FirTree_small_dead"].AddComponent<Piece>(), canBeRemoved: false)
                },
                new PrefabDB
                {
                    key = "Bush01",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    piece = CreatePiece("Bush01Name", "Bush01Description", prefabRefs["Bush01"].AddComponent<Piece>(), canBeRemoved: false)
                },
                new PrefabDB
                {
                    key = "Bush01_heath",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                   piece = CreatePiece("Bush02Name", "Bush02Description", prefabRefs["Bush01_heath"].AddComponent<Piece>(), canBeRemoved: false)
                },
                new PrefabDB
                {
                    key = "Bush02_en",
                    Resource = new KeyValuePair<string, int>("Wood", 3),
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    piece = CreatePiece("PlainsBushName", "PlainsBushDescription", prefabRefs["Bush02_en"].AddComponent<Piece>(), canBeRemoved: false)
                },
                new PrefabDB
                {
                    key = "shrub_2",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    piece = CreatePiece("Shrub01Name", "Shrub01Description", prefabRefs["shrub_2"].AddComponent<Piece>(), canBeRemoved: false)
                },
                new PrefabDB
                {
                    key = "shrub_2_heath",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    piece = CreatePiece("Shrub02Name", "Shrub02Description", prefabRefs["shrub_2_heath"].AddComponent<Piece>(), canBeRemoved: false)
                },
                new PrefabDB
                {
                    key = "vines",
                    Resource = new KeyValuePair<string, int>("Wood", 2),
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    recover = true,
                    piece = CreatePiece("VinesName", "VinesDescription", prefabRefs["vines"].AddComponent<Piece>(), isGrounded: false)
                },
                new PrefabDB
                {
                    key = "GlowingMushroom",
                    Resources = new Dictionary<string, int>(){ {"MushroomYellow", 3}, { "BoneFragments", 1 }, { "Ooze", 1} },
                    biome = (int)Heightmap.Biome.Meadows,
                    icon = true,
                    recover = true,
                    piece = CreatePiece("GlowingMushroomName", "GlowingMushroomDescription", prefabRefs["GlowingMushroom"].AddComponent<Piece>(), isGrounded: true, canBeRemoved: true)
                }
            });
            //pieceRefs.Add("Spawner_GreydwarfNest", new PrefabDB
            //{
            //    key = "Spawner_GreydwarfNest",
            //    resource = "AncientSeed",
            //    resourceCost = 10,
            //    biome = (int)Heightmap.Biome.Meadows,
            //    piece = CreatePiece("GreydwarfNestName", "GreydwarfNestDescription", prefabRefs["Spawner_GreydwarfNest"].AddComponent<Piece>(), true, false)
            //});
        }

        private static void InitPieces()
        {
            Dbgl("InitPieces");

            foreach (PrefabDB pdb in pieceRefs)
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
                            m_amount = pdb.resourceCost,
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
                        if (config.AlwaysShowSpawners)
                        {
                            pickable.m_hideWhenPicked = pdb.key.Equals("Pickable_Thistle") ? pdb.Prefab.transform.Find("visual").Find("flare").gameObject : null;
                        }
                        else
                        {
                            pickable.m_hideWhenPicked = pdb.Prefab.transform.Find("visual").gameObject;
                        }
                    }
                }

                if (config.EnforceBiomes)
                {
                    pdb.piece.m_onlyInBiome = (Heightmap.Biome)pdb.biome;
                }

                if (pdb.icon && !config.AlternateIcons)
                {
                    pdb.piece.m_icon = CreateSprite($"{pdb.key}PieceIcon.png", new Rect(0, 0, 64, 64));
                }
                else
                {
                    pdb.piece.m_icon = resource.m_itemData.GetIcon();
                }
            }
        }

        private static void InitSaplingRefs()
        {
            Dbgl("InitSaplingRefs");

            if (saplingRefs.Count > 0)
            {
                saplingRefs.Clear();
            }

            saplingRefs = new List<SaplingDB>()
            {
                //new SaplingDB
                //{
                //    key = "Birch_Sapling",
                //    source = "PineTree_Sapling",
                //    resource = "BirchCone",
                //    resourceCost = config.BirchCost,
                //    biome = 17,
                //    growTime = config.BirchGrowthTime,
                //    growRadius = config.BirchGrowRadius,
                //    minScale = config.BirchMinScale,
                //    maxScale = config.BirchMaxScale,
                //    grownPrefabs = new GameObject[] { prefabRefs["Birch1"], prefabRefs["Birch2"], prefabRefs["Birch1_aut"], prefabRefs["Birch2_aut"] }
                //},
                //new SaplingDB
                //{
                //    key = "Oak_Sapling",
                //    source = "Beech_Sapling",
                //    resource = "OakSeeds",
                //    resourceCost = config.OakCost,
                //    biome = 17,
                //    growTime = config.OakGrowthTime,
                //    growRadius = config.OakGrowRadius,
                //    minScale = config.OakMinScale,
                //    maxScale = config.OakMaxScale,
                //    grownPrefabs = new GameObject[] { prefabRefs["Oak1"] }
                //},
                new SaplingDB
                {
                    key = "Ancient_Sapling",
                    source = "PineTree_Sapling",
                    resource = "AncientSeeds",
                    resourceCost = 1,
                    biome = (int)Heightmap.Biome.Swamp,
                    growTime = config.AncientGrowthTime,
                    growRadius = config.AncientGrowRadius,
                    minScale = config.AncientMinScale,
                    maxScale = config.AncientMaxScale,
                    grownPrefabs = new GameObject[] { prefabRefs["SwampTree1"] }
                }
            };
        }

        private static void InitSaplings()
        {
            Dbgl("InitSaplings");

            if (!isInitialized)
            {
                //FixSeed("BirchCone", prefabRefs["PineCone"]);
                //FixSeed("OakSeeds", prefabRefs["BeechSeeds"]);
                FixSeed("AncientSeeds", prefabRefs["BeechSeeds"]);

                ModifyTreeDrops();
            }

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

        private static void InitItems(ObjectDB instance)
        {
            Dbgl("InitItems");

            //if (!instance.m_items.Contains(prefabRefs["BirchCone"])) instance.m_items.Add(prefabRefs["BirchCone"]);
            //if (!instance.m_items.Contains(prefabRefs["OakSeeds"])) instance.m_items.Add(prefabRefs["OakSeeds"]);
            if (!instance.m_items.Contains(prefabRefs["AncientSeeds"])) instance.m_items.Add(prefabRefs["AncientSeeds"]);
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
                if (!cultivator.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(pieceRefs[i].Prefab))
                    cultivator.m_itemData.m_shared.m_buildPieces.m_pieces.Add(pieceRefs[i].Prefab);
            }

            cultivator.m_itemData.m_shared.m_buildPieces.m_canRemovePieces = true;
            //cultivator.m_itemData.m_shared.m_buildPieces.m_useCategories = true;
        }

        private static void FinalInit(ZNetScene __instance)
        {
            InitPieceRefs();
            InitPieces();
            InitSaplingRefs();
            InitSaplings();
            InitCultivator();

            if (stringDictionary.Count > 0)
            {
                InitLocalization();
            }

            List<GameObject> prefabs = new()
            {
                //prefabRefs["BirchCone"],
                //prefabRefs["OakSeeds"],
                prefabRefs["AncientSeeds"],
                //prefabRefs["Birch_Sapling"],
                //prefabRefs["Oak_Sapling"],
                prefabRefs["Ancient_Sapling"]
            };

            foreach (GameObject prefab in prefabs)
            {
                if (!__instance.m_prefabs.Contains(prefab))
                    __instance.m_prefabs.Add(prefab);
            }
        }

        private static void FixSeed(string name, GameObject source)
        {
            GameObject prefab = prefabRefs[name];
            // prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0] = source.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0];

            if (source == prefabRefs["PineCone"])
            {
                prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0] = CreateSprite("birchConeItemIcon.png", new Rect(0, 0, 64, 64));
                prefab.transform.Find("cone").gameObject.GetComponent<MeshFilter>().mesh = source.transform.Find("cone").gameObject.GetComponent<MeshFilter>().mesh;
                prefab.transform.Find("cone").gameObject.GetComponent<MeshRenderer>().sharedMaterials = source.transform.Find("cone").gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                prefab.transform.Find("cone").gameObject.GetComponent<MeshCollider>().sharedMesh = source.transform.Find("cone").gameObject.GetComponent<MeshCollider>().sharedMesh;
                //prefab.GetComponent<ParticleSystem>(). // particle renderer could be copied here but unsure how
            }

            if (source == prefabRefs["BeechSeeds"])
            {
                string fileName = name.Equals("OakSeeds") ? "oakSeedsItemIcon.png" : "ancientSeedsItemIcon.png";
                prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0] = CreateSprite(fileName, new Rect(0, 0, 64, 64));
                prefab.transform.Find("Sphere (5)").gameObject.GetComponent<MeshFilter>().mesh = source.transform.Find("Sphere (5)").gameObject.GetComponent<MeshFilter>().mesh;
                prefab.transform.Find("Sphere (5)").gameObject.GetComponent<MeshRenderer>().sharedMaterials = source.transform.Find("Sphere (5)").gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                prefab.transform.Find("Sphere (6)").gameObject.GetComponent<MeshFilter>().mesh = source.transform.Find("Sphere (6)").gameObject.GetComponent<MeshFilter>().mesh;
                prefab.transform.Find("Sphere (6)").gameObject.GetComponent<MeshRenderer>().sharedMaterials = source.transform.Find("Sphere (5)").gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                prefab.transform.Find("Sphere (7)").gameObject.GetComponent<MeshFilter>().mesh = source.transform.Find("Sphere (7)").gameObject.GetComponent<MeshFilter>().mesh;
                prefab.transform.Find("Sphere (7)").gameObject.GetComponent<MeshRenderer>().sharedMaterials = source.transform.Find("Sphere (5)").gameObject.GetComponent<MeshRenderer>().sharedMaterials;
            }
        }

        private static void ModifyTreeDrops()
        {
            //Update later to add config options for seed drop rates
            Dictionary<GameObject, GameObject> dropsByTarget = new()
            {
                { prefabRefs["Birch1"], prefabRefs["BirchSeeds"] },
                { prefabRefs["Birch2"], prefabRefs["BirchSeeds"] },
                { prefabRefs["Birch2_aut"], prefabRefs["BirchSeeds"] },
                { prefabRefs["Birch1_aut"], prefabRefs["BirchSeeds"] },
                { prefabRefs["Oak1"], prefabRefs["Acorn"] },
                { prefabRefs["SwampTree1"], prefabRefs["AncientSeeds"] },
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
                target.m_dropWhenDestroyed.m_drops.Add(itemDrop);
                target.m_dropWhenDestroyed.m_dropChance = Mathf.Clamp(config.DropChance, 0f, 1f);
                target.m_dropWhenDestroyed.m_oneOfEach = config.OneOfEach;
            }
        }

        private static void ConfigReceived()
        {
            Dbgl("Config Received, re-initializing mod");
            FinalInit(ZNetScene.instance);
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

        private struct PrefabDB
        {
            internal string key;
            private Dictionary<string, int> resources;

            internal int resourceCost;
            internal int resourceReturn;
            internal int respawnTime;
            internal int biome;

            internal bool icon;
            internal bool recover;

            internal Piece piece;

            internal GameObject Prefab
            {
                get { return prefabRefs[key]; }
            }

            internal KeyValuePair<string, int> Resource
            {
                get { return Resources.Count > 0 ? Resources.First() : new KeyValuePair<string, int>(Prefab.GetComponent<Pickable>().m_itemPrefab.name, resourceCost); }
                set { if (resources == null) { resources = new Dictionary<string, int>(); } if (!resources.ContainsKey(value.Key)) resources.Add(value.Key, value.Value); }
            }

            internal Dictionary<string, int> Resources
            {
                get { return resources ?? new Dictionary<string, int>(); }
                set { resources = value; }
            }
        }

        private struct SaplingDB
        {
            internal string key;
            internal string source;
            internal string resource;

            internal int resourceCost;
            internal int biome;

            internal float growTime;
            internal float growRadius;
            internal float minScale;
            internal float maxScale;

            internal GameObject[] grownPrefabs;
            internal GameObject Prefab
            {
                get { return prefabRefs[key]; }
            }
        }
    }
}
