using System.Linq;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using Mkey;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PopupAdMob : BasePopup
    {
        [SerializeField] private CPButton watchAdButton;
        [SerializeField] private Text curText;
        [SerializeField] private Text maxText;

        private const int MAX_WATCH_AD_COUNT = 5;

        protected override void OnInit()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (watchAdButton != null)
            {
                watchAdButton.onClick.RemoveAllListeners();
                watchAdButton.onClick.AddListener(() => OnWatchAdButtonClick().Forget());
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            UpdateWatchAdCountUI();
        }
        
        private void UpdateWatchAdCountUI()
        {
            if (curText != null)
                curText.text = $"{CPPlayer.UserInfo.watchAdCount}";
            if (maxText != null)
                maxText.text = $"/ {MAX_WATCH_AD_COUNT}";
        }

        protected override void OnClose()
        {
            base.OnClose();
        }

        private async UniTask OnWatchAdButtonClick()
        {
            // 광고 시청 가능 여부 확인
            var req = await Services.Lobby.UserQuestAddAsync(AdMobType.WATCH_AD_DAILY.ToString());
            if (!req.IsSuccess)
            {
                if (req.Error.Code == lobby.ErrorCode.EMaxValue)
                {
                    LobbyErrorInfo errorInfo = StaticData.GetLobbyErrorInfo(lobby.ErrorCode.EMaxValue);
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(errorInfo.message_Kr));
                }
                else
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AdWatchVerifyFailed].StringToLocal));
                }
                return;
            }

            // 광고 재생
#if UNITY_EDITOR
            // 에디터에서는 광고 없이 바로 성공 처리
            bool success = true;
#else
            bool success = await AdMobManager.Instance.ShowRewardedAdAsync();
#endif
            if (success)
            {
                // 보상 요청
                var rewardReq = await Services.Lobby.UserQuestRequestAsync(RewardMissionType.WATCH_AD.ToString());
                if (!rewardReq.IsSuccess)
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AdRewardRequestFailed].StringToLocal));
                    return;
                }

                var res = rewardReq.Data;
                long before = CPPlayer.UserInfo.userDatabase.User.Gold;
                
                // 새 API: 응답에서 해당 퀘스트를 찾아서 보상 값 확인
                var quest = res.QuestList?.FirstOrDefault(q => q.QuestId == RewardMissionType.WATCH_AD.ToString());
                if (quest == null || quest.RewardValue < 0)
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.CannotGetReward].StringToLocal));
                    return;
                }

                CPPlayer.UserInfo.userDatabase.User.Gold += quest.RewardValue;

                long after = CPPlayer.UserInfo.userDatabase.User.Gold;

                // UI 애니메이션으로 갱신
                CPPlayer.Balance.MyBalTextAnimEvent?.Invoke(before, after);
                Debug.Log($"광고 보상 지급 완료! {before} > {after}");

                // 광고 시청 횟수 증가 및 UI 업데이트
                CPPlayer.UserInfo.watchAdCount = quest.ReceivedRewardValue;
                UpdateWatchAdCountUI();

                // 팝업 닫기
                //Close();
            }
        }
    }
}
