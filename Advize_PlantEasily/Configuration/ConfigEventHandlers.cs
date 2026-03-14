namespace Advize_PlantEasily;

using System;
using UnityEngine;
using static ModContext;

internal static class ConfigEventHandlers
{
    internal static void GridSizeChanged(object sender, EventArgs e) => GhostGrid.ResizeGrid();

    internal static void KeybindsChanged(object sender, EventArgs e) => KeyHintPatches.UpdateKeyHintText();

    internal static void GridSpacingChanged(object sender, EventArgs e) => PickableDB.InitPickableSpacingConfig();

    internal static void GridColorChanged(object sender, EventArgs e)
    {
        if (!GhostGrid.DirectionRenderer) return;

        GhostGrid.LineRenderers[0].startColor = config.RowStartColor;
        GhostGrid.LineRenderers[0].endColor = config.RowEndColor;
        GhostGrid.LineRenderers[1].startColor = config.ColumnStartColor;
        GhostGrid.LineRenderers[1].endColor = config.ColumnEndColor;
        GhostGrid.LineRenderers[2].startColor = config.SnapStartColor;
        GhostGrid.LineRenderers[2].endColor = config.SnapEndColor;
    }
}
