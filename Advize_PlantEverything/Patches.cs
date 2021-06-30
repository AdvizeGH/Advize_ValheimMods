using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
                            }
                            __instance.FaceLookDirection();
                            ___m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
                            __result = true;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Piece), "SetCreator")]
        public static class PieceSetCreator
        {
            public static void Postfix(Piece __instance)
            {
                if (__instance.m_name.StartsWith("$pe") || __instance.m_name.EndsWith("_sapling"))
                {
                    if (config.ResourcesSpawnEmpty && __instance.m_name.Contains("berryBush"))
                    {
                        __instance.GetComponent<ZNetView>().InvokeRPC(ZNetView.Everybody, "SetPicked", true);
                    }

                    if (config.PlaceAnywhere)
                    {
                        StaticPhysics sp = __instance.GetComponent<StaticPhysics>();
                        if (sp)
                        {
                            sp.m_fall = false;
                            __instance.GetComponent<ZNetView>().GetZDO().Set("pe_placeAnywhere", true);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Piece), "Awake")]
        public static class PieceAwake
        {
            public static void Postfix(Piece __instance)
            {
                CheckZDO(__instance);
            }
        }

        [HarmonyPatch(typeof(TreeBase), "Awake")]
        public static class TreeBaseAwake
        {
            public static void Postfix(TreeBase __instance)
            {
                CheckZDO(__instance);
            }
        }

        public static void CheckZDO(Component instance)
        {
            ZNetView nview = instance.GetComponent<ZNetView>();
            if (!nview || nview.GetZDO() == null) return;

            if (nview.GetZDO().GetBool("pe_placeAnywhere"))
            {
                instance.GetComponent<StaticPhysics>().m_fall = false;
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

        [HarmonyPatch(typeof(Plant), "HaveRoof")]
        public static class PlantHaveRoof
        {
            public static bool Prefix(Plant __instance, ref bool __result)
            {
                if (config.PlaceAnywhere && (__instance.m_name.StartsWith("$pe") || __instance.m_name.EndsWith("_sapling")))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Plant), "HaveGrowSpace")]
        public static class PlantHaveGrowSpace
        {
            public static bool Prefix(Plant __instance, ref bool __result)
            {
                if (config.PlaceAnywhere && (__instance.m_name.StartsWith("$pe") || __instance.m_name.EndsWith("_sapling")))
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Plant), "Grow")]
        public static class PlantGrow
        {
            private static readonly MethodInfo ModifyGrowMethod = AccessTools.Method(typeof(PlantGrow), nameof(ModifyGrow));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                .MatchForward(true, new CodeMatch(OpCodes.Stloc_2))
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt))
                .Advance(-1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, ModifyGrowMethod))
                .InstructionEnumeration();
            }

            private static void ModifyGrow(Plant plant, TreeBase treeBase)
            {
                if (plant.GetComponent<ZNetView>().GetZDO().GetBool("pe_placeAnywhere"))
                {
                    treeBase.GetComponent<StaticPhysics>().m_fall = false;
                    treeBase.GetComponent<ZNetView>().GetZDO().Set("pe_placeAnywhere", true);
                }
            }
        }
    }
}
