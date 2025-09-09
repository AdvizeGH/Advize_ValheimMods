namespace Advize_PlantEverything;

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using static StaticMembers;

[HarmonyPatch]
static class ApplyZDOPatches
{
    static readonly MethodInfo ModifyPlantGrowMethod = AccessTools.Method(typeof(ApplyZDOPatches), nameof(ModifyPlantGrow));

    static void ModifyPlantGrow(Plant plant, GameObject grownTree)
    {
        if (!plant.m_nview.GetZDO().GetBool(PlaceAnywhereHash) || !grownTree.TryGetComponent(out TreeBase tb) || !tb.TryGetComponent(out StaticPhysics sp))
            return;

        sp.m_fall = false;
        tb.m_nview.GetZDO().Set(PlaceAnywhereHash, true);
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Grow))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> PlantTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
        .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(TreeBase), nameof(TreeBase.Grow))))
        .ThrowIfInvalid("Could not patch Plant.Grow() ([Difficulty]PlaceAnywhere)")
        .Advance(1)
        .InsertAndAdvance(instructions: [new(OpCodes.Ldarg_0), new(OpCodes.Ldloc_1), new(OpCodes.Call, ModifyPlantGrowMethod)])
        .InstructionEnumeration();
    }
}
