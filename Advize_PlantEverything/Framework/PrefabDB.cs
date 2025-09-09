namespace Advize_PlantEverything;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal class PrefabDB
{
    internal string key;
    internal Heightmap.Biome biome;
    protected Dictionary<string, int> resources;
    internal int resourceCost;
    internal int resourceReturn;
    internal bool extraDrops;
    internal bool tolerateCold;
    internal bool tolerateHeat;
    internal bool icon;
    internal bool enabled = true;

    internal GameObject Prefab => StaticMembers.prefabRefs[key];

    internal KeyValuePair<string, int> Resource
    {
        get { return Resources.Count > 0 ? Resources.First() : new KeyValuePair<string, int>(Prefab.GetComponent<Pickable>().m_itemPrefab.name, resourceCost); }
        set { resources ??= []; resources.Add(value.Key, value.Value); }
    }

    internal Dictionary<string, int> Resources
    {
        get { return resources ?? []; }
        set { resources = value; }
    }
}

internal sealed class PieceDB : PrefabDB
{
    internal int respawnTime;
    internal bool recover;
    private Piece piece;
    internal List<Vector3> snapPoints;
    private string name;
    internal string pieceName;
    internal string pieceDescription;
    internal bool? isGrounded;
    internal bool? canBeRemoved;
    internal bool extraResource;
    internal bool hideWhenPicked;

    internal Piece Piece => piece ??= PluginUtils.CreatePiece(this);

    internal string Name
    {
        get { return name ?? key; }
        set { name = value; }
    }

    internal int ResourceCost
    {
        get { return resourceCost; }
        set { resourceCost = value; enabled = value != 0; }
    }
}

internal sealed class SaplingDB : PrefabDB
{
    internal string source;
    internal float growTime;
    internal float growRadius;
    internal float minScale;
    internal float maxScale;
    internal GameObject[] grownPrefabs;
}

internal sealed class ModdedPlantDB : PrefabDB
{
    //internal bool needCultivatedGround;
    internal float minScale;
    internal float maxScale;
    internal float growTime;
    internal float growTimeMax;
    internal float growRadius;
    //internal new DropTable extraDrops;
}
