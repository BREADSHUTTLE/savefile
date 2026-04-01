using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using CAPYBARA;

/// <summary>
/// 씬/프리팹에서 한글이 하드코딩된 TMP_Text / Text 컴포넌트를 찾아
/// LocalizeText 컴포넌트를 붙이고 descKeys 를 자동 세팅한다.
///
/// ▶ 중복 방지 규칙
///   - 이미 LocalizeText 가 있고 descKeys != None  → SKIPPED (건드리지 않음)
///   - 이미 LocalizeText 가 있지만 descKeys == None → 키만 업데이트
///   - LocalizeText 없음                           → 컴포넌트 추가 후 세팅
/// </summary>
public class LocalizeTextApplier : EditorWindow
{
    // ── 경로 상수 ──────────────────────────────────────────────────────────
    private const string DescJsonPath  = "Assets/13.CapyBara/Resources/JsonData/localizeddesc.json";
    private const string CsvOutputPath = "Assets/localize_applied.csv";
    private static readonly string[] ScenePaths  = { "Assets/01.Scenes" };
    private static readonly string[] PrefabPaths = { "Assets/04.Prefabs" };

    // ── 내부 상태 ──────────────────────────────────────────────────────────
    private Dictionary<string, LocalizeDescKeys> _krToKey;
    private List<string[]> _csvRows;
    private int _totalApplied;
    private int _totalSkipped;
    private int _totalUnmatched;

