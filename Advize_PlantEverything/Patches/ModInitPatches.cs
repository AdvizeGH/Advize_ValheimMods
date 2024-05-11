namespace Advize_PlantEverything;

using HarmonyLib;
using static PlantEverything;

[HarmonyPatch]
static class ModInitPatches
{
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    static void Postfix()
    {
        Dbgl("ObjectDBAwake");
        InitPrefabRefs();
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static void Postfix(ZNetScene __instance)
    {
        Dbgl("ZNetSceneAwake");
        FinalInit(__instance);
    }

    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static void LastPostfix(ZNetScene __instance)
    {
        if (!resolveMissingReferences) return;

        Dbgl("ZNetSceneAwake2");
        Dbgl("Performing final attempt to resolve missing references for configured ExtraResources", true);

        resolveMissingReferences = false;

        if (InitExtraResourceRefs(__instance, true))
        {
            Dbgl("One or more missing references for configured ExtraResources were successfully resolved", true);
            PieceSettingChanged(null, null);
        }
    }
}
