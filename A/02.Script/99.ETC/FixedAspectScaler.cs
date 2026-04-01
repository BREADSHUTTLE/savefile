using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using System.Runtime.InteropServices;
using CAPYBARA;

[ExecuteAlways]
public class FixedAspectScaler : TempMonoSingleton<FixedAspectScaler>
{
    public RectTransform scalerWrapper;
    public CanvasScaler canvasScaler;
    public RectTransform canvasRect;

    private const float targetAspect = 19.5f / 9f;

    protected override void Init()
    {
        base.Init();
        UpdateScaler();
        TouchScreenKeyboard.hideInput = true;

#if UNITY_IOS && !UNITY_EDITOR
    _RegisterKeyboardObserver();
#endif
    }

    void Update()
    {
        if (!portraitMode)
        {
            UpdateScaler(); // 에디터에서도 실시간 갱신
        }

        else
        {
            ChatBoxSet();
        }
    }

    void UpdateScaler()
    {
        if (scalerWrapper == null || scalerWrapper.parent == null || canvasScaler == null) return;

        RectTransform parentRect = scalerWrapper.parent.GetComponent<RectTransform>();
        Vector2 parentSize = parentRect.rect.size;
        float parentWidth = parentSize.x;
        float parentHeight = parentSize.y;
        float parentAspect = parentWidth / parentHeight;

        float targetWidth, targetHeight;

        if (parentAspect >= targetAspect)
        {
            targetHeight = parentHeight;
            targetWidth = targetHeight * parentAspect;
            canvasScaler.matchWidthOrHeight = 1f;
        }
        else
        {
            targetWidth = parentWidth;
            targetHeight = targetWidth / targetAspect;
            canvasScaler.matchWidthOrHeight = 0f;
        }

        scalerWrapper.sizeDelta = new Vector2(targetWidth, targetHeight);
        scalerWrapper.anchorMin = new Vector2(0.5f, 0.5f);
        scalerWrapper.anchorMax = new Vector2(0.5f, 0.5f);
        scalerWrapper.pivot = new Vector2(0.5f, 0.5f);
        scalerWrapper.anchoredPosition = Vector2.zero;
    }

    public RectTransform MessegeWindowBox;
    public RectTransform chatWindowBox;
    public RectTransform chatWindow;

    void LockToPortrait()
    {
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.Portrait;

        scalerWrapper.anchorMin = Vector2.zero;
        scalerWrapper.anchorMax = Vector2.one;
        scalerWrapper.offsetMin = Vector2.zero;
        scalerWrapper.offsetMax = Vector2.zero;
        scalerWrapper.pivot = new Vector2(0.5f, 0.5f);
        scalerWrapper.localScale = Vector3.one;
        canvasScaler.matchWidthOrHeight = 0.5f;

        chatWindow.SetParent(chatWindowBox);
        chatWindow.anchorMin = Vector2.zero;
        chatWindow.anchorMax = Vector2.one;
        chatWindow.offsetMin = Vector2.zero;
        chatWindow.offsetMax = Vector2.zero;
        chatWindow.pivot = new Vector2(0.5f, 0.5f);
        chatWindow.localScale = Vector3.one;

        StartCoroutine(ScrollToBottomNextFrame());
    }

    public void ScrollToBottom()
    {
        StartCoroutine(ScrollToBottomNextFrame());
    }

    IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        yield return null;

    }

    void RestoreLandscapeAutoRotation()
    {
        portraitMode = false;

        Screen.orientation = ScreenOrientation.LandscapeLeft;
        StartCoroutine(EnableLandscapeAutoRotationNextFrame());

        chatWindow.SetParent(MessegeWindowBox);
        chatWindow.anchorMin = new Vector2(0.5f, 0f);
        chatWindow.anchorMax = new Vector2(0.5f, 1f);
        chatWindow.pivot = new Vector2(0.5f, 0.5f);
        chatWindow.offsetMin = new Vector2(-705f, 40f);
        chatWindow.offsetMax = new Vector2(705f, 0f);
        chatWindow.anchoredPosition = new Vector2(465f, 0f);
    }

    IEnumerator EnableLandscapeAutoRotationNextFrame()
    {
        yield return null;
        yield return null;
        Screen.orientation = ScreenOrientation.AutoRotation;
        yield return null;
        yield return null;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;

        yield return null;
        yield return null;
        //Screen.fullScreen = false; // 👈 강제 리프레시 (일부 디바이스에서 필요)
        Screen.fullScreen = true;
        Canvas.ForceUpdateCanvases();

        // ✅ 여기서만 스크롤 고정
        StartCoroutine(ScrollToBottomWhenReady());

    }

    IEnumerator ScrollToBottomWhenReady()
    {
        yield return null;
    }

    public bool portraitMode;
    public void SetOrientationForChat(bool isChatMode)
    {
        portraitMode = isChatMode;

        chatTopButtonBox.SetActive(!portraitMode);

        if (isChatMode)
            LockToPortrait();
        else
            RestoreLandscapeAutoRotation();
    }

    public RectTransform chatBox;
    public GameObject chatTopButtonBox;
    public float defaultBottomOffset = 50f;
    public float defaultTopOffset = 100f;



#if UNITY_ANDROID || UNITY_IOS
    private float lastKeyboardHeight = 0f;
#endif

    void ChatBoxSet()
    {
#if UNITY_EDITOR
        return;
#endif

#if UNITY_ANDROID || UNITY_IOS
        float keyboardHeight = GetKeyboardHeight();
        // 🔥 항상 호출하도록 수정
        if (!Mathf.Approximately(keyboardHeight, lastKeyboardHeight) || keyboardHeight == 0)
        {
            lastKeyboardHeight = keyboardHeight;
            UpdateChatBoxBottom(keyboardHeight);
        }
#endif
    }

    void UpdateChatBoxBottom(float keyboardHeightPx)
    {
        if (keyboardHeightPx == 0 || canvasRect == null)
        {
            chatBox.offsetMin = new Vector2(chatBox.offsetMin.x, defaultBottomOffset);
            chatBox.offsetMax = new Vector2(chatBox.offsetMax.x, -defaultTopOffset);
            return;
        }

        float screenHeight = Screen.height;
        float canvasHeight = canvasRect.rect.height;

        float keyboardRatio = keyboardHeightPx / screenHeight;
        float keyboardHeightCanvas = keyboardRatio * canvasHeight;

        chatBox.offsetMin = new Vector2(chatBox.offsetMin.x, 20f + keyboardHeightCanvas);
        chatBox.offsetMax = new Vector2(chatBox.offsetMax.x, -defaultTopOffset);
        ScrollToBottom();

    }
#if UNITY_IOS
[DllImport("__Internal")]
private static extern void _RegisterKeyboardObserver();
#endif

    float keyboardHeightFromIOS = 0f;
    float GetKeyboardHeight()
    {
#if UNITY_ANDROID
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject view = activity.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView"))
        {
            using (AndroidJavaObject r = new AndroidJavaObject("android.graphics.Rect"))
            {
                view.Call("getWindowVisibleDisplayFrame", r);
                int screenHeight = view.Call<int>("getHeight");
                int visibleHeight = r.Call<int>("height");
                return (float)(screenHeight - visibleHeight);


            }

        }
#elif UNITY_IOS
        return keyboardHeightFromIOS;
#else
        return 0f;
#endif
    }


 

    

}
