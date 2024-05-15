namespace Advize_PlantEverything;

using HarmonyLib;
using static PlantEverything;
using static PluginUtils;
using static StaticContent;

[HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
static class PieceCreationPatches
{
    static void Postfix(Piece __instance)
    {
        if (!IsModdedPrefabOrSapling(__instance.m_name)) return;
        //TODO: Change this when not tired
        if (GetPrefabName(__instance) == "PE_VineAsh_sapling")
        {
            //Dbgl("Custom Sapling placed");

            Piece piece = UnityEngine.Object.Instantiate(prefabRefs["VineAsh_sapling"], __instance.transform.position, __instance.transform.rotation).GetComponent<Piece>();
            piece.SetCreator(Player.m_localPlayer.GetPlayerID());

            VineColor component = piece.GetComponent<VineColor>();

            //component.SetColorZDOs(BerryColorsFromConfig.Prepend(VineColorFromConfig).ToList());
            component.SetColorZDOs([VineColorFromConfig, .. BerryColorsFromConfig]);
            component.ApplyColor();

            __instance.m_nview.Destroy();
        }

        if (config.ResourcesSpawnEmpty && __instance.GetComponent<Pickable>() && __instance.m_name != "Pickable_Stone")
        {
            __instance.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", true);
        }

        if (config.PlaceAnywhere && __instance.TryGetComponent(out StaticPhysics sp))
        {
            sp.m_fall = false;
            __instance.m_nview.GetZDO().Set("pe_placeAnywhere", true);
        }
    }
}
