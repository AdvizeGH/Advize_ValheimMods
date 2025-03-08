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
    static void Postfix()
    {
        if (!isDedicatedServer)
            InitPrefabRefs();
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    static class ZNetScenePatches
    {
        private static List<GameObject> unfilteredPrefabs;

        [HarmonyPriority(Priority.First)]
        static void Prefix(ZNetScene __instance)
        {
            unfilteredPrefabs = moddedCropRefs.Count == 0 && moddedSaplingRefs.Count == 0 ? new(__instance.m_prefabs) : null;
        }

        static void Postfix(ZNetScene __instance)
        {
            if (isDedicatedServer)
                InitPrefabRefs();

            FullInit(__instance);
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void LastPostfix(ZNetScene __instance)
        {
            if (unfilteredPrefabs != null)
            {
                List<GameObject> filteredPrefabs = __instance.m_prefabs.Except(unfilteredPrefabs).ToList();

                filteredPrefabs.RemoveAll(x => !x.GetComponent<Plant>());
                filteredPrefabs.RemoveAll(saplingRefs.Select(x => x.Prefab).ToList().Contains);

                foreach (ModdedPlantDB moddedPlant in StaticContent.GenerateCustomPlantRefs(filteredPrefabs))
                {
                    if (moddedPlant.Prefab.GetComponent<Plant>().m_grownPrefabs.Any(x => x.GetComponent<TreeBase>()))
                    {
                        Dbgl($"Adding modded sapling reference {moddedPlant.key}");
                        moddedSaplingRefs.Add(moddedPlant);
                    }
                    else
                    {
                        Dbgl($"Adding modded crop reference {moddedPlant.key}");
                        moddedCropRefs.Add(moddedPlant);
                    }
                }

                if (moddedCropRefs.Count > 0)
                {
                    Dbgl($"Added {moddedCropRefs.Count} modded crop references");
                    ConfigEventHandlers.CropSettingChanged(null, null);
                }
                if (moddedSaplingRefs.Count > 0)
                {
                    Dbgl($"Added {moddedSaplingRefs.Count} modded sapling references");
                    ConfigEventHandlers.SaplingSettingChanged(null, null);
                }
                unfilteredPrefabs.Clear();
            }

            if (resolveMissingReferences)
            {
                Dbgl("Performing final attempt to resolve missing references for configured ExtraResources", true);

                resolveMissingReferences = false;

                if (InitExtraResourceRefs(__instance, true))
                {
                    Dbgl("One or more missing references for configured ExtraResources were successfully resolved", true);
                    ConfigEventHandlers.PieceSettingChanged(null, null);
                }
            }
        }
    }
}
