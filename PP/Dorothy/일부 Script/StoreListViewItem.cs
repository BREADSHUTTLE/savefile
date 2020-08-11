// StoreListViewItem.cs - StoreListViewItem implementation file
//
// Description      : StoreListViewItem
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/06/17
// Last Update      : 2019/06/17
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
using Dorothy.UI.Popup;

namespace Dorothy.UI
{
    public class StoreListViewItem : MonoBehaviour
    {
        enum Label
        {
            None,
            Most,
            Best,
            Booster,
        }

        [SerializeField] private Image coinIcon;
        [SerializeField] private Image fasterBoosterIcon;
        [SerializeField] private Image boomBoosterIcon;
        [SerializeField] private Image boomFasterBoosterIcon;

        [SerializeField] private GameObject bgBasic;
        [SerializeField] private GameObject bgSpecial;
        [SerializeField] private GameObject bgBooster;
        [SerializeField] private Text coin;
        [SerializeField] private Text totalCoin;
        [SerializeField] private Text coinRate;

        [SerializeField] private Text boosterName;
        [SerializeField] private Text boosterTime;

        [SerializeField] private Text boosterValue;

        [SerializeField] private GameObject coinRateBg;
        [SerializeField] private Text price;
        [SerializeField] private Image mostLabel;
        [SerializeField] private Image bestLabel;
        [SerializeField] private Image boosterLabel;
        [SerializeField] private Button buyButton;
        [SerializeField] private ParticleSystem buyButtonEffect;

        [SerializeField] private float notRateTotalCoinPosY = 4f;
        [SerializeField] private float isRateTotalCoinPosY = 18f;

        private UserGoodsInfo goodsData = null;
        private Label labelType = Label.None;
        private string how = "";
        private UserTracking.Location location = UserTracking.Location.none;

        public void Build(UserGoodsInfo data, string trackingHow, UserTracking.Location trackingLocation)
        {
            if (data == null)
                return;

            goodsData = data;
            how = trackingHow;
            location = trackingLocation;

            SetType();
            SetUI();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Session.Instance.ActionPurchase += SetButton;
                StoreInfo.Instance.ActionBuyItem += SetBuyButtonInteractable;
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                Session.Instance.ActionPurchase -= SetButton;
                StoreInfo.Instance.ActionBuyItem -= SetBuyButtonInteractable;
            }
        }

        private void SetType()
        {
            if (goodsData.salesLabel == null)
            {
                labelType = Label.None;
                return;
            }

            string type = goodsData.salesLabel.ToLower();
            switch (type)
            {
                case "most":
                    labelType = Label.Most;
                    break;
                case "best":
                    labelType = Label.Best;
                    break;
                case "booster":
                    labelType = Label.Booster;
                    break;
                default:
                    labelType = Label.None;
                    break;
            }
        }

        private void SetUI()
        {
            SetIcon();
            SetBackground();

            if (goodsData.effectCode != null)
            {
                SetBoosterName();
                SetBoosterValue();
            }
            else
            {
                SetCoinValue();
                SetRate();
            }
            
            SetPrice();
            SetButton();
        }

        private void SetIcon()
        {
            if (goodsData.effectCode != null)
            {
                UserInfo.EffectType type = UserInfo.Instance.GetEffectType(goodsData.effectCode);

                CommonTools.SetActive(fasterBoosterIcon, type == UserInfo.EffectType.Faster);
                CommonTools.SetActive(boomBoosterIcon, type == UserInfo.EffectType.Boom);
                CommonTools.SetActive(boomFasterBoosterIcon, type == UserInfo.EffectType.BoomFaster);

                CommonTools.SetActive(coinIcon, false);
            }
            else
            {
                CommonTools.SetActive(fasterBoosterIcon, false);
                CommonTools.SetActive(boomBoosterIcon, false);
                CommonTools.SetActive(boomFasterBoosterIcon, false);

                CommonTools.SetActive(coinIcon, true);
            }
        }

        private void SetBackground()
        {
            if (mostLabel != null)
                CommonTools.SetActive(mostLabel, labelType == Label.Most);

            if (bestLabel != null)
                CommonTools.SetActive(bestLabel, labelType == Label.Best);

            if (boosterLabel != null)
                CommonTools.SetActive(boosterLabel, labelType == Label.Booster);

            if (labelType == Label.Booster)
            {
                if (bgBooster != null)
                    CommonTools.SetActive(bgBooster, true);

                if (bgBasic != null)
                    CommonTools.SetActive(bgBasic, false);

                if (bgSpecial != null)
                    CommonTools.SetActive(bgSpecial, false);
            }
            else
            {
                if (bgBooster != null)
                    CommonTools.SetActive(bgBooster, false);

                if (bgBasic != null)
                    CommonTools.SetActive(bgBasic, labelType != Label.Most || labelType != Label.Best);

                if (bgSpecial != null)
                    CommonTools.SetActive(bgSpecial, labelType == Label.Most || labelType == Label.Best);
            }
        }

        private void SetCoinValue()
        {
            if (boosterName != null)
                CommonTools.SetActive(boosterName, false);

            if (boosterTime != null)
                CommonTools.SetActive(boosterTime, false);

            if (coin != null)
            {
                coin.text = goodsData.coin.ToString("N0");
                //CommonTools.SetActive(coin, true);
            }

            if (totalCoin != null)
                totalCoin.text = goodsData.totalCoin.ToString("N0");
        }

