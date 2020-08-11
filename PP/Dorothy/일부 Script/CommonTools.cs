// CommonTools.cs - CommonTools implementation file
//
// Description      : CommonTools main instance
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/03/07
// Last Update      : 2019/07/31
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Dorothy.DataPool;

#if NETFX_CORE
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
#endif


namespace Dorothy
{
    public enum MoneyCurrency
    {
        None = 0,
        Won,
        Dollar,
        Yen,
    }

    public enum MoneyType
    {
        OneUnit = 0,
        Normal,
        FullMoney,
    }

    public static class CommonTools
    {
//        public static string Clipboard
//        {
//            get
//            {
//#if UNITY_EDITOR
//                return GUIUtility.systemCopyBuffer;
//#elif UNITY_STANDALONE_WIN
//                return GUIUtility.systemCopyBuffer;
//#elif UNITY_WSA
//                return NemoWSAPlugin.CommonTools.GetClipboardText();
//#else
//                return UniPasteBoard.GetClipBoardString();
//#endif
//            }
//            set
//            {
//#if UNITY_EDITOR
//                GUIUtility.systemCopyBuffer = value;
//#elif UNITY_STANDALONE_WIN
//                GUIUtility.systemCopyBuffer = value;
//#elif UNITY_WSA
//                NemoWSAPlugin.CommonTools.SetClipboardText(text);
//#else
//                UniPasteBoard.SetClipBoardString(value);
//#endif
//            }
//        }

        public static bool NeedToUpgrade()
        {
#if UNITY_EDITOR
            return false;
#else
            string config = ConfigInfo.Instance.Find("appInfo.minReqVersion");
            if (string.IsNullOrEmpty(config))
                return false;

            string[] minReqVersion = config.Split('.');
            string[] version = Environment.Instance.Version.Split('.');
            int version0 = Int32.Parse(version[0]);
            int version1 = Int32.Parse(version[1]);
            int version2 = Int32.Parse(version[2]);
            int min_req_version0 = Int32.Parse(minReqVersion[0]);
            int min_req_version1 = Int32.Parse(minReqVersion[1]);
            int min_req_version2 = Int32.Parse(minReqVersion[2]);

            return
            !(
                (min_req_version0 == version0 && min_req_version1 == version1 && min_req_version2 == version2) ||
                (min_req_version0 < version0) ||
                (min_req_version1 < version1) ||
                (min_req_version2 < version2)
            );
#endif
        }

        public static string AddressByHostName(string hostName)
        {
            string ipAddress = string.Empty;
#if NETFX_CORE
            var thread = ResolveDNS(hostName);
            thread.Wait();
            ipAddress = thread.Result;
#else
            System.Net.IPAddress[] ip = System.Net.Dns.GetHostAddresses(hostName);
            if (ip.Length > 0)
                ipAddress = ip[0].ToString();
#endif
            return ipAddress;
        }

#if NETFX_CORE
        internal static async Task<string> ResolveDNS(string hostName)
        {
            try
            {
                IReadOnlyList<EndpointPair> data = await DatagramSocket.GetEndpointPairsAsync(new HostName(hostName), "0");
                if (data != null && data.Count > 0)
                {
                    foreach (EndpointPair item in data)
                    {
                        if (item != null && item.RemoteHostName != null && item.RemoteHostName.Type == HostNameType.Ipv4)
                        {
                            return item.RemoteHostName.CanonicalName;
                        }
                    }
                }
            } 
            catch (Exception)
            {
            }
            return "";
        }
#endif

        public static string LocalIP()
        {
            string ipAddress = string.Empty;
#if NETFX_CORE
            var icp = NetworkInformation.GetInternetConnectionProfile();
            if (icp != null && icp.NetworkAdapter != null)
            {
                var hostNames = NetworkInformation.GetHostNames();
                if (hostNames != null && hostNames.Count > 0)
                {
                    foreach (var host in hostNames)
                    {
                        if (host.IPInformation != null && 
                            host.IPInformation.NetworkAdapter != null && 
                            host.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId)
                        {
                            ipAddress = host.CanonicalName;
                        }
                    }
                }
            }
#elif UNITY_WEBGL
            ipAddress = "127.0.0.1";
#else
            ipAddress = NetworkManager.singleton.networkAddress;
#endif
            return ipAddress;
        }

        public static uint IP2UINT(string ipAddress)
        {
#if NETFX_CORE
            uint ip = 0;
            string[] units = ipAddress.Split('.');
            if (units.Length == 4)
            {
                int shifts = 0;
                foreach (string unit in units)
                {
                    ip |= ((uint)Convert.ToByte(unit) << shifts);
                    shifts += 8;
                }
            }
            return ip;
#else
            return BitConverter.ToUInt32(System.Net.IPAddress.Parse(ipAddress).GetAddressBytes(), 0);
#endif
        }

        public static bool GetActive(GameObject go)
        {
            return (go != null && go.activeInHierarchy == true);
        }

