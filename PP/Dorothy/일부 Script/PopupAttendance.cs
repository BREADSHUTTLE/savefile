// PopupAttendance.cs - PopupAttendance implementation file
//
// Description      : PopupAttendance
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/11/27
// Last Update      : 2019/11/27
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dorothy.DataPool;
using Dorothy.Network;

namespace Dorothy.UI.Popup
{
    public class PopupAttendance : PopupBase
    {
        private enum AttendState
        {
            None,
            TodayReady,
            TodayWait,
            Tomorrow,
            ReciveTomorrow,
        }

        [SerializeField] private List<AttendItem> items;
        [SerializeField] private Text timer;
        [SerializeField] private Image timerBackground;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform spotiLight;
        //[SerializeField] private CoinMoveController coinController;

        [Serializable] public class AttendanceAnimation
        {
            public Animation target;
            public AnimationClip open;
            public AnimationClip idle;
        }

        [Serializable] public class SpotiLightAnimation
        {
            public Animation target;
            public AnimationClip spotiLightOn;
            public AnimationClip spotiLightOff;
            public AnimationClip spotiLightIdle;
        }

        [SerializeField] private AttendanceAnimation attendAnimation = new AttendanceAnimation();
        [SerializeField] private SpotiLightAnimation spotiLightAnimation = new SpotiLightAnimation();

        [SerializeField] private SoundPlayer introSound;
        [SerializeField] private SoundPlayer coinSound;
        [SerializeField] private SoundPlayer tomorrowSound;
        [SerializeField] private SoundPlayer lastTomorrowSound;

        private Action ActionTimeout = null;
        private Action ActionClose = null;

        private Coroutine coroutineAttend = null;
        private Coroutine coroutineTimer = null;
        private Coroutine coroutineTomorrow = null;
        private AnswerAttendance data = null;

        private AttendState attendState = AttendState.None;

        private AttendItem todayItem = null;
        private AttendItem tomorrowItem = null;

        public static PopupOption<PopupAttendance> Open(Action close, float timeout = 0, int order = 0, float backAlpha = 0.8f)
        {
            var option = PopupSystem.Instance.Register<PopupAttendance>("popup", "POPUP_Attendance", order, backAlpha).OnInitialize(p => p.Initialize(close, timeout)).SelfActive();
            return option;
        }

        public void Initialize(Action close, float timeout)
        {
            CancelInvoke("OnTimeout");

            ActionTimeout = null;
            ActionClose = close;

            if (timeout > 0)
                Invoke("OnTimeout", timeout);

            SetData();
        }

        private void SetData()
        {
            if (coroutineAttend != null)
                StopCoroutine(coroutineAttend);

            if (coroutineTimer != null)
                StopCoroutine(coroutineTimer);

            if (coroutineTomorrow != null)
                StopCoroutine(coroutineTomorrow);

            SessionLobby.Instance.RequestAttendance((e) =>
            {
                if (e != null)
                {
                    Error("Error Request Attendance..");
                }
                else
                {
                    data = AttendInfo.Instance.AttendanceBonusResultData;
                    if (data == null)
                        Error("Null attend data");

                    SetPush();

                    attendState = data.result ? AttendState.TodayReady : AttendState.ReciveTomorrow;

                    Init();

                    SetActive(false);
                    keyUpEscape = false;

                    Open();
                }
            });
        }
        
        private void SetPush()
        {
            LocalNotification.Instance.CancelPushMessage(LocalNotification.PushMessageType.Daily);

            DateTime bonusTime = CommonTools.JavaTime(data.bonusNextDate);
            TimeSpan span = CommonTools.ToPSTTime(bonusTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);

            double totalSeconds = span.TotalSeconds;

            LocalNotification.Instance.AddPushMessage(LocalNotification.PushMessageType.Daily, "<b>SLOTODAY</b>", "Daily Bonus! Once a day! Collect today's Free Coins and get more tomorrow", (long)totalSeconds);
        }

        private void Init()
        {
            if (timer != null)
                CommonTools.SetActive(timer, false);

            if (timerBackground != null)
                CommonTools.SetActive(timerBackground, false);

            if (closeButton != null)
                CommonTools.SetActive(closeButton, false);

            //if (spotiLight != null)
            //    CommonTools.SetActive(spotiLight, false);

            SetItem();
        }

