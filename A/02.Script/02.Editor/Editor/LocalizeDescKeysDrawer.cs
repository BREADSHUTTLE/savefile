using System;
using UnityEditor;
using UnityEngine;
using CAPYBARA;

/// <summary>
/// LocalizeText 컴포넌트의 descKeyName(string) 필드를
/// 인스펙터에서 검색 가능한 드롭다운으로 표시합니다.
///
/// string으로 저장하기 때문에 LocalizeDescKeys enum 중간에
/// 값이 추가되어 순서가 바뀌어도 기존 데이터가 깨지지 않습니다.
/// </summary>
[CustomEditor(typeof(LocalizeText))]
public class LocalizeTextEditor : Editor
{
    private static readonly string[] EnumNames =
        Enum.GetNames(typeof(LocalizeDescKeys));

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // m_Script 필드 (읽기 전용으로 표시)
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        // ── descKeyName : 검색 팝업 버튼 ────────────────────────────────
        var descKeyProp = serializedObject.FindProperty("descKeyName");
        string currentName = descKeyProp.stringValue;
        bool isValid = !string.IsNullOrEmpty(currentName) &&
                       Array.IndexOf(EnumNames, currentName) >= 0;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Desc Key");

        var btnLabel  = isValid ? currentName : (string.IsNullOrEmpty(currentName) ? "None" : $"⚠ {currentName}");
        var btnStyle  = new GUIStyle(EditorStyles.popup);
        if (!isValid) btnStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

        if (GUILayout.Button(btnLabel, btnStyle))
        {
            var rect       = GUILayoutUtility.GetLastRect();
            var screenRect = GUIUtility.GUIToScreenRect(rect);
            LocalizeKeysSearchPopup.Show(screenRect, serializedObject, descKeyProp.propertyPath);
        }
        EditorGUILayout.EndHorizontal();

        // ── 나머지 필드 ─────────────────────────────────────────────────
        DrawPropertiesExcluding(serializedObject, "m_Script", "descKeyName");

        serializedObject.ApplyModifiedProperties();
    }
}

/// <summary>
/// 검색 필드가 있는 드롭다운 팝업.
/// SerializedProperty(string)에 enum 이름을 직접 기록합니다.
/// </summary>
public class LocalizeKeysSearchPopup : EditorWindow
{
    // ── 상태 ────────────────────────────────────────────────────────────────
    private SerializedObject _serializedObject;
    private string           _propertyPath;
    private string[]         _enumNames;
    private string           _currentName;

    private string           _search      = "";
    private Vector2          _scroll;
    private bool             _focusSearch = true;

    // ── 오픈 ────────────────────────────────────────────────────────────────
    public static void Show(Rect screenRect, SerializedObject so, string propertyPath)
    {
        var window               = CreateInstance<LocalizeKeysSearchPopup>();
        window._serializedObject = so;
        window._propertyPath     = propertyPath;
        window._enumNames        = Enum.GetNames(typeof(LocalizeDescKeys));
        window._currentName      = so.FindProperty(propertyPath).stringValue;
        window._search           = "";
        window._focusSearch      = true;
        window.ShowAsDropDown(screenRect, new Vector2(280, 340));
    }

    // ── GUI ─────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        // ── 검색 필드 ────────────────────────────────────────────────────
        GUILayout.Space(4f);
        GUI.SetNextControlName("LocalizeSearch");
        EditorGUI.BeginChangeCheck();
        _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
        if (EditorGUI.EndChangeCheck())
            _scroll = Vector2.zero;

        if (_focusSearch)
        {
            EditorGUI.FocusTextInControl("LocalizeSearch");
            _focusSearch = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            Close();
            return;
        }

        GUILayout.Space(2f);
        DrawSeparator();

        // ── 목록 ─────────────────────────────────────────────────────────
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        bool hasResult = false;
        foreach (var name in _enumNames)
        {
            if (!string.IsNullOrEmpty(_search) &&
                name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            hasResult = true;
            bool isSelected = name == _currentName;

            var style = new GUIStyle(EditorStyles.label);
            style.padding.left  = 6;
            style.padding.right = 6;
            if (isSelected)
            {
                style.fontStyle            = FontStyle.Bold;
                style.normal.textColor     = new Color(0.2f, 0.6f, 1f);
            }

            var rowRect = GUILayoutUtility.GetRect(
                new GUIContent(name), style, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                if (isSelected)
                    EditorGUI.DrawRect(rowRect, new Color(0.2f, 0.5f, 1f, 0.12f));
                else if (rowRect.Contains(Event.current.mousePosition))
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.07f));
            }

            EditorGUI.LabelField(rowRect, name, style);

            if (Event.current.type == EventType.MouseDown &&
                rowRect.Contains(Event.current.mousePosition))
            {
                ApplySelection(name);
                Event.current.Use();
                return;
            }
        }

        if (!hasResult)
            GUILayout.Label("(결과 없음)", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndScrollView();
    }

    // ── 선택 적용 ────────────────────────────────────────────────────────────
    private void ApplySelection(string name)
    {
        _serializedObject.Update();
        var prop = _serializedObject.FindProperty(_propertyPath);
        if (prop != null)
        {
            prop.stringValue = name;
            _serializedObject.ApplyModifiedProperties();
        }
        Close();
    }

    // ── 구분선 ───────────────────────────────────────────────────────────────
    private static void DrawSeparator()
    {
        var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
    }
}
