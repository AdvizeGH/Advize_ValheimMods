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

        if (interactable as Pickable && config.ReplantOnHarvest && pickableNamesToReplantDB.ContainsKey(GetPrefabName(interactable)))
            instanceIDS.Add(((Pickable)interactable).GetInstanceID());

        if (!config.EnableBulkHarvest || (!ZInput.GetKey(config.KeyboardHarvestModifierKey, false) && !ZInput.GetKey(config.GamepadModifierKey, false)))
            return;

        if (interactable as Pickable || interactable as Beehive)
        {
            foreach (Interactable extraInteractable in FindResourcesInRadius(go))
            {
                if (config.ReplantOnHarvest && pickableNamesToReplantDB.ContainsKey(GetPrefabName(extraInteractable)))
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
        string plantPrefabName = string.IsNullOrEmpty(lastPlacementGhost) ? 
            pickableNamesToReplantDB[__instance.name.Replace("(Clone)", "")].plantName : 
            pickableNamesToReplantDB[lastPlacementGhost].plantName;
        Piece piece = prefabRefs[plantPrefabName].GetComponent<Piece>();

        if (!player.HaveRequirements(piece, Player.RequirementMode.CanBuild) && !Player.m_localPlayer.m_noPlacementCost) return;

        PlacePiece(player, __instance.gameObject, piece.gameObject);
        player.ConsumeResources(piece.m_resources, 0);
    }
}
