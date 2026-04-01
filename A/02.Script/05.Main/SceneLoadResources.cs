using BlackTree.Bundles;
using CAPYBARA;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CAPYBARA.lobby;
using CAPYBARA.Model;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Firebase;
using Firebase.Crashlytics;

namespace CAPYBARA
{
    [Serializable]
    public class LoginResult
    {
        public string firebaseUid;
        public string idToken;
        public string accountId;
        public SignInfo signInfo;
    }

    [Serializable]
    public class SignInfo
    {
        public string id;
        public string password;
        public string provider;
    }

    public class SceneLoadResources : MonoBehaviour
    {
        public static Stack<IBackButtonSender> _backButtonHandlers = new Stack<IBackButtonSender>();
        public static Action<string, string, Action, Action> openPopup;

        bool _isPortraitView = false;
        private UniWebView webView;
        GameObject webViewObject;
        public RectTransform toolbarRectTransform;
        public Canvas canvas;
        public GameObject webViewBack;


        AsyncOperation asyncOperation;
        [SerializeField] EventSystem currentEventSystem;

        public SocialLoginController socialloginController;


        [Space(10)] [Header("스플래시 설정")] [SerializeField]
        private float holdSecondsInSplash = 1f;

        [SerializeField] private float fadeTimeInSplash = 0.7f;

        private CancellationTokenSource _cts;
        bool isLoginComplete = false;
        bool isWebviewClosed = false;
        bool isUserSet = false;
        private float minimumLoadingTime = 2.2f;
        float timer;

        private UniTask _loadResourceTask;
        private UniTask _loadUserAccountTask;
        public static Action callbackAfterNewLogin;

        [SerializeField] private bool isEditorLogin = false;
   

        private bool _firebaseInitialized = false;
        private static bool _errorHandlerRegistered = false;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            // Firebase Crashlytics 초기화
            InitFirebaseCrashlytics();

            // 에러 핸들러 등록 (폰에서만, 한 번만)
            if (!Application.isEditor && !_errorHandlerRegistered)
            {
                Application.logMessageReceived += OnLogMessageReceived;
                _errorHandlerRegistered = true;
                Debug.Log("[ErrorHandler] 에러 핸들러 등록 완료");
            }
        }

        private void OnDestroy()
        {
            openPopup = null;
            OnProgress = null;
            callbackAfterNewLogin = null;
            _backButtonHandlers.Clear();
        }
        
        // 에러 중복 방지용 (에러키 - 마지막 전송 시간)
        private static Dictionary<string, float> _lastErrorSentTime = new Dictionary<string, float>();
        private const float ERROR_COOLDOWN_SECONDS = 60f; // 동일 에러 쿨다운 (60초)

