using CAPYBARA.Bundles;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class PopupPaymentLimitExceeded : BasePopup
    {
        [SerializeField] private TMP_Text txtRemainingAmount;
        [SerializeField] private CPButton confirmButton;

        protected override void OnInit()
        {
            base.OnInit();
            if (confirmButton != null)
                confirmButton.onClick.AddListener(Close);
        }

        public void SetRemainingAmount(long remainingAmount)
        {
            if (txtRemainingAmount != null)
                txtRemainingAmount.text = Extension.ToKoreanFormat(remainingAmount) + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Won].StringToLocal;
        }
    }
}