        public static bool GetActive(MonoBehaviour mono)
        {
            if (mono == null)
                return false;

            return GetActive(mono.gameObject);
        }

        public static void SetActive(GameObject[] gos, bool value)
        {
            foreach (GameObject go in gos)
                SetActive(go, value);
        }

        public static void SetActive(GameObject go, bool value)
        {
            if (go == null)
                return;

            if (value == true)
            {
                if (go.activeSelf == false)
                    go.SetActive(true);
            }
            else
            {
                if (go.activeSelf == true)
                    go.SetActive(false);
            }
        }
        
        public static void SetActive(MonoBehaviour mono, bool value)
        {
            if (mono == null)
                return;

            SetActive(mono.gameObject, value);
        }

        public static void SetActive(MonoBehaviour[] mbs, bool value)
        {
            foreach (MonoBehaviour mb in mbs)
                SetActive(mb, value);
        }

        public static void SetActive(Behaviour behaviour, bool value)
        {
            if (behaviour == null)
                return;

            SetActive(behaviour.gameObject, value);
        }

        public static void SetActive(Component component, bool value)
        {
            if (component == null)
                return;

            SetActive(component.gameObject, value);
        }

        public static void SetActive(Component[] components, bool value)
        {
            foreach (Component component in components)
                SetActive(component, value);
        }

        public static bool GetEnable(GameObject go)
        {
            if (go == null)
                return false;

            if (go.GetComponent<Collider>() == null)
                return false;

            return go.GetComponent<Collider>().enabled;
        }

        public static float PlayAnimation(Animation component, float speed = 1f)
        {
            if (component == null)
                return 0;

            SetActive(component, true);
            component.Stop();
            component[component.clip.name].speed = speed;
            component.Play();
            return component.clip.length;
        }

        public static float PlayAnimation(Animation[] components, float speed = 1f)
        {
            float length = 0f;

            foreach (Animation component in components)
                length = PlayAnimation(component, speed);

            return length;
        }
        
        public static float PlayAnimation(Animation component, string name, float speed = 1f)
        {
            if (component == null)
                return 0f;

            AnimationClip clip = component.GetClip(name);
            if (clip == null)
                return 0f;

            SetActive(component, true);
            component.Stop();

            if (speed < 0)
                component[name].time = clip.length;

            component[name].speed = speed;
            component.Play(name);

            return clip.length;
        }
        
        public static void StopAnimation(Animation component)
        {
            if (component != null)
                component.Stop();
        }

        public static float PlayAnimation(Animator component, string name, float speed = 1f)
        {
            if (component == null)
                return 0;

            SetActive(component, true);
#if UNITY_2017_1_OR_NEWER
            component.enabled = false;
#else
            component.Stop();
#endif
            component.speed = speed;
            component.Play(name);

            AnimationClip[] animationClips = component.runtimeAnimatorController.animationClips;
            foreach (AnimationClip clip in animationClips)
            {
                if (clip.name == name)
                    return clip.length;
            }

            return 0f;
        }

        public static float BlendLeaner(float oldValue, float newValue, float startTime, float time)
        {
            if (time <= 0.0f)
                return oldValue;

            float pow = (Time.time - startTime) / time;
            if (pow > 1.0f)
                return newValue;

            return Mathf.Lerp(oldValue, newValue, pow);
        }

        public static Int64 BlendLeaner(Int64 oldValue, Int64 newValue, float startTime, float time)
        {
            if (time <= 0.0f)
                return oldValue;

            float pow = (Time.time - startTime) / time;
            if (pow > 1.0f)
                return newValue;

            return (Int64)Mathf.Lerp(oldValue, newValue, pow);
        }

        public static Vector3 BlendLeaner(Vector3 currentValue, Vector3 newValue, float pow)
        {
            Vector3 add = (newValue - currentValue) * Time.deltaTime * pow;
            if ((newValue - currentValue).magnitude < add.magnitude)
                currentValue = newValue;
            else
                currentValue += add;

            return currentValue;
        }

        public static DateTime UnixTime(double unixTimeStamp)
        {
            DateTime time = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            time = time.AddSeconds(unixTimeStamp).ToLocalTime();
            return time;
        }

        public static DateTime JavaTime(double javaTimeStamp)
        {
            DateTime time = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            time = time.AddMilliseconds(javaTimeStamp).ToLocalTime();
            return time;
        }

        public static DateTime UTCToLocalTime(long time)
        {
            return UTCToLocalTime(time.ToString());
        }

        public static DateTime UTCToLocalTime(string time)
        {
            return DateTime.ParseExact(time, "yyyyMMddHHmmss", CultureInfo.InvariantCulture).ToLocalTime();
        }

        public static DateTime UTCToDateTime(long time)
        {
            return UTCToDateTime(time.ToString());
        }

        public static DateTime UTCToDateTime(string time)
        {
            return (string.IsNullOrEmpty(time) || time.Length != 14) ? 
                DateTime.MinValue : DateTime.ParseExact(time, "yyyyMMddHHmmss", CultureInfo.CurrentCulture);
        }

