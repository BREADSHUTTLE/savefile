// PopupMission.cs - PopupMission implementation file
//
// Description      : PopupMission
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/07/11
// Last Update      : 2019/09/20
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dorothy.DataPool;
using UnityEngine.SceneManagement;

namespace Dorothy.UI.Popup
{
    public sealed class PopupMission : PopupBase
    {
        public enum Group
        {
            One = 1,
            Two,
            Three,
        }

        [Serializable] public class RewardPopupFX
        {
            public Animation target = null;
            public AnimationClip show = null;
            public AnimationClip close = null;
        }

        [Serializable] public class RewardTextFX
        {
            public Animation target = null;
            public AnimationClip textOn = null;
            public AnimationClip textOff = null;
            public AnimationClip textLastOff = null;
        }

        [SerializeField] private RewardPopupFX rewardPopupFX = new RewardPopupFX();
        [SerializeField] private RewardTextFX rewardTextFX = new RewardTextFX();
        [SerializeField] private Text curMissionTime;
        [SerializeField] private Text allClearMissionTime;
        [SerializeField] private MissionItem[] missionItem;
        [SerializeField] private Text weeklyMissionTime;
        [SerializeField] private Image weeklyGauge;
        [SerializeField] private Image weeklyGoods1;
        [SerializeField] private Image weeklyGoods2;
        [SerializeField] private Image weeklyGoods1Reward;
        [SerializeField] private Image weeklyGoods2Reward;
        [SerializeField] private GameObject rewardPopup;

        [SerializeField] private Image rewardBoxImage;
        [SerializeField] private Image rewardBoomBoosterImage;
        [SerializeField] private Image rewardFasterBoosterImage;
        [SerializeField] private Image rewardBoomFasterBoosterImage;

        [SerializeField] private Text rewardText;
        [SerializeField] private GameObject rewardBooster;
        [SerializeField] private Text rewardBoosterText1;
        [SerializeField] private Text rewardBoosterText2;
        [SerializeField] private RectTransform layoutGroup;
        [SerializeField] private GameObject allClearObj;
        [SerializeField] private GameObject missionTimeObj;
        [SerializeField] private Button weeklyCollect;
        [SerializeField] private Text weeklyNormalText;
        [SerializeField] private Text weeklyCollectText;
        [SerializeField] private Text weeklyPointText;
        [SerializeField] private SoundPlayer rewardPopupSound0;
        [SerializeField] private SoundPlayer rewardPopupSound1;
        [SerializeField] private CoinMoveController coinController;
        [SerializeField] private GameObject infoPopup;
        [SerializeField] private Text infoText;

        public Action ActionTimeout = null;
		public Action ActionUpdate = null;

		private MissionData data = null;
        private Coroutine curDailyTimeCoroutine = null;
        private Coroutine weeklyTimeCoroutine = null;
        private Coroutine rewardCloseCoroutine = null;
        private Coroutine rewardTextCoroutine = null;
        
        private Group popupGroup = Group.One;
        private MissionRewardType rewardType = MissionRewardType.None;
        private MissionType missionType = MissionType.None;
        private long rewardCoin = 0;
        private long rewardPoint = 0;
        private Dictionary<string, long> rewardEffect = new Dictionary<string, long>();
        private bool isAllClear = false;
        

        public static PopupOption<PopupMission> Open(Action updateCallback = null, string how = "", float timeout = 0, int order = 0, float backAlpha = 1f)
        {
            var option = PopupSystem.Instance.Register<PopupMission>("popup", "POPUP_Mission", order, backAlpha).OnInitialize(p => p.Initialize(updateCallback, timeout)).SelfActive();
            return option;
        }

        public void Initialize(Action updateCallback, float timeout)
        {
            CancelInvoke("OnTimeout");

            ActionTimeout = null;
			ActionUpdate = updateCallback;

			if (timeout > 0)
                Invoke("OnTimeout", timeout);

            CommonTools.SetActive(rewardPopup, false);
            CommonTools.SetActive(infoPopup, false);
            SetData();
        }

        protected override void OnEnable()
        {
            if (Application.isPlaying)
            {
                MissionInfo.Instance.ActionUpdateDailyGoodsList += SetData;
                MissionInfo.Instance.ActionUpdateWeeklyGoodsList += SetData;
            }
        }