        // 에러 발생 시 Slack 알림
        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // Exception만 알림
            if (type == LogType.Exception)
            {
                // 무시할 에러들 (클라이언트/네트워크/연결 관련)
                if (ShouldIgnoreError(condition, stackTrace))
                    return;

                // 중복 에러 방지 동일 에러는 60초에 1번만
                var errorKey = GetErrorKey(condition, stackTrace);
                if (IsErrorOnCooldown(errorKey))
                    return;

                _lastErrorSentTime[errorKey] = Time.realtimeSinceStartup;
                SendErrorToSlack(condition, stackTrace, type.ToString()).Forget();
            }
        }

        // 에러 식별 키 생성 (에러 종류 + 발생 위치)
        private string GetErrorKey(string condition, string stackTrace)
        {
            // 에러 메시지에서 핵심 부분 추출 (예 "NullReferenceException")
            var errorType = condition.Split(':')[0].Trim();

            // 스택 트레이스에서 첫 줄 (발생 위치)
            var location = "";
            if (!string.IsNullOrEmpty(stackTrace))
            {
                var lines = stackTrace.Split('\n');
                if (lines.Length > 0)
                    location = lines[0].Trim();
            }

            return $"{errorType}|{location}";
        }

        // 쿨다운 중인지 확인
        private bool IsErrorOnCooldown(string errorKey)
        {
            if (_lastErrorSentTime.TryGetValue(errorKey, out var lastTime))
            {
                return (Time.realtimeSinceStartup - lastTime) < ERROR_COOLDOWN_SECONDS;
            }

            return false;
        }

        // 무시할 에러인지 확인
        private bool ShouldIgnoreError(string error, string stackTrace = "")
        {
            // Slack/네트워크 관련 (무한루프 방지)
            if (error.Contains("Slack") || error.Contains("WebRequest"))
                return true;

            // 클라이언트/네트워크 에러는 무시 (정상적인 앱 종료/네트워크 끊김)
            if (error.Contains("CancellationToken") || error.Contains("ObjectDisposed") || error.Contains("OperationCanceled") ||
                error.Contains("SocketException") || error.Contains("Connection reset") || error.Contains("IOException") || error.Contains("Unable to read data") ||
                error.Contains("transport connection"))
                return true;

            // 연결 관련 에러는 무시 (네트워크 끊김 시 정상적인 재연결 시도)
            if (stackTrace.Contains("ConnectionManager") || stackTrace.Contains("ProtoConnection") || stackTrace.Contains("ProtoBootStrap"))
                return true;

            return false;
        }

        // Slack으로 에러 알림
        private static async UniTaskVoid SendErrorToSlack(string error, string stackTrace, string errorType)
        {
            try
            {
                var webhook = "https://hooks.slack.com/";

                // 빌드 정보
                var version = Application.version;
                var platform = Application.platform.ToString();

                // 스택 트레이스에서 CAPYBARA 네임스페이스 호출만 추출 (최대 5줄)
                var traceLines = "";
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    var lines = stackTrace.Split('\n');
                    var count = 0;
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;
                        if (count >= 5) break;
                        traceLines += (count > 0 ? "\\n" : "") + $"`{count + 1}` {EscapeJson(trimmed)}";
                        count++;
                    }
                }

                // 유저 정보 수집
                var userInfo = GetUserContextInfo();

                var safeError = EscapeJson(error);
                if (safeError.Length > 300) safeError = safeError.Substring(0, 300) + "...";

                var message = $":octagonal_sign: *앱 에러 발생*\\n\\n"
                              + $"*ATOZ POKER*\\n"
                              + $"버전: `{version}` ({platform})\\n"
                              + $"타입: `{errorType}`\\n\\n"
                              + $"*에러:*\\n{safeError}\\n\\n"
                              + $"*콜스택:*\\n{traceLines}\\n\\n"
                              + $"*유저 정보:*\\n{userInfo}";

                var payload = $"{{\"text\": \"{message}\"}}";

                using var request = new UnityWebRequest(webhook, "POST");
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
                request.SetRequestHeader("Content-Type", "application/json");
                await request.SendWebRequest();
            }
            catch
            {
                // 에러 알림 실패는 무시
            }
        }

        private static string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ").Replace("\r", "");
        }

        private static string GetUserContextInfo()
        {
            try
            {
                var parts = new System.Collections.Generic.List<string>();

                // 유저 기본 정보
                var userDb = CPPlayer.UserInfo.userDatabase;
                if (userDb != null && userDb.User != null)
                {
                    if (!string.IsNullOrEmpty(userDb.User.Nick))
                        parts.Add($"닉네임: `{EscapeJson(userDb.User.Nick)}`");
                    parts.Add($"골드: `{userDb.User.Gold:N0}`");
                }

                // 현재 게임 상태
                if (CPPlayer.SPoker.currentTableId > 0)
                    parts.Add($"세븐포커 테이블: `{CPPlayer.SPoker.currentTableId}`");
                if (CPPlayer.Holdem.currentTableId > 0)
                    parts.Add($"홀덤 테이블: `{CPPlayer.Holdem.currentTableId}`");
                if (CPPlayer.Badugi.currentTableId > 0)
                    parts.Add($"바둑이 테이블: `{CPPlayer.Badugi.currentTableId}`");

                return parts.Count > 0 ? string.Join("\\n", parts) : "정보 없음";
            }
            catch
            {
                return "정보 수집 실패";
            }
        }

        void InitFirebaseCrashlytics()
        {
            Debug.Log("[Firebase] 초기화 시작...");

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                var dependencyStatus = task.Result;
                Debug.Log($"[Firebase] 상태: {dependencyStatus}");

                if (dependencyStatus == DependencyStatus.Available)
                {
                    Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                    _firebaseInitialized = true;
                    Debug.Log("[Firebase] Crashlytics 초기화 완료!");
                }
                else
                {
                    Debug.LogError($"[Firebase] 초기화 실패: {dependencyStatus}");
                }
            });
        }

        void Start()
        {
            BackButtonManager.Instance.Disable();
            socialloginController.loadingScreenPopup.SetActive(false);
            _cts = new CancellationTokenSource();
            Application.targetFrameRate = 120;

            ResourceVersionManager.Instance.InitializeOnStart();
            socialloginController.Init();

            LoginData.Cloud = null;
            CPPlayer.Cloud = null;
            
            isLoginComplete = false;
            isWebviewClosed = false;
            isUserSet = false;

            callbackAfterNewLogin = null;
            callbackAfterNewLogin += () => { ProcessAfterUserLogin().Forget(); };

            //임시로 서버 접속 ui 생성(테스트 위함,.. 출시 후엔 바로 connnectlobby로 이어짐)
            socialloginController.ConnectBtn.onClick.AddListener(() => { ConnectLobby().Forget(); });

    
            _loadResourceTask = LoadResources().Preserve();
            socialloginController.IPPortWindowOpen();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePopup();
            }
        }




        #region LoadMainScene

        async UniTask ConnectLobby()
        {
            socialloginController.ConnectBtn.interactable = false;
            string ip = socialloginController.IPInputField.text;
            int port = int.Parse(socialloginController.PortInputField.text);
            
            IPPortData.IPPortInfo info = new IPPortData.IPPortInfo() { ip = ip, port = port };
            CPPlayer.IpPortData.ipportinfos.LobbyInfo = info;

            int maxRetry = 5;
            for (int attempt = 1; attempt <= maxRetry; attempt++)
            {
                try
                {
                    socialloginController.errorTxt.text = attempt > 1 ? $"로비 연결 재시도 중... ({attempt}/{maxRetry})" : "";
                    
                    CPPlayer.Server.lobbyConnection?.Dispose();
                    CPPlayer.Server.lobbyConnection = null;
                    Services.Lobby = null;
                    Services.LobbyDispatcher = null;

                    await ProtoBootStrap.InitLobby(ip, port);
                    ConnectionManager.Instance.LobbyDispatcherInit();
                    ConnectionManager.Instance.Reinitialize();
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"lobby connection failed [{ip}:{port}] (시도 {attempt}/{maxRetry}) {e.GetType().Name}: {e.Message}");

                    if (attempt >= maxRetry)
                    {
                        socialloginController.errorTxt.text = $"lobby connection failed [{ip}:{port}] : {e.Message}";
                        CPPlayer.Server.lobbyConnection?.Dispose();
                        CPPlayer.Server.lobbyConnection = null;
                        Services.Lobby = null;
                        Services.LobbyDispatcher = null;
                        socialloginController.ConnectBtn.interactable = true;
                        return;
                    }

                    await UniTask.Delay(1000 * attempt);
                }
            }

            var ipInfo = CPPlayer.IpPortData.ipportinfos.infos.Find(o => o.ip == ip && o.port == port);
            if (ipInfo == null)
            {
                CPPlayer.IpPortData.ipportinfos.infos.Add(info);
                LocalSaveLoader.SaveIPPortData();
            }
            else
            {
                CPPlayer.IpPortData.ipportinfos.infos.Remove(ipInfo);
                CPPlayer.IpPortData.ipportinfos.infos.Add(info);
                LocalSaveLoader.SaveIPPortData();
            }

            socialloginController.ipPortWindow.SetActive(false);
            LoadMainScene().Forget();
        }

        async UniTask LoadMainScene()
        {
            //splash load
            await socialloginController.FadeInHoldOutAsync(socialloginController.splashWindow, holdSecondsInSplash, fadeTimeInSplash);

            //loadingScreen
            await StaticData.Load();
            socialloginController.loadingLoginScreen.gameObject.SetActive(true);
            if (isLoadResourcesComplete)
            {
                //로딩바 보여주시 위해 true로 해놈 만일 로드 다 되있다면(isLoadResourcesComplete==true) 바로 loadingscreen 스킵해도 됨.
                socialloginController.OpenLoadingPopup();
                await _loadResourceTask;
                socialloginController.loadingScreen.gameObject.SetActive(false);
            }
            else
            {
                socialloginController.OpenLoadingPopup();
                await _loadResourceTask;
                socialloginController.loadingScreen.gameObject.SetActive(false);
            }

            //login
            bool isReLogin = LocalSaveLoader.ExistsLoginAutoToken();
            LoginData.Cloud = LocalSaveLoader.LoadLoginCloudData();
            if (isReLogin)
            {
                LoginData.Cloud.loginValue.isFirstLogin = false;
            }
            else
            {
                LoginData.Cloud.loginValue.isFirstLogin = true;
            }

#if !UNITY_EDITOR
            isEditorLogin = false;
#endif
            //바로 기존 계정 로그인후 게임씬 진입
            if (isEditorLogin)
            {
#if UNITY_EDITOR
                await socialloginController.EditorLoginProccess();
#endif
            }
            else
            {
                LoginData.Cloud.loginValue.uidList = LocalSaveLoader.LoadAutoLoginUserIdList();
                
                //이미 로그인 전적이 있다면 자동 로그인
                if (isReLogin&&string.IsNullOrEmpty(LoginData.Cloud.loginValue.userAutoToken)==false)
                {
                    int maxLoginRetry = 3;
                    for (int loginAttempt = 1; loginAttempt <= maxLoginRetry; loginAttempt++)
                    {
                        try
                        {
                            var loginResPacket = await socialloginController.ReLoginProccess();
                            if (loginResPacket.IsSuccess)
                            {
                                if (string.IsNullOrEmpty(loginResPacket.Data.LatestVersion) == false)
                                {
                                    await PopupManager.Instance.OpenAsync<PopupVersionUpdate>(popup => { popup.SetWindow(true); });
                                }
                                if (loginResPacket.Data.Maintenance != null)
                                {
                                    PopupManager.Instance.Open<PopupServerMaintenance>(popup=>popup.SetMaintenanceTime(loginResPacket.Data.Maintenance));
                                    return;
                                }
                                await ProcessAfterUserLogin();
                            }
                            else
                            {
                                if (loginResPacket.Error.Code == ErrorCode.ENeedUpdate)
                                {
                                    PopupManager.Instance.Open<PopupVersionUpdate>(popup => { popup.SetWindow(false); });
                                }
                                else if (loginResPacket.Error.Code == ErrorCode.EMaintaining)
                                {
                                    PopupManager.Instance.Open<PopupServerMaintenance>(popup => { popup.SetMaintenanceAtLogin(loginResPacket.Error.Detail); });
                                }
                                else
                                {
                                    socialloginController.TotalLoginWindowOpen();
                                }
                            }
                            break;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"로그인/로비 진입 중 연결 오류 (시도 {loginAttempt}/{maxLoginRetry}): {ex.Message}");
                            
                            if (loginAttempt >= maxLoginRetry)
                            {
                                Debug.LogError("로그인 재시도 최대 횟수 초과");
                                PopupManager.Instance.Open<PopupNetworkCheck>();
                                return;
                            }
                            
                            int waitResult = await UniTask.WhenAny(
                                UniTask.WaitUntil(() => CPPlayer.Server.lobbyConnection?.isConnected == true && Services.Lobby != null),
                                UniTask.Delay(15000)
                            );
                            
                            if (waitResult != 0)
                            {
                                Debug.LogError("로비 재연결 대기 타임아웃");
                                PopupManager.Instance.Open<PopupNetworkCheck>();
                                return;
                            }
                            
                            await UniTask.Delay(1000);
                        }
                    }
                }
                else
                {
                    socialloginController.TotalLoginWindowOpen();
                 
                }
            }
        }

        /// <summary>
        ///본인인증 로그인 모두 끝내면 이쪽으로 콜백 받음 서버데티어,로컬 데이터 비교 하여 시간 최신것으로 유저 데이터(옵션 등) 불러온뒤에 씬 로드 
        /// </summary>
        async UniTask ProcessAfterUserLogin()
        {
            socialloginController.loadingScreenPopup.SetActive(true);

            //model data setting
            bool isReLogin = LocalSaveLoader.ExistsLoginAutoToken();

            var localCloudInfo = LocalSaveLoader.LoadUserCloudData();
            var userinfo = await Services.Lobby.UserSettingsInfoReq();

            if (isReLogin)
            {
                CPPlayer.Cloud = localCloudInfo;
            }
            else
            {
                if (userinfo.IsSuccess)
                {
                    if (string.IsNullOrEmpty(userinfo.Data.Settings))
                    {
                        CPPlayer.Cloud = localCloudInfo;
                    }
                    else
                    {
                        var serverCloudinfo = JsonUtility.FromJson<UserCloudData>(userinfo.Data.Settings);
                        CPPlayer.Cloud = serverCloudinfo;
                    }
                }
                else
                {
                    CPPlayer.Cloud = localCloudInfo;
                }
            }

            //model data setting

            if (!await EnsureIdentityVerificationIfNeeded())
            {
                socialloginController.loadingScreenPopup.SetActive(false);
                socialloginController.TotalLoginWindowOpen();
                return;
            }

            //login register complete save local data
            LocalSaveLoader.SaveUserCloudData();
            var ds = LoginData.Cloud.loginValue.uidList;
            SaveLoginData();
            //login register complete save local data
            
            Debug.Log("게임 씬으로 로드!");

            asyncOperation = SceneManager.LoadSceneAsync("Game");
            asyncOperation.allowSceneActivation = true;
        }

        async UniTask<bool> EnsureIdentityVerificationIfNeeded()
        {
            if (Application.isEditor)
                return true;
            if (IsIdentityVerificationSkipByBuild())
                return true;
            if (IsTestAccountByUserId())
                return true;
            if (LoginData.Cloud.loginValue.loginres.IsIdentityVerify == 1)
                return true;

            var memberData = await Services.Lobby.MemberReqAsync(LoginData.Cloud.loginValue.userAutoToken);
            if (!memberData.IsSuccess)
                return true;

            if (!IsIdentityVerificationExpired(memberData.Data.ReVerifyAt))
                return true;

            socialloginController.loadingScreenPopup.SetActive(false);

            bool confirm = await ShowIdentityReverifyPopup();
            if (!confirm)
                return false;

            bool identifySuccess = await socialloginController.IdentifyForReverify();
            if (!identifySuccess)
                return false;

            socialloginController.loadingScreenPopup.SetActive(true);
            return true;
        }

        bool IsIdentityVerificationExpired(int reVerifyAt)
        {
            if (reVerifyAt <= 0)
                return true;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now >= reVerifyAt;
        }

        bool IsIdentityVerificationSkipByBuild()
        {
#if SKIP_IDENTITY_VERIFICATION
            return true;
#else
            return false;
#endif
        }

        bool IsTestAccountByUserId()
        {
            var userId = LoginData.Cloud.loginValue.userAccountID;
            return !string.IsNullOrEmpty(userId)
                   && userId.IndexOf("atest", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        async UniTask<bool> ShowIdentityReverifyPopup()
        {
            if (openPopup == null)
                return true;

            var tcs = new UniTaskCompletionSource<bool>();
            openPopup.Invoke(
                StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IdentityVerificationRequired].StringToLocal,
                StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IdentityVerificationExpiredReauth].StringToLocal,
                () => tcs.TrySetResult(true),
                () => tcs.TrySetResult(false)
            );
            return await tcs.Task;
        }

        /// <summary>
        /// 최초 로그인데이터 저장 위치
        /// </summary>
        void SaveLoginData()
        {
          
            LocalSaveLoader.SaveAutoLoginUserIdList();
            LocalSaveLoader.SaveLoginCloudData();

            LocalSaveLoader.SaveLoginToken();
        }

        #endregion

        #region UserLoginWebview(oblsolute)

        private async UniTask SetUserInformation()
        {
            await UniTask.Delay(200);
            webViewBack.SetActive(true);
            await UniTask.Delay(2000);

            ////login web view open
            UniWebView.SetAllowJavaScriptOpenWindow(true);

            string token = PlayerPrefs.GetString("AccountToken", "");
            string url = (PlayerPrefs.GetInt("LogOut", 0) == 1)
                ? "https://login.dev.atozgames.net/login" //일반 로그인
                : $"https://login.dev.atozgames.net/login/last?token={token}"; //자동로그인

            ///url = "https://demo.login.dev.atozgames.net";
            try
            {
                OpenLoginWeb(url, false);

                await UniTask.WaitUntil(() => isLoginComplete == true);

                //FirebaseRD.Init(CPPlayer.UserInfo.currentAccountId).Forget();
            }
            catch (System.Exception e)
            {
                //loginFail
                Debug.LogError($"Login Fail:::" + e.Message);
                throw;
            }


            ////login web view END
            isUserSet = true;
        }


        void OpenLoginWeb(string url, bool isPortrait)
        {
            _isPortraitView = isPortrait;
            bool signUp = url.EndsWith("/signup", StringComparison.OrdinalIgnoreCase);

            //            Debug.LogError($"login url call::{url}");
            webViewObject = new GameObject("UniWebViewObject");
            webView = webViewObject.AddComponent<UniWebView>();

            webView.SetSupportMultipleWindows(true, true);
            webView.EmbeddedToolbar.SetPosition(UniWebViewToolbarPosition.Top); // 상단 표시
            webView.EmbeddedToolbar.SetDoneButtonText(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Close].StringToLocal);

            // 프레임 설정 (풀스크린 또는 팝업)
            const float ratio = 19.5f / 9f;
            float sw = Screen.width, sh = Screen.height;
            float toolbarHeightPx = toolbarRectTransform.rect.height * canvas.scaleFactor;

            webView.Frame = new Rect(0, toolbarHeightPx, sw, sh - toolbarHeightPx);
            if (isPortrait || sw / sh > ratio)
            {
                webView.Frame = new Rect(0, toolbarHeightPx, sw, sh - toolbarHeightPx);
            }
            else
            {
                float w = sw;
                float h = w / ratio;
                float x = (sw - w) * 0.5f;
                float y = (sh - h) * 0.5f;
                webView.Frame = new Rect(x, y, w, h);
            }

            webView.BackgroundColor = new Color32(0xBB, 0xC2, 0xE0, 0xFF);
            webView.SetContentInsetAdjustmentBehavior(UniWebViewContentInsetAdjustmentBehavior.Never);
            toolbarRectTransform.gameObject.SetActive(false);

