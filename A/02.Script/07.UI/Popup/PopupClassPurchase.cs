using System;
using System.Linq;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PopupClassPurchase : BasePopup
    {
        public enum Type
        {
            Subscribe,
            NotSubscribe
        }

        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private Image imgIcon;
        [SerializeField] private GameObject[] classTypeObjects;
        [SerializeField] private CPButton purchaseButtons;
        [SerializeField] private TMP_Text txtPriceButton;
        [SerializeField] private CPButton termsAndConditions;
        [SerializeField] private CPButton personalInformation;

        private IAPType currentType;
        private Action purchaseCallback;
        private bool subscribe;

        protected override void OnInit()
        {
            if (purchaseButtons != null)
            {
                purchaseButtons.onClick.RemoveAllListeners();
                purchaseButtons.onClick.AddListener(OnClickPurchase);
            }

            termsAndConditions.onClick.AddListener(TermsOfServiceOpen);
            personalInformation.onClick.AddListener(PrivatePolicyOpen);
        }

        public void SetData(IAPType iapType, bool isSubscribe, Action onPurchase)
        {
            currentType = iapType;
            purchaseCallback = onPurchase;
            subscribe = isSubscribe;
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            Show();
        }

        private void Show()
        {
            Array.ForEach(classTypeObjects, obj => obj?.SetActive(false));

            int index = GetIndexByType(currentType);
            if (index >= 0 && index < classTypeObjects.Length && classTypeObjects[index] != null)
                classTypeObjects[index].SetActive(true);

            SetTitle();
            SetIcon();
            SetPrice();
        }

        private void SetTitle()
        {
            int index = GetIndexByType(currentType);
            if (index < 0)
                return;

            string className = index switch { 0 => "B", 1 => "A", 2 => "S", _ => "" };
            string title = subscribe ? $"{className}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Class].StringToLocal}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Subscription].StringToLocal} 구매" : $"{className}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Class].StringToLocal} 구매";
            txtTitle.text = title;
        }

        private void SetIcon()
        {
            if (imgIcon == null || ItemBundle.Loaded == null)
                return;

            int index = GetIndexByType(currentType);
            string classId = index switch
            {
                0 => nameof(ItemID.CLASS_B),
                1 => nameof(ItemID.CLASS_A),
                2 => nameof(ItemID.CLASS_S),
                _ => null
            };

            if (string.IsNullOrEmpty(classId))
                return;

            var sprite = ItemBundle.Loaded.GetItemSprite(classId);
            if (sprite != null)
                imgIcon.sprite = sprite;
        }

        private void SetPrice()
        {
            if (txtPriceButton == null)
                return;

            int index = GetIndexByType(currentType);
            string classItemId = index switch
            {
                0 => nameof(ItemID.CLASS_B),
                1 => nameof(ItemID.CLASS_A),
                2 => nameof(ItemID.CLASS_S),
                _ => null
            };
            if (string.IsNullOrEmpty(classItemId))
                return;

            var configItem = ConfigDataManager.inAppItems.FirstOrDefault(i =>
                i.ItemId == classItemId &&
                (subscribe ? i.InAppItemId.Contains("SUBSCRIBE") : !i.InAppItemId.Contains("SUBSCRIBE")));

            long price = configItem?.Price ?? 0;
            string priceText = price.ToString("N0") + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Won].StringToLocal;
            if (subscribe)
                priceText += StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PerMonth].StringToLocal;
            txtPriceButton.text = priceText;
        }

        private int GetIndexByType(IAPType iapType)
        {
            return iapType switch
            {
                IAPType.Class_sub_b or IAPType.Class_b => 0,
                IAPType.Class_sub_a or IAPType.Class_a => 1,
                IAPType.Class_sub_s or IAPType.Class_s => 2,
                _ => -1
            };
        }

        private void OnClickPurchase()
        {
            purchaseCallback?.Invoke();
            Close();
        }

        protected override void OnClose()
        {
            base.OnClose();
            purchaseCallback = null;
        }
        
        private void TermsOfServiceOpen()
        {
            Application.OpenURL(Constraints.TermsOfServiceUrl);
        }
        
        private void PrivatePolicyOpen()
        {
            Application.OpenURL(Constraints.privacypolicyUrl);
        }
    }
}