        public static DateTime LongToDateTime(long time)
        {
            return string.IsNullOrEmpty(time.ToString()) ? DateTime.MinValue : DateTime.ParseExact(time.ToString(), "yyyyMMddHHmmss", CultureInfo.CurrentCulture);
        }

        public static string TimeToString(DateTime time)
        {
            return time.ToString("yyyy.MM.dd HH:mm", CultureInfo.InvariantCulture);
        }

        public static string TimeToStringLine(DateTime time)
        {
            return time.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static string TimeToStringSlash(DateTime time)
        {
            return time.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }

        public static long TimeToLong(DateTime time)
        {
            return Int64.Parse(time.ToString("yyyyMMddHHmmss"));
        }

        public static DateTime ToLocalTime(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Unspecified:
                    return new DateTime(value.Ticks, DateTimeKind.Local);

                case DateTimeKind.Utc:
                    return value.ToLocalTime();

                case DateTimeKind.Local:
                    return value;
            }
            return value;
        }

        public static DateTime ToUtcTime(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Unspecified:
                    return new DateTime(value.Ticks, DateTimeKind.Utc);

                case DateTimeKind.Utc:
                    return value;

                case DateTimeKind.Local:
                    return value.ToUniversalTime();
            }
            return value;
        }

        public static DateTime ToPSTTime(DateTime value)
        {
            return ToUtcTime(value).AddHours(-8);
        }

        public static bool IsNextDay(DateTime current, DateTime from)
        {
            from = from.AddDays(1f);
            from = new DateTime(from.Year, from.Month, from.Day, 0, 0, 0);
            TimeSpan timeSpan = current - from;
            return (timeSpan.TotalSeconds > 0f);
        }

        public static bool IsNext24Hour(DateTime current, DateTime from)
        {
            from = from.AddDays(1f);
            TimeSpan timeSpan = current - from;
            return (timeSpan.TotalSeconds > 0f);
        }

