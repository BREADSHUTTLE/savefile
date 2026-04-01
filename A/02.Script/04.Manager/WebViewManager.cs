using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class WebViewManager : MonoSingleton<WebViewManager>
    {
        [HideInInspector]public GameObject webViewObject;
        [HideInInspector]public UniWebView webView;
        protected override void Init()
        {
            base.Init();
            if (webViewObject == null)
                webViewObject = new GameObject("WebViewObject_Inform");
            webView = webViewObject.GetComponent<UniWebView>();
            if (webView == null)
            {
                webView = webViewObject.AddComponent<UniWebView>();
            }

            webView.Hide();
        }
    }

}
