// PopupStore.cs - PopupStore implementation file
//
// Description      : PopupStore
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/05/24
// Last Update      : 2019/05/24
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
using Dorothy.UI;

namespace Dorothy.UI.Popup
{
    public class PopupStore : PopupBase
    {
        [SerializeField] private Image background;
        [SerializeField] private Button closeButton;
        [SerializeField] private StoreListView listView;

        [SerializeField] private Image bonusIcon;
        [SerializeField] private Image bonusIconDim;
        [SerializeField] private Image bonusBg;
        [SerializeField] private Sprite bonusBgActive;
        [SerializeField] private Sprite bonusBgDisable;
        [SerializeField] private Button bonusButton;
        [SerializeField] private Text bonusTimer;
        [SerializeField] private Text bonusValue;

        [SerializeField] private CoinMoveController coinController;
        [SerializeField] private ParticleSystem coinFX;
        [SerializeField] private SoundPlayer soundCoin;

        public Action actionTimeout = null;

        private StoreData storeData = null;
        private StoreBonus storeBonusData = null;
        private UserTracking.Location trackingLocation = UserTracking.Location.none;
        private bool isAuto = false;
        private bool isApplicationQuit = false;

        private Coroutine coroutineBonusTimer = null;
        private bool isBonus = false;

        public static PopupOption<PopupStore> Open(UserTracking.Location location = UserTracking.Location.none, bool auto = false, float timeout = 0, int order = 0, float backAlpha = 0.8f)
        {
            var option = PopupSystem.Instance.Register<PopupStore>("popup", "POPUP_Store", order, backAlpha).OnInitialize(p => p.Initialize(timeout, location, auto)).SelfActive();
            return option;
        }

        public void Initialize(float timeout, UserTracking.Location location = UserTracking.Location.none, bool auto = false)
        {
            CancelInvoke("OnTimeout");

            actionTimeout = null;
            trackingLocation = location;
            isAuto = auto;

            if (timeout > 0)
                Invoke("OnTimeout", timeout);

            if (coroutineBonusTimer != null)
            {
                StopCoroutine(coroutineBonusTimer);
                coroutineBonusTimer = null;
            }

            SetData();
        }

        protected override void OnEnable()
        {
            if (Application.isPlaying)
            {
                StoreInfo.Instance.ActionUpdate += SetUI;
            }
        }

        protected override void OnDisable()
        {
            if (Application.isPlaying)
            {
                StoreInfo.Instance.ActionUpdate -= SetUI;
                StopBonusCoroutine();
            }
        }

        private void SetData()
        {
            SessionLobby.Instance.RequestStoreSales(() =>
            {
                if (StoreInfo.Instance.StoreData != null)
                {
                    if (trackingLocation != UserTracking.Location.none)
                        UserTracking.Instance.LogPurchaseImp((long)StoreInfo.Instance.StoreData.storeId, UserTracking.PurchaseWhich.reference, isAuto ? "A" : "M", trackingLocation);

                    SetUI();

                    SetActive();

                    SetBonusUI();
                }
                else
                {
                    Error("SetData Store");
                }
            });
        }

        private void SetUI()
        {
            storeData = StoreInfo.Instance.StoreData;
            SetBackground();
            SetList();
        }

        private void SetBackground()
        {
            if (background == null)
                return;

            ImageDownloader.Instance.DownloadSprite(storeData.bgImg, (id, state, progress, sprite) =>
            {
                if ((state == ImageDownloader.DownloadImageInfo.State.Succeeded) && (sprite != null))
                {
                    if (background != null)
                    {
                        background.sprite = sprite;
                        background.SetNativeSize();
                    }
                }
                else if (state == ImageDownloader.DownloadImageInfo.State.Downloading)
                {
                }
            });
        }

        private void SetList()
        {
            if (listView == null)
                return;

            listView.Build(storeData, isAuto ? "A" : "M", trackingLocation);
        }