        public static string DisplayDefaultTime(TimeSpan dateTime, bool milliseconds = false)
        {
            string displayDate = string.Empty;

            if (dateTime.Days > 0)
            {
                displayDate = string.Format("{0:D1}{1}",
                    dateTime.Days, LocalizationSystem.Instance.Localize("COMMON.Days"));
            }
            else
            {
                if (milliseconds)
                {
                    displayDate = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}",
                        dateTime.Hours,
                        dateTime.Minutes,
                        dateTime.Seconds,
                        dateTime.Milliseconds);
                }
                else
                {
                    displayDate = string.Format("{0:D2}:{1:D2}:{2:D2}",
                        dateTime.Hours,
                        dateTime.Minutes,
                        dateTime.Seconds);
                }
            }

            return displayDate;
        }

        public static string DisplayShortTime(TimeSpan dateTime, bool second = false)
        {
            string displayDate = string.Empty;

            if (dateTime.Days > 0)
            {
                displayDate = string.Format("{0}{1}",
                    dateTime.Days, LocalizationSystem.Instance.Localize("COMMON.Days"));
            }
            else
            {
                if (dateTime.Hours > 0)
                {
                    displayDate = string.Format("{0}{1} {2}{3}",
                        dateTime.Hours, LocalizationSystem.Instance.Localize("COMMON.Hours"),
                        dateTime.Minutes, LocalizationSystem.Instance.Localize("COMMON.Minutes"));
                }
                else
                {
                    if (second == false || dateTime.Minutes > 0)
                    {
                        displayDate = string.Format("{0}{1}",
                            dateTime.Minutes, LocalizationSystem.Instance.Localize("COMMON.Minutes"));
                    }
                    else
                    {
                        if (dateTime.Seconds >= 0)
                        {
                            displayDate = string.Format("{0}{1}",
                                 dateTime.Seconds, LocalizationSystem.Instance.Localize("COMMON.Seconds"));
                        }
                        else
                        {
                            displayDate = string.Format("0{0}",
                                LocalizationSystem.Instance.Localize("COMMON.Seconds"));
                        }
                    }
                }
            }

            return displayDate;
        }

        public static string DisplayLongTime(TimeSpan dateTime, bool second = false)
        {
            string displayDate = string.Empty;

            if (dateTime.Days > 0)
            {
                displayDate = string.Format("{0}{1} {2}{3} {4}{5}",
                    dateTime.Days, LocalizationSystem.Instance.Localize("COMMON.Days"),
                    dateTime.Hours, LocalizationSystem.Instance.Localize("COMMON.Hours"),
                    dateTime.Minutes, LocalizationSystem.Instance.Localize("COMMON.Minutes"));
            }
            else
            {
                if (dateTime.Hours > 0)
                {
                    displayDate = string.Format("{0}{1} {2}{3}",
                        dateTime.Hours, LocalizationSystem.Instance.Localize("COMMON.Hours"),
                        dateTime.Minutes, LocalizationSystem.Instance.Localize("COMMON.Minutes"));
                }
                else
                {
                    if (second)
                    {
                        displayDate = string.Format("{0}{1} {2}{3}",
                            dateTime.Minutes, LocalizationSystem.Instance.Localize("COMMON.Minutes"),
                            dateTime.Seconds, LocalizationSystem.Instance.Localize("COMMON.Seconds"));
                    }
                    else
                    {
                        displayDate = string.Format("{0}{1}",
                            dateTime.Minutes, LocalizationSystem.Instance.Localize("COMMON.Minutes"));
                    }
                }
            }

            return displayDate;
        }

        public static string DisplayDigitalTime(TimeSpan dateTime)
        {
            return (dateTime.Days * 24) + dateTime.Hours + ":" + dateTime.Minutes.ToString("D2") + ":" + dateTime.Seconds.ToString("D2");
        }

        public static string DisplayDigitalTimeDay(TimeSpan dateTime)
        {
            if (dateTime.Days == 1)
            {
                return string.Format("{0} DAY", dateTime.Days);
            }
            else
            {
                if (dateTime.Days > 0)
                {
                    return string.Format("{0} DAYS", dateTime.Days);
                }
                else
                {
                    if (dateTime.TotalHours > 0)
                    {
                        return dateTime.Hours + ":" + dateTime.Minutes.ToString("D2") + ":" + dateTime.Seconds.ToString("D2");
                    }
                    else
                    {
                        return dateTime.Minutes.ToString("D2") + ":" + dateTime.Seconds.ToString("D2");
                    }
                }
            }
        }

        public static string GetEffectPeriodTime(long period)
        {
            string time = string.Empty;

            int hr = (int)period / 60;
            int min = (int)period % 60;

            if (hr <= 0)
            {
                time = period + " min";
            }
            else
            {
                if (min > 0)
                {
                    time += hr > 1 ? hr + " hrs" : hr + " hr";
                    time += " " + min + " min";
                }
                else
                {
                    time += hr > 1 ? hr + " hrs" : hr + " hr";
                }
            }

            Debug.Log(time);
            return time;
        }

        //TODO:: PST 16시
        public static double PushMessageTime()
        {
            // 현재시간
            DateTime serverTime = CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
            // 4시 체크
            DateTime tempTime = new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, 16, 0, 0);

            TimeSpan tempSpan = tempTime - serverTime;
            // 현재 시간이 오후 4시가 안됨
            if (tempSpan >= TimeSpan.Zero)
            {
                return tempSpan.TotalSeconds;
            }
            // 현재 시간이 오후 4시가 지났음
            else
            {
                tempTime = tempTime.AddDays(1);
                TimeSpan tempSpan2 = tempTime - serverTime;
                return tempSpan2.TotalSeconds;
            }
        }

        public static string GetSystemLanguage()
        {
            return Application.systemLanguage.ToString(); 
        }

        public static void OpenApp(string packageName, string installCode)
        {
#if UNITY_EDITOR
            Application.OpenURL(packageName);
#elif UNITY_ANDROID
            using (AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager"))
                    {
                        AndroidJavaObject intent = null;
                        try
                        {
                            intent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", packageName);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.ToString());
                        }

                        if (intent == null)
                        {
                            Application.OpenURL("market://details?id=" + packageName + "&referrer=" + installCode);
                        }
                        else
                        {
                            ca.Call("startActivity", intent);
                        }
                    }
                }
            }
#else
            Application.OpenURL(packageName);
#endif
        }

        public static void OpenURL(string launchURL, string installCode)
        {
#if UNITY_EDITOR
            Application.OpenURL(launchURL);
#elif UNITY_ANDROID
            using (AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
                    if (intent != null)
                    {
                        AndroidJavaClass Uri = new AndroidJavaClass("android.net.Uri");
                        AndroidJavaObject uri = Uri.CallStatic<AndroidJavaObject>("parse", launchURL + "&referrer=" + installCode);
                        intent.Call<AndroidJavaObject>("setPackage", "com.android.vending");
                        intent.Call<AndroidJavaObject>("setData", uri);
                        
                        ca.Call("startActivity", intent);
                    }
                    else
                    {
                        Application.OpenURL("market://details?id=" + launchURL + "&referrer=" + installCode);
                    }
                }
            }
#else
            Application.OpenURL(launchURL + "&referrer=" + installCode);
