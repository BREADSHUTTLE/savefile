// PopupShop.cs - PopupShop implementation file
//
// Description      : PopupShop
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2018/03/08
// Last Update      : 2018/09/13
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using UnityEngine;
using UnityEngine.UI;
using ST.MARIA.DataPool;
using ST.MARIA.UI;
using Firebase.Analytics;

namespace ST.MARIA.Popup
{
    public sealed class PopupShop : PopupBaseLayout<PopupShop>
    {
        [SerializeField] private GameObject tabObject;
        [SerializeField] private Image coinTabActive;
        [SerializeField] private Image coinTabDisable;
        [SerializeField] private Image gemTabActive;
        [SerializeField] private Image gemTabDisable;
        [SerializeField] private Image decoTabActive;
        [SerializeField] private Image decoTabDisable;
        [SerializeField] private Image gemShopComingSoon;

        [SerializeField] private Text extraText;
        [SerializeField] private Text[] coinTabText;
        [SerializeField] private Text[] gemTabText;
        [SerializeField] private Text[] decoTabText;
        [SerializeField] private GameObject[] listObject;

        [SerializeField] private ShopListView[] shopListView;

        private ShopInfo.Type shopCategory = ShopInfo.Type.None;
        
        public static PopupShop Create(ShopInfo.Type shopType)
        {
            PopupShop popup = OnCreate("POPUP-Shop");
            popup.Initialize(shopType);
            return popup;
        }

        public void Initialize(ShopInfo.Type shopType)
        {
            UserTracking.LogEvent(this.name, new Parameter[]
            {
                new Parameter("location", Session.Instance.Location),
                new Parameter("time", CommonTools.TimeToLong(ClientInfo.Instance.ServerTime)),
                new Parameter("coin", MyInfo.Instance.Coin),
                new Parameter("gem", MyInfo.Instance.Gem),
                new Parameter("what", "open"),
            });

            shopCategory = shopType;

            if (shopCategory == ShopInfo.Type.None)
                return;

            foreach (var list in listObject)
                CommonTools.SetActive(list, false);

            if (tabObject != null)
                CommonTools.SetActive(tabObject, false);

            Build();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (Application.isPlaying == true)
            {
                DataPool.ShopInfo.Instance.ActionGet += SetUI;
                DataPool.ShopInfo.Instance.ActionUpdate += SetUI;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Application.isPlaying == true)
            {
                DataPool.ShopInfo.Instance.ActionGet -= SetUI;
                DataPool.ShopInfo.Instance.ActionUpdate -= SetUI;
            }
        }

        private void Build()
        {
            ShowLoading(true);

            // CHECK: 런칭 이후 수정 해야 함. 젬 상점 닫아둡니다.
            SetGemShopComingSoon();
            if (shopCategory == ShopInfo.Type.Gem)
            {
                BuildGemShop();
                return;
            }

            DataPool.ShopInfo.Instance.GetShopData(shopCategory == ShopInfo.Type.Coin ? "C" :
                                                   shopCategory == ShopInfo.Type.Gem ? "G" :
                                                   shopCategory == ShopInfo.Type.Structure ? "S" : "");
        }

        private void BuildGemShop()
        {
            ShowLoading(false);
            SetText();
            SetTab();
        }

        private void SetGemShopComingSoon()
        {
            if (gemShopComingSoon != null)
                CommonTools.SetActive(gemShopComingSoon, shopCategory == ShopInfo.Type.Gem);

            if (shopListView[0] != null)
                CommonTools.SetActive(shopListView[0], shopCategory == ShopInfo.Type.Coin);

            if (extraText != null)
                CommonTools.SetActive(extraText.transform.parent, shopCategory == ShopInfo.Type.Coin);
        }

        private void SetUI()
        {
            ShowLoading(false);
            SetGemShopComingSoon();
            SetText();
            SetTab();
            SetShop();
        }

        private void SetText()
        {
            foreach(var coin in coinTabText)
                coin.text = LocalizationSystem.Instance.Localize("POPUP.SHOP.Category.Con");

            foreach(var gem in gemTabText)
                gem.text = LocalizationSystem.Instance.Localize("POPUP.SHOP.Category.Gem");

            foreach(var deco in decoTabText)
                deco.text = LocalizationSystem.Instance.Localize("POPUP.SHOP.Category.Structure");

            if (extraText != null)
                extraText.text = LocalizationSystem.Instance.Localize("POPUP.SHOP.List.Extra");
        }

