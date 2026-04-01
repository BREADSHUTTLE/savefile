#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerPrefsWindow : EditorWindow
{
    // ─────────────────────────────────────────────────────────────────────────────
    // 상태 저장 (EditorPrefs)
    // ─────────────────────────────────────────────────────────────────────────────
    const string kStateKey = "PlayerPrefsWindow_State_v1";

    [Serializable]
    public enum PrefType { Int, Float, String }

    [Serializable]
    public class Entry
    {
        public string key;
        public PrefType type = PrefType.String;
        public int intValue;
        public float floatValue;
        public string stringValue;

        public string DisplayValue =>
            type switch {
                PrefType.Int    => intValue.ToString(),
                PrefType.Float  => floatValue.ToString("G9"),
                _               => stringValue ?? string.Empty
            };
    }

    [Serializable]
    public class State
    {
        public List<Entry> entries = new List<Entry>();
        public string search = "";
        public bool showHelp = false;
    }

    State _state = new State();
    Vector2 _scroll;
    string _newKey = "";
    PrefType _newType = PrefType.String;

    // ─────────────────────────────────────────────────────────────────────────────
    // 메뉴 & 라이프사이클
    // ─────────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/🦫[CapyBara]🦫/PlayerPrefs Viewer (EditorWindow)")]
    public static void Open()
    {
        var win = GetWindow<PlayerPrefsWindow>("PlayerPrefs");
        win.minSize = new Vector2(680, 360);
        win.Show();
    }

    void OnEnable()  => LoadState();
    void OnDisable() => SaveState();

    void LoadState()
    {
        if (!EditorPrefs.HasKey(kStateKey)) return;
        try
        {
            var json = EditorPrefs.GetString(kStateKey);
            var st = JsonUtility.FromJson<State>(json);
            if (st != null) _state = st;
        }
        catch { /* ignore */ }
    }

    void SaveState()
    {
        try
        {
            var json = JsonUtility.ToJson(_state);
            EditorPrefs.SetString(kStateKey, json);
        }
        catch { /* ignore */ }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // GUI
    // ─────────────────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(6);
        DrawAddKeyRow();
        EditorGUILayout.Space(6);
        DrawList();
        EditorGUILayout.Space(8);
        DrawFooter();
    }

    void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            var searchStyle =
                GUI.skin.FindStyle("ToolbarSearchTextField") ??
                GUI.skin.FindStyle("ToolbarSeachTextField") ??     // 구버전 오타
                EditorStyles.toolbarSearchField ??                 // 있으면 사용
                GUI.skin.textField;                                // 최종 안전 fallback

            var cancelStyle =
                GUI.skin.FindStyle("ToolbarSearchCancelButton") ??
                GUI.skin.FindStyle("ToolbarSeachCancelButton") ??  // 구버전 오타
                GUI.skin.button;                                   // fallback

// 교체 라인
            _state.search = GUILayout.TextField(_state.search, searchStyle, GUILayout.MinWidth(180));
            
            if (GUILayout.Button("x", cancelStyle, GUILayout.Width(20))) {
                _state.search = "";
                GUI.FocusControl(null);
            }
            GUILayout.FlexibleSpace();

            _state.showHelp = GUILayout.Toggle(_state.showHelp, "Help", EditorStyles.toolbarButton);
            if (GUILayout.Button("Refresh All", EditorStyles.toolbarButton))
            {
                foreach (var e in _state.entries)
                    ReadFromPlayerPrefs(e, quiet: true);
                Repaint();
            }
        }

        if (_state.showHelp)
        {
            EditorGUILayout.HelpBox(
                "Unity는 PlayerPrefs의 전체 키를 나열하는 API가 없습니다.\n" +
                "이 창은 **네가 관리할 키 목록**을 EditorPrefs에 기억해두고, 각 키를 Read/Set/Delete 합니다.\n" +
                "상단 Search로 필터링, 하단 버튼으로 일괄 Read/Set/DeleteAll 가능합니다.",
                MessageType.Info);
        }
    }

    void DrawAddKeyRow()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Add / Focus Key", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _newKey = EditorGUILayout.TextField("Key", _newKey);
                _newType = (PrefType)EditorGUILayout.EnumPopup(_newType, GUILayout.MaxWidth(120));

                GUI.enabled = !string.IsNullOrWhiteSpace(_newKey);
                var exists = _state.entries.Exists(x => x.key == _newKey);
                if (GUILayout.Button(exists ? "Focus" : "Add", GUILayout.Width(90)))
                {
                    var e = GetOrAdd(_newKey.Trim(), _newType);
                    ReadFromPlayerPrefs(e);
                    SaveState();
                }
                GUI.enabled = true;
            }
        }
    }

    void DrawList()
    {
        // 헤더
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.MinWidth(180));
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Value", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("", GUILayout.Width(270));
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        var list = string.IsNullOrEmpty(_state.search)
            ? _state.entries
            : _state.entries.FindAll(e => e.key.IndexOf(_state.search, StringComparison.OrdinalIgnoreCase) >= 0);

        if (list.Count == 0)
        {
            GUILayout.Space(6);
            EditorGUILayout.HelpBox("등록된(또는 필터된) 키가 없습니다. 위에서 Key를 추가하세요.", MessageType.Info);
        }

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                // Key
                e.key = EditorGUILayout.TextField(e.key, GUILayout.MinWidth(180));

                // Type
                e.type = (PrefType)EditorGUILayout.EnumPopup(e.type, GUILayout.Width(60));

                // Value
                switch (e.type)
                {
                    case PrefType.Int:
                        e.intValue = EditorGUILayout.IntField(e.intValue);
                        break;
                    case PrefType.Float:
                        e.floatValue = EditorGUILayout.FloatField(e.floatValue);
                        break;
                    default:
                        e.stringValue = EditorGUILayout.TextField(e.stringValue);
                        break;
                }

                // Buttons
                using (new EditorGUILayout.HorizontalScope(GUILayout.Width(270)))
                {
                    if (GUILayout.Button("Read", GUILayout.Width(60)))
                        ReadFromPlayerPrefs(e);

                    if (GUILayout.Button("Set", GUILayout.Width(60)))
                        WriteToPlayerPrefs(e);

                    if (GUILayout.Button("Delete", GUILayout.Width(70)))
                        DeleteKey(e);

                    if (GUILayout.Button("X", GUILayout.Width(30)))
                    {
                        _state.entries.RemoveAll(x => x == e);
                        SaveState();
                        break;
                    }

                    // 존재 표시
                    var has = PlayerPrefs.HasKey(e.key);
                    var c = GUI.color;
                    GUI.color = has ? Color.green : Color.gray;
                    GUILayout.Label(has ? "exists" : "no key", GUILayout.Width(50));
                    GUI.color = c;
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawFooter()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Read All", GUILayout.Height(26)))
            {
                foreach (var e in _state.entries) ReadFromPlayerPrefs(e, quiet: true);
                Repaint();
            }

            if (GUILayout.Button("Set All", GUILayout.Height(26)))
            {
                foreach (var e in _state.entries) WriteToPlayerPrefs(e);
                PlayerPrefs.Save();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("PlayerPrefs.DeleteAll", "경고: 모든 PlayerPrefs 삭제"), GUILayout.Height(26), GUILayout.Width(200)))
            {
                if (EditorUtility.DisplayDialog("Delete All PlayerPrefs?",
                    "현재 플랫폼의 모든 PlayerPrefs를 삭제합니다. 계속할까요?",
                    "Delete All", "Cancel"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    ShowNotification(new GUIContent("Deleted all PlayerPrefs."));
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 내부 로직
    // ─────────────────────────────────────────────────────────────────────────────
    Entry GetOrAdd(string key, PrefType typeIfNew)
    {
        var e = _state.entries.Find(x => x.key == key);
        if (e == null)
        {
            e = new Entry { key = key, type = typeIfNew };
            _state.entries.Add(e);
        }
        return e;
    }

    void ReadFromPlayerPrefs(Entry e, bool quiet = false)
    {
        if (string.IsNullOrEmpty(e.key)) return;

        if (!PlayerPrefs.HasKey(e.key))
        {
            if (!quiet) ShowNotification(new GUIContent($"Key not found: {e.key}"));
            return;
        }

        switch (e.type)
        {
            case PrefType.Int:   e.intValue    = PlayerPrefs.GetInt(e.key);   break;
            case PrefType.Float: e.floatValue  = PlayerPrefs.GetFloat(e.key); break;
            default:             e.stringValue = PlayerPrefs.GetString(e.key);break;
        }
        SaveState();
    }

    void WriteToPlayerPrefs(Entry e)
    {
        if (string.IsNullOrEmpty(e.key)) return;

        switch (e.type)
        {
            case PrefType.Int:   PlayerPrefs.SetInt(e.key, e.intValue);         break;
            case PrefType.Float: PlayerPrefs.SetFloat(e.key, e.floatValue);     break;
            default:             PlayerPrefs.SetString(e.key, e.stringValue ?? ""); break;
        }
        PlayerPrefs.Save();
        ShowNotification(new GUIContent($"Set {e.key} = {e.DisplayValue}"));
    }

    void DeleteKey(Entry e)
    {
        if (string.IsNullOrEmpty(e.key)) return;

        if (PlayerPrefs.HasKey(e.key))
        {
            PlayerPrefs.DeleteKey(e.key);
            PlayerPrefs.Save();
            ShowNotification(new GUIContent($"Deleted key: {e.key}"));
        }
        else
        {
            ShowNotification(new GUIContent($"Key not found: {e.key}"));
        }
    }
}
#endif
