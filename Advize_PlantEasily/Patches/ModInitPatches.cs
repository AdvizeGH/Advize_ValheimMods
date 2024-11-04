namespace Advize_PlantEasily;

using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlantEasily;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ModInitPatches
{
    [HarmonyPriority(Priority.First)]
    static void Prefix(ZNetScene __instance, ref List<GameObject> __state)
    {
        __state = prefabRefs.Count == 0 ? new(__instance.m_prefabs) : null;

        if (__state != null)
        {
            List<GameObject> filteredPrefabs = new(__instance.m_prefabs);
            filteredPrefabs.RemoveAll(go => !go.TryGetComponent(out Plant p) || p.m_grownPrefabs.Any(tb => tb.GetComponent<TreeBase>()));

            Dbgl($"({filteredPrefabs.Count}) vanilla crops detected");
            foreach (GameObject go in filteredPrefabs)
            {
                ReplantDB replantDB = new(go);
                //vanillaCropRefs.Add(replantDB);
                pickableNamesToReplantDB.Add(replantDB.pickable.name, replantDB);
            }
        }
    }

    [HarmonyPriority(Priority.Last)]
    static void Postfix(ZNetScene __instance, List<GameObject> __state)
    {
        if (__state != null)
        {
            List<GameObject> filteredPrefabs = __instance.m_prefabs.Except(__state).ToList();
            filteredPrefabs.RemoveAll(go => !go.TryGetComponent(out Plant p) || p.m_grownPrefabs.Any(tb => tb.GetComponent<TreeBase>()));

            Dbgl($"({filteredPrefabs.Count}) modded crops detected");
            foreach (GameObject go in filteredPrefabs)
            {
                ReplantDB replantDB = new(go);
                //moddedCropRefs.Add(replantDB);
                pickableNamesToReplantDB.Add(replantDB.pickable.name, replantDB);
            }

            InitPrefabRefs();
        }
    }
}
