namespace Advize_Armoire;

using System.Collections.Generic;
using System.Linq;
using static ItemDrop.ItemData;
using static Skills;
using static StaticMembers;

static class AppearanceCategorizer
{
    private static readonly HashSet<ItemType> RelevantTypes =
    [
        ItemType.Helmet, ItemType.Chest, ItemType.Legs, ItemType.Shoulder,
        ItemType.Utility, ItemType.Trinket, ItemType.Bow, ItemType.Shield,
        ItemType.OneHandedWeapon, ItemType.TwoHandedWeapon/*, ItemType.TwoHandedWeaponLeft*/
    ];

    private static readonly HashSet<ItemType> ArmorTypes =
    [
        ItemType.Helmet, ItemType.Chest, ItemType.Legs,
        ItemType.Shoulder, ItemType.Utility, ItemType.Trinket
    ];

    internal static void CategorizeAppearances(Player player)
    {
        // Get all relevant items potentially eligible to be available appearances, filter out disabled ones
        // If excludeDLCItems is false, the DLC condition is ignored. If true, it filters them out.
        IEnumerable<ItemDrop> allRelevantItems = GetAllItemsByTypes(RelevantTypes)
            .Where(item => item.m_itemData.m_shared.m_icons.Length > 0 && !config.DisabledAppearanceNames.Contains(item.name) && 
            (!config.ExcludeDLCItems || string.IsNullOrEmpty(item.m_itemData.m_shared.m_dlc)));


        // Categorize items
        Dictionary<AppearanceSlotType, List<ItemDrop>> categorized = [];

        foreach (ItemDrop item in allRelevantItems)
        {
            ItemType type = item.m_itemData.m_shared.m_itemType;
            SkillType skill = item.m_itemData.m_shared.m_skillType;

            AddToCategory(categorized, MapAppearanceSlot(type, skill), item);
        }

        // Make a deep copy and filter to items discovered by the player
        Dictionary<AppearanceSlotType, List<ItemDrop>> unlockedCategorized = categorized
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Where(item => player.m_knownMaterial.Contains(item.m_itemData.m_shared.m_name)).ToList());

        // Account for variants
        AllAppearances = categorized.ToDictionary(kvp => kvp.Key, kvp => BuildIconCountMap(kvp.Value));
        UnlockedAppearances = unlockedCategorized.ToDictionary(kvp => kvp.Key, kvp => BuildIconCountMap(kvp.Value));
    }

    private static void AddToCategory(Dictionary<AppearanceSlotType, List<ItemDrop>> dict, AppearanceSlotType? slotType, ItemDrop item)
    {
        if (slotType is not AppearanceSlotType slot) return;

        if (!dict.ContainsKey(slot))
            dict[slot] = [];
        dict[slot].Add(item);
    }

    private static AppearanceSlotType? MapAppearanceSlot(ItemType type, SkillType skill)
    {
        if (ArmorTypes.Contains(type) && skill == SkillType.Swords)
            return type switch
            {
                ItemType.Helmet => AppearanceSlotType.Helmet,
                ItemType.Chest => AppearanceSlotType.Chest,
                ItemType.Legs => AppearanceSlotType.Legs,
                ItemType.Shoulder => AppearanceSlotType.Shoulder,
                ItemType.Utility => AppearanceSlotType.Utility,
                ItemType.Trinket => AppearanceSlotType.Trinket,
                _ => null
            };

        return type switch
        {
            ItemType.OneHandedWeapon => skill switch
            {
                SkillType.Axes => AppearanceSlotType.OneHandedAxe,
                SkillType.Clubs => AppearanceSlotType.OneHandedClub,
                SkillType.Swords => AppearanceSlotType.OneHandedSword,
                SkillType.Knives => AppearanceSlotType.Knife,
                SkillType.Spears => AppearanceSlotType.Spear,
                _ => null
            },
            ItemType.TwoHandedWeapon => skill switch
            {
                SkillType.Axes => AppearanceSlotType.TwoHandedAxe,
                SkillType.Clubs => AppearanceSlotType.TwoHandedClub,
                SkillType.Swords => AppearanceSlotType.TwoHandedSword,
                SkillType.Polearms => AppearanceSlotType.Polearm,
                SkillType.Pickaxes => AppearanceSlotType.Pickaxe,
                SkillType.ElementalMagic or SkillType.BloodMagic => AppearanceSlotType.Staff,
                SkillType.Knives => AppearanceSlotType.Knife,
                SkillType.Unarmed => AppearanceSlotType.Fist,
                _ => null
            },
            //ItemType.TwoHandedWeaponLeft => skill switch
            //{
            //    SkillType.ElementalMagic or SkillType.BloodMagic => AppearanceSlotType.Staff,
            //    _ => null
            //},
            ItemType.Bow => skill switch
            {
                SkillType.Bows => AppearanceSlotType.Bow,
                SkillType.Crossbows => AppearanceSlotType.Crossbow,
                _ => null
            },
            ItemType.Shield when skill == SkillType.Blocking => AppearanceSlotType.Shield,
            _ => null
        };
    }

    private static List<ItemDrop> GetAllItemsByTypes(HashSet<ItemType> types) => [.. ObjectDB.instance.m_items
        .Select(go => go.GetComponent<ItemDrop>()).Where(item => item && types.Contains(item.m_itemData.m_shared.m_itemType))];

    private static Dictionary<ItemDrop, int> BuildIconCountMap(List<ItemDrop> items) => items.ToDictionary(item => item, item => item.m_itemData.m_shared.m_icons.Length);

    internal static void RecalculateAppearances(Player player)
    {
        CategorizeAppearances(player);
        AppearanceTracker.UpdateTotal();
    }
}
