#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class SceneAuditWindow : EditorWindow
{
    [MenuItem("Tools/🦫[CapyBara]🦫/SomeTimesUse/Scene Scanner(씬내 누락컴포넌트 등 감지)")]
    private static void Open() => GetWindow<SceneAuditWindow>("Scene Scanner");

    private Vector2 _scroll;
    private List<GameObject> _missingScripts = new();
    private List<GameObject> _rbNoCollider = new();
    private List<GameObject> _mrNoMf = new();
    private List<GameObject> _imgNoSprite = new();

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Current Scene", GUILayout.Height(28))) Scan();
        if (GUILayout.Button("Select All MissingScripts")) Select(_missingScripts);
        if (GUILayout.Button("Select RB No Collider")) Select(_rbNoCollider);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawList("Missing Script", _missingScripts);
        DrawList("Rigidbody but no Collider", _rbNoCollider);
        DrawList("MeshRenderer but no MeshFilter", _mrNoMf);
        DrawList("UI Image but no Sprite", _imgNoSprite);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox("일반적 사고 포인트를 기본 규칙으로 넣었고, 필요하면 규칙 더 추가해서 쓰면 됨.", MessageType.Info);
    }

    void DrawList(string title, List<GameObject> list)
    {
        EditorGUILayout.LabelField($"{title} ({list.Count})", EditorStyles.boldLabel);
        foreach (var go in list)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
            if (GUILayout.Button("Ping", GUILayout.Width(60))) EditorGUIUtility.PingObject(go);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Space(8);
    }

    void Select(List<GameObject> list)
    {
        Selection.objects = list.Where(x => x != null).ToArray();
    }

    void Scan()
    {
        _missingScripts.Clear();
        _rbNoCollider.Clear();
        _mrNoMf.Clear();
        _imgNoSprite.Clear();

        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;

                // 1) Missing Scripts
                var comps = go.GetComponents<Component>();
                foreach (var c in comps)
                    if (c == null) { _missingScripts.Add(go); break; }

                // 2) Rigidbody & no Collider
                if (go.TryGetComponent<Rigidbody>(out _))
                {
                    if (!go.TryGetComponent<Collider>(out _) &&
                        go.GetComponentInChildren<Collider>(true) == null)
                        _rbNoCollider.Add(go);
                }

                // 3) MeshRenderer & no MeshFilter
                if (go.TryGetComponent<MeshRenderer>(out _))
                {
                    if (!go.TryGetComponent<MeshFilter>(out _) &&
                        go.GetComponentInChildren<MeshFilter>(true) == null)
                        _mrNoMf.Add(go);
                }

                // 4) UI Image but no Sprite
                if (go.TryGetComponent<Image>(out var img))
                {
                    if (img.sprite == null) _imgNoSprite.Add(go);
                }
            }
        }

        _missingScripts = _missingScripts.Distinct().ToList();
        _rbNoCollider = _rbNoCollider.Distinct().ToList();
        _mrNoMf = _mrNoMf.Distinct().ToList();
        _imgNoSprite = _imgNoSprite.Distinct().ToList();

        Repaint();
        Debug.Log($"[SceneAudit] Done. Missing:{_missingScripts.Count} RBxCol:{_rbNoCollider.Count} MRxMF:{_mrNoMf.Count} ImgNoSprite:{_imgNoSprite.Count}");
    }
}
#endif
