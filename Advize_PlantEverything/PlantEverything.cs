using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Advize_PlantEverything.Configuration;
using UnityEngine.Events;

namespace Advize_PlantEverything
{
    [BepInPlugin(PluginID, PluginName, Version)]
    public partial class PlantEverything : BaseUnityPlugin
    {
        public const string PluginID = "advize.PlantEverything";
        public const string PluginName = "PlantEverything";
        public const string Version = "1.4.0";

        private readonly Harmony harmony = new Harmony(PluginID);

        private static readonly Dictionary<string, GameObject> prefabRefs = new Dictionary<string, GameObject>();
        private static List<PrefabDB> pieceRefs = new List<PrefabDB>();
        private static List<SaplingDB> saplingRefs = new List<SaplingDB>();

        private static bool isInitialized = false; 

        private static readonly string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly AssetBundle assetBundle = LoadAssetBundle("planteverything");
        private static readonly Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();

        private new Config Config
        {
            get { return Config.Instance; }
        }
        internal static ModConfig config;

        private static readonly Dictionary<string, string> stringDictionary = new Dictionary<string, string>() {
            { "BirchConeName", "Birch Cone" },
            { "BirchConeDescription", "Plant it to grow a birch tree." },
            { "OakSeedsName", "Oak Seeds" },
            { "OakSeedsDescription", "Plant them to grow an oak tree." },
            { "AncientSeedsName", "Ancient Seeds" },
            { "AncientSeedsDescription", "Plant them to grow an ancient tree." },
            { "BirchSapling", "Birch Sapling" },
            { "OakSapling", "Oak Sapling" },
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
            { "VinesDescription", "Plant vines." }/*,
            { "GreydwarfNestName", "Greydwarf Nest" },
            { "GreydwarfNestDescription", "Plant your very own greydwarf nest" }*/
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called Implicitly")]
        private void Awake()
        {
            Config.Init(this, true);
            config = new ModConfig(Config);
            Config.OnConfigReceived.AddListener(new UnityAction(ConfigReceived));
            if (config.EnableLocalization)
                LoadLocalizedStrings();
            harmony.PatchAll();
        }

        private void LoadLocalizedStrings()
        {
            string fileName = $"{config.Language}_PlantEverything.json";
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
            
            string filePath = Path.Combine(modDirectory, "english_PlantEverything.json");

            LocalizedStrings localizedStrings = new LocalizedStrings();
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
                string str = $"{PluginName}: {message}";
                
                if (logError)
                {
                    Debug.LogError(str);
                }
                else
                {
                    Debug.Log(str);
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
                Texture2D texture = new Texture2D(0, 0);
                ImageConversion.LoadImage(texture, array);
                result = texture;
            }

            return result;
        }

        private static Piece CreatePiece(string name, string description, Piece component, bool isGrounded, bool canBeRemoved = true)
        {
            component.m_name = $"$pe{name}";
            component.m_description = $"$pe{description}";
            component.m_category = Piece.PieceCategory.Misc;
            component.m_cultivatedGroundOnly = config.RequireCultivation;
            component.m_groundOnly = component.m_groundPiece = isGrounded/* || !config.PlaceAnywhere*/;
            component.m_canBeRemoved = canBeRemoved;
            return component;
        }

        private static void InitPrefabRefs()
        {
            Dbgl("PlantEverything: InitPrefabRefs");
            if (prefabRefs.Count > 0)
            {
                return;
            }
            prefabRefs.Add("RaspberryBush", null);
            prefabRefs.Add("BlueberryBush", null);
            prefabRefs.Add("CloudberryBush", null);
            prefabRefs.Add("Pickable_Mushroom", null);
            prefabRefs.Add("Pickable_Mushroom_yellow", null);
            prefabRefs.Add("Pickable_Mushroom_blue", null);
            prefabRefs.Add("Pickable_Thistle", null);
            prefabRefs.Add("Pickable_Dandelion", null);
            
            prefabRefs.Add("SwampTree1", null);
            prefabRefs.Add("Birch2", null);
            prefabRefs.Add("Oak1", null);
            prefabRefs.Add("Birch1", null);

            prefabRefs.Add("Beech_Sapling", null);
            prefabRefs.Add("PineTree_Sapling", null);

            prefabRefs.Add("PineCone", null);
            prefabRefs.Add("BeechSeeds", null);
            prefabRefs.Add("FirCone", null);

            prefabRefs.Add("vfx_Place_wood_pole", null);
            prefabRefs.Add("sfx_build_cultivator", null);

            prefabRefs.Add("vfx_SawDust", null);
            prefabRefs.Add("sfx_bush_hit", null);

            prefabRefs.Add("Beech_small1", null);
            prefabRefs.Add("FirTree_small", null);
            prefabRefs.Add("FirTree_small_dead", null);
            prefabRefs.Add("Bush01", null);
            prefabRefs.Add("Bush01_heath", null);
            prefabRefs.Add("Bush02_en", null);
            prefabRefs.Add("shrub_2", null);
            prefabRefs.Add("shrub_2_heath", null);
            prefabRefs.Add("vines", null);
            prefabRefs.Add("Cultivator", null);
            //prefabRefs.Add("Spawner_GreydwarfNest", null);

            UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(GameObject));
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

            prefabRefs.Add("BirchCone", CreatePrefab("BirchCone"));
            prefabRefs.Add("OakSeeds", CreatePrefab("OakSeeds"));
            prefabRefs.Add("AncientSeeds", CreatePrefab("AncientSeeds"));
            prefabRefs.Add("Birch_Sapling", CreatePrefab("Birch_Sapling"));
            prefabRefs.Add("Oak_Sapling", CreatePrefab("Oak_Sapling"));
            prefabRefs.Add("Ancient_Sapling", CreatePrefab("Ancient_Sapling"));
        }

