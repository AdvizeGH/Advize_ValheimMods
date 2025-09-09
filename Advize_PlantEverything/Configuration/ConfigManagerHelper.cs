namespace Advize_PlantEverything;

using System;
using System.Linq;
using System.Reflection;
using static ConfigEventHandlers;

#nullable enable
internal class ConfigManagerHelper
{
    private static object? _configManagerInstance;
    private static Type? _configManagerType;
    private static MethodInfo? _buildSettingList;
    private static PropertyInfo? _displayingWindowProperty;

    internal static void Initialize()
    {
        Assembly? configManagerAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");

        _configManagerType = configManagerAssembly?.GetType("ConfigurationManager.ConfigurationManager");
        _configManagerInstance = _configManagerType == null ? null : BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(_configManagerType);
        _buildSettingList = _configManagerType?.GetMethod("BuildSettingList");
        _displayingWindowProperty = _configManagerType?.GetProperty("DisplayingWindow");

        // Setup Window Closed Event Subscription;
        if (_configManagerInstance?.GetType().GetEvent("DisplayingWindowChanged") is EventInfo eventInfo)
        {
            EventHandler handler = new(ConfigManagerDisplayingWindowChanged);
            Delegate delegateInstance = Delegate.CreateDelegate(eventInfo.EventHandlerType!, handler.Target, handler.Method);
            eventInfo.AddEventHandler(_configManagerInstance, delegateInstance);
        }
    }

    internal static void ReloadConfigDisplay() => _buildSettingList?.Invoke(_configManagerInstance, []);

    internal static bool IsConfigManagerWindowActive => _displayingWindowProperty?.GetValue(_configManagerInstance) is bool isActive && isActive;
}
