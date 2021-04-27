using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Pipakin.SkillInjectorMod;
using UnityEngine;
using Advize_CartographySkill.Configuration;
using UnityEngine.Events;

namespace Advize_CartographySkill
{
    [BepInPlugin(PluginID, PluginName, Version)]
    [BepInDependency("com.pipakin.SkillInjectorMod")]
    public class CartographySkill : BaseUnityPlugin
    {
        public const string PluginID = "advize.CartographySkill";
        public const string PluginName = "CartographySkill";
        public const string Version = "1.5.0";
        public const int SKILL_TYPE = 1337;

        private readonly Harmony harmony = new Harmony(PluginID);

        private static GameObject prefab;
        private static Recipe recipe;

        private static int zoomLevel = 1;
        private static float startingFov;
        private static float currentFov;
        private static bool isZooming;

        private static int tileCount = 0;
        private static bool[] explored;

        private static readonly AssetBundle assetBundle = LoadAssetBundle("spyglass");
        private static Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();

        private new Config Config
        {
            get { return Config.Instance; }
        }
        private static ModConfig config;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called Implicitly")]
        private void Awake()
        {
            Config.Init(this, true);
            config = new ModConfig(Config);
            Config.OnConfigReceived.AddListener(new UnityAction(ConfigReceived));
            SkillInjector.RegisterNewSkill(SKILL_TYPE, "Cartography", "Increases map explore radius.", 1.0f, CreateSprite("cartographyicon.png", new Rect(0, 0, 32, 32)));
            // Needed in case Skill Injector is ever abandoned
            /*customSkill = new CustomSkill();*/
            harmony.PatchAll();
        }

        private void ConfigReceived()
        {
            Minimap.instance.m_exploreRadius = config.BaseExploreRadius;

            Dbgl($"Explore Radius is now: {config.BaseExploreRadius}");
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
                Texture2D texture = new Texture2D(0, 0);
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
            //string name = "Spyglass";
            //string desc = "See further into the distance, or bash your enemies with it.";

            //Traverse.Create(Localization.instance).Method("AddWord", "csSpyglassName", name).GetValue("csSpyglassName", name);
            //Traverse.Create(Localization.instance).Method("AddWord", "csSpyglassDescription", desc).GetValue("csSpyglassDescription", desc);

            //Assign the item texture via code because I can't figure out Sprites in Unity
            item.m_itemData.m_shared.m_icons[0] = CreateSprite("spyglassicon.png", new Rect(0, 0, 64, 64));

            //Fixup some stuff I didn't want to replicate in the assetbundle, could probably do more through code and save on resources
            item.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[0] = itemClub.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[0];
            item.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[1] = itemClub.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[1];
            item.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[2] = itemClub.m_itemData.m_shared.m_hitEffect.m_effectPrefabs[2];

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

            List<Piece.Requirement> requirements = new List<Piece.Requirement>
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
            if (!prefab) prefab = CreatePrefab("advize_item_spyglass");
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
                Dbgl("ChangeZoom() starting fov was " + startingFov);
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
                    Dbgl("Spyglass zoomed out, current fov should be " + currentFov);
                    break;
                case 1:
                    currentFov = Mathf.Max(currentFov - (config.FovReductionFactor + (zoomLevel * config.FovReductionFactor)), 5);
                    Dbgl("Spyglass zoomed in, current fov should be " + currentFov);
                    break;
            }
        }

        private static void StopZoom()
        {
            zoomLevel = 1;
            GameCamera.instance.m_fov = currentFov = startingFov;
            isZooming = false;
            Dbgl("StopZoom() fov is now " + GameCamera.instance.m_fov);
        }

        private static bool IsSpyglassEquipped()
        {
            return Player.m_localPlayer.GetRightItem() != null && Player.m_localPlayer.GetRightItem().m_shared.m_name == "Spyglass";
        }

        private static void SyncDiscovered(Console instance)
        {
            instance.Print("CartographySkill:\nBeginning map tile discovery sync, this may cause momentary stutter");

            var _m_explored = typeof(Minimap).GetField("m_explored", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Minimap.instance);

            if (_m_explored == null)
            {
                string str = "_m_explored is null, could not sync map discovery";
                instance.Print(str);
                Dbgl(str, true, true);
                return;
            }

            bool[] m_explored = _m_explored as bool[];

            int legitDiscovered = explored.Count(c => c);
            int totalDiscovered = m_explored.Count(c => c);

            Dbgl("legitDiscovered = " + legitDiscovered + " totalDiscovered = " + totalDiscovered);

            int uncountedTiles = totalDiscovered - legitDiscovered;

            if (uncountedTiles == 0)
            {
                instance.Print("Map tile discovery was already up to date");
                return;
            }
            
            instance.Print("Found " + uncountedTiles + " non-officially discovered tiles");

            //Update our array
            Array.Copy(m_explored, explored, m_explored.Length);

            tileCount += uncountedTiles;

            instance.Print("Cartography Synchronization Completed Successfully\nXP will be awarded upon discovering a new map tile");
        }

