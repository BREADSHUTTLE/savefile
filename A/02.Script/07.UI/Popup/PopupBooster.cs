using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PopupBooster : BasePopup
    {
        [SerializeField] private CPButton descBtn;
        [SerializeField] private GameObject descPopup;

        [Header("보상 표시")]
        [SerializeField] private TMP_Text rewardAmountText;
        
        [Header("진행 바")]
        [SerializeField] private Image filledPointImage;
        [SerializeField] private TMP_Text progressText;
        
        [Header("보상 받기 버튼")]
        [SerializeField] private CPButton rewardButton;
        [SerializeField] private TMP_Text rewardButtonText;

        private List<ConfigPoints> canRewardPointList = new List<ConfigPoints>();
        private List<PointsDone> pointsDoneList = new List<PointsDone>();
        
        // 현재 단계 인덱스 (-1이면 모두 완료)
        private int currentStageIndex = -1;
        private bool canClaim = false;

        protected override void OnInit()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            canRewardPointList = ConfigDataManager.GetPointsByType("BOOST");
            canRewardPointList.Sort((a, b) => a.PointsMin.CompareTo(b.PointsMin));

            if (rewardButton != null)
            {
                rewardButton.onClick.RemoveAllListeners();
                rewardButton.onClick.AddListener(() => ClickRewardBtn().Forget());
            }

            if (descBtn != null)
            {
                descBtn.onClick.RemoveAllListeners();
                descBtn.onClick.AddListener(() =>
                {
                    if (descPopup != null)
                        descPopup.SetActive(!descPopup.activeInHierarchy);
                });
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            
            if (descPopup != null)
                descPopup.SetActive(false);
            
            OpenWindowAsync().Forget();
        }

        private async UniTask OpenWindowAsync()
        {
            int nowUnix = (int)(DateTimeOffset.Now.ToUnixTimeSeconds());
            DateTime oneYearAgo = DateTime.Now.AddYears(-1);
            int oneYearAgoUnix = (int)(new DateTimeOffset(oneYearAgo).ToUnixTimeSeconds());

            var currentPointReqPacket = await Services.Lobby.PointsReqAsync();
            var pointsMissionDonePacket = await Services.Lobby.PointsRewardDoneAsync(oneYearAgoUnix, nowUnix);

            if (!currentPointReqPacket.IsSuccess)
                return;
            if (!pointsMissionDonePacket.IsSuccess)
                return;

            CPPlayer.Inventory.myPoints = currentPointReqPacket.Data.Points;
            pointsDoneList = pointsMissionDonePacket.Data.PointsDone.ToList();

            UpdateUI();
        }

        private void UpdateUI()
        {
            currentStageIndex = -1;
            for (int i = 0; i < canRewardPointList.Count; i++)
            {
                var pointsDone = pointsDoneList.FirstOrDefault(o => o.PointsType == canRewardPointList[i].TxType);
                if (pointsDone == null)
                {
                    currentStageIndex = i;
                    break;
                }
            }

            if (currentStageIndex == -1 || currentStageIndex >= canRewardPointList.Count)
            {
                if (rewardAmountText != null)
                    rewardAmountText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AllRewardsCompleted].StringToLocal;
                if (progressText != null)
                    progressText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Completed].StringToLocal;
                if (filledPointImage != null)
                    filledPointImage.fillAmount = 1f;
                canClaim = false;
                if (rewardButtonText != null)
                    rewardButtonText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ReceiveCompleted].StringToLocal;
                if (rewardButton != null)
                    rewardButton.interactable = false;
                return;
            }

            var currentStage = canRewardPointList[currentStageIndex];
            long myBoostPoint = CPPlayer.Inventory.myPoints?.Boost ?? 0;
            long targetPoint = currentStage.PointsMin;

            if (rewardAmountText != null)
                rewardAmountText.text = Extension.ToKoreanFormat(currentStage.Amount) + " " + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Gold].StringToLocal;

            if (progressText != null)
                progressText.text = $"{Extension.ToKoreanFormat(myBoostPoint)} / {Extension.ToKoreanFormat(targetPoint)}";

            if (filledPointImage != null)
            {
                float progress = (float)myBoostPoint / (float)targetPoint;
                filledPointImage.fillAmount = Mathf.Clamp01(progress);
            }

            canClaim = myBoostPoint >= targetPoint;
            
            if (rewardButtonText != null)
                rewardButtonText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.GetReward].StringToLocal;

            if (rewardButton != null)
                rewardButton.interactable = canClaim;
        }

        private async UniTask ClickRewardBtn()
        {
            if (!canClaim)
                return;
            
            if (currentStageIndex < 0 || currentStageIndex >= canRewardPointList.Count)
                return;

            var currentStage = canRewardPointList[currentStageIndex];
            
            var reqpacket = await Services.Lobby.PointsRewardReqAsync(currentStage.RewardType);
            if (reqpacket == null || !reqpacket.IsSuccess)
                return;

            // 응답에서 포인트 데이터 갱신
            if (reqpacket.Data?.Points != null)
                CPPlayer.Inventory.myPoints = reqpacket.Data.Points;

            pointsDoneList.Add(new PointsDone { PointsType = currentStage.TxType });

            // 유저 골드 정보 갱신
            var userinfo = await Services.Lobby.GetUserInfoAsync();
            if (userinfo.IsSuccess)
                CPPlayer.UserInfo.userDatabase = userinfo.Data;

            CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();

            UpdateUI();
        }

        protected override void OnClose()
        {
            base.OnClose();
        }
    }
}
