using UnityEngine;
using UnityEngine.EventSystems;

public class DetectPointerExitAndClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isHolding = false;
    private GameObject thisObject;

    public UnityEngine.Events.UnityEvent onCancel;

    void Awake()
    {
        thisObject = gameObject;
    }

    void Update()
    {
        if (isHolding && Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverGameObject(thisObject))
            {
                Debug.Log("다른 곳을 눌렀으므로 취소 이벤트 발동");
                onCancel.Invoke();
                isHolding = false;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
    }

    private bool IsPointerOverGameObject(GameObject target)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject == target || result.gameObject.transform.IsChildOf(target.transform))
                return true;
        }

        return false;
    }
}
