// AppsFlyerSystem.cs - AppsFlyerSystem implementation file
//
// Description      : AppsFlyerSystem
// Author           : icoder
// Maintainer       : icoder, uhrain7761
// How to use       : 
// Created          : 2018/08/25
// Last Update      : 2020/06/26
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using System;
using System.Web;
using UnityEngine;
using System.Collections.Generic;
using Dorothy.DataPool;
using Dorothy;

public sealed class AppsFlyerSystem : BaseSystem<AppsFlyerSystem>
{
    private string afListenerCoupon = string.Empty;
    public string AFListenerCoupon
    {
        get { return afListenerCoupon; }
        set { afListenerCoupon = value; }
    }

    protected override void Awake()
    {
        base.Awake();

        Session.Instance.ActionOpen += OnSessionOpen;

        SetupAppsFlyer();
        SetIntent();
    }

    private void OnSessionOpen()
    {
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            AppsFlyer.setCustomerUserID(UserInfo.Instance.GSN.ToString());
    }

    private void SetupAppsFlyer()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            AppsFlyer.setAppsFlyerKey("XoeS6tz5EGf4JGVFHq8aVe");
            AppsFlyer.setAppID("1470058413134770");
            //AppsFlyer.getConversionData();
            AppsFlyer.trackAppLaunch();
        }
        if (Application.platform == RuntimePlatform.Android)
        {
            AppsFlyer.setAppsFlyerKey("XoeS6tz5EGf4JGVFHq8aVe");
            AppsFlyer.setAppID("1470058413134770");
            AppsFlyer.init("XoeS6tz5EGf4JGVFHq8aVe", "AppsFlyerSystem");
        }
    }

    public void LogEvent(string id, Dictionary<string, string> parameters)
    {
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            AppsFlyer.trackRichEvent(id, parameters);
    }

    private void SetIntent()
    {
        string intent = GetIntent();
        if (string.IsNullOrEmpty(intent) == false)
        {
            intent = HttpUtility.UrlDecode(intent);

            string[] result = intent.Split('&');

            for (int i = 0; i < result.Length; i++)
            {
                if (result[i].Contains("af_dp") == true)
                {
                    string link = result[i].Substring(result[i].IndexOf("=") + 1);
                    Debug.Log("link : " + link);

                    Debug.Log("coupon number : " + GetCouponNumber(link));

                    afListenerCoupon = GetCouponNumber(link);

                    break;
                }
            }
        }
    }

    private string GetCouponNumber(string link)
    {
        string coupon = string.Empty;
        Uri uri = new Uri(link);
        
        if (uri.Query.Contains("bonuslink") == true)
            coupon = uri.Query.Replace("?bonuslink/", "");

        return coupon;
    }

    private string GetIntent()
    {
        string data = string.Empty;

#if UNITY_ANDROID
        if (Application.platform.Equals(RuntimePlatform.Android))
        {
            using (var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                Debug.Log("************ activity ************");

                using (var ca = activity.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    Debug.Log("************ currentActivity ************");

                    using (AndroidJavaObject intent = ca.Call<AndroidJavaObject>("getIntent"))
                    {
                        Debug.Log("************ intent ************ :" + intent.Call<string>("getDataString"));
                        data = intent.Call<string>("getDataString");
                    }
                }
            }
        }
#endif
        return data;
    }


    public void didReceiveConversionData(string conversionData)
    {
        Log("Log Test = didReceiveConversionData : " + conversionData);

        if (string.IsNullOrEmpty(conversionData))
            return;

        AppsflyerDeepLinkInfo info = CommonTools.FromJsonString<AppsflyerDeepLinkInfo>(conversionData);

        Debug.Log("Log Test = didReceiveConversionData info dp : " + info.af_dp);

        if (string.IsNullOrEmpty(info.af_dp))
            return;

        bool firstLaunch = info.is_first_launch.ToLower() == "true" ? true : false;

        if (firstLaunch == false)
            return;

        Debug.Log("coupon number : " + GetCouponNumber(info.af_dp));

        Session.Instance.ReceiveLobbyRewardCoupon(GetCouponNumber(info.af_dp), null);
    }

    public void didReceiveConversionDataWithError(string error)
    {
        Log("Log Test = didReceiveConversionDataWithError : " + error);
    }

    public void didFinishValidateReceipt(string validateResult)
    {
        Log("Log Test = didFinishValidateReceipt : " + validateResult);
    }

    public void didFinishValidateReceiptWithError(string error)
    {
        Log("Log Test = didFinishValidateReceiptWithError : " + error);
    }

    public void onAppOpenAttribution(string validateResult)
    {
        //게임 실행중에 받은 result
        Log("Log Test = onAppOpenAttribution : " + validateResult);

        if (string.IsNullOrEmpty(validateResult))
            return;
        
        AppsflyerDeepLinkInfo info = CommonTools.FromJsonString<AppsflyerDeepLinkInfo>(validateResult);

        Debug.Log("Log Test = onAppOpenAttribution info dp : " + info.af_dp);

        if (string.IsNullOrEmpty(info.af_dp))
            return;

        Debug.Log("coupon number : " + GetCouponNumber(info.af_dp));

        Session.Instance.ReceiveLobbyRewardCoupon(GetCouponNumber(info.af_dp), null);
    }

    public void onAppOpenAttributionFailure(string error)
    {
        Log("Log Test = onAppOpenAttributionFailure : " + error);
    }

    public void onInAppBillingSuccess()
    {
        Log("Log Test = onInAppBillingSuccess");

    }
    public void onInAppBillingFailure(string error)
    {
        Log("Log Test = onInAppBillingFailure : " + error);

    }

    public void onInviteLinkGenerated(string link)
    {
        Log("Log Test = onInviteLinkGenerated : " + link);
    }

    public void onOpenStoreLinkGenerated(string link)
    {
        Log("Log Test = onOpenStoreLinkGenerated : " + link);
        Application.OpenURL(link);
    }
}

