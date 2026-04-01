using UnityEngine;

public class AndroidLogger
{
    public static void Log(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (AndroidJavaClass log = new AndroidJavaClass("android.util.Log"))
                {
                    log.CallStatic<int>("d", "UnityDebug", message);
                }
            }
        }
#else
        Debug.Log(message);
#endif
    }
}
