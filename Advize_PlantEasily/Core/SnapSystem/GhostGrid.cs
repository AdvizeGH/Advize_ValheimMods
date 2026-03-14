namespace Advize_PlantEasily;

using System.Collections.Generic;
using UnityEngine;
using static ModContext;
using static PlacementState;

internal static class GhostGrid
{
    internal static readonly List<GameObject> ExtraGhosts = [];
    internal static readonly List<GameObject> ValidExtraGhosts = [];
    internal static readonly List<Status> GhostPlacementStatus = [];

    internal static GameObject DirectionRenderer;
    internal static List<LineRenderer> LineRenderers = [];

    private static readonly int ghostLayer = LayerMask.NameToLayer("ghost");

    private static int _gridVersion;
    private static int _ghostUpdateIndex;

    private static Vector3 _lastBasePosition;
    private static Quaternion _lastBaseRotation;
    private static Quaternion _lastEffectiveRotation;
    private static int _lastRows;
    private static int _lastColumns;
    private static string _lastPieceName;
    private static bool _preservePool;

    private static int MaxActiveGhosts => Mathf.Min(config.Rows * config.Columns - 1, config.MaxConcurrentPlacements - 1);
    private static int TotalCells => 1 + MaxActiveGhosts;
    private static int ActualRows => (TotalCells + config.Columns - 1) / config.Columns;
    private static int ActualColumns => Mathf.Min(config.Columns, TotalCells);

    internal static void ResizeGrid()
    {
        _preservePool = true;
        // Not the cleanest, but this next part triggers UpdatePlacementGhost to re-invoke SetupPlacementGhost which calls PrepareGhostPool followed by BuildGrid()
        GhostPlacementStatus.Clear();
    }

    internal static void BuildGrid(GameObject rootGhost)
    {
        //Dbgl("BuildGrid");
        GrowPoolIfNeeded(rootGhost);

        GhostPlacementStatus.Clear();

        InitializeGhosts(rootGhost);

        DeactivateExcessGhosts();
    }

    internal static void PrepareGhostPool(GameObject currentPlacementGhost)
    {
        //Dbgl("PrepareGhostPool");
        DirectionRenderer?.SetActive(false);

        DetectPieceChange(currentPlacementGhost);

        //Dbgl($"InPlaceMode? {Player.m_localPlayer?.InPlaceMode()}");

        if (ShouldPreservePool())
        {
            //Dbgl("Preserving Ghosts: Not destroying and clearing extraGhosts");
            return;
        }

        DestroyExtraGhosts();
    }

    private static void GrowPoolIfNeeded(GameObject rootGhost)
    {
        //Dbgl("GrowPoolIfNeeded");
        int poolSize = ExtraGhosts.Count;
        string rootName = rootGhost.name;

        while (poolSize < MaxActiveGhosts && poolSize < config.MaxConcurrentPlacements - 1)
        {
            ZNetView.m_forceDisableInit = true;
            GameObject newGhost = Object.Instantiate(rootGhost);
            newGhost.AddComponent<GhostCache>().Init();
            ZNetView.m_forceDisableInit = false;

            newGhost.name = rootName;

            foreach (Transform t in newGhost.GetComponentsInChildren<Transform>())
                t.gameObject.layer = ghostLayer;

            ExtraGhosts.Add(newGhost);
            poolSize++;
        }
    }

    private static void InitializeGhosts(GameObject rootGhost)
    {
        Transform rootTransform = rootGhost.transform;
        int index = 0;

        for (int row = 0; row < config.Rows; row++)
        {
            for (int column = 0; column < config.Columns; column++)
            {
                if (row == 0 && column == 0)
                {
                    SetReferences(rootGhost);

                    GhostPlacementStatus.Add(Status.Healthy);
                    continue;
                }

                if (index >= MaxActiveGhosts)
                    return;

                GameObject ghost = ExtraGhosts[index++];
                ghost.SetActive(true);

                Transform t = ghost.transform;
                t.position = rootTransform.position;
                t.localScale = rootTransform.localScale;

                GhostPlacementStatus.Add(Status.Healthy);
            }
        }
    }

    private static void DeactivateExcessGhosts()
    {
        //Dbgl("DeactivateExcessGhosts");
        for (int i = MaxActiveGhosts; i < ExtraGhosts.Count; i++)
            ExtraGhosts[i].SetActive(false);
    }

    private static void DetectPieceChange(GameObject currentPlacementGhost)
    {
        if (!currentPlacementGhost)
            return;

        string currentPieceName = currentPlacementGhost.name;

        // First time init
        if (string.IsNullOrEmpty(_lastPieceName))
        {
            _lastPieceName = currentPieceName;
            return;
        }

        if (currentPieceName == _lastPieceName)
        {
            //Dbgl($"No piece change: {_lastPieceName} : {currentPieceName}");
            _preservePool = true;
            return;
        }

        //Dbgl($"Detected piece change: from {_lastPieceName} to {currentPieceName}");
        _lastPieceName = currentPieceName;
    }

    private static bool ShouldPreservePool()
    {
        if (!_preservePool || !config.ModActive)
            return false;

        _preservePool = false;
        return true;
    }

    private static void DestroyExtraGhosts()
    {
        foreach (GameObject ghost in ExtraGhosts)
            Object.Destroy(ghost);

        ExtraGhosts.Clear();
    }

