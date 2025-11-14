namespace Advize_Armoire;

using System;
using System.Collections.Generic;
using HarmonyLib;
using static StaticMembers;

[HarmonyPatch]
static class VersionHandshake
{
    static readonly List<ZRpc> ValidatedPeers = [];
    static string ConnectionError = string.Empty;
    static bool ClientVersionValidated = false;

    static void RPC_ArmoireVersionCheck(ZRpc rpc, ZPackage pkg)
    {
        string receivedVersion = pkg.ReadString();
        bool isServer = ZNet.instance.IsServer();

        Dbgl($"Version check, local: {Armoire.Version},  remote: {receivedVersion}");

        if (receivedVersion != Armoire.Version)
        {
            ConnectionError = $"{Armoire.PluginName} Installed: {Armoire.Version}\n Needed: {receivedVersion}";

            if (isServer)
            {
                // Version mismatch, disconnect client from server
                Dbgl($"Peer ({rpc.m_socket.GetHostName()}) has incompatible version, disconnecting...", forceLog: true, level: BepInEx.Logging.LogLevel.Warning);
                rpc.Invoke("Error", 3);
            }
            
            return;
        }
        
        if (isServer)
        {
            Dbgl($"Adding peer ({rpc.m_socket.GetHostName()}) to validated list");
            ValidatedPeers.Add(rpc);
        }
        else
        {
            Dbgl("Received same version from server!");
            ClientVersionValidated = true;
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
    [HarmonyPrefix]
    static void InitiateVersionHandshake(ZNetPeer peer)
    {
        string rpcName = $"{Armoire.PluginName}VersionCheck";

        Dbgl("Registering version RPC handler");
        peer.m_rpc.Register(rpcName, new Action<ZRpc, ZPackage>(RPC_ArmoireVersionCheck));

        Dbgl("Invoking version check");
        ZPackage zpkg = new();
        zpkg.Write(Armoire.Version);
        peer.m_rpc.Invoke(rpcName, zpkg);
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
    [HarmonyPrefix]
    static bool EnforceValidationStatus(ZRpc rpc, ref ZNet __instance)
    {
        bool isServer = __instance.IsServer();

        if (ValidatedPeers.Contains(rpc) || (!isServer && ClientVersionValidated)) return true;

        ConnectionError = "No <color=\"red\">Armoire</color> version received";

        if (isServer)
        {
            Dbgl($"Peer ({rpc.m_socket.GetHostName()}) never sent version or couldn't due to previous disconnect, disconnecting", forceLog: true, level: BepInEx.Logging.LogLevel.Warning);
            rpc.Invoke("Error", 3);
        }
        else
        {
            Dbgl("No version number received, mod may not be installed on server", forceLog: true, level: BepInEx.Logging.LogLevel.Warning);
            Game.instance.Logout();
            ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorVersion;
        }

        return false;
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
    [HarmonyPrefix]
    static void InvalidateDisconnectingPeer(ZNetPeer peer, ref ZNet __instance)
    {
        if (__instance.IsServer())
        {
            Dbgl($"Peer ({peer.m_rpc.m_socket.GetHostName()}) disconnected, removing from validated list");
            ValidatedPeers.Remove(peer.m_rpc);
        }
        else
            ClientVersionValidated = false;
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ShowConnectError))]
    [HarmonyPostfix]
    static void ShowConnectionError(FejdStartup __instance)
    {
        if (__instance.m_connectionFailedPanel.activeSelf)
            __instance.m_connectionFailedError.text += $"\n{ConnectionError}";
    }
}
