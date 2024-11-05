namespace Advize_PlantEasily;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[BepInPlugin(PluginID, PluginName, Version)]
public sealed class PlantEasily : BaseUnityPlugin
{
    public const string PluginID = "advize.PlantEasily";
    public const string PluginName = "PlantEasily";
    public const string Version = "1.10.0";

    private static readonly ManualLogSource PELogger = new($" {PluginName}");
    internal static ModConfig config;
    internal static PlantEasily pluginInstance;

    internal static readonly Dictionary<string, GameObject> prefabRefs = [];
    internal static Dictionary<string, ReplantDB> pickableNamesToReplantDB = [];

    internal static GameObject placementGhost;
    internal static string lastPlacementGhost = "";
    internal static readonly List<GameObject> extraGhosts = [];
    internal static readonly List<GameObject> currentValidGhosts = [];
    internal static readonly List<Status> ghostPlacementStatus = [];
    internal static readonly List<int> instanceIDS = [];
    internal static string keyboardHarvestModifierKeyLocalized;
    internal static string gamepadModifierKeyLocalized;
    internal static bool isPlanting = false;

    internal static GameObject gridRenderer;
    internal static List<LineRenderer> lineRenderers = [];

    //Might need these later
    //internal static List<ReplantDB> vanillaCropRefs = [];
    //internal static List<ReplantDB> moddedCropRefs = [];

