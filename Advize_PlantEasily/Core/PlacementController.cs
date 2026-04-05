namespace Advize_PlantEasily;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GhostGrid;
using static ModContext;

internal sealed class PlacementController : MonoBehaviour
{
    private static PlacementController _instance;

    internal static PlacementController Instance => _instance ??= Initialize();
    internal static bool IsPlanting => Instance._isPlanting;

    private readonly List<Renderer> _disabledRenderers = [];

    private bool _isPlanting;

    private static PlacementController Initialize()
    {
        GameObject go = new($"PlantEasily_PlacementController_{System.Guid.NewGuid()}")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        DontDestroyOnLoad(go);
        return go.AddComponent<PlacementController>();
    }

    internal void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    internal void StartBulkPlanting(GameObject piecePrefab)
    {
        StartCoroutine(BulkPlanting(piecePrefab));
    }

    private IEnumerator BulkPlanting(GameObject piecePrefab)
    {
        Player player = Player.m_localPlayer;
        _isPlanting = true;
        int count = 0;

        bool showGhosts = config.ShowGhostsDuringPlacement;

        // Hide either invalid ghosts or all ghosts if showGhosts is false
        foreach (GameObject ghost in ExtraGhosts)
        {
            bool shouldDisable = !showGhosts || !ValidExtraGhosts.Contains(ghost);
            if (shouldDisable)
                DisableRenderers(ghost);
        }

        // Tint valid ghosts
        if (showGhosts)
        {
            foreach (GameObject validGhost in ValidExtraGhosts)
                MaterialMan.instance.SetValue(validGhost, ShaderProps._Color, Color.gray);
        }

        //Plant stuff in batches
        foreach (GameObject go in ValidExtraGhosts)
        {
            count++;
            PlacePiece(player, go, piecePrefab);
            if (count % config.BulkPlantingBatchSize == 0) yield return null;
        }

        ValidExtraGhosts.Clear();
        _isPlanting = false;
        player.SetupPlacementGhost();
        ReEnableRenderers();
    }

    internal static void PlacePiece(Player player, GameObject go, GameObject piecePrefab)
    {
        Transform t = go.transform;
        Vector3 position = t.position;

        if (config.TryGetScatterRadius(out float radius))
        {
            position.x += Random.Range(-radius, radius);
            position.z += Random.Range(-radius, radius);
        }

        Quaternion rotation = config.RandomizeRotation ? Quaternion.Euler(0f, 22.5f * Random.Range(0, 16), 0f) : t.rotation;

        if (config.TryGetScatterAngle(out float angle))
        {
            rotation *= Quaternion.Euler(Random.Range(-angle, angle), 0f, Random.Range(-angle, angle));
        }

        go.SetActive(false);

        TerrainModifier.SetTriggerOnPlaced(trigger: true);
        GameObject clone = Instantiate(piecePrefab, position, rotation);
        TerrainModifier.SetTriggerOnPlaced(trigger: false);

        clone.GetComponent<Piece>().SetCreator(player.GetPlayerID());

        Game.instance.IncrementPlayerStat(PlayerStatType.Builds);
        player.RaiseSkill(Skills.SkillType.Farming, 1f);
    }

    private void DisableRenderers(GameObject go)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
            _disabledRenderers.Add(r);
        }
    }

    private void ReEnableRenderers()
    {
        foreach (Renderer r in _disabledRenderers)
            r.enabled = true;
        _disabledRenderers.Clear();
    }
}
