using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

public static class DeviceInfoCollector
{
    #region iOS Native Plugin
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string _GetIDFA();

    [DllImport("__Internal")]
    private static extern void _RequestTrackingAuthorization();

    [DllImport("__Internal")]
    private static extern string _GetBSSID();

    [DllImport("__Internal")]
    private static extern string _GetSSID();
#endif
    #endregion

    #region 기본 정보

    // OS 정보
    public static string GetOS()
    {
        return SystemInfo.operatingSystem;
    }

    // 플랫폼
    public static string GetPlatform()
    {
        return Application.platform.ToString();
    }
    public static int GetPlatformtoInt()
    {
        return (int)Application.platform;
    }
    // 제조사
    public static string GetManufacturer()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass build = new AndroidJavaClass("android.os.Build"))
            {
                return build.GetStatic<string>("MANUFACTURER");
            }
        }
        catch (Exception e)
        {
            return "Unknown";
        }
#elif UNITY_IOS && !UNITY_EDITOR
        return "Apple";
#else
        return "Unknown";
#endif
    }

    // 모델명
    public static string GetModel()
    {
        return SystemInfo.deviceModel;
    }

    #endregion

    #region 광고 식별자 (ADID/IDFA)

    // 광고 식별자
    public static string GetAdvertisingId()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return GetAndroidAdvertisingId();
#elif UNITY_IOS && !UNITY_EDITOR
        return GetIOSAdvertisingId();
#else
        return "editor-test-adid-" + SystemInfo.deviceUniqueIdentifier.Substring(0, 8);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static string GetAndroidAdvertisingId()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
            using (AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", activity))
            {
                return adInfo.Call<string>("getId");
            }
        }
        catch (Exception e)
        {
            return "";
        }
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    private static string GetIOSAdvertisingId()
    {
        try
        {
            return _GetIDFA();
        }
        catch (Exception e)
        {
            return "";
        }
    }
#endif

    // iOS ATT 권한 요청
    // iOS 14+ 에서 IDFA 접근 전 반드시 호출 필요
    public static void RequestTrackingAuthorization()
    {
#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            _RequestTrackingAuthorization();
        }
        catch (Exception e)
        {
            Debug.LogError($"RequestTrackingAuthorization 실패: {e.Message}");
        }
#endif
    }

    #endregion

    #region UUID/GUID

    private const string UUID_KEY = "DEVICE_APP_UUID";

    // 앱 설치 시 생성되는 고유값 (UUID/GUID)
    public static string GetAppUUID()
    {
        string uuid = PlayerPrefs.GetString(UUID_KEY, "");

        if (string.IsNullOrEmpty(uuid))
        {
            uuid = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(UUID_KEY, uuid);
            PlayerPrefs.Save();
        }

        return uuid;
    }

    // 기기 고유 식별자
    // Android 10+ 에서는 제한됨, iOS는 벤더별 ID
    public static string GetDeviceUniqueId()
    {
        return SystemInfo.deviceUniqueIdentifier;
    }

    #endregion

    #region SSAID / IDFV

    // Android SSAID (Android ID)
    // 앱 서명키 + 사용자 조합으로 고유, 공장초기화시 변경됨
    public static string GetSSAID()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return GetAndroidSSAID();
#else
        return "";
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static string GetAndroidSSAID()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject contentResolver = activity.Call<AndroidJavaObject>("getContentResolver"))
            using (AndroidJavaClass secure = new AndroidJavaClass("android.provider.Settings$Secure"))
            {
                return secure.CallStatic<string>("getString", contentResolver, "android_id");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"GetAndroidSSAID 실패: {e.Message}");
            return "";
        }
    }
#endif

    // iOS IDFV
    public static string GetIDFV()
    {
#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            return UnityEngine.iOS.Device.vendorIdentifier;
        }
        catch (Exception e)
        {
            Debug.LogError($"GetIDFV 실패: {e.Message}");
            return "";
        }
#else
        return "";
#endif
    }

    public static string GetPlatformDeviceId()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return GetSSAID();
#elif UNITY_IOS && !UNITY_EDITOR
        return GetIDFV();
#else
        return "editor-test-" + SystemInfo.deviceUniqueIdentifier.Substring(0, 8);
