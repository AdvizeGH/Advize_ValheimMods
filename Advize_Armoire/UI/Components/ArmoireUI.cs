namespace Advize_Armoire;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StaticMembers;

public partial class ArmoireUI : MonoBehaviour
{
    private const int OutfitSlotCount = 3;

    [Header("GameObject References")]
    public GameObject slotsPanel = null;
    public GameObject scrollView = null;
    public GameObject outfitsPanel = null;
    public GameObject scrollViewContent = null;
    public GameObject outfitNumBG = null;

    [Header("Button References")]
    public List<Button> slotButtons = null;
    public Button currentAppearanceButton = null;
    public Button hideSlotButton = null;
    public Toggle toggleHidden = null;
    public Button outfitsButton = null;
    public Button cancelButton = null;
    public List<Button> cycleOutfitButtons = null;
    public Button saveOutfitButton = null;
    public Button loadOutfitButton = null;
    public Button deleteOutfitButton = null;

    [Header("Image References")]
    public Image panelBorder = null;
    public Image verticalScrollbar = null;
    public List<Image> panelBackgrounds = null;
    public List<Image> childIcons = null;
    public List<Image> hintKeyBKG = null;

    [Header("Misc References")]
    public ScrollRect scrollRect = null;
    public UITooltip currentAppearanceButtonTooltip = null;

    [Header("Runtime State")]
    private readonly List<GameObject> generatedScrollViewButtons = [];
    private TextMeshProUGUI _acquiredAppearanceText = null;
    private Button _currentlySelectedButton = null;
    private AppearanceSlotType _lastSlotTypeSelected;
    private int _currentOutfitIndex = 0;

    public void LateUpdate()
    {
        KeyHints.instance.m_barberHints.SetActive(value: true);
        KeyHints.instance.m_combatHints.SetActive(value: false);

        if (ArmoireUIController.HandleEscapeOrCancelInput()) return;

        if (ZInput.GamepadActive && ZInput.GetButtonDown("JoyButtonX") && _currentlySelectedButton && !scrollView.activeSelf)
            AppearanceSlotButtonRightClicked(slotButtons.FindIndex(x => x == _currentlySelectedButton));

        if (ZInput.GetKey(KeyCode.Mouse1, false) || ZInput.IsGamepadActive())
            ControlAttachedPlayer();
    }

    public void OnEnable()
    {
        Dbgl("Enabled");
        _currentlySelectedButton = null;
        cancelButton.Select();
    }

    //Within slots panel

    private void AppearanceSlotButtonClicked(int slotIndex)
    {
        BuildScrollableGrid((AppearanceSlotType)slotIndex);
        ShowScrollView();
    }

    private void AppearanceSlotButtonRightClicked(int slotIndex) => UpdateAppearanceSlot((AppearanceSlotType)slotIndex, string.Empty, 0, hidden: false);

    private void UpdateAppearanceSlotButtonSelection(bool isSelected, int slotIndex) => _currentlySelectedButton = isSelected ? slotButtons[slotIndex] : null;

    //Within scroll view

    private void HideSlotButtonClick()
    {
        UpdateAppearanceSlot(_lastSlotTypeSelected, string.Empty, 0, hidden: true);
        HideScrollView();
    }

    private void UnlockedAppearanceOnClick(int buttonIndex, int variant)
    {
        UpdateAppearanceSlot(_lastSlotTypeSelected, generatedScrollViewButtons[buttonIndex].name, variant, hidden: false);
        HideScrollView();
    }

    private void ToggleHiddenClicked(bool showAllAppearances)
    {
        config.ShowAllAppearances = showAllAppearances;
        RebuildScrollableGrid();
    }

    // Within outfits panel

    private void CycleOutfitButtonClicked(int direction)
    {
        _currentOutfitIndex = (_currentOutfitIndex + direction + OutfitSlotCount) % OutfitSlotCount;
        UpdateOutfitLabel();
        UpdateOutfitState();
        UpdateOutfitIcons();
    }

    private void UpdateOutfitLabel() => outfitNumBG.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Outfit {_currentOutfitIndex + 1}";

