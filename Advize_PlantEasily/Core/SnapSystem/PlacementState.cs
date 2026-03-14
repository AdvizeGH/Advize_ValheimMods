namespace Advize_PlantEasily;

using System.Collections.Generic;
using UnityEngine;
using static ModContext;
using static ModUtils;

internal static class PlacementState
{
    internal static Vector3 BasePosition;
    internal static Vector3 RowDirection;
    internal static Vector3 ColumnDirection;
    internal static Quaternion BaseRotation;
    internal static Quaternion FixedRotation;
    internal static bool AltPlacement;

    internal static GameObject PlacementGhost;
    internal static Piece Piece;
    internal static Plant Plant;
    internal static string LastSelectedPlantName = ""; // only used for replant on harvest

    internal static Vector3 SnapDirection;

    internal static Vector3 SavedRowDirection;
    internal static Vector3 SavedColumnDirection;
    internal static Quaternion? SavedBaseRotation;

    internal static void SetReferences(GameObject rootGhost)
    {
        PlacementGhost = rootGhost;
        Plant = rootGhost.GetComponent<Plant>();
        Piece = rootGhost.GetComponent<Piece>();

        LastSelectedPlantName = null;

        if (!Plant)
            return;

        foreach (KeyValuePair<string, ReplantDB> kvp in ReplantDB.Registry)
        {
            if (kvp.Value.PlantName == rootGhost.name)
            {
                LastSelectedPlantName = kvp.Key;
                break;
            }
        }
    }

    internal static void Update(Vector3 playerPosition)
    {
        UpdateAltPlacementMode();
        UpdateBaseTransform();
        UpdateFixedRotation();
        UpdateGridDirectionsAndSnapping(playerPosition);
    }

    private static void UpdateAltPlacementMode()
    {
        if (config.ForceAltPlacement)
        {
            AltPlacement = true;
            return;
        }

        bool usingGamepad = ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive();

        if (usingGamepad)
        {
            AltPlacement = Player.m_localPlayer.m_altPlace;
            return;
        }

        bool altKey = ZInput.GetButton("AltPlace");
        bool altJoy = ZInput.GetButton("JoyAltPlace") && !ZInput.GetButton("JoyRotate");

        AltPlacement = altKey || altJoy;
    }

    private static void UpdateBaseTransform()
    {
        BasePosition = PlacementGhost.transform.position;
        BaseRotation = PlacementGhost.transform.rotation;
    }

    private static void UpdateFixedRotation()
    {
        FixedRotation = BaseRotation;

        Vector3 euler = FixedRotation.eulerAngles;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        FixedRotation.eulerAngles = euler;
    }

    private static void UpdateGridDirectionsAndSnapping(Vector3 playerPosition)
    {
        float pieceSpacing = GetPieceSpacing(PlacementGhost);

        ResetGridDirections();

        if (config.SnapActive)
        {
            if (SnapSystem.FindSnapPoints(pieceSpacing))
                return;

            ResetGridDirections();
        }

        RowDirection = BaseRotation * RowDirection * pieceSpacing;
        ColumnDirection = BaseRotation * ColumnDirection * pieceSpacing;

        void ResetGridDirections()
        {
            // Subtracts player position from placement ghost position to get a vector between the two and facing out from the player, normalizes it
            RowDirection = config.GloballyAlignGridDirections ? Vector3.forward : Utils.DirectionXZ(BasePosition - playerPosition);
            // Cross product of a vertical vector and the forward facing normalized vector, producing a perpendicular lateral vector
            ColumnDirection = Vector3.Cross(Vector3.up, RowDirection);
        }
    }
}
