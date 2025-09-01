namespace Advize_ColorfulVines;

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using static StaticMembers;

[HarmonyPatch]
static class ColorPropagationPatches
{
    [HarmonyPatch(typeof(Vine), nameof(Vine.Grow))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> VineGrowTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
        .MatchStartForward(new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ZDO), nameof(ZDO.Set), parameters: [typeof(int), typeof(long)])))
        .ThrowIfInvalid("Could not patch Vine.Grow() (Custom Vine Color Propagation)")
        .Advance(offset: 1)
        .InsertAndAdvance(instructions: [new(OpCodes.Ldarg_0), new(OpCodes.Ldloc_0), new(OpCodes.Call, AccessTools.Method(typeof(ColorPropagationPatches), nameof(ModifyVineGrow)))])
        .InstructionEnumeration();
    }

    static void ModifyVineGrow(Vine existingVine, Vine newVine) => PropagateColors(existingVine.m_nview.GetZDO(), newVine.gameObject);

    [HarmonyPatch(typeof(Plant), nameof(Plant.PlaceAgainst))]
    static void Postfix(Plant __instance, GameObject obj) => PropagateColors(__instance.m_nview.GetZDO(), obj);

    private static void PropagateColors(ZDO zdo, GameObject go)
    {
        if (zdo.GetBool(ModdedVineHash))
        {
            VineColor component = go.GetComponent<VineColor>();

            component.CopyZDOs(zdo);
            component.ApplyColor();
        }
    }
}
