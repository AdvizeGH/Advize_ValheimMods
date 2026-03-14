namespace Advize_PlantEasily;

using System.Collections.Generic;
using UnityEngine;
using static ModContext;
using static ModUtils;

internal static class GhostStatus
{
    private static readonly Dictionary<Heightmap.Biome, Status> TemperatureBiomeStatus = new()
    {
        { Heightmap.Biome.AshLands, Status.TooHot },
        { Heightmap.Biome.DeepNorth, Status.TooCold },
        { Heightmap.Biome.Mountain, Status.TooCold }
    };

    internal static Status EvaluateStatus(GameObject ghost, Status baseStatus = Status.Healthy)
    {
        bool isRoot = ghost == PlacementState.PlacementGhost;

        Piece piece;
        Plant plant;

        if (isRoot)
        {
            piece = PlacementState.Piece;
            plant = PlacementState.Plant;
        }
        else
        {
            GhostCache cache = ghost.GetComponent<GhostCache>();
            piece = cache.piece;
            plant = cache.plant;
        }

        Vector3 position = ghost.transform.position;
        Heightmap heightmap = Heightmap.FindHeightmap(position);

        if (heightmap == null)
            return Status.Invalid;

        // Cultivation
        bool needsCultivated = plant?.m_needCultivatedGround ?? piece.m_cultivatedGroundOnly;
        if (needsCultivated && !heightmap.IsCultivated(position))
            return Status.NotCultivated;

        // Biome
        Heightmap.Biome allowed = plant?.m_biome ?? piece.m_onlyInBiome;
        Heightmap.Biome biome = heightmap.GetBiome(position);

        if (allowed != 0 && (biome & allowed) == 0)
            return Status.WrongBiome;

        // Plant-specific checks
        if (plant)
        {
            if (!HasGrowSpace(plant, position))
                return Status.NoSpace;

            if (plant.HaveRoof())
                return Status.NoSun;

            if (plant.m_attachDistance > 0f && !plant.GetClosestAttachPosRot(out plant.m_attachPos, out plant.m_attachRot, out plant.m_attachNormal))
                return Status.NoAttachPiece;

            if (TemperatureBiomeStatus.TryGetValue(biome, out Status tempStatus))
            {
                bool tolerate =
                    tempStatus switch
                    {
                        Status.TooHot => plant.m_tolerateHeat,
                        Status.TooCold => plant.m_tolerateCold,
                        _ => true
                    };

                if (!tolerate)
                {
                    if (!ShieldGenerator.IsInsideShield(position))
                        return tempStatus;
                }
            }
        }
        else // Pickables
        {
            if (config.PreventOverlappingPlacements && PositionHasCollisions(position))
                return Status.NoSpace;
        }

        return baseStatus;
    }
}
