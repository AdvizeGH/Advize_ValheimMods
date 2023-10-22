using System;
using System.Collections.Generic;
using System.Linq;
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
            public static void Postfix()
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

        [HarmonyPatch(typeof(Player), nameof(Player.CheckCanRemovePiece))]
        public static class PlayerCheckCanRemovePiece
        {
            private static bool Prefix(Player __instance, Piece piece, ref bool __result)
            {
                // check if the piece exists and if the mod has modified it
                if (piece && pieceRefs.Any(x => x.piece.m_name == piece.m_name))
                {
                    // is piece from mod, so prevent deconstruction
                    // unless it is with the cultivator.
                    if (__instance.GetRightItem().m_shared.m_name != "$item_cultivator")
                    {
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
        public static class PieceDropResources
        {
            internal static void Prefix(
                Piece __instance,
                out Piece.Requirement[] __state
            )
            {
                __state = null;

                // Only interact if it is a piece that is modified by the mod
                if (__instance && pieceRefs.Any(x => x.piece.m_name == __instance.m_name))
                {
                    // If piece has a pickable component then adjust resource drops
                    // to prevent infinite item exploits by placing a pickable,
                    // picking it, and then deconstructing it to get extra items.
                    __state = __instance.m_resources;
                    __instance.m_resources = RemovePickableDropFromRequirements(
                        __instance.m_resources,
                        __instance.GetComponent<Pickable>()
                    );
                }
            }

            internal static void Postfix(Piece __instance, Piece.Requirement[] __state)
            {
                if (__state != null)
                {
                    // Restore resources if they were changed
                    __instance.m_resources = __state;
                }
            }

            private static Piece.Requirement[] RemovePickableDropFromRequirements(
                Piece.Requirement[] requirements,
                Pickable pickable
            )
            {
                // Pickables from this mod drop the pickable when deconstructed so
                // it doesn't matter if it's been picked or not.
                var pickableDrop = pickable?.m_itemPrefab?.GetComponent<ItemDrop>()?.m_itemData;
                if (requirements == null || pickable == null || pickableDrop == null)
                {
                    return requirements;
                }

                // Check if pickable is included in piece build requirements
                for (int i = 0; i < requirements.Length; i++)
                {
                    var req = requirements[i];
                    if (req.m_resItem.m_itemData.m_shared.m_name == pickableDrop.m_shared.m_name)
                    {
                        // Make a copy before altering drops
                        var pickedRequirements = new Piece.Requirement[requirements.Length];
                        requirements.CopyTo(pickedRequirements, 0);

                        // Get amount returned on picking based on world modifiers
                        var pickedAmount = GetScaledPickableDropAmount(pickable);

                        // Reduce drops by the amount that picking the item gave.
                        // This is to prevent infinite resource exploits.
                        pickedRequirements[i].m_amount = Mathf.Clamp(req.m_amount - pickedAmount, 0, req.m_amount);
                        return pickedRequirements;
                    }
                }

                // If no pickable item, return the requirements array unchanged.
                return requirements;
            }

            private static int GetScaledPickableDropAmount(Pickable pickable)
            {
                if (Game.instance == null)
                {
                    return pickable.m_amount;
                }
                return pickable.m_dontScale ? pickable.m_amount : Mathf.Max(pickable.m_minAmountScaled, Game.instance.ScaleDrops(pickable.m_itemPrefab, pickable.m_amount));
            }
        }

        [HarmonyPatch(typeof(Player), "RemovePiece")]
        public static class PlayerRemovePiece
        {
            public static bool Prefix(Player __instance, ref bool __result)
            {
                if (__instance.GetRightItem().m_shared.m_name == "$item_cultivator")
                {
                    if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, LayerMask.GetMask(layersForPieceRemoval)) && Vector3.Distance(hitInfo.point, __instance.m_eye.position) < __instance.m_maxPlaceDistance)
                    {
                        Piece piece = hitInfo.collider.GetComponentInParent<Piece>();
                        if (piece && pieceRefs.Any(x => x.piece.m_name == piece.m_name))
                        {
                            if (!CanRemove(piece.gameObject, __instance, true)) return false;

                            RemoveObject(piece.gameObject, __instance);
                            __result = true;
                        }
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
                if (!component.GetComponent<ZNetView>()) canRemove = false;
                return canRemove;
            }

            private static void RemoveObject(GameObject component, Player player)
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
                    Piece piece = component.GetComponent<Piece>();
                    piece.DropResources();
                    piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation);
                    if (component.GetComponentInParent<Pickable>())
                    {
                        component2.InvokeRPC("Pick");
                    }
                    ZNetScene.instance.Destroy(component.gameObject);
                }
                player.FaceLookDirection();
                player.m_zanim.SetTrigger(player.GetRightItem().m_shared.m_attack.m_attackAnimation);
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
                    __instance.GetComponentInParent<Piece>().m_cultivatedGroundOnly = config.CropRequireCultivation;
                }
            }
        }

        [HarmonyPatch(typeof(Plant), "HaveRoof")]
        public static class PlantHaveRoof
        {
            public static bool Prefix(Plant __instance, ref bool __result)
            {
                if (!config.CropRequireSunlight && __instance.m_name.StartsWith("$piece_sapling"))
                {
                    __result = false;
                    return false;
                }

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
                if (!config.CropRequireGrowthSpace && __instance.m_name.StartsWith("$piece_sapling"))
                {
                    __result = true;
                    return false;
                }

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
                .MatchForward(true, new CodeMatch(OpCodes.Stloc_3))
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt))
                .Advance(-1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_3))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, ModifyGrowMethod))
                .InstructionEnumeration();
            }

            private static void ModifyGrow(Plant plant, TreeBase treeBase)
            {
                if (!plant?.GetComponent<ZNetView>() || !treeBase)
                {
                    Dbgl("ModifyGrow not executed, a reference is null", true, level: BepInEx.Logging.LogLevel.Error);
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
            public static void Postfix(Pickable __instance, ref string __result)
            {
                if (__instance.m_picked && config.EnablePickableTimers && __instance.m_nview.GetZDO() != null)
                {
                    if (__instance.name.ToLower().Contains("core"))
                        return;

                    float growthTime = __instance.m_respawnTimeMinutes * 60;
                    DateTime pickedTime = new(__instance.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L));
                    string timeString = FormatTimeString(growthTime, pickedTime);

                    __result = Localization.instance.Localize(__instance.GetHoverName()) + $"\n{timeString}";
                }
            }
        }

        [HarmonyPatch(typeof(Plant), "GetHoverText")]
        public static class PlantGetHoverText
        {
            [HarmonyPostfix]
            public static void Postfix(Plant __instance, ref string __result)
            {
                if (config.EnablePlantTimers && __instance.m_status == 0 && __instance.m_nview.GetZDO() != null)
                {
                    float growthTime = __instance.GetGrowTime();
                    DateTime plantTime = new(__instance.m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks));
                    string timeString = FormatTimeString(growthTime, plantTime);

                    __result += $"\n{timeString}";
                }
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
                color = "#00FFFF"; // cyan
            else if (remainingRatio < 0.25)
                color = "#32CD32"; // lime
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

        //Replace this... possibly with separate config option and hopefully with different entry point and implementation
        //[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        //public class PlayerUpdatePlacementGhost
        //{
        //    public static void Postfix(ref GameObject ___m_placementGhost)
        //    {
        //        if (!config.CropRequireCultivation && ___m_placementGhost && ___m_placementGhost.GetComponent<Plant>())
        //        {
        //            ___m_placementGhost.GetComponent<Piece>().m_groundOnly = false;
        //        }
        //    }
        //}
    }
}