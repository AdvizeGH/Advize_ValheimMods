namespace Advize_ColorfulVines;

using System;
using BepInEx.Logging;
using static StaticMembers;

static class ConfigEventHandlers
{
    internal static void ApplyVineConfigSettings(object o, EventArgs e)
    {
        //Remove piece if disabled
        PieceTable pieceTable = prefabRefs["Cultivator"].GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
        if (!config.EnableCustomVinePiece && pieceTable.m_pieces.Remove(prefabRefs["CV_VineAsh_sapling"]) && HoldingCultivator())
        {
            SheatheCultivator();
        }
        //Add piece if enabled
        if (config.EnableCustomVinePiece && !pieceTable.m_pieces.Contains(prefabRefs["CV_VineAsh_sapling"]))
        {
            if (HoldingCultivator()) SheatheCultivator();
            int index = pieceTable.m_pieces.IndexOf(prefabRefs["VineAsh_sapling"]);
            pieceTable.m_pieces.Insert(index + 1, prefabRefs["CV_VineAsh_sapling"]);
        }

        //Update colors on existing vines
        VineColor.UpdateColors();
        //Update custom piece icon
        IconUtils.UpdateVineIcon();

        static bool HoldingCultivator() => Player.m_localPlayer?.GetRightItem()?.m_dropPrefab == prefabRefs["Cultivator"];

        static void SheatheCultivator()
        {
            Dbgl("Cultivator updated through config change, unequipping cultivator.", level: LogLevel.Warning);
            if (!ZNet.instance.HaveStopped) Player.m_localPlayer.HideHandItems();
        }
    }

    internal static void UpdateLocalization(object sender, EventArgs e)
    {
        // Add or Replace keys in dictionary
        Localization.instance.AddWord($"cvVineAshSaplingName", config.CustomPieceName);
        Localization.instance.AddWord($"cvVineAshSaplingDescription", config.CustomPieceDescription);
        //Update cached keys and values for immediate effect
        Localization.instance.m_cache.Put("$cvVineAshSaplingName", config.CustomPieceName);
        Localization.instance.m_cache.Put("$cvVineAshSaplingDescription", config.CustomPieceDescription);
    }
}