        private static void InitPieceRefs()
        {
            Dbgl("PlantEverything: InitPieceRefs");

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
                    icon = "raspberryBushPieceIcon",
                    resourceCost = config.RaspberryCost,
                    resourceReturn = config.RaspberryReturn,
                    respawnTime = config.RaspberryRespawnTime,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("RaspberryBushName", "RaspberryBushDescription", prefabRefs["RaspberryBush"].AddComponent<Piece>(), true)
                },
                new PrefabDB
                {
                    key = "BlueberryBush",
                    icon = "blueberryBushPieceIcon",
                    resourceCost = config.BlueberryCost,
                    resourceReturn = config.BlueberryReturn,
                    respawnTime = config.BlueberryRespawnTime,
                    biome = (int)Heightmap.Biome.BlackForest,
                    piece = CreatePiece("BlueberryBushName", "BlueBerryBushDescription", prefabRefs["BlueberryBush"].AddComponent<Piece>(), true)
                },
                new PrefabDB
                {
                    key = "CloudberryBush",
                    icon = "cloudberryBushPieceIcon",
                    resourceCost = config.CloudberryCost,
                    resourceReturn = config.CloudberryReturn,
                    respawnTime = config.CloudberryRespawnTime,
                    biome = (int)Heightmap.Biome.Plains,
                    piece = CreatePiece("CloudberryBushName", "CloudberryBushDescription", prefabRefs["CloudberryBush"].AddComponent<Piece>(), true)
                },
                new PrefabDB
                {
                    key = "Pickable_Mushroom",
                    resourceCost = config.MushroomCost,
                    resourceReturn = config.MushroomReturn,
                    respawnTime = config.MushroomRespawnTime,
                    biome = 9,
                    piece = CreatePiece("PickableMushroomName", "PickableMushroomDescription", prefabRefs["Pickable_Mushroom"].AddComponent<Piece>(), true)
                },
                new PrefabDB
                {
                    key = "Pickable_Mushroom_yellow",
                    resourceCost = config.YellowMushroomCost,
                    resourceReturn = config.YellowMushroomReturn,
                    respawnTime = config.YellowMushroomRespawnTime,
                    biome = 10,
                    piece = CreatePiece("PickableYellowMushroomName", "PickableYellowMushroomDescription", prefabRefs["Pickable_Mushroom_yellow"].AddComponent<Piece>(), true)
                },
                new PrefabDB
                {
                    key = "Pickable_Mushroom_blue",
                    resourceCost = config.BlueMushroomCost,
                    resourceReturn = config.BlueMushroomReturn,
                    respawnTime = config.BlueMushroomRespawnTime,
                    biome = 10,
                    piece = CreatePiece("PickableBlueMushroomName", "PickableBlueMushroomDescription", prefabRefs["Pickable_Mushroom_blue"].AddComponent<Piece>(), true)
                },
                new PrefabDB
                {
                    key = "Pickable_Thistle",
                    resourceCost = config.ThistleCost,
                    resourceReturn = config.ThistleReturn,
                    respawnTime = config.ThistleRespawnTime,
                    biome = 10,
                    piece = CreatePiece("PickableThistleName", "PickableThistleDescription", prefabRefs["Pickable_Thistle"].AddComponent<Piece>(), true)
                },
                new PrefabDB
                {
                    key = "Pickable_Dandelion",
                    resourceCost = config.DandelionCost,
                    resourceReturn = config.DandelionReturn,
                    respawnTime = config.DandelionRespawnTime,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("PickableDandelionName", "PickableDandelionDescription", prefabRefs["Pickable_Dandelion"].AddComponent<Piece>(), true)
                }
            };

