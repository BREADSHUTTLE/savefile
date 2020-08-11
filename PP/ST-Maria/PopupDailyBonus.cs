// PopupDailyBonus.cs - PopupDailyBonus implementation file
//
// Description      : PopupDailyBonus
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2018/11/30
// Last Update      : 2018/12/19
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ST.MARIA.DataPool;

namespace ST.MARIA.Popup
{
    public class PopupDailyBonus : PopupBaseLayout<PopupDailyBonus>
    {
        [SerializeField] private Text[] attendDay;
        [SerializeField] private GameObject[] attendTwoReward;
        [SerializeField] private GameObject[] attendCheck;
        [SerializeField] private Image attendSevenDay;
        [SerializeField] private Image[] attendDayBg;
        [SerializeField] private Image[] attendTodayBg;
        [SerializeField] private Text[] attendDayReward;
        [SerializeField] private Image[] attendActiveIcon;
        [SerializeField] private Image[] attendDefaultIcon;
        [SerializeField] private Text[] attendDayText;
        [SerializeField] private Image[] attendDisableBg;
        [SerializeField] private Image[] attendDisableIcon;
        [SerializeField] private Text[] stempDay;
        [SerializeField] private Image[] stempRewardIcon;
        [SerializeField] private Image stempProgress;
        [SerializeField] private Button okButton;
        [SerializeField] private Button collectButton;
        [SerializeField] private Image progressEffectImg;
        [SerializeField] private float progressEffectPosY;
        [SerializeField] private ParticleSystem buttonEffect;
        [SerializeField] private SoundPlayer checkSound;

        private DailyBonusData dailyBonusData = null;
        private List<DailyBonusData.AttendReward> attendList = new List<DailyBonusData.AttendReward>();
        private List<DailyBonusData.StampReward> stempList = new List<DailyBonusData.StampReward>();
        private PopupReward rewards = null;
        private DateTime lastAttendTime = new DateTime();

        public static PopupDailyBonus Create()
        {
            PopupDailyBonus popup = OnCreate("POPUP-DailyBonus");
            popup.Initialize();
            return popup;
        }

        public void Initialize()
        {
            InitUI();
            SessionLobby.Instance.RequestDailyBonusInfo(() =>
            {
                dailyBonusData = DailyBonusInfo.Instance.DailyBonusDatas;
                if (dailyBonusData == null)
                    return;
                
                SetUI(dailyBonusData);
            });
        }

        private void InitUI()
        {
            foreach (var image in attendDayBg)
                CommonTools.SetActive(image, true);

            foreach (var image in attendTodayBg)
                CommonTools.SetActive(image, false);

            foreach (var check in attendCheck)
                CommonTools.SetActive(check, false);

            foreach (var disableBg in attendDisableBg)
                CommonTools.SetActive(disableBg, false);

            foreach (var disableIcon in attendDisableIcon)
                CommonTools.SetActive(disableIcon, false);

            foreach (var reward in attendTwoReward)
                CommonTools.SetActive(reward, false);

            CommonTools.SetActive(okButton, false);
            CommonTools.SetActive(collectButton, false);

            if (buttonEffect != null)
                buttonEffect.Stop();
        }

        private void SetUI(DailyBonusData data)
        {
            SetAttendList(data.attendRewardList);
            SetStampList(data.stampRewardList);
            SetAttendCheck(data.userAttendInfo);
            SetCollectButton(data.userAttendInfo);
            SetTodayBackground(data.userAttendInfo);
            SetTodayIcon(data.userAttendInfo);
            SetTodayText(data.userAttendInfo);
            SetStempProgressBar(data.userAttendInfo, stempList);
            SetStempRewardIcon();
            DisableAttendImage(data.userAttendInfo);
        }

        private void SetAttendList(List<DailyBonusData.AttendReward> list)
        {
            CommonTools.SetActive(attendSevenDay, false);

            attendList = list.OrderBy(x => x.day).ToList();

            for (int i = 0; i < attendList.Count; i++)
            {
                // 7day 고정
                if (i >= (attendList.Count - 1))
                {
                    CommonTools.SetActive(attendSevenDay, true);
                }
                else
                {
                    if (attendDay[i] != null)
                        attendDay[i].text = attendList[i].day.ToString();
                }

                // 스펙에서 보상 2개 받는 부분이 없음
                if (attendTwoReward[i] != null)
                    CommonTools.SetActive(attendTwoReward[i], false);

                if (attendDayReward[i] != null)
                    attendDayReward[i].text = attendList[i].displayText;    
            }
        }
        
