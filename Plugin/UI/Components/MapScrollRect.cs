namespace DynamicMaps.UI.Components;

using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapScrollRect : ScrollRect
{
    public System.Action OnBeginDragCallback;
    public System.Action OnDragCallback;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        OnBeginDragCallback?.Invoke();
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        OnDragCallback?.Invoke();
    }
}