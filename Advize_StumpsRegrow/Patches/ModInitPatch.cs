namespace Advize_StumpsRegrow;

using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using static StumpsRegrow;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ModInitPatch
{
    static bool initialized = false;

    [HarmonyPriority(Priority.LowerThanNormal)]
    static void Postfix(ZNetScene __instance)
    {
        if (!initialized)
        {
            foreach (GameObject go in __instance.m_prefabs)
            {
                if (go.TryGetComponent(out TreeBase v) && v.m_stubPrefab)
                {
                    //Dbgl($"Found Tree prefab: {go.name}, stub: {v.m_stubPrefab.name}");

                    if (!PotentialTrees.TryGetValue(v.m_stubPrefab.name, out List<GameObject> treeList))
                    {
                        treeList = [];
                        PotentialTrees[v.m_stubPrefab.name] = treeList;
                    }

                    treeList.Add(go);

                    if (!v.m_stubPrefab.GetComponent<StumpGrower>())
                    {
                        v.m_stubPrefab.AddComponent<StumpGrower>();
                        HoverText hoverTextComponent = v.m_stubPrefab.GetComponent<HoverText>();
                        if (hoverTextComponent) Object.Destroy(hoverTextComponent);
                    }
                }
            }

            initialized = true;
        }

        //foreach (var b in PotentialTrees.Keys)
        //{
        //    Dbgl($"b {b}");
        //    foreach (var c in PotentialTrees[b])
        //    {
        //        Dbgl($"c {c.name}");
        //    }
        //}
    }
}
