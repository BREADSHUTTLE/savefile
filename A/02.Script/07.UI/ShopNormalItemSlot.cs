using System;
using CAPYBARA.Bundles;
using CAPYBARA.Definition;
using BlackTree.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class ShopNormalItemSlot : MonoBehaviour
    {
        [HideInInspector]public IAPProduct myproductInfo;

        public CPButton buyProduct;
        
        public Image productImage;
        public TMP_Text productName;
        public TMP_Text productPrice;
        private void Awake()
        {
            buyProduct.onClick.AddListener(OnClickevent);
        }
        
        public void InitWhenInstantiate()
        {
            buyProduct.onClick.AddListener(OnClickevent);
        }
        public void SetSlotInfo(IAPProduct product)
        {
            if (product.subTapType == ShopSubTapType.BOOSTER)
            {
                // 가입 후 7일 지났으면 안 보임
                long regDt = LoginData.Cloud.loginValue.loginres?.RegDt ?? 0;
                if (regDt == 0)
                {
                    this.gameObject.SetActive(false);
                    myproductInfo = product;
                    return;
                }
                
                DateTime signupDate = DateTimeOffset.FromUnixTimeSeconds(regDt).LocalDateTime;
                if (IsWeekPassed(signupDate))
                {
                    this.gameObject.SetActive(false);
                    myproductInfo = product;
                    return;
                }
                
                // 인벤토리에 BOOSTER 아이템이 있으면 안 보임
                bool hasBooster = false;
                if (CPPlayer.Inventory.inventoryInfo != null)
                {
                    for (int i = 0; i < CPPlayer.Inventory.inventoryInfo.Inventory.Count; i++)
                    {
                        if (CPPlayer.Inventory.inventoryInfo.Inventory[i].ItemId.Equals("BOOSTER", StringComparison.OrdinalIgnoreCase))
                        {
                            hasBooster = true;
                            break;
                        }
                    }
                }
                
                if (hasBooster)
                {
                    this.gameObject.SetActive(false);
                    myproductInfo = product;
                    return;
                }
            }
            
            if (product.subTapType == ShopSubTapType.LUCKY_POCKET)
            {
                bool canShow = CPPlayer.Inventory.myPoints != null && CPPlayer.Inventory.myPoints.LuckyBox >= 100000 && CPPlayer.Inventory.myPoints.WeeklyLuckyboxCnt < 3;
                if (!canShow)
                {
                    this.gameObject.SetActive(false);
                    myproductInfo = product;
                    return;
                }
            }
            
            this.gameObject.SetActive(true);
            myproductInfo = product;

            var configItem = ConfigDataManager.GetInAppItemByProductId(myproductInfo.productId);
            if (configItem != null)
            {
                var sprite = ItemBundle.Loaded.GetItemSprite(configItem.ItemId, false, IAPManager.StripProductPrefix(configItem.ProductId));
                productImage.sprite = sprite;
                productImage.gameObject.SetActive(sprite != null);
                productPrice.text = Extension.ToKoreanFormat((long)configItem.Price) + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Won].StringToLocal;
            }
            else
            {
                productImage.gameObject.SetActive(false);
            }

            productName.text = myproductInfo.title_Kr;
       
        }

        void OnClickevent()
        {
            CPPlayer.Inventory.shopnormalToastPopup?.Invoke(myproductInfo, productImage.sprite, (quantity) =>
            {
                CAPYBARA.CommonIAPManager.Instance.BuyProductById(myproductInfo.productId, quantity);
            });
        }
        
        public void SetOff()
        {
            this.gameObject.SetActive(false);
        }
        
        public bool IsWeekPassed(DateTime targetDate)
        {
            TimeSpan diff = DateTime.Now - targetDate;
            return diff.TotalDays >= 7;
        }
    }

}