        protected override void OnDisable()
        {
            if (Application.isPlaying)
            {
                MissionInfo.Instance.ActionUpdateDailyGoodsList -= SetData;
                MissionInfo.Instance.ActionUpdateWeeklyGoodsList -= SetData;
            }
        }

        private void Update()
        {
        }

        private void SetData()
        {
            SessionLobby.Instance.RequestMission((e) =>
            {
                if (e != null)
                {
                    //base.Close();
                    PopupSystem.Instance.ForceTerminatePopup(this);
                    MissionInfo.Instance.ActiveLobbyIcon = false;
                    return;
                }

                data = MissionInfo.Instance.MissionData;
                if (data == null)
                {
                    PopupSystem.Instance.ForceTerminatePopup(this);
                    MissionInfo.Instance.ActiveLobbyIcon = false;
                    return;
                }

                string curScene = SceneManager.GetActiveScene().name;
                UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.freecoin, UserTracking.EventWhat.mission, "M",
                                curScene == Constants.SCENE_LOBBY ? UserTracking.Location.lobby_main : UserTracking.Location.ingame);

                MissionInfo.Instance.ActiveLobbyIcon = true;
                gameObject.SetActive(true);

                SetUI();
                SetTime();
                SetWeeklyMissionGoodsPos();

                keyUpEscape = true;
            });
        }

