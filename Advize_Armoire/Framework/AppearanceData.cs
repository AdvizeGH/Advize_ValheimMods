namespace Advize_Armoire;

using System;
using System.Collections.Generic;
using static StaticMembers;

sealed class AppearanceData
{
    // Change this for future revisions
    static readonly Version MinimumSupported = new("1.0.0");

    internal static void LoadAppearanceData(ZPackage package, Dictionary<AppearanceSlotType, AppearanceSlot> overrides)
    {
        try
        {
            Dbgl("Loading player armoire data...");
            string versionString = package.ReadString();

            if (!Version.TryParse(versionString, out Version incomingVersion))
            {
                Dbgl($"Invalid version format: {versionString}", forceLog: true, level: BepInEx.Logging.LogLevel.Warning);
                return;
            }

            if (incomingVersion < MinimumSupported)
            {
                Dbgl($"Unsupported armoire data version: {versionString}", forceLog: true, level: BepInEx.Logging.LogLevel.Warning);
                return;
                // Can handle graceful migration here with switch statement, here's examples for future me (remove return statement above ofc)
                //switch (versionString)
                //{
                //    //Can use System.Version to do a version range check as well
                //    case "0.9.0":
                //        // Example: discard unused bool, load basic slot data
                //        int legacyOverrides = package.ReadInt();
                //        for (int i = 0; i < legacyOverrides; i++)
                //        {
                //            if (!Enum.TryParse(package.ReadString(), out AppearanceSlotType slotType) || !overrides.TryGetValue(slotType, out AppearanceSlot slot))
                //                continue;

                //            slot.ItemName = package.ReadString();
                //            slot.ItemVariant = package.ReadInt();
                //            package.ReadBool(); // discard legacy hidden flag
                //            if (slot.CanBeHidden)
                //                slot.Hidden = false; // default for legacy
                //        }

                //        Dbgl("Legacy data migrated from version 0.9.0");
                //        SaveAppearanceData("Armoire_Appearances");
                //        return;

                //    default:
                //        Dbgl($"No migration path for version: {versionString}", forceLog: true, level: BepInEx.Logging.LogLevel.Warning);
                //        return;
                //}
            }

            int appearanceOverrides = package.ReadInt();
            for (int i = 0; i < appearanceOverrides; i++)
            {
                if (!Enum.TryParse(package.ReadString(), out AppearanceSlotType slotType) || !overrides.TryGetValue(slotType, out AppearanceSlot slot))
                    continue;

                slot.ItemName = package.ReadString();
                slot.ItemVariant = package.ReadInt();
                if (slot.CanBeHidden)
                    slot.Hidden = package.ReadBool();
            }

            Dbgl("...player armoire data loading complete");
        }
        catch (Exception ex)
        {
            Dbgl($"Error loading appearance data: {ex.Message}", forceLog: true, level: BepInEx.Logging.LogLevel.Error);
        }
    }

    internal static void SaveAppearanceData(string dataKey)
    {
        //Write out data to be saved, start with mod version number
        ZPackage armoireData = new();
        armoireData.Write(Armoire.Version);

        //Write out total number of override appearance slots
        armoireData.Write(ActiveOverrides.Count);
        //For each one, serialize slot data
        foreach (KeyValuePair<AppearanceSlotType, AppearanceSlot> appearanceSlot in ActiveOverrides)
        {
            armoireData.Write(appearanceSlot.Key.ToString());
            armoireData.Write(appearanceSlot.Value.ItemName);
            armoireData.Write(appearanceSlot.Value.ItemVariant);
            if (appearanceSlot.Value.CanBeHidden)
                armoireData.Write(appearanceSlot.Value.Hidden);
        }

        //Save to player's custom data
        Player.m_localPlayer.m_customData[dataKey] = armoireData.GetBase64();
        Dbgl("Saved player armoire data");
    }
}
