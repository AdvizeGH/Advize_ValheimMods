namespace Advize_Armoire;

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using static StaticMembers;

[HarmonyPatch]
static class EquipmentPatches
{
    internal static string overriddenLeftItem = "";
    internal static string overriddenRightItem = "";

    [HarmonyPatch(typeof(VisEquipment))]
    static class VisEquipmentPatches
    {
        private static readonly Dictionary<string, (AppearanceSlotType slot, Func<Player, ItemDrop.ItemData> targetedItem)> SimpleItemMap = new()
        {
            { nameof(VisEquipment.SetHelmetItem), (AppearanceSlotType.Helmet, player => player.m_helmetItem) },
            { nameof(VisEquipment.SetChestItem), (AppearanceSlotType.Chest, player => player.m_chestItem) },
            { nameof(VisEquipment.SetLegItem), (AppearanceSlotType.Legs, player => player.m_legItem) },
            { nameof(VisEquipment.SetUtilityItem), (AppearanceSlotType.Utility, player => player.m_utilityItem) },
            { nameof(VisEquipment.SetTrinketItem), (AppearanceSlotType.Trinket, player => player.m_trinketItem) }
        };

        [HarmonyPatch(nameof(VisEquipment.SetHelmetItem))]
        [HarmonyPatch(nameof(VisEquipment.SetChestItem))]
        [HarmonyPatch(nameof(VisEquipment.SetLegItem))]
        [HarmonyPatch(nameof(VisEquipment.SetUtilityItem))]
        [HarmonyPatch(nameof(VisEquipment.SetTrinketItem))]
        [HarmonyPrefix]
        static void SetSimpleItem(VisEquipment __instance, MethodBase __originalMethod, ref string name)
        {
            if (!SimpleItemMap.TryGetValue(__originalMethod.Name, out (AppearanceSlotType slot, Func<Player, ItemDrop.ItemData> targetedItem) entry)) return;

            TryOverrideItem(__instance, ref name, entry.targetedItem, entry.slot);
        }

        [HarmonyPatch(nameof(VisEquipment.SetShoulderItem))]
        [HarmonyPrefix]
        static void SetShoulderItem(VisEquipment __instance, ref string name, ref int variant) =>
            TryOverrideItemWithVariant(__instance, ref name, ref variant, p => p.m_shoulderItem, AppearanceSlotType.Shoulder);

        [HarmonyPatch(nameof(VisEquipment.SetLeftItem))]
        [HarmonyPrefix]
        static void SetLeftItem(VisEquipment __instance, ref string name, ref int variant) =>
            TryOverrideMatchingItem(__instance, ref name, ref variant, p => p.m_leftItem, OverrideTarget.LeftItem);

        [HarmonyPatch(nameof(VisEquipment.SetLeftBackItem))]
        [HarmonyPrefix]
        static void SetLeftBackItem(VisEquipment __instance, ref string name, ref int variant) =>
            TryOverrideMatchingItem(__instance, ref name, ref variant, p => p.m_hiddenLeftItem);

        [HarmonyPatch(nameof(VisEquipment.SetRightItem))]
        [HarmonyPrefix]
        static void SetRightItem(VisEquipment __instance, ref string name)
        {
            int dummyVariant = 0;
            TryOverrideMatchingItem(__instance, ref name, ref dummyVariant, p => p.m_rightItem, OverrideTarget.RightItem);
        }

        [HarmonyPatch(nameof(VisEquipment.SetRightBackItem))]
        [HarmonyPrefix]
        static void SetRightBackItem(VisEquipment __instance, ref string name)
        {
            int dummyVariant = 0;
            TryOverrideMatchingItem(__instance, ref name, ref dummyVariant, p => p.m_hiddenRightItem);
        }

        private static bool ShouldOverrideAppearance(VisEquipment instance) =>
            config.EnableOverrides && (instance.gameObject == Player.m_localPlayer?.gameObject || !ZNetScene.instance);

        private static void TryOverrideItem(VisEquipment instance, ref string name, Func<Player, ItemDrop.ItemData> targetedItem, AppearanceSlotType slotType)
        {
            if (!ShouldOverrideAppearance(instance) || !instance.TryGetComponent(out Player player) || targetedItem(player) == null) return;

            AppearanceSlot slot = ActiveOverrides[slotType];
            if (!string.IsNullOrEmpty(slot.ItemName) || slot.Hidden)
                name = slot.ItemName;
        }

        private static void TryOverrideItemWithVariant(VisEquipment instance, ref string name, ref int variant, Func<Player, ItemDrop.ItemData> targetedItem, AppearanceSlotType slotType)
        {
            if (!ShouldOverrideAppearance(instance) || !instance.TryGetComponent(out Player player) || targetedItem(player) == null) return;

            AppearanceSlot slot = ActiveOverrides[slotType];
            if (!string.IsNullOrEmpty(slot.ItemName) || slot.Hidden)
            {
                name = slot.ItemName;
                variant = slot.ItemVariant;
            }
        }

        private static void TryOverrideMatchingItem(VisEquipment instance, ref string name, ref int variant, Func<Player, ItemDrop.ItemData> targetedItem, OverrideTarget? target = null)
        {
            if (!ShouldOverrideAppearance(instance) || !instance.TryGetComponent(out Player player)) return;

            ItemDrop.ItemData item = targetedItem(player);
            if (item == null) return;

            AppearanceSlot match = PluginUtils.FindMatchingSlot(item.m_shared);
            bool hasMatch = !string.IsNullOrEmpty(match?.ItemName);

            if (hasMatch)
            {
                name = match.ItemName;
                variant = match.ItemVariant;
            }

            if (target == OverrideTarget.LeftItem)
                overriddenLeftItem = hasMatch ? name : "";
            else if (target == OverrideTarget.RightItem)
                overriddenRightItem = hasMatch ? name : "";
        }

        private enum OverrideTarget
        {
            LeftItem,
            RightItem
        }
    }

    [HarmonyPatch(typeof(Humanoid))]
    static class MiscEquipmentPatches
    {
        [HarmonyPatch(nameof(Humanoid.SetupAnimationState))]
        [HarmonyPostfix]
        static void FixAnimationState(Humanoid __instance)
        {
            string overrideName = __instance.m_leftItem != null ?
                overriddenLeftItem : __instance.m_rightItem != null ?
                overriddenRightItem : null;

            if (string.IsNullOrEmpty(overrideName)) return;

            ItemDrop itemDrop = ZNetScene.instance.GetPrefab(overrideName)?.GetComponent<ItemDrop>();
            if (itemDrop == null) return;

            ItemDrop.ItemData.SharedData itemOverride = itemDrop.m_itemData.m_shared;
            AppearanceSlot match = PluginUtils.FindMatchingSlot(itemOverride);

            if (!string.IsNullOrEmpty(match?.ItemName))
                __instance.SetAnimationState(itemOverride.m_animationState);
        }
    }
}