#if UNITY_IOS
            webView.SetUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1");
#elif UNITY_ANDROID
            webView.SetUserAgent("Mozilla/5.0 (Linux; Android 13; Pixel 6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36");
#endif
            webView.SetSupportMultipleWindows(true, true);
            webView.SetAcceptThirdPartyCookies(true);

            if (PlayerPrefs.HasKey(Constraints.COOKIE_PREF_KEY))
            {
                string cookieString = PlayerPrefs.GetString(Constraints.COOKIE_PREF_KEY);
                webView.SetHeaderField("Cookie", cookieString);
            }

            // Handle custom schemes
            webView.AddUrlScheme("atoz-signin");
            webView.AddUrlScheme("atoz-signup");

            // 이벤트 등록
            webView.OnPageStarted += OnPageStarted;
            webView.OnPageFinished += OnPageFinished;
            webView.OnPageErrorReceived += (view, errorCode, message) => { Debug.LogError($"[OnPageError] {errorCode}: {message}"); };
            webView.OnMessageReceived += (view, message) =>
            {
                Debug.Log("[OnMessageReceived] " + message.RawMessage);
                HandleCustomScheme(message.RawMessage);
            };

            webView.OnMultipleWindowOpened += (view, windowId) =>
            {
                Debug.Log($"팝업 열림: {windowId}");
                // (별도 Show 호출 불필요) UniWebView가 자동으로 팝업을 동일한 위치/크기로 생성합니다.
            };

            // 팝업이 닫힐 때
            webView.OnMultipleWindowClosed += (view, windowId) =>
            {
                Debug.Log($"팝업 닫힘: {windowId}");
                // 팝업이 자동으로 삭제되니, 추가 정리 작업이 필요 없습니다.
            };

            webView.OnShouldClose += view =>
            {
                toolbarRectTransform.gameObject.SetActive(false);
                CloseWebView(false);
                return true;
            };


            // 로드 & 표시
            webView.Load(url, false, null);
            _state = ViewState.Showing;
            webView.Show(true, UniWebViewTransitionEdge.None, 0.0f, () => { Debug.Log("[OpenWebView] WebView shown"); });
            if (!isPortrait)
                StartCoroutine(WatchRoute());