    internal static readonly int CollisionMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "item");

    internal static bool HoldingCultivator => Player.m_localPlayer?.GetRightItem()?.m_shared.m_name == "$item_cultivator";

    internal static bool OverrideGamepadInput => placementGhost && ZInput.GetKey(config.GamepadModifierKey, logWarning: false);

    public void Awake()
    {
        BepInEx.Logging.Logger.Sources.Add(PELogger);
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Dbgl("This mod is client-side only and is not needed on a dedicated server. Plugin patches will not be applied.", true, LogLevel.Warning);
            return;
        }
        config = new ModConfig(Config);
        pluginInstance = this;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginID);
    }

    internal static bool IsPlantOrPickable(GameObject go) => go.GetComponent<Plant>() || go.GetComponent<Pickable>();

    internal static void GridSizeChanged(object sender, EventArgs e) => DestroyGhosts();

    internal static void KeybindsChanged(object sender, EventArgs e) => KeyHintPatches.UpdateKeyHintText();

    internal static void DestroyGhosts()
    {
        if (extraGhosts.Count > 0)
        {
            foreach (GameObject placementGhost in extraGhosts)
                Destroy(placementGhost);
            extraGhosts.Clear();
        }
        ghostPlacementStatus.Clear();
        gridRenderer.SetActive(false);
    }

    internal static void CreateGhosts(GameObject rootGhost)
    {
        for (int row = 0; row < config.Rows; row++)
        {
            for (int column = 0; column < config.Columns; column++)
            {
                if (row == 0 && column == 0)
                {
                    placementGhost = rootGhost;
                    lastPlacementGhost = pickableNamesToReplantDB.Where(kvp => kvp.Value.plantName == rootGhost.name).Select(kvp => kvp.Key).FirstOrDefault();
                    ghostPlacementStatus.Add(Status.Healthy);
                    continue;
                }

                if (extraGhosts.Count >= config.MaxConcurrentPlacements - 1) return;

                ZNetView.m_forceDisableInit = true;
                GameObject newGhost = Instantiate(rootGhost);
                ZNetView.m_forceDisableInit = false;
                newGhost.name = rootGhost.name;

                int layer = LayerMask.NameToLayer("ghost");
                foreach (Transform t in newGhost.GetComponentsInChildren<Transform>())
                    t.gameObject.layer = layer;

                newGhost.transform.position = rootGhost.transform.position;
                newGhost.transform.localScale = rootGhost.transform.localScale;
                extraGhosts.Add(newGhost);
                ghostPlacementStatus.Add(Status.Healthy);
            }
        }
    }

    internal static bool HasGrowSpace(Plant plant, Vector3 position) => Physics.OverlapSphere(position, plant.m_growRadius, Plant.m_spaceMask).Length == 0;

    internal static bool PositionHasCollisions(Vector3 position) => Physics.CheckCapsule(position, position + (Vector3.up / 2), 0.0001f, CollisionMask);

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
                    {
                        colliderRadius = Mathf.Max(colliderRadius, collider.radius);
                    }
                }
            }
            colliderRadius += isSapling ? config.ExtraSaplingSpacing : config.ExtraCropSpacing;
        }

        float growRadius = isSapling ? plant.m_growRadius * 2.2f : plant?.m_growRadius * (config.MinimizeGridSpacing ? 1.1f : 2f) ?? PickableSnapRadius(go.name);

        return growRadius + colliderRadius;
    }

    private static float PickableSnapRadius(string name)
    {
        if (berries.Contains(name))
            return config.BerryBushSnapRadius;
        if (mushrooms.Contains(name))
            return config.MushroomSnapRadius;
        if (flowers.Contains(name))
            return config.FlowerSnapRadius;

        return config.PickableSnapRadius;
    }

    internal static void SetPlacementGhostStatus(GameObject ghost, int index, Status placementStatus, ref int m_placementStatus)
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

    internal static Status CheckPlacementStatus(GameObject ghost, Status placementStatus = Status.Healthy)
    {
        Piece piece = ghost.GetComponent<Piece>();
        Plant plant = ghost.GetComponent<Plant>();
        Vector3 position = ghost.transform.position;
        Heightmap heightmap = Heightmap.FindHeightmap(position);

        if (heightmap == null) return Status.Invalid;

        bool cultivatedGroundOnly = plant?.m_needCultivatedGround ?? piece.m_cultivatedGroundOnly;
        Heightmap.Biome allowedBiomes = plant?.m_biome ?? piece.m_onlyInBiome;
        Heightmap.Biome currentBiome = heightmap.GetBiome(position);

        if (cultivatedGroundOnly && heightmap && !heightmap.IsCultivated(position))
            placementStatus = Status.NotCultivated;

        if (allowedBiomes != 0 && (currentBiome & allowedBiomes) == 0)
            placementStatus = Status.WrongBiome;

        if (plant)
        {
            if (!HasGrowSpace(plant, position))
                placementStatus = Status.NoSpace;

            if (plant.HaveRoof())
                placementStatus = Status.NoSun;

            if (plant.m_attachDistance > 0.0 && !plant.GetClosestAttachPosRot(out plant.m_attachPos, out plant.m_attachRot, out plant.m_attachNormal))
                placementStatus = Status.NoAttachPiece;

            if (!plant.m_tolerateHeat && currentBiome == Heightmap.Biome.AshLands && !ShieldGenerator.IsInsideShield(position))
                placementStatus = Status.TooHot;

            if (!plant.m_tolerateCold && (currentBiome == Heightmap.Biome.DeepNorth || currentBiome == Heightmap.Biome.Mountain) && !ShieldGenerator.IsInsideShield(position))
                placementStatus = Status.TooCold;
        }
        else
        {
            if (config.PreventOverlappingPlacements && PositionHasCollisions(position))
                placementStatus = Status.NoSpace;
        }

        return placementStatus;
    }

    internal static List<Interactable> FindResourcesInRadius(GameObject rootInteractable)
    {
        List<Interactable> extraInteractables = [];
        Collider[] obstructions = Physics.OverlapSphere(rootInteractable.transform.root.position, config.HarvestRadius, CollisionMask);

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

                Interactable resource = (Interactable)collidingPickable ?? collidingBeehive;
                if (!extraInteractables.Contains(resource))
                    extraInteractables.Add(resource);
            }
        }

        return extraInteractables;
    }

    internal static void PlacePiece(Player player, GameObject go, GameObject piecePrefab)
    {
        Vector3 position = go.transform.position;
        Quaternion rotation = config.RandomizeRotation ? Quaternion.Euler(0f, 22.5f * UnityEngine.Random.Range(0, 16), 0f) : go.transform.rotation;

        TerrainModifier.SetTriggerOnPlaced(trigger: true);
        GameObject clone = Instantiate(piecePrefab, position, rotation);
        TerrainModifier.SetTriggerOnPlaced(trigger: false);

        clone.GetComponent<Piece>().SetCreator(player.GetPlayerID());
        //Disable this, it's already applied to root placement ghost and impacts placement performance
        //piece.m_placeEffect.Create(position, rotation, gameObject2.transform, 1f);
        //player.AddNoise(50f);

        Game.instance.IncrementPlayerStat(PlayerStatType.Builds);
        player.RaiseSkill(Skills.SkillType.Farming, 1f);
    }

    internal IEnumerator BulkPlanting(GameObject piecePrefab)
    {
        Player player = Player.m_localPlayer;
        isPlanting = true;
        int count = 0;

        foreach (GameObject go in currentValidGhosts)
        {
            count++;
            PlacePiece(player, go, piecePrefab);
            if (count % config.BulkPlantingBatchSize == 0) yield return null;
        }

        currentValidGhosts.Clear();
        isPlanting = false;
    }

    internal enum Status
    {
        Healthy,        // 0
        LackResources,  // 1
        NotCultivated,  // 2
        WrongBiome,     // 3
        NoSpace,        // 4
        NoSun,          // 5
        Invalid,        // 6
        NoAttachPiece,  // 7
        TooHot,         // 8
        TooCold         // 9
    }

    internal static readonly Dictionary<int, string> statusMessage = new()
    {
        { 1, "$msg_missingrequirement" },
        { 2, "$piece_plant_notcultivated" },
        { 3, "$piece_plant_wrongbiome" },
        { 4, "$piece_plant_nospace" },
        { 5, "$piece_plant_nosun" },
        { 6, "$msg_invalidplacement" },
        { 7, "$piece_plant_nowall" },
        { 8, "$piece_plant_toohot" },
        { 9, "$piece_plant_toocold" }
    };

    private static readonly string[] berries = ["RaspberryBush", "BlueberryBush", "CloudberryBush"];
    private static readonly string[] mushrooms = ["Pickable_Mushroom", "Pickable_Mushroom_yellow", "Pickable_Mushroom_blue", "Pickable_SmokePuff"];
    private static readonly string[] flowers = ["Pickable_Dandelion", "Pickable_Thistle", "Pickable_Fiddlehead"];

    internal static void InitPrefabRefs()
    {
        Dbgl("InitPrefabRefs");

        foreach (string pickablePrefab in pickableNamesToReplantDB.Keys)
        {
            prefabRefs.Add(pickablePrefab, null);
            prefabRefs.Add(pickableNamesToReplantDB[pickablePrefab].plantName, null);
        }

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

        CreateLineRenderers();
    }

    private static void CreateLineRenderers()
    {
        Material material = Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "Default-Line");
        gridRenderer = new();
        DontDestroyOnLoad(gridRenderer);

        for (int i = 0; i < 2; i++)
        {
            GameObject child = new();
            child.transform.SetParent(gridRenderer.transform);
            lineRenderers.Add(child.AddComponent<LineRenderer>());
            lineRenderers[i].material = material;
            lineRenderers[i].widthMultiplier = 0.025f;
        }

        lineRenderers[0].startColor = Color.blue;
        lineRenderers[0].endColor = Color.cyan;
        lineRenderers[1].startColor = Color.green;
        lineRenderers[1].endColor = Color.yellow;
    }

    internal static void Dbgl(string message, bool forceLog = false, LogLevel level = LogLevel.Info)
    {
        if (forceLog || config.EnableDebugMessages)
        {
            switch (level)
            {
                case LogLevel.Error:
                    PELogger.LogError(message);
                    break;
                case LogLevel.Warning:
                    PELogger.LogWarning(message);
                    break;
                case LogLevel.Info:
                    PELogger.LogInfo(message);
                    break;
                case LogLevel.Message:
                    PELogger.LogMessage(message);
                    break;
                case LogLevel.Debug:
                    PELogger.LogDebug(message);
                    break;
                case LogLevel.Fatal:
                    PELogger.LogFatal(message);
                    break;
            }
        }
    }
}
