namespace Advize_PlantEasily;

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using static ModContext;
using static ModUtils;

[HarmonyPatch]
static class InteractPatches
{
    private static readonly List<int> _instanceIDS = [];

    [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
    static void Prefix(Player __instance, GameObject go, bool hold, bool alt)
    {
        if (!config.ModActive || (!config.EnableBulkHarvest && !config.ReplantOnHarvest) || __instance.InAttack() || __instance.InDodge() || (hold && Time.time - __instance.m_lastHoverInteractTime < 0.2f))
            return;

        if (go.GetComponentInParent<Interactable>() is not Interactable interactable)
            return;

        if (config.ReplantOnHarvest && interactable is Pickable pickable)
        {
            string prefabName = Utils.GetPrefabName(pickable.gameObject);

            if (ReplantDB.Registry.ContainsKey(prefabName))
                _instanceIDS.Add(pickable.GetInstanceID());
        }

        if (!config.EnableBulkHarvest || (!ZInput.GetKey(config.KeyboardHarvestModifierKey, false) && !ZInput.GetKey(config.GamepadModifierKey, false)))
            return;

        if (interactable is not Pickable && interactable is not Beehive)
            return;

        foreach (Interactable extraInteractable in FindResourcesInRadius(go))
        {
            if (config.ReplantOnHarvest && extraInteractable is Pickable extraPickable)
            {
                string pickablePrefabName = Utils.GetPrefabName(extraPickable.gameObject);

                if (ReplantDB.Registry.ContainsKey(pickablePrefabName))
                    _instanceIDS.Add(extraPickable.GetInstanceID());
            }

            extraInteractable.Interact(__instance, hold, alt);
        }
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.SetPicked))]
    static void Prefix(Pickable __instance, bool picked)
    {
        if (!config.ModActive || !config.ReplantOnHarvest || _instanceIDS.Count == 0 || !picked) return;

        int instanceID = __instance.GetInstanceID();
        if (!_instanceIDS.Remove(instanceID))
            return;

        string plantPrefabKey = string.IsNullOrEmpty(PlacementState.LastSelectedPlantName) ? Utils.GetPrefabName(__instance.gameObject) : PlacementState.LastSelectedPlantName;

        if (!ReplantDB.Registry.TryGetValue(plantPrefabKey, out ReplantDB replantEntry))
            return;

        if (!PrefabRefs.TryGetValue(replantEntry.PlantName, out GameObject prefab))
            return;

        Player player = Player.m_localPlayer;
        Piece piece = prefab.GetComponent<Piece>();

        if (!player.HaveRequirements(piece, Player.RequirementMode.CanBuild) && !player.m_noPlacementCost) return;

        PlacementController.PlacePiece(player, __instance.gameObject, piece.gameObject);
        player.ConsumeResources(piece.m_resources, 0);
    }
}
