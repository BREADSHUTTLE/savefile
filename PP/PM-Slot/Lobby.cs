// Lobby.cs : Lobby implementation file
//
// Description     : Lobby
// Author          : icoder
// Maintainer      : uhrain7761, icoder
// How to use      : 
// Created         : 2016/08/23
// Last Update     : 2017/11/10
// Known bugs      : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using UnityEngine;
using System;
using System.Collections.Generic;
using DUNK.Popup;
using DUNK.DataPool;
using DUNK.Tools;
using Adiscope.Ads;
using PmangPlus.Unity;


namespace DUNK.UI.Lobby
{
    [ExecuteInEditMode]
    public sealed class Lobby : UIBaseLayout<Lobby>
    {
        [Serializable] private class LevelGauge
        {
            public GameObject container = null;
            public UISprite gauge = null;
        }

        [SerializeField] private UIWWWTexture avatar = null;
        [SerializeField] private UILabel[] level = null;
        [SerializeField] private UILabelEllipsis userName = null;
        [SerializeField] private UILabel[] balance = null;
        [SerializeField] private UILabel[] balanceReward = null;
        [SerializeField] private Animation[] balanceClaimFX = null;
        [SerializeField] private GameObject eventPushback = null;
        [SerializeField] private GameObject inventoryPushback = null;
        [SerializeField] private LobbyBroadcast broadcast = null;
        [SerializeField] private Animation[] timeBonusReceivingFX = null;
        [SerializeField] private GameObject timeBonusButton = null;
        [SerializeField] private GameObject timeBonusActive = null;
        [SerializeField] private GameObject timeBonusInactive = null;
        [SerializeField] private UILabel timeBonusTime = null;
        [SerializeField] private RandomPlaySound bgmPlayer = null;
        [SerializeField] private GameObject localMode = null;
        [SerializeField] private GameObject[] levelUpLimiteObjects = null;
        [SerializeField] private LevelGauge[] levelGauges = null;
        [SerializeField] private GameObject questButton = null;
        [SerializeField] private GameObject questActive = null;
        [SerializeField] private GameObject questInactive = null;
        [SerializeField] private GameObject questPush = null;
        [SerializeField] private UILabel questTime = null;
        [SerializeField] private UILabel videoAdsMoney = null;
        [SerializeField] private UILabel goldTreeLevel = null;
        [SerializeField] private GameObject goldTreeLevelBK = null;
        [SerializeField] private TweenAlpha goldTreeLevelContainer = null;
        [SerializeField] private GameObject safeButton = null;
        [SerializeField] private GameObject exceedBalance = null;
        [SerializeField] private GameObject backLight = null;

        private float timeBonusTimer = 0f;
        private float timeSafeTimer = 0f;
        private TimeSpan questTimer;
        private UISprite expGauge = null;
        private int isOverMaxLevel = 0;

        private void Setup()
        {
            CommonTools.SetActive(timeBonusReceivingFX, false);
            CommonTools.SetActive(timeBonusActive, false);
            CommonTools.SetActive(timeBonusInactive, false);
            CommonTools.SetActive(balanceReward, false);
            CommonTools.SetActive(broadcast, false);

            SetupLevelGauge();
            SetupQuest();
#if !DUNK_PCKLP
            SetupVideoAdsRewardInfo();
#endif
            if (SlotInfo.Instance.SlotDatas.Count > 0)
                PopupLoading.Show(false);
        }

