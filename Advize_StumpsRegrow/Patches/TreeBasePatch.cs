namespace Advize_StumpsRegrow;

using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using static StumpsRegrow;

[HarmonyPatch(typeof(TreeBase), nameof(TreeBase.SpawnLog))]
static class TreeBasePatch
{
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> SpawnLogTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .Start()
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TreeBase), nameof(TreeBase.m_stubPrefab))),
                new CodeMatch(OpCodes.Ldarg_0))
            .ThrowIfInvalid($"Could not patch TreeBase.SpawnLog()! (stub-prefab-instantiate)")
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ZNetView), nameof(ZNetView.SetLocalScale))))
            .ThrowIfInvalid($"Could not patch TreeBase.SpawnLog()! (stub-prefab-netview-set-local-scale)")
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeBasePatch), nameof(ModifyStubPrefabNetView))))
            .InstructionEnumeration();
    }

    static ZNetView ModifyStubPrefabNetView(ZNetView netView, TreeBase treeBase)
    {
        string treeBaseName = Utils.GetPrefabName(treeBase.name);
        netView.GetZDO().Set(HashedZDOName, treeBaseName);

        return netView;
    }
}