#endif
        }

        public static bool RestartApp()
        {
#if UNITY_ANDROID
            AndroidJavaObject unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass intent = new AndroidJavaClass("android.app.PendingIntent");
            AndroidJavaObject baseContext = unity.Call<AndroidJavaObject>("getBaseContext");
            AndroidJavaObject intentObj = baseContext.Call<AndroidJavaObject>("getPackageManager").Call<AndroidJavaObject>("getLaunchIntentForPackage", baseContext.Call<string>("getPackageName"));
            AndroidJavaObject context = unity.Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaObject pendingIntent = intent.CallStatic<AndroidJavaObject>("getActivity", context, 123456, intentObj, intent.GetStatic<int>("FLAG_CANCEL_CURRENT"));
            AndroidJavaClass alarmManager = new AndroidJavaClass("android.app.AlarmManager");
            AndroidJavaClass java = new AndroidJavaClass("java.lang.System");
            AndroidJavaObject alarm = unity.Call<AndroidJavaObject>("getSystemService", "alarm");
            long time = java.CallStatic<long>("currentTimeMillis") + 100;
            alarm.Call("set", alarmManager.GetStatic<int>("RTC"), time, pendingIntent);
            java.CallStatic("exit", 0);
            return true;
#else
            return false;
#endif
        }

        public static bool ConvertAPIBool(string v)
        {
            switch (v)
            {
                case "Y":
                case "YES":
                case "Yes":
                case "y":
                case "yes":
                case "T":
                case "TRUE":
                case "True":
                case "t":
                case "true":
                    return true;
            }

            return false;
        }

        public static string GetIntent()
        {
            string val = string.Empty;

#if UNITY_ANDROID
            if (Application.platform.Equals(RuntimePlatform.Android))
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var ca = up.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (AndroidJavaObject intent = ca.Call<AndroidJavaObject>("getIntent"))
                        {
                            val = intent.Call<string>("getStringExtra", "serverInfo");
                        }
                    }
                }
            }
#endif
            return val;
        }

        public static int GetAndroidVersion()
        {
            int val = 0;

#if UNITY_ANDROID
            if (Application.platform.Equals(RuntimePlatform.Android))
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var ca = up.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        using (AndroidJavaObject packageMgr = ca.Call<AndroidJavaObject>("getPackageManager"))
                        {
                            string packageName = ca.Call<string>("getPackageName");
                            AndroidJavaObject packageInfo = packageMgr.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);

                            val = packageInfo.Get<int>("versionCode");
                        }
                    }
                }
            }
