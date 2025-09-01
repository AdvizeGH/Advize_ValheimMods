namespace Advize_ColorfulVines;

using System.Linq;
using HarmonyLib;
using UnityEngine;
using static ConfigEventHandlers;
using static StaticMembers;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ModInitPatch
{
    static void Postfix()
    {
        if (prefabRefs.Count > 0) return;

        // Run only once
        InitializePrefabRefs();
        SetupVines();

        // Apply config
        ApplyVineConfigSettings(null, null);
        UpdateLocalization(null, null);
    }

    static void InitializePrefabRefs()
    {
        prefabRefs.Add("Cultivator", null);
        prefabRefs.Add("VineAsh", null);
        prefabRefs.Add("VineAsh_sapling", null);

        Object[] array = Resources.FindObjectsOfTypeAll(typeof(GameObject));
        for (int i = 0; i < array.Length; i++)
        {
            GameObject gameObject = (GameObject)array[i];

            if (!prefabRefs.ContainsKey(gameObject.name)) continue;

            prefabRefs[gameObject.name] = gameObject;

            if (!prefabRefs.Any(key => !key.Value))
            {
                Dbgl("Found all prefab references.");
                break;
            }
        }
    }

    static void SetupVines()
    {
        GameObject cloneContainer = new("CV_VineAsh_sapling");
        cloneContainer.SetActive(false);
        Object.DontDestroyOnLoad(cloneContainer);

        GameObject VineAsh_saplingClone = Object.Instantiate(prefabRefs["VineAsh_sapling"], cloneContainer.transform);
        VineAsh_saplingClone.name = cloneContainer.name;

        Piece piece = VineAsh_saplingClone.GetComponent<Piece>();
        piece.m_name = "$cvVineAshSaplingName";
        piece.m_description = "$cvVineAshSaplingDescription";

        IconUtils.InitializeVineIcon(piece.m_icon);

        VineAsh_saplingClone.GetComponent<Plant>().m_name = "$cvVineAshSaplingName";
        prefabRefs.Add("CV_VineAsh_sapling", VineAsh_saplingClone);

        prefabRefs["VineAsh"].AddComponent<VineColor>();
        prefabRefs["VineAsh_sapling"].AddComponent<VineColor>();
    }
}
