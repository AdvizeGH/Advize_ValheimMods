namespace Advize_PlantEasily;

using HarmonyLib;
using static PlantEasily;

[HarmonyPatch]
static class InputPatches
{
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateBuildGuiInput))]
    static void Prefix(Player __instance)
    {
        if (config.EnableModKey.IsDown())
        {
            config.ModActive = !config.ModActive;
            Dbgl($"modActive was {!config.ModActive} setting to {config.ModActive}");
            __instance.Message(MessageHud.MessageType.TopLeft, $"PlantEasily.ModActive: {config.ModActive}");
            if (HoldingCultivator)
                __instance.SetupPlacementGhost();
        }
        if (config.EnableSnappingKey.IsDown())
        {
            config.SnapActive = !config.SnapActive;
            Dbgl($"snapActive was {!config.SnapActive} setting to {config.SnapActive}");
            __instance.Message(MessageHud.MessageType.TopLeft, $"PlantEasily.SnapActive: {config.SnapActive}");
        }
        if (config.ToggleAutoReplantKey.IsDown())
        {
            config.ReplantOnHarvest = !config.ReplantOnHarvest;
            Dbgl($"replantOnHarvest was {!config.ReplantOnHarvest} setting to {config.ReplantOnHarvest}");
            __instance.Message(MessageHud.MessageType.TopLeft, $"PlantEasily.ReplantOnHarvest: {config.ReplantOnHarvest}");
        }

        if (ZInput.GetKey(config.KeyboardModifierKey) || ZInput.GetKey(config.GamepadModifierKey))
        {
            if (ZInput.GetKeyDown(config.IncreaseXKey, false) || ZInput.GetButtonDown("JoyDPadRight"))
                config.Columns += 1;

            if (ZInput.GetKeyDown(config.IncreaseYKey, false) || ZInput.GetButtonDown("JoyDPadUp"))
                config.Rows += 1;

            if (ZInput.GetKeyDown(config.DecreaseXKey, false) || ZInput.GetButtonDown("JoyDPadLeft"))
                config.Columns -= 1;

            if (ZInput.GetKeyDown(config.DecreaseYKey, false) || ZInput.GetButtonDown("JoyDPadDown"))
                config.Rows -= 1;
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
    [HarmonyPatch(typeof(Player), nameof(Player.StartGuardianPower))]
    static void Prefix(ref bool __runOriginal) => __runOriginal = !OverrideGamepadInput;
}
