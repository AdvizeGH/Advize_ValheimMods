namespace Advize_PlantEasily;

using System.Collections.Generic;
using UnityEngine;

internal static class CollisionScanner
{
    internal static readonly int CollisionMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "item");

    private static readonly Collider[] _primary = new Collider[MaxRawColliders];
    private static readonly Collider[] _secondary = new Collider[MaxRawColliders];

    private const int MaxRawColliders = 50;

    internal static IEnumerable<Transform> Scan(Vector3 origin, float spacingRadius)
    {
        int count = Physics.OverlapSphereNonAlloc(origin, spacingRadius, _primary, CollisionMask);
        for (int i = 0; i < count; i++)
            yield return _primary[i].transform.root;
    }

    internal static IEnumerable<Transform> ScanNeighbours(Transform primary, float expectedSpacing)
    {
        float tolerance = Mathf.Max(expectedSpacing * 0.01f, 0.005f);

        float min = expectedSpacing - tolerance;
        float max = expectedSpacing + tolerance;

        float minSquared = min * min;
        float maxSquared = max * max;

        int neighbours = Physics.OverlapSphereNonAlloc(primary.position, max, _secondary, CollisionMask);

        Vector3 origin = primary.position;

        for (int i = 0; i < neighbours; i++)
        {
            Transform root = _secondary[i].transform.root;

            if (root == primary)
                continue;

            float distanceSquared = (root.position - origin).sqrMagnitude;

            if (distanceSquared >= minSquared && distanceSquared <= maxSquared)
                yield return root;
        }
    }
}
