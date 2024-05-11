namespace Advize_PlantEasily;

using HarmonyLib;
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
            //Dbgl("SetupPlacementGhost");
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
        sealed class SnapPosition
        {
            internal Vector3 rowDirection;
            internal Vector3 columnDirection;
            internal Vector3 position;

            internal SnapPosition(Vector3 rd, Vector3 cd, Vector3 p)
            {
                rowDirection = rd;
                columnDirection = cd;
                position = p;
            }
        }

        static bool FindSnapPoints(List<SnapPosition> snapPoints, Collider collider, Vector3 rowDirection, Vector3 columnDirection, Plant plant)
        {
            Vector3[] positions =
            [
                    collider.transform.position + rowDirection,
                    collider.transform.position + columnDirection,
                    collider.transform.position - rowDirection,
                    collider.transform.position - columnDirection
            ];

            foreach (Vector3 pos in positions)
            {
                bool invertRowDirection = false;
                bool invertColumnDirection = false;

                if (!PositionHasCollisions(pos))
                {
                    if (plant && !HasGrowSpace(plant, pos)) continue;

                    if (config.GridSnappingStyle == 0)
                    {
                        if (config.Rows > 1 && PositionHasCollisions(pos + rowDirection))
                            invertRowDirection = true;
                        if (config.Columns > 1 && PositionHasCollisions(pos + columnDirection))
                            invertColumnDirection = true;
                    }

                    snapPoints.Add(new SnapPosition(!invertRowDirection ? rowDirection : -rowDirection, !invertColumnDirection ? columnDirection : -columnDirection, pos));
                }
            }

            return snapPoints.Count > 0;
        }

        static void Prefix(Player __instance, GameObject ___m_placementGhost)
        {
            if (!config.ModActive || !___m_placementGhost || !HoldingCultivator || !IsPlantOrPickable(___m_placementGhost))
                return;

            //If there are no extra ghosts but there is supposed to be
            if (ghostPlacementStatus.Count == 0 || (extraGhosts.Count == 0 && !(config.Rows == 1 && config.Columns == 1)))
                __instance.SetupPlacementGhost();
        }

        static void Postfix(Player __instance, ref GameObject ___m_placementGhost, ref int ___m_placementStatus)
        {
            if (!config.ModActive || !___m_placementGhost || !HoldingCultivator || !IsPlantOrPickable(___m_placementGhost))
                return;

            for (int i = 0; i < extraGhosts.Count; i++)
            {
                GameObject extraGhost = extraGhosts[i];
                extraGhost.SetActive(___m_placementGhost.activeSelf);
            }

            Vector3 basePosition = ___m_placementGhost.transform.position;
            Quaternion baseRotation, fixedRotation;
            baseRotation = fixedRotation = ___m_placementGhost.transform.rotation;

            if (config.StandardizeGridRotations)
            {
                Vector3 vec = fixedRotation.eulerAngles;
                vec.x = Mathf.Round(vec.x / 90) * 90;
                vec.y = Mathf.Round(vec.y / 90) * 90;
                vec.z = Mathf.Round(vec.z / 90) * 90;
                fixedRotation.eulerAngles = vec;
            }

            float pieceSpacing = GetPieceSpacing(___m_placementGhost);

            // Takes position of ghost, subtracts position of player to get vector between the two and facing out from the player, normalizes that vector to have a magnitude of 1.0f
            Vector3 rowDirection = config.GloballyAlignGridDirections ? Vector3.forward : Utils.DirectionXZ(basePosition - __instance.transform.position);
            // Cross product of a vertical vector and the forward facing normalized vector, producing a perpendicular lateral vector
            Vector3 columnDirection = Vector3.Cross(Vector3.up, rowDirection);

            bool foundSnaps = false;
            if (config.SnapActive)
            {
                List<SnapPosition> snapPoints = [];
                Plant plant = ___m_placementGhost.GetComponent<Plant>();

                Collider[] obstructions = Physics.OverlapSphere(___m_placementGhost.transform.position, pieceSpacing, CollisionMask);
                int validFirstOrderCollisions = 0;

                foreach (Collider collider in obstructions)
                {
                    if (!IsPlantOrPickable(collider.transform.root.gameObject)) continue;
                    validFirstOrderCollisions++;
                    if (validFirstOrderCollisions > 8) break;

                    Collider[] secondaryObstructions = Physics.OverlapSphere(collider.transform.position, pieceSpacing, CollisionMask);
                    int validSecondOrderCollisions = 0;

                    foreach (Collider secondaryCollider in secondaryObstructions)
                    {
                        if (!IsPlantOrPickable(secondaryCollider.transform.root.gameObject)) continue;
                        if (secondaryCollider.transform.root == collider.transform.root) continue;
                        validSecondOrderCollisions++;
                        if (validSecondOrderCollisions > 8) break;

                        rowDirection = Utils.DirectionXZ(secondaryCollider.transform.position - collider.transform.position);
                        columnDirection = Vector3.Cross(Vector3.up, rowDirection);

                        rowDirection = (config.StandardizeGridRotations ? fixedRotation : baseRotation) * rowDirection * pieceSpacing;
                        columnDirection = (config.StandardizeGridRotations ? fixedRotation : baseRotation) * columnDirection * pieceSpacing;

                        foundSnaps = FindSnapPoints(snapPoints, collider, rowDirection, columnDirection, plant);
                    }

                    if (!foundSnaps)
                    {
                        rowDirection = baseRotation * rowDirection * pieceSpacing;
                        columnDirection = baseRotation * columnDirection * pieceSpacing;

                        foundSnaps = FindSnapPoints(snapPoints, collider, rowDirection, columnDirection, plant);
                    }
                }

                if (foundSnaps)
                {
                    SnapPosition firstSnapPos = snapPoints.OrderBy(o => snapPoints.Min(m => Utils.DistanceXZ(m.position, o.position)) + (o.position - basePosition).magnitude).First();
                    basePosition = ___m_placementGhost.transform.position = firstSnapPos.position;
                    if (config.GridSnappingStyle == 0)
                    {
                        rowDirection = firstSnapPos.rowDirection;
                        columnDirection = firstSnapPos.columnDirection;
                    }
                }
            }

            if (!foundSnaps)
            {
                if (config.SnapActive)
                {
                    rowDirection = config.GloballyAlignGridDirections ? Vector3.forward : Utils.DirectionXZ(basePosition - __instance.transform.position);
                    columnDirection = Vector3.Cross(Vector3.up, rowDirection);
                }

                rowDirection = baseRotation * rowDirection * pieceSpacing;
                columnDirection = baseRotation * columnDirection * pieceSpacing;
            }

            Piece piece = ___m_placementGhost.GetComponent<Piece>();
            int cost = piece.m_resources[0].m_amount;
            int currentCost = 0;

            for (int row = 0; row < config.Rows; row++)
            {
                for (int column = 0; column < config.Columns; column++)
                {
                    currentCost += cost;
                    piece.m_resources[0].m_amount = currentCost;
                    bool isRoot = (column == 0 && row == 0);
                    int ghostIndex = row * config.Columns + column;
                    GameObject ghost = isRoot ? ___m_placementGhost : extraGhosts[ghostIndex - 1];

                    Vector3 ghostPosition = isRoot ? basePosition : basePosition + rowDirection * row + columnDirection * column;

                    Heightmap.GetHeight(ghostPosition, out float height);
                    ghostPosition.y = height;

                    ghost.transform.position = ghostPosition;
                    ghost.transform.rotation = ___m_placementGhost.transform.rotation;

                    Status status = Status.Healthy;
                    if (!__instance.m_noPlacementCost && !__instance.HaveRequirements(piece, Player.RequirementMode.CanBuild))
                        status = Status.LackResources;

                    SetPlacementGhostStatus(ghost, ghostIndex, CheckPlacementStatus(ghost, status), ref ___m_placementStatus);
                }
            }
            piece.m_resources[0].m_amount = cost;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    static class PlayerPlacePiece
    {
        static int placementRotation;

        static bool Prefix(Player __instance, Piece piece, ref bool __result, ref bool __state)
        {
            //Dbgl("Player.PlacePiece Prefix");
            if (!config.ModActive || !piece || !HoldingCultivator || !IsPlantOrPickable(piece.gameObject))
                return true;

            __state = true;
            placementRotation = __instance.m_placeRotation;

            if (config.PreventInvalidPlanting)
            {
                int rootPlacementStatus = (int)CheckPlacementStatus(__instance.m_placementGhost);
                if (rootPlacementStatus > 1)
                {
                    __instance.Message(MessageHud.MessageType.Center, statusMessage[rootPlacementStatus]);
                    return __state = __result = false;
                }
            }

            if (config.PreventPartialPlanting)
            {
                //foreach (int i in ghostPlacementStatus)
                //{
                //    if (i != 0 && !(i == 1 && __instance.m_noPlacementCost))
                //    {
                //        __instance.Message(MessageHud.MessageType.Center, statusMessage[i]);
                //        return __state = __result = false;
                //    }
                //}

                foreach (int i in ghostPlacementStatus.Where(i => i != 0 && !((int)i == 1 && __instance.m_noPlacementCost)).Select(v => (int)v))
                {
                    __instance.Message(MessageHud.MessageType.Center, statusMessage[i]);
                    return __state = __result = false;
                }
            }
            return true;
        }

        static void Postfix(Player __instance, Piece piece, bool __state)
        {
            //Dbgl("Player.PlacePiece Postfix" + $"\n __state is {__state}");
            if (!config.ModActive || !__state || !piece || !HoldingCultivator || !IsPlantOrPickable(piece.gameObject))
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

                PlacePiece(__instance, extraGhosts[i], piece);
            }

            count = __instance.m_noPlacementCost ? 0 : count;

            for (int i = 0; i < count; i++)
                __instance.ConsumeResources(piece.m_resources, 0);

            if (config.UseStamina)
                __instance.UseStamina(rightItem.m_shared.m_attack.m_attackStamina * count, true);

            if (rightItem.m_shared.m_useDurability && config.UseDurability)
                rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain * count;
        }
    }
}
