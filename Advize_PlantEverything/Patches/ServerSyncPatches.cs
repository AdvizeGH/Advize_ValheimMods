namespace Advize_PlantEverything;

using HarmonyLib;
using ServerSync;
using static StaticMembers;

[HarmonyPatch(typeof(ConfigSync))]
static class ServerSyncPatches
{
    [HarmonyPatch("RPC_FromServerConfigSync")]
    [HarmonyPatch("RPC_FromOtherClientConfigSync")]
    [HarmonyPatch("resetConfigsFromServer")]
    internal static void Postfix()
    {
        Dbgl($"ServerSync event: Processing re-initialization queue.");
        ConfigEventHandlers.ProcessReInitQueue();
    }
}
