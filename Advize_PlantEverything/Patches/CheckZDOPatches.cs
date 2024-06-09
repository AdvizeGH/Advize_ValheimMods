namespace Advize_PlantEverything;

using HarmonyLib;
using static StaticContent;

[HarmonyPatch]
static class CheckZDOPatches
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Awake))]
    static void Postfix(ZNetView ___m_nview)
    {
        if (!___m_nview || !___m_nview.IsValid() || !___m_nview.GetZDO().GetBool(PlaceAnywhereHash)) return;

        ___m_nview.GetComponent<StaticPhysics>().m_fall = false;
    }
}
