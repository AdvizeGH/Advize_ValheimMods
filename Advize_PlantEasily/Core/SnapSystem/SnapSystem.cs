namespace Advize_PlantEasily;

using System.Collections.Generic;
using UnityEngine;
using static ModContext;
using static ModUtils;
using static PlacementState;

internal static class SnapSystem
{
    private const int MaxValidCollisions = 8;

    private static SnapPoint _lastSnap;
    private static bool _hasLastSnap;
    private static Quaternion _lastGhostRotation;

    internal static bool FindSnapPoints(float pieceSpacing)
    {
        // Try grid snapping first
        if (TryGridSnaps(pieceSpacing, out List<SnapPoint> gridSnaps))
        {
            ApplyGridSnap(gridSnaps);
            return true;
        }

        // Fallback to free snapping
        if (TryFreeSnaps(pieceSpacing, out List<SnapPoint> freeSnaps))
        {
            ApplyFreeSnap(freeSnaps);
            return true;
        }

        SnapDirection = Vector3.zero;
        return false;
    }

    private static void ApplyGridSnap(List<SnapPoint> availableSnapPoints)
    {
        ApplyRotationHysteresis();

        (SnapPoint nearestSnap, float nearestGridDistance) = FindNearestManhattan(availableSnapPoints);

        nearestSnap = ApplyPositionHysteresis(nearestSnap, nearestGridDistance);

        CommitSnap(nearestSnap);

        ApplyGridOrientation(nearestSnap);
    }

    private static void ApplyFreeSnap(List<SnapPoint> availableSnapPoints)
    {
        SnapPoint nearestSnap = FindNearestEuclidean(availableSnapPoints);

        CommitSnap(nearestSnap);

        ApplyFreeOrientation(nearestSnap);
    }

    private static bool TryGridSnaps(float pieceSpacing, out List<SnapPoint> snapPoints)
    {
        snapPoints = [];
        float tolerance = Mathf.Max(pieceSpacing * 0.01f, 0.005f);
        float spacingRadius = pieceSpacing + tolerance;

        // Collect all valid primary collisions and their neighbours
        List<(Transform transform, float distance, List<Transform> neighbours)> primaries = [];

        foreach (Transform validPrimary in ValidPrimaryCollisions(BasePosition, spacingRadius))
        {
            float distance = (validPrimary.position - BasePosition).sqrMagnitude;
            float expectedSpacing = GetPieceSpacing(validPrimary.gameObject);

            List<Transform> neighbours = new(8);

            foreach (Transform secondary in CollisionScanner.ScanNeighbours(validPrimary, expectedSpacing))
            {
                if (IsPlantOrPickable(secondary.gameObject))
                    neighbours.Add(secondary);
            }

            primaries.Add((validPrimary, distance, neighbours));
        }

        // Sort primaries by (distance ascending, neighbours.Count ascending)
        primaries.Sort((a, b) =>
        {
            int d = a.distance.CompareTo(b.distance);
            if (d != 0) return d;

            return a.neighbours.Count.CompareTo(b.neighbours.Count);
        });

        // Evaluate primaries in sorted order
        foreach ((Transform transform, float distance, List<Transform> neighbours) in primaries)
        {
            Transform snappedFrom = transform;
            int validSecondary = 0;

            foreach (Transform neighbour in neighbours)
            {
                if (++validSecondary > MaxValidCollisions)
                    break;

                if (!TryDetectGrid(snappedFrom.position, neighbour.position, pieceSpacing))
                    continue;

                if (SnapPointGenerator.Generate(snapPoints, snappedFrom.position, gridDetected: true))
                    return true;
            }
        }

        return false;
    }

    private static bool TryDetectGrid(Vector3 primary, Vector3 neighbour, float pieceSpacing)
    {
        Vector3 direction = Utils.DirectionXZ(neighbour - primary);

        if (direction.sqrMagnitude < 0.000001f)
            return false;

        direction = FixedRotation * direction;

        RowDirection = direction * pieceSpacing;
        ColumnDirection = Vector3.Cross(Vector3.up, direction).normalized * pieceSpacing;

        return true;
    }

