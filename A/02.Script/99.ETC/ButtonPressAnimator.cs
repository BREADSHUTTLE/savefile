using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Button))]
public class ButtonPressAnimator : 
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler,
    IPointerEnterHandler,
    IEndDragHandler
{
    Animator anim;
    Button button;
    bool isPointerDown;

    void Awake()
    {
        anim   = GetComponent<Animator>();
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 버튼이 비활성화 상태면 아무 것도 하지 않음
        if (!button.interactable) return;

        isPointerDown = true;
        anim.SetTrigger("Pressed");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isPointerDown = false;
        anim.SetTrigger("Normal");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!button.interactable) return;

        if (isPointerDown)
            anim.SetTrigger("Normal");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;

        if (isPointerDown)
            anim.SetTrigger("Pressed");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!button.interactable) return;

        if (isPointerDown)
        {
            isPointerDown = false;
            anim.SetTrigger("Normal");
        }
    }
}
