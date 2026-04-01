using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TabButtonController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform tabButton;
    [SerializeField] private ToggleGroup toggleGroup;

    [Header("Anim")]
    [SerializeField] private float duration = 0.22f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField] private float widthPadding = 24f;

    private Sequence seq;

    private void Start()
    {
        var toggles = toggleGroup.GetComponentsInChildren<Toggle>(true);
        Debug.Log($"[TabButtonController] toggles found: {toggles.Length}");

        foreach (var t in toggles)
        {
            t.group = toggleGroup; 

            var captured = t;
            captured.onValueChanged.AddListener(isOn =>
            {
                if (isOn) OnTabSelected(captured);
            });
        }

        foreach (var t in toggles)
        {
            if (t.isOn)
            {
                SnapTo(t);
                break;
            }
        }
    }

    // Toggle ON 되었을 때 호출
    private void OnTabSelected(Toggle t)
    {
        // 레이아웃 갱신 (폭/위치 정확도 확보)
        var parent = t.transform.parent as RectTransform;
        if (parent)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);

        RectTransform tab = t.GetComponent<RectTransform>();

        float targetX = tab.anchoredPosition.x;
        float targetW = tab.rect.width + widthPadding;

        seq?.Kill(false);

        seq = DOTween.Sequence()
            .Join(tabButton.DOAnchorPosX(targetX, duration).SetEase(ease))
            .Join(tabButton.DOSizeDelta(
                new Vector2(targetW, tabButton.sizeDelta.y),
                duration
            ).SetEase(ease));
    }

    private void SnapTo(Toggle t)
    {
        var parent = t.transform.parent as RectTransform;
        if (parent)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);

        RectTransform tab = t.GetComponent<RectTransform>();

        float x = tab.anchoredPosition.x;
        float w = tab.rect.width + widthPadding;

        tabButton.anchoredPosition = new Vector2(x, tabButton.anchoredPosition.y);
        tabButton.sizeDelta = new Vector2(w, tabButton.sizeDelta.y);
    }
}
