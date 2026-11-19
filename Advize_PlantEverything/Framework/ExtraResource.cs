namespace Advize_PlantEverything;

internal struct ExtraResource
{
    public string prefabName;
    public string resourceName;
    public int resourceCost;
    public bool groundOnly;
    public string pieceName;
    public string pieceDescription;

    // Spawner fields. When spawnerName is set the mod builds a new prefab based
    // on prefabName instead of referencing prefabName directly.
    public string spawnerName;
    public string dropItem;
    public float respawnTime;
    public int amount;
    public string fullVisual;
    public string emptyVisual;

    internal readonly bool IsSpawner => !string.IsNullOrEmpty(spawnerName);

    internal readonly bool IsValid()
    {
        if (prefabName == default || prefabName.StartsWith("PE_Fake")) return false;
        if (resourceName == default || resourceCost == default) return false;

        if (IsSpawner)
        {
            if (spawnerName.StartsWith("PE_Fake")) return false;
            if (dropItem == default) return false;
            if (!SpawnerFactory.IsValidName(spawnerName)) return false;
        }

        return true;
    }
}