        internal static void Dbgl(string message, bool forceLog = false, bool logError = false)
        {
            if (forceLog || config.EnableDebugMessages)
            {
                string str = PluginName + ": " + message;
                
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

        [HarmonyPatch(typeof(Minimap), "Awake")]
        public static class MinimapAwake
        {
            public static void Postfix(Minimap __instance)
            {
                __instance.m_exploreRadius = config.BaseExploreRadius;

                Dbgl($"Explore Radius is now: {config.BaseExploreRadius}");
            }
        }

        [HarmonyPatch(typeof(Minimap), "Start")]
        public static class MinimapStart
        {
            public static void Postfix(ref bool[] ___m_explored)
            {
                explored = new bool[___m_explored.Length];
            }
        }

        [HarmonyPatch(typeof(Minimap), "SetMapData")]
        public static class MinimapSetMapData
        {
            public static void Postfix(ref bool[] ___m_explored)
            {
                // Map data has loaded, copy array
                explored = new bool[___m_explored.Length];
                Array.Copy(___m_explored, explored, ___m_explored.Length);
            }
        }

        [HarmonyPatch(typeof(Minimap), "Explore", new Type[] { typeof(int), typeof(int) })]
        public static class MinimapExplore
        {
            public static void Prefix(Minimap __instance)
            {
                if (Player.m_localPlayer == null) return;

                float skillLevel = Player.m_localPlayer.GetSkillFactor((Skills.SkillType)SKILL_TYPE) * 100;
                float newExploreRadius = config.BaseExploreRadius + (config.ExploreRadiusIncrease * skillLevel);

                if (__instance.m_exploreRadius != newExploreRadius)
                {
                    Dbgl("Previous explore radius was: " + __instance.m_exploreRadius + " new radius is: " + newExploreRadius);
                    __instance.m_exploreRadius = newExploreRadius;
                }
            }

            public static void Postfix(int x, int y, ref bool __result, Minimap __instance)
            {
                if (Player.m_localPlayer == null) return;

                //if Explore(int,int) returns true, it means we have discovered more of the world map
                if (__result)
                {
                    //Ensure we update our array as this is legitimate tile discovery
                    explored[y * __instance.m_textureSize + x] = true;
                    tileCount++;
                    if (tileCount >= config.TilesDiscoveredForXPGain)
                    {
                        int num1 = tileCount / config.TilesDiscoveredForXPGain; // gets whole numbers
                        int num2 = tileCount % config.TilesDiscoveredForXPGain; // gets remainder

                        for (int i = 0; i < num1; i++)
                        {
                            Player.m_localPlayer.RaiseSkill((Skills.SkillType)SKILL_TYPE, config.SkillIncrease);
                        }

                        tileCount = num2;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Console), "InputText")]
        public static class ConsoleInputText
        {
            public static bool Prefix(Console __instance)
            {
                if (!Player.m_localPlayer || !Minimap.instance)
                {
                    return true;
                }

                string text = __instance.m_input.text.ToLower();
                if (text.Equals("cartxpsync"))
                {
                    SyncDiscovered(__instance);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDBAwake
        {
            public static void Postfix(ObjectDB __instance)
            {
                PrefabInit();

                if (ZNetScene.instance == null) return;

                if (!config.EnableSpyglass) return;

                ItemRecipeInit(__instance);
            }
        }

        //Add items and recipes when save games are deserialized and loaded at start scene
        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class ObjectDBCopyOtherDB
        {
            public static void Postfix(ObjectDB __instance)
            {
                PrefabInit();
                ItemRecipeInit(__instance);
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetSceneAwake
        {
            public static void Postfix(ZNetScene __instance)
            {
                if (!config.EnableSpyglass) return;

                PrefabInit();

                if (__instance.m_prefabs.Contains(prefab)) return;

                __instance.m_prefabs.Add(prefab);
            }
        }

        //Hack to render item when held or sheathed, should probably just look into updating the hash dictionary
        [HarmonyPatch(typeof(ObjectDB), "GetItemPrefab", new Type[] { typeof(int) })]
        public static class ObjectDBGetItemPrefab
        {
            public static void Postfix(int hash, ref GameObject __result)
            {
                if (__result == null)
                {
                    int spyglassHash = ObjectDB.instance.GetPrefabHash(prefab);
                    if (hash == spyglassHash)
                    {
                        __result = prefab;
                    }
                }
            }
        }

        //Hack to allow console to spawn the item, should probably just look into updating the hash dictionary
        [HarmonyPatch(typeof(ZNetScene), "GetPrefab", new Type[] { typeof(int) })]
        public static class ZNetSceneGetPrefab
        {
            public static void Postfix(int hash, ref GameObject __result)
            {
                if (!config.EnableSpyglass) return;
                if (__result == null)
                {
                    int spyglassHash = ZNetScene.instance.GetPrefabHash(prefab);
                    if (hash == spyglassHash)
                    {
                        __result = prefab;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        public static class PlayerUpdate
        {
            public static void Postfix()
            {
                if (!GameCamera.instance || !config.EnableSpyglass) return;

                if (isZooming && (!IsSpyglassEquipped() || ZInput.GetButtonDown("Attack") || (config.RemoveZoomKey != "" && Input.GetKeyDown(config.RemoveZoomKey))))
                {
                    StopZoom();
                }

                if (InventoryGui.IsVisible()) return;

                if (Input.GetKeyDown(config.IncreaseZoomKey) && IsSpyglassEquipped())
                {
                    if (Input.GetKey(config.DecreaseZoomModifierKey))
                    {
                        ChangeZoom(-1);
                    }
                    else
                    {
                        ChangeZoom(1);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
        public static class GameCameraUpdateCamera
        {
            [HarmonyPriority(Priority.Last)]
            public static void Prefix(GameCamera __instance)
            {
                if (isZooming) __instance.m_fov = currentFov;
            }

            public static void Postfix(ref GameCamera __instance)
            {
                if (isZooming)
                {
                    Vector3 scopeLevel = Vector3.zero;
                    scopeLevel += (Vector3.forward * zoomLevel * config.ZoomMultiplier);

                    Vector3 difference = __instance.transform.TransformVector(scopeLevel);
                    __instance.transform.position += difference;

                    //Keep camera above the ground hopefully
                    float num;
                    if (ZoneSystem.instance.GetGroundHeight(__instance.transform.position, out num) && __instance.transform.position.y < num)
                    {
                        Vector3 position = __instance.transform.position;
                        position.y = num;
                        __instance.transform.position = position;
                    }
                }
            }
        }
        
        // Needed in case Skill Injector is ever abandoned
        /*private static CustomSkill customSkill;

        private class CustomSkill
        {
            public string name = "Cartography";
            public Skills.SkillDef skillDef = new Skills.SkillDef
            {
                m_description = "Increases map explore radius.",
                m_icon = CreateSprite(Path.Combine(modDirectory, "cartographyicon.png"), new Rect(0, 0, 32, 32)),
                m_increseStep = 1.0f,
                m_skill = (Skills.SkillType)SKILL_TYPE
            };
        }

        [HarmonyPatch(typeof(Skills), "GetSkillDef")]
        public static class SkillsGetSkillDef
        {
            public static void Postfix(Skills.SkillType type, ref Skills.SkillDef __result, List<Skills.SkillDef> ___m_skills)
            {
                if (__result == null)
                {
                    if ((int)type == SKILL_TYPE)
                    {
                        Traverse.Create(Localization.instance).Method("AddWord", "skill_" + SKILL_TYPE, customSkill.name).GetValue("skill_" + SKILL_TYPE, customSkill.name);
                        ___m_skills.Add(customSkill.skillDef);
                        __result = customSkill.skillDef;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Skills), "IsSkillValid")]
        public static class SkillsIsSkillValid
        {
            public static void Postfix(Skills.SkillType type, ref bool __result)
            {
                if (!__result)
                {
                    __result = (int)type == SKILL_TYPE;
                }
            }
        }

        [HarmonyPatch(typeof(Skills), "CheatRaiseSkill")]
        public static class SkillsCheatRaiseSkill
        {
            public static bool Prefix(string name, float value, Skills __instance, Player ___m_player)
            {
                if (customSkill.name.ToLower() == name)
                {
                    Skills.Skill skill = Traverse.Create(__instance).Method("GetSkill", (Skills.SkillType)SKILL_TYPE).GetValue<Skills.Skill>((Skills.SkillType)SKILL_TYPE);
                    skill.m_level += value;
                    skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
                    ___m_player.Message(MessageHud.MessageType.TopLeft, "Skill increased " + customSkill.name + ": " + (int)skill.m_level, 0, skill.m_info.m_icon);
                    Console.instance.Print("Skill " + customSkill.name + " = " + skill.m_level);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Skills), "CheatResetSkill")]
        public static class SkillsCheatResetSkill
        {
            public static bool Prefix(string name, Player ___m_player)
            {
                if (customSkill.name.ToLower() == name)
                {
                    ___m_player.GetSkills().ResetSkill((Skills.SkillType)SKILL_TYPE);
                    return false;
                }
                return true;
            }
        }*/
 
    }
}