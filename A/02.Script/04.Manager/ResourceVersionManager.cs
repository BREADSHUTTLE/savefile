using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using CAPYBARA.Core;
using CAPYBARA.Definition;

namespace CAPYBARA
{
    public class ResourceVersionManager : MonoBehaviour
    {
        private static ResourceVersionManager _instance;
        public static ResourceVersionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ResourceVersionManager");
                    _instance = go.AddComponent<ResourceVersionManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private const string S3_BASE_URL = "https://atozpoker-bundles.s3.ap-northeast-2.amazonaws.com";
        private const string VERSION_JSON_URL = S3_BASE_URL + "/version.json";
        
        // 빌드 시 설정된 기본 버전
        private const string DEFAULT_VERSION_PLACEHOLDER = "1.0.0";

        public event Action<float, string> OnDownloadProgress;
        public event Action OnDownloadComplete;
        public event Action<string> OnDownloadError;

        public string CurrentResourceVersion { get; private set; }
        private bool _isTransformFuncSet = false;

        [Serializable]
        public class VersionInfo
        {
            public string latest;
            public string android;
            public string ios;
        }

        public void InitializeOnStart()
        {
            OnDownloadProgress = null;
            OnDownloadComplete = null;
            OnDownloadError = null;
        }

        private string GetLocalizedText(LocalizeDescKeys key, string fallback)
        {
            if (StaticData.Wrapper?.localizeddescDict != null && StaticData.Wrapper.localizeddescDict.TryGetValue(key, out var desc))
                return desc.StringToLocal;
                
            return fallback;
        }

        public async UniTask<bool> CheckAndDownloadResources()
        {
            try
            {
                Debug.Log("[ResourceVersionManager] CheckAndDownloadResources 시작");
                
#if USE_LOCAL_BUNDLE
                Debug.Log("로컬 번들 모드 - S3 다운로드 스킵");
                OnDownloadProgress?.Invoke(1f, GetLocalizedText(LocalizeDescKeys.LocalBundleUsed, "로컬 번들 사용"));
                CurrentResourceVersion = "local";

                // Addressables 초기화만 진행
                await InitializeAddressables();
                OnDownloadComplete?.Invoke();
                return true;
#elif UNITY_EDITOR
                if (IsEditorLocalMode())
                {
                    Debug.Log("에디터 로컬 모드 - S3 다운로드 스킵");
                    OnDownloadProgress?.Invoke(1f, GetLocalizedText(LocalizeDescKeys.LocalBundleUsed, "로컬 번들 사용"));
                    CurrentResourceVersion = "local";
                    
                    // Addressables 초기화만 진행
                    await InitializeAddressables();
                    OnDownloadComplete?.Invoke();
                    return true;
                }
#endif
                
                OnDownloadProgress?.Invoke(0f, GetLocalizedText(LocalizeDescKeys.CheckingVersion, "버전 확인 중..."));
                Debug.Log("[ResourceVersionManager] version.json 요청 시작...");
                
                var versionInfo = await GetVersionInfo();
                Debug.Log($"[ResourceVersionManager] version.json 응답: {(versionInfo != null ? "성공" : "실패")}");
                
                if (versionInfo == null)
                {
                    Debug.LogWarning("version.json을 가져올 수 없습니다. 로컬 번들 사용");
                    return true;
                }

#if UNITY_ANDROID
                CurrentResourceVersion = versionInfo.android;
#elif UNITY_IOS
                CurrentResourceVersion = versionInfo.ios;
#else
                CurrentResourceVersion = versionInfo.latest;
#endif

                Debug.Log($"리소스 버전: {CurrentResourceVersion}");
                
                SetupDynamicVersionTransform();

                OnDownloadProgress?.Invoke(0.1f, GetLocalizedText(LocalizeDescKeys.Initializing, "초기화 중..."));
                Debug.Log("[ResourceVersionManager] Addressables 초기화 시작...");
                await InitializeAddressables();
                Debug.Log("[ResourceVersionManager] Addressables 초기화 완료");

                OnDownloadProgress?.Invoke(0.2f, GetLocalizedText(LocalizeDescKeys.CheckingUpdates, "업데이트 확인 중..."));
                Debug.Log("[ResourceVersionManager] 카탈로그 업데이트 확인 시작...");
                var updateSize = await CheckForUpdates();
                Debug.Log($"[ResourceVersionManager] 업데이트 크기: {updateSize}");

                if (updateSize > 0)
                {
                    Debug.Log($"다운로드 필요: {FormatBytes(updateSize)}");
                    await DownloadUpdates();
                }
                else
                {
                    Debug.Log("최신 상태입니다.");
                    OnDownloadProgress?.Invoke(1f, "100%");
                }

                OnDownloadComplete?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"다운로드 실패: {e.Message}");
                OnDownloadError?.Invoke(e.Message);
                return false;
            }
        }