    private static bool TryFreeSnaps(float pieceSpacing, out List<SnapPoint> snapPoints)
    {
        snapPoints = [];
        float tolerance = Mathf.Max(pieceSpacing * 0.01f, 0.005f);
        float spacingRadius = pieceSpacing + tolerance;

        foreach (Transform validPrimary in ValidPrimaryCollisions(BasePosition, spacingRadius))
        {
            ComputeFreeDirections(validPrimary.position, pieceSpacing);

            if (SnapPointGenerator.Generate(snapPoints, validPrimary.position, gridDetected: false))
                return true;
        }

        return false;
    }

    private static void ComputeFreeDirections(Vector3 target, float pieceSpacing)
    {
        Vector3 direction = Utils.DirectionXZ(BasePosition - target);

        RowDirection = FixedRotation * direction * pieceSpacing;

        if (!AltPlacement)
        {
            float signed = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
            float snapped = Mathf.Round(signed / 22.5f) * 22.5f;

            RowDirection = FixedRotation * Quaternion.Euler(0, snapped, 0) * Vector3.forward * pieceSpacing;
        }

        ColumnDirection = Vector3.Cross(Vector3.up, RowDirection);
    }

    private static IEnumerable<Transform> ValidPrimaryCollisions(Vector3 origin, float spacingRadius)
    {
        int count = 0;

        foreach (Transform primaryCollision in CollisionScanner.Scan(origin, spacingRadius))
        {
            if (!IsPlantOrPickable(primaryCollision.gameObject))
                continue;

            if (++count > MaxValidCollisions)
                yield break;

            yield return primaryCollision;
        }
    }

    private static (SnapPoint snap, float distance) FindNearestManhattan(List<SnapPoint> snapPoints)
    {
        SnapPoint nearest = snapPoints[0];
        float nearestDistance = ManhattanDistance(nearest.pos, BasePosition, nearest.rowDir, nearest.colDir);

        for (int i = 1; i < snapPoints.Count; i++)
        {
            SnapPoint candidate = snapPoints[i];
            float snapDistance = ManhattanDistance(candidate.pos, BasePosition, candidate.rowDir, candidate.colDir);

            if (snapDistance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = snapDistance;
            }
        }

        return (nearest, nearestDistance);
    }

    private static SnapPoint FindNearestEuclidean(List<SnapPoint> snapPoints)
    {
        SnapPoint nearest = snapPoints[0];
        float nearestDistance = (nearest.pos - BasePosition).sqrMagnitude;

        for (int i = 1; i < snapPoints.Count; i++)
        {
            SnapPoint candidate = snapPoints[i];
            float snapDistance = (candidate.pos - BasePosition).sqrMagnitude;

            if (snapDistance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = snapDistance;
            }
        }

        return nearest;
    }

    // Measures path along grid lines instead of straight-line distance between 2 points
    private static float ManhattanDistance(Vector3 snapPos, Vector3 basePos, Vector3 rowDir, Vector3 colDir)
    {
        Vector3 direction = snapPos - basePos;

        float rowDistance = Mathf.Abs(Vector3.Dot(direction, rowDir));
        float colDistance = Mathf.Abs(Vector3.Dot(direction, colDir));

        return rowDistance + colDistance;
    }

    private static void CommitSnap(SnapPoint snap)
    {
        _lastSnap = snap;
        _hasLastSnap = true;

        _lastGhostRotation = PlacementGhost.transform.rotation;
        SavedBaseRotation ??= _lastGhostRotation;

        BasePosition = PlacementGhost.transform.position = snap.pos;
        SnapDirection = (snap.origin - snap.pos).normalized;
    }

    private static void ApplyRotationHysteresis()
    {
        const float rotationThreshold = 1f;

        if (_hasLastSnap && Quaternion.Angle(FixedRotation, _lastGhostRotation) > rotationThreshold)
            _hasLastSnap = false;

        if (SavedBaseRotation is Quaternion saved && Quaternion.Angle(saved, _lastGhostRotation) > rotationThreshold)
            ResetSavedOrientation();
    }

