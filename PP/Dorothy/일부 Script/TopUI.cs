// TopUI.cs - TopUI implementation file
//
// Description      : TopUI
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/02/27
// Last Update      : 2020/06/22
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using UnityEngine.SceneManagement;
using Dorothy.DataPool;
using Dorothy.UI.Popup;
using System.Collections.Generic;
using DG.Tweening;
using PlayEarth.Unity.Auth;

namespace Dorothy.UI
{
    public sealed class TopUI : UIBaseLayout<TopUI>
    {
        public enum TopBarColor
        {
            Yellow = 0,
            Red,
            Blue,
            Pupple,
            Black,
            Brown,
            Max
        }
        [SerializeField] private UserLevelUI userLevelUI;
        [SerializeField] private Text coin;

        [SerializeField] private GameObject levelUpPopup;
        [SerializeField] private GameObject levelupPage0;
        [SerializeField] private GameObject levelupPage1;
        [SerializeField] private Text levelupLevel;
        [SerializeField] private Text levelupReward;
        [SerializeField] private Text levelupBonus;
        [SerializeField] private Text levelupMegaBonus;
        [SerializeField] private Text levelupMaxBet;

        [SerializeField] private GameObject levelupEffectObj;
        [SerializeField] private Text levelupEffectName;
        [SerializeField] private Text levelupEffectValue;

        [SerializeField] private GameObject piggyEffect;

        [SerializeField] private GameObject backButton;
        [SerializeField] private GameObject fbProfile;
        [SerializeField] private Image fbPicture;
        [SerializeField] private Image fbSanctionsProfile;
        [SerializeField] private Button shopButton;
        [SerializeField] private GameObject dillObj;
        [SerializeField] private Button dillButton;
        [SerializeField] private Text dillTime;
        [SerializeField] private GameObject menu;
		[SerializeField] private GameObject inGameMenu;
		[SerializeField] private Image topBarBgCenter;
        [SerializeField] private Image topBarBgLeft;
        [SerializeField] private Image topBarBgRight;
        [SerializeField] private Image profileBtnBg;
        [SerializeField] private Image backBtnBg;
        [SerializeField] private Image levelBg;
        [SerializeField] private Image menuBg;

        [SerializeField] private Button fbShareButton;
        [SerializeField] private CoinMoveController coinController;
        [SerializeField] private GameObject coinFX;
        [SerializeField] private SoundPlayer soundCoin;

        [SerializeField] private Animation aniLevelUpToast;

        [Serializable] public class StoreBonusAnimation
        {
            public Animation animation;
            public AnimationClip idle;
        }

        [SerializeField] private GameObject storeBonusIcon;
        [SerializeField] private StoreBonusAnimation storeBonusAnimation;

        private SpriteAtlas atlasUITopBG;

        public Action ActionLevelupClose = null;
		public event Action ActionBackClicked;

		private readonly float HIDE_Y = 125.0f;

		private List<Button> buttonList = new List<Button>();

        private Coroutine coroutineCoin = null;

        private bool showLevelUP = false;
        private bool showLevelUPPage1 = false;
        private bool showLevelUPPage2 = false;
        private float levelupPageTimer = 0;

        private bool isInGameMode = false;
		private Action showPaytableCallback;

        private float aniLenthLvUpToastOpen = 0.0f;
        private float aniLenthLvUpToastClose = 0.0f;

        private bool openMenu = false;
        private float menuTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
            SetUI();
            userLevelUI.Init();
            
            buttonList.Clear();
			buttonList.AddRange(GetComponentsInChildren<Button>(true));

			piggyEffect.SetActive(false);
		}