            if (!config.EnableMiscFlora) return;

            pieceRefs.AddRange(new List<PrefabDB>()
            {
                new PrefabDB
                {
                    key = "Beech_small1",
                    Resource = "BeechSeeds",
                    resourceCost = 1,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("BeechSmallName", "BeechSmallDescription", prefabRefs["Beech_small1"].AddComponent<Piece>(), true, false)
                },
                new PrefabDB
                {
                    key = "FirTree_small",
                    Resource = "FirCone",
                    resourceCost = 1,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("FirSmallName", "FirSmallDescription", prefabRefs["FirTree_small"].AddComponent<Piece>(), true, false)
                },
                new PrefabDB
                {
                    key = "FirTree_small_dead",
                    Resource = "FirCone",
                    resourceCost = 1,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("FirSmallDeadName", "FirSmallDeadDescription", prefabRefs["FirTree_small_dead"].AddComponent<Piece>(), true, false)
                },
                new PrefabDB
                {
                    key = "Bush01",
                    Resource = "Wood",
                    resourceCost = 2,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("Bush01Name", "Bush01Description", prefabRefs["Bush01"].AddComponent<Piece>(), true, false)
                },
                new PrefabDB
                {
                    key = "Bush01_heath",
                   Resource = "Wood",
                   resourceCost = 2,
                    biome = (int)Heightmap.Biome.Meadows,
                   piece = CreatePiece("Bush02Name", "Bush02Description", prefabRefs["Bush01_heath"].AddComponent<Piece>(), true, false)
                },
                new PrefabDB
                {
                    key = "Bush02_en",
                    Resource = "Wood",
                    resourceCost = 3,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("PlainsBushName", "PlainsBushDescription", prefabRefs["Bush02_en"].AddComponent<Piece>(), true, false)
                },
                new PrefabDB
                {
                    key = "shrub_2",
                    Resource = "Wood",
                    resourceCost = 3,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("Shrub01Name", "Shrub01Description", prefabRefs["shrub_2"].AddComponent<Piece>(), true, false)
                },
                new PrefabDB
                {
                    key = "shrub_2_heath",
                    Resource = "Wood",
                    resourceCost = 2,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("Shrub02Name", "Shrub02Description", prefabRefs["shrub_2_heath"].AddComponent<Piece>(), true, false)
                },
                new PrefabDB
                {
                    key = "vines",
                    Resource = "Wood",
                    resourceCost = 2,
                    biome = (int)Heightmap.Biome.Meadows,
                    piece = CreatePiece("VinesName", "VinesDescription", prefabRefs["vines"].AddComponent<Piece>(), false)
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
            Dbgl("PlantEverything: InitPieces");

            foreach (PrefabDB pdb in pieceRefs)
            {
                ItemDrop resource = ObjectDB.instance.GetItemPrefab(pdb.Resource).GetComponent<ItemDrop>();

                pdb.piece.m_resources = new Piece.Requirement[]
                {
                    new Piece.Requirement
                    {
                        m_resItem = resource,
                        m_amount = pdb.resourceCost,
                        m_recover = false
                    }
                };

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

                if (pdb.Prefab.GetComponent<Pickable>() != null)
                {
                    pdb.Prefab.GetComponent<Pickable>().m_respawnTimeMinutes = pdb.respawnTime;
                    pdb.Prefab.GetComponent<Pickable>().m_amount = pdb.resourceReturn;
                }

                if (config.EnforceBiomes)
                {
                    pdb.piece.m_onlyInBiome = (Heightmap.Biome)pdb.biome;
                }

                if (pdb.icon != null && !config.AlternateIcons)
                {
                    pdb.piece.m_icon = CreateSprite(pdb.icon + ".png", new Rect(0, 0, 64, 64));
                }
                else
                {
                    pdb.piece.m_icon = resource.m_itemData.GetIcon();
                }
            }
        }

        private static void InitSaplingRefs()
        {
            Dbgl("PlantEverything: InitSaplingRefs");

            if (saplingRefs.Count > 0)
            {
                saplingRefs.Clear();
            }

            saplingRefs = new List<SaplingDB>()
            {
                new SaplingDB
                {
                    key = "Birch_Sapling",
                    source = "PineTree_Sapling",
                    resource = "BirchCone",
                    resourceCost = config.BirchCost,
                    biome = 17,
                    growTime = config.BirchGrowthTime,
                    growRadius = config.BirchGrowRadius,
                    minScale = config.BirchMinScale,
                    maxScale = config.BirchMaxScale,
                    grownPrefabs = new GameObject[] { prefabRefs["Birch1"], prefabRefs["Birch2"] }
                },
                new SaplingDB
                {
                    key = "Oak_Sapling",
                    source = "Beech_Sapling",
                    resource = "OakSeeds",
                    resourceCost = config.OakCost,
                    biome = 17,
                    growTime = config.OakGrowthTime,
                    growRadius = config.OakGrowRadius,
                    minScale = config.OakMinScale,
                    maxScale = config.OakMaxScale,
                    grownPrefabs = new GameObject[] { prefabRefs["Oak1"] }
                },
                new SaplingDB
                {
                    key = "Ancient_Sapling",
                    source = "PineTree_Sapling",
                    resource = "AncientSeeds",
                    resourceCost = config.AncientCost,
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
            Dbgl("PlantEverything: InitSaplings");

            if (!isInitialized)
            {
                FixSeed("BirchCone", prefabRefs["PineCone"]);
                FixSeed("OakSeeds", prefabRefs["BeechSeeds"]);
                FixSeed("AncientSeeds", prefabRefs["BeechSeeds"]);

                ModifyTreeDrops(); 
            }

            prefabRefs["Beech_Sapling"].GetComponent<Plant>().m_growRadius = config.BeechGrowRadius;
            prefabRefs["PineTree_Sapling"].GetComponent<Plant>().m_growRadius = config.PineGrowRadius;

            foreach (SaplingDB sdb in saplingRefs)
            {
                Plant plant = sdb.Prefab.GetComponent<Plant>();
                Piece piece = sdb.Prefab.GetComponent<Piece>();

                plant.m_growTime = plant.m_growTimeMax = sdb.growTime;
                plant.m_grownPrefabs = sdb.grownPrefabs;
                plant.m_minScale = sdb.minScale;
                plant.m_maxScale = sdb.maxScale;
                plant.m_needCultivatedGround = config.RequireCultivation;
                plant.m_growRadius = sdb.growRadius;

                piece.m_resources[0].m_resItem = prefabRefs[sdb.resource].GetComponent<ItemDrop>();
                piece.m_resources[0].m_amount = sdb.resourceCost;

                if (config.EnforceBiomes)
                {
                    piece.m_onlyInBiome = (Heightmap.Biome)sdb.biome;
                }

                if (isInitialized) continue;

                sdb.Prefab.transform.Find("healthy").gameObject.GetComponent<MeshFilter>().mesh = prefabRefs[sdb.source].transform.Find("healthy").gameObject.GetComponent<MeshFilter>().mesh;
                sdb.Prefab.transform.Find("healthy").gameObject.GetComponent<MeshRenderer>().sharedMaterials = prefabRefs[sdb.source].transform.Find("healthy").gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                sdb.Prefab.transform.Find("unhealthy").gameObject.GetComponent<MeshFilter>().mesh = prefabRefs[sdb.source].transform.Find("unhealthy").gameObject.GetComponent<MeshFilter>().mesh;
                sdb.Prefab.transform.Find("unhealthy").gameObject.GetComponent<MeshRenderer>().sharedMaterials = prefabRefs[sdb.source].transform.Find("unhealthy").gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                //sdb.Prefab.GetComponent<Piece>().m_icon = source.GetComponent<Piece>().m_icon;
                piece.m_icon = piece.m_resources[0].m_resItem.m_itemData.GetIcon();
                piece.m_placeEffect.m_effectPrefabs[0].m_prefab = prefabRefs["vfx_Place_wood_pole"];
                piece.m_placeEffect.m_effectPrefabs[1].m_prefab = prefabRefs["sfx_build_cultivator"];
                piece.m_groundOnly = piece.m_groundPiece = !config.PlaceAnywhere;
                sdb.Prefab.GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[0].m_prefab = prefabRefs["sfx_bush_hit"];
                sdb.Prefab.GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[1].m_prefab = prefabRefs["vfx_SawDust"];
            }

            isInitialized = true;
        }

        private static void InitItems(ObjectDB instance)
        {
            Dbgl("PlantEverything: InitItems");

            if (!instance.m_items.Contains(prefabRefs["BirchCone"])) instance.m_items.Add(prefabRefs["BirchCone"]);
            if (!instance.m_items.Contains(prefabRefs["OakSeeds"])) instance.m_items.Add(prefabRefs["OakSeeds"]);
            if (!instance.m_items.Contains(prefabRefs["AncientSeeds"])) instance.m_items.Add(prefabRefs["AncientSeeds"]);
        }

        private static void InitCultivator(ObjectDB instance)
        {
            Dbgl("PlantEverything: InitCultivator");

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
        }

        private static void FinalInit(ZNetScene __instance)
        {
            InitPieceRefs();
            InitPieces();

            InitSaplingRefs();
            InitSaplings();
            
            List<GameObject> prefabs = new List<GameObject>
            {
                prefabRefs["BirchCone"],
                prefabRefs["OakSeeds"],
                prefabRefs["AncientSeeds"],
                prefabRefs["Birch_Sapling"],
                prefabRefs["Oak_Sapling"],
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
            Dictionary<GameObject, GameObject> dropsByTarget = new Dictionary<GameObject, GameObject>
            {
                { prefabRefs["Birch1"], prefabRefs["BirchCone"] },
                { prefabRefs["Birch2"], prefabRefs["BirchCone"] },
                { prefabRefs["Oak1"], prefabRefs["OakSeeds"] },
                { prefabRefs["SwampTree1"], prefabRefs["AncientSeeds"] }
            };

            foreach (KeyValuePair<GameObject, GameObject> kvp in dropsByTarget)
            {
                TreeBase target = kvp.Key.GetComponent<TreeBase>();
                DropTable.DropData item = default;

                item.m_item = kvp.Value;
                item.m_stackMin = 1;
                item.m_stackMax = 5;

                target.m_dropWhenDestroyed.m_drops.Add(item);
                target.m_dropWhenDestroyed.m_dropChance = 1f;
                target.m_dropWhenDestroyed.m_oneOfEach = true;
                target.m_dropWhenDestroyed.m_dropMin = 1;
                target.m_dropWhenDestroyed.m_dropMax = 3;
            }
        }

        private static void ConfigReceived()
        {
            Dbgl("Config Received, re-initializing mod");
            FinalInit(ZNetScene.instance);
        }

        internal class LocalizedStrings
        {
            public List<string> localizedStrings = new List<string>();
        }

        private struct PrefabDB
        {
            internal string key;
            internal string icon;
            private string m_resource;

            internal int resourceCost;
            internal int resourceReturn;
            internal int respawnTime;
            internal int biome;

            internal Piece piece;

            internal GameObject Prefab
            {
                get { return prefabRefs[key]; }
            }

            internal string Resource
            {
                get { return m_resource ?? Prefab.GetComponent<Pickable>().m_itemPrefab.name; }
                set { m_resource = value; }
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
