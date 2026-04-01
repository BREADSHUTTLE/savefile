#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement; // 일부 버전 호환
using UnityEngine;
using System;

[InitializeOnLoad]
static class SafeInspectorReload
{
    static SafeInspectorReload()
    {
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
        AssemblyReloadEvents.afterAssemblyReload += OnAfterReload;

        Selection.selectionChanged += StripNulls;
        EditorApplication.projectChanged += StripNulls;
        EditorApplication.hierarchyChanged += StripNulls;
    }

    static void OnBeforeReload()
    {
        // 프리팹 스테이지 열려 있으면 메인으로
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null) StageUtility.GoToMainStage();

        ClearSel();
    }

    static void OnAfterReload()
    {
        // 한 프레임 뒤 다시 한 번 선택 비우기
        EditorApplication.delayCall += ClearSel;
    }

    static void StripNulls()
    {
        var objs = Selection.objects;
        if (objs == null || objs.Length == 0) return;

        bool hasNull = false;
        foreach (var o in objs) { if (o == null) { hasNull = true; break; } }
        if (hasNull) Selection.objects = Array.FindAll(objs, o => o != null);
    }

    static void ClearSel() => Selection.objects = Array.Empty<UnityEngine.Object>(); 
}
#endif