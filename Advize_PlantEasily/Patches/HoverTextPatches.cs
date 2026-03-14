namespace Advize_PlantEasily;

using HarmonyLib;
using static ModContext;

[HarmonyPatch]
static class HoverTextPatches
{
    static string CurrentModifierKey => ZInput.GamepadActive ? KeyHintPatches.GamepadModifierKeyLocalized : KeyHintPatches.KeyboardHarvestModifierKeyLocalized;

    static bool ShouldShowHints() => config.ModActive && config.EnableBulkHarvest && config.ShowHoverKeyHints;

    static string BuildAreaHint(string actionText)
    {
        return $"\n[<b><color=yellow>{CurrentModifierKey}</color> + <color=yellow>$KEY_Use</color></b>] {actionText} (area)";
    }

    [HarmonyPatch(typeof(Beehive), nameof(Beehive.GetHoverText))]
    static void Postfix(Beehive __instance, ref string __result)
    {
        if (!ShouldShowHints())
            return;

        bool isPrivate = !PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false);
        bool hasHoney = __instance.GetHoneyLevel() > 0;

        if (isPrivate || !hasHoney)
            return;

        string suffix = BuildAreaHint(__instance.m_extractText);
        __result += Localization.instance.Localize(suffix);
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    static void Postfix(Pickable __instance, ref string __result)
    {
        if (!ShouldShowHints() || __instance.GetPicked() || __instance.GetEnabled == 0)
            return;

        string suffix = BuildAreaHint("$inventory_pickup");

        if (config.ReplantOnHarvest && config.ShowHoverReplantHint && ReplantDB.Registry.TryGetValue(Utils.GetPrefabName(__instance.gameObject), out _))
        {
            string hoverName = string.IsNullOrEmpty(PlacementState.LastSelectedPlantName)
                ? __instance.GetHoverName()
                : ReplantDB.Registry[PlacementState.LastSelectedPlantName].Pickable.GetHoverName();

            suffix += $"\nReplant as: <color=green>{hoverName}</color>";
        }

        __result += Localization.instance.Localize(suffix);
    }
}
