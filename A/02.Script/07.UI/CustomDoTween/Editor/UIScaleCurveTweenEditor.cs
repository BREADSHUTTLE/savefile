#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DG.Tweening;
using DG.DOTweenEditor; // 중요!

[CustomEditor(typeof(UIScaleCurveTween))]
public class UIScaleCurveTweenEditor : Editor
{
    Tween _previewTween;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview (Edit Mode)"))
        {
            StartPreview();
        }
        if (GUILayout.Button("Stop Preview"))
        {
            StopPreview();
        }
        EditorGUILayout.EndHorizontal();

        // 값 바뀌었을 때 자동 프리뷰 재시작(원하면)
        if (GUI.changed && DOTweenEditorPreview.isPreviewing)
        {
            StartPreview();
        }
    }

    void StartPreview()
    {
        StopPreview();

        var comp = (UIScaleCurveTween)target;

        // 여기서 'Play()'는 런타임 트윈을 만들잖아?
        // 프리뷰에선 똑같이 만들되, DOTweenEditorPreview에 등록해줘야 함.
        // 가장 쉬운 방법: comp.Play()를 그대로 쓰고, 생성된 Tween을 comp에서 노출시키는 것.
        // 근데 지금 comp가 Tween을 private로 들고 있으니, 아래 중 하나 선택:
        // 1) comp에 Preview용 메서드 하나 추가해서 Tween을 리턴
        // 2) 여기서 comp 필드들 읽어서 동일 로직으로 Tween 생성
        //
        // 2) 방식으로 여기서 생성해볼게.

        var tr = GetTargetTransform(comp);
        if (tr == null) return;

        var duration = GetPrivateFloat(comp, "duration");
        var useUnscaled = GetPrivateBool(comp, "useUnscaledTime");
        var curve = GetPrivateCurve(comp, "scaleCurve");
        var captureBase = GetPrivateBool(comp, "captureBaseScaleOnPlay");
        var targetScale = GetPrivateVector3(comp, "targetScale");

        if (captureBase) targetScale = tr.localScale;

        // 트윈 생성
        _previewTween = DOTween.To(() => 0f, t =>
        {
            float m = curve.Evaluate(Mathf.Clamp01(t));
            tr.localScale = targetScale * m;

            // 에디터에서 씬 뷰/인스펙터 갱신
            EditorUtility.SetDirty(tr);
            SceneView.RepaintAll();
        }, 1f, duration);

        _previewTween.SetUpdate(useUnscaled);

        // Editor preview로 등록/재생
        DOTweenEditorPreview.PrepareTweenForPreview(_previewTween);
        DOTweenEditorPreview.Start();
    }

    void StopPreview()
    {
        if (DOTweenEditorPreview.isPreviewing)
            DOTweenEditorPreview.Stop();

        if (_previewTween != null && _previewTween.IsActive())
        {
            _previewTween.Kill();
            _previewTween = null;
        }
    }

    Transform GetTargetTransform(UIScaleCurveTween comp)
    {
        // target 필드가 private이라 reflection으로 가져옴
        var t = GetPrivateObject<Transform>(comp, "target");
        return t ? t : comp.transform;
    }

    // --- private field access helpers (reflection) ---
    static T GetPrivateObject<T>(object obj, string fieldName) where T : class
    {
        var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f?.GetValue(obj) as T;
    }

    static float GetPrivateFloat(object obj, string fieldName)
    {
        var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f != null ? (float)f.GetValue(obj) : 0f;
    }

    static bool GetPrivateBool(object obj, string fieldName)
    {
        var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f != null && (bool)f.GetValue(obj);
    }

    static Vector3 GetPrivateVector3(object obj, string fieldName)
    {
        var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f != null ? (Vector3)f.GetValue(obj) : Vector3.one;
    }

    static AnimationCurve GetPrivateCurve(object obj, string fieldName)
    {
        var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return f != null ? (AnimationCurve)f.GetValue(obj) : AnimationCurve.Linear(0, 1, 1, 1);
    }
}
#endif