        private void SetUI()
        {
            SetList();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup);
            SetWeeklyMission();
        }

        private void SetList()
        {
            if (data.missionStatus == null)
                return;

            if (data.currentMission == null)
            {
                //TODO:: 올클리어
                isAllClear = true;
                CommonTools.SetActive(allClearObj, true);
                CommonTools.SetActive(missionTimeObj, false);
            }
            else
            {
                if (CheckAllMissionClear())
                {
                    //TODO:: 올클리어
                    isAllClear = true;
                    CommonTools.SetActive(allClearObj, true);
                    CommonTools.SetActive(missionTimeObj, false);
                }
                else
                {
                    isAllClear = false;
                    CommonTools.SetActive(allClearObj, false);
                    CommonTools.SetActive(missionTimeObj, true);

                    var missionList = MissionInfo.Instance.Sort(data.missionStatus);
                    for (int i = 0; i < missionItem.Length; ++i)
                    {
                        if (i >= missionList.Count)
                            missionItem[i].Build(null, ActionUpdate);
                        else
                            missionItem[i].Build(missionList[i], ActionUpdate);
                    }
                }
            }
        }

        private bool CheckAllMissionClear()
        {
            for (int i = 0; i < missionItem.Length; ++i)
            {
                if (data.missionStatus[i].receivedReward == false)
                    return false;
            }

            return true;
        }

        public void SetWeeklyMission()
        {
            if (data.missionSet == null)
                return;

            var dataSet = data.missionSet;

            if (weeklyGauge != null)
                weeklyGauge.fillAmount = (float)dataSet.userPoint / (float)dataSet.totalPoint;

            bool collect1 = data.missionSet.receivedSetReward1 ? false : data.missionSet.userPoint >= data.missionSet.reward1Point ? true : false;
            bool collect2 = data.missionSet.receivedSetReward2 ? false : data.missionSet.userPoint >= data.missionSet.reward2Point ? true : false;

            bool collect = collect1 ? true : collect2 ? true : false;

            if (weeklyCollect != null)
                CommonTools.SetActive(weeklyCollect, collect);

            if (data.missionSet.receivedSetReward1 && data.missionSet.receivedSetReward2)
            {
                if (weeklyCollectText != null)
                    CommonTools.SetActive(weeklyCollectText, true);

                if (weeklyNormalText != null)
                    CommonTools.SetActive(weeklyNormalText, false);
            }
            else
            {
                if (weeklyCollectText != null)
                    CommonTools.SetActive(weeklyCollectText, false);

                if (weeklyNormalText != null)
                    CommonTools.SetActive(weeklyNormalText, true);
            }
 
            if (weeklyPointText != null)
                weeklyPointText.text = string.Format("{0}/{1}", dataSet.userPoint, dataSet.totalPoint);

            if (weeklyGoods1Reward != null)
            {
                CommonTools.SetActive(weeklyGoods1Reward, dataSet.receivedSetReward1);
                if (weeklyGoods1 != null)
                {
                    if (dataSet.receivedSetReward1)
                        weeklyGoods1.color = Color.gray;
                    else
                        weeklyGoods1.color = Color.white;
                }
            }

            if (weeklyGoods2Reward != null)
            {
                CommonTools.SetActive(weeklyGoods2Reward, dataSet.receivedSetReward2);
                if (weeklyGoods2 != null)
                {
                    if (dataSet.receivedSetReward2)
                        weeklyGoods2.color = Color.gray;
                    else
                        weeklyGoods2.color = Color.white;
                }
            }
        }

        private void SetWeeklyMissionGoodsPos()
        {
            float gaugeWidth = weeklyGauge.rectTransform.rect.width;
            float point = 0;

            if (weeklyGoods1 != null)
            {
                //TODO :: 여기에 보상 정보 오면 업데이트 합니다.  
                point = (float)data.missionSet.reward1Point;
                float fill = (float)point / (float)data.missionSet.totalPoint;

                float pos = gaugeWidth * fill;
                weeklyGoods1.rectTransform.anchoredPosition = new Vector2(pos, 0f);
            }

            if (weeklyGoods2 != null)
            {
                //TODO :: 여기에 보상 정보 오면 업데이트 합니다.
                point = data.missionSet.reward2Point;
                var fill = point / data.missionSet.totalPoint;

                var pos = gaugeWidth * fill;
                weeklyGoods2.rectTransform.anchoredPosition = new Vector2(pos, 0f);
            }
        }

        private void SetTime()
        {
            //TODO:: 타임정보
            if (weeklyTimeCoroutine != null)
                StopCoroutine(weeklyTimeCoroutine);

            if (weeklyMissionTime != null)
                weeklyTimeCoroutine = StartCoroutine("WeeklyDisplayTime", CommonTools.JavaTime(data.missionSet.endDate));

            if (curDailyTimeCoroutine != null)
                StopCoroutine(curDailyTimeCoroutine);

            if (data.currentMission != null)
            {
                if (curMissionTime != null)
                    curDailyTimeCoroutine = StartCoroutine("DailyDisplayTime", ClientInfo.Instance.ServerTime.AddSeconds(data.currentMission.leftSecond));
            }
            else
            {
                if (allClearMissionTime != null)
                {
                    var dailyTime = CommonTools.JavaTime(data.missionSet.dailyEndDate);
                    curDailyTimeCoroutine = StartCoroutine("DailyDisplayTime", dailyTime);
                }
            }
        }
        
        private IEnumerator DailyDisplayTime(DateTime curTime)
        {
            while(true)
            {
                TimeSpan timeout = CommonTools.ToPSTTime(curTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
                if (timeout >= TimeSpan.Zero)
                {
                    curMissionTime.text = string.Format(LocalizationSystem.Instance.Localize("DAILYQUEST.Daily.ValidTime"), CommonTools.DisplayDigitalTime(timeout));
                    allClearMissionTime.text = string.Format(LocalizationSystem.Instance.Localize("DAILYQUEST.AllMissionsComplete.ValidTime"), CommonTools.DisplayDigitalTime(timeout));
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(3f);

                    SetData();

                    yield break;
                }
            }
        }

        private IEnumerator WeeklyDisplayTime(DateTime curTime)
        {
            while (true)
            {
                TimeSpan timeout = CommonTools.ToPSTTime(curTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
                if (timeout >= TimeSpan.Zero)
                {
                    if (timeout.TotalDays < 1)
                        weeklyMissionTime.text = string.Format(LocalizationSystem.Instance.Localize("DAILYQUEST.Weekly.ValidTime"), timeout.Hours, timeout.Minutes);
                    else
                        weeklyMissionTime.text = string.Format(LocalizationSystem.Instance.Localize("DAILYQUEST.WeeklyChallenge.ValidTime"), Math.Ceiling(timeout.TotalDays));

                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    yield return new WaitForSeconds(4f);

                    SetData();

                    yield break;
                }
            }
        }

        public void OpenRewardPopup(long coin, long point, MissionType mType, Group group, MissionRewardType rType)
        {
            popupGroup = group;
            rewardType = rType;
            rewardCoin = coin;
            rewardPoint = point;
            missionType = mType;

            //TODO :: 나중에 아이콘 생기면 작업 ㄱㄱ
            //TODO :: 언어팩 작업도 필요
            //CommonTools.SetActive(rewardIcon, false);
            CommonTools.SetActive(rewardPopup, true);
            CommonTools.SetActive(rewardBooster, false);

            if (group == Group.One)
            {
                rewardPopupFX.target.Play(rewardPopupFX.show.name);

                if (rewardPoint > 0)
                {
                    if (rewardPopupSound1 != null)
                        rewardPopupSound1.Play();
                }
                else
                {
                    OpenRewardPopup(rewardCoin, rewardPoint, missionType, popupGroup + 1, rewardType);
                    return;
                }
            }
            else if (group == Group.Two)
            {
                if (rewardCoin > 0)
                {
                    if (rewardPopupSound1 != null)
                        rewardPopupSound1.Play();
                }
                else
                {
                    OpenRewardPopup(rewardCoin, rewardPoint, missionType, popupGroup + 1, rewardType);
                    return;
                }
            }
            else if (group == Group.Three)
            {
                if (rType == MissionRewardType.CoinAndBooster)
                {
                    ShowRewardEffect();
                }
                else
                {
                    OpenRewardPopup(rewardCoin, rewardPoint, missionType, popupGroup + 1, rewardType);
                    return;
                }
            }
            else
            {
                if (rewardCloseCoroutine != null)
                    StopCoroutine(rewardCloseCoroutine);

                rewardCloseCoroutine = StartCoroutine("CloseRewardPopup");

                return;
            }

            if (group <= Group.Three)
            {
                if (rewardTextCoroutine != null)
                    StopCoroutine(rewardTextCoroutine);

                rewardTextCoroutine = StartCoroutine("ShowRewardInfo", group);
            }
        }

        private IEnumerator ShowRewardInfo(Group group)
        {
            if (group == Group.One)
            {
                SetEffectRewardIcon(group, string.Empty);
                
                CommonTools.SetActive(rewardText, true);
                rewardTextFX.target.Play(rewardTextFX.textOn.name);
                rewardText.text = string.Format(LocalizationSystem.Instance.Localize("DAILYQUEST.Popup.Reward.Description.3"), rewardPoint);

                yield return new WaitForSeconds(2f);

                OpenRewardPopup(rewardCoin, rewardPoint, missionType, popupGroup + 1, rewardType);

                yield break;
            }

            rewardTextFX.target.Play(rewardTextFX.textOff.name);

            yield return new WaitForSeconds(rewardTextFX.textOff.length);

            rewardTextFX.target.Play(rewardTextFX.textOn.name);

            if (group == Group.Two)
            {
                SetEffectRewardIcon(group, string.Empty);

                CommonTools.SetActive(rewardText, true);
                rewardText.text = string.Format(LocalizationSystem.Instance.Localize("DAILYQUEST.Popup.Reward.Description.4"), rewardCoin);
            }
            else if (group == Group.Three)
            {
                CommonTools.SetActive(rewardText, false);
                CommonTools.SetActive(rewardBooster, true);
                var first = rewardEffect.First();
                string key = first.Key;

                SetEffectRewardIcon(group, key);

                SetEffectRewardText(key);
                rewardBoosterText2.text = string.Format(LocalizationSystem.Instance.Localize("DAILYQUEST.Popup.Reward.Description.Time"), CommonTools.GetEffectPeriodTime(rewardEffect[key]));
            }

            yield return new WaitForSeconds(2f);

            OpenRewardPopup(rewardCoin, rewardPoint, missionType, popupGroup + 1, rewardType);

            rewardTextCoroutine = null;
        }

        private IEnumerator CloseRewardPopup()
        {
            rewardPopupFX.target.Play(rewardPopupFX.close.name);
            rewardTextFX.target.Play(rewardTextFX.textLastOff.name);
            
            yield return new WaitForSeconds(rewardPopupFX != null ? rewardPopupFX.close.length : 0f);

            CommonTools.SetActive(rewardPopup, false);

            rewardCloseCoroutine = null;
        }

        private void ShowRewardEffect()
        {
            rewardEffect.Clear();

            if (missionType == MissionType.Daily)
                rewardEffect = MissionInfo.Instance.GetDailyRewardEffect();
            else if (missionType == MissionType.Weekly)
                rewardEffect = MissionInfo.Instance.GetWeeklyRewardEffect();

            if (rewardEffect.Count <= 0)
            {
                OpenRewardPopup(rewardCoin, rewardPoint, missionType, popupGroup + 1, rewardType);
                return;
            }

            var first = rewardEffect.First();
            string key = first.Key;

            if (rewardPopupSound1 != null)
                rewardPopupSound1.Play();

            if (missionType == MissionType.Daily)
                MissionInfo.Instance.RemoveDailyRewardEffect(key);
            else if (missionType == MissionType.Weekly)
                MissionInfo.Instance.RemoveWeeklyRewardEffect(key);
        }

        private void SetEffectRewardText(string code)
        {
            switch (UserInfo.Instance.GetEffectType(code))
            {
                case UserInfo.EffectType.Boom:
                    rewardBoosterText1.text = LocalizationSystem.Instance.Localize("DAILYQUEST.Popup.Reward.Description.5");
                    break;
                case UserInfo.EffectType.Faster:
                    rewardBoosterText1.text = LocalizationSystem.Instance.Localize("DAILYQUEST.Popup.Reward.Description.6");
                    break;
                case UserInfo.EffectType.Turbo:
                    rewardBoosterText1.text = LocalizationSystem.Instance.Localize("DAILYQUEST.Popup.Reward.Description.7");
                    break;
                case UserInfo.EffectType.BoomFaster:
                    rewardBoosterText1.text = LocalizationSystem.Instance.Localize("DAILYQUEST.Popup.Reward.Description.8");
                    break;
                default:
                    break;
            }
        }

        private void SetEffectRewardIcon(Group group, string code)
        {
            if (group == Group.One || group == Group.Two)
            {
                CommonTools.SetActive(rewardBoxImage, true);

                CommonTools.SetActive(rewardBoomBoosterImage, false);
                CommonTools.SetActive(rewardFasterBoosterImage, false);
                CommonTools.SetActive(rewardBoomFasterBoosterImage, false);
            }
            else if (group == Group.Three)
            {
                CommonTools.SetActive(rewardBoxImage, false);

                switch (UserInfo.Instance.GetEffectType(code))
                {
                    case UserInfo.EffectType.Boom:
                        CommonTools.SetActive(rewardBoomBoosterImage, true);
                        CommonTools.SetActive(rewardFasterBoosterImage, false);
                        CommonTools.SetActive(rewardBoomFasterBoosterImage, false);

                        break;
                    case UserInfo.EffectType.Faster:
                        CommonTools.SetActive(rewardBoomBoosterImage, false);
                        CommonTools.SetActive(rewardFasterBoosterImage, true);
                        CommonTools.SetActive(rewardBoomFasterBoosterImage, false);

                        break;
                    case UserInfo.EffectType.BoomFaster:
                        CommonTools.SetActive(rewardBoomBoosterImage, false);
                        CommonTools.SetActive(rewardFasterBoosterImage, false);
                        CommonTools.SetActive(rewardBoomFasterBoosterImage, true);

                        break;
                    default:
                        CommonTools.SetActive(rewardBoxImage, true);

                        CommonTools.SetActive(rewardBoomBoosterImage, false);
                        CommonTools.SetActive(rewardFasterBoosterImage, false);
                        CommonTools.SetActive(rewardBoomFasterBoosterImage, false);

                        break;
                }
            }
        }

        public void OnClickWeeklyRewardCollect()
        {
            Log();

            //TODO :: 첫번째 보상 포인트 이상이 아닌 이상 받을 수 없다.
            if (data.missionSet.userPoint < data.missionSet.reward1Point)
                return;

            weeklyCollect.interactable = false;

            SessionLobby.Instance.RequestMissionWeeklyReward(() => 
            {
                long totalCoin = MissionInfo.Instance.GetTotalWeeklyRewardCoin();
                var effectDic = MissionInfo.Instance.GetWeeklyRewardEffect();

                SetWeeklyMission();
                OpenRewardPopup(totalCoin,
                                0,
                                MissionType.Weekly,
                                Group.One,
                                effectDic.Count > 0 ? MissionRewardType.CoinAndBooster : MissionRewardType.Coin);

                weeklyCollect.interactable = true;
            });
        }

        public void OnClickInfo()
        {
            //TODO:: info 팝업 필요
            if (infoPopup.activeSelf == false)
            {
                CommonTools.SetActive(infoPopup, true);

                if (infoText != null)
                    infoText.text = LocalizationSystem.Instance.Localize("DAILYQUEST.Popup.Informaion.Description");
            }
            else
            {
                CommonTools.SetActive(infoPopup, false);
            }
        }

        public void OnClickClose()
        {
            //SessionLobby.Instance.RequestUser(() =>
            UserInfo.Instance.UpdateUserCoin(() =>
            {
                base.Close();
            });
        }
    }
}