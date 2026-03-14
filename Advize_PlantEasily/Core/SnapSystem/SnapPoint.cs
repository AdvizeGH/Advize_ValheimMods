namespace Advize_PlantEasily;

using UnityEngine;

internal class SnapPoint
{
    public Vector3 pos;
    public Vector3 rowDir;
    public Vector3 colDir;
    public Vector3 origin;
    public bool isCardinal;

    // Internal use only when pre-allocating array
    internal SnapPoint() { }

    internal SnapPoint(Vector3 pos, Vector3 row, Vector3 col, Vector3 origin)
    {
        this.pos = pos;
        this.rowDir = row;
        this.colDir = col;
        this.origin = origin;
    }
}
