// Lobby.cs - Lobby implementation file
//
// Description      : Lobby
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/02/13
// Last Update      : 2019/02/18
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Dorothy;
using Dorothy.UI;
using Dorothy.UI.Popup;
using Dorothy.Network;
using Dorothy.DataPool;
using Voyager.Unity;
using Firebase.Crashlytics;
using UnityEngine.CrashReportHandler;
using UnityEngine.EventSystems;
using PlayEarth.Unity;
using PlayEarth.Unity.Auth;

namespace Dorothy.UI.Lobby
{
    public sealed class Lobby : UIBaseLayout<Lobby>
    {
        [SerializeField] private float ScreenWidthScale = 1280;
        [SerializeField] private float ScreenHeightScale = 720;
        [SerializeField] private CanvasScaler canvasScaler;

        [SerializeField] private LobbyListView lobbyListView;
        [SerializeField] private BannerListView bannerListView;
        [SerializeField] private Text txtDailyQuest;
        [SerializeField] private Text txtBuyCoins;
        [SerializeField] private Text txtGifts;
        [SerializeField] private TimeBonus timeBonus;
        [SerializeField] private Button missionButton;
        [SerializeField] private Button missionButtonDisable;
        [SerializeField] private ItemCount countMission;
        [SerializeField] private ItemCount countGift;
        [SerializeField] private ItemCount countEvent;

        [SerializeField] private GameObject bonusObj;
        [SerializeField] private Image bonusButtonBadge;
        [SerializeField] private Text bonusName;
        private Coroutine corotineAttendanceTimer = null;

        [SerializeField] private GameObject questObj;
        [SerializeField] private Text questName;

        [SerializeField] private Text missionTime;
        private Coroutine coroutineMissionTimer = null;

        [SerializeField] private Button btnFake;

        [SerializeField] private SoundPlayer lobbyBGM;
        [SerializeField] private FadeInOut fadeInOut;

        [SerializeField] private EventSystem eventSystem;

        private List<ProgressiveJackpot> jackpotItemList = new List<ProgressiveJackpot>();
        
        private bool isShowQuitPopup = false;
        private bool isApplicationQuit = false;

        protected override void Awake()
        {
#if UNITY_EDITOR
            // LOBBY Scene에서 바로 테스트 할 경우 사용
            if (Environment.Instance.UseLocalTest == true)
            {
                ResourceManager.Instance.Type = Environment.Instance.AssetBundleType;
                PopupSystem.Instance.Initialize();
            }
#endif

            base.Awake();

            ClearLobbyJackpotItem();

            Session.Instance.IsGameEnter = false;

            float rH = Screen.height / ScreenHeightScale;
            float widthScale = Screen.width / rH;

            float ratio16_9 = 16.0f / 9.0f;           // 1.777777777777778 보다 작으면 1280 / 720 으로 크면 원래 해상도 비율로
            float ratio = (float)Screen.width / (float)Screen.height;
			
            //if ((Utils.Utils.WidthRatio == 8) && (Utils.Utils.HeightRatio == 5))
            if (ratio16_9 > ratio)
            {
                canvasScaler.referenceResolution = new Vector2(ScreenWidthScale, ScreenHeightScale);
            }
            else
            {
                canvasScaler.referenceResolution = new Vector2(widthScale, ScreenHeightScale);
            }

            Application.targetFrameRate = 30;
        }

        void OnApplicationQuit()
        {
            isApplicationQuit = true;

            var app = Firebase.FirebaseApp.DefaultInstance;
            if (app != null)
                app.Dispose();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                SessionLobby.Instance.AddNotificationMessageListener();
                SessionLobby.Instance.AddListenerUpdateGift(UpdateButtons);

                LobbyInfo.Instance.ActionMissionSummary += SetMissionButton;
                MissionInfo.Instance.ActionLobbyIcon += ShowMissionButton;

                AttendInfo.Instance.ActionAttend += SetAttendanceButton;

                LobbyInfo.Instance.ActionEventSummary += SetEventButton;

                UserInfo.Instance.ActionLobbyOpenPopup += PlayBGM;

                Session.Instance.ActionFacebookReady += DisableEvent;

                if (timeBonus != null)
                    timeBonus.ActionOpenMegaBonus += PlayBGM;

                if (fadeInOut != null)
                    fadeInOut.StartFadeInOut(true);

                // 백그라운드로 나갔다 왔을때 Loading Popup이 살아있는 경우 대비
                if (PopupSystem.Instance.IsPopupOpened("POPUP_Loading") == true)
                    PopupLoading.CloseSelf();

                InvokeRepeating("LoopLobbyJackpotItem", 5f, 5f);
            }
        }

