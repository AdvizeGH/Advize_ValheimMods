namespace Advize_Armoire;

using UnityEngine;
using UnityEngine.EventSystems;
using static StaticMembers;

public class ArmoireUIDragHandler : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform panelTransform = null;

    private Vector2 _lastUpdatedPosition;
    private bool _hasBeenDragged;

    public void OnDrag(PointerEventData eventData)
    {
        if (!config.AllowDragging || !ZInput.GetKey(KeyCode.LeftControl)) return;

        _hasBeenDragged = true;
        panelTransform.anchoredPosition += eventData.delta;
        _lastUpdatedPosition = panelTransform.anchoredPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_hasBeenDragged) return;

        config.UIPosition = _lastUpdatedPosition;
        _hasBeenDragged = false;
    }
}
