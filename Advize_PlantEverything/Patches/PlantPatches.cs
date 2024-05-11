namespace Advize_PlantEverything;

using HarmonyLib;
using UnityEngine;
using static PlantEverything;
using static StaticContent;

[HarmonyPatch]
static class PlantPatches
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.HaveRoof))]
    static class PlantHaveRoof
    {
        static bool Prefix(Plant __instance, ref bool __result)
        {
            if ((!config.CropRequireSunlight && __instance.m_name.StartsWith("$piece_sapling")) || (config.PlaceAnywhere && IsModdedPrefabOrSapling(__instance.m_name)))
                return __result = false;

            return true;
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.HaveGrowSpace))]
    static class PlantHaveGrowSpace
    {
        static bool Prefix(Plant __instance, ref bool __result)
        {
            if ((!config.CropRequireGrowthSpace && __instance.m_name.StartsWith("$piece_sapling")) || (config.PlaceAnywhere && IsModdedPrefabOrSapling(__instance.m_name)))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.PlaceAgainst))]
    static void Postfix(Plant __instance, GameObject obj)
    {
        //Dbgl("Vine Sapling Grow");
        if (__instance.m_nview.GetZDO().GetBool(ModdedVineHash))
        {
            //Dbgl("passing zdo from sapling to grown vine");
            VineColor component = obj.GetComponent<VineColor>();

            component.CopyZDOs(__instance.m_nview.GetZDO());
            component.ApplyColor();
        }
    }
}
