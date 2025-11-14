namespace Advize_Armoire;

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using static StaticMembers;

[HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
static class DiscoverItemPatches
{
    private static Sprite _armoireIcon;
    private static Sprite ArmoireIcon => _armoireIcon ??= armoirePiecePrefab.GetComponent<Piece>().m_icon;

    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> AddKnownItemPatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .Start()
            .MatchStartForward(
                // Match the Contains call and the brtrue that skips the block
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(HashSet<string>), "Contains")),
                new CodeMatch(OpCodes.Brtrue))
            .ThrowIfInvalid($"Could not patch Player.AddKnownItem()!")
            .Advance(offset: 2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1), // Load item argument
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DiscoverItemPatches), nameof(UnlockNewAppearance))))
            .InstructionEnumeration();
    }

    static void UnlockNewAppearance(ItemDrop.ItemData discoveredItem)
    {
        Dbgl($"New item discovered: {discoveredItem.m_shared.m_name}");
        ItemDrop item = discoveredItem.m_dropPrefab.GetComponent<ItemDrop>();
        AppearanceSlotType? slotType = PluginUtils.GetSlotTypeForItemDrop(item);
        if (slotType is null) return;

        MessageHud.instance.QueueUnlockMsg(ArmoireIcon, "<color=#00FFFF>Appearance Unlocked!</color>", discoveredItem.m_shared.m_name);
        UnlockedAppearances[slotType.Value][item] = item.m_itemData.m_shared.m_icons.Length;
        AppearanceTracker.UpdateTotal();
    }
}
