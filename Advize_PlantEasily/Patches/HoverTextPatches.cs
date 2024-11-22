namespace Advize_PlantEasily;

using HarmonyLib;
using static PlantEasily;

[HarmonyPatch]
static class HoverTextPatches
{
    static string CurrentModifierKey => ZInput.GamepadActive ? gamepadModifierKeyLocalized : keyboardHarvestModifierKeyLocalized;
    static string GetPrefabName(Pickable p) => p.name.Replace("(Clone)", "");

    [HarmonyPatch(typeof(Beehive), nameof(Beehive.GetHoverText))]
    static void Postfix(Beehive __instance, ref string __result)
    {
        if (!config.ModActive || !config.EnableBulkHarvest || !config.ShowHoverKeyHints) return;

        // only add our hover text if honey can actually be extracted
        bool isPrivate = !PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false);
        bool hasHoney = __instance.GetHoneyLevel() > 0;
        if (isPrivate || !hasHoney) return;

        string hoverTextSuffix = $"\n[<b><color=yellow>{CurrentModifierKey}</color> + <color=yellow>$KEY_Use</color></b>] {__instance.m_extractText} (area)";
        __result += Localization.instance.Localize(hoverTextSuffix);
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    static void Postfix(Pickable __instance, ref string __result)
    {
        if (!config.ModActive || !config.EnableBulkHarvest || !config.ShowHoverKeyHints) return;

        // only add our hover text if the pickable can actually be picked
        if (__instance.GetPicked() || __instance.GetEnabled == 0) return;

        string hoverTextSuffix = $"\n[<b><color=yellow>{CurrentModifierKey}</color> + <color=yellow>$KEY_Use</color></b>] $inventory_pickup (area)";
        if (config.ReplantOnHarvest && config.ShowHoverReplantHint && pickableNamesToReplantDB.ContainsKey(GetPrefabName(__instance)))
        {
            string hoverName = string.IsNullOrEmpty(lastPlacementGhost) ? __instance.GetHoverName() : pickableNamesToReplantDB[lastPlacementGhost].pickable.GetHoverName();
            hoverTextSuffix += $"\nReplant as: <color=green>{hoverName}</color>";
        }
        __result += Localization.instance.Localize(hoverTextSuffix);
    }
}