    private void UpdateOutfitState()
    {
        if (Player.m_localPlayer.m_customData.TryGetValue(GetOutfitKey(), out string data))
        {
            Dbgl($"Data in outfit slot {_currentOutfitIndex + 1}");
            UpdateOutfitButtonStates(true, new Color32(255, 161, 60, 255));
            AppearanceData.LoadAppearanceData(new ZPackage(data), OutfitOverrides[_currentOutfitIndex]);
            return;
        }

        Dbgl($"No data in outfit slot {_currentOutfitIndex + 1}");
        UpdateOutfitButtonStates(false, new Color32(128, 128, 128, 255));
    }

    private void UpdateOutfitIcons() => UpdateItemSlotIcons(OutfitOverrides[_currentOutfitIndex]);

    private void CancelButtonClicked()
    {
        if (scrollView.activeSelf)
        {
            HideScrollView();
            return;
        }
        if (outfitsPanel.activeSelf)
        {
            HideOutfitsPanel();
            return;
        }
        ArmoireUIController.cancelButtonWasClicked = true;
    }

    private void SaveOutfitButtonClicked()
    {
        AppearanceData.SaveAppearanceData(GetOutfitKey());
        UpdateOutfitState();
        UpdateItemSlotIcons(ActiveOverrides);
    }

    private void LoadOutfitButtonClicked()
    {
        ActiveOverrides = PluginUtils.CloneOverrides(OutfitOverrides[_currentOutfitIndex]);
        Player.m_localPlayer.SetupEquipment();
    }

    private void DeleteOutfitButtonClicked()
    {
        OutfitOverrides[_currentOutfitIndex].Values.ToList().ForEach(slot => slot.ResetSlot());
        Player.m_localPlayer.m_customData.Remove(GetOutfitKey());
        UpdateItemSlotIcons(OutfitOverrides[_currentOutfitIndex]);
    }

    private void UpdateScrollViewButtonSelection(bool selected, GameObject go)
    {
        Dbgl("Updating scroll view button selection");
        go.transform.Find("selected").gameObject.SetActive(selected);

        //Center selected button within viewport
        if (selected)
        {
            RectTransform selectedRect = go.GetComponent<RectTransform>();

            float contentHeight = scrollRect.content.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;

            // Get the position of the target relative to content
            float targetLocalY = -selectedRect.anchoredPosition.y; // Flip because anchoredPosition is bottom-up
            float centerOffset = targetLocalY - viewportHeight / 2f;

            float normalized = Mathf.Clamp01(centerOffset / (contentHeight - viewportHeight));
            scrollRect.verticalNormalizedPosition = 1f - normalized;
        }
    }

    internal void UpdateAppearanceSlot(AppearanceSlotType itemCategory, string itemName, int itemVariant, bool hidden)
    {
        Dbgl("UpdateAppearanceSlot");
        AppearanceSlot slot = ActiveOverrides[itemCategory];
        slot.ItemName = itemName;
        slot.ItemVariant = itemVariant;
        slot.Hidden = hidden;

        UpdateItemIcon(itemCategory, ActiveOverrides);

        Player.m_localPlayer.SetupEquipment();
        AppearanceData.SaveAppearanceData("Armoire_Appearances");
    }

    public void UpdateItemSlotIcons(Dictionary<AppearanceSlotType, AppearanceSlot> overrides)
    {
        Array.ForEach(ActiveOverrides.Keys.ToArray(), itemCategory => UpdateItemIcon(itemCategory, overrides));
    }

    public void UpdateItemIcon(AppearanceSlotType itemCategory, Dictionary<AppearanceSlotType, AppearanceSlot> overrides)
    {
        AppearanceSlot slot = overrides[itemCategory];
        string itemName = slot.ItemName;
        int variant = slot.ItemVariant;
        bool showHildirIcon = slot.Hidden;

        Sprite icon = showHildirIcon ? UIResources.GetSprite("mapicon_hildir") : string.IsNullOrEmpty(itemName) ? null : UIResources.GetItemIcon(itemName, variant);

        Transform iconTransform = slotButtons[(int)itemCategory].transform.GetChild(0);
        GameObject childIcon = iconTransform.childCount > 0 ? iconTransform.GetChild(0).gameObject : null;

        iconTransform.GetComponent<Image>().sprite = icon;
        iconTransform.gameObject.SetActive(icon || showHildirIcon);

        string tooltipText = icon ? (showHildirIcon ? "Slot Hidden" : ZNetScene.instance.GetPrefab(itemName).GetComponent<ItemDrop>().m_itemData.m_shared.m_name) : "";
        slotButtons[(int)itemCategory].GetComponent<UITooltip>().m_topic = tooltipText;

        childIcon?.SetActive(showHildirIcon);
    }

