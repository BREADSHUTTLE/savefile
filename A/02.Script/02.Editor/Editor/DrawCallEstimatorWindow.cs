#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DrawCallEstimatorWindow : EditorWindow
{
    [MenuItem("Tools/🦫[CapyBara]🦫/SomeTimesUse/Draw Call Estimator(드로우콜 측정)")]
    private static void Open() => GetWindow<DrawCallEstimatorWindow>("DrawCall Estimator");

    private Vector2 _scroll;
    private bool _onlySelected;

    class Row { public Renderer renderer; public int submeshCount; public int materials; }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        _onlySelected = EditorGUILayout.ToggleLeft("Only Selected Objects", _onlySelected, GUILayout.Width(180));
        if (GUILayout.Button("Estimate", GUILayout.Height(28))) Estimate();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Result (rough)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(_summary, EditorStyles.helpBox);

        foreach (var r in _rows)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(r.renderer, typeof(Renderer), true);
            GUILayout.Label($"SubMeshes:{r.submeshCount}  Materials:{r.materials}", GUILayout.Width(220));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox("같은 머티리얼/쉐이더/키워드, 같은 라이트맵/라이트프로브, GPU 인스턴싱 등 조건에 따라 실제 배칭 수가 달라진다. 본 도구는 '상한선에 가까운 러프한 추정'이다.", MessageType.Info);
    }

    private string _summary = "—";
    private readonly List<Row> _rows = new();

    void Estimate()
    {
        _rows.Clear();

        IEnumerable<GameObject> targets;
        if (_onlySelected && Selection.gameObjects.Length > 0)
            targets = Selection.gameObjects.SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);
        else
            targets = GameObject.FindObjectsOfType<Transform>(true).Select(t => t.gameObject);

        var renderers = targets
            .SelectMany(go => go.GetComponents<Renderer>())
            .Where(r => r.enabled && r.gameObject.activeInHierarchy)
            .ToArray();

        int totalRenderers = 0;
        int totalSubmeshes = 0;
        int totalMaterials = 0;

        foreach (var r in renderers)
        {
            int sm = 1;
            if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null) sm = smr.sharedMesh.subMeshCount;
            else if (r is MeshRenderer mr)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf && mf.sharedMesh) sm = mf.sharedMesh.subMeshCount;
            }

            int mats = r.sharedMaterials?.Count(m => m != null) ?? 0;
            if (mats == 0) mats = 1;

            _rows.Add(new Row { renderer = r, submeshCount = sm, materials = mats });
            totalRenderers++;
            totalSubmeshes += sm;
            totalMaterials += mats;
        }

        // 드로우콜 상한 추정: 보수적으로 materials 또는 submeshes 중 큰 값 합을 더함
        int roughDraws = _rows.Sum(x => Mathf.Max(x.submeshCount, x.materials));

        _summary = $"Renderers: {totalRenderers}, SubMeshes: {totalSubmeshes}, Materials: {totalMaterials}\n" +
                   $"Rough Draw Calls (upper bound-ish): ~ {roughDraws}";

        Repaint();
        Debug.Log($"[DrawCallEstimator] Renderers:{totalRenderers} RoughDraws:{roughDraws}");
    }
}
#endif
