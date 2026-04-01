using System;
using System.Linq;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA 
{
    public class DailyMissionSlot : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTitle;
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgAlreadyRewarded;
        [SerializeField] private GameObject goCanClaim;
        [SerializeField] private GameObject[] goFillEffect;
        [SerializeField] private Image imgDone;
        [SerializeField] private TMP_Text rewardAmountText;

        [Space(10)]
        [Header("Reward")]
        [SerializeField] private CPButton btnReward;
        [SerializeField] private GameObject[] goOnRewardEffect;
        [SerializeField] private UISlicedFill imgGauge;
        [SerializeField] private TMP_Text txtMissionCount;

        private Quest questInfo;
        private RewardMissionInfo staticInfo;
        private Action<Quest> onRewardClaimed;
        private int playTimeMinutes = -1;

        public bool canClaim => questInfo.QuestValue >= questInfo.MaxCount && questInfo.ReceivedRewardValue <= 0;
        public bool alreadyClaimed => questInfo.ReceivedRewardValue > 0;

        public void Init(Quest _questInfo, RewardMissionInfo _staticInfo, Action<Quest> _onRewardClaimed = null, int _playTimeMinutes = -1)
        {
            questInfo = _questInfo;
            staticInfo = _staticInfo;
            onRewardClaimed = _onRewardClaimed;
            playTimeMinutes = _playTimeMinutes;

            btnReward.onClick.RemoveAllListeners();
            btnReward.onClick.AddListener(() => { GetRewardForMission().Forget(); });

            SetReward();
            UpdateUI();
        }

        private async UniTask GetRewardForMission()
        {
            var reqpacket = await Services.Lobby.UserQuestRequestAsync(questInfo.QuestId);
            if (reqpacket == null || !reqpacket.IsSuccess)
            {
                Debug.Log("에러남");
                return;
            }

            var updatedQuest = reqpacket.Data.QuestList?.FirstOrDefault(q => q.QuestId == questInfo.QuestId);
            if (updatedQuest != null)
                questInfo = updatedQuest;

            if (Enum.TryParse<ItemID>(questInfo.RewardItemId, out var itemId))
            {
                if (itemId == ItemID.DEFAULT_CURRENCY)
                {
                    var userinfo = await Services.Lobby.GetUserInfoAsync();
                    if (userinfo.IsSuccess)
                        CPPlayer.UserInfo.userDatabase = userinfo.Data;

                    CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                }
                else
                {
                    ShowRewardPopup(questInfo.RewardItemId, questInfo.RewardValue);
                }
            }

            // PLAY_TIME 미션 보상 수령 시 플래그 설정
            if (questInfo.Type == "PLAY_TIME")
            {
                CPPlayer.OutGame.playTimeMissionRewarded = true;
                CPPlayer.OutGame.playTimeMissionClaimable = false;
            }

            UpdateUI();
            onRewardClaimed?.Invoke(questInfo);
        }
        
        private void ShowRewardPopup(string rewardItemId, long rewardValue)
        {
            var itemBundle = ItemBundle.Loaded;
            var sprite = itemBundle?.GetItemSprite(rewardItemId, true);
            
            PopupManager.Instance.Open<PopupGetReward>(new GetRewardPopupParameter
            {
                Title = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemAcquired].StringToLocal,
                ItemIcon = sprite,
                Description = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.CheckItemInVault].StringToLocal,
                ConfirmButtonText = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Close].StringToLocal
            });
        }

        private void UpdateUI()
        {
            SetTitle();
            SetIcon(canClaim, alreadyClaimed);
            SetAlreadyRewardIcon(canClaim, alreadyClaimed);
            SetRewardButton(canClaim, alreadyClaimed);
            SetFillGauge(alreadyClaimed);   
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTitle);
        }

        private void SetReward()
        {
            if (Enum.TryParse<ItemID>(questInfo.RewardItemId, out var itemId))
            {
                var itemBundle = ItemBundle.Loaded;
                imgIcon.gameObject.SetActive(true);
                rewardAmountText.gameObject.SetActive(true);

                if (itemId == ItemID.DEFAULT_CURRENCY)
                {
                    rewardAmountText.text = Extension.ToKoreanFormatReward(questInfo.RewardValue);
                    var sprCoin = itemBundle.GetItemSprite("DEFAULT_CURRENCY");
                    if (sprCoin == null)
                        imgIcon.gameObject.SetActive(false);
                    else
                        imgIcon.sprite = sprCoin;
                }
                else
                {
                    if (questInfo.RewardValue > 1)
                        rewardAmountText.text = Extension.ToKoreanFormatReward(questInfo.RewardValue);
                    else
                        rewardAmountText.gameObject.SetActive(false);

                    var sprItem = itemBundle.GetItemSprite(questInfo.RewardItemId, true);
                    if (sprItem == null)
                        imgIcon.gameObject.SetActive(false);
                    else
                        imgIcon.sprite = sprItem;
                }
            }
        }

        private void SetTitle()
        {
            if (txtTitle == null || staticInfo == null)
                return;

            txtTitle.text = staticInfo.message_Kr;
        }

        private void SetIcon(bool _claim, bool _alreadyClaimed)
        {
            imgIcon.color = _claim && !_alreadyClaimed ? Color.white : new Color32(79, 79, 79, 255);
            Debug.Log(imgIcon.color.r);
        }

        private void SetRewardButton(bool _claim, bool _alreadyClaimed)
        {
            btnReward.gameObject.SetActive(true);
            btnReward.enabled = _claim && !_alreadyClaimed; // 보상은 받을 수 있지만, 이미 받았을 땐 꺼주어야 함
            for (int i = 0; i < goOnRewardEffect.Length; i++)
                goOnRewardEffect[i].SetActive(_claim && !_alreadyClaimed);
        }

        private void SetAlreadyRewardIcon(bool _claim, bool _alreadyClaimed)
        {
            imgAlreadyRewarded.gameObject.SetActive(_alreadyClaimed);
            goCanClaim.SetActive(_claim && !_alreadyClaimed);
            imgDone.gameObject.SetActive(_alreadyClaimed);
        }

        private void SetFillGauge(bool _alreadyClaimed)
        {
            for (int i = 0; i < goFillEffect.Length; i++)
                goFillEffect[i].gameObject.SetActive(!_alreadyClaimed);

            int maxCount = questInfo.MaxCount > 0 ? questInfo.MaxCount : 1;
            
            int questValue = _alreadyClaimed ? maxCount : questInfo.QuestValue;
            float amountValue = (float)questValue / maxCount;
            
            txtMissionCount.gameObject.SetActive(true);
            
            // PLAY_TIME은 시간 -> 분 변환 (1시간 = 60분)
            bool isPlayTime = questInfo.Type == "PLAY_TIME";
            int displayMaxCount = isPlayTime ? maxCount * 60 : maxCount;
            
            int displayQuestValue = isPlayTime ? questValue * 60 : questValue;
            
            if (isPlayTime && !_alreadyClaimed)
            {
                SetPlayTimeGauge(displayMaxCount);
                return;
            }
            
            string valueKr = staticInfo?.value_Kr ?? "";
            txtMissionCount.text = string.IsNullOrEmpty(valueKr) 
                ? $"{Extension.ToKoreanFormatReward(displayQuestValue)}/{Extension.ToKoreanFormatReward(displayMaxCount)}" 
                : $"{Extension.ToKoreanFormatReward(displayQuestValue)}{valueKr}/{Extension.ToKoreanFormatReward(displayMaxCount)}{valueKr}";
            imgGauge.FillAmount = amountValue;
        }
        
        private void SetPlayTimeGauge(int maxMinutes)
        {
            int displayMinutes = playTimeMinutes >= 0 ? Mathf.Min(playTimeMinutes, maxMinutes) : 0;
            float fillAmount = Mathf.Clamp01((float)displayMinutes / maxMinutes);
            imgGauge.FillAmount = fillAmount;
            
            string valueKr = staticInfo?.value_Kr ?? "";
            txtMissionCount.text = string.IsNullOrEmpty(valueKr)
                ? $"{Extension.ToKoreanFormatReward(displayMinutes)}/{Extension.ToKoreanFormatReward(maxMinutes)}"
                : $"{Extension.ToKoreanFormatReward(displayMinutes)}{valueKr}/{Extension.ToKoreanFormatReward(maxMinutes)}{valueKr}";
        }
    }
}