    private void ShowScrollView()
    {
        ToggleScrollView();
        StartCoroutine(DeferredSetCurrentSelected());
    }

    IEnumerator DeferredSetCurrentSelected()
    {
        yield return null; // Wait one frame for scrollable grid to settle
        currentAppearanceButton.Select();
    }

    private void HideScrollView()
    {
        ToggleScrollView();
        slotButtons[(int)_lastSlotTypeSelected].Select();
        DestroyScrollableGrid();
    }

    private void ToggleScrollView()
    {
        bool showScroll = !scrollView.activeSelf;
        scrollView.SetActive(showScroll);
        slotsPanel.SetActive(!showScroll);
        ToggleExtraneousButtons();
    }

    private void ShowOutfitsPanel()
    {
        ToggleOutfitsPanel();
        cancelButton.Select();
    }

    private void HideOutfitsPanel()
    {
        ToggleOutfitsPanel();
        UpdateItemSlotIcons(ActiveOverrides);
        outfitsButton.Select();
    }

    private void ToggleOutfitsPanel()
    {
        bool showOutfits = !outfitsPanel.activeSelf;

        outfitsPanel.SetActive(showOutfits);
        Image bg = panelBackgrounds[1];
        bg.enabled = !showOutfits;
        bg.transform.GetChild(0).gameObject.SetActive(!showOutfits);

        foreach (Button b in slotButtons)
        {
            b.interactable = !b.interactable;
            b.transform.Find("LabelBG").gameObject.SetActive(!showOutfits);
        }

        if (showOutfits)
        {
            UpdateOutfitState();
            UpdateOutfitIcons();
        }

        ToggleExtraneousButtons();
    }

    private string GetOutfitKey() => $"Armoire_Outfit{_currentOutfitIndex + 1}";

    private void UpdateOutfitButtonStates(bool enabled, Color32 color)
    {
        loadOutfitButton.interactable = enabled;
        loadOutfitButton.transform.GetChild(0).GetComponent<Image>().color = color;
        deleteOutfitButton.interactable = enabled;
    }

    internal void ToggleExtraneousButtons(bool forceReset = false)
    {
        outfitsButton.interactable = forceReset || !outfitsButton.interactable;

        for (int i = 0; i < 2; i++)
        {
            bool shouldBeActive = forceReset ? i == 0 : !cancelButton.transform.GetChild(i).gameObject.activeSelf;
            cancelButton.transform.GetChild(i).gameObject.SetActive(shouldBeActive);
        }
    }

    private void BuildScrollableGrid(AppearanceSlotType slotType)
    {
        _lastSlotTypeSelected = slotType;
        AppearanceSlot slot = ActiveOverrides[slotType];
        string prefabName = slot.ItemName;
        int variant = slot.ItemVariant;

        hideSlotButton.gameObject.SetActive(slot.CanBeHidden);

        Transform iconTransform = currentAppearanceButton.transform.GetChild(0);
        Sprite itemIcon = string.IsNullOrEmpty(prefabName) ? null : UIResources.GetItemIcon(prefabName, variant);

        currentAppearanceButtonTooltip.m_topic = itemIcon ? ZNetScene.instance.GetPrefab(prefabName).GetComponent<ItemDrop>().m_itemData.m_shared.m_name : "No Override Selected";

        iconTransform.gameObject.SetActive(itemIcon);

        if (itemIcon)
            iconTransform.GetComponent<Image>().sprite = itemIcon;
        
        int buttonIndex = 0;
        foreach (KeyValuePair<ItemDrop, int> kvp in UnlockedAppearances[slotType])
        {
            for (int iconIndex = 0; iconIndex < kvp.Value; iconIndex++)
            {
                GameObject armoireSlotButton = CreateArmoireSlot(kvp.Key.name, iconIndex);
                int closureIndex = buttonIndex; // avoids closure issue
                int closureIndex2 = iconIndex; // avoids closure issue
                armoireSlotButton.GetComponent<Button>().onClick.AddListener(() => UnlockedAppearanceOnClick(closureIndex, closureIndex2));
                armoireSlotButton.GetComponent<ArmoireInputHandler>().onSelectChange.AddListener((isSelected) => UpdateScrollViewButtonSelection(isSelected, armoireSlotButton));
                generatedScrollViewButtons.Add(armoireSlotButton);
                buttonIndex++;
            }
        }

        if (config.ShowAllAppearances)
        {
            foreach (KeyValuePair<ItemDrop, int> kvp in GetLockedAppearances()[slotType])
            {
                for (int iconIndex = 0; iconIndex < kvp.Value; iconIndex++)
                {
                    GameObject armoireSlotButton = CreateArmoireSlot(kvp.Key.name, iconIndex, locked: true);
                    armoireSlotButton.GetComponent<ArmoireInputHandler>().onSelectChange.AddListener((isSelected) => UpdateScrollViewButtonSelection(isSelected, armoireSlotButton));
                    generatedScrollViewButtons.Add(armoireSlotButton);
                }
            }
        }

        // Filter out current override and variant from grid
        for (int i = 0; i < generatedScrollViewButtons.Count; i++)
        {
            if (generatedScrollViewButtons[i].name != prefabName) continue;

            generatedScrollViewButtons[i + variant].SetActive(false);
            break;
        }

        _acquiredAppearanceText.text = $"{AppearanceTracker.Unlocked(slotType)} / {AppearanceTracker.Total(slotType)}";
    }

