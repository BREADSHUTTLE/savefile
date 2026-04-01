#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

#endif
#if UNITY_EDITOR
public class MemoryQuickProfilerWindow : EditorWindow
{
    [MenuItem("Tools/🦫[CapyBara]🦫/SomeTimesUse/Memory Quick Profiler(메모리 프로파일러)")]
    private static void Open() => GetWindow<MemoryQuickProfilerWindow>("Memory Quick");

    private string _report = "—";
    private Vector2 _scroll;

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan (All Loaded Objects)", GUILayout.Height(28))) Scan();
        if (GUILayout.Button("Copy Report")) EditorGUIUtility.systemCopyBuffer = _report;
        EditorGUILayout.EndHorizontal();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox("에디터 추정치라 실제 디바이스/플랫폼의 메모리 사용과 차이가 있다. 빌드 후 프로파일러로 교차검증 권장.", MessageType.Info);
    }

    static long SizeOf<T>() where T : Object
    {
        long total = 0;
        foreach (var obj in Resources.FindObjectsOfTypeAll<T>())
        {
            if (obj.hideFlags.HasFlag(HideFlags.DontSaveInEditor)) continue;
            total += Profiler.GetRuntimeMemorySizeLong(obj);
        }
        return total;
    }

    void Scan()
    {
        long tex = SizeOf<Texture>();
        long mesh = SizeOf<Mesh>();
        long mat = SizeOf<Material>();
        long animClip = SizeOf<AnimationClip>();
        long audio = SizeOf<AudioClip>();
        long compute = SizeOf<ComputeShader>();
        long shaders = SizeOf<Shader>();

        long total = tex + mesh + mat + animClip + audio + compute + shaders;

        var sb = new StringBuilder();
        sb.AppendLine("== Memory Quick Summary ==");
        sb.AppendLine(Line("Textures", tex));
        sb.AppendLine(Line("Meshes", mesh));
        sb.AppendLine(Line("Materials", mat));
        sb.AppendLine(Line("AnimationClips", animClip));
        sb.AppendLine(Line("AudioClips", audio));
        sb.AppendLine(Line("Shaders", shaders));
        sb.AppendLine(Line("ComputeShaders", compute));
        sb.AppendLine("----------------------------------");
        sb.AppendLine(Line("TOTAL (approx)", total));
        sb.AppendLine();
        sb.AppendLine("* EditorUtility.GetRuntimeMemorySizeLong 기준의 러프 추정치");
        _report = sb.ToString();
        Repaint();
        Debug.Log("[MemoryQuick] scanned.");
    }

    static string Line(string k, long bytes)
    {
        float mb = bytes / (1024f * 1024f);
        return $"{k,-18}: {mb:0.0} MB  ({bytes:n0} B)";
    }
}
#endif
