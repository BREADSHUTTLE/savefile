// PopupInventory.cs : PopupInventory implementation file
//
// Description      : PopupInventory
// Author           : icoder
// Maintainer       : uhrain7761, icoder
// How to use       : 
// Created          : 2016/09/19
// Last Update      : 2017/11/1
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using UnityEngine;
using System;
using System.Collections.Generic;
using DUNK.DataPool;
using DUNK.Tools;


namespace DUNK.Popup
{
    [Serializable]
    public class CharacterFX
    {
        public Animation target = null;
        public AnimationClip giftShow = null;
        public AnimationClip letterShow = null;
    }

    public sealed class PopupInventory : PopupBaseLayout<PopupInventory>
    {
        [SerializeField] private CharacterFX characterFX = new CharacterFX();
        [SerializeField] private UILabel balance = null;
        [SerializeField] private GameObject[] pushbackGift = null;
        [SerializeField] private GameObject[] pushbackLetter = null;
        [SerializeField] private GameObject[] resetObjects = null;
        [SerializeField] private GameObject letterBox = null;
        [SerializeField] private GameObject giftBox = null;
        [SerializeField] private int itemCountPerPage = 30;

        private bool claimAll = false;

        public static PopupInventory Create()
        {
#if !DUNK_PCKLP
            if (SessionConnection.Instance.CheckOnlineMode() == false)
                return null;
#endif
            PopupInventory popup = OnCreate("POPUP.Inventory");
            popup.Initialize();
            return popup;
        }

        protected override void Awake()
        {
            base.Awake();
            SetupUI();
        }

        public void Initialize()
        {
            SetupData();
        }

        private void SetupUI()
        {
            CommonTools.SetActive(resetObjects, false);
        }