#endif
    }

    #endregion

    #region IP 주소

    // 공인 IP 주소
    public static async UniTask<string> GetPublicIPAsync()
    {
        try
        {
            using (UnityWebRequest request = UnityWebRequest.Get("https://api.ipify.org"))
            {
                request.timeout = 5;
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    return "";
                }
            }
        }
        catch (Exception e)
        {
            return "";
        }
    }

    // 로컬 IP 주소
    public static string GetLocalIP()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "";
            }
        }
        catch (Exception e)
        {
            return "";
        }
    }

    #endregion

    #region BSSID (Wi-Fi MAC 주소)

    // Wi-Fi BSSID
    public static string GetBSSID()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return GetAndroidBSSID();
#elif UNITY_IOS && !UNITY_EDITOR
        return GetIOSBSSID();
#else
        return "editor-test-bssid";
#endif
    }

    // Wi-Fi SSID
    public static string GetSSID()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return GetAndroidSSID();
#elif UNITY_IOS && !UNITY_EDITOR
        return GetIOSSSID();
#else
        return "editor-test-ssid";
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static string GetAndroidBSSID()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (AndroidJavaObject wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi"))
            using (AndroidJavaObject wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo"))
            {
                string bssid = wifiInfo.Call<string>("getBSSID");
                return bssid ?? "";
            }
        }
        catch (Exception e)
        {
            return "";
        }
    }

    private static string GetAndroidSSID()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (AndroidJavaObject wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi"))
            using (AndroidJavaObject wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo"))
            {
                string ssid = wifiInfo.Call<string>("getSSID");
                // SSID는 큰따옴표로 감싸져 있을 수 있음
                return ssid?.Trim('"') ?? "";
            }
        }
        catch (Exception e)
        {
            return "";
        }
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    private static string GetIOSBSSID()
    {
        try
        {
            return _GetBSSID();
        }
        catch (Exception e)
        {
            return "";
        }
    }

    private static string GetIOSSSID()
    {
        try
        {
            return _GetSSID();
        }
        catch (Exception e)
        {
            return "";
        }
    }
#endif

    #endregion

    #region 통합 수집
    public static async UniTask<DeviceInfo> CollectAllAsync()
    {
        string publicIP = await GetPublicIPAsync();

        return new DeviceInfo
        {
            OS = GetOS(),
            Platform = GetPlatform(),
            Manufacturer = GetManufacturer(),
            Model = GetModel(),
            AdvertisingId = GetAdvertisingId(),
            AppUUID = GetAppUUID(),
            DeviceUniqueId = GetDeviceUniqueId(),
            SSAID = GetSSAID(),
            IDFV = GetIDFV(),
            PublicIP = publicIP,
            LocalIP = GetLocalIP(),
            BSSID = GetBSSID(),
            SSID = GetSSID(),
            CollectedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public static DeviceInfo CollectBasic()
    {
        return new DeviceInfo
        {
            OS = GetOS(),
            Platform = GetPlatform(),
            Manufacturer = GetManufacturer(),
            Model = GetModel(),
            AdvertisingId = GetAdvertisingId(),
            AppUUID = GetAppUUID(),
            DeviceUniqueId = GetDeviceUniqueId(),
            SSAID = GetSSAID(),
            IDFV = GetIDFV(),
            PublicIP = "",
            LocalIP = GetLocalIP(),
            BSSID = GetBSSID(),
            SSID = GetSSID(),
            CollectedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    #endregion
}

[Serializable]
public class DeviceInfo
{
    public string OS;
    public string Platform;
    public string Manufacturer;
    public string Model;
    public string AdvertisingId;
    public string AppUUID;
    public string DeviceUniqueId;
    public string SSAID;
    public string IDFV;
    public string PublicIP;
    public string LocalIP;
    public string BSSID;
    public string SSID;
    public string CollectedAt;

    public string ToJson(bool prettyPrint = false)
    {
        return JsonUtility.ToJson(this, prettyPrint);
    }

    public override string ToString()
    {
        return $"[DeviceInfo]\n" +
               $"  OS: {OS}\n" +
               $"  Platform: {Platform}\n" +
               $"  Manufacturer: {Manufacturer}\n" +
               $"  Model: {Model}\n" +
               $"  AdvertisingId: {AdvertisingId}\n" +
               $"  AppUUID: {AppUUID}\n" +
               $"  DeviceUniqueId: {DeviceUniqueId}\n" +
               $"  SSAID: {SSAID}\n" +
               $"  IDFV: {IDFV}\n" +
               $"  PublicIP: {PublicIP}\n" +
               $"  LocalIP: {LocalIP}\n" +
               $"  BSSID: {BSSID}\n" +
               $"  SSID: {SSID}\n" +
               $"  CollectedAt: {CollectedAt}";
    }
}

