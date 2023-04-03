using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Advize_PlantEasily.Configuration;
using System;
using System.Reflection;

namespace Advize_PlantEasily
{
    [BepInPlugin(PluginID, PluginName, Version)]
    public partial class PlantEasily : BaseUnityPlugin
    {
        public const string PluginID = "advize.PlantEasily";
        public const string PluginName = "PlantEasily";
        public const string Version = "1.0.3";
        
        private readonly Harmony Harmony = new(PluginID);
        public static ManualLogSource PELogger = new($" {PluginName}");
        
        private static ModConfig config;

        private static GameObject placementGhost;
        private static List<GameObject> extraGhosts = new();
        private static List<Status> ghostPlacementStatus = new();
        
        private static int snapCollisionMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "item");
        
        public void Awake()
        {
            BepInEx.Logging.Logger.Sources.Add(PELogger);
            config = new ModConfig(Config);
            Harmony.PatchAll();
        }
        
        private static bool OverrideGamepadInput() => placementGhost && Input.GetKey(config.GamepadModifierKey);
        
        internal static void GridSizeChanged(object sender, EventArgs e) => DestroyGhosts();
        
        private static void DestroyGhosts()
        {
            if (extraGhosts.Count > 0)
            {
                foreach (GameObject placementGhost in extraGhosts)
                {
                    if (placementGhost)
                    {
                        DestroyImmediate(placementGhost);
                    }
                }
                extraGhosts.Clear();
            }
            ghostPlacementStatus.Clear();
        }
        
        private static void CreateGhosts(GameObject rootGhost)
        {
            for (int row = 0; row < config.Rows; row++)
            {
                for (int column = 0; column < config.Columns; column++)
                {
                    if (row == 0 && column == 0)
                    {
                        placementGhost = rootGhost;
                        ghostPlacementStatus.Add(Status.Healthy);
                        continue;
                    }
                    
                    ZNetView.m_forceDisableInit = true;
                    GameObject newGhost = Instantiate(rootGhost);
                    ZNetView.m_forceDisableInit = false;
                    newGhost.name = rootGhost.name;
                    
                    foreach (Transform t in newGhost.GetComponentsInChildren<Transform>())
                    {
                        t.gameObject.layer = LayerMask.NameToLayer("ghost");
                    }
                    
                    newGhost.transform.position = rootGhost.transform.position;
                    newGhost.transform.localScale = rootGhost.transform.localScale;
                    extraGhosts.Add(newGhost);
                    ghostPlacementStatus.Add(Status.Healthy);
                }
            }
        }

        private static bool HasGrowSpace(Plant plant, Vector3 position)
        {
            int plantCollisionMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");
            return Physics.OverlapSphere(position, plant.m_growRadius, plantCollisionMask).Length == 0;
        }

        private static bool HasRoof(GameObject ghost)
        {
            int roofMask = LayerMask.GetMask("Default", "static_solid", "piece");
            return Physics.Raycast(ghost.transform.position, Vector3.up, 100f, roofMask);
        }

        private static float PickableSnapRadius(string name)
        {
            // Find a new route, constant string operations for each game tick should be avoided.
            if (name.EndsWith("berryBush"))
                return config.BerryBushSnapRadius;
            if (name.StartsWith("Pickable_Mushroom"))
                return config.MushroomSnapRadius;
            if (name.Contains("Dandelion") || name.Contains("Thistle"))
                return config.FlowerSnapRadius;
            
            return config.PickableSnapRadius;
        }
        
        private static void SetPlacementGhostStatus(GameObject ghost, int index, Status placementStatus, ref int m_placementStatus)
        {
            ghost.GetComponent<Piece>().SetInvalidPlacementHeightlight(placementStatus != Status.Healthy);
            
            if (ghostPlacementStatus.Count > index)
            {
                ghostPlacementStatus[index] = placementStatus;
                if (index == 0 && placementStatus == Status.Healthy)
                {
                    m_placementStatus = 0;
                }
            }
        }
        
        private static Status CheckPlacementStatus(GameObject ghost, Status placementStatus = Status.Healthy)
        {
            Piece piece = ghost.GetComponent<Piece>();
            Plant plant = ghost.GetComponent<Plant>();
            Vector3 position = ghost.transform.position;
            Heightmap heightmap = Heightmap.FindHeightmap(position);

            bool cultivatedGroundOnly = plant?.m_needCultivatedGround ?? piece.m_cultivatedGroundOnly;
            Heightmap.Biome biome = plant?.m_biome ?? piece.m_onlyInBiome;

            if (cultivatedGroundOnly && heightmap && !heightmap.IsCultivated(position))
                placementStatus = Status.NotCultivated;

            if (biome != 0 && (Heightmap.FindBiome(position) & biome) == 0)
                placementStatus = Status.WrongBiome;

            if (plant && !HasGrowSpace(plant, ghost.transform.position))
                placementStatus = Status.NoSpace;

            if (plant && HasRoof(ghost))
                placementStatus = Status.NoSun;

            if (!plant && config.PreventOverlappingPlacements && Physics.CheckSphere(position, 0.025f, snapCollisionMask))
                placementStatus = Status.NoSpace;
            
            return placementStatus;
        }

        private static void FindResourcesInRadius()
        {

        }

        private static void FindConnectedResources()
        {

        }

        private enum Status
        {
            Healthy,        // 0
            LackResources,  // 1
            NotCultivated,  // 2
            WrongBiome,     // 3
            NoSpace,        // 4
            NoSun,          // 5
            Invalid         // 6
        }

        private static Dictionary<int, string> statusMessage = new()
        {
            { 1, "$msg_missingrequirement" },
            { 2, "$piece_plant_notcultivated" },
            { 3, "$piece_plant_wrongbiome" },
            { 4, "$piece_plant_nospace" },
            { 5, "$piece_plant_nosun" },
            { 6, "$msg_invalidplacement" }
        };

        internal static void Dbgl(string message, bool forceLog = false, bool logError = false)
        {
            if (forceLog || config.EnableDebugMessages)
            {
                if (logError)
                {
                    PELogger.LogError(message);
                }
                else
                {
                    PELogger.LogInfo(message);
                }
            }
        }
    }
}
