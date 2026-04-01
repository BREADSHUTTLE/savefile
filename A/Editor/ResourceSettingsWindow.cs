using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class ResourceSettingsWindow : EditorWindow
{
    private const string USE_LOCAL_BUNDLE_KEY = "ATOZPOKER_UseLocalBundle";
    private const string BUILD_USE_LOCAL_BUNDLE_KEY = "ATOZPOKER_BuildUseLocalBundle";
    
    // Play Mode Script 인덱스
    private const int PLAY_MODE_USE_ASSET_DATABASE = 0;
    private const int PLAY_MODE_SIMULATE_GROUPS = 1;
    private const int PLAY_MODE_USE_EXISTING_BUILD = 2;
    
    // 에디터 플레이 시 로컬 번들 사용
    public static bool UseLocalBundle
    {
        get => EditorPrefs.GetBool(USE_LOCAL_BUNDLE_KEY, true);
        set
        {
            EditorPrefs.SetBool(USE_LOCAL_BUNDLE_KEY, value);
            SetPlayModeScript(value);
        }
    }
    
    private static void SetPlayModeScript(bool useLocal)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;
        
        int targetIndex = useLocal ? PLAY_MODE_USE_ASSET_DATABASE : PLAY_MODE_USE_EXISTING_BUILD;
        
        if (settings.ActivePlayModeDataBuilderIndex != targetIndex)
        {
            settings.ActivePlayModeDataBuilderIndex = targetIndex;
            EditorUtility.SetDirty(settings);
            
            string modeName = useLocal ? "Use Asset Database" : "Use Existing Build";
            Debug.Log($"[리소스 설정] Play Mode Script 변경: {modeName}");
        }
    }
    
    // 빌드 시 로컬 번들 사용 (S3 다운로드 스킵)
    public static bool BuildUseLocalBundle
    {
        get => EditorPrefs.GetBool(BUILD_USE_LOCAL_BUNDLE_KEY, false);
        set => EditorPrefs.SetBool(BUILD_USE_LOCAL_BUNDLE_KEY, value);
    }
    
    [MenuItem("ATOZPOKER/리소스 설정")]
    public static void ShowWindow()
    {
        GetWindow<ResourceSettingsWindow>("리소스 설정");
    }

    [MenuItem("ATOZPOKER/로컬 번들 사용", true)]
    private static bool ValidateToggleLocalBundle()
    {
        Menu.SetChecked("ATOZPOKER/로컬 번들 사용", UseLocalBundle);
        return true;
    }
    
    [MenuItem("ATOZPOKER/로컬 번들 사용")]
    private static void ToggleLocalBundle()
    {
        UseLocalBundle = !UseLocalBundle;
        Debug.Log($"[리소스 설정] 에디터: {(UseLocalBundle ? "로컬 번들 사용" : "AWS S3에서 다운로드")}");
    }
    
    private void OnGUI()
    {
        GUILayout.Space(10);
        
        // ========== 에디터 플레이 설정 ==========
        EditorGUILayout.LabelField("에디터 리소스 설정", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        EditorGUILayout.BeginVertical("box");
        
        bool currentValue = UseLocalBundle;
        bool newValue = EditorGUILayout.Toggle("로컬 번들 사용", currentValue);
        
        if (newValue != currentValue)
            UseLocalBundle = newValue;
        
        GUILayout.Space(5);
        
        if (UseLocalBundle)
            EditorGUILayout.HelpBox("로컬 번들 사용 모드\n\n에디터에서 플레이 시 로컬에 빌드된 Addressables 번들을 사용합니다.\nS3에서 다운로드하지 않습니다.", MessageType.Info);
        else
            EditorGUILayout.HelpBox("AWS S3 다운로드 모드\n\n에디터에서 플레이 시 S3에서 최신 번들을 확인하고 다운로드합니다.\n실제 빌드된 앱과 동일한 동작을 테스트할 수 있습니다.", MessageType.Warning);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(20);
        
        // ========== 빌드 설정 ==========
        EditorGUILayout.LabelField("빌드 리소스 설정", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        EditorGUILayout.BeginVertical("box");
        
        bool buildCurrentValue = BuildUseLocalBundle;
        bool buildNewValue = EditorGUILayout.Toggle("로컬 번들 빌드", buildCurrentValue);
        
        if (buildNewValue != buildCurrentValue)
            BuildUseLocalBundle = buildNewValue;
        
        GUILayout.Space(5);
        
        if (BuildUseLocalBundle)
            EditorGUILayout.HelpBox("로컬 번들 빌드 모드\n\n• Addressables를 앱에 내장 (StreamingAssets)\n• S3 업로드 안함\n• 앱 실행 시 다운로드 없이 바로 사용\n\n※ 로컬 테스트용 빌드에 적합합니다.", MessageType.Info);
        else
            EditorGUILayout.HelpBox("S3 업로드 빌드 모드\n\n• Addressables를 ServerData에 빌드\n• S3에 업로드\n• 앱 실행 시 S3에서 다운로드\n\n※ 배포용 빌드에 적합합니다.", MessageType.Warning);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(20);
        
        // ========== 현재 상태 ==========
        EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        
        string editorStatus = UseLocalBundle ? "로컬" : "AWS S3";
        string buildStatus = BuildUseLocalBundle ? "로컬 (앱 내장)" : "S3 (업로드 후 다운로드)";
        string playModeStatus = GetCurrentPlayModeScriptName();
        
        EditorGUILayout.LabelField("에디터 플레이:", editorStatus);
        EditorGUILayout.LabelField("앱 빌드:", buildStatus);
        EditorGUILayout.LabelField("Play Mode:", playModeStatus);
        
        EditorGUILayout.EndVertical();
    }
    
    private static string GetCurrentPlayModeScriptName()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return "설정 없음";
        
        switch (settings.ActivePlayModeDataBuilderIndex)
        {
            case PLAY_MODE_USE_ASSET_DATABASE: return "Use Asset Database (로컬)";
            case PLAY_MODE_SIMULATE_GROUPS: return "Simulate Groups";
            case PLAY_MODE_USE_EXISTING_BUILD: return "Use Existing Build (S3)";
            default: return "알 수 없음";
        }
    }
}
