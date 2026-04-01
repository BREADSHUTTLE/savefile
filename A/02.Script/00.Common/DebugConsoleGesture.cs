using UnityEngine;
using IngameDebugConsole;

/// <summary>
/// 3손가락 탭으로 디버그 콘솔을 토글하는 스크립트
/// IngameDebugConsole 프리팹에 추가하거나 별도 오브젝트에 추가
/// </summary>
public class DebugConsoleGesture : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("콘솔을 열기 위해 필요한 손가락 개수")]
    [SerializeField] private int requiredFingers = 3;
    
    [Tooltip("탭으로 인정되는 최대 터치 시간 (초)")]
    [SerializeField] private float maxTapDuration = 2.0f;
    
    [Tooltip("탭으로 인정되는 최대 이동 거리")]
    [SerializeField] private float maxTapMovement = 100f;

    private float touchStartTime;
    private bool gestureStarted;
    private bool gestureTriggered;

    private void Update()
    {
        // 에디터/PC/에뮬레이터: F12 키 또는 ` (백틱) 키
        if (Input.GetKeyDown(KeyCode.F12) || Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleConsole();
            return;
        }

        // 실기기: 3손가락 탭
        HandleTouchGesture();
    }

    private void HandleTouchGesture()
    {
        int touchCount = Input.touchCount;

        // 필요한 손가락 수에 도달했을 때 제스처 시작
        if (touchCount >= requiredFingers && !gestureStarted)
        {
            gestureStarted = true;
            gestureTriggered = false;
            touchStartTime = Time.unscaledTime;
        }

        // 제스처 진행 중
        if (gestureStarted)
        {
            float elapsed = Time.unscaledTime - touchStartTime;

            // 시간 초과시 리셋
            if (elapsed > maxTapDuration)
            {
                ResetGesture();
                return;
            }

            // 손가락이 모두 떨어졌을 때
            if (touchCount == 0 && !gestureTriggered)
            {
                // 유효한 탭이면 콘솔 토글
                if (elapsed < maxTapDuration)
                {
                    ToggleConsole();
                    gestureTriggered = true;
                }
                ResetGesture();
            }
        }
    }

    private void ResetGesture()
    {
        gestureStarted = false;
    }

    private void ToggleConsole()
    {
        if (DebugLogManager.Instance == null)
        {
            Debug.LogWarning("[DebugConsoleGesture] DebugLogManager.Instance is null!");
            return;
        }

        if (DebugLogManager.Instance.IsLogWindowVisible)
        {
            DebugLogManager.Instance.HideLogWindow();
            Debug.Log("[DebugConsoleGesture] Console Hidden");
        }
        else
        {
            DebugLogManager.Instance.ShowLogWindow();
            Debug.Log("[DebugConsoleGesture] Console Shown");
        }
    }
}
