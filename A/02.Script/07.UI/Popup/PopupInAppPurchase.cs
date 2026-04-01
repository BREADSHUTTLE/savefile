using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PopupInAppPurchase : BasePopup
    {
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text itemPrice;

        [SerializeField] private GameObject quantityObject;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private CPButton quantityMinus;
        [SerializeField] private CPButton quantityPlus;

        [SerializeField] private TMP_Text itemDesc;
        [SerializeField] private TMP_Text buyBtnDesc;
        [SerializeField] private CPButton buyBtn;

        [SerializeField] private Image productImage;

        private int itemQuantity = 1;
        private int _maxQuantity = 1;
        private IAPProduct _currentProduct;
        private long _basePrice;

        protected override void OnInit()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (quantityMinus != null)
            {
                quantityMinus.onClick.RemoveAllListeners();
                quantityMinus.onClick.AddListener(ClickMinus);
            }

            if (quantityPlus != null)
            {
                quantityPlus.onClick.RemoveAllListeners();
                quantityPlus.onClick.AddListener(ClickPlus);
            }

            itemQuantity = 1;
            if (quantityText != null)
                quantityText.text = itemQuantity.ToString();
        }

        public void SetItemAndShowWindow(IAPProduct iapProduct, Sprite sprite, Action<int> callback)
        {
            _currentProduct = iapProduct;
            itemName.text = iapProduct.title_Kr;
            productImage.sprite = sprite;

            var configItem = ConfigDataManager.GetInAppItemByProductId(iapProduct.productId);
            _basePrice = configItem?.Price ?? 0;
            var priceString = Extension.ToKoreanFormat(_basePrice) + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Won].StringToLocal;            
            itemPrice.text = priceString;

            itemQuantity = 1;
            quantityText.text = itemQuantity.ToString();

            _maxQuantity = 1;
            string prefix = iapProduct.productId + "_X";
            foreach (var ci in ConfigDataManager.inAppItems)
            {
                if (ci.ProductId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(ci.ProductId.Substring(prefix.Length), out int qty))
                {
                    if (qty > _maxQuantity)
                        _maxQuantity = qty;
                }
            }
            quantityObject.SetActive(true);
            itemDesc.text = iapProduct.desc_Kr;
            buyBtnDesc.text = priceString;

            buyBtn.onClick.RemoveAllListeners();
            buyBtn.onClick.AddListener(() =>
            {
                callback?.Invoke(itemQuantity);
                Close();
            });

            Open();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnClose()
        {
            base.OnClose();
        }

        private void ClickMinus()
        {
            itemQuantity = Mathf.Clamp(itemQuantity - 1, 1, _maxQuantity);
            quantityText.text = itemQuantity.ToString();
            UpdatePriceDisplay();
        }

        private void ClickPlus()
        {
            itemQuantity = Mathf.Clamp(itemQuantity + 1, 1, _maxQuantity);
            quantityText.text = itemQuantity.ToString();
            UpdatePriceDisplay();
        }

        private void UpdatePriceDisplay()
        {
            if (_currentProduct == null) return;

            string actualProductId = IAPManager.GetActualProductId(_currentProduct.productId, itemQuantity);
            var configItem = ConfigDataManager.GetInAppItemByProductId(actualProductId);
            
            long price;
            if (configItem != null)
                price = (long)configItem.Price;
            else
                price = _basePrice * itemQuantity;

            var priceString = Extension.ToKoreanFormat(price) + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Won].StringToLocal;
            itemPrice.text = priceString;
            buyBtnDesc.text = priceString;
        }
    }
}
