namespace Advize_Armoire;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArmoireInputHandler : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    public UnityEvent onRightClick;
    public UnityEvent<bool> onSelectChange;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke();
    }

    public void OnSelect(BaseEventData eventData) => onSelectChange?.Invoke(true);

    public void OnDeselect(BaseEventData eventData) => onSelectChange?.Invoke(false);

    public void RemoveAllListeners()
    {
        GetComponent<Button>().onClick.RemoveAllListeners();
        onRightClick.RemoveAllListeners();
        onSelectChange.RemoveAllListeners();
    }
}
