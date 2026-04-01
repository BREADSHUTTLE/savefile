using UnityEngine;
using UnityEngine.UI;

public class GaugeFillActivator : MonoBehaviour
{
    public Image targetImage;
    public GameObject[] objectsToToggle;

    [Range(0f, 1f)]
    public float threshold = 0.99f;

    private bool isOn = false;

    void Start()
    {
        if (targetImage == null) return;

        // 시작 시점 fillAmount에 따라 초기 상태 강제 적용
        float fill = targetImage.fillAmount;
        isOn = fill >= threshold;

        SetObjectsActive(isOn);
    }

    void Update()
    {
        if (targetImage == null) return;

        float fill = targetImage.fillAmount;
        bool shouldBeOn = fill >= threshold;

        if (shouldBeOn == isOn) return;

        isOn = shouldBeOn;
        SetObjectsActive(isOn);
    }

    void SetObjectsActive(bool active)
    {
        foreach (var obj in objectsToToggle)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}
