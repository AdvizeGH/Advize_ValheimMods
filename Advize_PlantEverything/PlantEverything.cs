using System;
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
        public const string Version = "1.15.1";

        private readonly Harmony harmony = new(PluginID);
        public static ManualLogSource PELogger = new($" {PluginName}");

        private static readonly Dictionary<string, GameObject> prefabRefs = new();
        private static List<PieceDB> pieceRefs = new();
        private static List<SaplingDB> saplingRefs = new();
        private static List<ExtraResource> deserializedExtraResources = new();
        private static string[] layersForPieceRemoval = { "item", "piece_nonsolid", "Default_small", "Default" };

        private static bool isInitialized = false;

        private static AssetBundle assetBundle;
        private static readonly Dictionary<string, Texture2D> cachedTextures = new();

        private static ModConfig config;

        private static readonly Dictionary<string, string> stringDictionary = new()
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

        public void Awake()
        {
            BepInEx.Logging.Logger.Sources.Add(PELogger);
            assetBundle = LoadAssetBundle("planteverything");
            config = new ModConfig(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "1.15.0" });
            SetupWatcher();
            if (config.EnableExtraResources)
                ExtraResourcesFileOrSettingChanged(null, null);
            if (config.EnableLocalization)
                LoadLocalizedStrings();
            harmony.PatchAll();
            Game.isModded = true;
            Dbgl("PlantEverything has loaded and [General]EnableDebugMessages is set to true in mod configuration file. Set to false to disable these messages.", level: LogLevel.Message);
        }

        private static string ModConfigDirectory()
        {
            string path = Path.Combine(Paths.ConfigPath, "PlantEverything");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private static string ReplaceGameObjectName(string name) => name.Replace("(Clone)", "");

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
                    config.SyncedExtraResources.AssignLocalValue(new());
                }
            }
            else
            {
                Dbgl("IsSourceOfTruth: false, extra resources will not be loaded from disk");
                // Currently if a client changes their local ExtraResources.cfg while on a server, their new data won't be loaded.
                // If they then leave server and join a single player game their originally loaded ExtraResources.cfg data is used, not the updated file.
            }
        }

        private static List<ExtraResource> GenerateExampleResources()
        {
            return new()
            {
                new("PE_FakePrefab1", "PretendSeeds1", 1),
                new("PE_FakePrefab2", "PretendSeeds2", 2, false)
            };
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
                        //Dbgl($"er1 {er.prefabName}, {er.resourceName}, {er.resourceCost}, {er.groundOnly}");
                    }
                    else
                    {
                        Dbgl($"Invalid resource configured in {fileName}, skipping entry", true, LogLevel.Warning);
                        continue;
                    }
                }

                Dbgl($"Loaded extra resources from {filePath}", true);
                //Dbgl($"deserializedExtraResources.Count is {deserializedExtraResources.Count}");
                Dbgl($"Assigning local value from deserializedExtraResources");

                List<string> resourcesToSync = new();

                foreach (ExtraResource er in deserializedExtraResources)
                    resourcesToSync.Add(SerializeExtraResource(er));

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
                InitExtraResources(ZNetScene.s_instance);
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
            string filePath = Path.Combine(ModConfigDirectory(), $"english_{PluginName}.json");

            LocalizedStrings localizedStrings = new();
            foreach (KeyValuePair<string, string> kvp in stringDictionary)
            {
                localizedStrings.localizedStrings.Add($"{kvp.Key}:{kvp.Value}");
            }

            File.WriteAllText(filePath, JsonUtility.ToJson(localizedStrings, true));

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
                Dbgl("Unable to load texture", true, LogLevel.Error);
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

        private static Piece CreatePiece(string key, Piece component, bool? isGrounded = null, bool canBeRemoved = true, bool extraResource = false)
        {
            component.m_name = extraResource ? key : $"$pe{key}Name";
            component.m_description = extraResource ? "" : $"$pe{key}Description";
            component.m_category = Piece.PieceCategory.Misc;
            component.m_cultivatedGroundOnly = (key.Contains("berryBush") || key.Contains("Pickable")) && config.RequireCultivation;
            component.m_groundOnly = component.m_groundPiece = isGrounded ?? !config.PlaceAnywhere;
            component.m_canBeRemoved = canBeRemoved;
            component.m_targetNonPlayerBuilt = false;
            component.m_randomTarget = config.EnemiesTargetPieces;
            return component;
        }

        private static void InitPrefabRefs()
        {
            Dbgl("InitPrefabRefs");
            if (prefabRefs.Count > 0) return;

            bool foundAllRefs = false;

            prefabRefs.Add("Bush02_en", null);
            prefabRefs.Add("Bush01_heath", null);
            prefabRefs.Add("Bush01", null);
            prefabRefs.Add("GlowingMushroom", null);
            prefabRefs.Add("Pinetree_01", null);
            prefabRefs.Add("FirTree", null);
            prefabRefs.Add("YggaShoot_small1", null);
            prefabRefs.Add("Beech_small1", null);
            prefabRefs.Add("FirTree_small_dead", null);
            prefabRefs.Add("FirTree_small", null);
            prefabRefs.Add("Pickable_Dandelion", null);
            prefabRefs.Add("Sap", null);
            prefabRefs.Add("CloudberryBush", null);
            prefabRefs.Add("vines", null);
            prefabRefs.Add("Cultivator", null);
            prefabRefs.Add("SwampTree1", null);
            prefabRefs.Add("YggaShoot1", null);
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
            prefabRefs.Add("sapling_jotunpuffs", null);
            prefabRefs.Add("Birch_Sapling", null);
            prefabRefs.Add("sapling_carrot", null);
            prefabRefs.Add("sapling_seedcarrot", null);
            prefabRefs.Add("sapling_flax", null);
            prefabRefs.Add("sapling_magecap", null);
            prefabRefs.Add("sapling_seedturnip", null);
            prefabRefs.Add("vfx_Place_wood_pole", null);
            prefabRefs.Add("sfx_build_cultivator", null);
            prefabRefs.Add("YggaShoot3", null);
            prefabRefs.Add("YggaShoot2", null);

            foreach (ExtraResource er in deserializedExtraResources)
            {
                prefabRefs[er.prefabName] = null;
            }

            UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            for (int i = 0; i < array.Length; i++)
            {
                GameObject gameObject = ((GameObject)array[i]).transform.root.gameObject;

                if (!prefabRefs.ContainsKey(gameObject.name) || prefabRefs[gameObject.name]) continue;
                
                prefabRefs[gameObject.name] = gameObject;

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

                foreach (string s in nullKeys)
                {
                    Dbgl($"prefabRefs[{s}] value is null, removing key and value pair");
                    prefabRefs.Remove(s);
                }
            }

            prefabRefs.Add("Ancient_Sapling", CreatePrefab("Ancient_Sapling"));
            prefabRefs.Add("Ygga_Sapling", CreatePrefab("Ygga_Sapling"));
            prefabRefs.Add("Autumn_Birch_Sapling", CreatePrefab("Autumn_Birch_Sapling"));
            prefabRefs.Add("Pickable_Dandelion_Picked", CreatePrefab("Pickable_Dandelion_Picked"));
            prefabRefs.Add("Pickable_Thistle_Picked", CreatePrefab("Pickable_Thistle_Picked"));
            prefabRefs.Add("Pickable_Mushroom_Picked", CreatePrefab("Pickable_Mushroom_Picked"));
            prefabRefs.Add("Pickable_Mushroom_yellow_Picked", CreatePrefab("Pickable_Mushroom_yellow_Picked"));
            prefabRefs.Add("Pickable_Mushroom_blue_Picked", CreatePrefab("Pickable_Mushroom_blue_Picked"));
        }

        private static void InitExtraResources(ZNetScene instance)
        {
            Dbgl("InitExtraResources");

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
                    }
                    else
                    {
                        Dbgl($"Could not find prefab: {er.prefabName}");
                    }
                }
            }
        }

        private static void InitPieceRefs()
        {
            Dbgl("InitPieceRefs");

            if (pieceRefs.Count > 0)
            {
                RemoveFromCultivator(pieceRefs.ConvertAll(x => (PrefabDB)x), destroy: true);
                pieceRefs.Clear();
            }
            pieceRefs = GeneratePieceRefs();
        }

        private static Piece GetOrAddPieceComponent(GameObject go) => go.GetComponent<Piece>() ?? go.AddComponent<Piece>();

        private static List<PieceDB> GeneratePieceRefs()
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
                    recover = config.RecoverResources,
                    piece = CreatePiece("RaspberryBush", GetOrAddPieceComponent(prefabRefs["RaspberryBush"]))
                },
                new PieceDB
                {
                    key = "BlueberryBush",
                    ResourceCost = config.BlueberryCost,
                    resourceReturn = config.BlueberryReturn,
                    respawnTime = config.BlueberryRespawnTime,
                    biome = enforceBiomes ? (int)Heightmap.Biome.BlackForest : 0,
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
                    biome = enforceBiomes ? (int)Heightmap.Biome.Plains : 0,
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
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableMushroom", GetOrAddPieceComponent(prefabRefs["Pickable_Mushroom"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Mushroom_yellow",
                    ResourceCost = config.YellowMushroomCost,
                    resourceReturn = config.YellowMushroomReturn,
                    respawnTime = config.YellowMushroomRespawnTime,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableYellowMushroom", GetOrAddPieceComponent(prefabRefs["Pickable_Mushroom_yellow"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Mushroom_blue",
                    ResourceCost = config.BlueMushroomCost,
                    resourceReturn = config.BlueMushroomReturn,
                    respawnTime = config.BlueMushroomRespawnTime,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableBlueMushroom", GetOrAddPieceComponent(prefabRefs["Pickable_Mushroom_blue"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Thistle",
                    ResourceCost = config.ThistleCost,
                    resourceReturn = config.ThistleReturn,
                    respawnTime = config.ThistleRespawnTime,
                    biome = enforceBiomes ? (int)Heightmap.Biome.BlackForest : 0,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableThistle", GetOrAddPieceComponent(prefabRefs["Pickable_Thistle"]), isGrounded: true)
                },
                new PieceDB
                {
                    key = "Pickable_Dandelion",
                    ResourceCost = config.DandelionCost,
                    resourceReturn = config.DandelionReturn,
                    respawnTime = config.DandelionRespawnTime,
                    biome = enforceBiomes ? (int)Heightmap.Biome.Meadows : 0,
                    recover = config.RecoverResources,
                    piece = CreatePiece("PickableDandelion", GetOrAddPieceComponent(prefabRefs["Pickable_Dandelion"]), isGrounded: true)
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
                        key = "YggaShoot_small1",
                        Resources = new Dictionary<string, int>() { { "YggdrasilWood", 1 }, { "Wood", 2 } },
                        icon = true,
                        piece = CreatePiece("YggaShoot", GetOrAddPieceComponent(prefabRefs["YggaShoot_small1"]), canBeRemoved: false)
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
            }

            if (config.EnableExtraResources)
            {
                List<string> potentialNewLayers = layersForPieceRemoval.ToList();
                foreach (ExtraResource er in deserializedExtraResources)
                {
                    if (!prefabRefs.ContainsKey(er.prefabName) || !prefabRefs[er.prefabName])
                    {
                        Dbgl($"{er.prefabName} is not in dictionary of prefab references or has a null value", true, LogLevel.Warning);
                        continue;
                    }

                    if (ObjectDB.instance?.GetItemPrefab(er.resourceName)?.GetComponent<ItemDrop>() == null)
                    {
                        Dbgl($"{er.prefabName}'s required resource {er.resourceName} not found", true, LogLevel.Warning);
                        continue;
                    }

                    newList.Add(new PieceDB()
                    {
                        key = er.prefabName,
                        Resource = new KeyValuePair<string, int>(er.resourceName, er.resourceCost),
                        piece = CreatePiece(er.prefabName, GetOrAddPieceComponent(prefabRefs[er.prefabName]), canBeRemoved: true, isGrounded: er.groundOnly, extraResource: true)
                    });

                    foreach (Collider c in prefabRefs[er.prefabName].GetComponentsInChildren<Collider>())
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

        private static void InitPieces()
        {
            Dbgl("InitPieces");
            foreach (PieceDB pdb in pieceRefs)
            {
                if (config.DisabledResourceNames.Contains(pdb.key))
                {
                    Dbgl($"Resource disabled: {pdb.key}, skipping");
                    pdb.enabled = false;
                    continue;
                }
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
                if (pickable && !deserializedExtraResources.Any(x => x.prefabName == pdb.key))
                {
                    pickable.m_respawnTimeMinutes = pdb.respawnTime;
                    pickable.m_amount = pdb.resourceReturn;
                    pdb.piece.m_onlyInBiome = (Heightmap.Biome)pdb.biome;

                    if (pdb.Prefab.transform.Find("visual"))
                    {
                        if (config.ShowPickableSpawners)
                        {
                            Transform t = prefabRefs[pdb.key + "_Picked"].transform.Find("PE_Picked");
                            if (t)
                            {
                                t.SetParent(pdb.Prefab.transform);
                                t.GetComponent<MeshRenderer>().sharedMaterials = pdb.key == "Pickable_Thistle" ?
                                    pdb.Prefab.transform.Find("visual").Find("default").GetComponent<MeshRenderer>().sharedMaterials :
                                    pdb.Prefab.transform.Find("visual").GetComponent<MeshRenderer>().sharedMaterials;
                                if (pdb.key.Contains("Dandelion"))
                                {
                                    Material m = pdb.Prefab.transform.Find("visual").GetComponent<MeshRenderer>().sharedMaterials[0];
                                    t.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { m, m };
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

                pdb.piece.m_icon = pdb.icon ? CreateSprite($"{pdb.key}PieceIcon.png", new Rect(0, 0, 64, 64)) : resource.m_itemData.GetIcon();
            }
        }

        private static void InitSaplingRefs()
        {
            Dbgl("InitSaplingRefs");

            if (saplingRefs.Count > 0)
            {
                RemoveFromCultivator(saplingRefs.ConvertAll(x => (PrefabDB)x), destroy: false);
                saplingRefs.Clear();
            }
            saplingRefs = GenerateSaplingRefs();
        }

        private static List<SaplingDB> GenerateSaplingRefs()
        {
            List<SaplingDB> newList = new()
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
                    grownPrefabs = new GameObject[] { prefabRefs["YggaShoot1"], prefabRefs["YggaShoot2"], prefabRefs["YggaShoot3"] }
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
                    grownPrefabs = new GameObject[] { prefabRefs["SwampTree1"] }
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
                    grownPrefabs = new GameObject[] { prefabRefs["Birch1_aut"], prefabRefs["Birch2_aut"] }
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
                if (config.DisabledResourceNames.Contains(sdb.key))
                {
                    Dbgl($"Resource disabled: {sdb.key}");
                    sdb.enabled = false;
                }

                Plant plant = sdb.Prefab.GetComponent<Plant>();
                Piece piece = sdb.Prefab.GetComponent<Piece>();

                plant.m_growTime = plant.m_growTimeMax = sdb.growTime;
                plant.m_grownPrefabs = sdb.grownPrefabs;
                plant.m_minScale = sdb.minScale;
                plant.m_maxScale = sdb.maxScale;
                plant.m_growRadius = sdb.growRadius;

                piece.m_resources[0].m_resItem = prefabRefs[sdb.resource].GetComponent<ItemDrop>();
                piece.m_resources[0].m_amount = sdb.resourceCost;

                piece.m_onlyInBiome = plant.m_biome = (Heightmap.Biome)sdb.biome;
                plant.m_destroyIfCantGrow = piece.m_groundOnly = !config.PlaceAnywhere;

                if (isInitialized) continue;

                string[] p = { "healthy", "unhealthy" };
                Transform t = prefabRefs["Birch_Sapling"].transform.Find(p[0]);

                foreach (string parent in p)
                    sdb.Prefab.transform.Find(parent).GetComponent<MeshFilter>().mesh = t.Find("Birch_Sapling").GetComponent<MeshFilter>().mesh;

                if (sdb.source.StartsWith("Swamp"))
                {
                    Material[] m = new Material[] { prefabRefs[sdb.source].transform.Find("swamptree1").GetComponent<MeshRenderer>().sharedMaterials[0] };
                    m[0].shader = Shader.Find("Custom/Piece");

                    foreach (string parent in p)
                        sdb.Prefab.transform.Find(parent).GetComponent<MeshRenderer>().sharedMaterials = m;
                }
                else if (sdb.source.StartsWith("Ygga"))
                {
                    string[] foliage = { "birchleafs002", "birchleafs003", "birchleafs008", "birchleafs009", "birchleafs010", "birchleafs011" };
                    Material[] m = new Material[] { prefabRefs[sdb.source].transform.Find("beech").GetComponent<MeshRenderer>().sharedMaterials[0] };
                    Material[] m2 = new Material[] { prefabRefs[sdb.source].transform.Find("beech").GetComponent<MeshRenderer>().sharedMaterials[1] };

                    foreach(string parent in p)
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
                else if (sdb.source.StartsWith("Birch"))
                {
                    string[] foliage = { "birchleafs002", "birchleafs003", "birchleafs008", "birchleafs009", "birchleafs010", "birchleafs011" };
                    Material[] m = new Material[] { prefabRefs[sdb.source].transform.Find("Lod0").GetComponent<MeshRenderer>().sharedMaterials[0] };
                    Material[] m2 = new Material[] { t.Find("Birch_Sapling").GetComponent<MeshRenderer>().sharedMaterials[0] };

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
                
                piece.m_icon = sdb.icon ? CreateSprite($"{sdb.key}PieceIcon.png", new Rect(0, 0, 64, 64)) : piece.m_resources[0].m_resItem.m_itemData.GetIcon();

                piece.m_placeEffect.m_effectPrefabs[0].m_prefab = prefabRefs["vfx_Place_wood_pole"];
                piece.m_placeEffect.m_effectPrefabs[1].m_prefab = prefabRefs["sfx_build_cultivator"];
                sdb.Prefab.GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[0].m_prefab = prefabRefs["Birch_Sapling"].GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[0].m_prefab;
                sdb.Prefab.GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[1].m_prefab = prefabRefs["Birch_Sapling"].GetComponent<Destructible>().m_hitEffect.m_effectPrefabs[1].m_prefab;
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
                },
                new PrefabDB
                {
                    key = "sapling_magecap",
                    resourceCost = enableCropOverrides ? config.MagecapCost : 1,
                    resourceReturn = enableCropOverrides ? config.MagecapReturn : 1,
                    extraDrops = true
                },
                new PrefabDB
                {
                    key = "sapling_jotunpuffs",
                    resourceCost = enableCropOverrides ? config.JotunPuffsCost : 1,
                    resourceReturn = enableCropOverrides ? config.JotunPuffsReturn : 1,
                    extraDrops = true
                }
            };

            foreach (PrefabDB pdb in crops)
            {
                Piece piece = pdb.Prefab.GetComponent<Piece>();
                Plant plant = pdb.Prefab.GetComponent<Plant>();
                Pickable pickable = plant.m_grownPrefabs[0].GetComponent<Pickable>();

                piece.m_resources[0].m_amount = pdb.resourceCost;
                piece.m_primaryTarget = piece.m_randomTarget = config.EnemiesTargetCrops;

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

                //For jotun puffs and magecap
                pickable.m_extraDrops.m_drops.Clear();
                if (pdb.extraDrops & !enableCropOverrides)
                {
                    pickable.m_extraDrops.m_drops.Add(new DropTable.DropData{m_item = pickable.m_itemPrefab, m_stackMin = 1, m_stackMax = 1, m_weight = 0});
                }
            }
        }

        private static void InitCultivator()
        {
            Dbgl("InitCultivator");

            ItemDrop cultivator = prefabRefs["Cultivator"].GetComponent<ItemDrop>();

            for (int i = 0; i < saplingRefs.Count; i++)
            {
                if (!saplingRefs[i].enabled)
                    continue;
                if (!cultivator.m_itemData.m_shared.m_buildPieces.m_pieces.Contains(saplingRefs[i].Prefab))
                    cultivator.m_itemData.m_shared.m_buildPieces.m_pieces.Insert(16, saplingRefs[i].Prefab);
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

        private static void RemoveFromCultivator(List<PrefabDB> prefabs, bool destroy)
        {
            if (Player.m_localPlayer?.GetRightItem()?.m_shared.m_name == "$item_cultivator")
            {
                PELogger.LogWarning("Cultivator updated through config change, unequipping cultivator");
                Player.m_localPlayer.HideHandItems();
            }

            foreach (PrefabDB pdb in prefabs)
            {
                if (prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Contains(pdb.Prefab))
                    prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Remove(pdb.Prefab);
                if (destroy)
                    DestroyImmediate(pdb.Prefab.GetComponent<Piece>());
            }
        }

        private static void FinalInit(ZNetScene instance)
        {
            InitExtraResources(instance);
            InitPieceRefs();
            InitPieces();
            InitSaplingRefs();
            InitSaplings();
            InitCrops();
            InitCultivator();

            if (stringDictionary.Count > 0)
                InitLocalization();

            List<GameObject> customPrefabs = new() { prefabRefs["Ancient_Sapling"], prefabRefs["Ygga_Sapling"], prefabRefs["Autumn_Birch_Sapling"] };
            foreach (GameObject go in customPrefabs)
            {
                if (!instance.m_prefabs.Contains(go))
                {
                    instance.m_prefabs.Add(go);
                    instance.m_namedPrefabs.Add(instance.GetPrefabHash(go), go);
                }
            }
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
                itemDrop.m_weight = 1;
                target.m_dropWhenDestroyed.m_dropMin = config.TreeDropMin;
                target.m_dropWhenDestroyed.m_dropMax = config.TreeDropMax;
                target.m_dropWhenDestroyed.m_drops.Add(itemDrop);
                target.m_dropWhenDestroyed.m_dropChance = Mathf.Clamp(config.DropChance, 0f, 1f);
                target.m_dropWhenDestroyed.m_oneOfEach = config.OneOfEach;
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

        public static void InitLocalization()
        {
            Dbgl("InitLocalization");
            foreach (KeyValuePair<string, string> kvp in stringDictionary)
            {
                Localization.instance.AddWord($"pe{kvp.Key}", kvp.Value);
            }
            stringDictionary.Clear();
        }

        internal class LocalizedStrings
        {
            public List<string> localizedStrings = new();
        }

        public struct ExtraResource
        {
            public string prefabName;
            public string resourceName;
            public int resourceCost;
            public bool groundOnly;

            public ExtraResource(string prefabName, string resourceName, int resourceCost = 1, bool groundOnly = true)
            {
                this.prefabName = prefabName;
                this.resourceName = resourceName;
                this.resourceCost = resourceCost;
                this.groundOnly = groundOnly;
            }

            internal bool IsValid() => !(prefabName == default || prefabName.StartsWith("PE_Fake") || resourceName == default || resourceCost == default);
        }

        internal class PrefabDB
        {
            internal string key;
            internal int biome;
            internal int resourceCost;
            internal int resourceReturn;
            internal bool extraDrops;
            internal bool icon;
            internal bool enabled = true;

            internal GameObject Prefab
            {
                get { return prefabRefs[key]; }
            }
        }

        private class PieceDB : PrefabDB
        {
            private Dictionary<string, int> resources;
            internal int respawnTime;
            internal bool recover;
            internal Piece piece;
            internal List<Vector3> points;

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
    }
}
