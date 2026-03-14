namespace Advize_PlantEasily;

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ModContext;
using static ModUtils;

[HarmonyPatch]
static class KeyHintPatches
{
    internal static string KeyboardHarvestModifierKeyLocalized;
    internal static string GamepadModifierKeyLocalized;

    private static GameObject _keyboardHint;
    private static GameObject _gamepadHint;
    private static readonly Dictionary<string, string> _inputBindingPathToButtonDefNames = [];

    internal static void UpdateKeyHintText()
    {
        if (_keyboardHint) UpdateKeyboardHints();
        if (_gamepadHint) UpdateGamepadHints();
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
        if (!_gamepadHint)
            CreateGamepadHints();
        UpdateGamepadHints();
    }

    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> UpdateHintsTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
        .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PlayerCustomizaton), nameof(PlayerCustomizaton.IsBarberGuiVisible))))
        .ThrowIfInvalid("Could not patch KeyHints.UpdateHints() (BuildHUD Key Hints)")
        .Advance(-2)
        .InsertAndAdvance(instructions: [new(OpCodes.Call, AccessTools.Method(typeof(KeyHintPatches), nameof(SetKeyHintsActive)))])
        .InstructionEnumeration();
    }

    //Patch to a completely different class to support all of this thanks to bog witch update
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.AddButton))]
    [HarmonyPrefix]
    static void AddButton(string name, string path) => _inputBindingPathToButtonDefNames[path] = name;

    static void CreateKeyBoardHints()
    {
        Transform keyboardRoot = KeyHints.m_instance.m_buildHints.transform.Find("Keyboard");

        _keyboardHint = Object.Instantiate(keyboardRoot.Find("Copy").gameObject, keyboardRoot); // Copy the "Copy" KeyHint
        _keyboardHint.name = "Resize Grid";

        Transform hintRoot = _keyboardHint.transform;
        Transform label = hintRoot.GetChild(0);

        hintRoot.SetSiblingIndex(3);
        label.GetComponent<TextMeshProUGUI>().text = "Grid Size";
        label.GetComponent<LayoutElement>().preferredWidth = 75;

        GameObject keyTemplate = hintRoot.GetChild(1).gameObject;
        for (int i = 0; i < 3; i++)
            Object.Instantiate(keyTemplate, hintRoot);

        UpdateKeyboardHints();
    }

    static void CreateGamepadHints()
    {
        Transform gamepadRoot = KeyHints.m_instance.m_buildHints.transform.Find("Gamepad");

        // Can't use transform.Find() with a '/' in the string or it searches hierarchy like a path name
        GameObject hintTemplate = gamepadRoot.GetComponentsInChildren<Transform>().Where(x => x.gameObject.name == "Text - Copy Alt1/2").First().gameObject;

        _gamepadHint = Object.Instantiate(hintTemplate, gamepadRoot); // Copy the "Copy Alt1/2" KeyHint
        _gamepadHint.name = "Resize Grid";
        _gamepadHint.transform.SetSiblingIndex(3);
    }

    static void UpdateKeyboardHints()
    {
        KeyboardHarvestModifierKeyLocalized = KeyCodeToLocalizableString(config.KeyboardHarvestModifierKey);

        Localization localization = Localization.instance;
        Transform hintRoot = _keyboardHint.transform;

        hintRoot.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = localization.Localize(KeyCodeToLocalizableString(config.KeyboardModifierKey));
        hintRoot.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = localization.Localize(KeyCodeToLocalizableString(config.DecreaseXKey));
        hintRoot.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = localization.Localize(KeyCodeToLocalizableString(config.IncreaseYKey));
        hintRoot.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>().text = localization.Localize(KeyCodeToLocalizableString(config.IncreaseXKey));
        hintRoot.GetChild(6).GetChild(0).GetComponent<TextMeshProUGUI>().text = localization.Localize(KeyCodeToLocalizableString(config.DecreaseYKey));
    }

    static string KeyCodeToLocalizableString(KeyCode keyCode)
    {
        string keyCodeToPath = ZInput.KeyCodeToPath(keyCode);
        string key = ZInput.instance.MapKeyFromPath(keyCodeToPath);
        string modifiedKey = key.Substring(0, 1).ToLower() + key.Substring(1);
        string buttonDefBindingToName = _inputBindingPathToButtonDefNames.TryGetValue(keyCodeToPath, out string buttonDefName) ? buttonDefName : "";
        string localizableKeyString = ZInput.instance.GetBoundKeyString(buttonDefBindingToName, true);

        if (modifiedKey.EndsWith("Arrow"))
        {
            switch (modifiedKey)
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
        string buttonDefBindingToName = _inputBindingPathToButtonDefNames[keyCodeToPath];
        GamepadModifierKeyLocalized = ZInput.instance.GetBoundKeyString(buttonDefBindingToName);

        if (_gamepadHint.TryGetComponent(out TextMeshProUGUI gamepadKeyText))
        {
            string[] gamepadKeys = ["JoyDPadLeft", "JoyDPadUp", "JoyDPadRight", "JoyDPadDown"];
            string full = "";

            System.Array.ForEach(gamepadKeys, gamepadKey => full += ZInput.instance.GetBoundKeyString(gamepadKey));

            gamepadKeyText.text = $"Resize Grid {GamepadModifierKeyLocalized} + {full}";
            Localization.instance.Localize(gamepadKeyText.transform);
        }
    }

    static void SetKeyHintsActive()
    {
        bool flag = config.ShowHUDKeyHints && HoldingCultivator;
        _keyboardHint.SetActive(flag);
        _gamepadHint.SetActive(flag);
    }
}
