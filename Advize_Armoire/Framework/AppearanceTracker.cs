namespace Advize_Armoire;

using System.Linq;
using static StaticMembers;

static class AppearanceTracker
{
    internal static int Unlocked(AppearanceSlotType slotType) => UnlockedAppearances[slotType].Values.Sum();
    internal static int Total(AppearanceSlotType slotType) => AllAppearances[slotType].Values.Sum();

    internal static int TotalUnlocked { get; set; }
    internal static int TotalCollectable { get; set; }

    internal static void UpdateTotal()
    {
        TotalUnlocked = UnlockedAppearances.Values.Sum(dict => dict.Values.Sum());
        TotalCollectable = AllAppearances.Values.Sum(dict => dict.Values.Sum());

        Dbgl($"UnlockedAppearances Count: {TotalUnlocked}");
        Dbgl($"AllAppearances Count: {TotalCollectable}");
    }

    internal static double UnlockedPercentage => TotalCollectable == 0 ? 0 : (double)TotalUnlocked / TotalCollectable;
}
