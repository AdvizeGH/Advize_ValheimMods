using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Advize_CartographySkill
{
    public partial class CartographySkill
    {
        [HarmonyPatch(typeof(Minimap), "Awake")]
        public static class MinimapAwake
        {
            public static void Postfix(Minimap __instance)
            {
                __instance.m_exploreRadius = config.BaseExploreRadius;
                Dbgl($"Explore Radius is now: {__instance.m_exploreRadius}");
            }
        }

        [HarmonyPatch(typeof(Minimap), "Explore", new Type[] { typeof(int), typeof(int) })]
        public static class MinimapExplore
        {
            private static int tileCount = 0;

            public static void Prefix(Minimap __instance)
            {
                if (!Player.m_localPlayer || !config.EnableSkill) return;

                float skillLevel = Player.m_localPlayer.GetSkillFactor((Skills.SkillType)SKILL_TYPE) * 100;
                float newExploreRadius = config.BaseExploreRadius + (config.ExploreRadiusIncrease * skillLevel);

                Dbgl($"Previous explore radius was: {__instance.m_exploreRadius} new radius is: {newExploreRadius}");
                __instance.m_exploreRadius = newExploreRadius;
            }

            public static void Postfix(ref bool __result)
            {
                if (!Player.m_localPlayer || !config.EnableSkill || !__result) return;
                //if Explore(int,int) (__result) returns true, it means we have discovered more of the world map
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

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetSceneAwake
        {
            public static void Postfix()
            {
                //Dbgl("ZNetScene.Awake() Postfix");
                if (stringDictionary.Count > 0)
                    InitLocalization();
            }
        }

        [HarmonyPatch(typeof(Skills), "GetSkillDef")]
        public static class SkillsGetSkillDef
        {
            public static void Postfix(Skills.SkillType type, ref Skills.SkillDef __result, List<Skills.SkillDef> ___m_skills)
            {
                if (!config.EnableSkill || __result != null || (int)type != SKILL_TYPE) return;
                
                ___m_skills.Add(cartographySkill.skillDef);
                __result = cartographySkill.skillDef;
            }
        }

        [HarmonyPatch(typeof(Skills), "IsSkillValid")]
        public static class SkillsIsSkillValid
        {
            public static void Postfix(Skills.SkillType type, ref bool __result)
            {
                if (!config.EnableSkill || __result) return;
                
                __result = (int)type == SKILL_TYPE;
            }
        }

        [HarmonyPatch(typeof(Skills), "CheatRaiseSkill")]
        public static class SkillsCheatRaiseSkill
        {
            public static bool Prefix(Skills __instance, string name, float value, Player ___m_player)
            {
                if (!config.EnableSkill) return true;
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
                if (!config.EnableSkill) return true;
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
    }
}
