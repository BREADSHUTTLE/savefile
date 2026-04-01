using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

#if TMP_PRESENT
using TMPro;
#endif

[DisallowMultipleComponent]
public class UIColorAlphaCurveTween : MonoBehaviour,ISequenceTweenItem
{
    public enum TargetType
    {
        Graphic,
#if TMP_PRESENT
        TMP_Text
#endif
    }

    [Header("Target")]
    [SerializeField] private TargetType targetType = TargetType.Graphic;
    [SerializeField] private Graphic targetGraphic; // 비우면 GetComponent<Graphic>()
#if TMP_PRESENT
    [SerializeField] private TMP_Text targetTmp;     // 비우면 GetComponent<TMP_Text>()
#endif

    [Header("Timing")]
    [Min(0.0001f)]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Alpha Curve (t:0~1 -> alpha 0~1)")]
    [SerializeField] private AnimationCurve alphaCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [Header("Base Alpha")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float targetAlpha = 1f;

    Tween _tween;

    private void Reset()
    {
        AutoBind();
    }

    private void Awake()
    {
        AutoBind();
    }

    void AutoBind()
    {
        if (targetType == TargetType.Graphic)
        {
            if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
        }
#if TMP_PRESENT
        else
        {
            if (targetTmp == null) targetTmp = GetComponent<TMP_Text>();
        }
#endif
    }

    public Tween BuildTween()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill();

        _tween = DOTween.To(() => startAlpha, t =>
        {
            float a = alphaCurve != null ? alphaCurve.Evaluate(Mathf.Clamp01(t)) : 1f;
            a = Mathf.Clamp01(a);

            // baseAlpha를 “최대치”로 보고 스케일링 (원치 않으면 a만 쓰면 됨)
            SetAlpha(a * targetAlpha);
        }, 1f, duration)
        .SetUpdate(useUnscaledTime)
        .OnComplete(() =>
        {
            // 🔥 트윈 종료 시 무조건 알파 0
            SetAlpha(0f);
        });

        _tween.SetUpdate(useUnscaledTime);
        return _tween;
    }

    public Tween Play()
    {
        var tw = BuildTween();
        tw.Play();
        return tw;
    }

    public void Kill(bool resetToBase = true)
    {
        if (_tween != null && _tween.IsActive())
        {
            _tween.Kill();
            _tween = null;
        }

        if (resetToBase)
            SetAlpha(targetAlpha);
    }

    float GetAlpha()
    {
        if (targetType == TargetType.Graphic)
        {
            if (targetGraphic == null) return targetAlpha;
            return targetGraphic.color.a;
        }
#if TMP_PRESENT
        else
        {
            if (targetTmp == null) return baseAlpha;
            return targetTmp.color.a;
        }
#else
        return targetAlpha;
#endif
    }

    void SetAlpha(float a)
    {
        a = Mathf.Clamp01(a);

        if (targetType == TargetType.Graphic)
        {
            if (targetGraphic == null) return;
            var c = targetGraphic.color;
            c.a = a;
            targetGraphic.color = c;
        }
#if TMP_PRESENT
        else
        {
            if (targetTmp == null) return;
            var c = targetTmp.color;
            c.a = a;
            targetTmp.color = c;
        }
#endif
    }
}
