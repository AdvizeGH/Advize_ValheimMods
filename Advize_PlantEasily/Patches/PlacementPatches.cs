namespace Advize_PlantEasily;

using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using static ModContext;
using static ModUtils;

static class PlacementPatches
{
    [HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
    static class PlayerSetupPlacementGhost
    {
        private static int placementRotation;

        static void Prefix(Player __instance)
        {
            if (!config.ModActive || !HoldingCultivator)
                return;

            placementRotation = __instance.m_placeRotation;
        }

        static void Postfix(Player __instance)
        {
            //Dbgl("SetupPlacementGhost");
            if (PlacementController.IsPlanting)
                return;

            GhostGrid.PrepareGhostPool(__instance.m_placementGhost);

            if (!config.ModActive || !__instance.m_placementGhost || !HoldingCultivator || !IsPlantOrPickable(__instance.m_placementGhost))
                return;
            //Dbgl("SetupPlacementGhost2");
            __instance.m_placeRotation = placementRotation;

            GhostGrid.BuildGrid(__instance.m_placementGhost);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    static class PlayerUpdatePlacementGhost
    {
        static void Prefix(Player __instance)
        {
            if (!config.ModActive || !PlacementState.PlacementGhost)
                return;

            //If there are no extra ghosts but there is supposed to be
            if (GhostGrid.GhostPlacementStatus.Count == 0 || (GhostGrid.ExtraGhosts.Count == 0 && !(config.Rows == 1 && config.Columns == 1)))
            {
                //Dbgl($"Calling Setup from Update. placementCount:{GhostGrid.GhostPlacementStatus.Count}, ghostCount is 0? ({GhostGrid.ExtraGhosts.Count == 0})");
                __instance.SetupPlacementGhost();
            }
        }

        static void Postfix(Player __instance)
        {
            if (!config.ModActive || !PlacementState.PlacementGhost || PlacementController.IsPlanting)
                return;

            PlacementState.Update(__instance.transform.position);
            GhostGrid.Update(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    static class PlayerTryPlacePiece
    {
        private static readonly Dictionary<Status, string> StatusMessages = new()
        {
            { Status.LackResources, "$msg_missingrequirement" },
            { Status.NotCultivated, "$piece_plant_notcultivated" },
            { Status.WrongBiome, "$piece_plant_wrongbiome" },
            { Status.NoSpace, "$piece_plant_nospace" },
            { Status.NoSun, "$piece_plant_nosun" },
            { Status.Invalid, "$msg_invalidplacement" },
            { Status.NoAttachPiece, "$piece_plant_nowall" },
            { Status.TooHot, "$piece_plant_toohot" },
            { Status.TooCold, "$piece_plant_toocold" }
        };

        static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (!config.ModActive || !piece || !HoldingCultivator || !IsPlantOrPickable(piece.gameObject))
                return true;

            if (PlacementController.IsPlanting)
            {
                __result = false;
                return false;
            }

            if (config.PreventInvalidPlanting)
            {
                Status rootStatus = GhostStatus.EvaluateStatus(__instance.m_placementGhost);

                if (rootStatus != Status.Healthy)
                    return PreventPlacement(__instance, rootStatus, ref __result);
            }

            if (config.PreventPartialPlanting)
            {
                foreach (Status status in GhostGrid.GhostPlacementStatus)
                {
                    if (status == Status.Healthy)
                        continue;

                    if (status == Status.LackResources && __instance.m_noPlacementCost)
                        continue;

                    return PreventPlacement(__instance, status, ref __result);
                }
            }
            return true;
        }

        private static bool PreventPlacement(Player player, Status status, ref bool result)
        {
            if (StatusMessages.TryGetValue(status, out string message))
                player.Message(MessageHud.MessageType.Center, message);

            result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    static class PlayerPlacePiece
    {
        private static int placementRotation;

        static void Prefix(Player __instance) => placementRotation = __instance.m_placeRotation;

        static void Postfix(Player __instance, Piece piece)
        {
            if (!config.ModActive || !piece || !HoldingCultivator || !IsPlantOrPickable(piece.gameObject))
                return;

            __instance.m_placeRotation = placementRotation;

            int MaxActiveGhosts = Mathf.Min(config.Rows * config.Columns - 1, config.MaxConcurrentPlacements - 1);
            if (MaxActiveGhosts < 1)
                return;

            for (int i = 0; i < MaxActiveGhosts; i++)
            {
                Status status = GhostGrid.GhostPlacementStatus[i + 1];

                bool isHealthy = status == Status.Healthy;
                bool lackResourcesAllowed = status == Status.LackResources && __instance.m_noPlacementCost;
                bool invalidAllowed = !config.PreventInvalidPlanting && status > Status.LackResources;

                bool canPlant = isHealthy || lackResourcesAllowed || invalidAllowed;

                if (canPlant)
                    GhostGrid.ValidExtraGhosts.Add(GhostGrid.ExtraGhosts[i]);
            }

            int placementMultiplier = __instance.m_noPlacementCost ? 0 : GhostGrid.ValidExtraGhosts.Count;

            if (GhostGrid.ValidExtraGhosts.Count > 0)
            {
                GameObject piecePrefab = __instance.m_buildPieces.GetSelectedPiece().gameObject;

                PlacementController.Instance.StartBulkPlanting(piecePrefab);
            }

            ItemDrop.ItemData rightItem = __instance.GetRightItem();

            for (int i = 0; i < placementMultiplier; i++)
            {
                __instance.ConsumeResources(piece.m_resources, 0);

                if (config.UseStamina)
                    __instance.UseStamina(__instance.GetBuildStamina());

                if (config.UseDurability && rightItem.m_shared.m_useDurability)
                    rightItem.m_durability -= __instance.GetPlaceDurability(rightItem);
            }
        }
    }
}
