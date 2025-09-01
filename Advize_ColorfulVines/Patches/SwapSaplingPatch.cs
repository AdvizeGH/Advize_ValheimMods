namespace Advize_ColorfulVines;

using HarmonyLib;
using UnityEngine;
using static StaticMembers;

[HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
static class SwapSaplingPatch
{
    [HarmonyPostfix]
    static void SwapPlacedSapling(Piece __instance)
    {
        if (__instance.m_nview?.m_zdo.m_prefab != saplingHash)
            return;

        Piece piece = Object.Instantiate(prefabRefs["VineAsh_sapling"], __instance.transform.position, __instance.transform.rotation).GetComponent<Piece>();
        piece.SetCreator(Player.m_localPlayer.GetPlayerID());

        VineColor component = piece.GetComponent<VineColor>();

        component.SetColorZDOs([VineColorFromConfig, .. BerryColorsFromConfig]);
        component.ApplyColor();

        __instance.m_nview.Destroy();
    }
}
