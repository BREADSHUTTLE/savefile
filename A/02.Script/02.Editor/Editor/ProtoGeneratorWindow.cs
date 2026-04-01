using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

public class ProtoGeneratorWindow : EditorWindow
{
    string protocPath = @"C:\Users\USER\Downloads\protoc-3.12.3-win64\bin\protoc.exe";
    string protoDir   = @"C:\Users\USER\Documents\protocol";
    string outDir     = @"C:\Users\USER\ATOZ poker\ATOZPOKER\Assets\02.Script\01.Generated";

    [MenuItem("Tools/🦫[CapyBara]🦫/ProtoGenerator")]
    static void Open()
    {
        GetWindow<ProtoGeneratorWindow>("Proto Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Protoc Settings", EditorStyles.boldLabel);

        protocPath = EditorGUILayout.TextField("protoc.exe Path", protocPath);
        protoDir   = EditorGUILayout.TextField("Proto Directory", protoDir);
        outDir     = EditorGUILayout.TextField("Output Directory", outDir);

        if (GUILayout.Button("Generate .cs from .proto"))
        {
            Generate();
        }
    }

    void Generate()
    {
        if (!File.Exists(protocPath))
        {
            UnityEngine.Debug.LogError("protoc.exe not found at: " + protocPath);
            return;
        }

        if (!Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        // 모든 proto 파일 변환
        var protoFiles = Directory.GetFiles(protoDir, "*.proto", SearchOption.AllDirectories);
        foreach (var protoFile in protoFiles)
        {
            var args = $"--experimental_allow_proto3_optional -I=\"{protoDir}\" --csharp_out=\"{outDir}\" \"{protoFile}\"";
            RunProcess(protocPath, args);
        }

        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("Proto generation completed.");
    }

    void RunProcess(string exePath, string args)
    {
        var proc = new Process();
        proc.StartInfo.FileName = exePath;
        proc.StartInfo.Arguments = args;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.RedirectStandardError = true;
        proc.Start();

        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (!string.IsNullOrEmpty(stdout))
            UnityEngine.Debug.Log(stdout);
        if (!string.IsNullOrEmpty(stderr))
            UnityEngine.Debug.LogError(stderr);
    }
}
