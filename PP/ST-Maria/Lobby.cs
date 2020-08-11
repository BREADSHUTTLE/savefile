// Lobby.cs - Lobby implementation file
//
// Description      : Lobby
// Author           : uhrain7761
// Maintainer       : uhrain7761, icoder
// How to use       : 
// Created          : 2018/02/28
// Last Update      : 2018/12/26
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using UnityEngine;
using ST.MARIA;
using ST.MARIA.DataPool;
using ST.MARIA.Network;
using ST.MARIA.Popup;
using PlayEarth.Unity.Auth;
using Firebase.Analytics;
using UnityEngine.UI;

namespace ST.MARIA.UI.Lobby
{
    public sealed class Lobby : UIBaseLayout<Lobby>
    {
        [SerializeField] private CasinoListView casinoListView;
        [SerializeField] private GameObject lobbySound;
        [SerializeField] private Button mystripTutorial;
        [SerializeField] private Image newIslandStructure;
        [SerializeField] private RectTransform rewardIsland;
        [SerializeField] private Text rewardCoin;
        [SerializeField] private RectTransform rewardRect;

        private LobbyPlaySound sound;
        private bool dontTouch = false;
        private bool rewardTime = false;
        private float tempTimer = 0f;
        private long rewardTimeSeconds = 0;

        public bool DontTouch
        {
            get { return dontTouch; }
            set { dontTouch = value; }
        }

        protected override void Awake()
        {
            base.Awake();

            UserTracking.LogEvent(this.name, new Parameter[]
            {
                new Parameter("location", Session.Instance.Location),
                new Parameter("time", CommonTools.TimeToLong(ClientInfo.Instance.ServerTime)),
                new Parameter("coin", MyInfo.Instance.Coin),
                new Parameter("gem", MyInfo.Instance.Gem),
                new Parameter("what", "open"),
            });

            if (DataPool.CasinoInfo.Instance.CasinoDatas == null)
                Log("NULL casino data");

            PopupClaimGift.Create();
        }

        private void Start()
        {
            rewardTimeSeconds = InventoryInfo.Instance.mystripRewardData.minSecondsForStripReward;
            ShowRewardIcon(false);

            PopupLoading.Show(false);

            SetupUI();
            PlayLobbySound();
            ShowPopupDailyBonus();
        }

        private void Update()
        {
            if (rewardTime)
            {
                tempTimer += Time.deltaTime;
                if (tempTimer >= rewardTimeSeconds)
                {
                    tempTimer = 0;
                    rewardTime = false;
                    SynMystripReward();
                }
            }
        }

        private void SetupUI()
        {
            SetCasinoListView();
            SetButtonTutorial();
            ShowMyIslandRewardIcon(false);
            OnMyIslandNewIcon();
            SetRewardTime();
            NotiFacebookLogin();

            PopupBanner.Create();
        }

        private void SetRewardTime()
        {
            if (rewardTimeSeconds > 0)
            {
                rewardTime = true;
                ShowRewardIcon(false);
            }
            else if (rewardTimeSeconds == 0)
            {
                rewardTime = false;
                ShowRewardIcon(true);
            }
            else if (rewardTimeSeconds < 0)
            {
                rewardTime = false;
                ShowRewardIcon(false);
            }
        }

        private void ShowRewardIcon(bool show)
        {
            if (rewardIsland == null)
                return;

            if (newIslandStructure.gameObject.activeSelf)
                return;

            CommonTools.SetActive(rewardIsland, show);

            if (rewardCoin != null)
                rewardCoin.text = InventoryInfo.Instance.mystripRewardData.totalCoin.ToString("N0");

            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardRect);
        }

        private void SynMystripReward()
        {
            SessionLobby.Instance.RequestMystripSummary(() =>
            {
                rewardTimeSeconds = InventoryInfo.Instance.mystripRewardData.minSecondsForStripReward;
                SetRewardTime();
            });
        }

        private void SetCasinoListView()
        {
            if (casinoListView == null)
                return;

            var casinoData = DataPool.CasinoInfo.Instance.CasinoDatas;
            casinoListView.Build(casinoData, DataPool.CasinoInfo.Instance.RewardLastCasino);
            DataPool.CasinoInfo.Instance.RemoveRewardCasinoData();
        }

        private void SetButtonTutorial()
        {
            if (PlayerPrefs.GetInt("MyStripButtonTutorial") < 1)
                CommonTools.SetActive(mystripTutorial, true);
            else
                CommonTools.SetActive(mystripTutorial, false);
        }

