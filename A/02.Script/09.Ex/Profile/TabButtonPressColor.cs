using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class TabButtonPressColor : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Tab Button Image")]
    [SerializeField] private Image tabButton; // 공용 HighlightPill

    [Header("Owner Toggle (this tab)")]
    [SerializeField] private Toggle ownerToggle;

    [Header("선택된 탭일 때만 눌림 연출 적용")]
    [SerializeField] private bool onlyWhenSelected = true;

    [Header("클릭 시 토글 선택")]
    [SerializeField] private bool clickSelectsToggle = true;

    [Header("Press Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private Ease pressEase = Ease.OutQuad;

    private Tween _tween;

    private void Awake()
    {
        if (ownerToggle == null)
            ownerToggle = GetComponentInParent<Toggle>();

        if (tabButton != null)
            normalColor = tabButton.color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanReactPress()) return;
        PlayColor(pressedColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (clickSelectsToggle && ownerToggle != null && !ownerToggle.isOn)
            ownerToggle.isOn = true;

        if (!CanReactPress()) return;
        PlayColor(normalColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanReactPress()) return;
        PlayColor(normalColor);
    }

    private bool CanReactPress()
    {
        if (tabButton == null) return false;
        if (!onlyWhenSelected) return true;
        return ownerToggle != null && ownerToggle.isOn;
    }

    private void PlayColor(Color target)
    {
        _tween?.Kill(true);
        _tween = tabButton.DOColor(target, pressDuration).SetEase(pressEase);
    }
}
