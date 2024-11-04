namespace Advize_PlantEasily;
using UnityEngine;

internal class ReplantDB(GameObject plantPrefab)
{
    internal string plantName = plantPrefab.name;
    internal Pickable pickable = plantPrefab.GetComponent<Plant>().m_grownPrefabs[0].GetComponent<Pickable>();
}
