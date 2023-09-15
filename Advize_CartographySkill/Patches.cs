using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Advize_CartographySkill
{
    public partial class CartographySkill
    {
        #region Exploration Related Patches
        [HarmonyPatch(typeof(Minimap), "Awake")]
        public static class MinimapAwake
        {
            public static void Postfix(Minimap __instance)
            {
                if (!config.EnableSkill) return;
                __instance.m_exploreRadius = config.BaseExploreRadius;
                Dbgl($"Explore Radius is now: {config.BaseExploreRadius}");
            }
        }

        [HarmonyPatch(typeof(Minimap), "Explore", new Type[] { typeof(int), typeof(int) })]
        public static class MinimapExplore
        {
            public static void Prefix(Minimap __instance)
            {
                if (!Player.m_localPlayer || !config.EnableSkill) return;

                float skillLevel = Player.m_localPlayer.GetSkillFactor((Skills.SkillType)SKILL_TYPE) * 100;
                float newExploreRadius = config.BaseExploreRadius + (config.ExploreRadiusIncrease * skillLevel);

                if (__instance.m_exploreRadius != newExploreRadius)
                {
                    Dbgl($"Previous explore radius was: {__instance.m_exploreRadius} new radius is: {newExploreRadius}");
                    __instance.m_exploreRadius = newExploreRadius;
                }
            }

            public static void Postfix(ref bool __result)
            {
                if (!Player.m_localPlayer || !config.EnableSkill) return;

                //if Explore(int,int) returns true, it means we have discovered more of the world map
                if (__result)
                {
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
        #endregion
        #region Spyglass Related Patches
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDBAwake
        {
            public static void Prefix(ObjectDB __instance)
            {
                //Dbgl("ObjectDB.Awake() Prefix");
                PrefabInit();
                if (!config.EnableSpyglass || !ZNetScene.instance) return;
                
                if (!__instance.m_items.Contains(prefab)) __instance.m_items.Add(prefab);
            }
            public static void Postfix(ObjectDB __instance)
            {
                //Dbgl("ObjectDB.Awake() Postfix");
                if (!config.EnableSpyglass || !ZNetScene.instance) return;

                if (!__instance.m_recipes.Contains(recipe)) AddRecipe(__instance);
            }
        }

        //Add items and recipes when save games are deserialized and loaded at start scene
        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class ObjectDBCopyOtherDB
        {
            public static void Postfix(ObjectDB __instance)
            {
                //Dbgl("ObjectDB.CopyOtherDB() Postfix");
                if (!config.EnableSpyglass) return;

                if (!__instance.m_items.Contains(prefab))
                {
                    __instance.m_items.Add(prefab);
                    __instance.UpdateItemHashes();
                }
                if (!__instance.m_recipes.Contains(recipe)) AddRecipe(__instance);
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetSceneAwake
        {
            public static void Postfix(ZNetScene __instance)
            {
                //Dbgl("ZNetScene.Awake() Postfix");
                if (stringDictionary.Count > 0)
                    InitLocalization();
                if (!config.EnableSpyglass) return;

                if (!__instance.m_prefabs.Contains(prefab))
                {
                    __instance.m_prefabs.Add(prefab);
                    __instance.m_namedPrefabs.Add(__instance.GetPrefabHash(prefab), prefab);
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        public static class PlayerUpdate
        {
            public static void Postfix()
            {
                if (!config.EnableSpyglass || !GameCamera.m_instance) return;

                if (isZooming && (!IsSpyglassEquipped() || ZInput.GetButtonDown("Attack") || config.RemoveZoomKey.IsDown()))
                {
                    StopZoom();
                }

                if (InventoryGui.IsVisible()) return;

                if(IsSpyglassEquipped())
                {
                    if (config.IncreaseZoomKey.IsDown())
                    {
                        ChangeZoom(1);
                    }
                    if (config.DecreaseZoomModifierKey.IsDown())
                    {
                        ChangeZoom(-1);
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
                    scopeLevel += Vector3.forward * zoomLevel * config.ZoomMultiplier;
                    Vector3 difference = __instance.transform.TransformVector(scopeLevel);

                    //Try to prevent zooming through things? Needs lots of work still
                    if (!Physics.Raycast(__instance.transform.position, __instance.transform.forward, out var hitInfo, difference.magnitude, __instance.m_blockCameraMask))
                    {
                        __instance.transform.position += difference;
                    }
                    else
                    {
                        scopeLevel = Vector3.zero;
                        scopeLevel += Vector3.forward * Vector3.Distance(hitInfo.point, __instance.transform.position);
                        difference = __instance.transform.TransformVector(scopeLevel);

                        __instance.transform.position += difference;
                    }

                    //Keep camera above the ground hopefully
                    if (ZoneSystem.instance.GetGroundHeight(__instance.transform.position, out float num) && __instance.transform.position.y < num +1f)
                    {
                        Vector3 position = __instance.transform.position;
                        position.y = num + 1f;
                        __instance.transform.position = position;
                    }
                }
            }
        }
        #endregion
        #region Skill Related Patches
        [HarmonyPatch(typeof(Skills), "GetSkillDef")]
        public static class SkillsGetSkillDef
        {
            public static void Postfix(Skills.SkillType type, ref Skills.SkillDef __result, List<Skills.SkillDef> ___m_skills)
            {
                if (!config.EnableSkill) return;
                if (__result == null)
                {
                    if ((int)type == SKILL_TYPE)
                    {
                        ___m_skills.Add(cartographySkill.skillDef);
                        __result = cartographySkill.skillDef;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Skills), "IsSkillValid")]
        public static class SkillsIsSkillValid
        {
            public static void Postfix(Skills.SkillType type, ref bool __result)
            {
                if (!config.EnableSkill) return;
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
                string localizedSkillName = Localization.instance.Localize(cartographySkill.name);
                if (localizedSkillName.ToLower() == name.ToLower())
                {
                    Skills.Skill skill = __instance.GetSkill((Skills.SkillType)SKILL_TYPE);
                    skill.m_level += value;
                    skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
                    ___m_player.Message(MessageHud.MessageType.TopLeft, "Skill increased " + localizedSkillName + ": " + (int)skill.m_level, 0, skill.m_info.m_icon);
                    Console.instance.Print("Skill " + localizedSkillName + " = " + skill.m_level);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Skills), "CheatResetSkill")]
        public static class SkillsCheatResetSkill
        {
            public static bool Prefix(Skills __instance, string name)
            {
                string localizedSkillName = Localization.instance.Localize(cartographySkill.name);
                if (localizedSkillName.ToLower() == name.ToLower())
                {
                    __instance.ResetSkill((Skills.SkillType)SKILL_TYPE);
                    Console.instance.Print("Skill " + localizedSkillName + " reset");
                    return false;
                }
                return true;
            }
        }
        #endregion
    }
}
