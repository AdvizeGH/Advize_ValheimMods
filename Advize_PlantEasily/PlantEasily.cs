using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Advize_PlantEasily.Configuration;

namespace Advize_PlantEasily
{
    [BepInPlugin(PluginID, PluginName, Version)]
    [BepInProcess("valheim.exe")] // This mod shouldn't be run on the server... yet
    public partial class PlantEasily : BaseUnityPlugin
    {
        public const string PluginID = "advize.PlantEasily";
        public const string PluginName = "PlantEasily";
        public const string Version = "1.6.3";
        
        private readonly Harmony Harmony = new(PluginID);
        public static ManualLogSource PELogger = new($" {PluginName}");

        private static readonly Dictionary<string, GameObject> prefabRefs = new();

        private static ModConfig config;

        private static GameObject placementGhost;
        private static readonly List<GameObject> extraGhosts = new();
        private static readonly List<Status> ghostPlacementStatus = new();
        private static readonly List<int> instanceIDS = new();
        
        private static readonly int snapCollisionMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "item");
        private static readonly int plantCollisionMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");

        private static bool HoldingCultivator => Player.m_localPlayer?.GetRightItem()?.m_shared.m_name == "$item_cultivator";

        private static bool OverrideGamepadInput => placementGhost && Input.GetKey(config.GamepadModifierKey);

        public void Awake()
        {
            BepInEx.Logging.Logger.Sources.Add(PELogger);
            config = new ModConfig(Config);
            Harmony.PatchAll();
        }

        private static bool IsPlantOrPickable(GameObject go) => go.GetComponent<Plant>() || go.GetComponent<Pickable>();

        internal static void GridSizeChanged(object sender, EventArgs e) => DestroyGhosts();
        