        private void SetRate()
        {
            //TODO:: 이 부분 UI 나오면 다시 수정해야 됨.

            if (boosterValue != null)
                CommonTools.SetActive(boosterValue, false);

            if (coinRate != null)
            {
                CommonTools.SetActive(coinRateBg, Convert.ToInt64(goodsData.coinRate) > 0);
                CommonTools.SetActive(coinRate, Convert.ToInt64(goodsData.coinRate) > 0);

                coinRate.text = "+" + goodsData.coinRate + "%";

                if (Convert.ToInt64(goodsData.coinRate) <= 0 || string.IsNullOrEmpty(goodsData.coinRate))
                {
                    CommonTools.SetActive(coin, false);
                    totalCoin.rectTransform.anchoredPosition = new Vector2(totalCoin.rectTransform.anchoredPosition.x, notRateTotalCoinPosY);
                }
                else
                {
                    CommonTools.SetActive(coin, true);
                    totalCoin.rectTransform.anchoredPosition = new Vector2(totalCoin.rectTransform.anchoredPosition.x, isRateTotalCoinPosY);
                }
            }
        }

        private void SetBoosterName()
        {
            if (boosterName == null)
                return;

            if (boosterTime == null)
                return;

            CommonTools.SetActive(coin, false);
            CommonTools.SetActive(totalCoin, false);

            CommonTools.SetActive(boosterName, true);
            CommonTools.SetActive(boosterTime, true);
            
            boosterName.text = GetBoosterName(goodsData.effectCode);
            boosterTime.text = CommonTools.GetEffectPeriodTime(goodsData.effectPeriod);
        }

        private string GetBoosterName(string code)
        {
            switch (UserInfo.Instance.GetEffectType(code))
            {
                case UserInfo.EffectType.Boom:
                    return "BOOM BOOSTER";
                case UserInfo.EffectType.Faster:
                    return "FASTER BOOSTER";
                case UserInfo.EffectType.BoomFaster:
                    return "BOOM FASTER";
                case UserInfo.EffectType.Turbo:
                    return "TURBO BOOSTER";
                default:
                    return string.Empty;
            }
        }
        
        private void SetBoosterValue()
        {
            if (boosterValue == null)
                return;

            CommonTools.SetActive(coinRateBg, true);
            CommonTools.SetActive(coinRate, false);

            CommonTools.SetActive(boosterValue, true);

            UserInfo.EffectType type = UserInfo.Instance.GetEffectType(goodsData.effectCode);

            if (type == UserInfo.EffectType.Boom)
            {
                boosterValue.text = string.Format(LocalizationSystem.Instance.Localize("STORE.Booster.Boom.Value"), goodsData.effectValue);
            }
            else if (type == UserInfo.EffectType.Faster)
            {
                boosterValue.text = string.Format(LocalizationSystem.Instance.Localize("STORE.Booster.Faster.Value"), goodsData.effectValue);
            }
            else
            {
                boosterValue.text = string.Format(LocalizationSystem.Instance.Localize("STORE.Booster.BoomAndFaster.Value"), goodsData.effectValue2, goodsData.effectValue);
            }
        }

        private void SetPrice()
        {
            if (price != null)
                price.text = "$" + goodsData.price;
        }

        private void SetButton()
        {
            if (buyButton != null)
                buyButton.interactable = goodsData.goodsAvailable;

            if (buyButtonEffect != null)
                CommonTools.SetActive(buyButtonEffect, goodsData.goodsAvailable);
        }

        private void SetBuyButtonInteractable(bool isOn)
        {
            if (buyButton != null)
                buyButton.interactable = isOn;
        }

        public void OnClickPurchase()
        {
            //if (buyButton != null)
            //    buyButton.interactable = false;

            if (StoreInfo.Instance.ActionBuyItem != null)
                StoreInfo.Instance.ActionBuyItem(false);

#if Purchase_Test
            UserInfo.Instance.Coin += (ulong)goodsData.totalCoin;
            PopupPurchase.Open(PopupPurchase.Type.Success, goodsData.totalCoin, goodsData.effectCode);
#else
            UserTracking.Instance.LogPurchaseTouch(goodsData.goodsId, UserTracking.PurchaseWhich.reference, how, location);
            
            SessionLobby.Instance.RequestPurchaseValid(goodsData.goodsId, (exception, valid) => 
            {
                if (exception != null)
                {
                    if (StoreInfo.Instance.ActionBuyItem != null)
                        StoreInfo.Instance.ActionBuyItem(true);

                    PopupPurchase.Open(PopupPurchase.Type.Failed, 0, string.Empty);
                }
                else
                {
                    PlayerPrefs.SetInt(PlayerPrefs.STORE_PURCHASE_ITEM_COIN + UserInfo.Instance.GSN, (int)goodsData.totalCoin);
                    PlayerPrefs.SetString(PlayerPrefs.STORE_PURCHASE_ITEM_EFFECT + UserInfo.Instance.GSN, goodsData.effectCode);

                    Session.Instance.PurchaseItem(goodsData.inappId, (e, response) => 
                    {
                        if (e != null)
                        {
                            PopupPurchase.Open(PopupPurchase.Type.Failed, 0, string.Empty);
                        }
                        else
                        {
                            UserTracking.Instance.LogPurchaseSuccess(goodsData.goodsId, UserTracking.PurchaseWhich.reference, how, location);
                            
                            PopupPurchase.Open(PopupPurchase.Type.Success, goodsData.totalCoin, goodsData.effectCode).OnClose(() => 
                            {
                                SessionLobby.Instance.RequestStoreSales(() =>
                                {
                                    //SessionLobby.Instance.RequestUser(() =>
                                    UserInfo.Instance.UpdateUserCoin(() =>
                                    {
                                    });
                                });
                            });
                        }

                        //if (buyButton != null)
                        //    buyButton.interactable = true;
                    },
                    (status) => 
                    {
                    },
                    goodsData,
                    (cancel) => 
                    {
                        //if (buyButton != null)
                        //    buyButton.interactable = true;
                    });
                }
            });
#endif
        }
    }
}