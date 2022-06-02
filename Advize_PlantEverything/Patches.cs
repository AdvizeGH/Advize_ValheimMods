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

        [HarmonyPatch(typeof(ZNetScene), "GetPrefab", new Type[] { typeof(int) })]
        public static class ZNetSceneGetPrefab
        {
            public static void Postfix(int hash, ref GameObject __result, ZNetScene __instance)
            {
                if (__result == null)
                {
                    if (hash == __instance.GetPrefabHash(prefabRefs["Ancient_Sapling"]))
                        __result = prefabRefs["Ancient_Sapling"];
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
                    if (config.ResourcesSpawnEmpty && (__instance.m_name.Contains("berryBush") || (__instance.m_name.Contains("Pickable") && !__instance.m_name.Contains("Stone"))))
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
                if (!plant || !treeBase)
                {
                    Dbgl("ModifyGrow not executed, reference is null", logError : true);
                    return;
                }
                if (!plant.GetComponent<ZNetView>())
                {
                    Dbgl("ModifyGrow not executed, ZNetView component reference is null", logError: true);
                    return;
                }
                if (plant.GetComponent<ZNetView>().GetZDO().GetBool("pe_placeAnywhere") && treeBase.GetComponent<StaticPhysics>())
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

                    float growthTime = ___m_respawnTimeMinutes * 60;
                    DateTime pickedTime = new(___m_nview.GetZDO().GetLong("picked_time", 0L));
                    string timeString = FormatTimeString(growthTime, pickedTime);

                    __result = Localization.instance.Localize(__instance.GetHoverName()) + $"\n{timeString}";
                }
            }
        }

        [HarmonyPatch(typeof(Plant), "GetHoverText")]
        public static class PlantGetHoverText
        {
            [HarmonyPostfix]
            public static void Postfix(Plant __instance, ZNetView ___m_nview, int ___m_status, ref string __result)
            {
                if (config.EnablePlantTimers && ___m_status == 0)
                {
                    float growthTime = GetGrowTime(__instance, ___m_nview);
                    DateTime plantTime = new(___m_nview.GetZDO().GetLong("plantTime", ZNet.instance.GetTime().Ticks));
                    string timeString = FormatTimeString(growthTime, plantTime);

                    __result += $"\n{timeString}";
                }
            }

            public static float GetGrowTime(Plant plant, ZNetView m_nview)
            {
                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState((int)(m_nview.GetZDO().m_uid.id + m_nview.GetZDO().m_uid.userID));
                float value = UnityEngine.Random.value;
                UnityEngine.Random.state = state;
                return Mathf.Lerp(plant.m_growTime, plant.m_growTimeMax, value);
            }
        }

        public static string FormatTimeString(float growthTime, DateTime placedTime)
        {
            TimeSpan timeSincePlaced = ZNet.instance.GetTime() - placedTime;
            TimeSpan t = TimeSpan.FromSeconds(growthTime - timeSincePlaced.TotalSeconds);

            double remainingMinutes = (growthTime / 60) - timeSincePlaced.TotalMinutes;
            double remainingRatio = remainingMinutes / (growthTime / 60);
            int growthPercentage = Math.Min((int)((timeSincePlaced.TotalSeconds * 100) / growthTime), 100);

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

            string formattedString = config.GrowthAsPercentage ?
                $"(<color={color}>{growthPercentage}%</color>)" : remainingMinutes < 0.0 ?
                $"(<color={color}>Ready any second now</color>)" : $"(Ready in <color={color}>{timeRemaining}</color>)";

            return formattedString;
        }
    }
}