        private void Start()
        {
            if (Application.isPlaying == true)
            {
                Setup();

                SessionAuth.Instance.OpenPromotionBanner();
                PopupChargeup.Create(false);
                PopupReview.Create(false);
                PopupExceedBalance.Create();

                ShowDailyBonus();

                if (PopupDailyBonus.IsVisible == false)
                {
                    if (QuestInfo.Instance.AutoPopup)
                        OnClickQuest();
                }
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Session.Instance.ActionChangeState               += OnChangeSessionState;
                SessionService.Instance.ActionClaimTimeBonus     += OnClaimTimeBonus;
                SessionService.Instance.ActionDailyBonusInfo     += OnDailyBonusInfo;
                SessionService.Instance.ActionPromotionInfo      += OnPromotionInfo;
                SessionService.Instance.ActionVideoAdsRewardInfo += OnUpdateVideoAdsMoneyText;
                BannerInfo.Instance.ActionAssign                 += OnBannerInfo;
                EventInfo.Instance.ActionAssign                  += OnEventInfo;
                EventInfo.Instance.ActionUnreadCount             += OnUpdateEventCount;
                GiftInfo.Instance.ActionTotalCount               += OnUpdateInventoryCount;
                LetterInfo.Instance.ActionUnreadCount            += OnUpdateInventoryCount;
                LetterInfo.Instance.ActionUnreadNoticeCount      += OnUpdateInventoryCount;
                MyInfo.Instance.ActionUpdateProfileImage         += OnUpdateProfileImage;
                MyInfo.Instance.ActionUpdateNickName             += OnUpdateNickName;
                MyInfo.Instance.ActionUpdateAssetInfo            += OnUpdateAssetInfo;
                MyInfo.Instance.ActionUpdateLevelInfo            += OnUpdateLevelInfo;
                MyInfo.Instance.ActionUpdateTimeBonusInfo        += OnUpdateTimeBonusInfo;
                MyInfo.Instance.ActionUpdateSafeInfo             += OnUpdateSafeInfo;
                QuestInfo.Instance.QuestDatas.ActionQuestStatus  += OnQuestStatus;
                QuestInfo.Instance.ActionPush                    += OnQuestPush;
                OfferwallAd.Instance.OnFailedToShow              += OnOfferwallAdFailedShow;
                OfferwallAd.Instance.OnClosed                    += OnOfferwallAdClosed;
                PPInteractiveWebview.Instance.ActionUnload       += OnUIRootDefault;

                OnUpdateEventCount();
                OnUpdateInventoryCount();
                OnUpdateProfileImage();
                OnUpdateNickName();
                OnUpdateAssetInfo();
                OnUpdateLevelInfo();
                OnUpdateTimeBonusInfo();
                OnUpdateSafeInfo();
                OnChangeSessionState(Session.Instance.CurrentState);
                OnQuestStatus();
                OnGoldtreeBadge();

                GraphicSystem.Instance.SetupFrameRate(
#if DUNK_PCKLP
                    true);
#else
                    false);
#endif
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                Session.Instance.ActionChangeState               -= OnChangeSessionState;
                SessionService.Instance.ActionClaimTimeBonus     -= OnClaimTimeBonus;
                SessionService.Instance.ActionDailyBonusInfo     -= OnDailyBonusInfo;
                SessionService.Instance.ActionPromotionInfo      -= OnPromotionInfo;
                SessionService.Instance.ActionVideoAdsRewardInfo -= OnUpdateVideoAdsMoneyText;
                BannerInfo.Instance.ActionAssign                 -= OnBannerInfo;
                EventInfo.Instance.ActionAssign                  -= OnEventInfo;
                EventInfo.Instance.ActionUnreadCount             -= OnUpdateEventCount;
                GiftInfo.Instance.ActionTotalCount               -= OnUpdateInventoryCount;
                LetterInfo.Instance.ActionUnreadCount            -= OnUpdateInventoryCount;
                LetterInfo.Instance.ActionUnreadNoticeCount      -= OnUpdateInventoryCount;
                MyInfo.Instance.ActionUpdateProfileImage         -= OnUpdateProfileImage;
                MyInfo.Instance.ActionUpdateNickName             -= OnUpdateNickName;
                MyInfo.Instance.ActionUpdateAssetInfo            -= OnUpdateAssetInfo;
                MyInfo.Instance.ActionUpdateLevelInfo            -= OnUpdateLevelInfo;
                MyInfo.Instance.ActionUpdateTimeBonusInfo        -= OnUpdateTimeBonusInfo;
                MyInfo.Instance.ActionUpdateSafeInfo             -= OnUpdateSafeInfo;
                QuestInfo.Instance.QuestDatas.ActionQuestStatus  -= OnQuestStatus;
                QuestInfo.Instance.ActionPush                    -= OnQuestPush;
                OfferwallAd.Instance.OnFailedToShow              -= OnOfferwallAdFailedShow;
                OfferwallAd.Instance.OnClosed                    -= OnOfferwallAdClosed;
                PPInteractiveWebview.Instance.ActionUnload       -= OnUIRootDefault;
            }
        }

