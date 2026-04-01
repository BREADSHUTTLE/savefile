using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;

namespace CAPYBARA
{
    public class AppsFlyerInitializer : MonoBehaviour, IAppsFlyerConversionData
    {
        [SerializeField] string devKey;
        [SerializeField] string iOSAppId;

        void Awake()
        {
            if (FindObjectsByType<AppsFlyerInitializer>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            AppsFlyer.OnDeepLinkReceived += HandleDeepLink;
        }

        void Start()
        {
            AppsFlyer.setIsDebug(true);
            AppsFlyer.initSDK(devKey, iOSAppId, this);
            AppsFlyer.startSDK();

            CheckLaunchUrl();
        }

        void CheckLaunchUrl()
        {
            string url = Application.absoluteURL;
            if (string.IsNullOrEmpty(url))
                return;

            Debug.Log($"[DeepLink][LaunchURL] {url}");

            if (url.Contains("invite") && url.Contains("code="))
            {
                var uri = new System.Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                string code = query["code"];
                if (!string.IsNullOrEmpty(code) && !DeepLinkData.HasPendingInvite)
                {
                    Debug.Log($"[DeepLink][LaunchURL] Invite code from launch URL: {code}");
                    DeepLinkData.PendingInviteCode = code;
                    DeepLinkData.Source = DeepLinkSource.Direct;
                }
            }
        }

        void HandleDeepLink(object sender, System.EventArgs args)
        {
            var dlArgs = args as DeepLinkEventsArgs;
            if (dlArgs == null)
                return;

            Debug.Log($"[DeepLink][UDL] Status: {dlArgs.status}, Campaign: {dlArgs.getCampaign()}");

            if (dlArgs.status != DeepLinkStatus.FOUND)
                return;

            string deepLinkValue = dlArgs.getDeepLinkValue();
            if (deepLinkValue == "invite")
            {
                string inviteCode = dlArgs.getAfSub1();
                if (!string.IsNullOrEmpty(inviteCode))
                {
                    Debug.Log($"[DeepLink][UDL] Invite code received: {inviteCode}");
                    DeepLinkData.PendingInviteCode = inviteCode;
                    DeepLinkData.Source = DeepLinkSource.Direct;
                }
            }
        }

        public void onConversionDataSuccess(string conversionData)
        {
            Debug.Log($"[DeepLink][ConversionData] Success: {conversionData}");

            if (DeepLinkData.HasPendingInvite)
                return;

            var data = AppsFlyer.CallbackStringToDictionary(conversionData);
            if (data == null)
                return;

            bool isFirstLaunch = data.ContainsKey("is_first_launch")
                && data["is_first_launch"].ToString() == "true";

            string afStatus = data.ContainsKey("af_status")
                ? data["af_status"].ToString() : "";

            if (afStatus != "Non-organic" || !isFirstLaunch)
                return;

            string deepLinkValue = data.ContainsKey("deep_link_value")
                ? data["deep_link_value"].ToString() : "";

            if (deepLinkValue == "invite")
            {
                string inviteCode = data.ContainsKey("af_sub1")
                    ? data["af_sub1"].ToString() : "";

                if (!string.IsNullOrEmpty(inviteCode))
                {
                    Debug.Log($"[DeepLink][Deferred] Invite code from conversion data: {inviteCode}");
                    DeepLinkData.PendingInviteCode = inviteCode;
                    DeepLinkData.Source = DeepLinkSource.Deferred;
                }
            }
        }

        public void onConversionDataFail(string error)
        {
            Debug.LogWarning($"[DeepLink][ConversionData] Failed: {error}");
        }

        public void onAppOpenAttribution(string attributionData)
        {
            Debug.Log($"[DeepLink][Retargeting] Attribution: {attributionData}");

            if (DeepLinkData.HasPendingInvite)
                return;

            var data = AppsFlyer.CallbackStringToDictionary(attributionData);
            if (data == null)
                return;

            string deepLinkValue = data.ContainsKey("deep_link_value")
                ? data["deep_link_value"].ToString() : "";

            if (deepLinkValue == "invite")
            {
                string inviteCode = data.ContainsKey("af_sub1")
                    ? data["af_sub1"].ToString() : "";

                if (!string.IsNullOrEmpty(inviteCode))
                {
                    Debug.Log($"[DeepLink][Retargeting] Invite code: {inviteCode}");
                    DeepLinkData.PendingInviteCode = inviteCode;
                    DeepLinkData.Source = DeepLinkSource.Retargeting;
                }
            }
        }

        public void onAppOpenAttributionFailure(string error)
        {
            Debug.LogWarning($"[DeepLink][Retargeting] Failed: {error}");
        }

        void OnDestroy()
        {
            AppsFlyer.OnDeepLinkReceived -= HandleDeepLink;
        }
    }
}
