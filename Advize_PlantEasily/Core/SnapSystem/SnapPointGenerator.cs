namespace Advize_PlantEasily;

using System.Collections.Generic;
using UnityEngine;
using static ModContext;
using static ModUtils;
using static PlacementState;

internal static class SnapPointGenerator
{
    private const int MaxRotations = 16;

    private static readonly Vector3[] _positionBuffer = new Vector3[MaxRotations];
    private static readonly SnapPoint[] _snapBuffer = new SnapPoint[MaxRotations];

    static SnapPointGenerator()
    {
        for (int i = 0; i < MaxRotations; i++)
            _snapBuffer[i] = new SnapPoint();
    }

    internal static bool Generate(List<SnapPoint> snapPoints, Vector3 snapFromPos, bool gridDetected)
    {
        int candidateCount = BuildCandidatePositions(snapFromPos, gridDetected);
        return ValidateCandidates(snapPoints, candidateCount, snapFromPos);
    }

    private static int BuildCandidatePositions(Vector3 snapFromPos, bool gridDetected)
    {
        Vector3 row = RowDirection;
        Vector3 col = ColumnDirection;
        int count = 0;

        if (gridDetected)
        {
            _positionBuffer[count++] = snapFromPos + row;
            _positionBuffer[count++] = snapFromPos - row;
            _positionBuffer[count++] = snapFromPos + col;
            _positionBuffer[count++] = snapFromPos - col;

            _positionBuffer[count++] = snapFromPos + row - col;
            _positionBuffer[count++] = snapFromPos + row + col;
            _positionBuffer[count++] = snapFromPos - row - col;
            _positionBuffer[count++] = snapFromPos - row + col;
        }
        else if (AltPlacement)
        {
            _positionBuffer[count++] = snapFromPos + row;
            _positionBuffer[count++] = snapFromPos - row;
            _positionBuffer[count++] = snapFromPos + col;
            _positionBuffer[count++] = snapFromPos - col;
        }
        else
        {
            float step = 360f / MaxRotations;
            float radStep = step * Mathf.Deg2Rad;

            for (int r = 0; r < MaxRotations; r++)
            {
                float angle = radStep * r;
                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);

                Vector3 rotated = new(row.x * cos + row.z * sin, row.y, row.z * cos - row.x * sin);

                _positionBuffer[count++] = snapFromPos + rotated;
            }
        }

        return count;
    }

    private static bool ValidateCandidates(List<SnapPoint> snapPoints, int candidateCount, Vector3 snapFromPos)
    {
        Vector3 row = RowDirection;
        Vector3 col = ColumnDirection;

        int snapCount = 0;
        bool hasCardinal = false;

        // First pass, evaluate candidates
        for (int i = 0; i < candidateCount; i++)
        {
            Vector3 snapPosition = _positionBuffer[i];

            if (PositionHasCollisions(snapPosition))
                continue;

            if (Plant != null && !HasGrowSpace(Plant, snapPosition))
                continue;

            Vector3 direction = snapPosition - snapFromPos;

            float spacing = row.magnitude;
            float projectedRowDistance = Mathf.Abs(Vector3.Dot(direction, row.normalized));
            float projectedColumnDistance = Mathf.Abs(Vector3.Dot(direction, col.normalized));

            float cardinalThreshold = spacing * 0.25f; // 25% of spacing

            bool isCardinal =
                (projectedRowDistance < cardinalThreshold && projectedColumnDistance >= cardinalThreshold) ||
                (projectedColumnDistance < cardinalThreshold && projectedRowDistance >= cardinalThreshold);

            InitSnapPoint(snapCount++, snapPosition, row, col, snapFromPos, isCardinal);

            if (isCardinal)
                hasCardinal = true;
        }

        if (snapCount == 0)
            return false;

        // Second pass, output only cardinal points if any are present
        for (int i = 0; i < snapCount; i++)
        {
            SnapPoint sp = _snapBuffer[i];

            if (!config.PreferCardinalSnapping || !hasCardinal || sp.isCardinal)
                snapPoints.Add(sp);
        }

        return snapPoints.Count > 0;
    }

    private static SnapPoint InitSnapPoint(int index, Vector3 pos, Vector3 row, Vector3 col, Vector3 origin, bool isCardinal)
    {
        SnapPoint sp = _snapBuffer[index];
        sp.pos = pos;
        sp.rowDir = row;
        sp.colDir = col;
        sp.origin = origin;
        sp.isCardinal = isCardinal;
        return sp;
    }
}
