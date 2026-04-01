using CAPYBARA.Bundles;
using CAPYBARA.Definition;
using BlackTree.Bundles;
using Cysharp.Threading.Tasks;
using System.Linq;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class ShopClassItemSlot : MonoBehaviour
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
            this.gameObject.SetActive(true);
            myproductInfo = product;

            var configItems = ConfigDataManager.GetInAppItemsByProductId(myproductInfo.productId);
            var classConfigItem = configItems?.FirstOrDefault(item => item.ItemId == nameof(ItemID.CLASS_S) || item.ItemId == nameof(ItemID.CLASS_A) || item.ItemId == nameof(ItemID.CLASS_B));
            var configItem = classConfigItem ?? configItems?.FirstOrDefault();
            
            if (configItem != null)
                productImage.sprite = ItemBundle.Loaded.GetItemSprite(configItem.ItemId);
            productName.text = myproductInfo.title_Kr;
            
            long configPrice = configItem?.Price ?? 0;
            productPrice.text = Extension.ToKoreanFormat(configPrice) + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Won].StringToLocal;
     
        }

        private void OnClickevent()
        {
            var configItems = ConfigDataManager.GetInAppItemsByProductId(myproductInfo.productId);
            var configItem = configItems?.FirstOrDefault(item => item.ItemId == nameof(ItemID.CLASS_S) || item.ItemId == nameof(ItemID.CLASS_A)
                                                            || item.ItemId == nameof(ItemID.CLASS_B)) ?? configItems?.FirstOrDefault();
            (IAPType iapType, bool isSubscribe) = GetIAPTypeFromConfig(configItem);

            bool hasMultiQuantity = !isSubscribe && HasMultipleQuantityProducts(myproductInfo.productId);

            CPPlayer.Inventory.shopClassToastPopup?.Invoke(iapType, isSubscribe, () =>
                {
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
                });
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
        
        private (IAPType, bool) GetIAPTypeFromConfig(lobby.ConfigInAppItems configItem)
        {
            if (configItem == null)
                return (IAPType.Class_b, true);
            
            bool isSubscribe = configItem.InAppItemId.Contains("SUBSCRIBE");
            
            switch (configItem.ItemId)
            {
                case nameof(ItemID.CLASS_S):
                    return isSubscribe ? (IAPType.Class_sub_s, isSubscribe) : (IAPType.Class_s, isSubscribe);
                case nameof(ItemID.CLASS_A):
                    return isSubscribe ? (IAPType.Class_sub_a, isSubscribe) : (IAPType.Class_a, isSubscribe);
                case nameof(ItemID.CLASS_B):
                    return isSubscribe ? (IAPType.Class_sub_b, isSubscribe) : (IAPType.Class_b, isSubscribe);
                default:
                    return (IAPType.Class_b, true);
            }
        }
        
        private async UniTask BuyProductAsync()
        {
            var res=await Services.Lobby.AddItemToInventoryAsync(ItemID.MESSAGE.ToString(), 1);
            
            
            Debug.Log($"{myproductInfo.subTapType} buy success");
        }
        

        public void SetOff()
        {
            this.gameObject.SetActive(false);
        }
    }
}
