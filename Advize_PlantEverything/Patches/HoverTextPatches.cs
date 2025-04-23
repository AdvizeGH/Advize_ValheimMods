namespace Advize_PlantEverything;

using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using static PlantEverything;

[HarmonyPatch]
static class HoverTextPatches
{
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    static void Postfix(Pickable __instance, ref string __result)
    {
        if (config.EnablePickableTimers && __instance.m_picked && __instance.m_nview && __instance.m_nview.IsValid() && __instance.m_respawnTimeMinutes > 0)
        {
            long pickedTime = __instance.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L);
            if (pickedTime > 1)
            {
                TimeSpan timeSpan = ZNet.instance.GetTime() - new DateTime(pickedTime);
                double respawnTimeSeconds = GetSecondsToRespawnPickable(__instance);
                double percent = timeSpan.TotalSeconds / respawnTimeSeconds;
                
                string timeString = FormatTime(percent, respawnTimeSeconds - timeSpan.TotalSeconds);

                __result = Localization.instance.Localize(__instance.GetHoverName()) + $"\n{timeString}";
            }
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.GetHoverText))]
    static void Postfix(Plant __instance, ref string __result)
    {
        if (config.EnablePlantTimers && __instance.GetStatus() == Plant.Status.Healthy && __instance.m_nview && __instance.m_nview.IsValid())
        {
            double respawnTimeSeconds = GetSecondsToGrowPlant(__instance);
            double percent = __instance.TimeSincePlanted() / __instance.GetGrowTime();

            string timeString = FormatTime(percent, respawnTimeSeconds);

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

    private static Gradient gradient;

    public static void UpdateGradient()
    {
        if (gradient != null)
            return;

        gradient = new Gradient();
        gradient.SetKeys(
            [
                new GradientColorKey(Color.red, 0.0f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f), // Orange
                new GradientColorKey(Color.yellow, 0.75f),
                new GradientColorKey(new Color(0.2f, 0.8f, 0.2f), 1.0f)  // Limegreen
            ],
            Array.Empty<GradientAlphaKey>()
        );
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
        UpdateGradient();

        string color = percent < 0 ? "00FFFF" : ColorUtility.ToHtmlStringRGB(gradient.Evaluate(Mathf.Clamp01((float)percent)));

        if (config.GrowthAsPercentage)
            return $"(<color=#{color}>{percent:P0}</color>)";
        
        if (secondsToGrow <= 0)
            return Localization.instance.Localize("$hud_ready");

        TimeSpan timeSpan = TimeSpan.FromSeconds(secondsToGrow);

        string timeRemaining = timeSpan.Hours > 0
            ? $"{timeSpan.Hours:D2}h {timeSpan.Minutes:D2}m {timeSpan.Seconds:D2}s"
            : timeSpan.Minutes > 0
                ? $"{timeSpan.Minutes:D2}m {timeSpan.Seconds:D2}s"
                : $"{timeSpan.Seconds:D2}s";

        return secondsToGrow < 60 ? $"(<color=#{color}>Ready any second now</color>)" : $"(Ready in <color=#{color}>{timeRemaining}</color>)";
    }

    public static class SeasonsCompatibility
    {
        private static bool _isInitialized;
        private static bool _isReady;

        private const string GUID = "shudnal.Seasons";
        private static Assembly _assembly;
        private static FieldInfo _seasonState;
        private static MethodInfo _getSecondsToRespawnPickable;
        private static MethodInfo _getSecondsToGrowPlant;

        public static bool IsReady
        {
            get 
            {
                if (!_isInitialized)
                    Initialize();

                return _isReady;
            }
        }

        public static double GetSecondsToRespawnPickable(Pickable pickable) => (double)_getSecondsToRespawnPickable.Invoke(_seasonState.GetValue(_seasonState), [pickable]);

        public static double GetSecondsToGrowPlant(Plant plant) => (double)_getSecondsToGrowPlant.Invoke(_seasonState.GetValue(_seasonState), [plant]);

        private static void Initialize()
        {
            _isInitialized = true;

            if (Chainloader.PluginInfos.TryGetValue(GUID, out PluginInfo seasons))
            {
                _assembly = Assembly.GetAssembly(seasons.Instance.GetType());
                if (_assembly != null)
                {
                    _seasonState = AccessTools.Field(_assembly.GetType("Seasons.Seasons"), "seasonState");
                    _getSecondsToRespawnPickable = AccessTools.Method(_seasonState.FieldType, "GetSecondsToRespawnPickable");
                    _getSecondsToGrowPlant = AccessTools.Method(_seasonState.FieldType, "GetSecondsToGrowPlant");
                }
            }

            _isReady = _seasonState != null && _getSecondsToRespawnPickable != null && _getSecondsToGrowPlant != null;
        }
    }
}
