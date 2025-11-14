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
        { AppearanceSlotType.Helmet, new AppearanceSlot(sd => sd.m_itemType == ItemType.Helmet && sd.m_skillType == (SkillType)1, canBeHidden: true) },

        { AppearanceSlotType.Chest, new AppearanceSlot(sd => sd.m_itemType == ItemType.Chest && sd.m_skillType == (SkillType)1, canBeHidden: true) },

        { AppearanceSlotType.Legs, new AppearanceSlot(sd => sd.m_itemType == ItemType.Legs && sd.m_skillType == (SkillType)1, canBeHidden: true) },

        { AppearanceSlotType.Shoulder, new AppearanceSlot(sd => sd.m_itemType == ItemType.Shoulder && sd.m_skillType == (SkillType)1, canBeHidden: true) },

        { AppearanceSlotType.Utility, new AppearanceSlot(sd => sd.m_itemType == ItemType.Utility && sd.m_skillType == (SkillType)1, canBeHidden: true) },

        { AppearanceSlotType.Trinket, new AppearanceSlot(sd => sd.m_itemType == ItemType.Trinket && sd.m_skillType == (SkillType)1, canBeHidden: true) },

        { AppearanceSlotType.Bow, new AppearanceSlot(sd => sd.m_itemType == ItemType.Bow && sd.m_skillType == SkillType.Bows) },

        { AppearanceSlotType.Crossbow, new AppearanceSlot(sd => sd.m_itemType == ItemType.Bow && sd.m_skillType == SkillType.Crossbows) },

        { AppearanceSlotType.Shield, new AppearanceSlot(sd => sd.m_itemType == ItemType.Shield && sd.m_skillType == SkillType.Blocking) },

        { AppearanceSlotType.OneHandedAxe, new AppearanceSlot(sd => sd.m_itemType == ItemType.OneHandedWeapon && sd.m_skillType == SkillType.Axes) },

        { AppearanceSlotType.OneHandedClub, new AppearanceSlot(sd => sd.m_itemType == ItemType.OneHandedWeapon && sd.m_skillType == SkillType.Clubs) },

        { AppearanceSlotType.OneHandedSword, new AppearanceSlot(sd => sd.m_itemType == ItemType.OneHandedWeapon && sd.m_skillType == SkillType.Swords) },

        { AppearanceSlotType.Knife, new AppearanceSlot(sd => (sd.m_itemType is ItemType.OneHandedWeapon or ItemType.TwoHandedWeapon) && sd.m_skillType == SkillType.Knives) },

        { AppearanceSlotType.Spear, new AppearanceSlot(sd => sd.m_itemType == ItemType.OneHandedWeapon && sd.m_skillType == SkillType.Spears) },

        { AppearanceSlotType.TwoHandedAxe, new AppearanceSlot(sd => sd.m_itemType == ItemType.TwoHandedWeapon && sd.m_skillType == SkillType.Axes) },

        { AppearanceSlotType.TwoHandedClub, new AppearanceSlot(sd => sd.m_itemType == ItemType.TwoHandedWeapon && sd.m_skillType == SkillType.Clubs) },

        { AppearanceSlotType.TwoHandedSword, new AppearanceSlot(sd => sd.m_itemType == ItemType.TwoHandedWeapon && sd.m_skillType == SkillType.Swords) },

        { AppearanceSlotType.Polearm, new AppearanceSlot( sd => sd.m_itemType == ItemType.TwoHandedWeapon && sd.m_skillType == SkillType.Polearms) },

        { AppearanceSlotType.Pickaxe, new AppearanceSlot(sd => sd.m_itemType == ItemType.TwoHandedWeapon && sd.m_skillType == SkillType.Pickaxes) },

        { AppearanceSlotType.Staff, new AppearanceSlot(sd => (sd.m_itemType is ItemType.TwoHandedWeapon/* or ItemType.TwoHandedWeaponLeft*/) &&
            (sd.m_skillType is SkillType.ElementalMagic or SkillType.BloodMagic)) },

        { AppearanceSlotType.Fist, new AppearanceSlot(sd => sd.m_itemType == ItemType.TwoHandedWeapon && sd.m_skillType == SkillType.Unarmed) }
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
