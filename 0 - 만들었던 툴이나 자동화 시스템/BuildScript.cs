using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

public class BuildScript
{
    // S3 설정
    private const string S3_BUCKET = "atozpoker-bundles";
    private const string S3_REGION = "ap-northeast-2";
    
    public enum ResourceBuildMode
    {
        Skip,           // 리소스 빌드 스킵 (앱만 빌드)
        S3_New,         // S3 업로드 (새 버전, 중복 시 실패)
        S3_Overwrite,   // S3 강제 업로드 (덮어쓰기)
        Local           // 로컬 빌드 (앱에 내장)
    }
    
    // 스토어 타입
    public enum StoreType
    {
        Google,         // 구글 플레이 스토어
        OneStore        // 원스토어
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
    
    private static bool HasCommandLineArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        return args.Contains(name);
    }
    
    private static StoreType GetStoreType()
    {
        string store = GetCommandLineArg("-storeType");
        
        switch (store.ToLower())
        {
            case "onestore":
                return StoreType.OneStore;
            case "google":
            default:
                return StoreType.Google;
        }
    }
    
    // 스토어 타입에 따라 AndroidManifest 교체
    private static void SwitchManifestForStore()
    {
        var storeType = GetStoreType();
        string manifestDir = Path.Combine(Application.dataPath, "Plugins/Android");
        string targetManifest = Path.Combine(manifestDir, "AndroidManifest.xml");
        
        string sourceManifest = storeType == StoreType.OneStore
            ? Path.Combine(manifestDir, "AndroidManifest_OneStore.xml")
            : Path.Combine(manifestDir, "AndroidManifest_Google.xml");
        
        if (File.Exists(sourceManifest))
        {
            File.Copy(sourceManifest, targetManifest, true);
            Debug.Log($"[BuildScript] AndroidManifest 교체 완료: {storeType}");
        }
        else
        {
            Debug.LogWarning($"[BuildScript] Manifest 템플릿을 찾을 수 없음: {sourceManifest}");
        }
    }
    
    private static ResourceBuildMode GetResourceBuildMode()
    {
        string mode = GetCommandLineArg("-resourceMode");
        
        switch (mode.ToLower())
        {
            case "s3_new":
                return ResourceBuildMode.S3_New;
            case "s3_overwrite":
                return ResourceBuildMode.S3_Overwrite;
            case "local":
                return ResourceBuildMode.Local;
            case "skip":
                return ResourceBuildMode.Skip;
            case "":
                if (HasCommandLineArg("-skipResourceBuild"))
                    return ResourceBuildMode.Skip;
                if (HasCommandLineArg("-useLocalBundle") || ResourceSettingsWindow.BuildUseLocalBundle)
                    return ResourceBuildMode.Local;
                if (HasCommandLineArg("-forceUpload"))
                    return ResourceBuildMode.S3_Overwrite;
                if (!string.IsNullOrEmpty(GetCommandLineArg("-resourceVersion")))
                    return ResourceBuildMode.S3_New;
                return ResourceBuildMode.Skip;
            default:
                Debug.LogWarning($"[BuildScript] 알 수 없는 resourceMode: {mode}, Skip으로 처리");
                return ResourceBuildMode.Skip;
        }
    }
    
    private static bool IsLocalBundleMode()
    {
        return GetResourceBuildMode() == ResourceBuildMode.Local;
    }
    
    private static string GetResourceVersion()
    {
        string version = GetCommandLineArg("-resourceVersion");
        if (string.IsNullOrEmpty(version))
        {
            Debug.LogWarning("[BuildScript] -resourceVersion not specified, using 'dev'");
            return "dev";
        }
        return version;
    }
    
