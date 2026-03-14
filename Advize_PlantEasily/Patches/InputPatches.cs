namespace Advize_PlantEasily;

using System;
using HarmonyLib;
using static ModContext;
using static ModUtils;

[HarmonyPatch]
static class InputPatches
{
    private static bool modifierHeld;

    private static bool OverrideGamepadInput => PlacementState.PlacementGhost && ZInput.GetKey(config.GamepadModifierKey, logWarning: false);

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateBuildGuiInput))]
    static void Prefix(Player __instance)
    {
        if (!HoldingCultivator)
        {
            modifierHeld = false;
            return;
        }

        if (config.EnableModKey.IsDown())
        {
            config.ModActive = !config.ModActive;
            Dbgl($"modActive was {!config.ModActive} setting to {config.ModActive}");
            __instance.Message(MessageHud.MessageType.TopLeft, $"PlantEasily.ModActive: {config.ModActive}");
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

        if (ZInput.GetKeyDown(config.KeyboardModifierKey) || ZInput.GetKeyDown(config.GamepadModifierKey))
            modifierHeld = true;

        if (ZInput.GetKeyUp(config.KeyboardModifierKey) || ZInput.GetKeyUp(config.GamepadModifierKey))
            modifierHeld = false;

        if (!modifierHeld)
            return;

        if (!ZInput.IsGamepadActive())
        {
            if (CheckAndApply(() => ZInput.GetKeyDown(config.IncreaseXKey, false), () => config.Columns++)) return;
            if (CheckAndApply(() => ZInput.GetKeyDown(config.IncreaseYKey, false), () => config.Rows++)) return;
            if (CheckAndApply(() => ZInput.GetKeyDown(config.DecreaseXKey, false), () => config.Columns--)) return;
            if (CheckAndApply(() => ZInput.GetKeyDown(config.DecreaseYKey, false), () => config.Rows--)) return;
        }
        else
        {
            if (CheckAndApply(() => ZInput.GetButtonDown("JoyDPadRight"), () => config.Columns++)) return;
            if (CheckAndApply(() => ZInput.GetButtonDown("JoyDPadUp"), () => config.Rows++)) return;
            if (CheckAndApply(() => ZInput.GetButtonDown("JoyDPadLeft"), () => config.Columns--)) return;
            if (CheckAndApply(() => ZInput.GetButtonDown("JoyDPadDown"), () => config.Rows--)) return;
        }
    }

    private static bool CheckAndApply(Func<bool> inputCheck, Action apply)
    {
        if (inputCheck())
        {
            apply();
            return true;
        }
        return false;
    }

    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
    [HarmonyPatch(typeof(Player), nameof(Player.StartGuardianPower))]
    static bool Prefix() => !OverrideGamepadInput;
}
