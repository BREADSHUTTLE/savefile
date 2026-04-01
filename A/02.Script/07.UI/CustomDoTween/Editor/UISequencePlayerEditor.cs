#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DG.Tweening;
using DG.DOTweenEditor;
using Unity.VisualScripting;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;

#if TMP_PRESENT
using TMPro;
#endif

// UISequencePlayer(Item.actions: List<Component>) 구조 대응 버전
[CustomEditor(typeof(UISequencePlayer))]
public class UISequencePlayerEditor : Editor
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
            StopPreview(restore: true);
        EditorGUILayout.EndHorizontal();

        // 인스펙터 값 바꾸는 즉시 프리뷰 갱신(원하면 유지)
        if (GUI.changed && DOTweenEditorPreview.isPreviewing)
            StartPreview();
    }

    void StartPreview()
    {
        StopPreview(restore: false);

        EnsureDOTweenInitialized();

        var player = (UISequencePlayer)target;

        _previewSeq = DOTween.Sequence();
        _previewSeq.SetUpdate(player.useUnscaledTime);

        foreach (var item in player.items)
        {
            if (item == null || item.actions == null) continue;

            foreach (var comp in item.actions)
            {
                if (comp == null) continue;
                SetAlpha(comp, 0.0f);
            }
            foreach (var comp in item.actions)
            {
                if (comp == null) continue;

                Tween tw = BuildTweenForPreview(comp);
                if (tw == null) continue;

                _previewSeq.Insert(item.delay, tw);
            }
        }

        DOTweenEditorPreview.PrepareTweenForPreview(_previewSeq);
        DOTweenEditorPreview.Start();
    }

    void StopPreview(bool restore)
    {
        if (DOTweenEditorPreview.isPreviewing)
            DOTweenEditorPreview.Stop();

        if (_previewSeq != null && _previewSeq.IsActive())
        {
            _previewSeq.Kill();
            _previewSeq = null;
        }

        if (restore)
            RestoreAllItems();

        SceneView.RepaintAll();
    }

    void RestoreAllItems()
    {
        var player = (UISequencePlayer)target;
        if (player == null) return;

        foreach (var item in player.items)
        {
            if (item?.actions == null) continue;

            foreach (var comp in item.actions)
            {
                if (comp == null) continue;
                InvokeKillBool(comp, true);
            }
        }
    }

    static void EnsureDOTweenInitialized()
    {
        // DOTween 버전별 API 차이 회피: 그냥 Init 안전 호출
        try { DOTween.Init(false, true, LogBehaviour.ErrorsOnly); }
        catch { try { DOTween.Init(); } catch { /* ignore */ } }
    }

    Tween BuildTweenForPreview(Component comp)
    {
        // ✅ 에디터에서 안정적으로 돌리기 위해 known type은 "풀어서" 생성
        if (comp is UIScaleCurveTween scaleTween)
            return BuildScaleTweenInEditor(scaleTween);

        if (comp is UIColorAlphaCurveTween alphaTween)
            return BuildAlphaTweenInEditor(alphaTween);

        // 기타 타입은 런타임 빌더로 fallback
        if (comp is ISequenceTweenItem builder)
            return builder.BuildTween();

        Debug.LogWarning($"UISequencePlayer Preview: '{comp.name}'는 지원 타입이 아니고 ISequenceTweenItem도 아닙니다.");
        return null;
    }

    static void SetAlpha(Component comp, float a)
    {
        if (comp is UIColorAlphaCurveTween alphaTween)
        {
            var so = new SerializedObject(comp);

            int targetType = so.FindProperty("targetType")?.enumValueIndex ?? 0;
            float duration = Mathf.Max(0.0001f, so.FindProperty("duration")?.floatValue ?? 0.25f);
            bool useUnscaled = so.FindProperty("useUnscaledTime")?.boolValue ?? true;
            AnimationCurve curve = so.FindProperty("alphaCurve")?.animationCurveValue
                                   ?? AnimationCurve.Linear(0, 1, 1, 1);

            float targetAlpha = Mathf.Clamp01(so.FindProperty("targetAlpha")?.floatValue ?? 1f);

            float startAlpha = so.FindProperty("startAlpha")?.floatValue ?? 0f;
            startAlpha = Mathf.Clamp01(startAlpha);

            if (targetType == 0) // Graphic
            {
                var g = so.FindProperty("targetGraphic")?.objectReferenceValue as Graphic;
                if (g == null) g = comp.GetComponent<Graphic>();
                if (g == null)
                {
                    Debug.LogWarning("UIColorAlphaCurveTween Preview: targetGraphic이 없습니다.");
                }

                var c = g.color;
                c.a = a;
                g.color = c;
            }
            else // TMP
            {
#if TMP_PRESENT
            var tmp = so.FindProperty("targetTmp")?.objectReferenceValue as TMP_Text;
            if (tmp == null) tmp = comp.GetComponent<TMP_Text>();
            if (tmp == null)
            {
                Debug.LogWarning("UIColorAlphaCurveTween Preview: targetTmp가 없습니다.");
                return null;
            }

          var c = tmp.color;
                c.a = a;
                tmp.color = c;

#else
                Debug.LogWarning("UIColorAlphaCurveTween Preview: TMP_PRESENT가 없어서 TMP 타겟을 프리뷰할 수 없습니다.");

#endif
            }
        }
       
    }

    static void InvokeKillBool(Component comp, bool reset)
    {
        var m = comp.GetType().GetMethod("Kill",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new[] { typeof(bool) }, null);

        if (m != null)
        {
            try { m.Invoke(comp, new object[] { reset }); }
            catch { /* ignore */ }
        }
    }

    // ------------------------------------------------------------
    //  Editor Build: UIScaleCurveTween (BuildTween 호출 안 함)
    // ------------------------------------------------------------
    static Tween BuildScaleTweenInEditor(UIScaleCurveTween comp)
    {
        var so = new SerializedObject(comp);

        Transform target = so.FindProperty("target")?.objectReferenceValue as Transform;
        if (target == null) target = comp.transform;

        float duration = Mathf.Max(0.0001f, so.FindProperty("duration")?.floatValue ?? 0.25f);
        bool useUnscaled = so.FindProperty("useUnscaledTime")?.boolValue ?? true;
        AnimationCurve curve = so.FindProperty("scaleCurve")?.animationCurveValue
                               ?? AnimationCurve.Linear(0, 1, 1, 1);

        Vector3 targetScale = so.FindProperty("targetScale")?.vector3Value ?? Vector3.one;
        

        Tween tw = DOTween.To(() => 0f, t =>
        {
            float m = curve.Evaluate(Mathf.Clamp01(t));
            target.localScale = targetScale * m;

            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }, 1f, duration);

        tw.SetUpdate(useUnscaled);
        return tw;
    }

    // ------------------------------------------------------------
    //  Editor Build: UIColorAlphaCurveTween (BuildTween 호출 안 함)
    // ------------------------------------------------------------
    static Tween BuildAlphaTweenInEditor(UIColorAlphaCurveTween comp)
    {
        var so = new SerializedObject(comp);

        int targetType = so.FindProperty("targetType")?.enumValueIndex ?? 0;
        float duration = Mathf.Max(0.0001f, so.FindProperty("duration")?.floatValue ?? 0.25f);
        bool useUnscaled = so.FindProperty("useUnscaledTime")?.boolValue ?? true;
        AnimationCurve curve = so.FindProperty("alphaCurve")?.animationCurveValue
                               ?? AnimationCurve.Linear(0, 1, 1, 1);

        float targetAlpha = Mathf.Clamp01(so.FindProperty("targetAlpha")?.floatValue ?? 1f);

        float startAlpha = so.FindProperty("startAlpha")?.floatValue ?? 0f;
        startAlpha = Mathf.Clamp01(startAlpha);
        
        if (targetType == 0) // Graphic
        {
            var g = so.FindProperty("targetGraphic")?.objectReferenceValue as Graphic;
            if (g == null) g = comp.GetComponent<Graphic>();
            if (g == null)
            {
                Debug.LogWarning("UIColorAlphaCurveTween Preview: targetGraphic이 없습니다.");
                return null;
            }


            Tween tw = DOTween.To(() => startAlpha, t =>
            {
                float a = Mathf.Clamp01(curve.Evaluate(Mathf.Clamp01(t)));
                a = Mathf.Clamp01(a * targetAlpha);

                var c = g.color;
                c.a = a;
                g.color = c;

                EditorUtility.SetDirty(g);
                SceneView.RepaintAll();
            }, 1f, duration);
    

            tw.SetUpdate(useUnscaled);
            return tw;
        }
        else // TMP
        {
#if TMP_PRESENT
            var tmp = so.FindProperty("targetTmp")?.objectReferenceValue as TMP_Text;
            if (tmp == null) tmp = comp.GetComponent<TMP_Text>();
            if (tmp == null)
            {
                Debug.LogWarning("UIColorAlphaCurveTween Preview: targetTmp가 없습니다.");
                return null;
            }

            if (captureBase) baseAlpha = tmp.color.a;

            Tween tw = DOTween.To(() => 0f, t =>
            {
                float a = Mathf.Clamp01(curve.Evaluate(Mathf.Clamp01(t)));
                a = Mathf.Clamp01(a * baseAlpha);

                var c = tmp.color;
                c.a = a;
                tmp.color = c;

                EditorUtility.SetDirty(tmp);
                SceneView.RepaintAll();
            }, 1f, duration);

            tw.SetUpdate(useUnscaled);
            return tw;
#else
            Debug.LogWarning("UIColorAlphaCurveTween Preview: TMP_PRESENT가 없어서 TMP 타겟을 프리뷰할 수 없습니다.");
            return null;
#endif
        }
    }

    private void OnDisable()
    {
        // 인스펙터 포커스 이동/컴파일 시 프리뷰가 남으면 골치아픔
        StopPreview(restore: true);
    }
}
#endif
