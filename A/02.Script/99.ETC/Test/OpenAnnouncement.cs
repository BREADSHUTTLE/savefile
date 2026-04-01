using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniWebViewExternal;

namespace CAPYBARA
{
    public class OpenAnnouncement
    {
        UniWebView webView;
        GameObject webviewObject;
        public OpenAnnouncement()
        {
            CPPlayer.Option.OpenAnnouncementWebView += OpenAnnouncementWebView;
            CPPlayer.Option.OpenQAWebView += OpenQAWebView;
        }

        void OpenAnnouncementWebView(bool isPortrait)
        {
            string token = PlayerPrefs.GetString("AccountToken", "");
            string url = StaticData.Wrapper.webviewurl[0].Announcement;
            Debug.Log(StaticData.Wrapper.webviewurl[0].Announcement);
            webviewObject = new GameObject("AnnouncementWebview");
            webView = webviewObject.AddComponent<UniWebView>();

            webView.EmbeddedToolbar.SetPosition(UniWebViewToolbarPosition.Top);
            webView.EmbeddedToolbar.SetDoneButtonText(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Close].StringToLocal);

            Rect safeArea = Screen.safeArea;

            // UniWebView는 (left, top, width, height) 순서임
            // 하지만 좌표계가 달라서 Y좌표 조정 필요(유니티는 아래서부터, UniWebView는 위에서부터)
            float top = Screen.height - safeArea.y - safeArea.height;

            webView.Frame = new Rect(
                safeArea.x,            // left
                top,                   // top (Screen 기준에서 변환)
                safeArea.width,        // width
                safeArea.height        // height
            );

            webView.SetContentInsetAdjustmentBehavior(UniWebViewContentInsetAdjustmentBehavior.Never);

#if UNITY_IOS
        webView.SetUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1");
#elif UNITY_ANDROID
            webView.SetUserAgent("Mozilla/5.0 (Linux; Android 13; Pixel 6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36");
#endif
            webView.SetSupportMultipleWindows(true, true);
            webView.SetAcceptThirdPartyCookies(true);


            webView.OnMessageReceived += (view, message) =>
            {
                Debug.Log("[OnMessageReceived] " + message.RawMessage);
                if (message.Path == "close")
                {
                    webView.Hide();
                    webView.CleanCache();
                    CPPlayer.Option.SafeAreaActive?.Invoke(false);

                }
            };

            webView.Load(url, false, null);
            webView.Show(true, UniWebViewTransitionEdge.None, 0.0f, () =>
            {
                Debug.Log("[OpenWebView] WebView shown");
            });

            webView.OnShouldClose += view =>
            {
                CPPlayer.Option.SafeAreaActive?.Invoke(false);
                return true;
            };
        }

        private void OpenQAWebView(bool isPortrait)
        {
            string token = PlayerPrefs.GetString("AccountToken", "");
            string url = StaticData.Wrapper.webviewurl[0].QAUrl;
            Debug.Log(StaticData.Wrapper.webviewurl[0].QAUrl);
            webviewObject = new GameObject("AnnouncementWebview");
            webView = webviewObject.AddComponent<UniWebView>();

            webView.EmbeddedToolbar.SetPosition(UniWebViewToolbarPosition.Top);
            webView.EmbeddedToolbar.SetDoneButtonText(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Close].StringToLocal);

            Rect safeArea = Screen.safeArea;

            // UniWebView는 (left, top, width, height) 순서임
            // 하지만 좌표계가 달라서 Y좌표 조정 필요(유니티는 아래서부터, UniWebView는 위에서부터)
            float top = Screen.height - safeArea.y - safeArea.height;

            webView.Frame = new Rect(
                safeArea.x,            // left
                top,                   // top (Screen 기준에서 변환)
                safeArea.width,        // width
                safeArea.height        // height
            );

            webView.SetContentInsetAdjustmentBehavior(UniWebViewContentInsetAdjustmentBehavior.Never);

#if UNITY_IOS
        webView.SetUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1");
#elif UNITY_ANDROID
            webView.SetUserAgent("Mozilla/5.0 (Linux; Android 13; Pixel 6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36");
#endif
            webView.SetSupportMultipleWindows(true, true);
            webView.SetAcceptThirdPartyCookies(true);


            webView.OnMessageReceived += (view, message) =>
            {
                Debug.Log("[OnMessageReceived] " + message.RawMessage);
                if (message.Path == "close")
                {
                    webView.Hide();
                    webView.CleanCache();
                    CPPlayer.Option.SafeAreaActive?.Invoke(false);
                }
            };

            webView.Load(url, false, null);
            webView.Show(true, UniWebViewTransitionEdge.None, 0.0f, () =>
            {
                Debug.Log("[OpenWebView] WebView shown");
            });

            webView.OnShouldClose += view =>
            {
                CPPlayer.Option.SafeAreaActive?.Invoke(false);
                return true;
            };

            webView.OnPageFinished += (view, statusCode, url) =>
    {
        Debug.Log($"페이지 로드 완료: {url} (status: {statusCode})");

        // 페이지 로드가 성공한 경우만 실행
        if (statusCode == 200)
        {
            // string fbid = CPPlayer.UserInfo.currentFBid ?? "";
            // string js = $"OnFirebaseIdMsg('{fbid}')";
            //
            // webView.EvaluateJavaScript(js, payload =>
            // {
            //     if (payload.resultCode == "0")
            //         Debug.Log($"JS 실행 성공, 반환값: {payload.data}");
            //     else
            //         Debug.LogError($"JS 실행 실패: {payload.resultCode}");
            // });
        }
    };
        }

        private async UniTask DeleteWebView()
        {
            await UniTask.Delay(200);
            webView.CleanCache();
            UnityEngine.Object.Destroy(webviewObject);
        }
    }
}
