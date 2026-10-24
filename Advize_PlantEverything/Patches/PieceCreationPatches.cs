namespace Advize_PlantEverything;

using HarmonyLib;
using static PluginUtils;
using static StaticMembers;

[HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
static class PieceCreationPatches
{
    static void Postfix(Piece __instance)
    {
        bool isModdedPrefab = IsModdedPrefabOrSapling(__instance.m_name);
        // prefabs added through ExtraResources.json are treated like the plants of the mod
        bool isExtraResource = !isModdedPrefab && IsExtraResourcePrefab(GetPrefabName(__instance));

        if (!isModdedPrefab && !isExtraResource) return;

        if (config.ResourcesSpawnEmpty && CanSpawnEmpty(__instance) && __instance.m_name != "Pickable_Stone")
        {
            __instance.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", true);
        }

        // only the plants of the mod are placed anywhere
        if (isModdedPrefab && config.PlaceAnywhere && __instance.TryGetComponent(out StaticPhysics sp))
        {
            sp.m_fall = false;
            __instance.m_nview.GetZDO().Set(PlaceAnywhereHash, true);
        }
    }

    /// <summary>
    /// A pickable that does not regrow would never come back, so it is always
    /// placed full.
    /// </summary>
    private static bool CanSpawnEmpty(Piece piece)
    {
        Pickable pickable = piece.GetComponent<Pickable>();

        return pickable && pickable.m_respawnTimeMinutes > 0f;
    }
}