        private void ShowPopupDailyBonus()
        {
            string time = ST.MARIA.PlayerPrefs.GetString("DailyBonusCollectTime:" + MyInfo.Instance.GSN);
            if (string.IsNullOrEmpty(time))
            {
                PopupDailyBonus.Create();
            }
            else
            {
                DateTime getTime = new DateTime(long.Parse(time));
                DateTime lastTime = new DateTime(getTime.Year, getTime.Month, getTime.Day, 0, 0, 0);

                DateTime pstServerTime = CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
                DateTime serverTime = new DateTime(pstServerTime.Year, pstServerTime.Month, pstServerTime.Day, 0, 0, 0);

                TimeSpan span = serverTime - lastTime;
                Log("Daily bonus last day : " + span.Days);
                if (span.Days > 0)
                    PopupDailyBonus.Create();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying == true)
            {
                SessionLobby.Instance.ActionLevelup += OnLevelup;
                SessionLobby.Instance.ActionVideoAds += OnVideoAds;
                SessionLobby.Instance.ActionMissionAchievement += OnMissionAchievement;
                SessionLobby.Instance.ActionAdminNotification += OnAdminNotification;
                SessionLobby.Instance.ActionFBConnectReward += OnFBConnectReward;

                DataPool.CasinoInfo.Instance.ActionAdd += OnRefreshCasinoInventory;
                DataPool.InventoryInfo.Instance.ActionAddReward += OnMyIslandNewIcon;
                DataPool.ShopInfo.Instance.ActionBuyStructure += OnMyIslandNewIcon;
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying == true)
            {
                SessionLobby.Instance.ActionLevelup -= OnLevelup;
                SessionLobby.Instance.ActionVideoAds -= OnVideoAds;
                SessionLobby.Instance.ActionMissionAchievement -= OnMissionAchievement;
                SessionLobby.Instance.ActionAdminNotification -= OnAdminNotification;
                SessionLobby.Instance.ActionFBConnectReward -= OnFBConnectReward;

                DataPool.CasinoInfo.Instance.ActionAdd -= OnRefreshCasinoInventory;
                DataPool.InventoryInfo.Instance.ActionAddReward -= OnMyIslandNewIcon;
                DataPool.ShopInfo.Instance.ActionBuyStructure -= OnMyIslandNewIcon;
            }
        }

        protected override void OnDestroy()
        {
            if (Session.Instance.CurrentState != Session.State.InLobby)
                StopLobbySound();

            base.OnDestroy();
        }

        private void NotiFacebookLogin()
        {
            if (Session.Instance.FacebookLink && Auth.IsFacebook)
            {
                PopupMessageBox.Create("POPUP.MESSAGE.FacebookConnect.Title", "POPUP.MESSAGE.FacebookConnect.Description");
                Session.Instance.FacebookLink = false;
            }
        }

        private void OnLevelup(NotifyLevelup content)
        {
            PopupLevelup.Create(content.achievedLevel);
        }

        private void OnVideoAds(NotifyAdVideoReward content)
        {
            PopupVideoAds.Create();
        }

        private void OnMissionAchievement(NotifyMissionAchievement content)
        {
            string group = content.challenges[0].challengeType;
            switch (group)
            {
                case "D":
                    PopupDailyChallenge.Create();
                    break;

                case "C":
                    PopupStarMission.Create(content.challenges[0].casinoId);
                    break;

                default:
                    Assert(false);
                    break;
            }
        }

        private void OnAdminNotification(NotifyAdminNotification content)
        {
            if (content.location == "Lobby" || content.location == "All")
            {
                switch (content.type)
                {
                    case "Notice":
                    {
                        //PopupMessageBox.Create("POPUP.MESSAGE.Announcement.Title", content.message);
                        PopupNotice.Create(content.message);
                        break;
                    }
                    case "Maintenance":
                    {
                        PopupMessageBox.Create("POPUP.MESSAGE.Maintenance.Title", content.message).ActionCommand = (command) =>
                        {
                            Application.Quit();
                        };
                        break;
                    }
                }
            }
        }

        private void OnFBConnectReward(NotifyFBConnect content)
        {
            PopupFacebookReward.Create();
        }

        private void OnRefreshCasinoInventory()
        {
            casinoListView.Refresh(DataPool.CasinoInfo.Instance.CasinoDatas, DataPool.CasinoInfo.Instance.RewardLastCasino);
            DataPool.CasinoInfo.Instance.RemoveRewardCasinoData();
        }

        private void OnMyIslandNewIcon()
        {
            if (newIslandStructure == null)
                return;

            if (InventoryInfo.Instance.FindRewardKey("casino") > 0 || InventoryInfo.Instance.FindRewardKey("structure") > 0)
            {
                if (rewardIsland.gameObject.activeSelf)
                    CommonTools.SetActive(rewardIsland, false);

                CommonTools.SetActive(newIslandStructure, true);
            }
            else
            {
                CommonTools.SetActive(newIslandStructure, false);
            }
        }

        private void ShowMyIslandRewardIcon(bool show)
        {
            if (rewardIsland != null)
                CommonTools.SetActive(rewardIsland, show);
        }

        private void PlayLobbySound()
        {
            sound = GameObject.FindObjectOfType(typeof(LobbyPlaySound)) as LobbyPlaySound;
            if (sound == null)
            {
                GameObject obj = Instantiate(lobbySound) as GameObject;
                obj.transform.localPosition = new Vector3(0, 0, 0);
                obj.transform.localScale = new Vector3(1f, 1f, 1f);
                obj.gameObject.SetActive(true);
                sound = obj.GetComponent<LobbyPlaySound>();
            }

            sound.PlayBGM();
        }

        private void StopLobbySound()
        {
            if (sound == null)
                return;

            sound.StopBGM();
        }

        public void OnClickDailyQuest()
        {
            PopupDailyChallenge.Create();
        }

        public void OnClickMyStrip()
        {
            // CHECK : 아래 부분 시퀀스(eg. SessionLobby)로 옮겨 주세요. DontTouch boolean flag 방식 사용마시고 state로 체크해 주세요.

            if (DontTouch == true)
                return;

            PopupLoading.Show(true);
            CommonTools.LoadScene("MyStrip", false);
        }

        public void OnClickMyStripTutorial()
        {
            Log("Lobby MyStrip Button Tutorial");

            CommonTools.SetActive(mystripTutorial, false);
            PlayerPrefs.SetInt("MyStripButtonTutorial", 1);
            PopupTutorial.Create(PopupTutorial.Type.MyStripButton);
        }
    }
}