        private void SetupData()
        {
            ShowLoading(true);

            GiftInfo.Instance.Validate();

            if (CommonTools.GetActive(letterBox))
                OnClickLetterBox();
            else
                OnClickGiftBox();

            SessionService.Instance.Request("/inventory", "{\"index\":0, \"count\":" + itemCountPerPage + "}");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SessionService.Instance.ActionInventoryInfo += OnGetInventoryInfo;
            SessionService.Instance.ActionClaimGift     += OnClaimGift;
            SessionService.Instance.ActionGiftInfo      += OnGetGiftInfo;
            SessionService.Instance.ActionLetterInfo    += OnGetLetterInfo;
            MyInfo.Instance.ActionUpdateAssetInfo       += OnUpdateAssetInfo;
            LetterInfo.Instance.ActionUnreadCount       += OnLetterUnreadCount;
            LetterInfo.Instance.ActionUnreadNoticeCount += OnLetterUnreadCount;
            GiftInfo.Instance.ActionTotalCount          += OnGiftTotalCount;

            OnUpdateAssetInfo();
            OnGiftTotalCount();
            OnLetterUnreadCount();

            GraphicSystem.Instance.SetupAntiAliasing(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SessionService.Instance.ActionInventoryInfo -= OnGetInventoryInfo;
            SessionService.Instance.ActionClaimGift     -= OnClaimGift;
            SessionService.Instance.ActionGiftInfo      -= OnGetGiftInfo;
            SessionService.Instance.ActionLetterInfo    -= OnGetLetterInfo;
            MyInfo.Instance.ActionUpdateAssetInfo       -= OnUpdateAssetInfo;
            LetterInfo.Instance.ActionUnreadCount       -= OnLetterUnreadCount;
            LetterInfo.Instance.ActionUnreadNoticeCount -= OnLetterUnreadCount;
            GiftInfo.Instance.ActionTotalCount          -= OnGiftTotalCount;

            GraphicSystem.Instance.SetupAntiAliasing(false);
        }

        private void OnUpdateAssetInfo()
        {
            if (balance != null)
            {
                balance.text = CommonTools.MoneyFormat(
                    MyInfo.Instance.Asset.Balance, MoneyCurrency.None, MoneyType.FullMoney, true, false, false);
            }
        }

        private void OnGiftTotalCount()
        {
            int totalCount = Mathf.Max(GiftInfo.Instance.TotalCount, 0);

            foreach (GameObject go in pushbackGift)
            {
                if (totalCount > 0)
                {
                    CommonTools.SetActive(go, true);
                    go.GetComponentInChildren<UILabel>().text = totalCount.ToString();
                }
                else
                {
                    CommonTools.SetActive(go, false);
                }
            }
        }

        private void OnLetterUnreadCount()
        {
            int unreadCount = Mathf.Max(LetterInfo.Instance.UnreadCount + LetterInfo.Instance.UnreadNoticeCount, 0);

            foreach (GameObject go in pushbackLetter)
            {
                if (unreadCount > 0)
                {
                    CommonTools.SetActive(go, true);
                    go.GetComponentInChildren<UILabel>().text = unreadCount.ToString();
                }
                else
                {
                    CommonTools.SetActive(go, false);
                }
            }
        }

        private void OnGetInventoryInfo(object msg)
        {
            ShowLoading(false);
        }

        private void OnClaimGift(object msg)
        {
            ShowLoading(false);

            if (claimAll)
            {
                if (GiftInfo.Instance.LastClaimState == 1)
                {
                    if (PopupExceedBalance.Create(true) == null)
                        PopupExceedRewards.Create();
                }
                else
                {
                    long reward = 0;
                    Dictionary<string, object> body = msg as Dictionary<string, object>;
                    Dictionary<string, object> giftInfo = body["claim_gift_info"] as Dictionary<string, object>;
                    List<object> gifts = giftInfo["gifts"] as List<object>;

                    foreach (Dictionary<string, object> gift in gifts)
                        reward += (Int64)gift["reward"];
#if DUNK_PCKLP
                    string message = (reward > 0) ?
                        string.Format(LocalizationSystem.Instance.Localize("POPUP.MESSAGE.ClaimGift.PC"), reward) :
                        "POPUP.MESSAGE.ClaimGiftEmpty";
#else
                    string message = (reward > 0) ?
                        string.Format(LocalizationSystem.Instance.Localize("POPUP.MESSAGE.ClaimGift"), reward) :
                        "POPUP.MESSAGE.ClaimGiftEmpty";
#endif
                    PopupMessage.Create("POPUP.MESSAGE.Notice.Title", message, 10f, PopupMessage.Type.Ok);
                }
            }
            else
            {
                if (GiftInfo.Instance.LastClaimState == 1)
                {
                    if (PopupExceedBalance.Create(true) == null)
                        PopupExceedRewards.Create();

                    giftBox.GetComponent<PopupInventoryGiftListView>().ResetClaimButtons();
                }
            }
        }

        private void OnGetGiftInfo(object msg)
        {
            ShowLoading(false);
        }

        private void OnGetLetterInfo(object msg)
        {
            ShowLoading(false);
        }

        public void ClaimGift(string serial)
        {
            ShowLoading(true);
            SessionService.Instance.Request("/claim_gift", "{\"serial\": \"" + serial + "\"}");
            claimAll = false;
        }

        public void ClaimGiftAll()
        {
            ShowLoading(true);
            SessionService.Instance.Request("/claim_gift");
            claimAll = true;
        }

        public void ViewLetter(LetterData data)
        {
            PopupLetterView.Create(data);
        }

        public void RequestGift(int index)
        {
            ShowLoading(true);
            SessionService.Instance.Request("/gift", "{\"index\":" + index + ", \"count\":" + itemCountPerPage + "}");
        }

        public void RequestLetter(int index)
        {
            ShowLoading(true);
            SessionService.Instance.Request("/letter", "{\"index\":" + index + ", \"count\":" + itemCountPerPage + "}");
        }

        private void OnClickLetterBox()
        {
            if (characterFX != null)
                CommonTools.PlayAnimation(characterFX.target, characterFX.letterShow.name);
        }

        private void OnClickGiftBox()
        {
            if (characterFX != null)
                CommonTools.PlayAnimation(characterFX.target, characterFX.giftShow.name);
        }
    }
}