        private void OnDisable()
        {
            if (isApplicationQuit == true)
                return;

            if (Application.isPlaying)
            {
                SessionLobby.Instance.RemonveAllNotificationMessageCallback();
                SessionLobby.Instance.RemoveListenerUpdateGift();

                LobbyInfo.Instance.ActionMissionSummary -= SetMissionButton;
                MissionInfo.Instance.ActionLobbyIcon -= ShowMissionButton;

                AttendInfo.Instance.ActionAttend -= SetAttendanceButton;

                LobbyInfo.Instance.ActionEventSummary -= SetEventButton;

                UserInfo.Instance.ActionLobbyOpenPopup -= PlayBGM;

                Session.Instance.ActionFacebookReady -= DisableEvent;

                if (timeBonus != null)
                    timeBonus.ActionOpenMegaBonus -= PlayBGM;

                ClearLobbyJackpotItem();
            }
        }

        private void Start()
        {
            SetUI();
            SetActiveFakeBtn(false);

            CrashReportHandler.SetUserMetadata("UID", UserInfo.Instance.GSN.ToString());    // Unity Crashes and Exceptions meta info
            CrashReportHandler.SetUserMetadata("Environment", Environment.Instance.Target.ToString());
            Crashlytics.SetUserId(UserInfo.Instance.GSN.ToString());                        // FireBase Crashlytics user id
            Crashlytics.SetCustomKey("Environment", Environment.Instance.Target.ToString());                        
        }

        private void Update()
        {
            //if (Application.platform == RuntimePlatform.Android)
            //{
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (TopUI.Instance != null)
                {
                    if (TopUI.Instance.CheckActiveMenu(true))
                    {
                        TopUI.Instance.SetMenu(false);
                        return;
                    }
                }

                if (PopupSystem.Instance.Count > 0)
                {
                    //string curOpenedPopup = PopupSystem.Instance.GetCurrentOpenedPopupName();
                    //if (curOpenedPopup.CompareTo("POPUP_Gift") != 0)
                    //{
                    //    PopupSystem.Instance.OnKeyUpEscape();
                    //}

                    PopupSystem.Instance.OnKeyUpEscape();
                }
                else
                {
                    if (isShowQuitPopup == false)
                    {
                        isShowQuitPopup = true;

                        UserTracking.Instance.LogExit(UserTracking.EventWhich.popup, UserTracking.EventWhat.lobby_back, UserTracking.Location.lobby);
                        PopupMessage.Open(LocalizationSystem.Instance.Localize("LEAVEGAME.Popup.Title"), LocalizationSystem.Instance.Localize("LEAVEGAME.Popup.Description"), 0f,
                        PopupMessage.Type.LeaveStay,
                        command =>
                        {
                            if (command == PopupMessage.Command.Yes)
                            {

                                if (Application.platform == RuntimePlatform.Android)
                                {
                                    new AndroidJavaClass("java.lang.System").CallStatic("exit", 0);
                                }

                                Application.Quit();
                            }

                            isShowQuitPopup = false;
                        });
                    }
                }
            }

            if (Input.GetKey(KeyCode.Home))
            {
                UserTracking.Instance.LogExit(UserTracking.EventWhich.home, UserTracking.EventWhat.lobby_main, UserTracking.Location.lobby);
            }

