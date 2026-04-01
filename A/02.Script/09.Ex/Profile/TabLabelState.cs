using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class TabLabelState : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Toggle ownerToggle;
    [SerializeField] private RectTransform toggleRoot;

    [Header("TMP Refs")]
    [SerializeField] private TMP_Text tmpDefault;
    [SerializeField] private TMP_Text tmpGlow;

    [Header("Colors")]
    [SerializeField] private Color onDefaultColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    [SerializeField] private Color onPressedColor = new Color32(0x97, 0x9E, 0xAF, 0xFF);
    [SerializeField] private Color offDefaultColor = new Color32(0x97, 0x9E, 0xAF, 0xFF);
    [SerializeField] private Color offPressedColor = new Color32(0x62, 0x67, 0x72, 0xFF);

    [Header("Font Size")]
    [SerializeField] private float selectedFontSize = 36.3f;
    [SerializeField] private float unselectedFontSize = 31.6f;

    [Header("TMP Default Y Offset")]
    [SerializeField] private float selectedYOffset = 5f;
    [SerializeField] private float unselectedYOffset = 0f;

    [Header("Glow (Face Dilate)")]
    [SerializeField] private float onGlowDilate = -0.3f;
    [SerializeField] private float offGlowDilate = -1f;

    [Header("Scale (Press)")]
    [SerializeField] private float downScale = 0.95f;
    [SerializeField] private float upScale = 1.05f;
    [SerializeField] private float settleScale = 1.0f;

    [Header("Anim")]
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    private Tween _colorTween;
    private Tween _sizeTween;
    private Tween _scaleTween;
    private Tween _yPosTween;
    private Tween _dilateTween;

    private bool _isPressed;

    private int _faceDilateId;
    private Material _glowMatInstance;

    private void Awake()
    {
        _faceDilateId = Shader.PropertyToID("_FaceDilate");

        if (ownerToggle == null)
            ownerToggle = GetComponentInParent<Toggle>();

        if (toggleRoot == null && ownerToggle != null)
            toggleRoot = ownerToggle.GetComponent<RectTransform>();

        if (tmpGlow != null)
        {
            var src = tmpGlow.fontSharedMaterial;
            _glowMatInstance = new Material(src);
            tmpGlow.fontSharedMaterial = _glowMatInstance;
            tmpGlow.SetMaterialDirty();
        }

        if (ownerToggle != null)
            ownerToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnEnable()
    {
        bool isOn = ownerToggle != null && ownerToggle.isOn;

        ApplySelectInstant(isOn);
        ApplyDefaultYInstant(isOn);
        ApplyGlowDilateInstant(isOn);

        if (toggleRoot != null)
            toggleRoot.localScale = Vector3.one * settleScale;
    }

    private void OnDestroy()
    {
        if (ownerToggle != null)
            ownerToggle.onValueChanged.RemoveListener(OnToggleChanged);

        if (_glowMatInstance != null)
            Destroy(_glowMatInstance);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (_isPressed)
        {
            ApplyPressedAnimated(isOn);
            ApplyFontSizeAnimated(isOn);
            ApplyDefaultYAnimated(isOn);
            ApplyGlowDilateAnimated(isOn);
            return;
        }

        ApplyDefaultAnimated(isOn);
        ApplyFontSizeAnimated(isOn);
        ApplyDefaultYAnimated(isOn);
        ApplyGlowDilateAnimated(isOn);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;

        bool isOn = ownerToggle != null && ownerToggle.isOn;
        ApplyPressedAnimated(isOn);

        AnimateScale(downScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;

        bool isOn = ownerToggle != null && ownerToggle.isOn;
        ApplyDefaultAnimated(isOn);
        ApplyFontSizeAnimated(isOn);

        if (toggleRoot != null)
        {
            _scaleTween?.Kill(true);
            _scaleTween = DOTween.Sequence()
                .Append(toggleRoot.DOScale(upScale, duration).SetEase(ease))
                .Append(toggleRoot.DOScale(settleScale, duration).SetEase(ease));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPressed = false;

        bool isOn = ownerToggle != null && ownerToggle.isOn;
        ApplyDefaultAnimated(isOn);
        ApplyFontSizeAnimated(isOn);
        ApplyDefaultYAnimated(isOn);
        ApplyGlowDilateAnimated(isOn);

        AnimateScale(settleScale);
    }

    private void ApplyPressedAnimated(bool isOn)
    {
        Color target = isOn ? onPressedColor : offPressedColor;
        AnimateColorPair(target);
    }

    private void ApplyDefaultAnimated(bool isOn)
    {
        Color target = isOn ? onDefaultColor : offDefaultColor;
        AnimateColorPair(target);
    }

    private void ApplySelectInstant(bool isOn)
    {
        Color c = isOn ? onDefaultColor : offDefaultColor;
        float s = isOn ? selectedFontSize : unselectedFontSize;

        if (tmpDefault != null)
        {
            tmpDefault.color = c;
            tmpDefault.fontSize = s;
        }

        if (tmpGlow != null)
        {
            tmpGlow.color = c;
            tmpGlow.fontSize = s;
        }
    }

    private void AnimateColorPair(Color target)
    {
        _colorTween?.Kill(true);

        Color start = GetCurrentColor();
        _colorTween = DOTween.To(
            () => start,
            c =>
            {
                start = c;
                SetColorPair(c);
            },
            target,
            duration
        ).SetEase(ease);
    }

    private void SetColorPair(Color c)
    {
        if (tmpDefault != null) tmpDefault.color = c;
        if (tmpGlow != null) tmpGlow.color = c;
    }

    private Color GetCurrentColor()
    {
        if (tmpDefault != null) return tmpDefault.color;
        if (tmpGlow != null) return tmpGlow.color;
        return Color.white;
    }

    private void ApplyFontSizeAnimated(bool isOn)
    {
        float target = isOn ? selectedFontSize : unselectedFontSize;

        _sizeTween?.Kill(true);

        float start = GetCurrentFontSize();
        _sizeTween = DOTween.To(
            () => start,
            v =>
            {
                start = v;
                SetFontSizePair(v);
            },
            target,
            duration
        ).SetEase(ease);
    }

    private void SetFontSizePair(float v)
    {
        if (tmpDefault != null) tmpDefault.fontSize = v;
        if (tmpGlow != null) tmpGlow.fontSize = v;
    }

    private float GetCurrentFontSize()
    {
        if (tmpDefault != null) return tmpDefault.fontSize;
        if (tmpGlow != null) return tmpGlow.fontSize;
        return unselectedFontSize;
    }

    private void ApplyDefaultYAnimated(bool isOn)
    {
        if (tmpDefault == null) return;

        RectTransform rt = tmpDefault.rectTransform;
        Vector2 pos = rt.anchoredPosition;
        float targetY = isOn ? selectedYOffset : unselectedYOffset;

        _yPosTween?.Kill(true);
        _yPosTween = DOTween.To(
            () => pos.y,
            y =>
            {
                pos.y = y;
                rt.anchoredPosition = pos;
            },
            targetY,
            duration
        ).SetEase(ease);
    }

    private void ApplyDefaultYInstant(bool isOn)
    {
        if (tmpDefault == null) return;

        RectTransform rt = tmpDefault.rectTransform;
        Vector2 pos = rt.anchoredPosition;
        pos.y = isOn ? selectedYOffset : unselectedYOffset;
        rt.anchoredPosition = pos;
    }

    private void ApplyGlowDilateAnimated(bool isOn)
    {
        if (_glowMatInstance == null) return;
        if (!_glowMatInstance.HasProperty(_faceDilateId)) return;

        float target = isOn ? onGlowDilate : offGlowDilate;

        _dilateTween?.Kill(true);
        float from = _glowMatInstance.GetFloat(_faceDilateId);
        _dilateTween = DOTween.To(
            () => from,
            v =>
            {
                from = v;
                _glowMatInstance.SetFloat(_faceDilateId, v);
            },
            target,
            duration
        ).SetEase(ease);
    }

    private void ApplyGlowDilateInstant(bool isOn)
    {
        if (_glowMatInstance == null) return;
        if (!_glowMatInstance.HasProperty(_faceDilateId)) return;

        _glowMatInstance.SetFloat(_faceDilateId, isOn ? onGlowDilate : offGlowDilate);
    }

    private void AnimateScale(float target)
    {
        if (toggleRoot == null) return;

        _scaleTween?.Kill(true);
        _scaleTween = toggleRoot
            .DOScale(target, duration)
            .SetEase(ease);
    }
}