        private void SetAttendCheck(DailyBonusData.UserAttendInfo info)
        {
            if (attendCheck.Length <= 0)
                return;

            bool check = false;
            for (int i = 0; i < attendCheck.Length; i++)
            {
                if (attendCheck.Length < info.attendCount)
                {
                    if (i == attendCheck.Length - 1)
                    {
                        CommonTools.SetActive(attendCheck[i], false);
                    }
                    else
                    {
                        CommonTools.SetActive(attendCheck[i], true);
                        if (!check)
                            check = true;
                    }
                }
                else
                {
                    if (i < (info.attendCount - 1))
                    {
                        CommonTools.SetActive(attendCheck[i], true);
                        if (!check)
                            check = true;
                    }
                    else if (i == (info.attendCount - 1))
                    {
                        bool collect = CheckTodayCollect(info);
                        CommonTools.SetActive(attendCheck[i], collect);
                        if (!check)
                            check = collect;
                    }
                    else
                    {
                        CommonTools.SetActive(attendCheck[i], false);
                    }
                }
            }

            if (check)
                checkSound.Play();
        }

        private void SetTodayBackground(DailyBonusData.UserAttendInfo info)
        {
            if (attendDayBg.Length <= 0 || attendTodayBg.Length <= 0)
                return;

            for (int i = 0; i < attendDayBg.Length; i++)
            {
                if (attendDayBg.Length < info.attendCount)
                {
                    if (i == attendDayBg.Length - 1)
                        CommonTools.SetActive(attendDayBg[i], false);
                    else
                        CommonTools.SetActive(attendDayBg[i], true);
                }
                else
                {
                    if (i == (info.attendCount - 1))
                        CommonTools.SetActive(attendDayBg[i], false);
                    else
                        CommonTools.SetActive(attendDayBg[i], true);
                }
            }

            for (int j = 0; j < attendTodayBg.Length; j++)
            {
                if (attendTodayBg.Length < info.attendCount)
                {
                    if (j == attendTodayBg.Length - 1)
                    {
                        CommonTools.SetActive(attendTodayBg[j], true);
                        if (CheckTodayCollect(info))
                        {
                            foreach (var child in attendTodayBg[j].transform.GetComponentsInChildren<RectTransform>())
                            {
                                if (child != attendTodayBg[j].rectTransform)
                                    CommonTools.SetActive(child, false);
                            }
                        }
                    }
                    else
                    {
                        CommonTools.SetActive(attendTodayBg[j], false);
                    }
                }
                else
                {
                    if (j == (info.attendCount - 1))
                    {
                        CommonTools.SetActive(attendTodayBg[j], true);
                        if (CheckTodayCollect(info))
                        {
                            foreach (var child in attendTodayBg[j].transform.GetComponentsInChildren<RectTransform>())
                            {
                                if (child != attendTodayBg[j].rectTransform)
                                    CommonTools.SetActive(child, false);
                            }
                        }
                    }
                    else
                    {
                        CommonTools.SetActive(attendTodayBg[j], false);
                    }
                }
            }
        }

        private void SetTodayIcon(DailyBonusData.UserAttendInfo info)
        {
            if (attendActiveIcon.Length <= 0 || attendDefaultIcon.Length <= 0)
                return;

            for (int i = 0; i < attendActiveIcon.Length; i++)
            {
                if (attendActiveIcon.Length < info.attendCount)
                {
                    if (i == attendActiveIcon.Length - 1)
                        CommonTools.SetActive(attendActiveIcon[i], true);
                    else
                        CommonTools.SetActive(attendActiveIcon[i], false);
                }
                else
                {
                    if (i == (info.attendCount - 1))
                        CommonTools.SetActive(attendActiveIcon[i], true);
                    else
                        CommonTools.SetActive(attendActiveIcon[i], false);
                }
            }

            for (int j = 0; j < attendDefaultIcon.Length; j++)
            {
                if (attendDefaultIcon.Length < info.attendCount)
                {
                    if (j == attendDefaultIcon.Length - 1)
                        CommonTools.SetActive(attendDefaultIcon[j], false);
                    else
                        CommonTools.SetActive(attendDefaultIcon[j], true);
                }
                else
                {
                    if (j == (info.attendCount - 1))
                        CommonTools.SetActive(attendDefaultIcon[j], false);
                    else
                        CommonTools.SetActive(attendActiveIcon[j], true);
                }
            }
        }

        private void SetTodayText(DailyBonusData.UserAttendInfo info)
        {
            if (attendDayText.Length <= 0)
                return;

            bool today = false;
            for (int i = 0; i < attendDayText.Length; i++)
            {
                if (attendDayText.Length < info.attendCount)
                {
                    if (i == attendDayText.Length - 1)
                    {
                        today = true;
                        attendDayText[i].text = "TODAY";
                    }
                    else
                    {
                        attendDayText[i].text = "DAY";
                    }
                }
                else
                {
                    if (i == (info.attendCount - 1))
                    {
                        today = true;
                        attendDayText[i].text = "TODAY";
                    }
                    else
                    {
                        attendDayText[i].text = "DAY";
                    }

                    var outline = attendDayText[i].gameObject.GetComponent<Outline>();
                    if (outline != null)
                        outline.effectColor = today ? new Color32(75, 31, 14, 170) : new Color32(30, 48, 56, 170);
                }
            }
        }