            //}
        }

        private void DisableEvent(bool disable)
        {
            if (eventSystem == null)
                return;

            eventSystem.enabled = !disable;
        }

        private void SetUI(bool ignoreList = false, bool isShowMainPopup = true)
        {
            if (ignoreList == false)
            {
                SetSlotListView();
                SetBannerListView();
            }

            SetButtons();

            if (isShowMainPopup == true)
                ShowMainPopup();
        }

        private void SetSlotListView()
        {
            lobbyListView.Build(LobbyInfo.Instance.LobbyLayout);
        }

        private void SetBannerListView()
        {
            bannerListView.Build(LobbyInfo.Instance.RollingBannerData);
        }

        //TODO:: 공지 -> 웰컴 -> 페이스북 연결 보상 -> 페이스북 피드보상(딥링크) -> 쿠폰(푸시&원링크포함)  -> 핫딜(스크래치 보너스게임 안받은거) -> 
        //데일리 보너스 -> 빅배너 -> 오퍼 -> keep 언락 -> 신규슬롯 -> 슬롯언락 -> 구매 리트라이
        private void ShowMainPopup()
        {
            ShowNotiPopup(() =>
            {
                ShowWelcomeBonus(() =>
                {
                    ShowRewardAFCoupon(() =>
                    {
                        ShowFacebookConnectBonus(() =>
                        {
                            ShowRewardFacebookFeed(() =>
                            {
                                ShowRewardCoupon(() =>
                                {
                                    ShowHotDealBonus(() =>
                                    {
                                        ShowDailyStamp(() =>
                                        {
                                            ShowBigBanner(() =>
                                            {
                                                ShowHotDealOffer(() =>
                                                {
                                                    ShowSlotUnlockKeep(() =>
                                                    {
                                                        ShowNewSlot(() =>
                                                        {
                                                            ShowSlotUnlock(() =>
                                                            {
                                                                ShowPurchaseRetry(null);
                                                            });
                                                        });
                                                    });
                                                });
                                            });
                                        });
                                    });
                                });
                            });
                        });
                    });
                });
            });
        }

        private void ShowNotiPopup(Action callback)
        {
            var bannerList = BigBannerInfo.Instance.NotiTextBanners;

            if (bannerList == null || bannerList.Count <= 0)
            {
                if (callback != null)
                    callback();

                return;
            }

            var templist = new List<BigBannerData>();
            
            for (int i = 0; i < bannerList.Count; i++)
            {
                if (BigBannerInfo.Instance.CheckTimeSpan(bannerList[i].bannerId))
                    templist.Add(bannerList[i]);
            }

            if (templist.Count > 0)
            {
                PopupNotiBanner.Open(templist, () =>
                {
                    if (callback != null)
                        callback();
                });
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private void ShowHotDealBonus(Action callback)
        {
            if (HotDealInfo.Instance.HotDealBonusData != null)
            {
                var datas = HotDealInfo.Instance.HotDealBonusData;
                if (datas.Count <= 0)
                {
                    if (callback != null)
                        callback();
                }
                else
                {
                    //TODO:: 여기에 핫딜 보너스 게임 UI 개발되면 작업합니다. (스크래치게임)
                    //TODO:: 일단 유저가 보너스 게임을 해야한다는 안내 팝업을 먼저 띄우고,

                    if (PlayerPrefs.HasKey(PlayerPrefs.HOTDEAL_LOTTERY_ALLSCRATCH))
                    {
                        //TODO:: 스크래치 다했음  결과 화면
                        PopupMessage.Open(LocalizationSystem.Instance.Localize("SCRATCH.Result.Title"),
                                        string.Format(LocalizationSystem.Instance.Localize("SCRATCH.Result.Description"), datas[0].totalBonusCoin.ToString("N0")),
                                        0f , PopupMessage.Type.Ok, (command) => 
                                        {
                                            if (command == PopupMessage.Command.Ok)
                                            {
                                                SessionLobby.Instance.RequestHotDealBonusReceive(datas[0].offerId, (exception) =>
                                                {
                                                    if (exception != null)
                                                    {
                                                        Error("Error Hot Deal Bonus Receive : " + exception.Message);
                                                    }
                                                    else
                                                    {
                                                        //SessionLobby.Instance.RequestUser(() => 
                                                        //{
                                                        //    if (callback != null)
                                                        //        callback();
                                                        //});

                                                        UserInfo.Instance.UpdateUserCoin(() =>
                                                        {
                                                            if (callback != null)
                                                                callback();
                                                        });
                                                    }
                                                });
                                            }
                                        });
                    }
                    else
                    {
                        //TODO:: 스크래치 다 안함 다시 하세요
                        PopupMessage.Open(LocalizationSystem.Instance.Localize("SCRATCH.Retry.Title"), LocalizationSystem.Instance.Localize("SCRATCH.Retry.Description"), 0f, PopupMessage.Type.Ok, (command) =>
                        {
                            if (command == PopupMessage.Command.Ok)
                            {
                                //TODO:: OK 버튼을 누르면 스크래치 게임을 시작한다.
                                PopupHotDealLottery.Open().OnClose(() =>
                                    {
                                        if (callback != null)
                                            callback();
                                    });
                            }
                        });
                    }
                }
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private void ShowWelcomeBonus(Action callback)
        {
            if (LobbyInfo.Instance.WelcomeBonusData != null)
            {
                PopupWelcomeBonus.Open(() =>
                {
                    if (callback != null)
                        callback();
                });
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private void ShowFacebookConnectBonus(Action callback)
        {
            if (LobbyInfo.Instance.FBConnectBonusData != null)
            {
                PopupFacebookReward.Open(() =>
                {
                    if (callback != null)
                        callback();
                });
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private void ShowDailyStamp(Action callback)
        {
            if (AttendInfo.Instance.AttendBonusData != null)
            {
                if (AttendInfo.Instance.AttendBonusData.bonusAvailable)
                {
                    UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.freecoin, UserTracking.EventWhat.attend_stamp, "A", UserTracking.Location.none);
                    
                    PopupAttendance.Open(() =>
                    {
                        if (callback != null)
                            callback();
                    });
                }
                else
                {
                    if (callback != null)
                        callback();
                }
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private void ShowBigBanner(Action callback)
        {
            if (BigBannerInfo.Instance.BigBannerData != null)
            {
                if (BigBannerInfo.Instance.BigBannerData.Count > 0)
                {
                    if (CheckBigBanner())
                    {
                        PopupBigBanner.Open(null, (action) =>
                        {
                            if (callback != null)
                                callback();
                        });
                    }
                }
                else
                {
                    if (callback != null)
                        callback();
                }
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private bool CheckBigBanner()
        {
            var list = BigBannerInfo.Instance.BigBannerData;

            for (int i = 0; i < list.Count; ++i)
            {
                if (CheckBannerTime(list[i].bannerId))
                    return true;
            }

            return false;
        }

        private bool CheckBannerTime(long id)
        {
            //TODO:: true - 보여줌 / false - 안보여줌
            if (PlayerPrefs.HasKey(UserInfo.Instance.GSN.ToString() + "/" + id.ToString()) == false)
                return true;

            var now = CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
            var next = CommonTools.UTCToDateTime(PlayerPrefs.GetString(UserInfo.Instance.GSN.ToString() + "/" + id.ToString()));

            int result = DateTime.Compare(now, next);

            if (result == 0)
            {
                //TODO:: 두 시간이 동일합니다.
                return true;
            }
            else if (result > 0)
            {
                //TODO:: now 현재 시간이 더 큽니다. (저장된 시간보다 커서 지났다는 의미니까 배너를 보여줘야 합니다.)
                return true;
            }
            else if (result < 0)
            {
                //TODO:: next 저장된 시간이 더 큽니다. (저장된 시간이 더 크므로 아직 지나지 않았으므로 보여주면 안됩니다.)
                return false;
            }

            return true;
        }

        private void ShowHotDealOffer(Action callback)
        {
            if (HotDealInfo.Instance.HotDealOfferData != null)
            {
                if (HotDealInfo.Instance.HotDealOfferData.Count > 0)
                {
                    if (GetTimeSpanDay(PlayerPrefs.HOT_DEAL_DAY) > 0)
                    {
                        UserTracking.Instance.LogPurchaseImp(HotDealInfo.Instance.HotDealOfferData[0].offerId, UserTracking.PurchaseWhich.hotdeal, "A", UserTracking.Location.lobby_main);
                        PopupHotDealOffer.Open(() =>
                        {
                            if (callback != null)
                                callback();
                        }, null, "A", UserTracking.Location.lobby_main);
                    }
                    else
                    {
                        if (callback != null)
                            callback();
                    }
                }
                else
                {
                    if (callback != null)
                        callback();
                }
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private int GetTimeSpanDay(string key)
        {
            if (PlayerPrefs.HasKey(key) == false)
                return 1;

            var nowTime = CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
            var saveTime = CommonTools.UTCToDateTime(PlayerPrefs.GetString(key));

            TimeSpan time = saveTime - nowTime;

            return time.Days;
        }

        private void ShowSlotUnlockKeep(Action callback)
        {
            if (SlotUnlockInfo.Instance.SlotUnlockKeep == true)
            {
                PopupKeepItUp.Open(() =>
                {
                    if (callback != null)
                        callback();
                });
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private void ShowNewSlot(Action callback)
        {
            if (SlotUnlockInfo.Instance.NewSlot != null)
            {
                PopupSlotUnlock.Open(() =>
                {
                    lobbyListView.OnSelectItem(SlotUnlockInfo.Instance.NewSlot.msn);
                    SlotUnlockInfo.Instance.ClearNew();     //TODO:: 한번쓰고 버림
                },
                () =>
                {
                    SlotUnlockInfo.Instance.ClearNew();     //TODO:: 한번쓰고 버림

                    if (callback != null)
                        callback();
                },
                true);

            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private void ShowSlotUnlock(Action callback)
        {
            if (SlotUnlockInfo.Instance.SlotUnlock != null)
            {
                //if (PlayerPrefs.GetBool(PlayerPrefs.SLOT_UNLOCK_POPUP + SlotUnlockInfo.Instance.SlotUnlock.msn) == true)
                //{
                //    if (callback != null)
                //        callback();
                //}
                //else
                {
                    PopupSlotUnlock.Open(() =>
                    {
                        lobbyListView.OnSelectItem(SlotUnlockInfo.Instance.SlotUnlock.msn);
                        SlotUnlockInfo.Instance.ClearUnlock();    //TODO:: 한번쓰고 버림
                    },
                    () =>
                    {
                        SlotUnlockInfo.Instance.ClearUnlock();    //TODO:: 한번쓰고 버림

                        if (callback != null)
                            callback();
                    },
                    false);
                }
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        private void ShowRewardCoupon(Action callback)
        {
            Session.Instance.ShowRewardCoupon(callback);
        }

        private void ShowRewardAFCoupon(Action callback)
        {
            Session.Instance.ShowRewardAFCoupon(callback);
        }

        private void ShowRewardFacebookFeed(Action callback)
        {
            Session.Instance.ShowRewardFacebookFeed(callback);
        }

        private void ShowPurchaseRetry(Action callback)
        {
            Session.Instance.PurchaseRetry(callback);
        }

        private void SetButtons()
        {
            if (txtDailyQuest != null)
                txtDailyQuest.text = LocalizationSystem.Instance.Localize("LOBBY.DailyQuest.Name");

            if (txtGifts != null)
                txtGifts.text = LocalizationSystem.Instance.Localize("LOBBY.Gifts.Name");

            if ((countGift != null) && (LobbyInfo.Instance.InboxGiftSummary != null))
                countGift.SetCount(LobbyInfo.Instance.InboxGiftSummary.totalGiftCount);

            if (bonusName != null)
                bonusName.text = LocalizationSystem.Instance.Localize("LOBBY.Bonus.Name");

            SetMissionButton();

            SetAttendanceButton();
            SetQuestButton();

            SetEventButton();
        }
        
        private void ShowMissionButton()
        {
            CommonTools.SetActive(missionButton, MissionInfo.Instance.ActiveLobbyIcon);
            CommonTools.SetActive(missionButtonDisable, !MissionInfo.Instance.ActiveLobbyIcon);
        }

        private void SetMissionButton()
        {
            if (MissionInfo.Instance.ActiveLobbyIcon == false || LobbyInfo.Instance.MissionSummaryData == null)
            {
                CommonTools.SetActive(countMission, false);
                CommonTools.SetActive(missionTime, false);
                return;
            }

            if (countMission != null)
                countMission.SetCount(LobbyInfo.Instance.MissionSummaryData.leftMissionCount);


            if (missionTime != null)
            {
                if (LobbyInfo.Instance.MissionSummaryData.dailyEndDate <= 0)
                {
                    CommonTools.SetActive(missionTime, false);
                }
                else
                {
                    CommonTools.SetActive(missionTime, true);

                    if (coroutineMissionTimer != null)
                        StopCoroutine(coroutineMissionTimer);

                    coroutineMissionTimer = StartCoroutine("MissionDisplayTime", CommonTools.JavaTime(LobbyInfo.Instance.MissionSummaryData.dailyEndDate));
                }
            }
        }

        private IEnumerator MissionDisplayTime(DateTime curTime)
        {
            while (true)
            {
                TimeSpan timeout = CommonTools.ToPSTTime(curTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
                if (timeout >= TimeSpan.Zero)
                {
                    missionTime.text = CommonTools.DisplayDigitalTime(timeout);
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(1f);

                    Refresh();

                    yield break;
                }
            }
        }

        private void SetAttendanceButton()
        {
            if (AttendInfo.Instance.AttendBonusData == null)
            {
                if (bonusObj != null)
                    CommonTools.SetActive(bonusObj, false);

                return;
            }

            if (LobbyInfo.Instance.QuestSummaryData.questEnabled == true)
            {
                if (bonusObj != null)
                    CommonTools.SetActive(bonusObj, false);

                return;
            }

            if (bonusObj != null)
                CommonTools.SetActive(bonusObj, true);

            if (corotineAttendanceTimer != null)
            {
                StopCoroutine(corotineAttendanceTimer);
                corotineAttendanceTimer = null;
            }

            if (AttendInfo.Instance.AttendBonusData.bonusAvailable == true)
            {
                if (bonusButtonBadge != null)
                    CommonTools.SetActive(bonusButtonBadge, true);
            }
            else
            { 
                if (bonusButtonBadge != null)
                    CommonTools.SetActive(bonusButtonBadge, false);

                corotineAttendanceTimer = StartCoroutine("AttendanceTimer", CommonTools.JavaTime(AttendInfo.Instance.AttendBonusData.bonusNextDate));
            }
        }

        private IEnumerator AttendanceTimer(DateTime endTime)
        {
            while (true)
            {
                TimeSpan timeout = CommonTools.ToPSTTime(endTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
                if (timeout >= TimeSpan.Zero)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(1f);

                    //CommonTools.SetActive(attendanceEffect, true);
                    Refresh();

                    yield break;
                }
            }
        }

        private void SetQuestButton()
        {
            if (LobbyInfo.Instance.QuestSummaryData.questEnabled == false)
            {
                if (questObj != null)
                    CommonTools.SetActive(questObj, false);
                return;
            }

            if (questObj != null)
                CommonTools.SetActive(questObj, true);
        }

        private void SetEventButton()
        {
            if (countEvent != null)
            {
                if (LobbyInfo.Instance.BlastSummaryData != null)
                    countEvent.SetCount(LobbyInfo.Instance.BlastSummaryData.pickCount >= 99 ? 99 : LobbyInfo.Instance.BlastSummaryData.pickCount);
                else
                    countEvent.SetCount(0);
            }
        }

        public void UpdateButtons()
        {
            if ((countGift != null) && (LobbyInfo.Instance.InboxGiftSummary != null))
                countGift.SetCount(LobbyInfo.Instance.InboxGiftSummary.totalGiftCount);
        }

        public void PlayBGM(bool play)
        {
            if (!play)
                lobbyBGM.Play();
            else
                lobbyBGM.Stop();
        }

        public void Refresh(float delay = 0.0f)
        {
            // KICK 상태 팝업 오픈 되어있을 경우는 재 통신을 하지 않아야 한다.
            if (PopupSystem.Instance.IsPopupOpened("POPUP_DoubleConnection") == true)
                return;

            // Invalid 상태 팝업 오픈 되어있을 경우는 재 통신을 하지 않아야 한다.
            if (PopupSystem.Instance.IsPopupOpened("POPUP_InvalidSessionId") == true)
                return;

            if (delay > 0.0f)
            {
                StartCoroutine(CoroutineRefresh(delay));
            }
            else
            {
                if (PopupSystem.Instance.IsPopupOpened("POPUP_Loading") == true)
                    PopupLoading.CloseSelf();

                PopupLoading.Open();
                SessionLobby.Instance.RefreshLobby(() =>
                {
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.CompareTo(Constants.SCENE_LOBBY) == 0)
                    {
                        SetUI(true, false);
                        //Invoke("CloseLoading", 0.1f);
                        PopupLoading.CloseSelf();
                    }
                    else
                    {
                        PopupLoading.CloseSelf();
                    }
                });
            }
        }

        public void RefreshDelay()
        {
            if (coroutineMissionTimer != null)
                StopCoroutine(coroutineMissionTimer);

            StartCoroutine(CoroutineRefresh(0.1f));
        }

        IEnumerator CoroutineRefresh(float delay)
        {
            yield return new WaitForSeconds(delay);

            Refresh();
        }
        
        public void OnClickMission()
        {
            Log("OnClickMission");
            if (coroutineMissionTimer != null)
                StopCoroutine(coroutineMissionTimer);

            PopupMission.Open().OnClose(RefreshDelay);
        }

        public void OnClickAttendance()
        {
            Log("OnClickAttendance");
            
            PopupAttendance.Open(null);

            UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.freecoin, UserTracking.EventWhat.attend_stamp, "M", UserTracking.Location.none);
        }

        public void OnClickGift()
        {
            Log("OnClickGift");

            //PopupMessage.Open("COMING SOON", "We're working on making game better for you!", 0f, PopupMessage.Type.Ok);
            UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.freecoin, UserTracking.EventWhat.inbox, "M", UserTracking.Location.lobby_main);

            PopupGift.Open().OnClose(() => 
            {
                RefreshDelay();

                GiftInfo.Instance.ActionSendRefresh = null;
                GiftInfo.Instance.ActionOutboxGiftSummary = null;                
            });
        }

        public void OnClickEvent()
        {
            Log("OnClickEvent");

            PopupBlast.Open(null, () => { RefreshDelay(); }, UserTracking.Location.icon);
        }

        public void OnClickQuest()
        {
            Log("OnClickQuest");

            PopupQuest.Open();
        }

        public List<string> GetSlotMachineBundleList(int msn)
        {
            NEXT.GAMES.SlotDataList slotDataList = ResourceManager.Instance.Load<NEXT.GAMES.SlotDataList>(NEXT.GAMES.GameConstants.ASSETBUNDLE_COMMON_NAME, NEXT.GAMES.GameConstants.ASSET_SLOTDATALIST_NAME);
            if (slotDataList != null)
                return slotDataList.GetItem(msn).GetDownloadAssetbundleList();
            else
                return null;
        }

        public void DownloadSlotBundles(int msn, Action<bool> callback)
        {
            List<string> listAssets = GetSlotMachineBundleList(msn);
            if (listAssets == null)
            {
                if (callback != null)
                    callback(false);
            }

            ResourceManager.Instance.LoadAssetBundles(listAssets.ToArray(), (name, result, state, progress) =>
            {
                if (result == true)
                {
                    if (callback != null)
                        callback(true);
                }
                else
                {
                    if (progress == 1.0f)
                        Debug.LogError(name + " LoadAssetBundles Error");
                }
            });
        }

        public void CloseLoading()
        {
            PopupLoading.CloseSelf();
        }

        void OnApplicationFocus(bool focusStatus)
        {
#if !UNITY_EDITOR
            if (focusStatus == true)
            {
                UserTracking.Instance.LogLobbyEvent(UserTracking.LobbyWhich.pause);
                Refresh();
            }
#endif
        }

        public void SetActiveFakeBtn(bool value)
        {
            if (btnFake == null)
                return;

            btnFake.gameObject.SetActive(value);
        }

        public void FadeOut(Action callback)
        {
            if (fadeInOut != null)
                fadeInOut.StartFadeInOut(false, callback);
        }

        public void LoopLobbyJackpotItem()
        {
            for (int i = 0; i < jackpotItemList.Count; i++)
                jackpotItemList[i].SetJackpotItem();
        }

        public void AddLobbyJackpotItem(ProgressiveJackpot item)
        {
            jackpotItemList.Add(item);
        }

        public void RemoveJackpotItem(ProgressiveJackpot item)
        {
            for (int i = 0; i < jackpotItemList.Count; i++)
            {
                if (jackpotItemList[i] == item)
                    jackpotItemList.Remove(jackpotItemList[i]);
            }
        }

        public void ClearLobbyJackpotItem()
        {
            jackpotItemList.Clear();
        }
    }
}
