// PopupProfile.cs : PopupProfile implementation file
//
// Description     : PopupProfile
// Author          : icoder
// Maintainer      : uhrain7761
// How to use      : 
// Created         : 2016/08/23
// Last Update     : 2017/11/16
// Known bugs      : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using UnityEngine;
using DUNK.DataPool;
using DUNK.Tools;
using DUNK.UI;
using Adiscope.Ads;


namespace DUNK.Popup
{
    public sealed class PopupProfile : PopupBaseLayout<PopupProfile>
    {
        [SerializeField] private UIWWWTexture avatar = null;
        [SerializeField] private UILabel[] balances = null;
        [SerializeField] private UILabel[] level = null;
        [SerializeField] private UILabel[] exp = null;
        [SerializeField] private GameObject[] levelUpLimiteObjects;
        [SerializeField] private UISprite expGauge = null;
        [SerializeField] private UILabelEllipsis userName = null;
        [SerializeField] private UILabelEllipsis userID = null;
        [SerializeField] private UILabel[] freeChipsCount = null;
        [SerializeField] private UILabel safeBoxStatus = null;
        [SerializeField] private GameObject PPIAdsButton = null;
        [SerializeField] private GameObject videoAdsButton = null;
        [SerializeField] private GameObject[] freeChipsButton = null;
        [SerializeField] private GameObject safeBoxButton = null;
        [SerializeField] private GameObject backLight = null;

        private int LevelIndex = 0;

        public static PopupProfile Create()
        {
#if !DUNK_PCKLP
            if (SessionConnection.Instance.CheckOnlineMode() == false)
                return null;
#endif
            PopupProfile popup = OnCreate("POPUP.Profile");
            popup.Initialize();
            return popup;
        }

        public void Initialize()
        {
            SetupUI();
            SetupData();
        }

        private void SetupUI()
        {
            CommonTools.SetButtonEnable(freeChipsButton, false);
            CommonTools.SetButtonEnable(videoAdsButton, true);
            CommonTools.SetButtonEnable(PPIAdsButton, true);
            CommonTools.SetButtonEnable(safeBoxButton, false);
            CommonTools.SetActive(backLight, false);

            CheckLevelGauge();
        }

        private void SetupData()
        {
            ShowLoading(true);

            SessionService.Instance.Request("/freechips");
            SessionService.Instance.Request("/user_asset");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SessionService.Instance.ActionFreeChipsInfo     += OnGetFreeChipsInfo;
            SessionService.Instance.ActionClaimFreeChips    += OnClaimFreeChips;
            MyInfo.Instance.ActionUpdateUserBaseInfo        += OnUpdateUserBaseInfo;
            MyInfo.Instance.ActionUpdateProfileImage        += OnUpdateProfileImage;
            MyInfo.Instance.ActionUpdateNickName            += OnUpdateNickName;
            MyInfo.Instance.ActionUpdateAssetInfo           += OnUpdateAssetInfo;
            MyInfo.Instance.ActionUpdateLevelInfo           += OnUpdateLevelInfo;
            MyInfo.Instance.ActionUpdateFreeChipsInfo       += OnUpdateFreeChipsInfo;
            MyInfo.Instance.ActionUpdateSafeInfo            += OnUpdateSafeInfo;
            OfferwallAd.Instance.OnClosed                   += OnOfferwallAdClosed;

            OnUpdateUserBaseInfo();
            OnUpdateProfileImage();
            OnUpdateNickName();
            OnUpdateAssetInfo();
            OnUpdateLevelInfo();
            OnUpdateFreeChipsInfo();
            OnUpdateSafeInfo();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SessionService.Instance.ActionFreeChipsInfo     -= OnGetFreeChipsInfo;
            SessionService.Instance.ActionClaimFreeChips    -= OnClaimFreeChips;
            MyInfo.Instance.ActionUpdateUserBaseInfo        -= OnUpdateUserBaseInfo;
            MyInfo.Instance.ActionUpdateProfileImage        -= OnUpdateProfileImage;
            MyInfo.Instance.ActionUpdateNickName            -= OnUpdateNickName;
            MyInfo.Instance.ActionUpdateAssetInfo           -= OnUpdateAssetInfo;
            MyInfo.Instance.ActionUpdateLevelInfo           -= OnUpdateLevelInfo;
            MyInfo.Instance.ActionUpdateFreeChipsInfo       -= OnUpdateFreeChipsInfo;
            MyInfo.Instance.ActionUpdateSafeInfo            -= OnUpdateSafeInfo;
            OfferwallAd.Instance.OnClosed                   -= OnOfferwallAdClosed;
        }

        private void CheckLevelGauge()
        {
            if (MyInfo.Instance.Level.Level >= MyInfo.Instance.Level.MaxLevel)
            {
                LevelIndex = 1;
                CommonTools.SetActive(levelUpLimiteObjects[1], true);
                CommonTools.SetActive(levelUpLimiteObjects[0], false);
                expGauge = levelUpLimiteObjects[1].transform.Find("Gauge").GetComponent<UISprite>();
            }
            else
            {
                LevelIndex = 0;
                CommonTools.SetActive(levelUpLimiteObjects[0], true);
                CommonTools.SetActive(levelUpLimiteObjects[1], false);
                expGauge = levelUpLimiteObjects[0].transform.Find("Gauge").GetComponent<UISprite>();
            }
        }

