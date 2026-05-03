using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn script này vào prefab QuestLogSlot (slot con được Instantiate vào Content).
/// Mục đích: Ngăn Scroll Rect của QuestSlot (parent) nuốt mất sự kiện click
/// khi người dùng nhấn vào các slot nhiệm vụ bên trái.
/// </summary>
public class StopScrollPropagation : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private ScrollRect parentScrollRect;

    private void Awake()
    {
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null)
            parentScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null)
            parentScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null)
            parentScrollRect.OnEndDrag(eventData);
    }


    public void OnPointerClick(PointerEventData eventData)
    {

        eventData.Use();
    }
}