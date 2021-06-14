using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Advize_PlantEverything
{
    public partial class PlantEverything
    {
        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        public static class ObjectDBAwake
        {
            public static void Postfix(ObjectDB __instance)
            {
                Dbgl("ObjectDBAwake");
                InitPrefabRefs();
                if (ZNetScene.instance == null) return;
                Dbgl("& ZNetScene not null");
                InitItems(__instance);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        public static class ObjectDBCopyOtherDB
        {
            public static void Postfix(ObjectDB other)
            {
                Dbgl("ObjectDBCopyOtherDB");
                InitItems(other);
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetSceneAwake
        {
            public static void Postfix(ZNetScene __instance)
            {
                Dbgl("ZNetSceneAwake");
                Dbgl("Performing final mod initialization");
                FinalInit(__instance);
            }
        }

        [HarmonyPatch(typeof(ObjectDB), "GetItemPrefab", new Type[] { typeof(int) })]
        public static class ObjectDBGetItemPrefab
        {
            public static void Postfix(int hash, ref GameObject __result, ObjectDB __instance)
            {
                if (__result == null && prefabRefs.Count > 0)
                {
                    List<GameObject> prefabs = new List<GameObject>
                    {
                        prefabRefs["BirchCone"],
                        prefabRefs["OakSeeds"],
                        prefabRefs["AncientSeeds"]
                    };

                    foreach (GameObject prefab in prefabs)
                    {
                        if (hash == __instance.GetPrefabHash(prefab))
                        {
                            __result = prefab;
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ZNetScene), "GetPrefab", new Type[] { typeof(int) })]
        public static class ZNetSceneGetPrefab
        {
            public static void Postfix(int hash, ref GameObject __result, ZNetScene __instance)
            {
                if (__result == null)
                {
                    List<GameObject> prefabs = new List<GameObject>
                    {
                        prefabRefs["BirchCone"],
                        prefabRefs["OakSeeds"],
                        prefabRefs["AncientSeeds"],
                        prefabRefs["Birch_Sapling"],
                        prefabRefs["Oak_Sapling"],
                        prefabRefs["Ancient_Sapling"]
                    };

                    foreach (GameObject prefab in prefabs)
                    {
                        if (hash == __instance.GetPrefabHash(prefab))
                        {
                            __result = prefab;
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), "RemovePiece")]
        public static class PlayerRemovePiece
        {
            public static bool Prefix(Player __instance, ZSyncAnimation ___m_zanim, ref bool __result)
            {
                ItemDrop.ItemData rightItem = __instance.GetRightItem();
                if (rightItem.m_shared.m_name == "$item_cultivator")
                {
                    if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, LayerMask.GetMask("item", "piece_nonsolid", "Default_small")) && Vector3.Distance(hitInfo.point, __instance.m_eye.position) < __instance.m_maxPlaceDistance)
                    {
                        Piece piece = hitInfo.collider.GetComponentInParent<Piece>();
                        if (piece && piece.m_name.StartsWith("$pe"))
                        {
                            if (!piece.m_canBeRemoved) return false;
                            if (!PrivateArea.CheckAccess(piece.transform.position))
                            {
                                __instance.Message(MessageHud.MessageType.Center, "$msg_privatezone");
                                return false;
                            }

                            ZNetView component = piece.GetComponent<ZNetView>();
                            if (component == null) return false;
                            
                            WearNTear component2 = piece.GetComponent<WearNTear>();
                            if (component2)
                            {
                                __instance.m_removeEffects.Create(piece.transform.position, Quaternion.identity);
                                component2.Remove();
                            }
                            else
                            {
                                component.ClaimOwnership();
                                piece.DropResources();
                                piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, piece.gameObject.transform);
                                __instance.m_removeEffects.Create(piece.transform.position, Quaternion.identity);
                                ZNetScene.instance.Destroy(piece.gameObject);
                                __instance.FaceLookDirection();
                                ___m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
                            }
                            __result = true;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Plant), "Awake")]
        public static class PlantAwake
        {
            public static void Postfix(Plant __instance)
            {
                if (!config.EnforceBiomesVanilla)
                {
                    __instance.m_biome = (Heightmap.Biome)895;
                }
                if (config.EnableCropOverrides && __instance.name.StartsWith("sapling_"))
                {
                    __instance.m_minScale = config.CropMinScale;
                    __instance.m_maxScale = config.CropMaxScale;
                    __instance.m_growTime = config.CropGrowTimeMin;
                    __instance.m_growTimeMax = config.CropGrowTimeMax;
                    __instance.m_growRadius = config.CropGrowRadius;
                    __instance.m_needCultivatedGround = config.CropRequireCultivation;
                    (__instance.GetComponentInParent(typeof(Piece)) as Piece).m_cultivatedGroundOnly = config.CropRequireCultivation;
                }
            }
        }
    }
}
