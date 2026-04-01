using System;
using CAPYBARA;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;

namespace CAPYBARA.Core
{
    public class AppStateManager : MonoSingleton<AppStateManager>
    {
        private CancellationTokenSource _debounceCts;
        private const float DEBOUNCE_DELAY = 0.3f;

        protected override void Init()
        {
            base.Init();
            Debug.Log("[AppStateManager] 초기화 완료");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if UNITY_EDITOR
            return; // 에디터에서는 동작 안 함
#endif
            Debug.Log($"[AppStateManager] OnApplicationFocus - hasFocus: {hasFocus}");

            if (hasFocus)
            {
                // 포그라운드: 디바운스 후 전송 (따닥 호출 방지)
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
                SendForegroundWithDebounceAsync(_debounceCts.Token).Forget();
            }
            else
            {
                // 백그라운드: 즉시 전송 (기다리면 앱 멈춤)
                Services.Lobby?.AppStateFireAndForget(false);
                Debug.Log("[AppStateManager] 백그라운드 전송");
            }
        }

        private async UniTaskVoid SendForegroundWithDebounceAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(DEBOUNCE_DELAY), cancellationToken: ct);
                Services.Lobby?.AppStateFireAndForget(true);
                Debug.Log("[AppStateManager] 포그라운드 전송");
            }
            catch (OperationCanceledException)
            {
                // 취소됨 - 정상
            }
        }

        protected override void Release()
        {
            base.Release();
            _debounceCts?.Cancel();
            Debug.Log("[AppStateManager] 해제됨");
        }
    }
}