        private void Update()
        {
            if (Application.isPlaying == true)
            {
                UpdateTimeBonusTimer();
                UpdateSafeExpireTimer();

                if (QuestInfo.Instance.Status() == QuestStatus.Progress)
                    UpdateQuestTimer();
            }
        }

        public void OnGoldtreeBadge(object msg = null)
        {
            int level = PlayerPrefs.GetInt(SessionService.Instance.GoldentreeLevel, 0);
            bool spraying = (PlayerPrefs.GetInt(SessionService.Instance.GoldentreeSpraying, 0) == 0);
            bool view = (level > 0) && (spraying == true);

            goldTreeLevelContainer.enabled = view;
            goldTreeLevelBK.SetActive(view);
            goldTreeLevel.enabled = view;

            if (view == true)
            {
                goldTreeLevel.text = level.ToString();
                goldTreeLevelContainer.Reset();
            }
        }

        private void SetupLevelGauge()
        {
            if (MyInfo.Instance.Level.Level >= MyInfo.Instance.Level.MaxLevel)
            {
                isOverMaxLevel = 1;
                CommonTools.SetActive(levelGauges[1].container, true);
                CommonTools.SetActive(levelGauges[0].container, false);
                expGauge = levelGauges[1].gauge;
            }
            else
            {
                isOverMaxLevel = 0;
                CommonTools.SetActive(levelGauges[0].container, true);
                CommonTools.SetActive(levelGauges[1].container, false);
                expGauge = levelGauges[0].gauge;
            }
        }

        private void SetupQuest()
        {
            QuestStepData currentStepData = QuestInfo.Instance.QuestDatas.GetCurrentStep();
            if (currentStepData != null)
            {
                if (QuestInfo.Instance.ActionPush != null)
                {
                    if (currentStepData.Achieved && currentStepData.Rewarded == false)
                        OnQuestPush(true);
                    else
                        OnQuestPush(false);
                }
            }
        }

        private void SetupVideoAdsRewardInfo()
        {
            SessionService.Instance.Request("/user_video_reward");
        }

        private void ResetSafeExpireTimer()
        {
#if DUNK_PCKLP
            timeSafeTimer = 
                (MyInfo.SafeInfo.CheckExpireDate == DateTime.MinValue || MyInfo.SafeInfo.CheckExpireDate == CommonTools.DefaultTime()) ?
                float.MinValue : (float)((MyInfo.SafeInfo.CheckExpireDate - ClientInfo.Instance.ServerTime).TotalSeconds);

            if (timeSafeTimer > 0f && timeSafeTimer != float.MinValue)
            {
                if (PlayerPrefs.HasKey(PlayerPrefs.SAFE_BOX_EXPIRED))
                    PlayerPrefs.DeleteKey(PlayerPrefs.SAFE_BOX_EXPIRED);
            }
#else
            timeSafeTimer = 0f;
#endif
        }

        private void UpdateSafeExpireTimer()
        {
#if DUNK_PCKLP
            if (timeSafeTimer != float.MinValue)
            {
                timeSafeTimer -= Time.deltaTime;

                if (timeSafeTimer < 0f)
                {
                    timeSafeTimer = float.MinValue;
                    MyInfo.SafeInfo.CheckExpireDate = DateTime.MinValue;
                    if (PlayerPrefs.HasKey(PlayerPrefs.SAFE_BOX_EXPIRED) == false)
                        PopupSafeBoxExpired.Create();
                }
            }
#endif
        }

        private void ResetTimeBonusTimer()
        {
            timeBonusTimer =
                (float)((MyInfo.Instance.TimeBonus.NextClaimTime - ClientInfo.Instance.ServerTime).TotalSeconds);
        }