        private static void DestroyGhosts()
        {
            if (extraGhosts.Count > 0)
            {
                foreach (GameObject placementGhost in extraGhosts)
                    Destroy(placementGhost);
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
                        t.gameObject.layer = LayerMask.NameToLayer("ghost");
                    
                    newGhost.transform.position = rootGhost.transform.position;
                    newGhost.transform.localScale = rootGhost.transform.localScale;
                    extraGhosts.Add(newGhost);
                    ghostPlacementStatus.Add(Status.Healthy);
                }
            }
        }

        private static bool HasGrowSpace(Plant plant, Vector3 position) => Physics.OverlapSphere(position, plant.m_growRadius, plantCollisionMask).Length == 0;
        
        private static bool PositionHasCollisions(Vector3 position) => Physics.CheckCapsule(position, position + (Vector3.up / 2), 0.0001f, snapCollisionMask);

        private static float GetPieceSpacing(GameObject go)
        {
            float colliderRadius = 0f;
            bool isSapling = false;
            Plant plant = go.GetComponent<Plant>();
            
            if (plant)
            {
                List<GameObject> colliderRoots = new();
                colliderRoots.Add(go);
                colliderRoots.AddRange(plant.m_grownPrefabs);
                isSapling = colliderRoots.Any(x => x.GetComponent<TreeBase>());

                if (!config.StandardizeGridSpacing && !isSapling)
                {
                    for (int i = 0; i < colliderRoots.Count; i++)
                    {
                        foreach (CapsuleCollider collider in colliderRoots[i].GetComponentsInChildren<CapsuleCollider>())
                        {
                            colliderRadius = Mathf.Max(colliderRadius, collider.radius);
                        }
                    }
                }
                colliderRadius += isSapling ? config.ExtraSaplingSpacing : config.ExtraCropSpacing;
            }
            
            float growRadius = isSapling ? plant.m_growRadius * 2.2f : plant?.m_growRadius * (config.StandardizeGridSpacing ? 2f : 1.1f) ?? PickableSnapRadius(go.name);

            return growRadius + colliderRadius;
        }

        private static float PickableSnapRadius(string name)
        {
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

            if (plant && (bool)Traverse.Create(plant).Method("HaveRoof").GetValue())
                placementStatus = Status.NoSun;

            if (!plant && config.PreventOverlappingPlacements && PositionHasCollisions(position))
                placementStatus = Status.NoSpace;
            
            return placementStatus;
        }

        private static List<Interactable> FindResourcesInRadius(GameObject rootInteractable)
        {
            List<Interactable> extraInteractables = new();
            Collider[] obstructions = Physics.OverlapSphere(rootInteractable.transform.root.position, config.HarvestRadius, snapCollisionMask);

            foreach (Collider obstruction in obstructions)
            {
                Pickable collidingPickable = obstruction.GetComponentInParent<Pickable>();
                Beehive collidingBeehive = obstruction.GetComponentInParent<Beehive>();

                if (!collidingPickable && !collidingBeehive)
                    continue;
                
                GameObject collidingInteractable = collidingPickable?.gameObject ?? collidingBeehive.gameObject;

                if (collidingInteractable.transform.root != rootInteractable.transform.root)
                {
                    if (config.HarvestStyle == HarvestStyle.LikeResources && collidingInteractable.transform.root.name != rootInteractable.transform.root.name)
                        continue;
                    if (collidingBeehive && collidingBeehive.GetHoneyLevel() < 1)
                        continue;

                    Interactable resource = (Interactable) collidingPickable ?? collidingBeehive;
                    if (!extraInteractables.Contains(resource))
                        extraInteractables.Add(resource);
                }
            }

            return extraInteractables;
        }

        private static void PlacePiece(Player player, GameObject go, Piece piece)
        {
            Vector3 position = go.transform.position;
            Quaternion rotation = config.RandomizeRotation ? Quaternion.Euler(0f, 22.5f * UnityEngine.Random.Range(0, 16), 0f) : go.transform.rotation;
            GameObject gameObject = piece.gameObject;

            TerrainModifier.SetTriggerOnPlaced(trigger: true);
            GameObject gameObject2 = Instantiate(gameObject, position, rotation);
            TerrainModifier.SetTriggerOnPlaced(trigger: false);

            gameObject2.GetComponent<Piece>().SetCreator(player.GetPlayerID());

            piece.m_placeEffect.Create(position, rotation, gameObject2.transform, 1f);
            player.AddNoise(50f);

            Game.instance.IncrementPlayerStat(PlayerStatType.Builds);
            Gogan.LogEvent("Game", "PlacedPiece", gameObject.name, 0L);
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

        private static readonly Dictionary<int, string> statusMessage = new()
        {
            { 1, "$msg_missingrequirement" },
            { 2, "$piece_plant_notcultivated" },
            { 3, "$piece_plant_wrongbiome" },
            { 4, "$piece_plant_nospace" },
            { 5, "$piece_plant_nosun" },
            { 6, "$msg_invalidplacement" }
        };

        private static readonly Dictionary<string, string> pickablesToPlants = new()
        {
            { "Pickable_SeedOnion", "sapling_seedonion" },
            { "Pickable_Onion", "sapling_onion" },
            { "Pickable_Turnip", "sapling_turnip" },
            { "Pickable_Barley", "sapling_barley" },
            { "Pickable_Mushroom_JotunPuffs", "sapling_jotunpuffs" },
            { "Pickable_Carrot", "sapling_carrot" },
            { "Pickable_SeedCarrot", "sapling_seedcarrot" },
            { "Pickable_Flax", "sapling_flax" },
            { "Pickable_Mushroom_Magecap", "sapling_magecap" },
            { "Pickable_SeedTurnip", "sapling_seedturnip" }
        };

        private static void InitPrefabRefs()
        {
            Dbgl("InitPrefabRefs");
            if (prefabRefs.Count > 0) return;

            foreach (string prefabName in pickablesToPlants.Values)
                prefabRefs.Add(prefabName, null);

            UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            for (int i = 0; i < array.Length; i++)
            {
                GameObject gameObject = (GameObject)array[i];

                if (!prefabRefs.ContainsKey(gameObject.name)) continue;

                prefabRefs[gameObject.name] = gameObject;

                if (!prefabRefs.Any(key => !key.Value))
                {
                    Dbgl("Found all prefab references");
                    break;
                }
            }
        }

        internal static void Dbgl(string message, bool forceLog = false, bool logError = false)
        {
            if (forceLog || config.EnableDebugMessages)
            {
                if (!logError)
                    PELogger.LogInfo(message);
                else
                    PELogger.LogError(message);
            }
        }
    }
}
