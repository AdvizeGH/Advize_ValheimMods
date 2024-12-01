namespace Advize_PlantEasily;

using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlantEasily;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ModInitPatches
{
    private static List<GameObject> unfilteredPrefabs;

    [HarmonyPriority(Priority.First)]
    static void Prefix(ZNetScene __instance)
    {
        unfilteredPrefabs = prefabRefs.Count == 0 ? new(__instance.m_prefabs) : null;

        if (unfilteredPrefabs != null)
        {
            List<GameObject> filteredPrefabs = new(__instance.m_prefabs);
            filteredPrefabs.RemoveAll(go => !go.TryGetComponent(out Plant p) || p.m_grownPrefabs.Any(gp => !gp.GetComponent<Pickable>() || gp.GetComponent<Vine>() || gp.GetComponent<TreeBase>()));

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
    static void Postfix(ZNetScene __instance)
    {
        if (unfilteredPrefabs != null)
        {
            List<GameObject> plantablePickables = new(unfilteredPrefabs);
            plantablePickables.RemoveAll(go => !go.GetComponent<Pickable>() || !go.GetComponent<Piece>());
            plantablePickables.ForEach(go => pickableRefs.Add(new(go.name)));
            Dbgl($"({plantablePickables.Count}) plantable pickables detected");

            List<GameObject> filteredPrefabs = __instance.m_prefabs.Except(unfilteredPrefabs).ToList();
            filteredPrefabs.RemoveAll(go => !go.TryGetComponent(out Plant p) || p.m_grownPrefabs.Any(gp => !gp.GetComponent<Pickable>() || gp.GetComponent<Vine>() || gp.GetComponent<TreeBase>()));

            Dbgl($"({filteredPrefabs.Count}) modded crops detected");
            foreach (GameObject go in filteredPrefabs)
            {
                ReplantDB replantDB = new(go);
                //moddedCropRefs.Add(replantDB);
                pickableNamesToReplantDB.Add(replantDB.pickable.name, replantDB);
            }

            InitPrefabRefs();
            unfilteredPrefabs.Clear();
        }
    }
}