        private async UniTask<VersionInfo> GetVersionInfo()
        {
            try
            {
                using var request = UnityWebRequest.Get(VERSION_JSON_URL);
                request.timeout = 10;
                
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"version.json 로드 실패: {request.error}");
                    return null;
                }

                var json = request.downloadHandler.text;
                Debug.Log($"version.json: {json}");
                
                return JsonUtility.FromJson<VersionInfo>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"version.json 파싱 실패: {e.Message}");
                return null;
            }
        }

        private void SetupDynamicVersionTransform()
        {
            if (_isTransformFuncSet) return;
            
            Addressables.ResourceManager.InternalIdTransformFunc = (location) =>
            {
                string originalId = location.InternalId;
                
                // S3 URL이고 버전이 다른 경우에만 변환
                if (!string.IsNullOrEmpty(CurrentResourceVersion) && 
                    originalId.Contains(S3_BASE_URL) &&
                    originalId.Contains($"/{DEFAULT_VERSION_PLACEHOLDER}/"))
                {
                    string newId = originalId.Replace(
                        $"/{DEFAULT_VERSION_PLACEHOLDER}/", 
                        $"/{CurrentResourceVersion}/");
                    
                    if (originalId != newId)
                        Debug.Log($"[ResourceVersionManager] URL 변환: {DEFAULT_VERSION_PLACEHOLDER} → {CurrentResourceVersion}");
                    return newId;
                }
                
                return originalId;
            };
            
            _isTransformFuncSet = true;
            Debug.Log($"[ResourceVersionManager] 동적 버전 변환 설정 완료 (버전: {CurrentResourceVersion})");
        }
        
        private async UniTask InitializeAddressables()
        {
            Debug.Log("[ResourceVersionManager] Addressables.InitializeAsync() 호출...");
            var initHandle = Addressables.InitializeAsync();
            
            // 타임아웃 30초
            using var cts = new CancellationTokenSource(30000);
            
            try
            {
                await initHandle.ToUniTask().AttachExternalCancellation(cts.Token);
                Debug.Log("Addressables 초기화 완료");
            }
            catch (OperationCanceledException)
            {
                Debug.LogError("[ResourceVersionManager] Addressables 초기화 타임아웃 (30초)!");
                throw new Exception("Addressables initialization timeout");
            }
        }

        private async UniTask<long> CheckForUpdates()
        {
            try
            {
                var checkHandle = Addressables.CheckForCatalogUpdates(false);
                var catalogsToUpdate = await checkHandle.ToUniTask();
                
                if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
                {
                    Debug.Log($"카탈로그 업데이트 발견: {catalogsToUpdate.Count}개");
                    
                    var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                    await updateHandle.ToUniTask();
                }
                
                Addressables.Release(checkHandle);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResourceVersionManager] 카탈로그 업데이트 확인 실패 : {e.Message}");
            }

            // 다운로드 크기 확인 default 라벨이 없으면 0 반환
            try
            {
                var sizeHandle = Addressables.GetDownloadSizeAsync("default");
                var size = await sizeHandle.ToUniTask();
                Addressables.Release(sizeHandle);
                return size;
            }
            catch (InvalidKeyException)
            {
                Debug.Log("[ResourceVersionManager] 라벨 없음 - 다운로드 필요 없음");
                return 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ResourceVersionManager] 다운로드 크기 확인 실패: {e.Message}");
                return 0;
            }
        }

        private async UniTask DownloadUpdates()
        {
            var downloadHandle = Addressables.DownloadDependenciesAsync("default", false);
            
            while (!downloadHandle.IsDone)
            {
                var status = downloadHandle.GetDownloadStatus();
                float progress = 0.2f + (status.Percent * 0.8f);
                string percentText = $"{(int)(progress * 100)}%";
                
                OnDownloadProgress?.Invoke(progress, percentText);
                
                await UniTask.Yield();
            }

            if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("다운로드 완료!");
                OnDownloadProgress?.Invoke(1f, "100%");
            }
            else
            {
                throw new Exception(GetLocalizedText(LocalizeDescKeys.DownloadFailed, "다운로드 실패"));
            }
            
            Addressables.Release(downloadHandle);
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }
        
#if UNITY_EDITOR
        private bool IsEditorLocalMode()
        {
            return UnityEditor.EditorPrefs.GetBool("ATOZPOKER_UseLocalBundle", true);
        }
#endif
    }
}
