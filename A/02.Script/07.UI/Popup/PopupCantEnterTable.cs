using CAPYBARA.Bundles;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class PopupCantEnterTable : BasePopup
    {
        [SerializeField] private CPButton confirmButton;
        [SerializeField] private TMP_Text desc;

        protected override void OnInit()
        {
            base.OnInit();
            confirmButton.onClick.AddListener(Close);
        }

        public void SetDesc(long minmaxGoldValue,bool isgoldLow)
        {
            if (isgoldLow)
            {
                string gold=Extension.ToKoreanFormat(minmaxGoldValue);
                desc.text = $"{gold} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.GoldMinEntry].StringToLocal}";
            }
            else
            {
                string gold=Extension.ToKoreanFormat(minmaxGoldValue);
                desc.text = $"{gold} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.GoldMaxEntry].StringToLocal}";
            }
        }
    }

}