        private void DisableAttendImage(DailyBonusData.UserAttendInfo info)
        {
            if (attendDisableBg.Length <= 0 || attendDisableIcon.Length <= 0)
                return;

            for (int i = 0; i < attendDisableBg.Length; i++)
            {
                if (attendDisableBg.Length < info.attendCount)
                {
                    if (i == attendDisableBg.Length - 1)
                    {
                        CommonTools.SetActive(attendDisableBg[i], false);
                        CommonTools.SetActive(attendDisableIcon[i], false);
                    }
                    else
                    {
                        CommonTools.SetActive(attendDisableBg[i], true);
                        CommonTools.SetActive(attendDisableIcon[i], true);
                    }
                }
                else
                {
                    if (i < (info.attendCount - 1))
                    {
                        CommonTools.SetActive(attendDisableBg[i], true);
                        CommonTools.SetActive(attendDisableIcon[i], true);
                    }
                    else
                    {
                        CommonTools.SetActive(attendDisableBg[i], false);
                        CommonTools.SetActive(attendDisableIcon[i], false);
                    }
                }
            }
        }

        private void SetStampList(List<DailyBonusData.StampReward> list)
        {
            stempList = list.OrderBy(x => x.day).ToList();

            for (int i = 0; i < stempList.Count; i++)
            {
                if (stempDay[i] != null)
                    stempDay[i].text = stempList[i].day.ToString() + "th";
            }
        }

        private void SetStempRewardIcon()
        {
            for (int i = 0; i < stempList.Count; i++)
            {
                if (stempRewardIcon[i] != null)
                {
                    CommonTools.SetActive(stempRewardIcon[i], true);
                    stempRewardIcon[i].sprite = Resources.Load<Sprite>("POPUP-DailyBonus/Assets/MYSTRIP/" + stempList[i].structureId.Replace(":", ""));
                    stempRewardIcon[i].SetNativeSize();
                }
            }
        }

        private void SetStempProgressBar(DailyBonusData.UserAttendInfo info, List<DailyBonusData.StampReward> list)
        {
            if (stempProgress == null)
                return;

            if (list.Count <= 0)
                return;

            float ratio = (float)info.stampCount / (float)list[(list.Count - 1)].day;
            stempProgress.fillAmount = ratio;

            if (progressEffectImg == null)
                return;

            float range = (float)progressEffectPosY / (float)list[(list.Count - 1)].day;
            float cur = (float)info.stampCount * (float)range;

            progressEffectImg.rectTransform.anchoredPosition = new Vector2(0f, cur);
        }

        private void SetCollectButton(DailyBonusData.UserAttendInfo info)
        {
            if (okButton == null || collectButton == null)
                return;

            CommonTools.SetActive(okButton, CheckTodayCollect(info));
            CommonTools.SetActive(collectButton, !CheckTodayCollect(info));
        }

        private bool CheckTodayCollect(DailyBonusData.UserAttendInfo info)
        {
            if (info.lastAttendDate <= 0)
                return false;

            DateTime attendTime = CommonTools.JavaTime(info.lastAttendDate);
            DateTime pstAttendTime = CommonTools.ToPSTTime(attendTime);
            Log("PST Attend Time : " + CommonTools.ToPSTTime(attendTime));
            lastAttendTime = pstAttendTime;

            DateTime lastTime = new DateTime(pstAttendTime.Year, pstAttendTime.Month, pstAttendTime.Day, 0, 0, 0);

            DateTime pstServerTime = CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
            Log("PST ServerTime : " + CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime));

            DateTime serverTime = new DateTime(pstServerTime.Year, pstServerTime.Month, pstServerTime.Day, 0, 0, 0);

            TimeSpan span = serverTime - lastTime;
            Log("span.Days : " + span.Days);
            return span.Days <= 0;
        }

        private void OpenRewardPopup()
        {
            rewards = PopupReward.Create(RewardGroup.DailyBonus);
            if (rewards != null)
                rewards.ActionClose = () => OpenRewardPopup();
        }

        private void SaveCollectTime(DateTime collectTime)
        {
            ST.MARIA.PlayerPrefs.SetString("DailyBonusCollectTime:" + MyInfo.Instance.GSN, collectTime.Ticks.ToString());
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (Application.isPlaying == true)
            {

            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Application.isPlaying == true)
            {

            }
        }

        public override bool OnKeyUpEscape()
        {
            return false;
        }

        public void OnClickOK()
        {
            if (buttonEffect != null)
                buttonEffect.Play();

            string time = ST.MARIA.PlayerPrefs.GetString("DailyBonusCollectTime:" + MyInfo.Instance.GSN);
            if (string.IsNullOrEmpty(time))
                SaveCollectTime(lastAttendTime);
            
            base.Close();
        }

        public void OnClickCollect()
        {
            if (buttonEffect != null)
                buttonEffect.Play();

            PopupLoading.Show(true);
            SessionLobby.Instance.RequestDailyBonusCollect((complete) => 
            {
                if (complete.result)
                {
                    SessionLobby.Instance.RequestReward("DailyBonus", (reward) =>
                    {
                        PopupLoading.Show(false);
                        SaveCollectTime(CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime));
                        base.Close();
                        OpenRewardPopup();
                    });
                }
            });
        }
    }
}