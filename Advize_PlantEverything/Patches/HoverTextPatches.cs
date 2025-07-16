namespace Advize_PlantEverything;

using HarmonyLib;
using System;
using UnityEngine;
using static PlantEverything;

[HarmonyPatch]
static class HoverTextPatches
{
    private static Gradient _hoverGradient;
    private static Gradient HoverGradient => _hoverGradient ?? InitializeGradient();

    public static Gradient InitializeGradient()
    {
        _hoverGradient = new();
        _hoverGradient.SetKeys(
        [
            new GradientColorKey(Color.red, 0.0f),
            new GradientColorKey(new Color(1f, 0.6470588f, 0f), 0.3f), // Orange #FFA500
            new GradientColorKey(Color.yellow, 0.6f),
            new GradientColorKey(Color.green, 0.9f),
            new GradientColorKey(Color.cyan, 1.0f)
        ], []);

        return _hoverGradient;
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    static void Postfix(Pickable __instance, ref string __result)
    {
        if (!config.EnablePlantTimers || !__instance.m_picked || __instance.m_respawnTimeMinutes <= 0 || __instance.m_nview?.GetZDO() is not ZDO zdo) return;

        long pickedTime = zdo.GetLong(ZDOVars.s_pickedTime, 0L);
        TimeSpan timeSpan = ZNet.instance.GetTime() - new DateTime(pickedTime);
        double respawnTimeSeconds = GetSecondsToRespawnPickable(__instance);
        double percent = timeSpan.TotalSeconds / respawnTimeSeconds;
                
        string timeString = FormatTime(percent, respawnTimeSeconds - timeSpan.TotalSeconds);

        __result = Localization.instance.Localize(__instance.GetHoverName()) + $"\n{timeString}";
        
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.GetHoverText))]
    static void Postfix(Plant __instance, ref string __result)
    {
        if (!config.EnablePlantTimers || __instance.m_status != 0 || __instance.m_nview?.GetZDO() is null) return;

        double respawnTimeSeconds = GetSecondsToGrowPlant(__instance);
        double percent = __instance.TimeSincePlanted() / __instance.GetGrowTime();

        string timeString = FormatTime(percent, respawnTimeSeconds);

        __result += $"\n{timeString}";
    }

    static double GetSecondsToRespawnPickable(Pickable pickable)
    {
        if (SeasonsCompatibility.IsReady)
            return SeasonsCompatibility.GetSecondsToRespawnPickable(pickable);

        return pickable.m_respawnTimeMinutes * 60;
    }

    static double GetSecondsToGrowPlant(Plant plant)
    {
        if (SeasonsCompatibility.IsReady)
            return SeasonsCompatibility.GetSecondsToGrowPlant(plant);

        return plant.GetGrowTime() - plant.TimeSincePlanted();
    }

    static string FormatTime(double percent, double secondsToGrow)
    {
        float clampedPercentage = Mathf.Clamp01((float)percent);
        string color = ColorUtility.ToHtmlStringRGB(HoverGradient.Evaluate(clampedPercentage));

        if (config.GrowthAsPercentage)
            return $"(<color=#{color}>{clampedPercentage:P0}</color>)";
        
        if (secondsToGrow <= 0)
            return Localization.instance.Localize($"<color=#{color}>$hud_ready</color>");

        TimeSpan t = TimeSpan.FromSeconds(secondsToGrow);

        string timeRemaining = t.Hours <= 0 ? t.Minutes <= 0 ?
            $"{t.Seconds:D2}s" : $"{t.Minutes:D2}m {t.Seconds:D2}s" : $"{t.Hours:D2}h {t.Minutes:D2}m {t.Seconds:D2}s";

        return $"(Ready in <color=#{color}>{timeRemaining}</color>)";
    }
}
