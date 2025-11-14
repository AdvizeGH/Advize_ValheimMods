namespace Advize_Armoire;

using System.Linq;
using HarmonyLib;
using static ArmoireUIController;
using static StaticMembers;

[HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.LoadPlayerData))]
static class LoadDataPatches
{
    static void Postfix(Player player)
    {
        if (player.m_customData.TryGetValue("Armoire_Appearances", out string data))
        {
            Dbgl("Armoire_Appearances key found in player.m_customData");
            AppearanceData.LoadAppearanceData(new ZPackage(data), ActiveOverrides);
        }
        else
        {
            ActiveOverrides.Values.ToList().ForEach(slot => slot.ResetSlot());
        }

        if (IsArmoirePanelValid())
        {
            Dbgl("UI is initialized, updating item slot icons");
            ArmoireUIInstance.UpdateItemSlotIcons(ActiveOverrides);
        }
        else
        {
            Dbgl("UI isn't initialized, can't update item slot icons");
        }

        player.SetupEquipment();

        AppearanceCategorizer.RecalculateAppearances(player);
    }
}
