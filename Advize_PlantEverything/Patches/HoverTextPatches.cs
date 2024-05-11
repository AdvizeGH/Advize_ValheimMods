namespace Advize_PlantEverything;

using HarmonyLib;
using System;
using static PlantEverything;

[HarmonyPatch]
static class HoverTextPatches
{
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    static void Postfix(Pickable __instance, ref string __result)
    {
        if (__instance.m_picked && config.EnablePickableTimers && __instance.m_nview.GetZDO() != null)
        {
            if (__instance.m_respawnTimeMinutes == 0) return;

            float growthTime = __instance.m_respawnTimeMinutes * 60;
            DateTime pickedTime = new(__instance.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L));
            string timeString = FormatTimeString(growthTime, pickedTime);

            __result = Localization.instance.Localize(__instance.GetHoverName()) + $"\n{timeString}";
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.GetHoverText))]
    static void Postfix(Plant __instance, ref string __result)
    {
        if (config.EnablePlantTimers && __instance.m_status == 0 && __instance.m_nview.GetZDO() != null)
        {
            float growthTime = __instance.GetGrowTime();
            DateTime plantTime = new(__instance.m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks));
            string timeString = FormatTimeString(growthTime, plantTime);

            __result += $"\n{timeString}";
        }
    }

    static string FormatTimeString(float growthTime, DateTime placedTime)
    {
        TimeSpan timeSincePlaced = ZNet.instance.GetTime() - placedTime;
        TimeSpan t = TimeSpan.FromSeconds(growthTime - timeSincePlaced.TotalSeconds);

        double remainingMinutes = (growthTime / 60) - timeSincePlaced.TotalMinutes;
        double remainingRatio = remainingMinutes / (growthTime / 60);
        int growthPercentage = Math.Min((int)((timeSincePlaced.TotalSeconds * 100) / growthTime), 100);

        string color = "red";
        if (remainingRatio < 0)
            color = "#00FFFF"; // cyan
        else if (remainingRatio < 0.25)
            color = "#32CD32"; // lime
        else if (remainingRatio < 0.5)
            color = "yellow";
        else if (remainingRatio < 0.75)
            color = "orange";

        string timeRemaining = t.Hours <= 0 ? t.Minutes <= 0 ?
            $"{t.Seconds:D2}s" : $"{t.Minutes:D2}m {t.Seconds:D2}s" : $"{t.Hours:D2}h {t.Minutes:D2}m {t.Seconds:D2}s";

        string formattedString = config.GrowthAsPercentage ?
            $"(<color={color}>{growthPercentage}%</color>)" : remainingMinutes < 0.0 ?
            $"(<color={color}>Ready any second now</color>)" : $"(Ready in <color={color}>{timeRemaining}</color>)";

        return formattedString;
    }
}
