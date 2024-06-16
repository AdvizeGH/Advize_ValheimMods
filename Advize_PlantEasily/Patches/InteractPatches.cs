namespace Advize_PlantEasily;

using HarmonyLib;
using UnityEngine;
using static PlantEasily;

[HarmonyPatch]
static class InteractPatches
{
    static string GetPrefabName(Interactable i) => i.ToString().Replace("(Clone) (Pickable)", "");

    [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
    static void Prefix(Player __instance, GameObject go, bool hold, bool alt)
    {
        if (!config.ModActive || (!config.EnableBulkHarvest && !config.ReplantOnHarvest) || __instance.InAttack() || __instance.InDodge() || (hold && Time.time - __instance.m_lastHoverInteractTime < 0.2f))
            return;

        Interactable interactable = go.GetComponentInParent<Interactable>();
        if (interactable == null) return;

        if (interactable as Pickable && config.ReplantOnHarvest && pickablesToPlants.ContainsKey(GetPrefabName(interactable)))
            instanceIDS.Add(((Pickable)interactable).GetInstanceID());

        if (!config.EnableBulkHarvest || (!ZInput.GetKey(config.KeyboardHarvestModifierKey, false) && !ZInput.GetKey(config.GamepadModifierKey, false)))
            return;

        if (interactable as Pickable || interactable as Beehive)
        {
            foreach (Interactable extraInteractable in FindResourcesInRadius(go))
            {
                if (config.ReplantOnHarvest && pickablesToPlants.ContainsKey(GetPrefabName(extraInteractable)))
                {
                    instanceIDS.Add(((Pickable)extraInteractable).GetInstanceID());
                }
                extraInteractable.Interact(__instance, hold, alt);
            }
        }
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.SetPicked))]
    static void Prefix(Pickable __instance, bool picked)
    {
        if (!config.ModActive || !config.ReplantOnHarvest || instanceIDS.Count == 0 || !picked) return;

        int instanceID = __instance.GetInstanceID();
        if (!instanceIDS.Contains(instanceID)) return;

        instanceIDS.Remove(instanceID);

        Player player = Player.m_localPlayer;
        GameObject plantObject = prefabRefs[pickablesToPlants[__instance.name.Replace("(Clone)", "")]];
        Piece piece = plantObject.GetComponent<Piece>();

        if (!player.HaveRequirements(piece, Player.RequirementMode.CanBuild) && !Player.m_localPlayer.m_noPlacementCost) return;

        PlacePiece(player, __instance.gameObject, piece);
        player.ConsumeResources(piece.m_resources, 0);
    }

    [HarmonyPatch(typeof(Beehive), nameof(Beehive.GetHoverText))]
    static void Postfix(Beehive __instance, ref string __result)
    {
        if (!config.EnableBulkHarvest) return;

        // only add our hover text if honey can actually be extracted
        var isPrivate = !PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false);
        var hasHoney = __instance.GetHoneyLevel() > 0;
        if (isPrivate || !hasHoney) return;

        KeyCode currentModifierKey = ZInput.GamepadActive ? config.GamepadModifierKey : config.KeyboardHarvestModifierKey;

        var hoverTextSuffix = $"\n[<b><color=yellow>{currentModifierKey.ToLocalizableString()}</color> + <color=yellow>$KEY_Use</color></b>] {__instance.m_extractText} (area)";
        __result += Localization.instance.Localize(hoverTextSuffix);
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    static void Postfix(Pickable __instance, ref string __result)
    {
        if (!config.EnableBulkHarvest) return;

        // only add our hover text if the pickable can actually be picked
        if (__instance.GetPicked() || __instance.GetEnabled == 0) return;

        KeyCode currentModifierKey = ZInput.GamepadActive ? config.GamepadModifierKey : config.KeyboardHarvestModifierKey;

        var hoverTextSuffix = $"\n[<b><color=yellow>{currentModifierKey.ToLocalizableString()}</color> + <color=yellow>$KEY_Use</color></b>] $inventory_pickup (area)";
        __result += Localization.instance.Localize(hoverTextSuffix);
    }
}
