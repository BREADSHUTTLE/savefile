using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class LongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float holdDuration = 1f;
    private bool isPressed = false;
    private float pressTime = 0f;

    public UnityEvent onLongPress; // ✅ 인스펙터에서 연결 가능

    void Update()
    {
        if (isPressed)
        {
            pressTime += Time.unscaledDeltaTime;
            if (pressTime >= holdDuration)
            {
                isPressed = false;
                onLongPress.Invoke(); // 호출
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        pressTime = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPressed = false;
    }
}
