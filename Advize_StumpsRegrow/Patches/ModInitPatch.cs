namespace Advize_StumpsRegrow;

using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using static StumpsRegrow;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ModInitPatch
{
    [HarmonyPriority(Priority.LowerThanNormal)]
    static void Postfix(ZNetScene __instance)
    {
        if (TreesPerStump.Count == 0)
        {
            foreach (GameObject go in __instance.m_prefabs)
            {
                if (go.TryGetComponent(out TreeBase v) && v.m_stubPrefab)
                {
                    if (!TreesPerStump.TryGetValue(v.m_stubPrefab.name, out List<GameObject> treeList))
                    {
                        treeList = [];
                        TreesPerStump[v.m_stubPrefab.name] = treeList;
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
        }
    }
}
