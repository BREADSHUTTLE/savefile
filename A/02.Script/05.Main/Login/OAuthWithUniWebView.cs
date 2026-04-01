using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class OAuthWithUniWebView : MonoBehaviour
    {
        const string NAVER_TOKEN_URL = "https://nid.naver.com/oauth2.0/token";
        const string NAVER_PROFILE_URL = "https://openapi.naver.com/v1/nid/me";
        const string KAKAO_TOKEN_URL = "https://kauth.kakao.com/oauth/token";
        const string KAKAO_PROFILE_URL = "https://kapi.kakao.com/v2/user/me";

        private const string JWT_URL = "https://gw.dev.atozgames.net/api/ingame-token";

        [Header("Common")] string redirectUri_naver = "https://gw.dev.atozgames.net/api/oauth/naver";
        string redirectUri_kakao = "https://gw.dev.atozgames.net/api/oauth/kakao";
        [SerializeField] bool useNaver = true;

        [Header("Naver")] [SerializeField] string naverClientId = "vAG4NmKULK0uLdxoAMuv";
        [SerializeField] string naverClientSecret = "JIHGdIHk9l";

        [Header("Kakao")] [SerializeField] string kakaoRestApiKey = "e4629e8d8ee2ef4bf4c728a997210bf6";
        string kakaoClientSecret = "ccrPEnLYkkenEP2teqBIyUipHUfsi7uG";


        [SerializeField] private string expectedWV;
        GameObject webViewObject;
        UniWebView webView;
        string codeVerifier;

        System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
        CancellationTokenSource _cts = new CancellationTokenSource();

        [SerializeField] private Button testButton;


        private void Awake()
        {
            _cts.Cancel();
            _cts = null;
            _cts = new CancellationTokenSource();

            // testButton.onClick.AddListener(() => OpenTestUrl().Forget());
        }

        void OnDestroy()
        {
            if (webView != null)
            {
                webView.OnPageStarted -= OnPageStarted;
                webView.OnMultipleWindowOpened -= OnPopupOpened;
                webView.OnMultipleWindowClosed -= OnPopupClosed;
                webView.OnMessageReceived -= OnMessageReceived;
            }
        }

        bool isLoginComplete = false;
        private bool dataLoadComplete = false;

        public async UniTask<bool> StartLogin(bool isnaver)
        {
            isLoginComplete = false;
            dataLoadComplete = false;
            useNaver = isnaver;
            string authUrl;

            var jwtToken = await GetJWT();
            expectedWV = jwtToken.token;
            if (useNaver)
            {
                codeVerifier = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                var codeChallenge = Base64UrlNoPad(Sha256(codeVerifier));

                authUrl = "https://gw.dev.atozgames.net/naver?" +
                          $"&naverClientId={naverClientId}" +
                          $"&redirectUri_naver={Uri.EscapeDataString(redirectUri_naver)}" +
                          $"&expectedWV={expectedWV}" +
                          $"&email={Uri.EscapeDataString("name email")}" +
                          $"&codeChallenge={codeChallenge}" +
                          $"&code_challenge_method=S256" + "";
            }
            else
            {
                codeVerifier = null;
                authUrl = "https://kauth.kakao.com/oauth/authorize" +
                          $"?response_type=code&client_id={kakaoRestApiKey}" +
                          $"&redirect_uri={Uri.EscapeDataString(redirectUri_kakao)}" +
                          $"&state={expectedWV}" +
                          $"&scope={Uri.EscapeDataString("account_email")}";
            }


            //Debug.LogError($"URL 설정:{authUrl}");
            if (webView == null)
            {
                if (webViewObject == null)
                    webViewObject = new GameObject("WebViewObject");
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
                    // 사용자가 네이티브 Back 등으로 닫을 때
                    CleanupWebView();
                    return true;
                };
            }

            webView.Load(authUrl);
            webView.Show();

            await UniTask.WaitUntil(() => isLoginComplete);
            await UniTask.WaitUntil(() => dataLoadComplete);

            return !string.IsNullOrEmpty(LoginData.Cloud.loginValue.userSocialToken);
        }

        async UniTask OpenTestUrl()
        {
            if (webView == null)
            {
                if (webViewObject == null)
                    webViewObject = new GameObject("WebViewObject_identify");
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
                    // 사용자가 네이티브 Back 등으로 닫을 때
                    CleanupWebView();
                    return true;
                };
                webView.SetSupportMultipleWindows(true, true);
                webView.SetAcceptThirdPartyCookies(true);
            }

            var jwtToken = await GetJWT();
            var authUrl = "https://gw.dev.atozgames.net/identity-verification" +
                          $"?wv={jwtToken.token}";

            //authUrl = "http://127.0.0.1:5500/index.html";
            webView.Load(authUrl);
            webView.Show();
        }

        void OnPopupOpened(UniWebView parent, string windowId)
        {
            Debug.Log($"[UniWebView] popup opened: {windowId}");
            // 팝업도 OnMessageReceived 이벤트는 동일하게 들어옴.
        }


        void OnPopupClosed(UniWebView parent, string windowId)
        {
            Debug.Log($"[UniWebView] popup closed: {windowId}");
        }

        void OnPageStarted(UniWebView view, string url)
        {
            Debug.Log($"Start Page::{url}");
            if (useNaver)
            {
                if (url.StartsWith(redirectUri_naver, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log("[hint] Redirect detected. Waiting for uniwebview:// message…");
                }
            }
            else
            {
                if (url.StartsWith(redirectUri_kakao, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log("[hint] Redirect detected. Waiting for uniwebview:// message…");
                }
            }

            TryHandleRedirect(url);
        }

        void OnPageFinished(UniWebView view, int statusCode, string url)
        {
            Debug.Log($"Fisish Page::{statusCode}//{url}");
            // 팝업도 OnMessageReceived 이벤트는 동일하게 들어옴.
            TryHandleRedirect(url);
        }

        void OnPageError(UniWebView view, int code, string message, UniWebViewNativeResultPayload payload)
        {
            Debug.LogError($"Error Page::{code}//{message}");
            // 팝업도 OnMessageReceived 이벤트는 동일하게 들어옴.
        }

        void TryHandleRedirect(string url)
        {
            Debug.Log($"Redirect !!::{url}");
            // 팝업도 OnMessageReceived 이벤트는 동일하게 들어옴.

            var qs = ParseQuery(url);
            if (qs.TryGetValue("code", out var code)) Debug.Log($"[OAuth] code={code}");
            if (qs.TryGetValue("state", out var state)) Debug.Log($"[OAuth] state={state}");
            if (qs.TryGetValue("wv", out var wv)) Debug.Log($"[OAuth] wv={wv}");
        }


        Dictionary<string, string> ParseQuery(string fullUrl)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var uri = new Uri(fullUrl);
                var q = uri.Query; // "?a=1&b=2"
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
                Debug.LogWarning($"[OAuth] ParseQuery fail: {e.Message}");
            }

            return dict;
        }

        private bool _authDone = false;

        [Serializable]
        public class AuthRoot
        {
            public string data;
            public string comType;
        }

        [Serializable]
        public class WebPayload
        {
            public string code;
            public AuthRoot auth;
            public string wv;
            public long mid;
        }

        void OnMessageReceived(UniWebView view, UniWebViewMessage msg)
        {
            //if (msg.Scheme != "uniwebview") return;
            //if (msg.Path != "oauth") return;

            Debug.LogError($"[UniWebView] path: {msg.Path}");
            Debug.LogError($"[UniWebView] path: {msg.Args}");
            if (msg.Args.Count > 0)
            {
                foreach (var arg in msg.Args)
                {
                    Debug.LogError($"[UniWebView] path: {arg}");
                }
            }

            WebPayload webpayload = null;

            switch (msg.Path)
            {
                case "identify":
                    if (msg.Args.TryGetValue("payload", out var identifyjson))
                    {
                        webpayload = JsonUtility.FromJson<WebPayload>(identifyjson);
                    }

                    if (webpayload != null)
                    {
                        if (webpayload.auth != null)
                        {
                            Debug.LogError($"[PayLoadData] auth-data : {webpayload.auth.data}");
                            Debug.LogError($"[PayLoadData] auth-comtype : {webpayload.auth.comType}");
                        }
                        else
                        {
                            Debug.LogError($"[PayLoadData] is not exist");
                        }

                        Debug.LogError($"[PayLoadData] wv : {webpayload.wv}");
                        Debug.LogError($"[PayLoadData] mid : {webpayload.mid}");
                    }

                 
                    break;
                //카카오& 네이버 로그인
                case "auth":
                    if (msg.Path == "auth" && msg.Args.TryGetValue("payload", out var authjson))
                    {
                        Debug.LogError("[WEB→UNITY] identify payload: " + authjson);

                        webpayload = JsonUtility.FromJson<WebPayload>(authjson);
                    }

                    if (webpayload == null)
                    {
                        Debug.LogError("Missing code/state in message.");
                        return;
                    }

                    // CSRF 방지: state 검증
                    if (!string.Equals(webpayload.wv, expectedWV))
                    {
                        Debug.LogError($"State mismatch. expected={expectedWV}, got={webpayload.wv}");
                        FailAndClose();
                        return;
                    }

                    if (_authDone) return; // 중복 방지
                    _authDone = true;
                    Debug.LogError($"[OAuth] code: {webpayload.code}");
                    // 필요 시 UI 갱신 후 닫기
                    StartCoroutine(ExchangeCodeForToken(webpayload.code));
                    break;
                case "back":
                {
                    Debug.LogError($"[UniWebView] back button pressed, closing webview");
                    CloseWithFade();
                    break;
                }
                case "close":
                {
                    Debug.LogError($"[UniWebView] close path: {msg.Path}");
                    CloseWithFade();
                    break;
                }

                default:
                    Debug.Log($"[UniWebView] Unknown path: {msg.Path}");
                    break;
            }
        }

        private void FailAndClose()
        {
            // 실패 처리 UI 필요하면 표시
            CloseWithFade();
        }

        private void CloseWithFade()
        {
            if (webView == null) return;
            Debug.LogError("webview close");
            CleanupWebView();
            //webView.Hide(true, UniWebViewTransitionEdge.Bottom, 0.25f, () => { CleanupWebView(); });
        }

        private void CleanupWebView()
        {
            if (webView != null)
            {
                webView.OnMessageReceived -= OnMessageReceived;
                webView.CleanCache();
                Destroy(webView.gameObject);

                isLoginComplete = true;
                dataLoadComplete = true;  // 취소 경로에서도 await가 풀려야 함
                Debug.Log($"Login All Success!!! destroy webview!!");
            }
        }

        System.Collections.IEnumerator ExchangeCodeForToken(string code)
        {
            if (useNaver)
            {
                var f = new WWWForm();
                f.AddField("grant_type", "authorization_code");
                f.AddField("client_id", naverClientId);
                f.AddField("client_secret", naverClientSecret);
                f.AddField("code", code);
                f.AddField("state", expectedWV);
                f.AddField("redirect_uri", redirectUri_naver);
                if (!string.IsNullOrEmpty(codeVerifier)) f.AddField("code_verifier", codeVerifier);

                using (var req = UnityWebRequest.Post(NAVER_TOKEN_URL, f))
                {
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[uniywebView] error:{req.error}/code:{code}");
                        yield break;
                    }

                    var tk = JsonUtility.FromJson<NaverToken>(req.downloadHandler.text);
                    Debug.Log($"[Naver] access={Head(tk.access_token)}");
                    StartCoroutine(GetProfile("naver", NAVER_PROFILE_URL, tk.access_token));
                }
            }
            else
            {
                kakaoClientSecret = "ccrPEnLYkkenEP2teqBIyUipHUfsi7uG";

                var f = new WWWForm();
                f.AddField("grant_type", "authorization_code");
                f.AddField("client_id", kakaoRestApiKey);
                f.AddField("redirect_uri", redirectUri_kakao);
                f.AddField("code", code);
                if (!string.IsNullOrEmpty(kakaoClientSecret))
                    f.AddField("client_secret", kakaoClientSecret);

                using (var req = UnityWebRequest.Post(KAKAO_TOKEN_URL, f))
                {
                    req.SetRequestHeader(
                        "Content-Type",
                        "application/x-www-form-urlencoded;charset=utf-8"
                    );
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[uniywebView][{req.downloadHandler.text}] error:{req.error}/code:{code}");
                        yield break;
                    }

                    var tk = JsonUtility.FromJson<KakaoToken>(FixJson(req.downloadHandler.text));
                    Debug.Log($"[Kakao] access={Head(tk.access_token)}");
                    StartCoroutine(GetProfile("kakao", KAKAO_PROFILE_URL, tk.access_token));

                    Debug.Log(req.responseCode);
                    Debug.Log(req.downloadHandler.text);
                }
            }
        }

        [Serializable]
        public class KaKaoProfileRoot
        {
            public long id;
            public string connected_at;
            public KaKaoProfile kakao_account;
        }

        [Serializable]
        public class KaKaoProfile
        {
            public bool has_email;
            public bool email_needs_agreement;
            public bool is_email_valid;
            public bool is_email_verified;
            public string email;
        }

        [Serializable]
        public class NaverProfileRoot
        {
            public string resultcode;
            public string message;
            public NaverProfile response;
        }

        [Serializable]
        public class NaverProfile
        {
            public string id;
            public string email;
            public string name;
        }

        System.Collections.IEnumerator GetProfile(string provider, string url, string accessToken)
        {
            var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
                yield break;
            }

            Debug.LogError($"[{provider} profile] {req.downloadHandler.text}");

            if (provider == "naver")
            {
                var root = JsonUtility.FromJson<NaverProfileRoot>(req.downloadHandler.text);
                string userEmail = root.response.email;
                string userToken = root.response.id;

                string userID;
                userID = userEmail.Substring(0, userEmail.IndexOf('@'));

                LoginData.Cloud.loginValue.userAccountID = userID;
                LoginData.Cloud.loginValue.userSocialEmail = userEmail;
                LoginData.Cloud.loginValue.userSocialToken = userToken;
                LoginData.Cloud.loginValue.accessToken = accessToken;
                LoginData.Cloud.loginValue.loginType = LoginType.NAVER;
            }
            else
            {
                Debug.LogError($"KAKAO Login All Success!!! _0");
                var root = JsonUtility.FromJson<KaKaoProfileRoot>(req.downloadHandler.text);
                string userEmail = root.kakao_account.email;
                string userToken = root.id.ToString();
                string userID;
                Debug.LogError($"KAKAO Login All Success!!! _1");
                userID = userEmail.Substring(0, userEmail.IndexOf('@'));

                Debug.LogError($"KAKAO Login All Success!!! _2 userToken : {userToken}");
                LoginData.Cloud.loginValue.userAccountID = userID;
                LoginData.Cloud.loginValue.userSocialEmail = userEmail;
                LoginData.Cloud.loginValue.userSocialToken = userToken;
                LoginData.Cloud.loginValue.accessToken = accessToken;
                LoginData.Cloud.loginValue.loginType = LoginType.KAKAO;

                Debug.LogError($"KAKAO Login All Success!!! ");
            }


            if (webView != null)
            {
                webView.OnMessageReceived -= OnMessageReceived;
                webView.CleanCache();
                //webView = null;
                Destroy(webView.gameObject);
                isLoginComplete = true;
                dataLoadComplete = true;

                Debug.LogError($"Login All Success!!! destroy webview!!");
            }
        }

        public class JWTRes
        {
            public string token;
        }

        async UniTask<JWTRes> GetJWT()
        {
            Debug.LogError("getJWT");
            using var req = new UnityWebRequest(JWT_URL, UnityWebRequest.kHttpVerbPOST);
            req.downloadHandler = new DownloadHandlerBuffer();

            await req.SendWebRequest().ToUniTask(cancellationToken: cts.Token);
            //Debug.LogError(req.ToString());
            if (req.result != UnityWebRequest.Result.Success ||
                req.responseCode < 200 || req.responseCode >= 300)
                throw new Exception($"HTTP {(int)req.responseCode} : {req.error}\n{req.downloadHandler?.text}");

            var json = req.downloadHandler.text;
            var res = JsonUtility.FromJson<JWTRes>(json);
            Debug.LogError(json);
            return res;
        }


        // === 유틸 ===
        static byte[] Sha256(string s)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        static string Base64UrlNoPad(byte[] b) =>
            Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        static string Head(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length <= 8 ? s : s.Substring(0, 8) + "…");
        static string FixJson(string raw) => raw.Replace("\\u0026", "&");

        [Serializable]
        class NaverToken
        {
            public string access_token;
            public string refresh_token;
            public int expires_in;
            public string token_type;
        }

        [Serializable]
        class KakaoToken
        {
            public string token_type;
            public string access_token;
            public int expires_in;
            public string refresh_token;
            public int refresh_token_expires_in;
            public string scope;
        }
    }
}