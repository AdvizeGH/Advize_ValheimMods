using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System;
using HarmonyLib;
using UnityEngine.Events;

namespace Advize_CartographySkill.Configuration
{
    public class Config
    {
        private static Config _instance = null;
        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Config();
                }
                return _instance;
            }
            set { }
        }
        public static ZNet ZNet => ZNet.instance;
        public Dictionary<string, ConfigBaseEntry> _configEntries;
        public BaseUnityPlugin _mod;

        public static string GUID => Instance._mod.Info.Metadata.GUID;
        public static string RPC_SYNC_GUID => "AuthoritativeConfig_" + GUID;
        private static BepInEx.Configuration.ConfigEntry<bool> _ServerIsAuthoritative;
        private static bool _DefaultBindAuthority;
        public static BepInEx.Logging.ManualLogSource Logger;

        public UnityEvent OnConfigReceived = new UnityEvent();

        public void Init(BaseUnityPlugin mod, bool defaultBindServerAuthority = false)
        {
            _mod = mod;
            //logger
            Logger = new BepInEx.Logging.ManualLogSource(RPC_SYNC_GUID);
            BepInEx.Logging.Logger.Sources.Add(Logger);

            _configEntries = new Dictionary<string, ConfigBaseEntry>();
            _DefaultBindAuthority = defaultBindServerAuthority;
            _ServerIsAuthoritative = _mod.Config.Bind("ServerAuthoritativeConfig", "ServerIsAuthoritative", true, "<Server Only> Forces Clients to use Server defined configs.");
            Harmony.CreateAndPatchAll(typeof(Config));
            Logger.LogInfo("Initialized Server Authoritative Config Manager.");
        }

        #region Harmony_Hooks
        [HarmonyPatch(typeof(Game), "Start")]
        [HarmonyPostfix]
        private static void RegisterSyncConfigRPC()
        {
            Logger.LogInfo($"Authoritative Config Registered -> {RPC_SYNC_GUID}");
            ZRoutedRpc.instance.Register(RPC_SYNC_GUID, new Action<long, ZPackage>(RPC_SyncServerConfig));
            //clear server values
            foreach (ConfigBaseEntry entry in Instance._configEntries.Values)
            {
                entry.ClearServerValue();
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
        [HarmonyPostfix]
        private static void RequestConfigFromServer()
        {
            if (!ZNet.IsServer() && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
            {
                long? serverPeerID = AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID").Invoke(ZRoutedRpc.instance, null) as long?;
                ZRoutedRpc.instance.InvokeRoutedRPC((long)serverPeerID, RPC_SYNC_GUID, new object[] { new ZPackage() });
                Logger.LogInfo($"Authoritative Config Registered -> {RPC_SYNC_GUID}");
                Debug.Log(Instance._mod.Info.Metadata.Name + ": Authoritative Config Requested -> " + RPC_SYNC_GUID);
            }
            else if (!ZNet.IsServer())
            {
                Logger.LogWarning($"Failed to Request Configs. Bad Peer? Too Early?");
            }
        }
        #endregion

        #region Bind_Impl
        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, ConfigDescription configDescription = null, bool? serverAuthoritative = null)
        {
            ConfigEntry<T> entry = new ConfigEntry<T>(_mod.Config.Bind(section, key, defaultValue, configDescription), serverAuthoritative != null ? (bool)serverAuthoritative : _DefaultBindAuthority);
            _configEntries[entry.BaseEntry.Definition.ToString()] = entry;
            return entry;
        }

        public ConfigEntry<T> Bind<T>(ConfigDefinition configDefinition, T defaultValue, ConfigDescription configDescription = null, bool? serverAuthoritative = null)
        {
            ConfigEntry<T> entry = new ConfigEntry<T>(_mod.Config.Bind(configDefinition, defaultValue, configDescription), serverAuthoritative != null ? (bool)serverAuthoritative : _DefaultBindAuthority);
            _configEntries[entry.BaseEntry.Definition.ToString()] = entry;
            return entry;
        }

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description, bool? serverAuthoritative = null)
        {
            ConfigEntry<T> entry = new ConfigEntry<T>(_mod.Config.Bind(section, key, defaultValue, description), serverAuthoritative != null ? (bool)serverAuthoritative : _DefaultBindAuthority);
            _configEntries[entry.BaseEntry.Definition.ToString()] = entry;
            return entry;
        }
        #endregion

        #region RPC
        public static void SendConfigToClient(long sender)
        {
            if (ZNet.IsServer())
            {
                ZPackage pkg = new ZPackage();
                int entries = 0;
                foreach (var item in Instance._configEntries)
                {
                    if (item.Value.ServerAuthoritative)
                    {
                        pkg.Write(item.Key);
                        pkg.Write(item.Value.BaseEntry.GetSerializedValue());
                        entries++;
                        //LogInfo($"Sending Config {item.Key}: {item.Value.BaseEntry.GetSerializedValue()}");
                    }
                }
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, RPC_SYNC_GUID, new object[] { pkg });
                Logger.LogInfo($"Sent {entries} config pairs to client {sender}");
            }
        }

        public static void ReadConfigPkg(ZPackage pkg)
        {
            if (!ZNet.IsServer())
            {
                int entries = 0;
                while (pkg.GetPos() != pkg.Size())
                {
                    string configKey = pkg.ReadString();
                    string stringVal = pkg.ReadString();
                    entries++;
                    if (Instance._configEntries.ContainsKey(configKey))
                    {
                        Instance._configEntries[configKey].SetSerializedValue(stringVal);
                        //Logger.LogInfo($"Applied Server Authoritative config pair => {configKey}: {stringVal}");
                    }
                    else
                    {
                        Logger.LogError($"Recieved config key we dont have locally. Possible Version Mismatch. {configKey}: {stringVal}");
                    }
                }
                Logger.LogInfo($"Applied {entries} config pairs");
                UnityEvent onConfigReceived = Instance.OnConfigReceived;
                if (onConfigReceived != null)
                {
                    onConfigReceived.Invoke();
                }
            }
        }

        public static void RPC_SyncServerConfig(long sender, ZPackage pkg)
        {
            if (ZNet.IsServer() && _ServerIsAuthoritative.Value)
            {
                SendConfigToClient(sender);
            }
            else if (!ZNet.IsServer() && pkg != null && pkg.Size() > 0)
            {
                //Only read configs from the server.
                long? serverPeerID = AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID").Invoke(ZRoutedRpc.instance, null) as long?;
                if (serverPeerID == sender)
                {
                    //Client handle recieving config
                    ReadConfigPkg(pkg);
                }
            }
        }
        #endregion
    }
}