using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectNoBubbleBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ScrollRect _scroll;

    void Awake() => _scroll = GetComponent<ScrollRect>();

    public void OnBeginDrag(PointerEventData e) => _scroll.OnBeginDrag(e);
    public void OnDrag(PointerEventData e) => _scroll.OnDrag(e);
    public void OnEndDrag(PointerEventData e) => _scroll.OnEndDrag(e);
}