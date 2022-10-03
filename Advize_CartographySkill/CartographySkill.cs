using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Advize_CartographySkill.Configuration;

namespace Advize_CartographySkill
{
    [BepInPlugin(PluginID, PluginName, Version)]
    public partial class CartographySkill : BaseUnityPlugin
    {
        public const string PluginID = "advize.CartographySkill";
        public const string PluginName = "CartographySkill";
        public const string Version = "2.1.1";
        public const int SKILL_TYPE = 1337;

        private readonly Harmony harmony = new(PluginID);
        public static ManualLogSource CSLogger = new($" {PluginName}");

        private static GameObject prefab;
        private static Recipe recipe;
        private static CustomSkill customSkill;

        private static int zoomLevel = 1;
        private static float startingFov;
        private static float currentFov;
        private static bool isZooming;

        private static int tileCount = 0;

        private static readonly string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly AssetBundle assetBundle = LoadAssetBundle("spyglass");
        private static readonly Dictionary<string, Texture2D> cachedTextures = new();

        private static ModConfig config;

        private static readonly Dictionary<string, string> stringDictionary = new()
        {
            { "SpyglassName", "Spyglass" },
            { "SpyglassDescription", "See further into the distance, or bash your enemies with it." }
        };

        private void Awake()
        {
            BepInEx.Logging.Logger.Sources.Add(CSLogger);
            config = new ModConfig(Config, new ServerSync.ConfigSync(PluginID) { DisplayName = PluginName, CurrentVersion = Version, MinimumRequiredVersion = "2.1.1" });
            LoadLocalizedStrings();
            customSkill = new CustomSkill();
            harmony.PatchAll();
        }

        private void LoadLocalizedStrings()
        {
            string fileName = $"Advize_{PluginName}.json";
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
                Dbgl("Unable to load localized text file, generating new one from default English values", true);
            }
            SerializeDict();
        }

        private void SerializeDict()
        {
            string filePath = Path.Combine(modDirectory, $"Advize_{PluginName}.json");

            LocalizedStrings localizedStrings = new LocalizedStrings();
            foreach (KeyValuePair<string, string> kvp in stringDictionary)
            {
                localizedStrings.localizedStrings.Add($"{kvp.Key}:{kvp.Value}");
            }

            File.WriteAllText(filePath, JsonUtility.ToJson(localizedStrings, true));

            Dbgl($"Saved english localized strings to {filePath}");
        }

        public static void InitLocalization()
        {
            Dbgl("InitLocalization");
            foreach (KeyValuePair<string, string> kvp in stringDictionary)
            {
                Traverse.Create(Localization.instance).Method("AddWord", $"cs{kvp.Key}", kvp.Value).GetValue($"cs{kvp.Key}", kvp.Value);
            }
            stringDictionary.Clear();
        }