        private void SetItem()
        {
            if (coroutineAttend != null)
            {
                StopCoroutine(coroutineAttend);
                coroutineAttend = null;
            }

            if (coroutineTimer != null)
            {
                StopCoroutine(coroutineTimer);
                coroutineTimer = null;
            }

            if (coroutineTomorrow != null)
            {
                StopCoroutine(coroutineTomorrow);
                coroutineTomorrow = null;
            }

            if (items.Count <= 0)
                return;

            AttendItem.ItemState itemState = AttendItem.ItemState.None;

            if (attendState == AttendState.TodayReady)
            {
                for (int i = 1; i <= items.Count; ++i)
                {
                    if (i == data.attendedDay && i == data.attendedDayBefore)
                    {
                        itemState = AttendItem.ItemState.TodayReceive;
                    }
                    else
                    {
                        itemState = i == data.attendedDay ? AttendItem.ItemState.TodayReceive :
                                    i <= data.attendedDayBefore ? AttendItem.ItemState.Received : AttendItem.ItemState.None;
                    }

                    items[i - 1].Build(itemState, data.bonusGoodsByDay[i]);
                }

                todayItem = items[data.attendedDay - 1];

                RectTransform itemRect = items[data.attendedDay - 1].GetComponent<RectTransform>();
                //RectTransform coinEffectRect = coinController.GetComponent<RectTransform>();

                //coinEffectRect.anchoredPosition = itemRect.anchoredPosition;

                if (spotiLight != null)
                {
                    spotiLight.anchoredPosition = itemRect.anchoredPosition;
                    CommonTools.SetActive(spotiLight, true);
                    if (spotiLightAnimation.spotiLightOn != null)
                    {
                        spotiLightAnimation.target.PlayQueued(spotiLightAnimation.spotiLightOn.name, QueueMode.CompleteOthers);
                        spotiLightAnimation.target.PlayQueued(spotiLightAnimation.spotiLightIdle.name, QueueMode.CompleteOthers);
                    }
                }
            }
            else if (attendState == AttendState.Tomorrow || attendState == AttendState.ReciveTomorrow)
            {
                Log("Tomorrow State");

                for (int i = 1; i <= items.Count; ++i)
                {
                    if (i == items.Count)
                    {
                        if ((data.attendedDay + 1) > items.Count)
                        {
                            itemState = AttendItem.ItemState.TomorrowGiveLast;
                        }
                        else
                        {
                            itemState = i == (data.attendedDay + 1) ? AttendItem.ItemState.TomorrowGive :
                                    i <= data.attendedDay ? AttendItem.ItemState.Received : AttendItem.ItemState.None;
                        }
                    }
                    else
                    {
                        itemState = i == (data.attendedDay + 1) ? AttendItem.ItemState.TomorrowGive :
                                    i <= data.attendedDay ? AttendItem.ItemState.Received : AttendItem.ItemState.None;
                    }

                    items[i - 1].Build(itemState, data.bonusGoodsByDay[i]);
                }

                if ((data.attendedDay + 1) > items.Count)
                    tomorrowItem = items[items.Count - 1];
                else
                    tomorrowItem = items[data.attendedDay];
            }
        }

        private void Open()
        {
            if (introSound != null)
                introSound.Play();

            if (attendState == AttendState.ReciveTomorrow)
            {
                coroutineTimer = StartCoroutine("OnNextAttendTimer", CommonTools.JavaTime(data.bonusNextDate));

                if (timer != null)
                    CommonTools.SetActive(timer, true);

                if (timerBackground != null)
                    CommonTools.SetActive(timerBackground, true);

                RectTransform itemRect = null;
                if ((data.attendedDay + 1) > items.Count)
                    itemRect = items[items.Count - 1].GetComponent<RectTransform>();
                else
                    itemRect = items[data.attendedDay].GetComponent<RectTransform>();

                if (spotiLight != null)
                {
                    spotiLight.anchoredPosition = itemRect.anchoredPosition;
                    CommonTools.SetActive(spotiLight, true);

                    if (spotiLightAnimation.spotiLightOn != null)
                    {
                        spotiLightAnimation.target.PlayQueued(spotiLightAnimation.spotiLightOn.name, QueueMode.CompleteOthers);
                        spotiLightAnimation.target.PlayQueued(spotiLightAnimation.spotiLightIdle.name, QueueMode.CompleteOthers);
                    }
                }

                if (closeButton != null)
                    CommonTools.SetActive(closeButton, true);

                keyUpEscape = true;
            }
            else
            {
                if (attendAnimation.target != null)
                {
                    attendAnimation.target.PlayQueued(attendAnimation.open.name, QueueMode.CompleteOthers);
                    attendAnimation.target.PlayQueued(attendAnimation.idle.name, QueueMode.CompleteOthers);

                    //Invoke("SetSequence", attendAnimation.open.length);
                    Invoke("SetSequence", 1.2f);
                }
            }
        }