        private void SetBonusUI()
        {
            isBonus = false;
            StopBonusCoroutine();

            storeBonusData = StoreInfo.Instance.StoreBonusData;
            if (storeBonusData == null)
            {
                CommonTools.SetActive(bonusButton, false);
                CommonTools.SetActive(bonusTimer, false);
                CommonTools.SetActive(bonusValue, false);
                return;
            }

            CommonTools.SetActive(bonusButton, true);

            if (bonusTimer != null)
            {
                //TimeSpan timeout = CommonTools.ToPSTTime(CommonTools.JavaTime(bonusData.bonusNextDate)) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
                //if (timeout <= TimeSpan.Zero)
                //{
                //    CommonTools.SetActive(bonusTimer, false);
                //    CommonTools.SetActive(bonusValue, true);
                //}
                //else
                //{

                if (storeBonusData.bonusAvailable == false)
                {
                    coroutineBonusTimer = StartCoroutine("BonusDisplayTime");
                }
                else
                {
                    isBonus = true;

                    CommonTools.SetActive(bonusTimer, false);
                    CommonTools.SetActive(bonusValue, true);

                    CommonTools.SetActive(bonusIcon, true);
                    CommonTools.SetActive(bonusIconDim, false);

                    if (bonusBgActive != null)
                        bonusBg.sprite = bonusBgActive;

                    if (bonusValue != null)
                        bonusValue.text = storeBonusData.totalBonusCoin.ToString("N0");
                }
                //}
            }
        }

        private IEnumerator BonusDisplayTime()
        {
            while (true)
            {
                if (isApplicationQuit == true)
                    break;

                TimeSpan timeout = CommonTools.ToPSTTime(CommonTools.JavaTime(storeBonusData.bonusNextDate)) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);

                if (timeout >= TimeSpan.Zero)
                {
                    isBonus = false;

                    CommonTools.SetActive(bonusTimer, true);
                    CommonTools.SetActive(bonusValue, false);

                    CommonTools.SetActive(bonusIcon, false);
                    CommonTools.SetActive(bonusIconDim, true);

                    if (bonusBgDisable != null)
                        bonusBg.sprite = bonusBgDisable;

                    if (bonusTimer != null)
                        bonusTimer.text = string.Format("{0} {1}", CommonTools.DisplayDigitalTime(timeout), "LEFT");

                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);

                    CommonTools.SetActive(bonusTimer, false);
                    CommonTools.SetActive(bonusValue, true);

                    CommonTools.SetActive(bonusIcon, true);
                    CommonTools.SetActive(bonusIconDim, false);

                    if (bonusBgActive != null)
                        bonusBg.sprite = bonusBgActive;

                    if (bonusValue != null)
                        bonusValue.text = storeBonusData.totalBonusCoin.ToString("N0");

                    isBonus = true;

                    yield break;
                }
            }
        }

        private void StopBonusCoroutine()
        {
            if (coroutineBonusTimer != null)
            {
                StopCoroutine(coroutineBonusTimer);
                coroutineBonusTimer = null;
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

        public void OnClickBonus()
        {
            if (isBonus == false)
            {
                Log("It's not time yet");
                return;
            }

            bonusButton.interactable = false;
            SessionLobby.Instance.RequestStoreBonus((e) =>
            {
                if (e != null)
                {
                    Error("Receive Bonus Error");
                }
                else
                {
                    DateTime bonusTime = CommonTools.JavaTime(StoreInfo.Instance.StoreBonusData.bonusNextDate);
                    TimeSpan span = CommonTools.ToPSTTime(bonusTime) - CommonTools.ToPSTTime(ClientInfo.Instance.ServerTime);
                    double totalSeconds = span.TotalSeconds;

                    LocalNotification.Instance.AddPushMessage(LocalNotification.PushMessageType.Store, "<b>SLOTODAY</b>", "Store Bonus! Did you know you can collect Free Coins from the Shop? Surprise!", (long)totalSeconds);

                    UserInfo.Instance.UpdateUserCoin(() =>
                    {
                        if (coinController != null)
                        {
                            if (coinFX != null)
                            {
                                CommonTools.SetActive(coinFX.gameObject, true);
                                coinFX.Stop();
                                coinFX.Play();
                            }

                            if (soundCoin != null)
                                soundCoin.Play();

                            coinController.StartCoinMoveDelay(() =>
                            {
                                SetBonusUI();
                                bonusButton.interactable = true;
                            });
                        }
                    });
                }
            });
        }

        public void OnClickClose()
        {
            closeButton.interactable = false;
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
                        UserTracking.Instance.LogPurchaseImp(HotDealInfo.Instance.HotDealOfferData[0].offerId, UserTracking.PurchaseWhich.hotdeal, "A", UserTracking.Location.lobby_main);
                        PopupHotDealOffer.Open(null, () =>
                        {
                            OpenRewardAdPopup();
                        },
                        "A", UserTracking.Location.lobby_main);
                    }
                    else
                    {
                        OpenRewardAdPopup();
                    }
                }

                //closeButton.interactable = true;
                base.Close();
            });

        }

        private void OnApplicationQuit()
        {
            isApplicationQuit = true;
            StopBonusCoroutine();
        }
    }
}