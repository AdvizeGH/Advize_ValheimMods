﻿namespace Advize_PlantEverything;

using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static StaticContent;

[HarmonyPatch]
static class ApplyZDOPatches
{
    static readonly MethodInfo ModifyPlantGrowMethod = AccessTools.Method(typeof(ApplyZDOPatches), nameof(ModifyPlantGrow));
    static readonly MethodInfo ModifyVineGrowMethod = AccessTools.Method(typeof(ApplyZDOPatches), nameof(ModifyVineGrow));

    static void ModifyPlantGrow(Plant plant, GameObject grownTree)
    {
        if (!plant.m_nview.GetZDO().GetBool(PlaceAnywhereHash) || !grownTree.TryGetComponent(out TreeBase tb) || !tb.TryGetComponent(out StaticPhysics sp))
            return;

        sp.m_fall = false;
        tb.m_nview.GetZDO().Set(PlaceAnywhereHash, true);
    }

    static void ModifyVineGrow(Vine existingVine, Vine newVine)
    {
        if (existingVine.m_nview.GetZDO().GetBool(ModdedVineHash))
        {
            VineColor component = newVine.GetComponent<VineColor>();

            component.CopyZDOs(existingVine.m_nview.GetZDO());
            component.ApplyColor();
        }
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

    [HarmonyPatch(typeof(Vine), nameof(Vine.Grow))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> VineTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
        .MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ZDO), nameof(ZDO.Set), parameters: [typeof(int), typeof(long)])))
        .ThrowIfInvalid("Could not patch Vine.Grow() (Custom Vine Color Propagation)")
        .Advance(1)
        .InsertAndAdvance(instructions: [new(OpCodes.Ldarg_0), new(OpCodes.Ldloc_0), new(OpCodes.Call, ModifyVineGrowMethod)])
        .InstructionEnumeration();
    }
}
