namespace Advize_PlantEasily;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModContext;

internal static class ModUtils
{
    internal static bool HoldingCultivator => Player.m_localPlayer?.GetRightItem()?.m_shared.m_name == "$item_cultivator";

    internal static bool IsPlantOrPickable(GameObject go) => go.GetComponent<Plant>() || go.GetComponent<Pickable>();

    internal static bool HasGrowSpace(Plant plant, Vector3 position) => Physics.OverlapSphere(position, plant.m_growRadius, Plant.m_spaceMask).Length == 0;

    internal static bool PositionHasCollisions(Vector3 position) => Physics.CheckCapsule(position, position + (Vector3.up * 0.1f), Mathf.Epsilon, CollisionScanner.CollisionMask);

    internal static float GetPieceSpacing(GameObject go)
    {
        float colliderRadius = 0f;
        bool isSapling = false;
        Plant plant = go.GetComponent<Plant>();

        if (plant)
        {
            List<GameObject> colliderRoots = [go, .. plant.m_grownPrefabs];

            isSapling = colliderRoots.Any(x => x.GetComponent<TreeBase>());

            if (config.MinimizeGridSpacing && !isSapling)
            {
                for (int i = 0; i < colliderRoots.Count; i++)
                {
                    foreach (CapsuleCollider collider in colliderRoots[i].GetComponentsInChildren<CapsuleCollider>())
                        colliderRadius = Mathf.Max(colliderRadius, collider.radius);
                }
            }
            colliderRadius += isSapling ? config.ExtraSaplingSpacing : config.ExtraCropSpacing;
        }

        float growRadius = isSapling ? plant.m_growRadius * 2.2f : plant?.m_growRadius * (config.MinimizeGridSpacing ? 1.1f : 2f) ?? PickableSnapRadius(go.GetComponent<Piece>());

        return growRadius + colliderRadius;
    }

    internal static HashSet<Interactable> FindResourcesInRadius(GameObject rootInteractable)
    {
        HashSet<Interactable> extraInteractables = [];
        Transform root = rootInteractable.transform.root;

        Collider[] hits = Physics.OverlapSphere(root.position, config.HarvestRadius, CollisionScanner.CollisionMask);

        foreach (Collider hit in hits)
        {
            Pickable pickable = hit.GetComponentInParent<Pickable>();
            Beehive beehive = pickable ? null : hit.GetComponentInParent<Beehive>();

            if (!pickable && !beehive)
                continue;

            GameObject target = pickable ? pickable.gameObject : beehive.gameObject;
            Transform targetRoot = target.transform.root;

            if (targetRoot == root)
                continue;

            if (config.HarvestStyle == HarvestStyle.LikeResources && targetRoot.name != root.name)
                continue;

            if (beehive && beehive.GetHoneyLevel() < 1)
                continue;

            Interactable resource = pickable ?? (Interactable)beehive;
            extraInteractables.Add(resource);
        }

        return extraInteractables;
    }

    private static float PickableSnapRadius(Piece p) => p?.m_harvestRadius > 0 ? p.m_harvestRadius : config.DefaultGridSpacing;
}