#if UNITY_ANDROID && UNITY_EDITOR

            _isPortraitView = false;


            CloseWebView(false);

            OrientationSet();
            PlayerPrefs.SetInt("LogOut", 0);

            isLoginComplete = true;
            isWebviewClosed = true;
#endif
        }

        private void OnPageStarted(UniWebView view, string url)
        {
            Debug.Log("[OnPageStarted] URL: " + url);

            if (url.Contains("atoz-signin:/"))
            {
                Debug.Log("[로그인 URL 감지됨] → 수동 처리 시작");

                string json = ExtractJsonFromUrl(url);
                CloseWebView(false);
                HandleLoginResult(json);
            }
            else if (url.Contains("atoz-signin:"))
            {
                if (_state == ViewState.Showing)
                {
                    _state = ViewState.Hidden;
                    Debug.LogError("하이드 호출2");
                    webView.Hide(true, UniWebViewTransitionEdge.None, 0.0f, () => { CloseWebView(false); });
                }
            }

            if (url.Contains("google") || url.Contains("apple") || url.Contains("kakao") || url.Contains("naver") || url.Contains("firebaseapp"))
            {
                //toolbarRectTransform.gameObject.SetActive(true);
            }
            else
            {
                //toolbarRectTransform.gameObject.SetActive(false);
            }
        }

        enum ViewState
        {
            Hidden,
            Showing,
            Visible,
            Hiding
        }

        private ViewState _state = ViewState.Showing;

        public void CloseWebView(bool openSignUp)
        {
            if (!openSignUp)
            {
                //_isPortraitView = false;
            }

            if (webView == null) return;

            if (_state == ViewState.Showing)
            {
                _state = ViewState.Hidden;
                webView.Hide(true, UniWebViewTransitionEdge.None, 0.0f, () => { StartCoroutine(CloseWebViewRoutine()); });
            }

            //webViewBack.SetActive(false);
        }

        private IEnumerator CloseWebViewRoutine()
        {
            // ✅ 최소 2프레임 대기
            yield return null;
            yield return null;
#if UNITY_IOS
            webView.CleanCache();
#endif
            // ✅ GameObject 자체를 제거 (loginWindow 안 쓰고 따로 만든 WebView 오브젝트일 때)
            Destroy(webView.gameObject);
            webView = null;

            yield return null;
            yield return null;

            // ✅ UI 복구
            SaveWebViewCookies();

            // ✅ EventSystem 리셋 (iOS에서 터치 복구에 중요)
            currentEventSystem.enabled = false;
            yield return null;
            currentEventSystem.enabled = true;

            isWebviewClosed = true;
        }

        private string ExtractJsonFromUrl(string fullUrl)
        {
            int idx = fullUrl.IndexOf("atoz-signin:/") + "atoz-signin:/".Length;
            string jsonEncoded = fullUrl.Substring(idx);
            string decoded = UnityWebRequest.UnEscapeURL(jsonEncoded).TrimStart('/');
            return decoded;
        }

        private void HandleLoginResult(string json)
        {
            try
            {
                LoginResult result = JsonUtility.FromJson<LoginResult>(json);
                if (!string.IsNullOrEmpty(result.accountId))
                {
                    PlayerPrefs.SetString("AccountToken", result.idToken);

                    SetStartMain(result);
                }
                else
                {
                    Debug.LogError("로그인 실패: accountId 없음");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("HandleLoginResult Error: " + e.Message);
            }
        }

        private void SaveWebViewCookies()
        {
            if (webView == null) return;

            // document.cookie 값을 가져오는 JS
            string js = "(function() { return document.cookie; })();";

            webView.EvaluateJavaScript(js, payload =>
            {
                // UniWebViewNativeResultPayload 에는 소문자 프로퍼티가 있습니다.
                // resultCode 가 "0" 이면 정상 실행, data 에 실제 쿠키 문자열이 담겨있습니다.
                if (payload.resultCode == "0")
                {
                    string cookieString = payload.data;
                    if (!string.IsNullOrEmpty(cookieString))
                    {
                        PlayerPrefs.SetString(Constraints.COOKIE_PREF_KEY, cookieString);
                        PlayerPrefs.Save();
                    }
                }
                else
                {
                    Debug.LogWarning($"JS 실행 오류, resultCode={payload.resultCode}");
                }
            });
        }

        private void OnPageFinished(UniWebView view, int statusCode, string url)
        {
        }

        private void HandleCustomScheme(string url)
        {
            if (url.Contains("atoz-signin:/"))
            {
                CloseWebView(false);
                int idx = url.IndexOf("atoz-signin:/") + "atoz-signin:/".Length;
                string json = url.Substring(idx);
                string decoded = UnityWebRequest.UnEscapeURL(json).TrimStart('/');
                HandleLoginResult(decoded);
            }
        }


        private IEnumerator WatchRoute()
        {
            while (webView != null)
            {
                bool done = false;
                bool shouldReopen = false;

                webView.EvaluateJavaScript("window.location.pathname;", result =>
                {
                    if (result.resultCode == "0")
                    {
                        string path = result.data;
                        Debug.Log("[Route Watcher] 현재 경로: " + path);

                        if (path.Contains("/signup") && !_isPortraitView)
                        {
                            shouldReopen = true;
                        }
                    }

                    done = true;
                });
                yield return new WaitUntil(() => done);

                if (shouldReopen)
                {
                    Debug.Log("[Route Watcher] /signup 감지됨 → 세로모드로 재오픈");
                    yield break;
                }

                yield return new WaitForSeconds(0.2f);
            }
        }

        void SetStartMain(LoginResult account)
        {
            _isPortraitView = false;

            OrientationSet();
            PlayerPrefs.SetInt("LogOut", 0);
            isLoginComplete = true;
        }

        void OrientationSet()
        {
            if (_isPortraitView)
            {
                //Screen.orientation = ScreenOrientation.Portrait;
            }
            else
            {
                StartCoroutine(EnableLandscapeAutoRotationNextFrame());
            }
        }


        public void GoBack()
        {
            if (webView != null && webView.CanGoBack)
            {
                webView.GoBack();
                webView.GoBack();
            }
        }

        // 🔜 앞으로 가기
        public void GoForward()
        {
            if (webView != null && webView.CanGoForward)
            {
                webView.GoForward();
            }
        }

        #endregion

        async UniTaskVoid SetTimerText()
        {
            while (true)
            {
                // 로딩 진행률 계산
                float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);

                timer += Time.deltaTime;

                // 씬 로딩 완료 & 최소 시간 경과 시 전환
                if (asyncOperation.progress >= 0.9f && timer >= minimumLoadingTime)
                {
                    timer = minimumLoadingTime + 1;
                    break;
                }

                await UniTask.Yield(_cts.Token);
            }
        }

        IEnumerator EnableLandscapeAutoRotationNextFrame()
        {
            // 1단계: LandscapeLeft로 강제 설정
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            // 2단계: 2프레임 대기 (렌더링 강제 갱신)
            yield return null;
            yield return null;

            // 3단계: 다시 AutoRotation으로 전환
            Screen.orientation = ScreenOrientation.AutoRotation;

            // 4단계: UI 레이아웃 강제 갱신 (일부 디바이스에서 필요)
            Canvas.ForceUpdateCanvases();
            Screen.fullScreen = false; // 👈 강제 리프레시 (일부 디바이스에서 필요)
            Screen.fullScreen = true;
        }

        [ContextMenu("test")]
        public void Test()
        {
            var sdsd = LocalSaveLoader.LoadUserCloudData();
            Debug.LogError($" 로드 예약베팅 값{sdsd.optionValue.reserveBet}");
        }

        #region LoadResources

        public static Action<float, string> OnProgress;

        private bool isLoadResourcesComplete = false;

        public async UniTask LoadResources()
        {
            
            
            isLoadResourcesComplete = false;
            _cts = new CancellationTokenSource();

            // ========== S3 리소스 버전 체크 및 다운로드 ==========
            OnProgress?.Invoke(0f, "리소스 확인 중...");

            try
            {
                ResourceVersionManager.Instance.OnDownloadProgress += (progress, text) =>
                {
                    // 0% ~ 30% 구간을 S3 다운로드에 할당
                    float adjustedProgress = progress * 0.3f;
                    OnProgress?.Invoke(adjustedProgress, text);
                };

                bool downloadSuccess = await ResourceVersionManager.Instance.CheckAndDownloadResources();

                if (!downloadSuccess)
                    Debug.LogWarning("S3 리소스 다운로드 실패, 로컬 번들 사용");
                else
                    Debug.Log($"리소스 버전: {ResourceVersionManager.Instance.CurrentResourceVersion}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SceneLoadResources] S3 리소스 체크 실패: {e.Message}, 로컬 번들 사용");
            }
            // ========== S3 리소스 버전 체크 완료 ==========

            var handles = new List<(string name, AsyncOperationHandle handle)>();

            await ViewCanvas.StartLoadAssets(_cts);

            var h2 = LobbyResourcesBundle.BeginLoad();
            handles.Add(("Lobby", h2));
            var h3 = InGameResourcesBundle.BeginLoad();
            handles.Add(("InGame", h3));
            var h4 = PopupDatabase.BeginLoad();
            handles.Add(("PopupDB", h4));
            var h5 = ItemBundle.BeginLoad();
            handles.Add(("Item", h5));
            var h6 = SocialBundle.BeginLoad();
            handles.Add(("Social", h6));

            await TrackProgressUntilDone(handles, _cts.Token);

            foreach (var handleValue in handles)
            {
                if (handleValue.handle.Status != AsyncOperationStatus.Succeeded)
                    throw new System.Exception($"{handleValue.name}bundle load fail");
            }

            LobbyResourcesBundle.Loaded = (LobbyResourcesBundle)h2.Result;
            InGameResourcesBundle.Loaded = (InGameResourcesBundle)h3.Result;
            PopupDatabase.Loaded = (PopupDatabase)h4.Result;
            ItemBundle.Loaded = (ItemBundle)h5.Result;
            SocialBundle.Loaded = (SocialBundle)h6.Result;

            // 4) 마지막 내부 초기화도 로딩바 100% 직전에 포함시키면 UX 좋음
            //OnProgress?.Invoke(0.95f, "Initializing...");
            OnProgress?.Invoke(0.95f, $"95%");
            PopupManager.Instance.Setup<PopupToast>();

            //OnProgress?.Invoke(1f, "Done");
            OnProgress?.Invoke(1.1f, $"100%");
            isLoadResourcesComplete = true;
        }

        private async UniTask TrackProgressUntilDone(
            List<(string name, AsyncOperationHandle handle)> handles,
            CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                float sum = 0f;
                int doneCount = 0;

                foreach (var (name, h) in handles)
                {
                    // PercentComplete는 0~1
                    sum += h.PercentComplete;
                    if (h.IsDone) doneCount++;
                }

                float progress = sum / handles.Count;

                string current = GetMostPendingName(handles);
                // S3 다운로드(0~30%) + 로컬 번들(30~95%) = 30% ~ 95% 구간 사용
                float adjustedProgress = 0.3f + (progress * 0.65f);
                OnProgress?.Invoke(adjustedProgress, $"{adjustedProgress * 100:F0}%");

                if (doneCount == handles.Count)
                    break;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private string GetMostPendingName(List<(string name, AsyncOperationHandle handle)> handles)
        {
            float min = 999f;
            string pick = "";
            foreach (var (name, h) in handles)
            {
                if (h.IsDone) continue;
                if (h.PercentComplete < min)
                {
                    min = h.PercentComplete;
                    pick = name;
                }
            }

            return string.IsNullOrEmpty(pick) ? "Finishing..." : pick;
        }

        public static void OpenPopup(IBackButtonSender ibbs)
        {
            _backButtonHandlers.Push(ibbs);
        }

        public static void ClosePopup()
        {
            if (_backButtonHandlers.Count > 0)
            {
                var window = _backButtonHandlers.Pop();
                window.CloseThisWindow();
            }
        }
        public static void CloseAllPopup()
        {
            while (_backButtonHandlers.Count > 0)
            {
                var window = _backButtonHandlers.Pop();
                window.CloseThisWindow();
            }
        }

        #endregion
    }
}