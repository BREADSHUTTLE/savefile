#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DG.Tweening;
using DG.DOTweenEditor;
using UnityEngine.UI;

#if TMP_PRESENT
using TMPro;
#endif

[CustomEditor(typeof(UIColorAlphaCurveTween))]
public class UIColorAlphaCurveTweenEditor : Editor
{
    Tween _previewTween;

    float _originalAlpha;
    bool _hasOriginal;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview (Edit Mode)"))
            StartPreview();
        if (GUILayout.Button("Stop Preview"))
            StopPreview(true);
        EditorGUILayout.EndHorizontal();

        if (GUI.changed && DOTweenEditorPreview.isPreviewing)
            StartPreview();
    }

    void StartPreview()
    {
        StopPreview(false);

        // if (!DOTween.initialized)
         //   DOTween.Init(false, true, LogBehaviour.ErrorsOnly);

        var comp = (UIColorAlphaCurveTween)target;

        _previewTween = BuildTweenInEditor(comp);
        if (_previewTween == null) return;

        DOTweenEditorPreview.PrepareTweenForPreview(_previewTween);
        DOTweenEditorPreview.Start();
    }

    void StopPreview(bool restore)
    {
        if (DOTweenEditorPreview.isPreviewing)
            DOTweenEditorPreview.Stop();

        if (_previewTween != null && _previewTween.IsActive())
        {
            _previewTween.Kill();
            _previewTween = null;
        }

        if (restore && _hasOriginal)
        {
            ApplyAlphaToTarget((UIColorAlphaCurveTween)target, _originalAlpha);
            _hasOriginal = false;
        }

        SceneView.RepaintAll();
    }

    Tween BuildTweenInEditor(UIColorAlphaCurveTween comp)
    {
        var so = new SerializedObject(comp);

        int targetType = so.FindProperty("targetType")?.enumValueIndex ?? 0;
        float duration = Mathf.Max(0.0001f, so.FindProperty("duration")?.floatValue ?? 0.25f);
        bool useUnscaled = so.FindProperty("useUnscaledTime")?.boolValue ?? true;

        AnimationCurve curve = so.FindProperty("alphaCurve")?.animationCurveValue
                               ?? AnimationCurve.Linear(0, 1, 1, 1);

        float targetAlpha = so.FindProperty("targetAlpha")?.floatValue ?? 1f;
        targetAlpha = Mathf.Clamp01(targetAlpha);
        float startAlpha = so.FindProperty("startAlpha")?.floatValue ?? 0f;
        startAlpha = Mathf.Clamp01(startAlpha);
        var g = so.FindProperty("targetGraphic")?.objectReferenceValue as Graphic;
        
        // 타겟 찾고 원본 알파 저장
        if (!TryGetTargetAlpha(comp, targetType, out float currentAlpha))
            return null;

        _originalAlpha = currentAlpha;
        _hasOriginal = true;


        Tween tw = DOTween.To(() => startAlpha, t =>
        {
            float a = curve != null ? curve.Evaluate(Mathf.Clamp01(t)) : 1f;
            a = Mathf.Clamp01(a);

            // 런타임과 동일하게 baseAlpha를 최대치로 스케일링
            ApplyAlphaToTarget(comp, a * targetAlpha);

            EditorUtility.SetDirty(comp);
            SceneView.RepaintAll();
        }, 1f, duration);
        
        return tw;
    }

    static bool TryGetTargetAlpha(UIColorAlphaCurveTween comp, int targetType, out float alpha)
    {
        alpha = 1f;

        if (targetType == 0)
        {
            var so = new SerializedObject(comp);
            var g = so.FindProperty("targetGraphic")?.objectReferenceValue as Graphic;
            if (g == null) g = comp.GetComponent<Graphic>();
            if (g == null) { Debug.LogWarning("UIColorAlphaCurveTween: targetGraphic이 없습니다."); return false; }
            alpha = g.color.a;
            return true;
        }

#if TMP_PRESENT
        {
            var so = new SerializedObject(comp);
            var t = so.FindProperty("targetTmp")?.objectReferenceValue as TMP_Text;
            if (t == null) t = comp.GetComponent<TMP_Text>();
            if (t == null) { Debug.LogWarning("UIColorAlphaCurveTween: targetTmp가 없습니다."); return false; }
            alpha = t.color.a;
            return true;
        }
#else
        Debug.LogWarning("UIColorAlphaCurveTween: TMP_PRESENT 정의가 없어서 TMP 타겟을 사용할 수 없습니다.");
        return false;
#endif
    }

    static void ApplyAlphaToTarget(UIColorAlphaCurveTween comp, float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        var so = new SerializedObject(comp);
        int targetType = so.FindProperty("targetType")?.enumValueIndex ?? 0;

        if (targetType == 0)
        {
            var g = so.FindProperty("targetGraphic")?.objectReferenceValue as Graphic;
            if (g == null) g = comp.GetComponent<Graphic>();
            if (g == null) return;

            var c = g.color;
            c.a = alpha;
            g.color = c;

            EditorUtility.SetDirty(g);
            return;
        }

#if TMP_PRESENT
        {
            var t = so.FindProperty("targetTmp")?.objectReferenceValue as TMP_Text;
            if (t == null) t = comp.GetComponent<TMP_Text>();
            if (t == null) return;

            var c = t.color;
            c.a = alpha;
            t.color = c;

            EditorUtility.SetDirty(t);
        }
#endif
    }

    private void OnDisable()
    {
        StopPreview(true);
    }
}
#endif