        private void OnGetFreeChipsInfo(object msg)
        {
            ShowLoading(false);
        }

        private void OnClaimFreeChips(object msg)
        {
            PopupLoading.Show(false);
            CommonTools.SetButtonEnable(freeChipsButton, true);
        }

        private void OnUpdateUserBaseInfo()
        {
            if (userID != null)
                userID.text = MyInfo.Instance.UID;
        }

        private void OnUpdateProfileImage()
        {
            avatar.Load(MyInfo.Instance.ProfileImage);
        }

        private void OnUpdateNickName()
        {
            if (userName != null)
                userName.text = MyInfo.Instance.NickName;
        }

        private void OnUpdateAssetInfo()
        {
            foreach (UILabel balance in balances)
            {
                balance.text = CommonTools.MoneyFormat(
                    MyInfo.Instance.Asset.Balance, MoneyCurrency.None, MoneyType.FullMoney, true, false, false);
            }
        }

        private void OnUpdateLevelInfo()
        {
            if (level[LevelIndex] != null)
                level[LevelIndex].text = MyInfo.Instance.Level.Level.ToString();

            CheckLevelGauge();

            float range = (float)(MyInfo.Instance.Level.MaxExp - MyInfo.Instance.Level.MinExp);
            float current = (float)(MyInfo.Instance.Level.Exp - MyInfo.Instance.Level.MinExp);
            float percent = (range == 0) ? 0f : current / range;

            if (expGauge != null)
                expGauge.fillAmount = percent;

            if (exp[LevelIndex] != null)
            {
                if (MyInfo.Instance.Level.MaxExp < 0)
                    exp[LevelIndex].text = "(100%)";
                else
                    exp[LevelIndex].text = (percent == 1f) ? "(100%)" : string.Format("({0:P})", percent);
            }
        }

        private void OnUpdateFreeChipsInfo()
        {
            if (freeChipsCount != null)
            {
                foreach (UILabel label in freeChipsCount)
                {
                    label.text = string.Format(
                        LocalizationSystem.Instance.Localize("POPUP.PROFILE.FreeChips.Description"), MyInfo.Instance.FreeChips.LeftCount);
                }
            }

            CommonTools.SetButtonEnable(freeChipsButton, true);
        }

        private void OnUpdateSafeInfo()
        {
            if (safeBoxStatus != null)
            {
                safeBoxStatus.text =
                    CommonTools.HasSafeBox() ?
                    LocalizationSystem.Instance.Localize("PCS.POPUP.CHARGEUP.HasSafe") :
                    LocalizationSystem.Instance.Localize("PCS.POPUP.CHARGEUP.NoSafe");
            }

            CommonTools.SetButtonEnable(safeBoxButton, true);
        }

        private void OnClickAvatar()
        {
            CommonTools.SetActive(backLight, true);
            SessionAuth.Instance.OpenProfileAvatar((result) =>
            {
                CommonTools.SetActive(backLight, false);
            });
        }

        private void OnClickNickName()
        {
            CommonTools.SetActive(backLight, true);
            SessionAuth.Instance.OpenProfileNickName((result) =>
            {
                CommonTools.SetActive(backLight, false);
            });
        }

        private void OnClickAccount()
        {
            SessionAuth.Instance.OpenDashboard();
        }

        private void OnClickPPIAds()
        {
            CommonTools.SetButtonEnable(PPIAdsButton, false);
            SessionService.Instance.ShowPPIAds();
        }

        private void OnOfferwallAdClosed(object sender, Adiscope.Model.ShowResult args)
        {
            PopupLoading.Show(false);
            CommonTools.SetButtonEnable(PPIAdsButton, true);
            Debug.Log("OfferwallAdCallback - Close");
        }

        private void OnClickVideoAds()
        {
            PopupVideoAds.Create();
            Close();
        }

        private void OnClickFreeChips()
        {
            if (MyInfo.Instance.FreeChips.AvailableMinMoney < MyInfo.Instance.TotalBalance)
            {
#if DUNK_PCKLP
                string message = string.Format(
                    LocalizationSystem.Instance.Localize("POPUP.MESSAGE.FreeChipsLimit.PC"),
                    MyInfo.Instance.FreeChips.AvailableMinMoney);
#else
                string message = string.Format(
                    LocalizationSystem.Instance.Localize("POPUP.MESSAGE.FreeChipsLimit"),
                    MyInfo.Instance.FreeChips.AvailableMinMoney);
#endif
                PopupMessage.Create("POPUP.MESSAGE.Notice.Title", message, 0f, PopupMessage.Type.Ok);
            }
            else if (MyInfo.Instance.FreeChips.LeftCount <= 0)
            {
                PopupMessage.Create("POPUP.MESSAGE.Notice.Title", "POPUP.MESSAGE.FreeChipsLimitCount", 0f, PopupMessage.Type.Ok);
            }
            else
            {
                PopupLoading.Show(true);
                CommonTools.SetButtonEnable(freeChipsButton, false);
                SessionService.Instance.Request("/claim_freechips");
            }
        }

        private void OnClickSafeBox()
        {
            if (CommonTools.HasSafeBox())
            {
                Close();
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
                    {
                        Application.OpenURL(ConfigInfo.Instance.FindInfo("url.premium_item"));
                    }
                };
            }
        }
    }
}
