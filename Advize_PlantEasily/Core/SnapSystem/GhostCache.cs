namespace Advize_PlantEasily;

using UnityEngine;

internal sealed class GhostCache : MonoBehaviour
{
    internal Piece piece;
    internal Plant plant;
    internal int lastUpdatedVersion = -1;

    public void Init()
    {
        piece = GetComponent<Piece>();
        plant = GetComponent<Plant>();
    }
}
