using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEngine;
using Directory = UnityEngine.Windows.Directory;

public class SimpleBuildWindow : EditorWindow
{
    private const string PREF_BUILD_PATH = "SimpleBuildWindow.BuildPath";

    string buildPath;
    
    private const string PREF_KEYSTORE_PATH = "SimpleBuildWindow.KeystorePath";
    private const string PREF_KEYSTORE_PASS = "SimpleBuildWindow.KeystorePass";
    private const string PREF_KEYALIAS_NAME = "SimpleBuildWindow.KeyaliasName";
    private const string PREF_KEYALIAS_PASS = "SimpleBuildWindow.KeyaliasPass";
    string keystorePath;
    string keystorePass;
    string keyaliasName;
    string keyaliasPass;
    
    [MenuItem("Tools/Simple Build/Addressables + Android")]
    static void Open()
    {
        GetWindow<SimpleBuildWindow>("Simple Build");
    }
    void OnEnable()
    {
        // 기본값 + 저장된 값 복원
        buildPath = EditorPrefs.GetString(
            PREF_BUILD_PATH,
            "Builds/Android"
        );
        
        keystorePath = EditorPrefs.GetString(PREF_KEYSTORE_PATH, PlayerSettings.Android.keystoreName);
        keystorePass = EditorPrefs.GetString(PREF_KEYSTORE_PASS, "");
        keyaliasName = EditorPrefs.GetString(PREF_KEYALIAS_NAME, PlayerSettings.Android.keyaliasName);
        keyaliasPass = EditorPrefs.GetString(PREF_KEYALIAS_PASS, "");
    }

    void OnDisable()
    {
        EditorPrefs.SetString(PREF_BUILD_PATH, buildPath);
        
        EditorPrefs.SetString(PREF_KEYSTORE_PATH, keystorePath ?? "");
        EditorPrefs.SetString(PREF_KEYSTORE_PASS, keystorePass ?? "");
        EditorPrefs.SetString(PREF_KEYALIAS_NAME, keyaliasName ?? "");
        EditorPrefs.SetString(PREF_KEYALIAS_PASS, keyaliasPass ?? "");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Android Keystore (Saved in EditorPrefs)", EditorStyles.boldLabel);

        keystorePath = EditorGUILayout.TextField("Keystore Path", keystorePath);
        keyaliasName = EditorGUILayout.TextField("Alias Name", keyaliasName);

        keystorePass = EditorGUILayout.PasswordField("Keystore Pass", keystorePass);
        keyaliasPass = EditorGUILayout.PasswordField("Alias Pass", keyaliasPass);

        if (GUILayout.Button("Apply Keystore Settings Now"))
        {
            ApplyKeystoreFromPrefsOrEnv();
            Debug.Log("Keystore applied.");
        }
        
        GUILayout.Label("Build Output Path", EditorStyles.boldLabel);

        using (new GUILayout.HorizontalScope())
        {
            buildPath = EditorGUILayout.TextField(buildPath);

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selected = EditorUtility.OpenFolderPanel(
                    "Select Build Folder",
                    Application.dataPath,
                    ""
                );

                if (!string.IsNullOrEmpty(selected))
                {
                    // 프로젝트 기준 상대경로로 변환
                    if (selected.StartsWith(Application.dataPath))
                    {
                        buildPath = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        buildPath = selected;
                    }
                }
            }
        }

        GUILayout.Space(20);

        if (GUILayout.Button("BUILD (Addressables → Android)", GUILayout.Height(50)))
        {
            BuildAndroid();
        }
    }

    public void BuildAndroid()
    {
        Debug.Log("=== Starting Android Build ===");

        SetupKeystore();
        ApplyKeystoreFromPrefsOrEnv(); // 추가
        ApplyAndroidPlayerSettings();
        
        // Addressables 먼저 빌드
        if (!BuildAddressables())
        {
            return;
        }
        
        string[] scenes = GetBuildScenes();
        
        if (scenes.Length == 0)
        {
            Debug.LogError("No scenes found in Build Settings!");
            return;
        }
        
        Debug.Log($"Building {scenes.Length} scenes...");
        
        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        string outputPath = Path.Combine(
            buildPath,
            $"{PlayerSettings.productName}.apk"
        );

        
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        
        BuildReport report = BuildPipeline.BuildPlayer(options);
        
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build Failed! Errors: {report.summary.totalErrors}");
        }
        else
        {
            Debug.Log($"Build Succeeded! Time: {report.summary.totalTime}");    
            EditorUtility.RevealInFinder(buildPath);
        }
    }
    
    private static void ApplyAndroidPlayerSettings()
    {
        // 스크립팅 백엔드 IL2CPP 강제
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        // ARMv7 해제, ARM64만 사용
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Managed Stripping Level = High
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);
    }
    
    // Build Settings에서 씬 목록 가져오기
    static string[] GetBuildScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }
    
    static void SetupKeystore()
    {
        string keystorePass = Environment.GetEnvironmentVariable("KEYSTORE_PASS");
        string aliasPass = Environment.GetEnvironmentVariable("KEY_ALIAS_PASS");
        
        if (!string.IsNullOrEmpty(keystorePass))
        {
            PlayerSettings.Android.keystorePass = keystorePass;
            Debug.Log("Keystore password set from environment variable.");
        }
        
        if (!string.IsNullOrEmpty(aliasPass))
        {
            PlayerSettings.Android.keyaliasPass = aliasPass;
            Debug.Log("Key alias password set from environment variable.");
        }
    }
    private static string GetCommandLineArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        return string.Empty;
    }
    
    
    // Addressables 빌드
    static bool BuildAddressables()
    {
        Debug.Log("=== Building Addressables ===");

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("Addressables settings not found. Skipping Addressables build.");
            return true;
        }

        // 기존 빌드 정리
        AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);

        // Addressables 빌드
        AddressableAssetSettings.BuildPlayerContent(out var result);

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"Addressables Build Failed: {result.Error}");
            return false;
        }

        Debug.Log($"Addressables Build Succeeded! Duration: {result.Duration}s");
        return true;
    }
    
    void ApplyKeystoreFromPrefsOrEnv()
    {
        // 1) 환경변수 우선 (CI/자동화 대비)
        string ksPath = Environment.GetEnvironmentVariable("KEYSTORE_PATH");
        string ksPass = Environment.GetEnvironmentVariable("KEYSTORE_PASS");
        string alName = Environment.GetEnvironmentVariable("KEY_ALIAS_NAME");
        string alPass = Environment.GetEnvironmentVariable("KEY_ALIAS_PASS");

        // 2) 환경변수 없으면 EditorPrefs 값 사용
        if (string.IsNullOrEmpty(ksPath)) ksPath = keystorePath;
        if (string.IsNullOrEmpty(ksPass)) ksPass = keystorePass;
        if (string.IsNullOrEmpty(alName)) alName = keyaliasName;
        if (string.IsNullOrEmpty(alPass)) alPass = keyaliasPass;

        // 3) 최소 검증
        if (string.IsNullOrEmpty(ksPath) || string.IsNullOrEmpty(alName))
        {
            Debug.LogWarning("Keystore path or alias name is empty. Skipping keystore apply.");
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = ksPath;
        PlayerSettings.Android.keyaliasName = alName;

        // 비밀번호는 빈 값이면 안 넣음(기존 값 유지)
        if (!string.IsNullOrEmpty(ksPass))
            PlayerSettings.Android.keystorePass = ksPass;

        if (!string.IsNullOrEmpty(alPass))
            PlayerSettings.Android.keyaliasPass = alPass;
    }
    
}