        private static AssetBundle LoadAssetBundle(string fileName)
        {
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Advize_{PluginName}.Assets.{fileName}");
            return AssetBundle.LoadFromStream(manifestResourceStream);
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

        private static GameObject CreatePrefab(string name)
        {
                GameObject loadedPrefab = assetBundle.LoadAsset<GameObject>(name);
                loadedPrefab.SetActive(true);

                return loadedPrefab;
        }

        private static void AddItem()
        {
            ItemDrop item = prefab.GetComponent<ItemDrop>();
            ItemDrop itemClub = ObjectDB.instance.GetItemPrefab("Club").GetComponent<ItemDrop>();

            //Assign the item texture via code because I can't figure out Sprites in Unity
            item.m_itemData.m_shared.m_icons[0] = CreateSprite("spyglassicon.png", new Rect(0, 0, 64, 64));

            //Fixup some stuff I didn't want to replicate in the assetbundle, could probably do more through code and save on resources
            item.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[0] = itemClub.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[0];
            item.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[1] = itemClub.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[1];
            item.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[2] = itemClub.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[2];

            item.m_itemData.m_shared.m_blockEffect.m_effectPrefabs[0] = itemClub.m_itemData.m_shared.m_blockEffect.m_effectPrefabs[0];
            item.m_itemData.m_shared.m_blockEffect.m_effectPrefabs[1] = itemClub.m_itemData.m_shared.m_blockEffect.m_effectPrefabs[1];
            item.m_itemData.m_shared.m_blockEffect.m_effectPrefabs[2] = itemClub.m_itemData.m_shared.m_blockEffect.m_effectPrefabs[2];

            item.m_itemData.m_shared.m_triggerEffect.m_effectPrefabs[0] = itemClub.m_itemData.m_shared.m_triggerEffect.m_effectPrefabs[0];
            item.m_itemData.m_shared.m_trailStartEffect.m_effectPrefabs[0] = itemClub.m_itemData.m_shared.m_trailStartEffect.m_effectPrefabs[0];

            //Add the prefab to the list of items
            ObjectDB.instance.m_items.Add(prefab);
        }

        private static void AddRecipe()
        {
            recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = "recipe_spyglass";
            recipe.m_amount = 1;
            recipe.m_minStationLevel = 1;
            recipe.m_item = prefab.GetComponent<ItemDrop>();
            recipe.m_enabled = true;

            //Surely there is a more efficient way of doing this
            foreach (Recipe rec in ObjectDB.instance.m_recipes)
            {
                if (rec.m_craftingStation != null && rec.m_craftingStation.m_name == "$piece_workbench")
                {
                    recipe.m_craftingStation = rec.m_craftingStation;
                    break;
                }
            }

            List<Piece.Requirement> requirements = new()
            {
                new Piece.Requirement
                {
                    m_amount = 1,
                    m_resItem = ObjectDB.instance.GetItemPrefab("Crystal").GetComponent<ItemDrop>(),
                    m_recover = true
                },
                new Piece.Requirement
                {
                    m_amount = 2,
                    m_resItem = ObjectDB.instance.GetItemPrefab("Obsidian").GetComponent<ItemDrop>(),
                    m_recover = true
                },
                new Piece.Requirement
                {
                    m_amount = 2,
                    m_resItem = ObjectDB.instance.GetItemPrefab("Bronze").GetComponent<ItemDrop>(),
                    m_recover = true
                }
            };
            recipe.m_resources = requirements.ToArray();

            ObjectDB.instance.m_recipes.Add(recipe);
        }

        private static void PrefabInit()
        {
            if (!prefab) prefab = CreatePrefab("AdvizeSpyglass");
        }

        private static void ItemRecipeInit(ObjectDB instance)
        {
            if (!instance.m_items.Contains(prefab)) AddItem();
            if (!instance.m_recipes.Contains(recipe)) AddRecipe();
        }

        private static void ChangeZoom(int delta)
        {
            if (zoomLevel == 1)
            {
                startingFov = currentFov = GameCamera.instance.m_fov;
                Dbgl($"ChangeZoom() starting fov was {startingFov}");
            }

            zoomLevel += delta;
            isZooming = true;

            switch (zoomLevel)
            {
                case 2:
                case 3:
                case 4:
                    break;
                default:
                    StopZoom();
                    return;
            }

            switch (delta)
            {
                case -1:
                    currentFov = Mathf.Max(currentFov + (config.FovReductionFactor + ((zoomLevel + 1) * config.FovReductionFactor)), 5);
                    Dbgl($"Spyglass zoomed out, current fov should be {currentFov}");
                    break;
                case 1:
                    currentFov = Mathf.Max(currentFov - (config.FovReductionFactor + (zoomLevel * config.FovReductionFactor)), 5);
                    Dbgl($"Spyglass zoomed in, current fov should be {currentFov}");
                    break;
            }
        }

        private static void StopZoom()
        {
            zoomLevel = 1;
            GameCamera.instance.m_fov = currentFov = startingFov;
            isZooming = false;
            Dbgl($"StopZoom() fov is now {GameCamera.instance.m_fov}");
        }

        private static bool IsSpyglassEquipped()
        {
            return Player.m_localPlayer.GetRightItem() != null && Player.m_localPlayer.GetRightItem().m_shared.m_name == "$csSpyglassName";
        }

        internal static void Dbgl(string message, bool forceLog = false, bool logError = false)
        {
            if (forceLog || config.EnableDebugMessages)
            {                
                if (logError)
                {
                    CSLogger.LogError(message);
                }
                else
                {
                    CSLogger.LogInfo(message);
                }
            }
        }

        private class CustomSkill
        {
            public string name = "Cartography";
            public Skills.SkillDef skillDef = new()
            {
                m_description = "Increases map explore radius.",
                m_icon = CreateSprite("cartographyicon.png", new Rect(0, 0, 32, 32)),
                m_increseStep = 1.0f,
                m_skill = (Skills.SkillType)SKILL_TYPE
            };
        }

        internal class LocalizedStrings
        {
            public List<string> localizedStrings = new();
        }
    }
}