        private void SetTab()
        {
            if (tabObject != null)
                CommonTools.SetActive(tabObject, true);

            if (coinTabActive != null)
            {
                CommonTools.SetActive(coinTabActive, shopCategory == ShopInfo.Type.Coin);
                CommonTools.SetActive(coinTabDisable, shopCategory != ShopInfo.Type.Coin);
            }

            if (gemTabActive != null)
            {
                CommonTools.SetActive(gemTabActive, shopCategory == ShopInfo.Type.Gem);
                CommonTools.SetActive(gemTabDisable, shopCategory != ShopInfo.Type.Gem);
            }

            if (decoTabActive != null)
            {
                CommonTools.SetActive(decoTabActive, shopCategory == ShopInfo.Type.Structure);
                CommonTools.SetActive(decoTabDisable, shopCategory != ShopInfo.Type.Structure);
            }

            CommonTools.SetActive(listObject[0], shopCategory ==  ShopInfo.Type.Coin ? true :
                                                shopCategory ==  ShopInfo.Type.Gem ? true : false );

            CommonTools.SetActive(listObject[1], shopCategory ==  ShopInfo.Type.Structure ? true : false);
        }

        private void SetShop()
        {
            if (shopListView == null || shopListView.Length == 0)
                return;

            string shopKey = "";
            var shopData = ShopInfo.Instance.ShopDatas;

            if ( shopCategory == ShopInfo.Type.Coin)
            {
                shopKey = "C";
                if (shopData.ContainsKey(shopKey))
                {
                    ShopInfo.Instance.Sort(shopKey, shopData[shopKey], ShopInfo.SortType.Price, ShopInfo.Type.Coin);
                    shopListView[0].Build(shopData[shopKey].GoodsInfo, ShopInfo.Type.Coin, true);
                }
            }
            else if (shopCategory == ShopInfo.Type.Gem)
            {
                shopKey = "G";
                if (shopData.ContainsKey(shopKey))
                {
                    ShopInfo.Instance.Sort(shopKey, shopData[shopKey], ShopInfo.SortType.Price, ShopInfo.Type.Gem);
                    shopListView[0].Build(shopData[shopKey].GoodsInfo, ShopInfo.Type.Gem, true);
                }
            }
            else if (shopCategory == ShopInfo.Type.Structure)
            {
                shopKey = "S";
                if (shopData.ContainsKey(shopKey))
                {
                    ShopInfo.Instance.Sort(shopKey, shopData[shopKey], ShopInfo.SortType.Name, ShopInfo.Type.Structure, false);
                    shopListView[1].Build(shopData[shopKey].GoodsInfo, shopCategory, true);
                }
            }

            ShowLoading(false);
        }

        public void OnClickMoneyShopTab()
        {
            UserTracking.LogEvent(this.name, new Parameter[]
            {
                new Parameter("location", Session.Instance.Location),
                new Parameter("time", CommonTools.TimeToLong(ClientInfo.Instance.ServerTime)),
                new Parameter("coin", MyInfo.Instance.Coin),
                new Parameter("gem", MyInfo.Instance.Gem),
                new Parameter("what", "category"),
                new Parameter("how", "click"),
                new Parameter("which", ShopInfo.Type.Coin.ToString()),
            });

            if (shopCategory == ShopInfo.Type.Coin)
                return;

            shopCategory = ShopInfo.Type.Coin;
            ShowLoading(true);

            if (DataPool.ShopInfo.Instance.GetShopData("C") == true)
                SetUI();
        }

        public void OnClickGemShopTab()
        {
            UserTracking.LogEvent(this.name, new Parameter[]
            {
                new Parameter("location", Session.Instance.Location),
                new Parameter("time", CommonTools.TimeToLong(ClientInfo.Instance.ServerTime)),
                new Parameter("coin", MyInfo.Instance.Coin),
                new Parameter("gem", MyInfo.Instance.Gem),
                new Parameter("what", "category"),
                new Parameter("how", "click"),
                new Parameter("which", ShopInfo.Type.Gem.ToString()),
            });

            if (shopCategory == ShopInfo.Type.Gem)
                return;

            shopCategory = ShopInfo.Type.Gem;
            ShowLoading(true);

            // CHECK: 런칭 이후 수정 해야 함. 젬 상점 닫아둡니다.
            SetGemShopComingSoon();
            if (shopCategory == ShopInfo.Type.Gem)
            {
                BuildGemShop();
                return;
            }

            if (DataPool.ShopInfo.Instance.GetShopData("G") == true)
                SetUI();
        }

        public void OnClickDecoShopTab()
        {
            UserTracking.LogEvent(this.name, new Parameter[]
            {
                new Parameter("location", Session.Instance.Location),
                new Parameter("time", CommonTools.TimeToLong(ClientInfo.Instance.ServerTime)),
                new Parameter("coin", MyInfo.Instance.Coin),
                new Parameter("gem", MyInfo.Instance.Gem),
                new Parameter("what", "category"),
                new Parameter("how", "click"),
                new Parameter("which", ShopInfo.Type.Structure.ToString()),
            });

            if (shopCategory == ShopInfo.Type.Structure)
                return;

            shopCategory = ShopInfo.Type.Structure;
            ShowLoading(true);

            if (DataPool.ShopInfo.Instance.GetShopData("S") == true)
                SetUI();
        }

        public void OnClickClose()
        {
            base.Close();
        }
    }
}