    private static SnapPoint ApplyPositionHysteresis(SnapPoint nearestSnap, float nearestGridDistance)
    {
        const float linearThreshold = 0.05f;
        const float sqrThreshold = linearThreshold * linearThreshold;

        if (_hasLastSnap && _lastSnap.origin == nearestSnap.origin)
        {
            float lastDistance = ManhattanDistance(_lastSnap.pos, BasePosition, _lastSnap.rowDir, _lastSnap.colDir);

            if (nearestGridDistance > lastDistance - sqrThreshold)
                return _lastSnap;
        }

        return nearestSnap;
    }

    private static void ApplyGridOrientation(SnapPoint snap)
    {
        // This first condition can add a secondary function for grid snapping (hold shift and it allows overlapping placements)
        if (/*AltPlacement || */config.GridSnappingStyle != 0)
            return;

        Vector3 row = snap.rowDir;
        Vector3 col = snap.colDir;

        if (config.Rows > 1)
            row = ChooseDirection(snap.pos, row);

        if (config.Columns > 1)
            col = ChooseDirection(snap.pos, col);

        RowDirection = row;
        ColumnDirection = col;

        bool hasSavedOrientation = SavedRowDirection != Vector3.zero;

        if (!hasSavedOrientation)
        {
            SavedRowDirection = RowDirection.normalized;
            SavedColumnDirection = ColumnDirection.normalized;
        }
        else
        {
            Vector3 rowNormalized = RowDirection.normalized;
            float dot = Mathf.Abs(Vector3.Dot(SavedRowDirection, rowNormalized));
            float tolerance = 0.05f;

            bool isGridAligned = dot > 1f - tolerance || dot < tolerance;

            if (!isGridAligned)
            {
                ResetSavedOrientation();
                SavedRowDirection = rowNormalized;
                SavedColumnDirection = ColumnDirection.normalized;
            }
            else
            {
                float spacing = RowDirection.magnitude;
                RowDirection = ChooseDirection(snap.pos, SavedRowDirection * spacing);
                ColumnDirection = ChooseDirection(snap.pos, SavedColumnDirection * spacing);
            }
        }

        if (!snap.isCardinal)
        {
            if (!PositionHasCollisions(snap.pos + RowDirection + ColumnDirection))
                return;

            ColumnDirection = -ColumnDirection;
        }

        //if (!snap.isCardinal)
        //{
        //    (int rowSign, int columnSign)[] signs = { (1, 1), (1, -1), (-1, 1), (-1, -1) };

        //    for (int i = 0; i < signs.Length; i++)
        //    {
        //        (int rowSign, int columnSign) = signs[i];

        //        Vector3 testRow = RowDirection * rowSign;
        //        Vector3 testCol = ColumnDirection * columnSign;

        //        Vector3 diagonal = testRow + testCol;

        //        if (!PositionHasCollisions(snap.pos + diagonal))
        //        {
        //            RowDirection = testRow;
        //            ColumnDirection = testCol;
        //            Dbgl($"Accepted diagonal orientation {i}");
        //            break;
        //        }
        //    }
        //}
    }

    private static void ApplyFreeOrientation(SnapPoint snap)
    {
        RowDirection = ChooseDirection(snap.pos, RowDirection);
        ColumnDirection = ChooseDirection(snap.pos, ColumnDirection);
    }

    private static Vector3 ChooseDirection(Vector3 origin, Vector3 direction)
    {
        if (!PositionHasCollisions(origin + direction))
            return direction;

        if (!PositionHasCollisions(origin - direction))
            return -direction;

        // Both directions have collisions, fallback to original
        return direction;
    }

    private static void ResetSavedOrientation()
    {
        SavedBaseRotation = null;
        SavedRowDirection = Vector3.zero;
        SavedColumnDirection = Vector3.zero;
    }
}