        private void UpdateTimeBonusTimer()
        {
            timeBonusTimer -= Time.deltaTime;

            if (timeBonusTimer <= 0f)
            {
                CommonTools.SetActive(timeBonusActive, true);
                CommonTools.SetActive(timeBonusInactive, false);
                CommonTools.SetButtonEnable(timeBonusButton, true);
            }
            else
            {
                CommonTools.SetActive(timeBonusActive, false);
                CommonTools.SetActive(timeBonusInactive, true);
                CommonTools.SetButtonEnable(timeBonusButton, false);

                if (timeBonusTime != null)
                {
                    int hours = Mathf.FloorToInt(timeBonusTimer / 3600);
                    int minutes = Mathf.FloorToInt((timeBonusTimer % 3600) / 60);
                    int seconds = Mathf.FloorToInt(timeBonusTimer) % 60;

                    timeBonusTime.text = hours.ToString("00")
                        + ":" + minutes.ToString("00")
                        + ":" + seconds.ToString("00");
                }
            }
        }

        private void UpdateQuestTimer()
        {
            questTimer = QuestInfo.Instance.GetLeftTime(ClientInfo.Instance.ServerTime);

            if (questTimer.TotalSeconds <= 0)
            {
                QuestInfo.Instance.QuestDatas.Status = QuestStatus.None;
            }
            else
            {
                if (questTime != null)
                    questTime.text = CommonTools.DisplayDefaultTime(questTimer);
            }
        }

        private void OnChangeSessionState(Session.State state)
        {
#if !DUNK_PCKLP
            CommonTools.SetActive(localMode, Session.State.LocalMode == state);
#else
            CommonTools.SetActive(localMode, false);
#endif
        }

        private void OnUpdateTimeBonusInfo()
        {
            ResetTimeBonusTimer();
            UpdateTimeBonusTimer();
        }

        private void OnUpdateSafeInfo()
        {
#if DUNK_PCKLP
            CommonTools.SetButtonColor(safeButton, CommonTools.HasSafeBox() ? Color.white : Color.gray);

            ResetSafeExpireTimer();
            UpdateSafeExpireTimer();
#endif
        }

        private void OnQuestStatus()
        {
            if (QuestInfo.Instance.Status() == QuestStatus.Progress)
            {
                CommonTools.SetActive(questActive, true);
                CommonTools.SetActive(questInactive, false);
                CommonTools.SetButtonEnable(questButton, true);
            }
            else
            {
                CommonTools.SetActive(questActive, false);
                CommonTools.SetActive(questInactive, true);
                CommonTools.SetButtonEnable(questButton, false);
            }
        }

        private void OnQuestPush(bool push)
        {
            CommonTools.SetActive(questPush, push);
        }

        private void OnClaimTimeBonus(object msg)
        {
            switch (MyInfo.Instance.TimeBonus.LastClaimState)
            {
                case 0:
                {
                    var body = msg as Dictionary<string, object>;
                    long reward = (Int64)body["reward"];
                    UpdateBonusReward(reward);
                    break;
                }
                case 1:
                {
                    PopupMessage.Create("POPUP.MESSAGE.Notice.Title", "POPUP.MESSAGE.ClaimTimeBonusEmpty", 10f, PopupMessage.Type.Ok);
                    break;
                }
                case 2:
                {
                    if (PopupExceedBalance.Create(true) == null)
                        PopupExceedRewards.Create();
                    break;
                }
                default:
                {
                    Debug.Assert(false);
                    break;
                }
            }
        }

        private void OnDailyBonusInfo(object msg)
        {
            if (Session.Instance.CurrentState != Session.State.InLobby)
                return;

            ShowDailyBonus();
        }

        private void ShowDailyBonus()
        {
            if (Session.Instance.CurrentState != Session.State.InLobby)
                return;

            OnShowDailyBonus(() =>
            {
                OnBannerInfo();
                OnPromotionInfo(null);
                OnEventInfo();
            });
        }
        
