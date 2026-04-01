#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class DefineSymbolsEditorWindow : EditorWindow
{
    private string defineInput = "";
    private List<string> currentDefines = new List<string>();
    private List<string> allDefines = new List<string>();
    private Vector2 scroll;
    private const string AllDefinesKey = "DefineSymbolsEditorWindow_AllDefines";

    [MenuItem("Tools/🦫[CapyBara]🦫/SomeTimesUse/Define Symbol Editor(디파인(정의값) 수정하기)")]
    public static void ShowWindow()
    {
        GetWindow<DefineSymbolsEditorWindow>("Define Symbol Editor");
    }

    private void OnEnable()
    {
        RefreshDefineSymbols();
        LoadAllDefines();
    }

    private void OnGUI()
    {
        GUILayout.Label("쉼표(,)로 여러 디파인 입력 (예: MYCompany,TEST,DEBUG_MODE)", EditorStyles.boldLabel);
        defineInput = EditorGUILayout.TextField("Define Symbols", defineInput);

        if (GUILayout.Button("등록/추가"))
        {
            AddDefineSymbols(defineInput);
            defineInput = "";
        }

        GUILayout.Space(10);
        GUILayout.Label("[내부 저장 리스트] 모든 디파인 심볼", EditorStyles.boldLabel);

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(220));
        foreach (var symbol in allDefines)
        {
            EditorGUILayout.BeginHorizontal();
            bool isRegistered = currentDefines.Contains(symbol);
            EditorGUILayout.LabelField(symbol, GUILayout.Width(200));
            GUI.enabled = !isRegistered;
            if (GUILayout.Button("등록", GUILayout.Width(50)))
                RegisterSymbol(symbol);
            GUI.enabled = isRegistered;
            if (GUILayout.Button("해제", GUILayout.Width(50)))
                UnregisterSymbol(symbol);
            GUI.enabled = true;
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
                DeleteSymbol(symbol);
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("새로고침"))
        {
            RefreshDefineSymbols();
            LoadAllDefines();
        }
    }

    private void AddDefineSymbols(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        var inputList = input.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        foreach (var symbol in inputList)
        {
            if (!allDefines.Contains(symbol))
                allDefines.Add(symbol);
        }
        SaveAllDefines();
        RegisterSymbols(inputList);
        RefreshDefineSymbols();
    }

    private void RegisterSymbol(string symbol)
    {
        RegisterSymbols(new List<string> { symbol });
        RefreshDefineSymbols();
    }

    private void RegisterSymbols(List<string> symbols)
    {
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        var defineList = current.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        foreach (var s in symbols)
        {
            if (!defineList.Contains(s))
                defineList.Add(s);
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defineList));
    }

    private void UnregisterSymbol(string symbol)
    {
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        var defineList = current.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        if (defineList.Remove(symbol))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defineList));
            RefreshDefineSymbols();
        }
    }

    private void DeleteSymbol(string symbol)
    {
        if (EditorUtility.DisplayDialog("삭제 확인", $"[{symbol}] 심볼을 정말 삭제하시겠습니까?\n(모든 리스트에서 완전히 제거)", "삭제", "취소"))
        {
            UnregisterSymbol(symbol); // 프로젝트에 적용 중이면 해제
            allDefines.Remove(symbol);
            SaveAllDefines();
            RefreshDefineSymbols();
        }
    }

    private void RefreshDefineSymbols()
    {
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        currentDefines = current.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
    }

    private void LoadAllDefines()
    {
        string saved = EditorPrefs.GetString(AllDefinesKey, "");
        allDefines = saved.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
        // 한 번도 저장된 적 없다면 현재 정의된 심볼 가져오기
        if (allDefines.Count == 0)
            allDefines = currentDefines.ToList();
    }

    private void SaveAllDefines()
    {
        EditorPrefs.SetString(AllDefinesKey, string.Join(";", allDefines));
    }
}
#endif
