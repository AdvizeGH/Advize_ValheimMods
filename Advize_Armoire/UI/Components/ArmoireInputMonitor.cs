namespace Advize_Armoire;

using UnityEngine;
using UnityEngine.UI;

public class ArmoireInputMonitor : MonoBehaviour
{
    private Button button;
    private Toggle toggle;
    public GameObject hint;
    public string zInputKey;
    public KeyCode keyCode;

    public void Start()
    {
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();
        hint?.SetActive(false);
    }
    
    public void Update()
    {
        hint?.SetActive(IsInteractive());
        if (ButtonPressed())
        {
            if (button)
            {
                button.Select();
                button.OnSubmit(null);
            }
            if (toggle)
            {
                toggle.Select();
                toggle.OnSubmit(null);
            }
        }
    }

    private bool ButtonPressed()
    {
        if (IsBlocked()) return false;

        return (!string.IsNullOrEmpty(zInputKey) && ZInput.GetButtonDown(zInputKey)) || (keyCode != KeyCode.None && ZInput.GetKeyDown(keyCode, false));
    }

    private bool IsBlocked() => global::Console.instance && global::Console.IsVisible();

    private bool IsInteractive() => !((button && !button.interactable) || (toggle && !toggle.interactable));
}
