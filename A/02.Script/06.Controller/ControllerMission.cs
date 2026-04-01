using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CAPYBARA
{
    public class ControllerMission
    {
        public enum Type
        {
            ACHIEVEMENT,
            MISSION,
        }
        
        public enum AchieveCategory
        {
            PLAY_GAMES,
            CONTINUOUS_ACCESS,
            INVITE_FRIEND,
            POINTS,  // 마지막 (ConfigDataManager.points로 그림)
        }
        
        private class DailyMissionData
        {
            public Quest Quest;
            public RewardMissionInfo StaticInfo;
        }
        
        public ViewMission view;
        public CancellationTokenSource cts;

        private List<DailyMissionData> _dailyMissionDataList = new List<DailyMissionData>();
        private Dictionary<AchieveCategory, AchieveItemSlot> achieveItemSlotlist = new Dictionary<AchieveCategory, AchieveItemSlot>();
        private IEnumerable<PointsDone> _pointsDoneList;
        private int _playTimeMinutes = -1;

        private int currentTabIndex = 0;
        private Tweener scrollTween;

        public ControllerMission(ViewMission _view, CancellationTokenSource _cts)
        {
            view = _view;
            cts = _cts;

            view.onScrollDragBegin += StopScrollAnimation;
            view.tabGroup.onIndexChanged += OnClickTap;
            view.closeBtn.onClick.AddListener(CloseView);
            view.gotoPurchaseClassBtn.onClick.AddListener(OnClickGoToShopClass);
            view.dailyScrollView.OnCellUpdate = OnDailyCellUpdate;

            CPPlayer.OutGame.OpenMissionView += () => { OpenView().Forget(); };
        }

        private void OnClickTap(int index)
        {
            if (currentTabIndex == index)
            {
                AnimateScrollToStart();
                return;
            }

            currentTabIndex = index;
            view.SetActiveWindow(index);
        }

        private void CloseView()
        {
            view.gameObject.SetActive(false);
        }

        private void OnClickGoToShopClass()
        {
            CPPlayer.OutGame.openShopUIWithTab?.Invoke(ShopMainTapType.CLASS, () => view.gameObject.SetActive(false));
        }

        private void ClearData()
        {
            _dailyMissionDataList.Clear();
            
            // 업적 슬롯 정리
            foreach (var kvp in achieveItemSlotlist)
            {
                if (kvp.Value != null)
                    GameObject.Destroy(kvp.Value.gameObject);
            }
            achieveItemSlotlist.Clear();
        }

        private Type? GetQuestType(string questType)
        {
            if (Enum.TryParse<Type>(questType, true, out var result))
                return result;
            return null;
        }
        
        private AchieveCategory? GetAchieveCategoryKey(string questType)
        {
            foreach (AchieveCategory category in Enum.GetValues(typeof(AchieveCategory)))
            {
                if (questType.Contains(category.ToString()))
                    return category;
            }
            return null;
        }
        
        private Quest MergeQuestData(Quest configQuest, Quest userQuest)
        {
            return new Quest
            {
                QuestId = configQuest.QuestId,
                QuestType = configQuest.QuestType,
                Type = configQuest.Type,
                MaxCount = configQuest.MaxCount,
                RewardItemId = configQuest.RewardItemId,
                RewardValue = configQuest.RewardValue,
                QuestValue = userQuest?.QuestValue ?? 0,
                ReceivedRewardValue = userQuest?.ReceivedRewardValue ?? 0
            };
        }

        private RewardMissionInfo GetStaticInfoForQuest(string questId)
        {
            if (!Enum.TryParse<RewardMissionType>(questId, out var missionType))
                return null;

            if (missionType == RewardMissionType.PLAY_GAMES_RANDOM_10 || missionType == RewardMissionType.PLAY_GAMES_RANDOM_DAILY)
            {
                var todayGameType = LoginData.Cloud.loginValue.loginres?.TodayGameType;
                RewardGameType gameType = todayGameType?.ToUpper() switch
                {
                    "HOLDEM" => RewardGameType.HOLDEM,
                    "BADUGI" => RewardGameType.BADUGI,
                    "SEVENPOKER" => RewardGameType.SEVENPOKER,
                    _ => RewardGameType.HOLDEM  // 기본값
                };
                
                return StaticData.Wrapper.rewardMissionInfo.FirstOrDefault(s => s.rewardId == missionType && s.rewardGameType == gameType);
            }

            return StaticData.Wrapper.rewardMissionInfo.FirstOrDefault(s => s.rewardId == missionType);
        }

        private async UniTask OpenView()
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
            
            // Points 정보 갱신
            var pointsRes = await Services.Lobby.PointsReqAsync();
            if (pointsRes.IsSuccess)
                CPPlayer.Inventory.myPoints = pointsRes.Data.Points;
            
            var missionPacket = await Services.Lobby.UserQuestListAsync();
            var userQuestList = missionPacket.IsSuccess ? missionPacket.Data?.QuestList : null;

            // 포인트 보상 수령 여부 조회
            int nowUnix = (int)(DateTimeOffset.Now.ToUnixTimeSeconds());
            int oneYearAgoUnix = (int)(new DateTimeOffset(DateTime.Now.AddYears(-1)).ToUnixTimeSeconds());
            var pointsDonePacket = await Services.Lobby.PointsRewardDoneAsync(oneYearAgoUnix, nowUnix);
            _pointsDoneList = pointsDonePacket.IsSuccess ? pointsDonePacket.Data?.PointsDone : null;

            ClearData();

            foreach (var configQuest in ConfigDataManager.quests)
            {   
                var staticInfo = GetStaticInfoForQuest(configQuest.QuestId);
                if (staticInfo == null)
                    continue;
                
                var userQuest = userQuestList?.FirstOrDefault(q => q.QuestId == configQuest.QuestId);
                var mergedQuest = MergeQuestData(configQuest, userQuest);
                
                var questType = GetQuestType(configQuest.QuestType);
                if (!questType.HasValue)
                    continue;
                
                if (questType.Value == Type.MISSION)
                {
                    // ALL_IN, WATCH_AD, ATTENDANCE 타입은 스킵
                    if (configQuest.Type.Contains("ALL_IN") || configQuest.Type.Contains("WATCH_AD") || configQuest.Type.Contains("ATTENDANCE"))
                        continue;
                    
                    _dailyMissionDataList.Add(new DailyMissionData
                    {
                        Quest = mergedQuest,
                        StaticInfo = staticInfo
                    });
                }
                else if (questType.Value == Type.ACHIEVEMENT)
                {
                    AchieveCategory? categoryKey = GetAchieveCategoryKey(configQuest.Type);
                    if (!categoryKey.HasValue)
                        continue;
                    
                    AchieveItemSlot achieveItemSlot;
                    if (achieveItemSlotlist.ContainsKey(categoryKey.Value))
                    {
                        achieveItemSlot = achieveItemSlotlist[categoryKey.Value];
                    }
                    else
                    {
                        achieveItemSlot = GameObject.Instantiate(view.achieveItemSlotPrefab);
                        achieveItemSlot.transform.SetParent(view.achieveScrollRect.content, false);
                        achieveItemSlot.Init(staticInfo, view.achieveScrollRect, configQuest.Type);
                        achieveItemSlotlist.Add(categoryKey.Value, achieveItemSlot);
                    }

                    var achieveSlot = GameObject.Instantiate(view.achieveSlotPrefab);
                    achieveSlot.transform.SetParent(achieveItemSlot.achieveslotParent, false);
                    achieveSlot.Init(mergedQuest, staticInfo);
                    achieveSlot.SetParentItemSlot(achieveItemSlot);
                    achieveItemSlot.achieveSlotList.Add(achieveSlot);
                }
            }

            // POINTS 업적 슬롯 생성
            CreatePointsAchieveSlots(_pointsDoneList);

            // PLAY_TIME 미션 상태 미리 체크
            await CheckPlayTimeMissions();

            // 업적 슬롯 상태 갱신
            foreach (var achieveItemSlot in achieveItemSlotlist.Values)
            {
                achieveItemSlot.RefreshStatusImages();
            }

            view.dailyScrollView.SetItemCount(_dailyMissionDataList.Count);

            ResetAllScrollPositions();

            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
            
            // 업적 알림 배지 끄기
            CPPlayer.OutGame.newAchievementNotiCallback?.Invoke(false);
            
            view.gameObject.SetActive(true);
            
            currentTabIndex = 0;
            view.tabGroup.SetActiveToggle(0);
            view.SetActiveWindow(0);
        }

        private void CreatePointsAchieveSlots(IEnumerable<PointsDone> pointsDoneList)
        {
            var pointsConfig = ConfigDataManager.points.Where(p => p.PointsType == "ACHIEVEMENTS").ToList();
            if (pointsConfig.Count == 0)
                return;

            var pointsItemSlot = GameObject.Instantiate(view.achieveItemSlotPrefab);
            pointsItemSlot.transform.SetParent(view.achieveScrollRect.content, false);
            pointsItemSlot.Init(pointsConfig[0], view.achieveScrollRect);
            achieveItemSlotlist.Add(AchieveCategory.POINTS, pointsItemSlot);

            int pointsSlotIndex = 0;
            foreach (var configPoints in pointsConfig)
            {
                var achieveSlot = GameObject.Instantiate(view.achieveSlotPrefab);
                achieveSlot.transform.SetParent(pointsItemSlot.achieveslotParent, false);
                achieveSlot.Init(pointsSlotIndex, configPoints);
                achieveSlot.SetParentItemSlot(pointsItemSlot);
                achieveSlot.SetPointsState(CPPlayer.Inventory.myPoints, pointsDoneList);
                pointsItemSlot.achieveSlotList.Add(achieveSlot);
                pointsSlotIndex++;
            }
        }

        private async UniTask CheckPlayTimeMissions()
        {
            var playTimeMissions = _dailyMissionDataList.Where(d => d.Quest.Type == "PLAY_TIME").ToList();
            if (playTimeMissions.Count == 0)
                return;

            var res = await Services.Lobby.UserGameOnReqAsync();
            if (!res.IsSuccess || res.Data == null)
                return;

            _playTimeMinutes = (int)(res.Data.OnSec / 60);
            
            foreach (var mission in playTimeMissions)
            {
                // 이미 완료된 미션은 스킵
                if (mission.Quest.QuestValue >= mission.Quest.MaxCount)
                    continue;
                    
                int maxMinutes = mission.Quest.MaxCount * 60;
                if (_playTimeMinutes >= maxMinutes)
                {
                    var addRes = await Services.Lobby.UserQuestAddAsync(mission.Quest.Type);
                    if (addRes.IsSuccess)
                        mission.Quest.QuestValue = mission.Quest.MaxCount;
                }
            }
        }

        private void OnDailyCellUpdate(GameObject cell, int index)
        {
            if (index < 0 || index >= _dailyMissionDataList.Count)
                return;
            
            var data = _dailyMissionDataList[index];
            var slot = cell.GetComponent<DailyMissionSlot>();
            slot.Init(data.Quest, data.StaticInfo, OnDailyMissionRewardClaimed, _playTimeMinutes);
        }
        
        private void OnDailyMissionRewardClaimed(Quest updatedQuest)
        {
            var targetData = _dailyMissionDataList.FirstOrDefault(d => d.Quest.QuestId == updatedQuest.QuestId);
            if (targetData != null)
                targetData.Quest = updatedQuest;
            
            view.dailyScrollView.SetItemCount(_dailyMissionDataList.Count);
        }

        private void AnimateScrollToStart()
        {
            if (currentTabIndex == 0)
            {
                var rect = view.dailyScrollView.GetComponent<ScrollRect>();
                if (rect.normalizedPosition.y >= 0.99f)
                    return;

                scrollTween?.Kill();

                scrollTween = DOTween.To
                (
                    () => rect.normalizedPosition,
                    pos => rect.normalizedPosition = pos,
                    new Vector2(rect.normalizedPosition.x, 1),
                    0.3f
                ).SetEase(Ease.OutQuad);
            }
            else
            {
                if (view.achieveScrollRect.normalizedPosition.x <= 0.01f)
                    return;

                scrollTween?.Kill();

                scrollTween = DOTween.To(
                    () => view.achieveScrollRect.normalizedPosition,
                    pos => view.achieveScrollRect.normalizedPosition = pos,
                    new Vector2(0, view.achieveScrollRect.normalizedPosition.y),
                    0.3f
                ).SetEase(Ease.OutQuad);
            }
        }

        private void ResetAllScrollPositions()
        {
            view.dailyScrollView.ScrollToTop();
            view.achieveScrollRect.normalizedPosition = new Vector2(0, 1);
        }

        private void StopScrollAnimation()
        {
            scrollTween?.Kill();
            scrollTween = null;
        }
    }
}
