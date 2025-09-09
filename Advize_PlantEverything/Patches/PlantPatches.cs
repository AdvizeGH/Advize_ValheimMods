namespace Advize_PlantEverything;

using HarmonyLib;
using static PluginUtils;
using static StaticMembers;

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
}