    // ── 메뉴 진입점 ────────────────────────────────────────────────────────
    [MenuItem("Tools/\U0001f9ab[CapyBara]\U0001f9ab/Localize/한글 텍스트 → LocalizeText 자동 적용")]
    public static void RunFromMenu()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "LocalizeText 자동 적용",
            "씬(Assets/01.Scenes)과 프리팹(Assets/04.Prefabs)의\n" +
            "한글 텍스트에 LocalizeText 컴포넌트를 자동 추가합니다.\n\n" +
            "이미 descKeys 가 세팅된 오브젝트는 건드리지 않습니다.\n\n" +
            "⚠️  파일이 직접 수정됩니다. 계속하시겠습니까?",
            "적용", "취소");
        if (!confirm) return;

        CreateInstance<LocalizeTextApplier>().Execute();
    }

    // ── 실행 ───────────────────────────────────────────────────────────────
    private void Execute()
    {
        if (!LoadLocalizationData()) return;

        _csvRows        = new List<string[]>();
        _totalApplied   = 0;
        _totalSkipped   = 0;
        _totalUnmatched = 0;
        _csvRows.Add(new[] { "File", "GameObject Path", "Korean Text", "Key", "Status" });

        try
        {
            ProcessAllPrefabs();
            ProcessAllScenes();
            WriteCsv();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "완료",
            $"✅ 적용됨                  : {_totalApplied}\n" +
            $"⏭  스킵 (이미 키 세팅됨)  : {_totalSkipped}\n" +
            $"❓ 미매칭 (키 없음)        : {_totalUnmatched}\n\n" +
            $"CSV → {CsvOutputPath}",
            "확인");

        Debug.Log($"[LocalizeTextApplier] 완료 — 적용:{_totalApplied}  스킵:{_totalSkipped}  미매칭:{_totalUnmatched}");
    }

    // ── JSON 로드 ──────────────────────────────────────────────────────────
    private bool LoadLocalizationData()
    {
        _krToKey = new Dictionary<string, LocalizeDescKeys>();
        if (!File.Exists(DescJsonPath))
        {
            EditorUtility.DisplayDialog("오류", $"파일을 찾을 수 없습니다:\n{DescJsonPath}", "확인");
            return false;
        }
        var json    = File.ReadAllText(DescJsonPath, Encoding.UTF8);
        var entries = JsonConvert.DeserializeObject<List<LocalizeEntry>>(json);
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.kr) || e.kr == ".") continue;
            if (Enum.TryParse<LocalizeDescKeys>(e.key, out var key) && key != LocalizeDescKeys.None)
                _krToKey[e.kr] = key;
        }
        Debug.Log($"[LocalizeTextApplier] 로컬라이즈 키 로드: {_krToKey.Count}개");
        return true;
    }

    // ── 프리팹 ─────────────────────────────────────────────────────────────
    private void ProcessAllPrefabs()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", PrefabPaths);
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("프리팹 처리 중...", Path.GetFileName(path), (float)i / guids.Length);
            ProcessPrefab(path);
        }
    }

    private void ProcessPrefab(string prefabPath)
    {
        var root  = PrefabUtility.LoadPrefabContents(prefabPath);
        bool dirty = false;
        try
        {
            foreach (var go in CollectAll(root))
            {
                var tmp = go.GetComponent<TMP_Text>();
                var txt = go.GetComponent<Text>();
                if      (tmp != null) dirty |= Apply(go, tmp.text, prefabPath, isTMP: true);
                else if (txt != null) dirty |= Apply(go, txt.text, prefabPath, isTMP: false);
            }
            if (dirty) PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ── 씬 ────────────────────────────────────────────────────────────────
    private void ProcessAllScenes()
    {
        var guids = AssetDatabase.FindAssets("t:Scene", ScenePaths);
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("씬 처리 중...", Path.GetFileName(path), (float)i / guids.Length);
            ProcessScene(path);
        }
    }

    private void ProcessScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool dirty = false;
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            foreach (var go in CollectAll(rootGo))
            {
                var tmp = go.GetComponent<TMP_Text>();
                var txt = go.GetComponent<Text>();
                if      (tmp != null) dirty |= Apply(go, tmp.text, scenePath, isTMP: true);
                else if (txt != null) dirty |= Apply(go, txt.text, scenePath, isTMP: false);
            }
        }
        if (dirty) EditorSceneManager.SaveScene(scene);
    }

    // ── 핵심 적용 로직 ─────────────────────────────────────────────────────
    /// <returns>true = 파일 수정됨 (dirty)</returns>
    private bool Apply(GameObject go, string text, string filePath, bool isTMP)
    {
        if (!HasKorean(text)) return false;

        var goPath   = GetPath(go);
        var existing = go.GetComponent<LocalizeText>();

        // ── 이미 LocalizeText 가 있고 descKeyName != "" → 완전 스킵 ──────
        if (existing != null)
        {
            var soCheck  = new SerializedObject(existing);
            var keyCheck = soCheck.FindProperty("descKeyName");
            if (keyCheck != null && !string.IsNullOrEmpty(keyCheck.stringValue) &&
                keyCheck.stringValue != LocalizeDescKeys.None.ToString())
            {
                _csvRows.Add(new[] { filePath, goPath, text, keyCheck.stringValue, "SKIPPED" });
                _totalSkipped++;
                return false;
            }
        }

        // ── 매칭 시도 ───────────────────────────────────────────────────
        if (!_krToKey.TryGetValue(text, out var descKey))
        {
            _csvRows.Add(new[] { filePath, goPath, text, "", "UNMATCHED" });
            _totalUnmatched++;
            return false;
        }

        // ── 컴포넌트 추가 or 재사용 ────────────────────────────────────
        var comp = existing != null ? existing : go.AddComponent<LocalizeText>();
        var so   = new SerializedObject(comp);

        // descKeyName (string으로 저장 — enum 순서 변경에 안전)
        var descKeysProp = so.FindProperty("descKeyName");
        descKeysProp.stringValue = descKey.ToString();

        // text 컴포넌트 레퍼런스
        if (isTMP)
            so.FindProperty("myText").objectReferenceValue  = go.GetComponent<TMP_Text>();
        else
            so.FindProperty("myText2").objectReferenceValue = go.GetComponent<Text>();

        so.ApplyModifiedPropertiesWithoutUndo();

        _csvRows.Add(new[] { filePath, goPath, text, descKey.ToString(), "APPLIED" });
        _totalApplied++;
        return true;
    }

    // ── 유틸 ───────────────────────────────────────────────────────────────
    private static IEnumerable<GameObject> CollectAll(GameObject root)
    {
        var stack = new Stack<Transform>();
        stack.Push(root.transform);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            yield return t.gameObject;
            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
    }

    private static bool HasKorean(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (var c in text)
            if ((c >= '\uAC00' && c <= '\uD7A3') || (c >= '\u1100' && c <= '\u11FF') || (c >= '\u3130' && c <= '\u318F'))
                return true;
        return false;
    }

    private static string GetPath(GameObject go)
    {
        var parts = new List<string>();
        for (var t = go.transform; t != null; t = t.parent)
            parts.Insert(0, t.name);
        return string.Join("/", parts);
    }

    private void WriteCsv()
    {
        var sb = new StringBuilder();
        foreach (var row in _csvRows)
        {
            var cells = new string[row.Length];
            for (int i = 0; i < row.Length; i++)
            {
                var c = row[i] ?? "";
                if (c.Contains(",") || c.Contains("\"") || c.Contains("\n") || c.Contains("\r"))
                    c = "\"" + c.Replace("\"", "\"\"") + "\"";
                cells[i] = c;
            }
            sb.AppendLine(string.Join(",", cells));
        }
        File.WriteAllText(CsvOutputPath, sb.ToString(), new UTF8Encoding(true));
        Debug.Log($"[LocalizeTextApplier] CSV 저장: {CsvOutputPath}");
    }

    [Serializable]
    private class LocalizeEntry
    {
        public string key;
        public string kr;
        public string en;
    }
}
