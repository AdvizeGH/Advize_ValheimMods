namespace Advize_PlantEasily;

using BepInEx.Configuration;
using UnityEngine;
using static ModContext;

internal class PickableDB
{
    private static bool initialized = false;

    internal readonly string key;

    internal ConfigEntry<float> configEntry;
    internal string itemName;
    private Piece piece;

    internal GameObject Prefab => PrefabRefs[key];
    internal Piece Piece => piece ??= Prefab?.GetComponent<Piece>();

    internal PickableDB(string k) => key = k;

    internal static void InitPickableSpacingConfig()
    {
        if (PrefabRefs.Count == 0) return;

        if (!initialized)
        {
            config.BindPickableSpacingSettings();
            initialized = true;
        }

        PickableRefs.ForEach(pdb => pdb.Piece.m_harvestRadius = pdb.configEntry.Value);
        Player.m_localPlayer?.SetupPlacementGhost();
    }
}
