namespace Advize_PlantEasily;

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PlantEasily;

static class PlacementPatches
{
    [HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
    static class PlayerSetupPlacementGhost
    {
        static int placementRotation;
        static void Prefix()
        {
            if (!config.ModActive || !HoldingCultivator)
                return;

            placementRotation = Player.m_localPlayer.m_placeRotation;
        }

        static void Postfix(GameObject ___m_placementGhost, ref int ___m_placeRotation)
        {
            if (isPlanting) return;
            DestroyGhosts();

            if (!config.ModActive || !___m_placementGhost || !HoldingCultivator || !IsPlantOrPickable(___m_placementGhost))
                return;

            ___m_placeRotation = placementRotation;
            CreateGhosts(___m_placementGhost);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    static class PlayerUpdatePlacementGhost
    {
        static readonly Collider[] primaryObstructions = new Collider[50];
        static readonly Collider[] secondaryObstructions = new Collider[50];

        static Vector3 basePosition = Vector3.zero;
        static Vector3 rowDirection = Vector3.zero;
        static Vector3 columnDirection = Vector3.zero;
        static Quaternion baseRotation = Quaternion.identity;
        static Quaternion fixedRotation = Quaternion.identity;

        static bool altPlacement;

        sealed private class SnapPoint(Vector3 p, Vector3 rd, Vector3 cd)
        {
            internal Vector3 pos = p;
            internal Vector3 rowDir = rd;
            internal Vector3 colDir = cd;
        }

        static void UpdateGhosts(Vector3 playerPosition)
        {
            altPlacement = config.ForceAltPlacement || ((ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive()) ? Player.m_localPlayer.m_altPlace : (ZInput.GetButton("AltPlace") || (ZInput.GetButton("JoyAltPlace") && !ZInput.GetButton("JoyRotate"))));

            void ResetGridDirections()
            {
                // Subtracts player position from placement ghost position to get a vector between the two and facing out from the player, normalizes it
                rowDirection = config.GloballyAlignGridDirections ? Vector3.forward : Utils.DirectionXZ(basePosition - playerPosition);
                // Cross product of a vertical vector and the forward facing normalized vector, producing a perpendicular lateral vector
                columnDirection = Vector3.Cross(Vector3.up, rowDirection);
            }

            basePosition = placementGhost.transform.position;
            baseRotation = fixedRotation = placementGhost.transform.rotation;

            Vector3 vec = fixedRotation.eulerAngles;
            vec.y = Mathf.Round(vec.y / 90) * 90;
            fixedRotation.eulerAngles = vec;

            float pieceSpacing = GetPieceSpacing(placementGhost);

            ResetGridDirections();

            if (!(config.SnapActive && FindSnapPoints(pieceSpacing)))
            {
                if (config.SnapActive)
                {
                    ResetGridDirections();
                }

                rowDirection = baseRotation * rowDirection * pieceSpacing;
                columnDirection = baseRotation * columnDirection * pieceSpacing;
            }
        }

        static bool FindSnapPoints(float pieceSpacing)
        {
            bool foundSnaps = false;
            bool gridFound = false;
            List<SnapPoint> snapPoints = [];
            Plant plant = placementGhost.GetComponent<Plant>();

            int primaryCollisions = Physics.OverlapSphereNonAlloc(basePosition, pieceSpacing, primaryObstructions, CollisionMask);
            int validFirstOrderCollisions = 0;

            for (int firstCollisionIndex = 0; firstCollisionIndex < primaryCollisions; firstCollisionIndex++)
            {
                if (gridFound && foundSnaps) break;
                Transform t1 = primaryObstructions[firstCollisionIndex].transform.root;

                if (!IsPlantOrPickable(t1.gameObject)) continue;
                if (++validFirstOrderCollisions > 8) break;

                float pieceSpacing2 = GetPieceSpacing(t1.gameObject);
                int secondaryCollisions = Physics.OverlapSphereNonAlloc(t1.position, pieceSpacing2, secondaryObstructions, CollisionMask);
                int validSecondOrderCollisions = 0;

                for (int secondCollisionIndex = 0; secondCollisionIndex < secondaryCollisions; secondCollisionIndex++)
                {
                    Transform t2 = secondaryObstructions[secondCollisionIndex].transform.root;
                    if (t2 == t1 || !IsPlantOrPickable(t2.gameObject)) continue;
                    if (++validSecondOrderCollisions > 8) break;
                    if (!(Math.Round(Utils.DistanceXZ(t2.position, t1.position), 2) <= Math.Round(pieceSpacing2, 2))) continue;

                    gridFound = true;

                    rowDirection = fixedRotation * Utils.DirectionXZ(t1.position - t2.position) * pieceSpacing;
                    columnDirection = Vector3.Cross(Vector3.up, rowDirection);
                    foundSnaps = ValidateSnapPoints(snapPoints, t1.position, plant, gridDetected: true);
                    break;
                }

                if (!gridFound && !foundSnaps)
                {
                    rowDirection = Utils.DirectionXZ(basePosition - t1.position) * pieceSpacing;

                    if (!altPlacement)
                    {
                        //Find the nearest angle that is a valid 22.5 degree rotation
                        float nearestRotationAngle = Mathf.Round(Vector3.Angle(Vector3.forward, rowDirection) / 22.5f) * 22.5f;
                        //Find out if the rowDirection points to the left of Vector3.forward (left 2 quadrants)
                        if (Vector3.Angle(Vector3.right, rowDirection) > 90)
                        {
                            nearestRotationAngle = -nearestRotationAngle;
                        }
                        
                        rowDirection = fixedRotation * Quaternion.Euler(0, nearestRotationAngle, 0) * Vector3.forward * pieceSpacing;
                    }

                    columnDirection = Vector3.Cross(Vector3.up, rowDirection);
                    foundSnaps = ValidateSnapPoints(snapPoints, t1.position, plant, gridDetected: false);
                }
            }

            if (foundSnaps)
            {
                //Dbgl($"Found {snapPoints.Count} snap points");
                SnapPoint nearestSnapPoint = snapPoints.OrderBy(o => (o.pos - basePosition).sqrMagnitude).First();
                basePosition = placementGhost.transform.position = nearestSnapPoint.pos;
                if (config.GridSnappingStyle == 0)
                {
                    rowDirection = nearestSnapPoint.rowDir;
                    columnDirection = nearestSnapPoint.colDir;
                }

                return true;
            }

            return false;
        }

        static bool ValidateSnapPoints(List<SnapPoint> snapPoints, Vector3 snapFromPos, Plant plant, bool gridDetected)
        {
            List<Vector3> potentialPositions = [];
            int maxRotations = 16/*config.MaxRotations??*/;

            if (gridDetected)
            {
                potentialPositions.Add(snapFromPos + rowDirection);
                potentialPositions.Add(snapFromPos - rowDirection);
                potentialPositions.Add(snapFromPos + columnDirection);
                potentialPositions.Add(snapFromPos - columnDirection);

                potentialPositions.Add(snapFromPos + rowDirection - columnDirection);
                potentialPositions.Add(snapFromPos + rowDirection + columnDirection);
                potentialPositions.Add(snapFromPos - rowDirection - columnDirection);
                potentialPositions.Add(snapFromPos - rowDirection + columnDirection);
            }
            else
            {
                if (altPlacement)
                {
                    potentialPositions.Add(snapFromPos + rowDirection);
                    potentialPositions.Add(snapFromPos - rowDirection);
                }
                else
                {
                    for (int r = 0; r < maxRotations; r++)
                    {
                        potentialPositions.Add(snapFromPos + Quaternion.Euler(0, (360f / maxRotations) * r, 0) * rowDirection);
                    }
                }
            }

            foreach (Vector3 pos in potentialPositions)
            {
                bool invertRowDirection = false;
                bool invertColumnDirection = false;

                if (!PositionHasCollisions(pos))
                {
                    if (plant && !HasGrowSpace(plant, pos)) continue;

                    if (config.GridSnappingStyle == 0)
                    {
                        invertRowDirection = config.Rows > 1 && PositionHasCollisions(pos + rowDirection);
                        invertColumnDirection = config.Columns > 1 && PositionHasCollisions(pos + columnDirection);
                    }

                    snapPoints.Add(new SnapPoint(pos, !invertRowDirection ? rowDirection : -rowDirection, !invertColumnDirection ? columnDirection : -columnDirection));
                }
            }
            return snapPoints.Count > 0;
        }

        static void CreateGrid(Player player)
        {
            Piece piece = placementGhost.GetComponent<Piece>();
            int cost = piece.m_resources[0].m_amount;
            int currentCost = 0;

            if (config.ShowGridDirections)
            {
                gridRenderer.SetActive(placementGhost.activeSelf);
                Vector3 vertex = basePosition + (Vector3.up / 2);
                lineRenderers[0].SetPositions([vertex, vertex + (rowDirection * (config.Rows - 1))]);
                lineRenderers[1].SetPositions([vertex, vertex + (columnDirection * (config.Columns - 1))]);
            }

            for (int row = 0; row < config.Rows; row++)
            {
                for (int column = 0; column < config.Columns; column++)
                {
                    int ghostIndex = row * config.Columns + column;

                    if (ghostIndex > extraGhosts.Count) break;

                    currentCost += cost;
                    piece.m_resources[0].m_amount = currentCost;

                    GameObject ghost = ghostIndex == 0 ? placementGhost : extraGhosts[ghostIndex - 1];
                    Vector3 ghostPosition = ghostIndex == 0 ? basePosition : basePosition + rowDirection * row + columnDirection * column;

                    Heightmap.GetHeight(ghostPosition, out float height);
                    ghostPosition.y = height;

                    ghost.transform.position = ghostPosition;
                    ghost.transform.rotation = placementGhost.transform.rotation;

                    Status status = Status.Healthy;
                    if (!player.m_noPlacementCost && !player.HaveRequirements(piece, Player.RequirementMode.CanBuild))
                        status = Status.LackResources;

                    SetPlacementGhostStatus(ghost, ghostIndex, CheckPlacementStatus(ghost, status));
                }
            }

            piece.m_resources[0].m_amount = cost;
        }

        static void Prefix(Player __instance)
        {
            if (!config.ModActive || !placementGhost)
                return;

            //If there are no extra ghosts but there is supposed to be
            if (ghostPlacementStatus.Count == 0 || (extraGhosts.Count == 0 && !(config.Rows == 1 && config.Columns == 1)))
                __instance.SetupPlacementGhost();
        }

        static void Postfix(Player __instance)
        {
            if (!config.ModActive || !placementGhost || isPlanting)
                return;

            for (int i = 0; i < extraGhosts.Count; i++)
            {
                GameObject extraGhost = extraGhosts[i];
                extraGhost.SetActive(placementGhost.activeSelf);
            }

            UpdateGhosts(__instance.transform.position);
            CreateGrid(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    static class PlayerTryPlacePiece
    {
        static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (!config.ModActive || !piece || !HoldingCultivator || !IsPlantOrPickable(piece.gameObject))
                return true;

            if (config.PreventInvalidPlanting)
            {
                int rootPlacementStatus = (int)CheckPlacementStatus(__instance.m_placementGhost);
                if (rootPlacementStatus > 1)
                {
                    __instance.Message(MessageHud.MessageType.Center, statusMessage[rootPlacementStatus]);
                    return __result = false;
                }
            }

            if (config.PreventPartialPlanting)
            {
                foreach (int i in ghostPlacementStatus.Where(i => i != 0 && !((int)i == 1 && __instance.m_noPlacementCost)).Select(v => (int)v))
                {
                    __instance.Message(MessageHud.MessageType.Center, statusMessage[i]);
                    return __result = false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    static class PlayerPlacePiece
    {
        static int placementRotation;

        static void Prefix(Player __instance) => placementRotation = __instance.m_placeRotation;

        static void Postfix(Player __instance, Piece piece)
        {
            if (!config.ModActive || !piece || !HoldingCultivator || !IsPlantOrPickable(piece.gameObject))
                return;
            //This doesn't apply to the root placement ghost.
            __instance.m_placeRotation = placementRotation;
            ItemDrop.ItemData rightItem = __instance.GetRightItem();
            int count = extraGhosts.Count;

            for (int i = 0; i < extraGhosts.Count; i++)
            {
                if (ghostPlacementStatus[i + 1] != Status.Healthy)
                {
                    bool canPlant = (ghostPlacementStatus[i + 1] == Status.LackResources && __instance.m_noPlacementCost) || (!config.PreventInvalidPlanting && (int)ghostPlacementStatus[i + 1] > 1);
                    if (!canPlant)
                    {
                        count--;
                        continue;
                    }
                }
                currentValidGhosts.Add(extraGhosts[i]);
            }

            pluginInstance.StartCoroutine("BulkPlanting", piece.gameObject);
            count = __instance.m_noPlacementCost ? 0 : count;

            for (int i = 0; i < count; i++)
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
