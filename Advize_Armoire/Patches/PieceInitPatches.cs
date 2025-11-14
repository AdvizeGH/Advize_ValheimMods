namespace Advize_Armoire;

using System;
using HarmonyLib;
using SoftReferenceableAssets;
using UnityEngine;
using static StaticMembers;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class PieceInitPatches
{
    static bool isInitialized = false;

    static void Postfix(ZNetScene __instance)
    {
        __instance.m_prefabs.Add(armoirePiecePrefab);
        __instance.m_namedPrefabs.Add(__instance.GetPrefabHash(armoirePiecePrefab), armoirePiecePrefab);

        PieceTable pieceTable = __instance.GetPrefab("Hammer").GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;

        if (!pieceTable.m_pieces.Contains(armoirePiecePrefab))
        {
            Dbgl("piece is not in build table, adding");
            pieceTable.m_pieces.Add(armoirePiecePrefab);
        }

        if (!isInitialized)
        {
            InitializeArmoirePiece(__instance);
            isInitialized = true;
        }
    }

    private static void InitializeArmoirePiece(ZNetScene instance)
    {
        /* Fix refs for armoire piece */
        // Get the source prefab and component refs
        GameObject sourcePrefab = instance.GetPrefab("wood_door");
        Door door = sourcePrefab.GetComponent<Door>();
        Piece sourcePiece = sourcePrefab.GetComponent<Piece>();
        WearNTear sourceWNT = sourcePrefab.GetComponent<WearNTear>();

        // Get the armoire component refs
        Piece armoirePiece = armoirePiecePrefab.GetComponent<Piece>();
        WearNTear targetWNT = armoirePiecePrefab.GetComponent<WearNTear>();

        // Copy effects from wood_door to armoire
        ArmoireDoor.SoundEffects.Add(door.m_closeEffects);
        ArmoireDoor.SoundEffects.Add(door.m_openEffects);
        targetWNT.m_destroyedEffect.m_effectPrefabs = sourceWNT.m_destroyedEffect.m_effectPrefabs;
        targetWNT.m_hitEffect.m_effectPrefabs = sourceWNT.m_hitEffect.m_effectPrefabs;
        armoirePiece.m_placeEffect = sourcePiece.m_placeEffect;
        armoirePiece.m_craftingStation = sourcePiece.m_craftingStation;

        // Assign resource requirements
        string[] itemNames = { "Wood", "Tin", "Bronze" };
        for (int i = 0; i < itemNames.Length; i++)
            armoirePiece.m_resources[i].m_resItem = ObjectDB.instance.GetItemPrefab(itemNames[i]).GetComponent<ItemDrop>();

        // Setup mesh materials
        Material[] materials = CreateArmoireMaterials();
        Array.ForEach(["DoorLeft", "DoorRight", "Wardrobe"], child => armoirePiecePrefab.transform.Find(child).GetComponent<MeshRenderer>().sharedMaterials = materials);

        /* Now fix refs for equipment inside wardrobe */
        Transform equipment = armoirePiecePrefab.transform.Find("EquipmentAttachPoints");

        ReplaceMesh(instance, "ShieldIronBuckler", "attach/model (1)", equipment.GetChild(0).GetChild(0));
        ReplaceMesh(instance, "SpearElderbark", "attach/default", equipment.GetChild(1).GetChild(0));
        ReplaceMesh(instance, "Bow", "attach/default", equipment.GetChild(1).GetChild(1));
        ReplaceSkinnedMesh(instance, "HelmetOdin", "attach_skin/hood", equipment.GetChild(2).GetChild(0).GetChild(0));
        ReplaceSkinnedMesh(instance, "CapeOdin", "attach_skin/cape1", equipment.GetChild(2).GetChild(1).GetChild(0));
    }

    private static Material[] CreateArmoireMaterials()
    {
        AssetID.TryParse("f6de4704e075b4095ae641aed283b641", out AssetID id);
        SoftReference<Shader> shaderRef = new(id);
        shaderRef.Load();

        Material mat1 = new(shaderRef.Asset) { mainTexture = UIResources.GetTexture("woodtower_d") };
        mat1.SetInt("_Cull", 0);
        mat1.SetFloat("_TwoSidedNormals", 1f);

        Material mat2 = new(shaderRef.Asset) { mainTexture = UIResources.GetTexture("DarkWoodBeams_d") };

        return [mat1, mat2];
    }

    private static void ReplaceMesh(ZNetScene scene, string prefabName, string path, Transform target)
    {
        Transform source = scene.GetPrefab(prefabName).transform.Find(path);
        target.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        target.GetComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
    }

    private static void ReplaceSkinnedMesh(ZNetScene scene, string prefabName, string path, Transform target)
    {
        SkinnedMeshRenderer source = scene.GetPrefab(prefabName).transform.Find(path).GetComponent<SkinnedMeshRenderer>();
        SkinnedMeshRenderer targetRenderer = target.GetComponent<SkinnedMeshRenderer>();
        targetRenderer.sharedMesh = source.sharedMesh;
        targetRenderer.sharedMaterials = source.sharedMaterials;
    }
}
