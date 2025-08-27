namespace Advize_PlantEverything;

using System;
using System.Collections.Generic;
using static PlantEverything;

static class ConfigEventHandlers
{
    private static bool s_reInitQueueInProcess = false;
    private static bool s_isLocalConfigChange = false;
    private static bool s_reloadFromDisk = true;
    private static readonly HashSet<Action> s_reInitMethodSet = [];
    private static readonly Queue<Action> s_reInitMethodQueue = [];

    private static bool PerformingLocalConfigChange
    {
        get { return s_isLocalConfigChange; }
        set { Dbgl("Config change " + (value == true ? "is" : "was") + " local."); s_isLocalConfigChange = s_reloadFromDisk = value; }
    }

    private static bool ShouldQueueMethod => isDedicatedServer || (!isDedicatedServer && !config.IsSourceOfTruth && !PerformingLocalConfigChange);

    static void QueueReInitMethod(Action method)
    {
        if (!s_reInitQueueInProcess)
        {
            Dbgl("Beginning method queue.");
            s_reInitQueueInProcess = true;
        }
        // Check if the method is not already in the HashSet
        if (s_reInitMethodSet.Add(method)) // HashSet.Add returns false if the item is already present
        {
            Dbgl("Adding method to queue.");
            s_reInitMethodQueue.Enqueue(method);  // Only add to queue if it's not a duplicate
        }
    }

    internal static void ProcessReInitQueue()
    {
        if (s_reInitMethodQueue.Count == 0 && !isDedicatedServer && !config.IsSourceOfTruth && config.InitialSyncDone)
        {
            PerformingLocalConfigChange = true;
            return;
        }

        while (s_reInitMethodQueue.Count > 0)
        {
            var method = s_reInitMethodQueue.Dequeue();
            method.Invoke();
        }
        s_reInitMethodSet.Clear();
        s_reInitQueueInProcess = false;
    }

    internal static void CoreSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, scheduling re-initialization of mod");

        if (ShouldQueueMethod)
        {
            QueueReInitMethod(InitPieceRefs);
            QueueReInitMethod(InitPieces);
            QueueReInitMethod(InitSaplingRefs);
            QueueReInitMethod(InitSaplings);
            QueueReInitMethod(InitCrops);
            QueueReInitMethod(InitVines);
            QueueReInitMethod(InitCultivator);
            return;
        }

        InitPieceRefs();
        InitPieces();
        InitSaplingRefs();
        InitSaplings();
        InitCrops();
        InitVines();
        InitCultivator();

        PerformingLocalConfigChange = false;
    }

    internal static void PieceSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, scheduling re-initialization of pieces");

        if (ShouldQueueMethod)
        {
            QueueReInitMethod(InitPieceRefs);
            QueueReInitMethod(InitPieces);
            QueueReInitMethod(InitCultivator);
            return;
        }

        InitPieceRefs();
        InitPieces();
        InitCultivator();

        PerformingLocalConfigChange = false;
    }

    internal static void SaplingSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, scheduling re-initialization of saplings");

        if (ShouldQueueMethod)
        {
            QueueReInitMethod(InitSaplingRefs);
            QueueReInitMethod(InitSaplings);
            QueueReInitMethod(InitCultivator);
            return;
        }

        InitSaplingRefs();
        InitSaplings();
        InitCultivator();

        PerformingLocalConfigChange = false;
    }

    internal static void SeedSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, scheduling modification of TreeBase drop tables");

        if (ShouldQueueMethod)
        {
            QueueReInitMethod(ModifyTreeDrops);
            return;
        }

        ModifyTreeDrops();

        PerformingLocalConfigChange = false;
    }

    internal static void CropSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, scheduling re-initialization of crops");

        if (ShouldQueueMethod)
        {
            QueueReInitMethod(InitCrops);
            return;
        }

        InitCrops();

        PerformingLocalConfigChange = false;
    }

    internal static void VineSettingChanged(object o, EventArgs e)
    {
        Dbgl("Config setting changed, scheduling re-initialization of vines");

        if (ShouldQueueMethod)
        {
            QueueReInitMethod(InitVines);
            return;
        }

        InitVines();

        PerformingLocalConfigChange = false;
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
        if (ConfigWatcher.ExtraResourcesWatcher == null)
        {
            ConfigWatcher.InitExtraResourcesWatcher();
        }

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

    internal static void ConfigFileChanged(object sender, EventArgs e)
    {
        //Reloading does not override synced settings in MP
        if (s_reloadFromDisk)
        {
            Dbgl("s_reloadFromDisk: true");
            config.ReloadFromDisk();
        }
        else
        {
            Dbgl("s_reloadFromDisk: false");
            s_reloadFromDisk = true;
        }
    }
}
