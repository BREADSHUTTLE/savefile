using UnityEngine;
using GoogleMobileAds.Api;
using Cysharp.Threading.Tasks;

namespace CAPYBARA
{
    public class AdMobManager : MonoSingleton<AdMobManager>
    {
        [Header("광고 ID (나중에 실제 ID로 교체해야 함)")]
        [SerializeField] private string androidAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // 테스트 ID
        [SerializeField] private string iosAdUnitId = "ca-app-pub-3940256099942544/1712485313";     // 테스트 ID

        private RewardedAd rewardedAd;
        private bool isInitialized = false;
        private UniTaskCompletionSource<bool> adCompletion;

        private string AdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return androidAdUnitId;
#elif UNITY_IOS
                return iosAdUnitId;
#else
                return "unused";
#endif
            }
        }

        protected override void Init()
        {
            base.Init();
            InitializeAdsAsync().Forget();
        }

        protected override void Release()
        {
            base.Release();
            rewardedAd?.Destroy();
        }

        private async UniTaskVoid InitializeAdsAsync()
        {
            var unit = new UniTaskCompletionSource();
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("광고 SDK 초기화 완료");
                isInitialized = true;
                unit.TrySetResult();    // 완료 후 결과 반환
            });

            await unit.Task;            // 대기
            await LoadRewardedAdAsync();
        }

        public async UniTask LoadRewardedAdAsync()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("광고 SDK가 아직 초기화되지 않았습니다.");
                return;
            }

            // 기존 광고 정리
            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            Debug.Log("광고 로딩 중...");

            var unit = new UniTaskCompletionSource<RewardedAd>();
            var adRequest = new AdRequest();

            RewardedAd.Load(AdUnitId, adRequest, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError($"광고 로드 실패: {error?.GetMessage()}");
                    unit.TrySetResult(null);
                    return;
                }

                Debug.Log("광고 로드 완료!");
                unit.TrySetResult(ad);
            });

            rewardedAd = await unit.Task;
            if (rewardedAd != null)
                RegisterEventHandlers(rewardedAd);
        }

        public async UniTask<bool> ShowRewardedAdAsync()
        {
            if (rewardedAd == null || !rewardedAd.CanShowAd())
            {
                Debug.LogWarning("광고가 준비되지 않았습니다. 다시 로드합니다.");
                await LoadRewardedAdAsync();
                return false;
            }
            
            adCompletion = new UniTaskCompletionSource<bool>();

            Debug.Log("광고 표시");
            rewardedAd.Show(reward =>
            {
                Debug.Log($"보상 Type: {reward.Type}, Amount: {reward.Amount}");
                adCompletion?.TrySetResult(true);
            });

            return await adCompletion.Task;
        }

        public bool IsAdReady()
        {
            return rewardedAd != null && rewardedAd.CanShowAd();
        }

        private void RegisterEventHandlers(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("광고 닫힘 - 새 광고 로드");
                LoadRewardedAdAsync().Forget();
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogError($"광고 표시 실패: {error.GetMessage()}");
                adCompletion?.TrySetResult(false);
                LoadRewardedAdAsync().Forget();
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("광고 클릭");
            };

            ad.OnAdPaid += adValue =>
            {
                Debug.Log($"광고 수익: {adValue.Value} {adValue.CurrencyCode}");
            };
        }
    }
}