        private void OnShowDailyBonus(Action callback)
        {
            if (Session.Instance.CurrentState != Session.State.InLobby)
                return;

            PopupDailyBonus popup = PopupDailyBonus.Create();
            if (popup != null)
            {
                PlayBGM(false);

                popup.ActionClose = () =>
                {
                    PlayBGM(true);

                    popup.ActionClose = null;
                    UpdateBonusReward(PopupDailyBonus.Instance.TotalReward);

                    if (callback != null)
                        callback.Invoke();
                };
            }
            else
            {
                PlayBGM(true);

                if (callback != null)
                    callback.Invoke();
            }
        }

        private void OnBannerInfo()
        {
            if (Session.Instance.CurrentState != Session.State.InLobby)
                return;

            if (PopupDailyBonus.IsVisible)
                return;

            if (PopupChargeup.IsVisible)
                return;

            BannerSystem.Instance.Show();
        }

        private void OnPromotionInfo(object msg)
        {
            if (Session.Instance.CurrentState != Session.State.InLobby)
                return;

            OnGoldtreeBadge(msg);

            if (PopupDailyBonus.IsVisible)
                return;

            if (QuestInfo.Instance.AutoPopup)
            {
                QuestInfo.Instance.ActionPromotionInfo = OnPromotionInfo;
            }
            else
            {
                QuestInfo.Instance.ActionPromotionInfo = null;
                PopupPromotion.Create();
            }
        }

        private void OnEventInfo()
        {
#if DUNK_PCKLP
            if (Session.Instance.CurrentState != Session.State.InLobby)
                return;

            if (PopupDailyBonus.IsVisible)
                return;

            if (PopupChargeup.IsVisible)
                return;

            if (string.IsNullOrEmpty(ClientInfo.Instance.LinkType) == false)
            {
                PopupEvent.ShowLink(ClientInfo.Instance.LinkType);
                ClientInfo.Instance.LinkType = string.Empty;
            }
#endif
        }

        private void PlayBGM(bool play)
        {
            if (bgmPlayer != null)
            {
                if (play == true)
                    bgmPlayer.Play();
                else
                    bgmPlayer.Stop();
            }
        }

        private void UpdateBonusReward(long reward)
        {
            foreach (UILabel label in balanceReward)
                label.text = "+" + CommonTools.MoneyFormatPMCR(reward, true, true);

            CommonTools.PlayAnimation(balanceClaimFX);
        }

        private void OnUpdateInventoryCount()
        {
            if (inventoryPushback != null)
            {
                int inventoryCount = Mathf.Max(
                    LetterInfo.Instance.UnreadCount + 
                    LetterInfo.Instance.UnreadNoticeCount + 
                    GiftInfo.Instance.TotalCount, 0);

                inventoryPushback.SetActive(inventoryCount > 0);
                inventoryPushback.GetComponentInChildren<UILabel>().text = inventoryCount.ToString();
            }
        }

        private void OnUpdateEventCount()
        {
            if (eventPushback != null)
                eventPushback.SetActive(EventInfo.Instance.UnreadCount > 0);
        }

        private void OnUpdateProfileImage()
        {
            if (avatar != null)
                avatar.Load(MyInfo.Instance.ProfileImage);
        }

        private void OnUpdateNickName()
        {
            if (userName != null)
                userName.text = MyInfo.Instance.NickName;
        }

        private void OnUpdateAssetInfo()
        {
            foreach (UILabel label in balance)
                label.text = CommonTools.MoneyFormatPMCR(
                    MyInfo.Instance.Asset.Balance, true, true);

            CommonTools.SetActive(exceedBalance, CommonTools.HasExtraBalance());
        }

        private void OnUpdateLevelInfo()
        {
            if (level != null)
            {
                foreach (UILabel label in level)
                    label.text = MyInfo.Instance.Level.Level.ToString();
            }

            SetupLevelGauge();

            if (expGauge != null)
            {
                float range = (float)(MyInfo.Instance.Level.MaxExp - MyInfo.Instance.Level.MinExp);
                float current = (float)(MyInfo.Instance.Level.Exp - MyInfo.Instance.Level.MinExp);
                expGauge.fillAmount = (range == 0) ? 0f : current / range;
            }
        }

