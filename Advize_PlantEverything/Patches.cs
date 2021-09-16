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
                    List<GameObject> prefabs = new()
                    {
                        //prefabRefs["BirchCone"],
                        //prefabRefs["OakSeeds"],
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
                    List<GameObject> prefabs = new()
                    {
                        //prefabRefs["BirchCone"],
                        //prefabRefs["OakSeeds"],
                        prefabRefs["AncientSeeds"],
                        //prefabRefs["Birch_Sapling"],
                        //prefabRefs["Oak_Sapling"],
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
                if (__instance.GetRightItem().m_shared.m_name == "$item_cultivator")
                {
                    if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, LayerMask.GetMask("item", "piece_nonsolid", "Default_small", "Default")) && Vector3.Distance(hitInfo.point, __instance.m_eye.position) < __instance.m_maxPlaceDistance)
                    {
                        do
                        {
                            Piece piece = hitInfo.collider.GetComponentInParent<Piece>();
                            if (piece && piece.m_name.StartsWith("$pe"))
                            {
                                if (!CanRemove(piece.gameObject, __instance, true)) break;

                                RemoveObject(piece.gameObject, __instance, ___m_zanim, true);
                                __result = true;
                                break;
                            }

                            Pickable pickable = hitInfo.collider.GetComponentInParent<Pickable>();
                            if (pickable && (pickable.name.Contains("Branch") || pickable.name.Contains("Stone") || pickable.name.Contains("Flint")))
                            {
                                if (!CanRemove(pickable.gameObject, __instance, false)) break;

                                RemoveObject(pickable.gameObject, __instance, ___m_zanim, false);
                                __result = true;
                                break;
                            }
                        } while (false);
                    }
                    return false;
                }
                return true;
            }
            
            private static bool CanRemove(GameObject component, Player instance, bool isPiece)
            {
                bool canRemove = true;
                if (isPiece)
                {
                    if (!component.GetComponent<Piece>().m_canBeRemoved) canRemove = false;
                }
                if (!PrivateArea.CheckAccess(component.transform.position))
                {
                    instance.Message(MessageHud.MessageType.Center, "$msg_privatezone");
                    canRemove = false;
                }
                if (component.GetComponent<ZNetView>() == null) canRemove = false;
                return canRemove;
            }

            private static void RemoveObject(GameObject component, Player player, ZSyncAnimation m_zanim, bool isPiece)
            {
                ZNetView component2 = component.GetComponent<ZNetView>();
                WearNTear component3 = component.GetComponent<WearNTear>();
                if (component3)
                {
                    player.m_removeEffects.Create(component.transform.position, Quaternion.identity);
                    component3.Remove();
                }
                else
                {
                    component2.ClaimOwnership();
                    if (isPiece)
                    {
                        Piece piece = component.GetComponent<Piece>();
                        piece.DropResources();
                        piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation);
                    }
                    else
                    {
                        component2.InvokeRPC("Pick"); ;
                    }
                    player.m_removeEffects.Create(component.transform.position, Quaternion.identity);
                    ZNetScene.instance.Destroy(component.gameObject);
                }
                player.FaceLookDirection();
                m_zanim.SetTrigger(player.GetRightItem().m_shared.m_attack.m_attackAnimation);
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

        [HarmonyPatch(typeof(Pickable), "GetHoverText")]
        public static class PickableGetHoverText
        {
            [HarmonyPostfix]
            public static void Postfix(Pickable __instance, bool ___m_picked, ZNetView ___m_nview, int ___m_respawnTimeMinutes, ref string __result)
            {
                if (___m_picked && config.EnablePickableTimers && ___m_nview.GetZDO() != null)
                {
                    if (__instance.name.ToLower().Contains("surt"))
                        return;

                    DateTime pickedTime = new(___m_nview.GetZDO().GetLong("picked_time", 0L));
                    TimeSpan difference = ZNet.instance.GetTime() - pickedTime;
                    TimeSpan t = TimeSpan.FromSeconds(((float)___m_respawnTimeMinutes * 60) - difference.TotalSeconds);

                    double remainingMinutes = ___m_respawnTimeMinutes - difference.TotalMinutes;
                    double remainingRatio = remainingMinutes / (float)___m_respawnTimeMinutes;

                    string color = "red";
                    if (remainingRatio < 0)
                        color = "cyan";
                    else if (remainingRatio < 0.25)
                        color = "lime";
                    else if (remainingRatio < 0.5)
                        color = "yellow";
                    else if (remainingRatio < 0.75)
                        color = "orange";

                    string timeRemaining = t.Hours <= 0 ? t.Minutes <= 0 ?
                    $"{t.Seconds:D2}s" : $"{t.Minutes:D2}m {t.Seconds:D2}s" : $"{t.Hours:D2}h {t.Minutes:D2}m {t.Seconds:D2}s";
                    string message = remainingMinutes < 0.0 ? $"\n(<color={color}>Ready any second now</color>)" : $"\n(Ready in <color={color}>{timeRemaining}</color>)";

                    __result = Localization.instance.Localize(__instance.GetHoverName() + message);
                }
            }
        }
    }
}
