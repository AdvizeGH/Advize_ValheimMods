namespace Advize_PlantEasily;

using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PlantEasily;

[HarmonyPatch]
static class KeyHintPatches
{
    static GameObject keyboardHint;
    static GameObject gamepadHint;
    static readonly Dictionary<string, string> inputBindingPathToButtonDefNames = [];

    internal static void UpdateKeyHintText()
    {
        if (keyboardHint) UpdateKeyboardHints();
        if (gamepadHint) UpdateGamepadHints();
    }

    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.Start))]
    [HarmonyPostfix]
    static void Start() => CreateKeyBoardHints();

    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.ApplySettings))]
    [HarmonyPrefix]
    static void ApplySettings() => UpdateKeyHintText();

    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.SetGamePadBindings))]
    [HarmonyPostfix]
    static void SetGamePadBindings()
    {
        //Seems to be called when game settings are saved
        if (!gamepadHint)
            CreateGamepadHints();
        UpdateGamepadHints();
    }

    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> UpdateHintsTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
        .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PlayerCustomizaton), nameof(PlayerCustomizaton.IsBarberGuiVisible))))
        .ThrowIfInvalid("Could not patch KeyHints.UpdateHints() (BuildHUD Key Hints")
        .Advance(-2)
        .InsertAndAdvance(instructions: [new(OpCodes.Call, AccessTools.Method(typeof(KeyHintPatches), nameof(SetKeyHintsActive)))])
        .InstructionEnumeration();
    }

    //Patch to a completely different class to support all of this thanks to bog witch update
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.AddButton))]
    [HarmonyPrefix]
    static void AddButton(string name, string path) => inputBindingPathToButtonDefNames[path] = name;

    static void CreateKeyBoardHints()
    {
        Transform Keyboard = KeyHints.m_instance.m_buildHints.transform.Find("Keyboard");

        keyboardHint = Object.Instantiate(Keyboard.Find("Copy").gameObject, Keyboard); // Copy the "Copy" KeyHint
        keyboardHint.name = "Resize Grid";
        keyboardHint.transform.SetSiblingIndex(3);
        keyboardHint.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Grid Size";
        keyboardHint.transform.GetChild(0).GetComponent<LayoutElement>().preferredWidth = 75;

        GameObject hintKey = Object.Instantiate(keyboardHint.transform.GetChild(1).gameObject);
        Object.Instantiate(hintKey, keyboardHint.transform);
        Object.Instantiate(hintKey, keyboardHint.transform);
        Object.Instantiate(hintKey, keyboardHint.transform);

        UpdateKeyboardHints();
    }

    static void CreateGamepadHints()
    {
        Transform Gamepad = KeyHints.m_instance.m_buildHints.transform.Find("Gamepad");
        // Can't use transform.Find() with a '/' in the string or it searches hierarchy like a path name
        GameObject hintToClone = Gamepad.GetComponentsInChildren<Transform>().Where(x => x.gameObject.name == "Text - Copy Alt1/2").First().gameObject;

        gamepadHint = Object.Instantiate(hintToClone, Gamepad); // Copy the "Copy Alt1/2" KeyHint
        gamepadHint.name = "Resize Grid";
        gamepadHint.transform.SetSiblingIndex(3);
    }

    static void UpdateKeyboardHints()
    {
        keyboardHarvestModifierKeyLocalized = KeyCodeToLocalizableString(config.KeyboardHarvestModifierKey);

        keyboardHint.transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize(KeyCodeToLocalizableString(config.KeyboardModifierKey));
        keyboardHint.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize(KeyCodeToLocalizableString(config.DecreaseXKey));
        keyboardHint.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize(KeyCodeToLocalizableString(config.IncreaseYKey));
        keyboardHint.transform.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize(KeyCodeToLocalizableString(config.IncreaseXKey));
        keyboardHint.transform.GetChild(6).GetChild(0).GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize(KeyCodeToLocalizableString(config.DecreaseYKey));
    }

    static string KeyCodeToLocalizableString(KeyCode keyCode)
    {
        string keyCodeToPath = ZInput.KeyCodeToPath(keyCode);
        string key = ZInput.instance.MapKeyFromPath(keyCodeToPath);
        string modifiedKey = key.Substring(0, 1).ToLower() + key.Substring(1);
        string buttonDefBindingToName = inputBindingPathToButtonDefNames.TryGetValue(keyCodeToPath, out string buttonDefName) ? buttonDefName : "";
        string localizableKeyString = ZInput.instance.GetBoundKeyString(buttonDefBindingToName, true);

        if (modifiedKey.EndsWith("Arrow"))
        {
            switch(modifiedKey)
            {
                case "upArrow":
                    localizableKeyString = "↑";
                    break;
                case "rightArrow":
                    localizableKeyString = "→";
                    break;
                case "downArrow":
                    localizableKeyString = "↓";
                    break;
                case "leftArrow":
                    localizableKeyString = "←";
                    break;
                default:
                    break;
            }
        }

        return localizableKeyString switch
        {
            "" => ZInput.s_keyLocalizationMap.TryGetValue(modifiedKey, out string keyStringToken) ? keyStringToken : ZInput.KeyCodeToDisplayName(keyCode),
            _ => localizableKeyString
        };
    }

    static void UpdateGamepadHints()
    {
        string keyCodeToPath = ZInput.KeyCodeToPath(config.GamepadModifierKey);
        string buttonDefBindingToName = inputBindingPathToButtonDefNames[keyCodeToPath];
        gamepadModifierKeyLocalized = ZInput.instance.GetBoundKeyString(buttonDefBindingToName);

        if (gamepadHint.TryGetComponent<TextMeshProUGUI>(out var gamepadKeyText))
        {
            string[] gamepadKeys = ["JoyDPadLeft", "JoyDPadUp", "JoyDPadRight", "JoyDPadDown"];
            string full = "";

            System.Array.ForEach(gamepadKeys, gamepadKey => full += ZInput.instance.GetBoundKeyString(gamepadKey));

            gamepadKeyText.text = $"Resize Grid {gamepadModifierKeyLocalized} + {full}";
            Localization.instance.Localize(gamepadKeyText.transform);
        }
    }

    static void SetKeyHintsActive()
    {
        bool flag = config.ShowHUDKeyHints && HoldingCultivator;
        keyboardHint.SetActive(flag);
        gamepadHint.SetActive(flag);
    }
}