    internal static void Update(Player player)
    {
        UpdateVisibility();

        if (config.ShowGridDirections)
            ShowGridDirections();

        if (!PlacementGhost)
            return;

        Piece piece = Piece;
        int baseCost = piece.m_resources[0].m_amount;

        UpdateGridVersion();

        // Always update root ghost immediately, even though this results in the occasional double update per frame
        UpdateGhost(player, piece, 0, 0, 0);

        int totalGhosts = 1 + MaxActiveGhosts; // Root + extras
        int updatesThisFrame = Mathf.Min(config.GhostUpdateBatchSize, totalGhosts);

        for (int i = 0; i < updatesThisFrame; i++)
        {
            int ghostIndex = _ghostUpdateIndex % totalGhosts;
            _ghostUpdateIndex++;

            int row = ghostIndex / config.Columns;
            int column = ghostIndex % config.Columns;

            if (row >= config.Rows)
                continue;

            UpdatePieceCost(piece, ghostIndex, baseCost);
            UpdateGhost(player, piece, row, column, ghostIndex);
        }

        UpdatePieceCost(piece, 0, baseCost);
    }

    private static void UpdatePieceCost(Piece piece, int ghostIndex, int baseCost)
    {
        piece.m_resources[0].m_amount = baseCost * (ghostIndex + 1);
    }

    private static void UpdateVisibility()
    {
        bool active = PlacementGhost && PlacementGhost.activeSelf;

        for (int i = 0; i < ExtraGhosts.Count; i++)
        {
            bool shouldBeActive = active && i < MaxActiveGhosts;

            if (ExtraGhosts[i].activeSelf != shouldBeActive)
                ExtraGhosts[i].SetActive(shouldBeActive);
        }
    }

    private static void ShowGridDirections()
    {
        DirectionRenderer.SetActive(PlacementGhost.activeSelf);
        Vector3 vertex = BasePosition + Vector3.up * 0.5f;
        LineRenderers[0].SetPositions([vertex, vertex + (RowDirection * (ActualRows - 1))]);
        LineRenderers[1].SetPositions([vertex, vertex + (ColumnDirection * (ActualColumns - 1))]);
        // Debug purposes, show direction to snap origin
        LineRenderers[2].gameObject.SetActive(config.ShowSnapDirection);
        LineRenderers[2].SetPositions([vertex, vertex + SnapDirection * RowDirection.magnitude]);
    }

    private static void UpdateGridVersion()
    {
        bool changed = false;

        if ((BasePosition - _lastBasePosition).sqrMagnitude > 0.0001f)
        {
            _lastBasePosition = BasePosition;
            changed = true;
        }

        if (Quaternion.Angle(BaseRotation, _lastBaseRotation) > 1f)
        {
            _lastBaseRotation = BaseRotation;
            changed = true;
        }

        Quaternion effectiveRotation = SavedBaseRotation ?? BaseRotation;
        if (Quaternion.Angle(SavedBaseRotation ?? BaseRotation, _lastEffectiveRotation) > 0.1f)
        {
            _lastEffectiveRotation = effectiveRotation;
            changed = true;
        }

        if (config.Rows != _lastRows || config.Columns != _lastColumns)
        {
            _lastRows = config.Rows;
            _lastColumns = config.Columns;
            changed = true;
        }

        if (changed)
            _gridVersion++;
    }

    private static void UpdateGhost(Player player, Piece piece, int row, int column, int index)
    {
        GameObject ghost = GetGhostObject(index);
        GhostCache cache = GetGhostCache(ghost);

        bool hasCache = cache is not null;

        if (hasCache && cache.lastUpdatedVersion == _gridVersion)
            return;

        UpdateGhostTransform(ghost, GetGhostPosition(row, column, index));
        UpdateGhostStatus(player, piece, ghost, index);

        if (hasCache)
            cache.lastUpdatedVersion = _gridVersion;
    }

    private static GameObject GetGhostObject(int index)
    {
        return index == 0 ? PlacementGhost : ExtraGhosts[index - 1];
    }

    private static GhostCache GetGhostCache(GameObject ghost)
    {
        return ghost == PlacementGhost ? null : ghost.GetComponent<GhostCache>();
    }

    private static Vector3 GetGhostPosition(int row, int column, int index)
    {
        Vector3 pos = index == 0 ? BasePosition : BasePosition + RowDirection * row + ColumnDirection * column;

        Heightmap.GetHeight(pos, out float height);
        pos.y = height;

        return pos;
    }

    private static void UpdateGhostTransform(GameObject ghost, Vector3 position)
    {
        ghost.transform.position = position;
        ghost.transform.rotation = BaseRotation;
    }

    private static void UpdateGhostStatus(Player player, Piece piece, GameObject ghost, int index)
    {
        Status baseStatus =
            !player.m_noPlacementCost && !player.HaveRequirements(piece, Player.RequirementMode.CanBuild)
                ? Status.LackResources
                : Status.Healthy;

        Status finalStatus = GhostStatus.EvaluateStatus(ghost, baseStatus);

        ghost.GetComponent<Piece>().SetInvalidPlacementHeightlight(finalStatus != Status.Healthy);

        GhostPlacementStatus[index] = finalStatus;

        if (index == 0 && finalStatus == Status.Healthy)
        {
            if (config.HighlightRootPlacementGhost && GhostPlacementStatus.Count > 1)
            {
                MaterialMan.instance.SetValue(ghost, ShaderProps._Color, config.RootGhostHighlightColor);
                MaterialMan.instance.SetValue(ghost, ShaderProps._EmissionColor, config.RootGhostHighlightColor * 0.7f);
            }

            player.m_placementStatus = 0;
        }
    }
}
