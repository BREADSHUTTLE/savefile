using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace CAPYBARA
{
    public class PopupQuestionAccountDelete : BasePopup
    {
        [SerializeField] private CPButton yesButton;
        [SerializeField] private CPButton noButton;

        private const string LoadingSceneName = "Loading";
        private const string JWT_URL = "https://gw.dev.atozgames.net/api/ingame-token";
        private string WithdrawalLimitTitle;
        private string WithdrawalLimitMessage;

        private GameObject webViewObject;
        private UniWebView webView;
        private bool isIdentifyCompleted = false;
        private bool isProcessing = false;

        protected override void OnInit()
        {
            if (yesButton != null)
            {
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(() => OnClickYes().Forget());
            }
            
            if (noButton != null)
            {
                noButton.onClick.RemoveAllListeners();
                noButton.onClick.AddListener(Close);
            }
            
            WithdrawalLimitTitle = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AccountDeleteLimitTitle].StringToLocal;
            WithdrawalLimitMessage = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AccountDeleteLimitMsg].StringToLocal;
        }

        private async UniTaskVoid OnClickYes()
        {
            if (isProcessing)
                return;

            isProcessing = true;
            try
            {
                bool identifyResult = await Identify();
                if (!identifyResult)
                {
                    isProcessing = false;
                    return;
                }

                var result = await Services.Lobby.WithdrawalAsync();
                if (result.IsSuccess)
                {
                    Debug.Log("탈퇴 성공");
                    PopupManager.Instance.CloseAll();
                    
                    PlayerPrefs.DeleteAll();

                    ConnectionManager.Instance.Dispose();
                    
                    PoolManager.Clear();
                    
                    SceneManager.LoadScene(LoadingSceneName);
                }
                else
                {
                    Debug.LogError($"탈퇴 실패: {result.Error}");
                    if (result.Error.Code == lobby.ErrorCode.EMaxValue)
                    {
                        PopupManager.Instance.CloseAll();
                        PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(WithdrawalLimitTitle, WithdrawalLimitMessage, null));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AccountDelete] 예외 발생: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                isProcessing = false;
            }
        }

        #region 본인인증

        private async UniTask<bool> Identify()
        {
            try
            {
                isIdentifyCompleted = false;
                Debug.Log("[AccountDelete] 본인인증 시작");
                Debug.Log($"[AccountDelete] 현재 userAutoToken: {LoginData.Cloud.loginValue.userAutoToken}");
                
                if (Application.isEditor || IsIdentityVerificationSkipByBuild())
                {
                    Debug.Log("[AccountDelete] 본인인증 스킵");
                    await UniTask.Yield();
                    return true;
                }

                await OpenIdentifyUrl();
                Debug.Log("[AccountDelete] 웹뷰 열림, 완료 대기 중...");
                await UniTask.WaitUntil(() => isIdentifyCompleted);
                Debug.Log("[AccountDelete] 웹뷰 닫힘 감지");
                
                // SocialLoginController와 동일하게 userMemberId로 성공 여부 판단
                bool success = !string.IsNullOrEmpty(LoginData.Cloud.loginValue.userAutoToken);
                
                if (success)
                {
                    Debug.Log("[AccountDelete] 본인인증 성공");
                    return true;
                }
                else
                {
                    Debug.Log("[AccountDelete] 본인인증 취소/실패");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AccountDelete] 본인인증 예외: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        private bool IsIdentityVerificationSkipByBuild()
        {
#if SKIP_IDENTITY_VERIFICATION
            return true;
#else
            return false;
#endif
        }

        private async UniTask OpenIdentifyUrl()
        {
            if (webView == null)
            {
                if (webViewObject == null)
                    webViewObject = new GameObject("WebViewObject_AccountDelete");
                    
                webView = webViewObject.AddComponent<UniWebView>();
                webView.Frame = new Rect(0, 0, Screen.width, Screen.height);
                webView.SetShowToolbar(true);
                webView.SetSupportMultipleWindows(true);
                webView.SetAllowBackForwardNavigationGestures(true);
                webView.AddUrlScheme("uniwebview");

                webView.OnPageStarted += OnPageStarted;
                webView.OnPageFinished += OnPageFinished;
                webView.OnLoadingErrorReceived += OnPageError;
                webView.OnMessageReceived += OnMessageReceived;
                webView.OnMultipleWindowOpened += OnPopupOpened;
                webView.OnMultipleWindowClosed += OnPopupClosed;
                webView.OnShouldClose += (view) =>
                {
                    CleanupWebView();
                    return true;
                };
                webView.SetSupportMultipleWindows(true, true);
                webView.SetAcceptThirdPartyCookies(true);
            }

            var jwtToken = await GetJWT();
            var authUrl = "https://gw.dev.atozgames.net/identity-verification" +
                          $"?wv={jwtToken.token}";

            webView.Load(authUrl);
            webView.Show();
        }

        private void OnPopupOpened(UniWebView parent, string windowId)
        {
            Debug.Log($"[AccountDelete WebView] popup opened: {windowId}");
        }

        private void OnPopupClosed(UniWebView parent, string windowId)
        {
            Debug.Log($"[AccountDelete WebView] popup closed: {windowId}");
        }

        private void OnPageStarted(UniWebView view, string url)
        {
            Debug.Log($"[AccountDelete WebView] Start Page: {url}");
            TryHandleRedirect(url);
        }

        private void OnPageFinished(UniWebView view, int statusCode, string url)
        {
            Debug.Log($"[AccountDelete WebView] Finish Page: {statusCode} / {url}");
            TryHandleRedirect(url);
        }

        private void OnPageError(UniWebView view, int code, string message, UniWebViewNativeResultPayload payload)
        {
            Debug.LogError($"[AccountDelete WebView] Error Page: {code} / {message}");
        }

        private void TryHandleRedirect(string url)
        {
            Debug.Log($"[AccountDelete WebView] Redirect: {url}");
            var qs = ParseQuery(url);
            if (qs.TryGetValue("code", out var code)) Debug.Log($"[AccountDelete] code={code}");
            if (qs.TryGetValue("state", out var state)) Debug.Log($"[AccountDelete] state={state}");
            if (qs.TryGetValue("wv", out var wv)) Debug.Log($"[AccountDelete] wv={wv}");
        }

        private Dictionary<string, string> ParseQuery(string fullUrl)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var uri = new Uri(fullUrl);
                var q = uri.Query;
                if (string.IsNullOrEmpty(q) || q.Length <= 1) return dict;

                foreach (var pair in q.Substring(1).Split('&'))
                {
                    if (string.IsNullOrEmpty(pair)) continue;
                    var kv = pair.Split(new[] { '=' }, 2);
                    var k = Uri.UnescapeDataString(kv[0]);
                    var v = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
                    dict[k] = v;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AccountDelete] ParseQuery fail: {e.Message}");
            }
            
            return dict;
        }

        

        private void OnMessageReceived(UniWebView view, UniWebViewMessage msg)
        {
            Debug.Log($"[AccountDelete WebView] path: {msg.Path}");
            
            switch (msg.Path)
            {
                case "identify":
                    if (msg.Args.TryGetValue("payload", out var identifyJson))
                    {
                        var webPayload = JsonUtility.FromJson<SocialLoginController.WebPayload>(identifyJson);
                        if (webPayload != null)
                        {
                            
                            // 본인인증 토큰 및 멤버 ID 업데이트
                            if (webPayload.auth != null)
                            {
                                LoginData.Cloud.loginValue.registerToken = webPayload.auth.registerToken;
                                LoginData.Cloud.loginValue.userAutoToken = webPayload.auth.autoToken;
                            }
                                
                        }
                    }
                    break;
                    
                case "close":
                    Debug.Log($"[AccountDelete WebView] close path: {msg.Path}");
                    CloseWithFade();
                    break;
                    
                default:
                    Debug.Log($"[AccountDelete WebView] Unknown path: {msg.Path}");
                    break;
            }
            
            CleanupWebView();
        }

        private void CloseWithFade()
        {
            if (webView == null)
                return;

            webView.Hide(true, UniWebViewTransitionEdge.Bottom, 0.25f, () => { CleanupWebView(); });
        }

        private void CleanupWebView()
        {
            if (webView != null)
            {
                webView.OnPageStarted -= OnPageStarted;
                webView.OnPageFinished -= OnPageFinished;
                webView.OnLoadingErrorReceived -= OnPageError;
                webView.OnMessageReceived -= OnMessageReceived;
                webView.OnMultipleWindowOpened -= OnPopupOpened;
                webView.OnMultipleWindowClosed -= OnPopupClosed;
                webView.CleanCache();
                Destroy(webView);
                webView = null;

                Debug.Log("[AccountDelete] WebView 정리 완료");
            }

            if (webViewObject != null)
            {
                Destroy(webViewObject);
                webViewObject = null;
            }

            isIdentifyCompleted = true;
        }

        [Serializable]
        private class JWTRes
        {
            public string token;
        }

        private async UniTask<JWTRes> GetJWT()
        {
            using var req = new UnityWebRequest(JWT_URL, UnityWebRequest.kHttpVerbPOST);
            req.downloadHandler = new DownloadHandlerBuffer();

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success ||
                req.responseCode < 200 || req.responseCode >= 300)
                throw new Exception($"HTTP {(int)req.responseCode} : {req.error}\n{req.downloadHandler?.text}");

            var json = req.downloadHandler.text;
            var res = JsonUtility.FromJson<JWTRes>(json);
            return res;
        }

        #endregion

        protected override void OnOpen()
        {
            base.OnOpen();
            isProcessing = false;
            isIdentifyCompleted = false;
        }

        protected override void OnClose()
        {
            base.OnClose();
            CleanupWebView();
        }
    }
}