    private static bool CheckS3VersionExists(string version, string platform)
    {
        string s3Path = $"s3://{S3_BUCKET}/{version}/{platform}/";
        
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "aws",
            Arguments = $"s3 ls {s3Path}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        try
        {
            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                return !string.IsNullOrEmpty(output.Trim());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuildScript] AWS CLI 실행 실패: {e.Message}");
            return false;
        }
    }
    
    private static bool UploadToS3(string localPath, string version, string platform)
    {
        string s3Path = $"s3://{S3_BUCKET}/{version}/{platform}/";
        
        Debug.Log($"[BuildScript] S3 업로드 시작: {localPath} → {s3Path}");
        
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "aws",
            Arguments = $"s3 sync \"{localPath}\" {s3Path}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        try
        {
            using (Process process = Process.Start(psi))
            {
                // 비동기로 출력 읽기 (데드락 방지)
                System.Text.StringBuilder outputBuilder = new System.Text.StringBuilder();
                System.Text.StringBuilder errorBuilder = new System.Text.StringBuilder();
                
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };
                
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                // 타임아웃 10분 (대용량 업로드 대비)
                bool exited = process.WaitForExit(600000);
                
                if (!exited)
                {
                    process.Kill();
                    Debug.LogError("[BuildScript] S3 업로드 타임아웃 (10분 초과)");
                    return false;
                }
                
                if (process.ExitCode != 0)
                {
                    Debug.LogError($"[BuildScript] S3 업로드 실패: {errorBuilder}");
                    return false;
                }
                
                Debug.Log($"[BuildScript] S3 업로드 완료!\n{outputBuilder}");
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuildScript] AWS CLI 실행 실패: {e.Message}");
            return false;
        }
    }
    
    private static bool UpdateVersionJson(string version, string platform)
    {
        string tempFile = Path.Combine(Application.dataPath, "../Temp/version.json");
        string s3Path = $"s3://{S3_BUCKET}/version.json";
        
        string existingJson = "{}";
        ProcessStartInfo downloadPsi = new ProcessStartInfo
        {
            FileName = "aws",
            Arguments = $"s3 cp {s3Path} \"{tempFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        try
        {
            using (Process process = Process.Start(downloadPsi))
            {
                process.WaitForExit();
                if (process.ExitCode == 0 && File.Exists(tempFile))
                    existingJson = File.ReadAllText(tempFile);
            }
        }
        catch { }
        
        string platformKey = platform.ToLower();
        string newJson;
        
        if (existingJson.Contains(platformKey))
        {
            int startIdx = existingJson.IndexOf($"\"{platformKey}\"");
            if (startIdx >= 0)
            {
                int colonIdx = existingJson.IndexOf(":", startIdx);
                int quoteStart = existingJson.IndexOf("\"", colonIdx);
                int quoteEnd = existingJson.IndexOf("\"", quoteStart + 1);
                newJson = existingJson.Substring(0, quoteStart + 1) + version + existingJson.Substring(quoteEnd);
            }
            else
            {
                newJson = $"{{\"latest\":\"{version}\",\"{platformKey}\":\"{version}\"}}";
            }
        }
        else
        {
            newJson = $"{{\"latest\":\"{version}\",\"{platformKey}\":\"{version}\"}}";
        }
        
        File.WriteAllText(tempFile, newJson);
        
        ProcessStartInfo uploadPsi = new ProcessStartInfo
        {
            FileName = "aws",
            Arguments = $"s3 cp \"{tempFile}\" {s3Path}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        try
        {
            using (Process process = Process.Start(uploadPsi))
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Debug.Log($"[BuildScript] version.json 업데이트 완료: {newJson}");
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuildScript] version.json 업데이트 실패: {e.Message}");
        }
        
        return false;
    }
    
    private static void ApplyCustomSymbols(NamedBuildTarget namedBuildTarget)
    {
        string customSymbols = GetCommandLineArg("-customSymbols");
        
        List<string> symbolsToAdd = new List<string>();
        
        if (!string.IsNullOrEmpty(customSymbols))
            symbolsToAdd.AddRange(customSymbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        
        // 로컬 번들 빌드 모드 체크
        if (ResourceSettingsWindow.BuildUseLocalBundle || HasCommandLineArg("-useLocalBundle"))
        {
            if (!symbolsToAdd.Contains("USE_LOCAL_BUNDLE"))
            {
                symbolsToAdd.Add("USE_LOCAL_BUNDLE");
                Debug.Log("[BuildScript] 로컬 번들 빌드 모드 활성화 (USE_LOCAL_BUNDLE 심볼 추가)");
            }
        }

        if (HasCommandLineArg("-skipIdentityVerification"))
        {
            if (!symbolsToAdd.Contains("SKIP_IDENTITY_VERIFICATION"))
            {
                symbolsToAdd.Add("SKIP_IDENTITY_VERIFICATION");
                Debug.Log("[BuildScript] 본인인증 스킵 모드 활성화 (SKIP_IDENTITY_VERIFICATION 심볼 추가)");
            }
        }
        
        if (symbolsToAdd.Count == 0)
        {
            Debug.Log("[BuildScript] 추가 심볼 없음");
            return;
        }
        
        string existingSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
        string[] currentSymbols = existingSymbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        
        var mergedSymbols = currentSymbols.Union(symbolsToAdd).Distinct().ToList();
        string finalSymbols = string.Join(";", mergedSymbols);
        
        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, finalSymbols);
        
        Debug.Log($"[BuildScript] 심볼 적용됨: {finalSymbols}");
        
        if (symbolsToAdd.Contains("IAP_TEST"))
            Debug.LogWarning("<color=yellow>[BuildScript] IAP_TEST 모드 활성화! 프로덕션 빌드가 아닌지 확인하세요!</color>");
        
        if (symbolsToAdd.Contains("USE_LOCAL_BUNDLE"))
            Debug.LogWarning("<color=cyan>[BuildScript] USE_LOCAL_BUNDLE 모드 활성화! 앱이 S3 대신 내장 번들을 사용합니다.</color>");

        if (symbolsToAdd.Contains("SKIP_IDENTITY_VERIFICATION"))
            Debug.LogWarning("<color=yellow>[BuildScript] SKIP_IDENTITY_VERIFICATION 모드 활성화! 본인인증이 생략됩니다.</color>");
    }
    
    // APK 빌드 시 Addressables 자동 빌드 비활성화
    static void DisableAddressablesAutoBuild()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            Debug.Log("[BuildScript] Addressables 자동 빌드 비활성화됨");
        }
    }
    
    // StreamingAssets/aa 폴더 삭제 (이전 Local 빌드 번들 정리)
    static void CleanStreamingAssetsBundle()
    {
        string streamingAssetsAA = Path.Combine(Application.streamingAssetsPath, "aa");
        if (Directory.Exists(streamingAssetsAA))
        {
            Debug.Log($"[BuildScript] 이전 로컬 번들 삭제: {streamingAssetsAA}");
            Directory.Delete(streamingAssetsAA, true);
        }
    }
    
    // Addressables 빌드 메인
    static bool BuildAddressables()
    {
        var buildMode = GetResourceBuildMode();
        
        switch (buildMode)
        {
            case ResourceBuildMode.Skip:
                Debug.Log("[BuildScript] 리소스 빌드 스킵 (앱만 빌드, S3에서 다운로드)");
                CleanStreamingAssetsBundle(); // 이전 Local 빌드 번들 정리
                DisableAddressablesAutoBuild();
                return true;
                
            case ResourceBuildMode.S3_New:
                Debug.Log("[BuildScript] 리소스 빌드 + S3 업로드 (새 버전)");
                return BuildAddressablesWithMode(false);
                
            case ResourceBuildMode.S3_Overwrite:
                Debug.Log("[BuildScript] 리소스 빌드 + S3 강제 업로드 (덮어쓰기)");
                return BuildAddressablesWithMode(true);
                
            case ResourceBuildMode.Local:
                Debug.Log("[BuildScript] 로컬 번들 빌드 (앱 내장)");
                return BuildAddressablesLocal();
                
            default:
                return true;
        }
    }
    
    // S3 업로드 모드로 Addressables 빌드
    static bool BuildAddressablesWithMode(bool forceUpload)
    {
        Debug.Log("=== Building Addressables (S3 Mode) ===");

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("Addressables settings not found. Skipping Addressables build.");
            return true;
        }
        
        // 이전 Local 빌드 번들 삭제 (S3 모드에서는 앱에 포함되면 안 됨)
        CleanStreamingAssetsBundle();
        
        string resourceVersion = GetResourceVersion();
        string platform = EditorUserBuildSettings.activeBuildTarget.ToString();
        
        Debug.Log($"[BuildScript] 리소스 버전: {resourceVersion}, 플랫폼: {platform}, 강제업로드: {forceUpload}");
        
        if (!forceUpload && resourceVersion != "dev")
        {
            if (CheckS3VersionExists(resourceVersion, platform))
            {
                Debug.LogError($"[BuildScript] 버전 {resourceVersion}/{platform}이(가) 이미 S3에 존재합니다! 강제 업로드 옵션을 사용하세요.");
                return false;
            }
        }

        // APK 빌드 시 자동 빌드 비활성화 (APK 빌드 완료까지 유지)
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

        AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
        AddressableAssetSettings.BuildPlayerContent(out var result);

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"Addressables Build Failed: {result.Error}");
            return false;
        }

        Debug.Log($"Addressables Build Succeeded! Duration: {result.Duration}s");
        
        // S3 업로드 - ServerData 폴더
        string buildPath = Path.Combine(Application.dataPath, "../ServerData", platform);
        if (Directory.Exists(buildPath))
        {
            if (!UploadToS3(buildPath, resourceVersion, platform))
            {
                Debug.LogError("[BuildScript] S3 업로드 실패!");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[BuildScript] 빌드 경로를 찾을 수 없음: {buildPath}");
        }
        
        // S3 업로드 - Library 카탈로그 폴더
        string catalogPath = Path.Combine(Application.dataPath, "../Library/com.unity.addressables/aa", platform);
        if (Directory.Exists(catalogPath))
        {
            Debug.Log($"[BuildScript] 카탈로그 업로드: {catalogPath}");
            if (!UploadToS3(catalogPath, resourceVersion, platform))
            {
                Debug.LogError("[BuildScript] 카탈로그 S3 업로드 실패!");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[BuildScript] 카탈로그 경로를 찾을 수 없음: {catalogPath}");
        }
        
        UpdateVersionJson(resourceVersion, platform);
        
        return true;
    }
    
    // 로컬 번들 빌드 (앱에 내장)
    static bool BuildAddressablesLocal()
    {
        Debug.Log("=== Building Addressables (Local Mode) ===");

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("Addressables settings not found. Skipping Addressables build.");
            return true;
        }
        
        string platform = EditorUserBuildSettings.activeBuildTarget.ToString();
        
        // 이전 로컬 번들 삭제 (누적 방지)
        CleanStreamingAssetsBundle();
        
        // 현재 프로파일의 Remote 경로를 StreamingAssets로 변경
        var profileSettings = settings.profileSettings;
        var activeProfileId = settings.activeProfileId;
        
        string originalBuildPath = profileSettings.GetValueByName(activeProfileId, "Remote.BuildPath");
        string originalLoadPath = profileSettings.GetValueByName(activeProfileId, "Remote.LoadPath");
        
        // 로컬 경로로 변경 (StreamingAssets 사용)
        profileSettings.SetValue(activeProfileId, "Remote.BuildPath", "[UnityEngine.Application.streamingAssetsPath]/aa/[BuildTarget]");
        profileSettings.SetValue(activeProfileId, "Remote.LoadPath", "{UnityEngine.Application.streamingAssetsPath}/aa/[BuildTarget]");
        
        Debug.Log($"[BuildScript] Remote.BuildPath 변경: {originalBuildPath} → StreamingAssets");
        Debug.Log($"[BuildScript] Remote.LoadPath 변경: {originalLoadPath} → StreamingAssets");

        // APK 빌드 시 자동 빌드 비활성화 (APK 빌드 완료까지 유지)
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        
        AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
        AddressableAssetSettings.BuildPlayerContent(out var result);
        
        // 경로만 복원 (BuildAddressablesWithPlayerBuild는 APK 빌드 완료까지 유지)
        profileSettings.SetValue(activeProfileId, "Remote.BuildPath", originalBuildPath);
        profileSettings.SetValue(activeProfileId, "Remote.LoadPath", originalLoadPath);
        Debug.Log("[BuildScript] Addressables 경로 원래대로 복원 (자동 빌드는 비활성화 유지)");

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"Addressables Build Failed: {result.Error}");
            return false;
        }

        Debug.Log($"Addressables Build Succeeded (Local)! Duration: {result.Duration}s");
        Debug.Log("[BuildScript] 로컬 번들 모드 - S3 업로드 스킵");
        
        return true;
    }
    
    // Addressables만 빌드하고 S3 업로드 (앱 빌드 없이)
    public static void BuildAddressablesOnly()
    {
        Debug.Log("=== Building Addressables Only ===");
        
        if (!BuildAddressables())
        {
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log("[BuildScript] Addressables 빌드 완료!");
        EditorApplication.Exit(0);
    }
    
    // iOS 빌드
    public static void BuildiOS()
    {
        Debug.Log("=== Starting iOS Build ===");
        
        ApplyCustomSymbols(NamedBuildTarget.iOS);
        
        // Addressables 먼저 빌드
        if (!BuildAddressables())
        {
            EditorApplication.Exit(1);
            return;
        }
        
        string[] scenes = GetBuildScenes();
        
        if (scenes.Length == 0)
        {
            Debug.LogError("No scenes found in Build Settings!");
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log($"Building {scenes.Length} scenes...");
        
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };
        
        BuildReport report = BuildPipeline.BuildPlayer(options);
        
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build Failed! Errors: {report.summary.totalErrors}");
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log($"Build Succeeded! Time: {report.summary.totalTime}");
        EditorApplication.Exit(0);
    }
    
    // Keystore 설정 (환경변수에서 비밀번호 가져오기)
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
    
    // Android 빌드 (APK)
    public static void BuildAndroid()
    {
        Debug.Log("=== Starting Android Build (APK) ===");

        SwitchManifestForStore();
        Debug.Log($"[BuildScript] 스토어 타입: {GetStoreType()}");
        
        ApplyAndroidPlayerSettings();
        
        // Keystore 비밀번호 설정 (환경변수에서)
        SetupKeystore();
        ApplyCustomSymbols(NamedBuildTarget.Android);
        
        // APK 빌드 모드
        EditorUserBuildSettings.buildAppBundle = false;
        
        // Addressables 먼저 빌드
        if (!BuildAddressables())
        {
            EditorApplication.Exit(1);
            return;
        }
        
        string[] scenes = GetBuildScenes();
        
        if (scenes.Length == 0)
        {
            Debug.LogError("No scenes found in Build Settings!");
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log($"Building {scenes.Length} scenes...");
        
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/Android/ATOZPOKER.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        
        BuildReport report = BuildPipeline.BuildPlayer(options);
        
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build Failed! Errors: {report.summary.totalErrors}");
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log($"Build Succeeded! Time: {report.summary.totalTime}");
        EditorApplication.Exit(0);
    }
    
    // Android 빌드 (AAB - Google Play 업로드용)
    public static void BuildAndroidAAB()
    {
        Debug.Log("=== Starting Android Build (AAB) ===");

        SwitchManifestForStore();
        Debug.Log($"[BuildScript] 스토어 타입: {GetStoreType()}");
        
        ApplyAndroidPlayerSettings();
        // Keystore 비밀번호 설정 (환경변수에서)
        SetupKeystore();
        ApplyCustomSymbols(NamedBuildTarget.Android);
        
        // AAB 빌드 모드 활성화
        EditorUserBuildSettings.buildAppBundle = true;
        Debug.Log("[BuildScript] AAB 빌드 모드 활성화");
        
        // Addressables 먼저 빌드
        if (!BuildAddressables())
        {
            EditorApplication.Exit(1);
            return;
        }
        
        string[] scenes = GetBuildScenes();
        
        if (scenes.Length == 0)
        {
            Debug.LogError("No scenes found in Build Settings!");
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log($"Building {scenes.Length} scenes...");
        
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/Android/ATOZPOKER.aab",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        
        BuildReport report = BuildPipeline.BuildPlayer(options);
        
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build Failed! Errors: {report.summary.totalErrors}");
            EditorApplication.Exit(1);
            return;
        }
        
        Debug.Log($"AAB Build Succeeded! Time: {report.summary.totalTime}");
        EditorApplication.Exit(0);
    }
    
    //android 빌드 환경 세팅
    private static void ApplyAndroidPlayerSettings()
    {
        // 스크립팅 백엔드 IL2CPP 강제
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        // ARMv7 해제, ARM64만 사용
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // Managed Stripping Level = High
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);
        
        PlayerSettings.Android.optimizedFramePacing = false;
    }
    
    static string[] GetBuildScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }
}

