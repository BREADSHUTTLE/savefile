#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using DG.DOTweenEditor;
using DG.Tweening;

[CustomEditor(typeof(UIScaleCurveSequencePlayer))]
public class UIScaleCurveSequencePlayerEditor : Editor
{
    Sequence _previewSeq;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview (Edit Mode)"))
            StartPreview();
        if (GUILayout.Button("Stop Preview"))
            StopPreview();
        EditorGUILayout.EndHorizontal();
    }

    void StartPreview()
    {
        StopPreview();

        var player = (UIScaleCurveSequencePlayer)target;

        // DOTweenEditorPreview가 있는지 확인 (리플렉션)
        var editorPreviewType = Type.GetType("DG.DOTweenEditor.DOTweenEditorPreview, DOTweenEditor");
        if (editorPreviewType == null)
        {
            Debug.LogWarning("DOTween Editor Preview가 없습니다. (DOTweenEditorPreview 미탑재) 에디터 프리뷰는 불가하고, 플레이모드에서는 정상 동작합니다.");
            return;
        }

        // 시퀀스 빌드
        _previewSeq = DOTween.Sequence();
        _previewSeq.SetUpdate(player.useUnscaledTime);

        foreach (var it in player.items)
        {
            if (it == null || it.tween == null) continue;

            // BuildTween() 호출 금지: 에디터 프리뷰에서는 직접 트윈을 만든다
            var tw = BuildTweenInEditor(it.tween, player.useUnscaledTime);
            if (tw == null) continue;

            _previewSeq.Insert(it.delay, tw);
        }

        // 에디터 프리뷰로 돌리기
        DOTweenEditorPreview.PrepareTweenForPreview(_previewSeq);
        DOTweenEditorPreview.Start();
    }

  void StopPreview()
    {
        if (DOTweenEditorPreview.isPreviewing)
            DOTweenEditorPreview.Stop();

        if (_previewSeq != null && _previewSeq.IsActive())
        {
            _previewSeq.Kill();
            _previewSeq = null;
        }

        SceneView.RepaintAll();
    }

    static Tween BuildTweenInEditor(UIScaleCurveTween comp, bool seqUseUnscaledTime)
    {
        // UIScaleCurveTween의 private [SerializeField] 들을 에디터에서 읽는다
        var so = new SerializedObject(comp);

        Transform target = GetTransform(so, "target") ?? comp.transform;
        if (target == null) return null;

        float duration = GetFloat(so, "duration", 0.25f);
        bool tweenUseUnscaled = GetBool(so, "useUnscaledTime", true);
        var curve = GetCurve(so, "scaleCurve");
        bool captureBase = GetBool(so, "captureBaseScaleOnPlay", true);
        Vector3 targetScale = GetVector3(so, "targetScale", Vector3.one);

        // baseScale 결정: 런타임 로직과 동일하게
        if (captureBase) targetScale = target.localScale;

        // 에디터 프리뷰는 “값 적용 + 씬 갱신”까지 같이 해줘야 눈에 보임
        Tween tw = DOTween.To(() => 0f, t =>
        {
            float m = curve != null ? curve.Evaluate(Mathf.Clamp01(t)) : 1f;
            target.localScale = targetScale * m;

            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }, 1f, Mathf.Max(0.0001f, duration));

        // 개별 트윈의 unscaled 설정을 우선 적용(원하면 seqUseUnscaledTime 강제해도 됨)
        tw.SetUpdate(tweenUseUnscaled);

        return tw;
    }

    // ---- SerializedProperty helpers ----
    static Transform GetTransform(SerializedObject so, string name)
    {
        var p = so.FindProperty(name);
        if (p == null) return null;
        return p.objectReferenceValue as Transform;
    }

    static float GetFloat(SerializedObject so, string name, float fallback)
    {
        var p = so.FindProperty(name);
        return p != null ? p.floatValue : fallback;
    }

    static bool GetBool(SerializedObject so, string name, bool fallback)
    {
        var p = so.FindProperty(name);
        return p != null ? p.boolValue : fallback;
    }

    static Vector3 GetVector3(SerializedObject so, string name, Vector3 fallback)
    {
        var p = so.FindProperty(name);
        return p != null ? p.vector3Value : fallback;
    }

    static AnimationCurve GetCurve(SerializedObject so, string name)
    {
        var p = so.FindProperty(name);
        return p != null ? p.animationCurveValue : AnimationCurve.Linear(0, 1, 1, 1);
    }
}
#endif