    internal void DestroyScrollableGrid()
    {
        Dbgl("Destroying scrollable grid (buttons and listeners)");
        foreach (GameObject go in generatedScrollViewButtons)
        {
            go.GetComponent<ArmoireInputHandler>().RemoveAllListeners();
            Destroy(go);
        }
        generatedScrollViewButtons.Clear();
    }

    internal void RebuildScrollableGrid()
    {
        DestroyScrollableGrid();
        BuildScrollableGrid(_lastSlotTypeSelected);
    }

    private GameObject CreateArmoireSlot(string prefabName, int variant, bool locked = false)
    {
        Sprite itemIcon = UIResources.GetItemIcon(prefabName, variant);
        GameObject newSlot = Instantiate(armoireSlot, scrollViewContent.transform);
        newSlot.name = prefabName;

        UITooltip tooltip = newSlot.GetComponent<UITooltip>();
        tooltip.m_topic = locked ? "???" : ZNetScene.instance.GetPrefab(prefabName).GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
        tooltip.m_text = !locked || config.ShowUndiscoveredHoverDetails ? $"</i><size=90%>{prefabName}{(variant > 0 ? $"\nVariant: {variant}" : "")}<i>" : "";

        Image image = newSlot.transform.GetChild(0).GetComponent<Image>();

        image.sprite = itemIcon;
        image.color = locked ? Color.black : Color.white;
        newSlot.SetActive(true);

        return newSlot;
    }

    private void ControlAttachedPlayer()
    {
        if (Hud.InRadial()) return;

        Vector2 mouseLook;

        if (ZInput.IsGamepadActive())
        {
            mouseLook = Vector2.zero;

            if (!ZInput.GetButton("JoyRotate"))
            {
                float stickX = ZInput.GetJoyRightStickX(true);
                float stickY = ZInput.GetJoyRightStickY(true);
                Vector2 stickInput = new(stickX, -stickY);

                if (PlayerController.cameraDirectionLock != Vector2.zero && stickInput != PlayerController.cameraDirectionLock)
                {
                    PlayerController.cameraDirectionLock = Vector2.zero;
                }

                if (PlayerController.cameraDirectionLock == Vector2.zero)
                {
                    float scale = 110f * Time.deltaTime * PlayerController.m_gamepadSens;
                    mouseLook = stickInput * scale;
                }
            }

            if (PlayerController.m_invertCameraX) mouseLook.x *= -1f;
            if (PlayerController.m_invertCameraY) mouseLook.y *= -1f;
        }
        else
        {
            mouseLook = ZInput.GetMouseDelta() * PlayerController.m_mouseSens * 2;
            if (PlayerController.m_invertMouse) mouseLook.y *= -1f;
        }

        Player.m_localPlayer.SetMouseLook(mouseLook);
        Player.m_localPlayer.GetAttachPoint().localRotation *= Quaternion.Euler(0, -mouseLook.x, 0);
    }
}
