namespace Advize_PlantEasily;

using System.Collections.Generic;
using UnityEngine;

internal class ReplantDB
{
    internal static readonly Dictionary<string, ReplantDB> Registry = [];

    internal string PlantName { get; }
    internal Pickable Pickable { get; }

    internal ReplantDB(GameObject plantPrefab)
    {
        PlantName = plantPrefab.name;
        Pickable = plantPrefab.GetComponent<Plant>().m_grownPrefabs[0].GetComponent<Pickable>();

        Registry[PlantName] = this;
    }
}
