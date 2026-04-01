using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Definition;
using Cysharp.Threading.Tasks;
using System.Linq;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class ShopItemSlot : MonoBehaviour
    {
        [HideInInspector]public IAPProduct myproductInfo;

        public CPButton buyProduct;

        public GameObject discountObjBack;
        public GameObject discountObjShadow;
        public GameObject discountObj;
        public TMP_Text discount;

        public Image productImage;
        public TMP_Text productPrice;
        public TMP_Text productGoodsQuantity;

        public Image avatarImage;

        private void Awake()
        {
            buyProduct.onClick.AddListener(OnClickevent);
        }

        public void InitWhenInstantiate()
        {
            buyProduct.onClick.AddListener(OnClickevent);
        }
        public void SetSlotInfo(int index, IAPProduct product, int totalCount)
        {
            this.gameObject.SetActive(true);
            myproductInfo = product;

            if(myproductInfo.discount<=0)
            {
                discountObjBack.SetActive(false);
                discountObjShadow.SetActive(false);
                discountObj.SetActive(false);
            }
            else
            {
                discountObjBack.SetActive(true);
                discountObjShadow.SetActive(true);
                discountObj.SetActive(true);
                discount.text=string.Format($"{myproductInfo.discount}%");
            }

            var bundleItems = ConfigDataManager.inAppItems.Where(i => i.ProductId == myproductInfo.productId).ToList();
            var avatarItem = bundleItems.FirstOrDefault(i => System.Enum.TryParse<ItemID>(i.ItemId, out var itemId) && itemId.ToString().StartsWith("AVATAR"));
            if (avatarItem != null)
                avatarImage.sprite = ItemBundle.Loaded.GetItemSprite(avatarItem.ItemId);
            avatarImage.SetNativeSize();
            avatarImage.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
            
            int coinIndex = totalCount - index;
            productImage.sprite = ItemBundle.Loaded.GetCoinSprite($"COIN_{coinIndex}", true);

            var currencyItem = bundleItems.FirstOrDefault(i => System.Enum.TryParse<ItemID>(i.ItemId, out var itemId) && itemId.ToString().Contains("DEFAULT_CURRENCY"));
            long goldAmount = currencyItem?.Amount ?? 0;
            
            long configPrice = bundleItems.FirstOrDefault()?.Price ?? 0;

            productPrice.text = Extension.ToKoreanFormat(configPrice) + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Won].StringToLocal;
            productGoodsQuantity.gameObject.SetActive(true);
            productGoodsQuantity.text = Extension.ToKoreanFormat(goldAmount);
        }

        void OnClickevent()
        {
            int requiredClassLevel = GetRequiredClassLevel(myproductInfo.productId);
            int myClassLevel = CPPlayer.Inventory.classNumber;
            
            if (myClassLevel < requiredClassLevel)
            {
                string requiredClassName = GetClassNameByLevel(requiredClassLevel);
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.CannotPurchase].StringToLocal, $"{requiredClassName} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Class].StringToLocal} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OrMore].StringToLocal}만 구매할 수 있습니다.", null));
                return;
            }

            bool hasMultiQuantity = HasMultipleQuantityProducts(myproductInfo.productId);
            if (hasMultiQuantity)
            {
                CPPlayer.Inventory.shopnormalToastPopup?.Invoke(myproductInfo, productImage.sprite, (quantity) =>
                {
                    CAPYBARA.CommonIAPManager.Instance.BuyProductById(myproductInfo.productId, quantity);
                });
            }
            else
            {
                CAPYBARA.CommonIAPManager.Instance.BuyProductById(myproductInfo.productId);
            }
        }

        private bool HasMultipleQuantityProducts(string productId)
        {
            string prefix = productId + "_X";
            foreach (var ci in ConfigDataManager.inAppItems)
            {
                if (ci.ProductId.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(ci.ProductId.Substring(prefix.Length), out _))
                    return true;
            }
            return false;
        }
        
        private int GetRequiredClassLevel(string productId)
        {
            var s = IAPManager.StripProductPrefix(productId);
            if (s.StartsWith("gold_x_")) return 0;
            if (s.StartsWith("gold_s_")) return 3;
            if (s.StartsWith("gold_a_")) return 2;
            if (s.StartsWith("gold_b_")) return 1;
            return 0;
        }
        
        private string GetClassNameByLevel(int level)
        {
            return level switch
            {
                1 => "B",
                2 => "A",
                3 => "S",
                _ => ""
            };
        }

        public void SetOff()
        {
            this.gameObject.SetActive(false);
        }
    }

}
