using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class OpenInfoWebView : MonoBehaviour
    {
        [SerializeField] private CPButton gotoInformBtn;

        [SerializeField] private string QAurl;
        [SerializeField] private string InfoUrl;
        [SerializeField] private bool isQA;

        [SerializeField] private GameObject backObj;
        GameObject webViewObject;
        UniWebView webView;
        [SerializeField] private RectTransform webviewRect;
        [SerializeField] private RectTransform fullWebviewRect;

        private void Awake()
        {
            gotoInformBtn.onClick.AddListener(() => { OpenIdentifyUrl().Forget(); });
        }

        async UniTask OpenIdentifyUrl()
        {
            webViewObject = WebViewManager.Instance.webViewObject;
            webView = WebViewManager.Instance.webView;

            if (isQA)
            {
                webView.ReferenceRectTransform = fullWebviewRect;
            }
            else
            {
                    webView.ReferenceRectTransform = webviewRect;
            }
        

            webView.SetShowToolbar(true);
            webView.SetBackButtonEnabled(false);
            webView.SetRoundCornerRadius(20f);
            webView.SetSupportMultipleWindows(true);
            webView.SetAllowBackForwardNavigationGestures(true);
            webView.AddUrlScheme("uniwebview");

            webView.OnPageStarted += OnPageStarted;
            webView.OnPageFinished += OnPageFinished;
            webView.OnLoadingErrorReceived += OnPageError;
            webView.OnMessageReceived += OnMessageReceived;
            webView.OnMultipleWindowOpened += OnPopupOpened;
            webView.OnMultipleWindowClosed += OnPopupClosed;
            webView.OnShouldClose += CleanupWebView;

            webView.SetSupportMultipleWindows(true, true);
            webView.SetAcceptThirdPartyCookies(true);

            backObj.SetActive(true);


            string url = null;
            if (isQA)
            {
                url = QAurl + $"{LoginData.Cloud.loginValue.userAutoToken}";
            }
            else
            {
                url = InfoUrl;
            }

            Debug.LogError(url);

            webView.Load(url);
            webView.Show();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (webView == null) return;

                if (webView.CanGoBack)
                {
                    webView.GoBack();
                }
                else
                {
                    CleanupWebView(webView);
                }
            }
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

        private bool CleanupWebView(UniWebView _webView)
        {
            if (webView != null)
            {
                webView.OnMessageReceived -= OnMessageReceived;
                webView.OnPageStarted -= OnPageStarted;
                webView.OnPageFinished -= OnPageFinished;
                webView.OnLoadingErrorReceived -= OnPageError;
                webView.OnMessageReceived -= OnMessageReceived;
                webView.OnMultipleWindowOpened -= OnPopupOpened;
                webView.OnMultipleWindowClosed -= OnPopupClosed;
                webView.OnShouldClose -= CleanupWebView;

                webView.CleanCache();

                backObj.SetActive(false);
                webView.Hide();

                Debug.Log($"Login All Success!!! destroy webview!!");
            }

            return true;
        }

        void OnMessageReceived(UniWebView view, UniWebViewMessage msg)
        {
            Debug.Log($"[UniWebView] path: {msg.Path}");
            Debug.Log($"[UniWebView] args: {msg.Args}");

            if (msg.Args.Count > 0)
            {
                foreach (var arg in msg.Args)
                {
                    Debug.Log($"[UniWebView] args: {arg}");
                }
            }


            switch (msg.Path)
            {
                case "close":
                {
                    Debug.Log($"[UniWebView] close path: {msg.Path}");
                    CleanupWebView(webView);
                    break;
                }

                default:
                    Debug.Log($"[UniWebView] Unknown path: {msg.Path}");
                    break;
            }

            //CleanupWebView();
        }

        private void CloseWithFade()
        {
            if (webView == null) return;
            webView.Hide(true, UniWebViewTransitionEdge.Bottom, 0.25f, () => { CleanupWebView(webView); });
        }
    }
}