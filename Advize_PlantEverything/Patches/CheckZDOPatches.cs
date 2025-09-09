namespace Advize_PlantEverything;

using HarmonyLib;
using static StaticMembers;

[HarmonyPatch]
static class CheckZDOPatches
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Awake))]
    static void Postfix(ZNetView ___m_nview)
    {
        if (___m_nview?.GetZDO() is not ZDO zdo || !zdo.GetBool(PlaceAnywhereHash)) return;

        ___m_nview.GetComponent<StaticPhysics>().m_fall = false;
    }
}
