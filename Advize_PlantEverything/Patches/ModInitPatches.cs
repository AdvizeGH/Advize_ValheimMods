namespace Advize_PlantEverything;

using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlantEverything;

[HarmonyPatch]
static class ModInitPatches
{
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    static void Postfix() => InitPrefabRefs();

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScenePatches
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(ZNetScene __instance, out List<GameObject> __state)
        {
            __state = customPlantRefs.Count == 0 ? new(__instance.m_prefabs) : null;
        }

        static void Postfix(ZNetScene __instance) => FinalInit(__instance);

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void LastPostfix(ZNetScene __instance, List<GameObject> __state)
        {
            if (__state != null)
            {
                List<GameObject> filteredPrefabs = __instance.m_prefabs.Except(__state).ToList();
                __state.Clear();

                filteredPrefabs.RemoveAll(x => !x.GetComponent<Plant>());
                filteredPrefabs.RemoveAll(saplingRefs.Select(x => x.Prefab).ToList().Contains);

                customPlantRefs = StaticContent.GenerateCustomPlantRefs(filteredPrefabs);

                if (customPlantRefs.Count > 0)
                {
                    CropSettingChanged(null, null);
                }
            }

            if (resolveMissingReferences)
            {
                Dbgl("Performing final attempt to resolve missing references for configured ExtraResources", true);

                resolveMissingReferences = false;

                if (InitExtraResourceRefs(__instance, true))
                {
                    Dbgl("One or more missing references for configured ExtraResources were successfully resolved", true);
                    PieceSettingChanged(null, null);
                }
            }
        }
    }
}
