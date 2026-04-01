using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class UIScaleCurveTween : MonoBehaviour,ISequenceTweenItem
{
    [Header("Targets")]
    [SerializeField] private Transform target; // 비우면 자기 자신

    [Header("Timing")]
    [Min(0.0001f)]
    [SerializeField] public float duration = 0.25f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Curve (t:0~1 -> scale multiplier)")]
    [SerializeField] private AnimationCurve scaleCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.7f, 1.3f),
            new Keyframe(1f, 1f)
        );

    [Header("Scale Base")]
    [SerializeField] private Vector3 targetScale = Vector3.one;

    [Header("Options")]
    [SerializeField] private bool resetToBaseOnKill = true;

    Tween _tween;

    private Transform Target => target ? target : transform;

    private void Awake()
    {
        if (target == null) target = transform;
    }

    public Tween BuildTween()
    {
        // 기존 트윈 제거(빌드만 할 때도 안전하게)
        if (_tween != null && _tween.IsActive())
            _tween.Kill();

        _tween = DOTween.To(() => 0f, t =>
        {
            float m = scaleCurve.Evaluate(Mathf.Clamp01(t));
            Target.localScale = targetScale * m;
        }, 1f, duration);

        _tween.SetUpdate(useUnscaledTime);

        // Kill되면 원복 옵션
        if (resetToBaseOnKill)
        {
            _tween.OnKill(() =>
            {
                if (Target != null)
                    Target.localScale = targetScale;
            });
        }

        return _tween;
    }

    public Tween Play()
    {
        var tw = BuildTween();
        tw.Play();
        return tw;
    }

    public void Kill(bool reset)
    {
        if (_tween != null && _tween.IsActive())
        {
            _tween.Kill();
            _tween = null;
        }

        if (reset)
            Target.localScale = targetScale;
    }
}
