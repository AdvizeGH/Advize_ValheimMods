namespace Advize_CartographySkill;

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

public partial class CartographySkill
{
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
    static class MinimapAwake
    {
        static void Postfix(Minimap __instance)
        {
            __instance.m_exploreRadius = config.BaseExploreRadius;
            Dbgl($"Explore Radius is now: {__instance.m_exploreRadius}");
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore), [typeof(int), typeof(int)])]
    static class MinimapExplore
    {
        private static int tileCount;

        static void Postfix(ref bool __result)
        {
            //if Explore(int,int) (__result) returns true, it means we have discovered more of the world map
            if (!config.EnableSkill || !__result) return;

            tileCount++;
            if (tileCount < config.TilesDiscoveredForXPGain) return;

            int xpGainCount = tileCount / config.TilesDiscoveredForXPGain;

            for (int i = 0; i < xpGainCount; i++)
                Player.m_localPlayer?.RaiseSkill((Skills.SkillType)SKILL_TYPE, config.SkillIncrease);

            tileCount %= config.TilesDiscoveredForXPGain;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
    static class PlayerOnSkillLevelup
    {
        static void Postfix(Skills.SkillType skill, float level)
        {
            if ((int)skill != SKILL_TYPE) return;

            UpdateExploreRadius();
        }
    }

    [HarmonyPatch(typeof(ZNetScene))]
    static class ZNetScenePatches
    {
        [HarmonyPatch(nameof(ZNetScene.Awake))]
        static void AwakePostfix()
        {
            UpdateLocalization(null, null);
            Localization.OnLanguageChange += OnLanguageChange;
        }

        [HarmonyPatch(nameof(ZNetScene.Shutdown))]
        static void ShutdownPostfix()
        {
            Localization.OnLanguageChange -= OnLanguageChange;
        }
    }

    [HarmonyPatch(typeof(Skills))]
    static class SkillPatches
    {
        [HarmonyPatch(nameof(Skills.GetSkillDef))]
        [HarmonyPostfix]
        static void GetSkillDefPostfix(Skills.SkillType type, ref Skills.SkillDef __result, List<Skills.SkillDef> ___m_skills)
        {
            if (!config.EnableSkill || __result != null || (int)type != SKILL_TYPE) return;

            ___m_skills.Add(cartographySkillDef);
            __result = cartographySkillDef;
        }

        [HarmonyPatch(nameof(Skills.IsSkillValid))]
        [HarmonyPostfix]
        static void IsSkillValidPostfix(Skills.SkillType type, ref bool __result)
        {
            if (!config.EnableSkill || __result) return;

            __result = (int)type == SKILL_TYPE;
        }


        [HarmonyPatch(nameof(Skills.CheatRaiseSkill))]
        [HarmonyPrefix]
        static bool CheatRaiseSkillPrefix(Skills __instance, string name, float value, Player ___m_player)
        {
            if (!config.EnableSkill || !IsMatchingSkillName(name)) return true;

            Skills.Skill skill = __instance.GetSkill((Skills.SkillType)SKILL_TYPE);
            skill.m_level = Mathf.Clamp(skill.m_level + value, 0f, 100f);

            ___m_player.Message(MessageHud.MessageType.TopLeft, $"Skill increased {config.SkillName}: {(int)skill.m_level}", 0, skill.m_info.m_icon);
            Console.instance.Print($"Skill {config.SkillName} = {skill.m_level}");

            UpdateExploreRadius();

            return false;
        }

        [HarmonyPatch(nameof(Skills.CheatResetSkill))]
        [HarmonyPrefix]
        static bool CheatResetSkillPrefix(Skills __instance, string name)
        {
            if (!config.EnableSkill || !IsMatchingSkillName(name)) return true;

            __instance.ResetSkill((Skills.SkillType)SKILL_TYPE);
            Console.instance.Print("Skill " + config.SkillName + " reset");

            UpdateExploreRadius();

            return false;
        }
    }
}
