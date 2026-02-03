namespace Advize_Armoire;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using static Armoire;
using static ItemDrop.ItemData;
using static Skills;

static class StaticMembers
{
    internal static ManualLogSource ModLogger = new($" {PluginName}");
    internal static ModConfig config;
    internal static AssetBundle assetBundle;

    internal static GameObject guiPrefab = null;
    internal static GameObject armoireSlot = null;
    internal static GameObject armoirePiecePrefab = null;

    internal static readonly Dictionary<LogLevel, Action<string>> logActions = new()
    {
        { LogLevel.Fatal, ModLogger.LogFatal },
        { LogLevel.Error, ModLogger.LogError },
        { LogLevel.Warning, ModLogger.LogWarning },
        { LogLevel.Message, ModLogger.LogMessage },
        { LogLevel.Info, ModLogger.LogInfo },
        { LogLevel.Debug, ModLogger.LogDebug }
    };

    internal static Dictionary<AppearanceSlotType, AppearanceSlot> ActiveOverrides = new()
    {
        { AppearanceSlotType.Helmet, new AppearanceSlot(canBeHidden: true) },
        { AppearanceSlotType.Chest, new AppearanceSlot(canBeHidden: true) },
        { AppearanceSlotType.Legs, new AppearanceSlot(canBeHidden: true) },
        { AppearanceSlotType.Shoulder, new AppearanceSlot(canBeHidden: true) },
        { AppearanceSlotType.Utility, new AppearanceSlot(canBeHidden: true) },
        { AppearanceSlotType.Trinket, new AppearanceSlot(canBeHidden: true) },

        { AppearanceSlotType.Bow,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.Bow &&
                candidate.m_skillType == SkillType.Bows)
        },

        { AppearanceSlotType.Crossbow,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.Bow &&
                candidate.m_skillType == SkillType.Crossbows)
        },

        { AppearanceSlotType.Shield,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.Shield &&
                candidate.m_skillType == SkillType.Blocking)
        },

        { AppearanceSlotType.OneHandedAxe,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.OneHandedWeapon &&
                candidate.m_skillType == SkillType.Axes)
        },

        { AppearanceSlotType.OneHandedClub,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.OneHandedWeapon &&
                candidate.m_skillType == SkillType.Clubs)
        },

        { AppearanceSlotType.OneHandedSword,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.OneHandedWeapon &&
                candidate.m_skillType == SkillType.Swords)
        },

        { AppearanceSlotType.Knife,
            new AppearanceSlot(slotCriteria: candidate =>
                (candidate.m_itemType is ItemType.OneHandedWeapon or ItemType.TwoHandedWeapon) &&
                candidate.m_skillType == SkillType.Knives)
        },

        { AppearanceSlotType.Spear,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.OneHandedWeapon &&
                candidate.m_skillType == SkillType.Spears)
        },

        { AppearanceSlotType.TwoHandedAxe,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.TwoHandedWeapon &&
                candidate.m_skillType == SkillType.Axes)
        },

        { AppearanceSlotType.TwoHandedClub,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.TwoHandedWeapon &&
                candidate.m_skillType == SkillType.Clubs)
        },

        { AppearanceSlotType.TwoHandedSword,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.TwoHandedWeapon &&
                candidate.m_skillType == SkillType.Swords)
        },

        { AppearanceSlotType.Polearm,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.TwoHandedWeapon &&
                candidate.m_skillType == SkillType.Polearms)
        },

        { AppearanceSlotType.Pickaxe,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.TwoHandedWeapon &&
                candidate.m_skillType == SkillType.Pickaxes)
        },

        { AppearanceSlotType.Staff,
            new AppearanceSlot(slotCriteria: candidate =>
                (candidate.m_itemType is ItemType.TwoHandedWeapon/* or ItemType.TwoHandedWeaponLeft */) &&
                (candidate.m_skillType is SkillType.ElementalMagic or SkillType.BloodMagic))
        },

        { AppearanceSlotType.Fist,
            new AppearanceSlot(slotCriteria: candidate =>
                candidate.m_itemType == ItemType.TwoHandedWeapon &&
                candidate.m_skillType == SkillType.Unarmed)
        }
    };

    internal static List<Dictionary<AppearanceSlotType, AppearanceSlot>> OutfitOverrides = [.. Enumerable.Range(0, 3).Select(_ => PluginUtils.CloneOverrides(ActiveOverrides))];

    internal static Dictionary<AppearanceSlotType, Dictionary<ItemDrop, int>> AllAppearances = [];
    internal static Dictionary<AppearanceSlotType, Dictionary<ItemDrop, int>> UnlockedAppearances = [];

    //internal static Dictionary<AppearanceSlotType, Dictionary<ItemDrop, int>> LockedAppearances =
    //AllAppearances.ToDictionary(
    //    kvp => kvp.Key,
    //    kvp => kvp.Value
    //        .Where(pair => !UnlockedAppearances[kvp.Key].ContainsKey(pair.Key))
    //        .ToDictionary(pair => pair.Key, pair => pair.Value)
    //);

    // This should probably be cached
    internal static Dictionary<AppearanceSlotType, Dictionary<ItemDrop, int>> GetLockedAppearances()
    {
        return AllAppearances.ToDictionary(
        kvp => kvp.Key,
        kvp => UnlockedAppearances.TryGetValue(kvp.Key, out Dictionary<ItemDrop, int> unlocked)
            ? kvp.Value.Where(pair => !unlocked.ContainsKey(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value)
            : new Dictionary<ItemDrop, int>(kvp.Value)
        );
    }

    internal static AssetBundle LoadAssetBundle(string fileName)
    {
        Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Advize_{PluginName}.Assets.{fileName}");
        return AssetBundle.LoadFromStream(manifestResourceStream);
    }

    internal static void Dbgl(string message, bool forceLog = false, LogLevel level = LogLevel.Info)
    {
        if (forceLog || config.EnableDebugMessages)
            logActions[level](message);
    }
}