        private void Start()
        {
            AnimationClip lvUpToastOpenClip = aniLevelUpToast.GetClip("LevelupOpen");
            if (lvUpToastOpenClip != null)
                aniLenthLvUpToastOpen = lvUpToastOpenClip.length;

            AnimationClip lvUpToastCloseClip = aniLevelUpToast.GetClip("LevelupClose");
            if (lvUpToastCloseClip != null)
                aniLenthLvUpToastClose = lvUpToastCloseClip.length;
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                UserInfo.Instance.ActionUpdateAssetInfo += SetAssetInfo;
                HotDealInfo.Instance.ActionUpdateOffer += SetShopButton;
                TimerSystem.Instance.ActionOfferTimer += SetOfferTimer;
                TimerSystem.Instance.ActionStoreBonusTimer += SetStoreBonusIcon;
                LobbyInfo.Instance.ActionLobbyStoreBonus += SetStoreBonus;
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                UserInfo.Instance.ActionUpdateAssetInfo -= SetAssetInfo;
                HotDealInfo.Instance.ActionUpdateOffer -= SetShopButton;
                LobbyInfo.Instance.ActionLobbyStoreBonus -= SetStoreBonus;

                if (TimerSystem.I != null)
				{
					TimerSystem.Instance.ActionOfferTimer -= SetOfferTimer;
                    TimerSystem.Instance.ActionStoreBonusTimer -= SetStoreBonusIcon;
                }
            }
        }

        private void Update()
        {
            if (showLevelUP)
            {
                if (showLevelUPPage1 == false)
                    ShowLevelUpPopup();

                levelupPageTimer += Time.deltaTime;
                if (levelupPageTimer >= 3f)
                {
                    ShowLevelUpPopup();
                    levelupPageTimer = 0f;
                }
            }

            if (openMenu)
            {
                menuTimer += Time.deltaTime;
                if (menuTimer >= 3f)
                    SetMenu(false);
            }
        }

        private void SetUI()
        {
            CommonTools.SetActive(levelUpPopup, false);

            if (fbShareButton != null)
                CommonTools.SetActive(fbShareButton, Auth.IsFacebook);

            SetTopUI();
            SetProfile();
            SetAssetInfo();
            SetShopButton();
            SetStoreBonus();
            SetMenu(false);

            menuTimer = 0f;
        }

        public void SetTopUI(TopBarColor color = TopBarColor.Yellow)
		{
            Sprite spTopBg1 = null;
            Sprite spTopBg2 = null;
            Sprite spBackBg = null;
            Sprite spLevelBg = null;

            atlasUITopBG = ResourceManager.Instance.Load<SpriteAtlas>("topui", "SpriteAtlas_TopUI");
            if (atlasUITopBG == null)
                return;

            switch (color)
            {
                case TopBarColor.Red:
                    spTopBg1 = atlasUITopBG.GetSprite("top-barred01");
                    spTopBg2 = atlasUITopBG.GetSprite("top-barred02");
                    spBackBg = atlasUITopBG.GetSprite("menu-bgred");
                    spLevelBg = atlasUITopBG.GetSprite("level-bgred");
                    break;

                case TopBarColor.Yellow:
                    //spTopBg1 = atlasUITopBG.GetSprite("top-baryellow01");
                    //spTopBg2 = atlasUITopBG.GetSprite("top-baryellow02");
                    spTopBg1 = atlasUITopBG.GetSprite("top-bar01");
                    spTopBg2 = atlasUITopBG.GetSprite("top-bar02");
                    spBackBg = atlasUITopBG.GetSprite("menu-bg");
                    spLevelBg = atlasUITopBG.GetSprite("level-bg");
                    break;

                case TopBarColor.Blue:
                    spTopBg1 = atlasUITopBG.GetSprite("top-barblue01");
                    spTopBg2 = atlasUITopBG.GetSprite("top-barblue02");
                    spBackBg = atlasUITopBG.GetSprite("menu-bgblue");
                    spLevelBg = atlasUITopBG.GetSprite("level-bgblue");
                    break;

                case TopBarColor.Pupple:
                    spTopBg1 = atlasUITopBG.GetSprite("top-barpupple01");
                    spTopBg2 = atlasUITopBG.GetSprite("top-barpupple02");
                    spBackBg = atlasUITopBG.GetSprite("menu-bgpupple");
                    spLevelBg = atlasUITopBG.GetSprite("level-bgpupple");
                    break;

                case TopBarColor.Black:
                    spTopBg1 = atlasUITopBG.GetSprite("top-barblack01");
                    spTopBg2 = atlasUITopBG.GetSprite("top-barblack02");
                    spBackBg = atlasUITopBG.GetSprite("menu-bgblack");
                    spLevelBg = atlasUITopBG.GetSprite("level-bgblack");
                    break;

                case TopBarColor.Brown:
                    spTopBg1 = atlasUITopBG.GetSprite("top_barbrown01");
                    spTopBg2 = atlasUITopBG.GetSprite("top_barbrown02");
                    spBackBg = atlasUITopBG.GetSprite("menu-bg");
                    spLevelBg = atlasUITopBG.GetSprite("level-bg");
                    break;
            }

            if (topBarBgCenter != null)
                topBarBgCenter.sprite = spTopBg1;

            if (topBarBgLeft != null)
                topBarBgLeft.sprite = spTopBg2;

            if (topBarBgRight != null)
                topBarBgRight.sprite = spTopBg2;

            if (profileBtnBg != null)
                profileBtnBg.sprite = spBackBg;

            if (backBtnBg != null)
                backBtnBg.sprite = spBackBg;

            if (levelBg != null)
                levelBg.sprite = spLevelBg;

            if (menuBg != null)
                menuBg.sprite = spBackBg;
        }

		public void SetPaytableCallback(Action showPaytableCallback)
		{
			isInGameMode = true;
			this.showPaytableCallback = showPaytableCallback;
		}

        private void SetAssetInfo()
        {
            if (coroutineCoin != null)
                StopCoroutine(coroutineCoin);

            if (coin != null)
            {
                ulong value = 0;
                ulong.TryParse(coin.text.Replace(",", ""), out value);
                coroutineCoin = StartCoroutine(TweenUp(coin, value, UserInfo.Instance.Coin));
            }
        }

        private IEnumerator TweenUp(Text label, ulong start, ulong end)
        {
            ulong value = end - start;
            float during = 0.5f;
            float time = 0.5f;

            while (true)
            {
                yield return null;

                if (start < end)
                    start += (ulong)(value * (Time.deltaTime / during));
                else
                    start = end;

                label.text = start.ToString("N0");

                if (start >= end || time <= 0f)
                {
                    start = end;
                    break;
                }

                time -= Time.deltaTime;
            }

            label.text = start.ToString("N0");
        }

        private void SetProfile()
        {
            if (fbProfile != null && backButton != null)
            {
                if (Session.Instance.CurrentState == Session.State.InLobby ||
                    Session.Instance.CurrentState == Session.State.GameToInLobby)
                {
                    CommonTools.SetActive(fbProfile, true);
                    CommonTools.SetActive(backButton, false);
                }
                else
                {
                    CommonTools.SetActive(fbProfile, false);
                    CommonTools.SetActive(backButton, true);
                }
            }

            if (fbPicture != null)
            {
                if (Auth.IsFacebook)
                {
                    if (string.IsNullOrEmpty(UserInfo.Instance.ProfileImage) || UserInfo.Instance.ProfileImage == "none")
                    {
                        CommonTools.SetActive(fbSanctionsProfile, true);
                        CommonTools.SetActive(fbPicture, false);
                    }
                    else
                    {
                        CommonTools.SetActive(fbSanctionsProfile, false);
                        DownloadProfile(fbPicture);
                    }
                }
                else
                {
                    //CommonTools.SetActive(fbPicture, false);
                    CommonTools.SetActive(fbSanctionsProfile, false);
                }
            }
        }

        private void DownloadProfile(Image profile)
        {
            ImageDownloader.Instance.DownloadSprite(UserInfo.Instance.ProfileImage, (id, state, progress, sprite) =>
            {
                if ((state == ImageDownloader.DownloadImageInfo.State.Succeeded) && (sprite != null))
                {
                    if (profile != null)
                        profile.sprite = sprite;
                    CommonTools.SetActive(profile, true);
                }
                else if (state == ImageDownloader.DownloadImageInfo.State.Downloading)
                {
                    CommonTools.SetActive(profile, false);
                }
            });
        }

        private void SetShopButton()
        {
            if (shopButton == null || dillButton == null)
                return;

            if (dillTime != null)
                CommonTools.SetActive(dillTime, false);

            TimerSystem.Instance.StopOfferTimer();

            if (TimerSystem.Instance.RestOfferTime > 0)
                HotDealInfo.Instance.UpdateOfferTime(TimerSystem.Instance.RestOfferTime);

            var data = HotDealInfo.Instance.HotDealOfferData;

            if (data == null || data.Count <= 0)
            {
                CommonTools.SetActive(shopButton, true);
                CommonTools.SetActive(dillObj, false);
                CommonTools.SetActive(dillButton, false);
            }
            else
            {
                CommonTools.SetActive(shopButton, false);
                CommonTools.SetActive(dillObj, true);
                CommonTools.SetActive(dillButton, true);

                TimerSystem.Instance.StartOfferTimer(ClientInfo.Instance.ServerTime.AddSeconds(data[0].expireSeconds));
            }
        }

        private void SetOfferTimer(string strTimer)
        {
            if (dillTime == null)
                return;

            CommonTools.SetActive(dillTime, true);
            dillTime.text = strTimer;
        }

        private void SetStoreBonus()
        {
            SetStoreBonusIcon(false);

            var storeBonusData = LobbyInfo.Instance.LobbyStoreBonus;

            if (storeBonusData != null)
                TimerSystem.Instance.StartStoreBonusTimer(CommonTools.JavaTime(storeBonusData.bonusNextDate));
        }

        private void SetStoreBonusIcon(bool show)
        {
            if (storeBonusIcon == null)
                return;

            CommonTools.SetActive(storeBonusIcon, show);

            if (show)
            {
                if (storeBonusAnimation.animation != null)
                {
                    storeBonusAnimation.animation.Stop();
                    storeBonusAnimation.animation.Play(storeBonusAnimation.idle.name);
                }
            }
        }

        public void OpenLevelUpPopup(string level, string levelBenefitsReward, string maxBetReward, string coinbonusReward, string megaBonusReward, string effectCode, uint effectPeriod, Action closeCallback)
        {
            ActionLevelupClose = closeCallback;

            CommonTools.SetActive(levelUpPopup, true);

            if (levelupLevel != null)
                levelupLevel.text = string.Format("LEVEL {0}", level);

            if (levelupReward != null)
                levelupReward.text = levelBenefitsReward;

            if (levelupBonus != null)
                levelupBonus.text = coinbonusReward;

            if (levelupMegaBonus != null)
                levelupMegaBonus.text = megaBonusReward;

            if (levelupMaxBet != null)
                levelupMaxBet.text = maxBetReward;

            if (string.IsNullOrEmpty(effectCode))
            {
                CommonTools.SetActive(levelupEffectObj, false);
            }
            else
            {
                CommonTools.SetActive(levelupEffectObj, true);

                if (levelupEffectName != null)
                    levelupEffectName.text = string.Format("{0} : ", GetEffectName(effectCode));

                if (levelupEffectValue != null)
                    levelupEffectValue.text = CommonTools.GetEffectPeriodTime(effectPeriod);
            }

            showLevelUP = true;

            //aniLevelUpToast.Play("LevelupOpen");
            Invoke("StartCoinMove", aniLenthLvUpToastOpen);
        }

        private string GetEffectName(string code)
        {
            UserInfo.EffectType type = UserInfo.Instance.GetEffectType(code);
            switch(type)
            {
                case UserInfo.EffectType.Boom:
                    return LocalizationSystem.Instance.Localize("LEVELUP.Tost.Effect.Name.1");
                case UserInfo.EffectType.Faster:
                    return LocalizationSystem.Instance.Localize("LEVELUP.Tost.Effect.Name.2");
                case UserInfo.EffectType.BoomFaster:
                    return LocalizationSystem.Instance.Localize("LEVELUP.Tost.Effect.Name.3");
                default:
                    return string.Empty;
            }
        }

        public void SetUserEffect()
        {
            //TODO:: 이펙트 갱신
            // 전에 관련 이슈로 API 형식으로 바꾸기로 함.
            // (이슈)서버에서 중첩되는 이펙트를 따로 골라내서 클라이언트로 내려주기 때문에 클라에서는 서버에서 내려주는 정보를 그대로 보여주기로 함.
            SessionLobby.Instance.RequestUserEffect(() =>
            {
                Log("Update User Effect");
            });
        }

        public void StartCoinMove()
        {
            if (coinFX != null)
                coinFX.gameObject.SetActive(true);

            if (soundCoin != null)
                soundCoin.Play();

            if (coinController != null)
            {
                coinController.StartCoinMoveDelay(() =>
                {

                });
            }
        }

        private void ShowLevelUpPopup()
        {
            if (showLevelUPPage1 == false)
            {
                CommonTools.SetActive(levelupPage0, true);
                CommonTools.SetActive(levelupPage1, false);
                showLevelUPPage1 = true;
            }
            else
            {
                if (showLevelUPPage2 == false)
                {
                    CommonTools.SetActive(levelupPage0, false);
                    CommonTools.SetActive(levelupPage1, true);
                    showLevelUPPage2 = true;
                }
                else
                {
                    showLevelUPPage1 = false;
                    showLevelUPPage2 = false;
                    showLevelUP = false;

                    if (coinFX != null)
                        coinFX.gameObject.SetActive(false);

                    //CloseLevelupPopup();

                    StartCoroutine(CoroutineCloseLevelupPopup());
                }
            }
        }

        private void OpenRewardAdPopup()
        {
            if (RewardAdInfo.Instance.RewardAdStatus != null)
            {
                if (RewardAdInfo.Instance.RewardAdStatus.rewardedCnt >= RewardAdInfo.Instance.RewardAdStatus.rewardLimitCnt)
                {
                    // 오늘 시청 가능 횟수 초과
                    Log("Exceeded number of views");
                    //PopupRewardAdLimit.Open();
                }
                else
                {
                    PopupRewardAd.Open(RewardAdSystem.ShowType.OutGame);
                }
            }
        }

        public void CloseLevelupPopup()
        {
            CommonTools.SetActive(levelUpPopup, false);
            
            if (ActionLevelupClose != null)
                ActionLevelupClose();
        }

        IEnumerator CoroutineCloseLevelupPopup()
        {
            aniLevelUpToast.Play("LevelupClose");

            yield return new WaitForSeconds(aniLenthLvUpToastClose);

            CloseLevelupPopup();
        }

        public void SetMenu(bool show)
        {
			if (!isInGameMode)
			{
				if (menu != null)
					CommonTools.SetActive(menu, show);
			}
			else
			{
				if (inGameMenu != null)
					CommonTools.SetActive(inGameMenu, show);

			}

            openMenu = show;

            if (openMenu == false)
                menuTimer = 0;
        }

        public bool CheckActiveMenu(bool outgame)
        {
            return outgame ? menu.activeSelf : inGameMenu.activeSelf;
        }

		public void SetPiggyEffect(bool isShow)
		{
			piggyEffect.SetActive(isShow);
		}

		public void SetEnableButtons(bool enable)
		{
			foreach (Button button in buttonList)
			{
				button.interactable = enable;
			}
		}

		public void Show(float duration)
		{
			Move(0, duration, Ease.OutQuint);
		}

		public void Hide(float duration)
		{
			Move(HIDE_Y, duration, Ease.OutQuint);
		}

		private void Move(float targetY, float duration, Ease ease = Ease.OutQuint)
		{
			RectTransform.DOKill(false);
			RectTransform.DOAnchorPosY(targetY, duration).SetEase(ease);
			//uiTransform.DOKill(false);
			//uiTransform.DOAnchorPosY(targetY, duration).SetEase(ease);
		}

		public void OnClickBack()
        {
            Log("OnClickBack");
			if (ActionBackClicked != null)
			{
				ActionBackClicked.Invoke();
			}
        }

        public void OnClickFB()
        {
            Log("OnClickFB");

            PopupLoading.Open();
            Session.Instance.LinkFacebook((exception) => 
            {
                UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.freecoin, UserTracking.EventWhat.fb_connect, "M", UserTracking.Location.lobby_main);
                PopupLoading.CloseSelf();
                PopupSystem.Instance.UnregisterAll();
            });
        }

        public void OnClickShop()
        {
            Log("OnClickShop");

            string curScene = SceneManager.GetActiveScene().name;
            
            PopupStore.Open(curScene == Constants.SCENE_LOBBY ? UserTracking.Location.lobby_main_t : UserTracking.Location.ingame);
        }

        public void OnClickDill()
        {
            Log("OnClickDill");
            dillButton.interactable = false;
            SessionLobby.Instance.RequestHotDealOffer((e) => 
            {
                if (e != null)
                {
                    Error("Error Hot Deal Offer : " + e.Message);
                    OpenRewardAdPopup();    // 에러나면 그냥 동영상광고 팝업 띄움
                }
                else
                {
                    if (HotDealInfo.Instance.HotDealOfferData != null && HotDealInfo.Instance.HotDealOfferData.Count > 0)
                    {
                        string curScene = SceneManager.GetActiveScene().name;


                        UserTracking.Instance.LogPurchaseImp(HotDealInfo.Instance.HotDealOfferData[0].offerId, UserTracking.PurchaseWhich.hotdeal, "M",
                                                            curScene == Constants.SCENE_LOBBY ? UserTracking.Location.lobby_main : UserTracking.Location.ingame);
                        PopupHotDealOffer.Open(null, ()=> 
                        {
                            OpenRewardAdPopup();
                        },
                        "M", curScene == Constants.SCENE_LOBBY ? UserTracking.Location.lobby_main : UserTracking.Location.ingame);
                        dillButton.interactable = true;
                    }
                    else
                    {
                        OpenRewardAdPopup();
                    }
                }
            });
        }

        public void OnClickHotTopics()
        {
            Log("OnClickHotTopics");

            PopupHotTopics.Open();
        }

        public void OnClickPiggyBank()
        {
            Log("OnClickPiggyBank");

            string curScene = SceneManager.GetActiveScene().name;
            
            PopupPiggyBank.Open(curScene == Constants.SCENE_LOBBY ? UserTracking.Location.lobby_main : UserTracking.Location.ingame);
        }

        public void OnClickMenu()
        {
            Log("OnClickMenu");

			if (!isInGameMode)
			{
				SetMenu(!menu.activeSelf);
			}
			else
			{
				SetMenu(!inGameMenu.activeSelf);
			}
        }

		public void OnClickPaytable()
		{
			SetMenu(false);
			if (showPaytableCallback != null)
			{
                UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.setting, UserTracking.EventWhat.pay_table, "M", UserTracking.Location.ingame);
                showPaytableCallback.Invoke();
			}
		}

        public void OnClickRateUs()
        {
            Log("OnClickRateUs");
            SetMenu(false);
            UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.setting, UserTracking.EventWhat.rate_us, "M", UserTracking.Location.lobby_main);

            PopupRateUs.Open(PopupRateUs.Type.Yes, (command) =>
			{
				if (command == PopupRateUs.Command.Yes)
				{
					Warning("TEST COMMAND : RateUs");
				}
				else if (command == PopupRateUs.Command.No)
				{
					Warning("TEST COMMAND : CS");
				}
				else if (command == PopupRateUs.Command.Cancel)
				{
					Warning("TEST COMMAND : Cancel");
				}
			});
        }

        public void OnClickSetting()
        {
            Log("OnClickSetting");
            SetMenu(false);

            string curScene = SceneManager.GetActiveScene().name;
            UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.setting, UserTracking.EventWhat.setting, "M",
                                curScene == Constants.SCENE_LOBBY ? UserTracking.Location.lobby_main_t : UserTracking.Location.ingame);

            PopupSetting.Open();
        }

        public void OnClickAbout()
        {
            Log("OnClickAbout");
            SetMenu(false);

            string curScene = SceneManager.GetActiveScene().name;
            UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.setting, UserTracking.EventWhat.about, "M",
                                curScene == Constants.SCENE_LOBBY ? UserTracking.Location.lobby_main_t : UserTracking.Location.ingame);

            PopupAbout.Open();
        }

        public void CloseLoading()
        {
            PopupLoading.CloseSelf();
        }

        public void OnClickShare()
        {
            //string url = Dorothy.DataPool.AppConfigInfo.Instance.value.app.url.share + Defines.SHARE_LEVEL_UP;
            Session.Instance.ShareLinkFacebook(FacebookFeedInfo.FacebookFeedType.LEVEL_UP.ToString(), (exception, success) => {
                if (success == false)
                {
                    Debug.LogError("ShareLinkFacebook Error: " + exception + "[" + Defines.SHARE_LEVEL_UP.ToString() + "]");
                }
            });
        }
    }
}