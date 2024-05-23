namespace Advize_PlantEverything;

using System;
using static PlantEverything;

static class ConfigEventHandlers
{
    internal static void CoreSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing mod");
        InitPieceRefs();
        InitPieces();
        InitSaplingRefs();
        InitSaplings();
        InitCrops();
        InitVines();
        InitCultivator();
    }

    internal static void PieceSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing pieces");
        InitPieceRefs();
        InitPieces();
        InitCultivator();
    }

    internal static void SaplingSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing saplings");
        InitSaplingRefs();
        InitSaplings();
        InitCultivator();
    }

    internal static void SeedSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, modifying TreeBase drop tables");
        ModifyTreeDrops();
    }

    internal static void CropSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing crops");
        InitCrops();
    }

    internal static void VineSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, re-initializing vines");
        InitVines();
    }

    //CustomSyncedValue value changed event handler
    internal static void ExtraResourcesChanged()
    {
        Dbgl("ExtraResourcesChanged");
        //Dbgl($"deserializedExtraResources.Count is currently {deserializedExtraResources.Count}");
        //Dbgl($"config.SyncedExtraResources.Count is currently {config.SyncedExtraResources.Value.Count}");

        deserializedExtraResources.Clear();
        foreach (string s in config.SyncedExtraResources.Value)
        {
            ExtraResource er = PluginUtils.DeserializeExtraResource(s);
            deserializedExtraResources.Add(er);
            //Dbgl($"er2 {er.prefabName}, {er.resourceName}, {er.resourceCost}, {er.groundOnly}");
        }

        //Dbgl($"deserializedExtraResources.Count is now {deserializedExtraResources.Count}");

        //Dbgl("Attempting to call InitExtraResources");
        if (ZNetScene.s_instance)
        {
            //Dbgl("Calling InitExtraResources");
            InitExtraResourceRefs(ZNetScene.s_instance);
            PieceSettingChanged(null, null);
        }
    }

    //Tidy up these events related to extra resources in followup updates, code flow is getting ridiculous. Clean up all the log messages too while you're at it.
    internal static void ExtraResourcesFileOrSettingChanged(object sender, EventArgs e)
    {
        Dbgl($"ExtraResources file or setting has changed");
        if (config.IsSourceOfTruth)
        {
            if (config.EnableExtraResources)
            {
                Dbgl("IsSourceOfTruth: true, loading extra resources from disk");
                PluginUtils.LoadExtraResources();
            }
            else
            {
                config.SyncedExtraResources.AssignLocalValue([]);
            }
        }
        else
        {
            Dbgl("IsSourceOfTruth: false, extra resources will not be loaded from disk");
            // Currently if a client changes their local ExtraResources.cfg while on a server, their new data won't be loaded.
            // If they then leave server and join a single player game their originally loaded ExtraResources.cfg data is used, not the updated file.
        }
    }
}