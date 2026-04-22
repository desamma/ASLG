// ScrollRectNoBubbleBlock.cs
// G?n vào cùng GameObject v?i ScrollRect
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectNoBubbleBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ScrollRect _scroll;

    void Awake() => _scroll = GetComponent<ScrollRect>();

    // Cho phép drag scroll bình th??ng
    public void OnBeginDrag(PointerEventData e) => _scroll.OnBeginDrag(e);
    public void OnDrag(PointerEventData e) => _scroll.OnDrag(e);
    public void OnEndDrag(PointerEventData e) => _scroll.OnEndDrag(e);
}