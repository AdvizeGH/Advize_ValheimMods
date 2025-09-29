namespace Advize_PlantEverything;

using System.IO;
using BepInEx;
using static PlantEverything;
using static StaticMembers;

sealed class ConfigWatcher
{
    internal static FileSystemWatcher ExtraResourcesWatcher = null;

    internal static void InitConfigWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, $"{PluginID}.cfg");
        watcher.Changed += ConfigEventHandlers.ConfigFileChanged;
        watcher.Created += ConfigEventHandlers.ConfigFileChanged;
        watcher.Renamed += ConfigEventHandlers.ConfigFileChanged;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
    }

    internal static void InitExtraResourcesWatcher()
    {
        ExtraResourcesWatcher = new(CustomConfigPath, "ExtraResources.json");
        ExtraResourcesWatcher.Changed += ConfigEventHandlers.ExtraResourcesFileOrSettingChanged;
        ExtraResourcesWatcher.Created += ConfigEventHandlers.ExtraResourcesFileOrSettingChanged;
        ExtraResourcesWatcher.Renamed += ConfigEventHandlers.ExtraResourcesFileOrSettingChanged;
        ExtraResourcesWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        ExtraResourcesWatcher.IncludeSubdirectories = true;
        ExtraResourcesWatcher.EnableRaisingEvents = true;
    }
}