        private void SetSequence()
        {
            if (attendState == AttendState.TodayReady)
            {
                coroutineAttend = StartCoroutine("OnTodayRecive");

                attendState = AttendState.TodayWait;
            }
            else if (attendState == AttendState.Tomorrow)
            {
                coroutineTimer = StartCoroutine("OnNextAttendTimer", CommonTools.JavaTime(data.bonusNextDate));

                coroutineTomorrow = StartCoroutine("OnTomorrowSequence");

                //TomorrowSequence();
            }
        }

        //private void TomorrowSequence()
        private IEnumerator OnTomorrowSequence()
        {
            bool lastTomorrow = false;
            
            tomorrowItem.ShowEffect();

            if (timer != null)
                CommonTools.SetActive(timer, true);

            if (timerBackground != null)
                CommonTools.SetActive(timerBackground, true);

            RectTransform itemRect = null;
            if ((data.attendedDay + 1) > items.Count)
            {
                itemRect = items[items.Count - 1].GetComponent<RectTransform>();
                lastTomorrow = true;
            }
            else
            {
                itemRect = items[data.attendedDay].GetComponent<RectTransform>();
                lastTomorrow = false;
            }

            if (lastTomorrow)
            {
                if (lastTomorrowSound != null)
                    lastTomorrowSound.Play();
            }
            else
            {
                if (tomorrowSound != null)
                    tomorrowSound.Play();
            }

            if (spotiLight != null)
            {
                spotiLight.anchoredPosition = itemRect.anchoredPosition;
                CommonTools.SetActive(spotiLight, true);

                if (spotiLightAnimation.spotiLightOn != null)
                {
                    spotiLightAnimation.target.PlayQueued(spotiLightAnimation.spotiLightOn.name, QueueMode.CompleteOthers);
                    spotiLightAnimation.target.PlayQueued(spotiLightAnimation.spotiLightIdle.name, QueueMode.CompleteOthers);
                }
            }

            yield return new WaitForSeconds(tomorrowItem.ItemAnimation.tomorrow.length);

            if (closeButton != null)
                CommonTools.SetActive(closeButton, true);

            keyUpEscape = true;

            coroutineTomorrow = null;
        }

        private IEnumerator OnTodayRecive()
        {
            todayItem.ShowEffect();

            yield return new WaitForSeconds(0.15f);

            //if (coinController != null)
            //{
            //    coinController.StartCoinMoveDelay(() =>
            //    {
            //        attendState = AttendState.Tomorrow;

            //        CommonTools.SetActive(spotiLight, false);

            //        SetItem();
            //        SetSequence();

            //    });
            //}

            if (coinSound != null)
                coinSound.Play();

            todayItem.ShowCoinEffect(() =>
            {
                attendState = AttendState.Tomorrow;

                //CommonTools.SetActive(spotiLight, false);

                if (spotiLightAnimation.spotiLightOff != null)
                    spotiLightAnimation.target.Play(spotiLightAnimation.spotiLightOff.name);

                SetItem();
                SetSequence();
            });
        }

        private IEnumerator OnNextAttendTimer(DateTime nextTime)
        {
            while(true)
            {
                TimeSpan timeout = CommonTools.ToPSTTime(nextTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
                if (timeout >= TimeSpan.Zero)
                {
                    if (timer != null)
                        timer.text = string.Format("{0}", CommonTools.DisplayDigitalTime(timeout));

                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(1f);

                    SetData();

                    yield break;
                }
            }
        }

        public void OnClickClose()
        {
            Close();
        }

        protected override void Close()
        {
            //SessionLobby.Instance.RequestUser(() =>{ });
            UserInfo.Instance.UpdateUserCoin(null);

            if (attendState == AttendState.Tomorrow || attendState == AttendState.ReciveTomorrow)
                AttendInfo.Instance.AttendBonusAvailable = false;

            if (ActionClose != null)
            {
                ActionClose();
                ActionClose = null;
            }

            base.Close();
        }
    }
}