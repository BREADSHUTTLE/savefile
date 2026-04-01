using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollDragForwarder : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler, IScrollHandler
{
    public  ScrollRect scrollRect;

    void Awake()
    {
        if (!scrollRect) scrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (scrollRect) scrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect) scrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (scrollRect) scrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrollRect) scrollRect.OnEndDrag(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect) scrollRect.OnScroll(eventData);
    }
}