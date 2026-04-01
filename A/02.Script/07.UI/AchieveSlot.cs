using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA 
{
    public enum AchieveSlotType
    {
        Quest,
        Points
    }

    public class AchieveSlot : MonoBehaviour
    {
        private AchieveSlotType slotType;
        
        // 일반 업적
        private Quest questInfo = null;
        public Quest QuestInfo => questInfo;
        private RewardMissionInfo staticInfo = null;
        
        // 포인트 업적
        private ConfigPoints configPointsInfo = null;
        private Points currentPoints = null;
        private PointsDone pointsDone = null;

        [Space(10)]
        [Header("UI")]
        [SerializeField] private Image imgIcon;
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private Image imgGauge;
        [SerializeField] private TMP_Text txtGauge;
        [SerializeField] private GameObject goDone;
        [SerializeField] private TMP_Text txtReward;
        [SerializeField] private CPButton btnReward;

        [Space(10)]
        [Header("Effects")]
        [SerializeField] private GameObject[] goFillEffect;
        [SerializeField] private GameObject[] goOnRewardEffect;
        
        public bool canClaim => questInfo.QuestValue >= questInfo.MaxCount && questInfo.ReceivedRewardValue <= 0;
        public bool alreadyClaimed => questInfo.ReceivedRewardValue > 0;
        
        public bool canClaimPoints => currentPoints != null && configPointsInfo != null && currentPoints.Achievements >= configPointsInfo.PointsMin && !alreadyClaimedPoints;
        public bool alreadyClaimedPoints => pointsDone != null && !string.IsNullOrEmpty(pointsDone.PointsType);
        
        public bool canClaimAny => slotType == AchieveSlotType.Quest ? canClaim : canClaimPoints;
        public bool isAlreadyClaimed => slotType == AchieveSlotType.Quest ? alreadyClaimed : alreadyClaimedPoints;
        public long sortValue => slotType == AchieveSlotType.Points ? (configPointsInfo?.PointsMin ?? 0) : (questInfo?.MaxCount ?? 0);
        
        private AchieveItemSlot parentItemSlot;

        private void Awake()
        {
            btnReward.onClick.AddListener(OnClickReward);
        }
        
        public void SetParentItemSlot(AchieveItemSlot parent)
        {
            parentItemSlot = parent;
        }

        private void OnClickReward()
        {
            if (!canClaimAny || isAlreadyClaimed)
                return;
            
            if (slotType == AchieveSlotType.Quest)
                GetRewardForMission().Forget();
            else
                GetRewardForPointsMission().Forget();
        }

        // 일반 업적
        public void Init(Quest _questInfo, RewardMissionInfo _staticInfo)
        {
            slotType = AchieveSlotType.Quest;
            questInfo = _questInfo;
            staticInfo = _staticInfo;

            if (txtTitle != null && staticInfo != null)
                txtTitle.text = staticInfo.message_Kr;

            UpdateUI();
        }

        // 포인트 업적
        public void Init(int _index, ConfigPoints _configPointsInfo)
        {
            slotType = AchieveSlotType.Points;
            configPointsInfo = _configPointsInfo;

            var localizeInfo = System.Linq.Enumerable.FirstOrDefault(StaticData.Wrapper.rewardPointsInfo, x => x.rewardId.ToString() == configPointsInfo.RewardType);
            
            if (txtTitle != null && localizeInfo != null)
                txtTitle.text = localizeInfo.message_Kr;

            SetPointsReward(_index);
        }

        private void SetPointsReward(int index)
        {
            if (configPointsInfo == null)
                return;

            // quests config에서 포인트 업적 보상 정보 가져오기
            var questReward = ConfigDataManager.quests.FirstOrDefault(q => 
                q.QuestType == "ACHIEVEMENT" && q.Type == "ACHIEVEMENTS" && q.MaxCount == configPointsInfo.PointsMin);
            
            if (questReward == null)
                return;

            if (Enum.TryParse<ItemID>(questReward.RewardItemId, out var itemId))
            {
                var itemBundle = ItemBundle.Loaded;
                imgIcon.gameObject.SetActive(true);
                txtReward.gameObject.SetActive(true);

                if (itemId == ItemID.DEFAULT_CURRENCY)
                {
                    txtReward.text = Extension.ToKoreanFormatReward(questReward.RewardValue);
                    var sprCoin = itemBundle.GetItemSprite($"COIN_{index + 2}");
                    if (sprCoin == null)
                        imgIcon.gameObject.SetActive(false);
                    else
                        imgIcon.sprite = sprCoin;
                }
                else
                {
                    if (questReward.RewardValue > 1)
                        txtReward.text = Extension.ToKoreanFormatReward(questReward.RewardValue);
                    else
                        txtReward.gameObject.SetActive(false);

                    var sprItem = itemBundle.GetItemSprite(questReward.RewardItemId, true);
                    if (sprItem == null)
                        imgIcon.gameObject.SetActive(false);
                    else
                        imgIcon.sprite = sprItem;
                }
            }
        }

        public void SetPointsState(Points points, IEnumerable<PointsDone> doneList = null)
        {
            if (slotType != AchieveSlotType.Points || configPointsInfo == null)
                return;

            currentPoints = points ?? new Points { Achievements = 0 };
            pointsDone = doneList?.FirstOrDefault(d => d.PointsType == configPointsInfo.TxType);

            UpdatePointsUI();
        }

        private async UniTask GetRewardForMission()
        {
            if (questInfo == null)
                return;

            var reqpacket = await Services.Lobby.UserQuestRequestAsync(questInfo.QuestId);
            if (reqpacket == null || !reqpacket.IsSuccess)
                return;
            
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
                    CPPlayer.OutGame.RenewInventory?.Invoke();
                }
            }

            UpdateUI();

            parentItemSlot?.RefreshStatusImages();
        }

        private async UniTask GetRewardForPointsMission()
        {
            if (configPointsInfo == null)
                return;

            var reqpacket = await Services.Lobby.PointsRewardReqAsync(configPointsInfo.RewardType);
            if (!reqpacket.IsSuccess)
                return;
            
            if (reqpacket.Data?.Points != null)
                currentPoints = reqpacket.Data.Points;
            
            pointsDone = new PointsDone { PointsType = configPointsInfo.TxType };
            var questReward = ConfigDataManager.quests.FirstOrDefault(q => q.QuestType == "ACHIEVEMENT" && q.Type == "ACHIEVEMENTS" && q.MaxCount == configPointsInfo.PointsMin);
            if (questReward != null)
            {
                if (Enum.TryParse<ItemID>(questReward.RewardItemId, out var itemId))
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
                        ShowRewardPopup(questReward.RewardItemId, questReward.RewardValue);
                        CPPlayer.OutGame.RenewInventory?.Invoke();
                    }
                }
            }
            
            UpdatePointsUI();
            parentItemSlot?.RefreshStatusImages();
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

        public void UpdateUI()
        {
            if (slotType != AchieveSlotType.Quest || questInfo == null)
                return;
            
            SetIcon(canClaim, alreadyClaimed);
            SetDoneIcon(alreadyClaimed);
            SetRewardEffect(canClaim, alreadyClaimed);
            SetFillEffect(!alreadyClaimed);
            
            int maxCount = questInfo.MaxCount > 0 ? questInfo.MaxCount : 1;
            float fillValue = (float)questInfo.QuestValue / maxCount;
            imgGauge.fillAmount = fillValue;
            
            if (txtGauge != null)
            {
                txtGauge.gameObject.SetActive(true);
                string valueKr = staticInfo?.value_Kr ?? "";
                txtGauge.text = string.IsNullOrEmpty(valueKr)
                    ? $"{Extension.ToKoreanFormatReward(questInfo.QuestValue)}/{Extension.ToKoreanFormatReward(questInfo.MaxCount)}" 
                    : $"{Extension.ToKoreanFormatReward(questInfo.QuestValue)}{valueKr}/{Extension.ToKoreanFormatReward(questInfo.MaxCount)}{valueKr}";
            }
        }

        private void UpdatePointsUI()
        {
            if (slotType != AchieveSlotType.Points || configPointsInfo == null)
                return;

            SetIcon(canClaimPoints, alreadyClaimedPoints);
            SetDoneIcon(alreadyClaimedPoints);
            SetRewardEffect(canClaimPoints, alreadyClaimedPoints);
            SetFillEffect(!alreadyClaimedPoints);

            float fillValue = (float)currentPoints.Achievements / configPointsInfo.PointsMin;
            imgGauge.fillAmount = fillValue;
            
            if (txtGauge != null)
            {
                txtGauge.gameObject.SetActive(true);
                string valueKr = staticInfo?.value_Kr ?? "";
                txtGauge.text = string.IsNullOrEmpty(valueKr) 
                    ? $"{Extension.ToKoreanFormatReward(currentPoints.Achievements)}/{Extension.ToKoreanFormatReward(configPointsInfo.PointsMin)}" 
                    : $"{Extension.ToKoreanFormatReward(currentPoints.Achievements)}{valueKr}/{Extension.ToKoreanFormatReward(configPointsInfo.PointsMin)}{valueKr}";
            }
        }

        private void SetIcon(bool canClaim, bool alreadyClaimed)
        {
            imgIcon.color = canClaim && !alreadyClaimed ? new Color32(255, 255, 255, 255) : new Color32(79, 79, 79, 255);
        }

        public void SetReward(int index)
        {
            if (ItemBundle.Loaded?.coinSprites == null || ItemBundle.Loaded.coinSprites.Count == 0)
                return;

            if (questInfo == null)
                return;

            if (Enum.TryParse<ItemID>(questInfo.RewardItemId, out var itemId))
            {
                var itemBundle = ItemBundle.Loaded;
                imgIcon.gameObject.SetActive(true);
                txtReward.gameObject.SetActive(true);

                if (itemId == ItemID.DEFAULT_CURRENCY)
                {
                    txtReward.text = Extension.ToKoreanFormatReward(questInfo.RewardValue);
                    var sprCoin = itemBundle.GetItemSprite($"COIN_{index + 2}");
                    if (sprCoin == null)
                        imgIcon.gameObject.SetActive(false);
                    else
                        imgIcon.sprite = sprCoin;
                }
                else
                {
                    if (questInfo.RewardValue > 1)
                        txtReward.text = Extension.ToKoreanFormatReward(questInfo.RewardValue);
                    else
                        txtReward.gameObject.SetActive(false);

                    var sprItem = itemBundle.GetItemSprite(questInfo.RewardItemId, true);
                    if (sprItem == null)
                        imgIcon.gameObject.SetActive(false);
                    else
                        imgIcon.sprite = sprItem;
                }
            }
        }

        private void SetDoneIcon(bool alreadyClaimed)
        {
            if (goDone != null)
                goDone.SetActive(alreadyClaimed);
        }

        private void SetRewardEffect(bool canClaim, bool alreadyClaimed)
        {
            if (goOnRewardEffect == null)
                return;
            
            for (int i = 0; i < goOnRewardEffect.Length; i++)
                goOnRewardEffect[i].SetActive(canClaim && !alreadyClaimed);
        }

        private void SetFillEffect(bool active)
        {
            if (goFillEffect == null)
                return;
            
            for (int i = 0; i < goFillEffect.Length; i++)
                goFillEffect[i].SetActive(active);
        }
    }
}
