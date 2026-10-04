using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace ConditionalConfigSyncAPI
{
    /// <summary>
    /// Mirrors the stable CCS config-ownership mode names without referencing the CCS assembly.
    /// </summary>
    public enum SyncMode
    {
        /// <summary>The server always owns and synchronizes the setting when CCS is active.</summary>
        AlwaysServerControlled,

        /// <summary>CCS policy may choose server or client ownership when CCS is active.</summary>
        Conditional,

        /// <summary>Each client always owns the setting; CCS never synchronizes it from the server.</summary>
        AlwaysClientControlled,
    }

    /// <summary>
    /// Mirrors the stable CCS mod-requirement mode names without referencing the CCS assembly.
    /// </summary>
    public enum RequirementMode
    {
        /// <summary>The author-defined mod requirement cannot be overridden by server policy.</summary>
        Fixed,

        /// <summary>Server policy may override the author-defined requirement for incoming clients.</summary>
        Conditional,
    }

    /// <summary>
    /// Optional adapter for mods that work without CCS but use it automatically when the standalone CCS plugin is active.
    /// </summary>
    /// <remarks>
    /// This adapter always binds ordinary BepInEx ConfigEntry instances. When CCS is available, the same entries are
    /// registered with the active CCS runtime through its reflection bridge. When CCS is absent or incompatible, the
    /// entries remain normal local BepInEx settings and the owning mod continues to work without CCS.
    /// </remarks>
    public sealed class ConfigSync
    {
        /// <summary>BepInEx plugin GUID used for the optional CCS soft dependency.</summary>
        public const string PluginGuid = "_shudnal.ConditionalConfigSync";

        /// <summary>Minimum CCS package version that exposes the supported optional integration bridge.</summary>
        public const string MinimumCcsVersion = "1.0.10";

        private static readonly System.Version MinimumSupportedCcsVersion = new(MinimumCcsVersion);

        private const string BridgeTypeName = "ConditionalConfigSync.SoftDependencyBridge";
        private const int MinimumBridgeApiVersion = 1;

        private readonly string _name;
        private readonly string _displayName;
        private readonly string _currentVersion;
        private readonly string _minimumRequiredVersion;
        private readonly bool _modRequired;
        private readonly RequirementMode _requirementMode;
        private readonly bool _allowClientConfigUpdatesWhenUnlocked;
        private readonly ManualLogSource _logger;

        private Type _bridgeType;
        private PropertyInfo _apiVersionProperty;
        private PropertyInfo _runtimeReadyProperty;
        private MethodInfo _createConfigSyncMethod;
        private MethodInfo _registerConfigEntryMethod;
        private MethodInfo _registerLockingConfigEntryMethod;
        private object _configSyncInstance;

        private bool _permanentFailure;
        private bool _permanentFailureLogged;
        private string _failureReason;

        /// <summary>
        /// Creates one optional synchronization context for a mod.
        /// </summary>
        public ConfigSync(
            string name,
            string displayName,
            string currentVersion,
            string minimumRequiredVersion = null,
            bool modRequired = false,
            RequirementMode requirementMode = RequirementMode.Fixed,
            bool allowClientConfigUpdatesWhenUnlocked = false,
            ManualLogSource logger = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A stable mod GUID is required.", nameof(name));

            _name = name;
            _displayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName;
            _currentVersion = currentVersion;
            _minimumRequiredVersion = minimumRequiredVersion;
            _modRequired = modRequired;
            _requirementMode = requirementMode;
            _allowClientConfigUpdatesWhenUnlocked = allowClientConfigUpdatesWhenUnlocked;
            _logger = logger;
        }

        /// <summary>
        /// True when the active BepInEx CCS plugin exposes a compatible bridge and its runtime is ready.
        /// </summary>
        public bool IsAvailable
        {
            get { return EnsureBridge() && IsRuntimeReady(); }
        }

        /// <summary>
        /// True after this adapter has created its CCS consumer context.
        /// </summary>
        public bool IsActive
        {
            get { return _configSyncInstance != null; }
        }

        /// <summary>
        /// Last permanent compatibility or initialization failure. CCS simply being absent is not a failure.
        /// </summary>
        public string FailureReason
        {
            get { return _failureReason; }
        }

        /// <summary>
        /// Creates the CCS consumer context when possible. Returns false and keeps local BepInEx behavior otherwise.
        /// </summary>
        public bool TryActivate()
        {
            return EnsureContext();
        }

        /// <summary>
        /// Binds a normal BepInEx config entry and registers the same entry with CCS when available.
        /// </summary>
        public ConfigEntry<T> Bind<T>(
            ConfigFile configFile,
            string section,
            string key,
            T defaultValue,
            string description,
            SyncMode syncMode = SyncMode.Conditional,
            bool serverControlledByDefault = true)
        {
            if (configFile == null)
                throw new ArgumentNullException(nameof(configFile));

            ConfigEntry<T> entry = configFile.Bind(section, key, defaultValue, description);
            RegisterConfigEntry(entry, syncMode, serverControlledByDefault);
            return entry;
        }

        /// <summary>
        /// Binds a normal BepInEx config entry and registers the same entry with CCS when available.
        /// </summary>
        public ConfigEntry<T> Bind<T>(
            ConfigFile configFile,
            string section,
            string key,
            T defaultValue,
            ConfigDescription description,
            SyncMode syncMode = SyncMode.Conditional,
            bool serverControlledByDefault = true)
        {
            if (configFile == null)
                throw new ArgumentNullException(nameof(configFile));

            ConfigEntry<T> entry = configFile.Bind(section, key, defaultValue, description);
            RegisterConfigEntry(entry, syncMode, serverControlledByDefault);
            return entry;
        }

        /// <summary>
        /// Registers an already-bound config entry with CCS when available.
        /// </summary>
        /// <returns>True when the entry was registered with CCS; false when it remains local-only.</returns>
        public bool RegisterConfigEntry<T>(
            ConfigEntry<T> entry,
            SyncMode syncMode = SyncMode.Conditional,
            bool serverControlledByDefault = true)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (!EnsureContext())
                return false;

            try
            {
                Invoke(
                    _registerConfigEntryMethod,
                    null,
                    new object[]
                    {
                        _configSyncInstance,
                        entry,
                        syncMode.ToString(),
                        serverControlledByDefault,
                    });
                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Failed to register config entry '" + entry.Definition.Section + " -> " + entry.Definition.Key +
                    "' with Conditional Config Sync. The entry remains a normal local BepInEx setting. " +
                    GetInnermostMessage(exception));
                return false;
            }
        }

        /// <summary>
        /// Registers an already-bound entry as the protected CCS locking setting when available.
        /// </summary>
        /// <returns>True when the entry was registered with CCS; false when it remains local-only.</returns>
        public bool RegisterLockingConfigEntry<T>(ConfigEntry<T> entry) where T : IConvertible
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (!EnsureContext())
                return false;

            try
            {
                Invoke(
                    _registerLockingConfigEntryMethod,
                    null,
                    new object[] { _configSyncInstance, entry });
                return true;
            }
            catch (Exception exception)
            {
                LogWarning(
                    "Failed to register locking config entry '" + entry.Definition.Section + " -> " + entry.Definition.Key +
                    "' with Conditional Config Sync. The entry remains a normal local BepInEx setting. " +
                    GetInnermostMessage(exception));
                return false;
            }
        }

        /// <summary>
        /// Binds a normal BepInEx entry and registers it as the protected CCS locking setting when available.
        /// </summary>
        public ConfigEntry<T> BindLocking<T>(
            ConfigFile configFile,
            string section,
            string key,
            T defaultValue,
            string description) where T : IConvertible
        {
            if (configFile == null)
                throw new ArgumentNullException(nameof(configFile));

            ConfigEntry<T> entry = configFile.Bind(section, key, defaultValue, description);
            RegisterLockingConfigEntry(entry);
            return entry;
        }

        /// <summary>
        /// Binds a normal BepInEx entry and registers it as the protected CCS locking setting when available.
        /// </summary>
        public ConfigEntry<T> BindLocking<T>(
            ConfigFile configFile,
            string section,
            string key,
            T defaultValue,
            ConfigDescription description) where T : IConvertible
        {
            if (configFile == null)
                throw new ArgumentNullException(nameof(configFile));

            ConfigEntry<T> entry = configFile.Bind(section, key, defaultValue, description);
            RegisterLockingConfigEntry(entry);
            return entry;
        }

        private bool EnsureContext()
        {
            if (_configSyncInstance != null)
                return true;

            if (_permanentFailure || !EnsureBridge() || !IsRuntimeReady())
                return false;

            try
            {
                _configSyncInstance = Invoke(
                    _createConfigSyncMethod,
                    null,
                    new object[]
                    {
                        _name,
                        _displayName,
                        _currentVersion,
                        _minimumRequiredVersion,
                        _modRequired,
                        _requirementMode.ToString(),
                        _allowClientConfigUpdatesWhenUnlocked,
                    });

                if (_configSyncInstance == null)
                {
                    SetPermanentFailure("Conditional Config Sync returned no consumer context.", null);
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                SetPermanentFailure("Failed to create the Conditional Config Sync consumer context.", exception);
                return false;
            }
        }

        private bool EnsureBridge()
        {
            if (_bridgeType != null)
                return true;

            if (_permanentFailure)
                return false;

            try
            {
                if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out var pluginInfo)
                    || pluginInfo == null
                    || pluginInfo.Instance == null)
                {
                    return false;
                }

                // BepInEx 5 cannot express a minimum version while keeping a dependency soft:
                // the versioned BepInDependency constructor is always a hard dependency.
                // Treat older CCS installs exactly like CCS being absent so the owning mod keeps local config behavior.
                if (pluginInfo.Metadata == null
                    || pluginInfo.Metadata.Version == null
                    || pluginInfo.Metadata.Version < MinimumSupportedCcsVersion)
                {
                    return false;
                }

                // Resolve through the actual BepInEx plugin instance, never by scanning AppDomain assemblies by name.
                // This avoids passive duplicate assemblies loaded by metadata scanners such as AzuAntiCheat.
                Assembly pluginAssembly = pluginInfo.Instance.GetType().Assembly;
                Type bridgeType = pluginAssembly.GetType(BridgeTypeName, throwOnError: false);
                if (bridgeType == null)
                {
                    SetPermanentFailure(
                        "The installed Conditional Config Sync version does not expose the optional soft-dependency bridge.",
                        null);
                    return false;
                }

                PropertyInfo apiVersionProperty = bridgeType.GetProperty(
                    "ApiVersion",
                    BindingFlags.Static | BindingFlags.Public);
                PropertyInfo runtimeReadyProperty = bridgeType.GetProperty(
                    "IsRuntimeReady",
                    BindingFlags.Static | BindingFlags.Public);
                MethodInfo createConfigSyncMethod = bridgeType.GetMethod(
                    "CreateConfigSync",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(string),
                        typeof(string),
                        typeof(string),
                        typeof(bool),
                        typeof(string),
                        typeof(bool),
                    },
                    null);
                MethodInfo registerConfigEntryMethod = bridgeType.GetMethod(
                    "RegisterConfigEntry",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(object), typeof(ConfigEntryBase), typeof(string), typeof(bool) },
                    null);
                MethodInfo registerLockingConfigEntryMethod = bridgeType.GetMethod(
                    "RegisterLockingConfigEntry",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(object), typeof(ConfigEntryBase) },
                    null);

                if (apiVersionProperty == null
                    || runtimeReadyProperty == null
                    || createConfigSyncMethod == null
                    || registerConfigEntryMethod == null
                    || registerLockingConfigEntryMethod == null)
                {
                    SetPermanentFailure(
                        "The installed Conditional Config Sync soft-dependency bridge is incomplete or incompatible.",
                        null);
                    return false;
                }

                object apiVersionValue = apiVersionProperty.GetValue(null, null);
                if (!(apiVersionValue is int apiVersion) || apiVersion < MinimumBridgeApiVersion)
                {
                    SetPermanentFailure(
                        "The installed Conditional Config Sync soft-dependency bridge version is not supported.",
                        null);
                    return false;
                }

                _bridgeType = bridgeType;
                _apiVersionProperty = apiVersionProperty;
                _runtimeReadyProperty = runtimeReadyProperty;
                _createConfigSyncMethod = createConfigSyncMethod;
                _registerConfigEntryMethod = registerConfigEntryMethod;
                _registerLockingConfigEntryMethod = registerLockingConfigEntryMethod;
                return true;
            }
            catch (Exception exception)
            {
                SetPermanentFailure("Failed to resolve the Conditional Config Sync soft-dependency bridge.", exception);
                return false;
            }
        }

        private bool IsRuntimeReady()
        {
            try
            {
                object value = _runtimeReadyProperty.GetValue(null, null);
                return value is bool ready && ready;
            }
            catch (Exception exception)
            {
                SetPermanentFailure("Failed to query the Conditional Config Sync runtime state.", exception);
                return false;
            }
        }

        private static object Invoke(MethodInfo method, object target, object[] arguments)
        {
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private void SetPermanentFailure(string message, Exception exception)
        {
            _permanentFailure = true;
            _failureReason = exception == null
                ? message
                : message + " " + GetInnermostMessage(exception);

            if (_permanentFailureLogged)
                return;

            _permanentFailureLogged = true;
            LogWarning(_failureReason);
        }

        private void LogWarning(string message)
        {
            if (_logger != null)
                _logger.LogWarning(message);
        }

        private static string GetInnermostMessage(Exception exception)
        {
            Exception current = exception;
            while (current.InnerException != null)
                current = current.InnerException;

            return current.Message;
        }
    }
}
