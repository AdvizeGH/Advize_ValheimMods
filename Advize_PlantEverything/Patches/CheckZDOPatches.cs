namespace Advize_PlantEverything;

using HarmonyLib;

[HarmonyPatch]
static class CheckZDOPatches
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Awake))]
    static void Postfix(ZNetView ___m_nview)
    {
        if (!___m_nview || ___m_nview.GetZDO() == null || !___m_nview.GetZDO().GetBool("pe_placeAnywhere")) return;

        ___m_nview.GetComponent<StaticPhysics>().m_fall = false;
    }
}