#endif
            return val;
        }

        //public static void ResetPopups()
        //{
        //    foreach (PopupBase popup in PopupBaseSystem.Instance.PopupList)
        //    {
        //        if (!popup.IsPopup)
        //            continue;

        //        if (popup is PopupLoading)
        //            continue;

        //        popup.Close(false);
        //    }
        //}

        public static string GetUserKey(string prefix)
        {
            //return prefix + UserInfo.Instance.GSN;
            return prefix;
        }

        public static bool EqualDateTime(DateTime d1, DateTime d2)
        {
            return
            (
                d1.Year == d2.Year &&
                d1.Month == d2.Month &&
                d1.Day == d2.Day &&
                d1.Hour == d2.Hour &&
                d1.Minute == d2.Minute &&
                d1.Second == d2.Second
                // NOTE: DO ignore miliseconds
            );
        }

        public static bool UnityKeyDownBugfix(string key)
        {
            // NOTE: KeyCode.Plus does not work on Unity-5.3.6f1
            return (Input.anyKeyDown && Input.inputString == key);
        }

        public static void DigestObject(Dictionary<string, string> entries, object obj, string parent)
        {
            if (IsType(obj, "System.Collections.ArrayList") == true)
            {
                ArrayList array = (ArrayList)obj;

                for (int i = 0; i < array.Count; ++i)
                {
                    string path = parent + "[" + i.ToString() + "]";
                    string valueString = string.Empty;

                    if (IsTerminal(array[i], ref valueString) == true)
                        entries[path.ToLower()] = valueString;
                    else
                        DigestObject(entries, array[i], path);
                }

                entries[parent.ToLower() + ".count"] = array.Count.ToString();
            }

            if (IsType(obj, "System.Collections.Hashtable") == true)
            {
                foreach (DictionaryEntry entry in (Hashtable)obj)
                {
                    object key = entry.Key;
                    object value = entry.Value;

                    if (IsType(key, "System.String"))
                    {
                        string path = ((string.IsNullOrEmpty(parent) == true) ? string.Empty : parent + ".") + key.ToString();
                        string valueString = string.Empty;

                        if (IsTerminal(value, ref valueString) == true)
                            entries[path.ToLower()] = valueString;
                        else
                            DigestObject(entries, value, path);
                    }
                }
            }
        }

        public static bool IsType(object obj, string type)
        {
            return string.Equals(obj.GetType().ToString(), type, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTerminal(object obj, ref string valueString)
        {
            if (IsType(obj, "System.String") || IsType(obj, "System.Double") || IsType(obj, "System.Int32"))
            {
                if (valueString != null)
                    valueString = obj.ToString();
                return true;
            }

            if (IsType(obj, "System.Boolean"))
            {
                if (valueString != null)
                    valueString = obj.ToString().ToLower();
                return true;
            }

            return false;
        }

        static public GameObject AddChild(GameObject parent)
        {
            GameObject go = new GameObject();

            if (parent != null)
            {
                Transform t = go.transform;
                t.parent = parent.transform;
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                go.layer = parent.layer;
            }
            return go;
        }

        static public GameObject AddChild(GameObject parent, GameObject prefab)
        {
            GameObject go = GameObject.Instantiate(prefab) as GameObject;
            if (go != null && parent != null)
            {
                Transform t = go.transform;
                t.SetParent(parent.transform);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                go.layer = parent.layer;
            }
            return go;
        }

        public static string CutString(string value, int maxChars)
        {
            if (value == null)
                return "";

            StringBuilder cutString = new StringBuilder();
            int length = Convert.ToInt32(Encoding.GetEncoding(949).GetBytes(value).GetLength(0));

            if (length > maxChars)
            {
                int idx = 0;
                char[] charArr = value.ToCharArray();
                for (int i = 0; i < charArr.Length; ++i)
                {
                    if (char.GetUnicodeCategory(charArr[i]) == UnicodeCategory.OtherLetter)
                        idx += 2;
                    else
                        ++idx;

                    if (idx + 3 > maxChars)
                        break;

                    cutString.Append(value.Substring(i, 1));
                }

                cutString.Append("...");
            }
            else
            {
                cutString.Append(value);
            }

            return cutString.ToString();
        }

        public static string MoneyFormat(long money, MoneyType type = MoneyType.Normal, MoneyCurrency currency = MoneyCurrency.Dollar, bool blank = true)
        {
            string result = string.Empty;
            int parseCount = (currency == MoneyCurrency.Won || currency == MoneyCurrency.Yen) ? 4 : 3;

            bool negative = (money < 0);
            if (negative)
                money *= -1;

            result = money.ToString();
            List<string> buckets = new List<string>();
            while (result.Length > 0)
            {
                int index = result.Length - parseCount;
                if (index < 0)
                    index = 0;

                buckets.Add(result.Substring(index));
                result = result.Remove(index);
            }
            buckets.Reverse();

            switch (type)
            {
                case MoneyType.OneUnit:
                    result = buckets[0];
                    if (buckets.Count >= 2)
                        result += LocalizationSystem.Instance.Localize(string.Format("COMMON.Money.Digit{0}", buckets.Count - 2));
                    break;

                case MoneyType.Normal:
                    if (currency == MoneyCurrency.Won || currency == MoneyCurrency.Yen)
                    {
                        for (int i = 0; i < 2; ++i)
                        {
                            if (i >= buckets.Count)
                                break;

                            int sub = 0;
                            int.TryParse(buckets[i], out sub);
                            if (i == 0 || sub > 0)
                            {
                                result += sub.ToString();

                                if (buckets.Count >= (i + 2))
                                    result += LocalizationSystem.Instance.Localize(string.Format("COMMON.Money.Digit{0}", buckets.Count - (i + 2)));

                                if (blank)
                                    result += " ";
                            }
                        }
                        result.TrimEnd();
                    }
                    else
                    {
                        result = buckets[0];
                        if (buckets.Count >= 2)
                        {
                            int sub = 0;
                            int.TryParse(buckets[1], out sub);

                            if (sub > 9)
                            {
                                char[] charsToTrim = { ' ', '0' };
                                result += string.Format(".{0:D2}", sub / 10).TrimEnd(charsToTrim);
                            }

                            result += LocalizationSystem.Instance.Localize(string.Format("COMMON.Money.Digit{0}", buckets.Count - 2));
                        }
                    }
                    break;

                case MoneyType.FullMoney:
                    result = money.ToString("#,##0");
                    break;
            }

            if (negative)
                result = "-" + result;

            return result;
        }

        public static string ToJsonString(object payload)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            StringWriter sw = new StringWriter();
            JsonWriter writer = new JsonTextWriter(sw);
            serializer.Serialize(writer, payload);
            return sw.ToString();
        }

        public static T FromJsonString<T>(string json)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;

            StringReader read = new StringReader(json);
            JsonReader reader = new JsonTextReader(read);
            return serializer.Deserialize<T>(reader);
        }

        /*
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////        
                // TODO: 아래 코드 리팩토링 필요!!! 거지같애...
                public static string MoneyFormat(long money, MoneyType moneyType = MoneyType.Normal, bool useBlank = true, bool useSign = false)
                {
                    return MoneyFormat(money, MoneyCurrency.Won, moneyType, useBlank, useSign);
                }

                public static string MoneyFormatPMCR(long money, bool useSpace = false, bool useCutoff = true)
                {
                    System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
                    long baseUnitFilterValue = 10000;
                    long cashedValue = money;
                    long resultValue;

                    stringBuilder.Remove(0, stringBuilder.Length);

                    // GOS.Yopkigom 2016/01/22 : cashedValue에 1234가 입력되었을 경우 길이는 4 베이스 필터 값은 10000으로 길이는 (5 - 1) :: 결과는 1 이지만 나머지 연산 검증으로 -1 되어 0 처리
                    // 12345일 경우 5 // 1
                    // 123400일 경우 6 // 1
                    // 1234001일 경우 7 // 1
                    // 12340010일 경우 8 // 2 이지만 나머지 연산 검증으로 -1 되어 1 처리
                    // 123400100일 경우 9 // 2 ...
                    int loopCountMax = cashedValue.ToString().Length / (baseUnitFilterValue.ToString().Length - 1);
                    if (0 == cashedValue.ToString().Length % (baseUnitFilterValue.ToString().Length - 1))
                    {
                        loopCountMax--;
                    }

                    // GOS.Yopkigom 2016/01/15 : 네자릿수(천단위) baseUnitFilterValue.ToString().Length - 1
                    if (0 == loopCountMax)
                    {
                        stringBuilder.Insert(0, cashedValue);
                    }
                    else // GOS.Yopkigom 2016/01/15 : 네자릿수(천단위) 이상
                    {
                        // GOS.Yopkigom 2016/01/15 : loopCountMax 만큼 순회하면서 자릿수(baseUnitFilterValue)만큼을 잘라내어(resultValue) 자릿수 명칭(GetNumberUnitWord)과 조합합니다. 
                        for (int index = 0; index <= loopCountMax; index++)
                        {
                            resultValue = cashedValue % baseUnitFilterValue;

                            // GOS.Yopkigom 2016/01/15 : 아래의 조건을 만족하면, 아랫단위 숫자를 축약하여 출력합니다.
                            // 컷오프 옵션 사용 && 두자릿수(1억) 초과 && 상위 두 단위수(1조1억1만1천 -> 1조1억)
                            if (true == useCutoff && 1 < loopCountMax && index < loopCountMax - 1)
                            {
                                cashedValue /= baseUnitFilterValue;

                                continue;
                            }


                            if (0 != resultValue)
                            {
                                if (useSpace)
                                    stringBuilder.Insert(0, " ");
                                stringBuilder.Insert(0, GetNumberUnitWord(index));
                                stringBuilder.Insert(0, resultValue);
                            }

                            if (baseUnitFilterValue > cashedValue)
                                break;

                            cashedValue /= baseUnitFilterValue;
                        } // End Loop
                    }

                    return stringBuilder.ToString().TrimEnd();
                }

                private static string GetNumberUnitWord(int _value)
                {
                    switch (_value)
                    {
                        case 1: return "만";
                        case 2: return "억";
                        case 3: return "조";
                        case 4: return "경";
                        case 5: return "해";
                    }

                    return string.Empty;
                }

                // GOS.Yopkigom 2016/01/28 Comment : 기존에 있던 MoneyFormat은 세자리 단위의 숫자 표기 형식(123,456,789)이기 때문에
                // 이를 대한민국의 셈 방식인 네자리 단위(1억2345만6789)에 대응하기에는 너무 많은 참조를 수정하여야 합니다.(약 110여개)
                // 따라서 별도로 구성한 MoneyFormatPMCR을 기본적으로 사용하도록 루틴을 설정하되,
                // 이상 동작이 확인된 곳에서는 예전 루틴을 따르도록 usePMCRRoutine 변수를 추가(Default Value : true)하여 대응합니다.
                // true : 네자리 단위 방식으로 동작합니다.
                // false : 세자리 단위 방식으로 동작합니다.
                // GOS.Yopkigom 2016/03/02 추가 : usePMCRCutoff 소지금의 상위 두자리만을 기본적으로 출력(Default Value : true)하도록 합니다.
                // true : 상위 두 단위만 출력합니다. (ex> 123,456,789,000 -> 1234억 5678만)
                // false : 모든 단위를 출력합니다. (ex> 123,456,789,000 -> 1234억 5678만 9000)
                public static string MoneyFormat(long money, MoneyCurrency moneyCurrency, MoneyType moneyType = MoneyType.Normal, bool useBlank = true, bool useSign = false, bool usePMCRRoutine = true, bool usePMCRCutoff = true)
                {
                    if (true == usePMCRRoutine)
                    {
                        // GOS.JJIH 2016/01/28 참조가 많이 일단 이렇게..
                        return MoneyFormatPMCR(money, usePMCRCutoff);
                    }

                    string moneyString = "";
                    string[] units = { "K", "M", "B", "T", "Q", "Q", "S" };

                    // Negative
                    bool isNegative = (money < 0);
                    if (isNegative)
                        money *= -1;

                    // Parse
                    int parseCount = 3;
                    if (moneyCurrency == MoneyCurrency.Won || moneyCurrency == MoneyCurrency.Yen)
                        parseCount = 4;

                    moneyString = money.ToString();
                    List<string> moneySplits = new List<string>();
                    while (moneyString.Length > 0)
                    {
                        int startIndex = moneyString.Length - parseCount;
                        if (startIndex < 0)
                            startIndex = 0;

                        string temp = moneyString.Substring(startIndex);
                        moneySplits.Add(temp);

                        moneyString = moneyString.Remove(startIndex);
                    }
                    moneySplits.Reverse();

                    Debug.Assert(moneySplits.Count > 0);

                    // Type
                    switch (moneyType)
                    {
                        case MoneyType.OneUnit:
                            moneyString = moneySplits[0];
                            if (moneySplits.Count >= 2)
                                moneyString += units[moneySplits.Count - 2];
                            break;

                        case MoneyType.Normal:
                            if (moneyCurrency == MoneyCurrency.Won || moneyCurrency == MoneyCurrency.Yen)
                            {
                                for (int i = 0; i < 2; ++i)
                                {
                                    if (i >= moneySplits.Count)
                                        break;

                                    int submoney = 0;
                                    int.TryParse(moneySplits[i], out submoney);
                                    if (i == 0 || submoney > 0)
                                    {
                                        moneyString += submoney.ToString();

                                        if (moneySplits.Count >= (i + 2))
                                            moneyString += units[moneySplits.Count - (i + 2)];

                                        if (useBlank)
                                            moneyString += " ";
                                    }
                                }
                                moneyString.TrimEnd();
                            }
                            else
                            {
                                moneyString = moneySplits[0];

                                if (moneySplits.Count >= 2)
                                {
                                    int submoney = 0;
                                    int.TryParse(moneySplits[1], out submoney);

                                    if (submoney > 9)
                                    {
                                        char[] charsToTrim = { ' ', '0' };
                                        moneyString += string.Format(".{0:D2}", submoney / 10).TrimEnd(charsToTrim);
                                    }

                                    moneyString += units[moneySplits.Count - 2];
                                }

                            }
                            break;

                        case MoneyType.FullMoney:
                            moneyString = money.ToString("#,##0");
                            break;
                    }

                    // Currency
                    switch (moneyCurrency)
                    {
                        case MoneyCurrency.Won:
                            moneyString = "￦" + moneyString;
                            break;

                        case MoneyCurrency.Dollar:
                            moneyString = "＄" + moneyString;
                            break;

                        case MoneyCurrency.Yen:
                            moneyString = "￥" + moneyString;
                            break;

                        default:
                            break;
                    }

                    if (isNegative)
                        moneyString = "-" + moneyString;
                    else if (useSign)
                        moneyString = "+" + moneyString;

                    return moneyString;
                }

                // 만,억,조로 표현하는 머니표기 함수
                // unitCount:
                // 1억 2000만 3000 에서 unitCount가 0일 때 모두 출력
                // 1일 때 1억, 2일 때 1억 2000만, 3일 때, 1억 2000만 3000 
                public static string MoneyFormatKorean(long number, int unitCount, bool useSpace)
                {
                    string numberString = number.ToString();
                    if (numberString.Length <= 4)
                        return numberString;

                    int length = numberString.Length;

                    List<string> numberByUnit = new List<string>();

                    int pivot = length - 4;
                    for (int i = 0; i < length; i += 4)
                    {
                        int count = pivot < 1 ? 4 + pivot : 4;

                        pivot = pivot < 1 ? 0 : pivot;

                        numberByUnit.Add(numberString.Substring(pivot, count));

                        pivot -= 4;
                    }

                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    string[] units = { "", "만", "억", "조", "경" };

                    if (unitCount > numberByUnit.Count)
                        unitCount = numberByUnit.Count;

                    int unitCountIndex = 0;// unitCount - 1;

                    for (int i = numberByUnit.Count - 1; i >= 0; --i)
                    {
                        int tmpInteger = Int32.Parse(numberByUnit[i]);
                        if (tmpInteger == 0)
                            continue;

                        sb.Append(tmpInteger.ToString());
                        sb.Append(units[i]);

                        //if (i == unitCountIndex)
                        //    break;
                        ++unitCountIndex;
                        if (unitCountIndex == unitCount || unitCountIndex == numberByUnit.Count)
                            break;

                        bool notFirst = sb.Length > 0;
                        if (notFirst && i != 0)
                            sb.Append(" ");
                    }

                    string result = sb.ToString();

                    if (result.EndsWith(" "))
                        return result.Remove(result.Length - 1);
                    else
                        return result;
                }

                // 블랙잭, 바카라를 위한 7글자 제한 버전
                public static string MoneyFormatKoreanLetter7(long number)//, int unitCount, bool useSpace)
                {
                    string output = MoneyFormatKorean(number, 2, false);
                    if (output.Length > 7)
                    {
                        return MoneyFormatKorean(number, 1, false);

                    }
                    else
                        return output;
                }

                public static string CashFormat(double money, MoneyCurrency moneyCurrency)
                {
                    string moneyString = "";

                    // Negative
                    bool isNegative = (money < 0);
                    if (isNegative)
                        money *= -1;

                    // Parse
                    moneyString = money.ToString();

                    // Currency
                    switch (moneyCurrency)
                    {
                        case MoneyCurrency.Won:
                            moneyString = "￦" + moneyString;
                            break;

                        case MoneyCurrency.Dollar:
                            moneyString = "＄" + moneyString;
                            break;

                        case MoneyCurrency.Yen:
                            moneyString = "￥" + moneyString;
                            break;

                        default:
                            break;
                    }

                    if (isNegative)
                        moneyString = "-" + moneyString;

                    return moneyString;
                }*/
    }
}
