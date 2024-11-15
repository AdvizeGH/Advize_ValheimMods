namespace Advize_PlantEasily;

using BepInEx.Configuration;
using UnityEngine;
using static PlantEasily;

internal class PickableDB
{
    private static bool initialized = false;

    internal string key;
    internal string itemName;
    private Piece piece;
    internal ConfigEntry<float> configEntry;

    internal GameObject Prefab => prefabRefs[key];
    internal Piece Piece => piece ??= Prefab?.GetComponent<Piece>();

    internal PickableDB(string k) => key = k;

    internal static void InitPickableSpacingConfig()
    {
        if (prefabRefs.Count == 0) return;

        if (!initialized)
        {
            config.BindPickableSpacingSettings();
            initialized = true;
        }

        pickableRefs.ForEach(pdb => pdb.Piece.m_harvestRadius = pdb.configEntry.Value);
        Player.m_localPlayer?.SetupPlacementGhost();
    }
}