        private void OnUpdateVideoAdsMoneyText(object msg)
        {
            videoAdsMoney.text = VideoAdsInfo.Instance.GetReward((int)VideoAdsInfo.Instance.TodayViewCount + 1).ToString("N0");
        }

        private void OnUIRootDefault()
        {
            var loading = PopupLoading.Instance.gameObject;
            CommonTools.GetUniWebViewEdgeInsets(loading != null ? loading : gameObject, UIRoot.Scaling.FixedSize);

            PopupLoading.Show(false);
        }

        private void OnClickPPIAds()
        {
            SessionService.Instance.ShowPPIAds();
        }

        private void OnClickTimeBonus()
        {
#if !DUNK_PCKLP
            if (SessionConnection.Instance.CheckOnlineMode() == false)
                return;
#endif
            CommonTools.PlayAnimation(timeBonusReceivingFX);
            CommonTools.SetButtonEnable(timeBonusButton, false);
            SessionService.Instance.Request("/claim_time_bonus");
        }

        private void OnClickQuest()
        {
            PopupQuest.Create();
        }

        private void OnClickVideoAds()
        {
            PopupVideoAds.Create();
        }

        private void OnClickMyInfo()
        {
            PopupProfile.Create();
        }

        private void OnClickGoldTree()
        {
            PopupEvent.ShowLink("gold tree");
        }

        private void OnClickTopMenu()
        {
            PopupLobbyMenu.Create();
        }

        private void OnClickInventory()
        {
            PopupInventory.Create();
        }

        private void OnClickExchange()
        {
            PopupLoading.Show(true);
            var loading = PopupLoading.Instance.gameObject;

            CommonTools.SetActive(backLight, true);
            SessionAuth.Instance.OpenExchangePopup(loading != null ? loading : gameObject, (result) =>
            {
                CommonTools.SetActive(backLight, false);
            }, UIRoot.Scaling.FixedRatio);
        }

        private void OnClickChargeup()
        {
            PopupChargeup.Create();
        }

        private void OnClickSafeBox()
        {
            if (CommonTools.HasSafeBox())
            {
                PopupSafeBox.Create();
            }
            else
            {
                PopupMessage popup = PopupMessage.Create(
                    "PCS.POPUP.MESSAGE.SAFE.Title", "PCS.POPUP.MESSAGE.SAFE.NeedToClass", 0f, PopupMessage.Type.OkCancel);

                popup.OkText = "PCS.POPUP.MESSAGE.ViewPokerPass";
                popup.CancelText = "POPUP.MESSAGE.Ok";
                popup.ActionCommand = (command) =>
                {
                    popup.ActionCommand = null;
                    if (command == PopupMessage.Command.Ok)
                        Application.OpenURL(ConfigInfo.Instance.FindInfo("url.premium_item"));
                };
            }
        }

        private void OnClickExceedBalance()
        {
            PopupExceedBalance.Create(true);
        }

        private void OnZoomIn()
        {
            // NOTE: this method called from the LOBBY.ZoomIn animation.

            CommonTools.SetActive(balance, true);
            CommonTools.SetActive(balanceReward, false);
            CommonTools.SetActive(timeBonusReceivingFX, false);
        }

        private void OnZoomOut()
        {
            // NOTE: this method called from the LOBBY.ZoomOut animation.

            CommonTools.SetActive(balance, true);
            CommonTools.SetActive(balanceReward, false);
            CommonTools.SetActive(timeBonusReceivingFX, false);
        }

        private void OnOfferwallAdClosed(object sender, Adiscope.Model.ShowResult args)
        {
            PopupLoading.Show(false);
        }

        private void OnOfferwallAdFailedShow(object sender, Adiscope.Model.ShowFailure args)
        {
            PopupLoading.Show(false);
            PopupMessage.Create("POPUP.MESSAGE.Notice.Title", "POPUP.MESSAGE.NoOfferwall", 10f, PopupMessage.Type.Ok);
        }
    }
}
