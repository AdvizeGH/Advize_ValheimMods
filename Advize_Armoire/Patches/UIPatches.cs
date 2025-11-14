namespace Advize_Armoire;

using HarmonyLib;
using static ArmoireUIController;

[HarmonyPatch]
static class UIPatches
{
    [HarmonyPatch(typeof(Hud))]
    static class HUDPatches
    {
        [HarmonyPatch(nameof(Hud.Awake))]
        static void Postfix(Hud __instance) => CreateArmoireUI(__instance.m_rootObject.transform);

        [HarmonyPatch(nameof(Hud.OnDestroy))]
        static void Prefix() => DestroyArmoireUI();
    }

    [HarmonyPatch(typeof(UnifiedPopup), nameof(UnifiedPopup.IsVisible))]
    static class UnifiedPopupIsVisiblePatch
    {
        static bool Prefix(ref bool __result)
        {
            if (Player.m_localPlayer && IsArmoirePanelActive())
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
