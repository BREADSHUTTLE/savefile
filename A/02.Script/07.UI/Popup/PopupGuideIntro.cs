using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PopupGuideIntro : BasePopup
    {
        [SerializeField] private CPButton sevenpokerOpenBtn;
        [SerializeField] private CPButton badugiOpenBtn;
        [SerializeField] private CPButton holdemOpenBtn;
        [SerializeField] private CPButton jackpotOpenBtn;
        [SerializeField] private CPButton dealerFeeOpenBtn;
        [SerializeField] private CPButton classInfoOpenBtn;
        [SerializeField] private CPButton guildOpenBtn;

        
        protected override void OnInit()
        {
            base.OnInit();
            sevenpokerOpenBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.Get<PopupGuideBook>().OpenBook(GuideBookType.SevenPoker);
            });
            badugiOpenBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.Get<PopupGuideBook>().OpenBook(GuideBookType.Badugi);
            });
            holdemOpenBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.Get<PopupGuideBook>().OpenBook(GuideBookType.Holdem);
            });
            jackpotOpenBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.Get<PopupGuideBook>().OpenBook(GuideBookType.JackPot);
            });
            dealerFeeOpenBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.Get<PopupGuideBook>().OpenBook(GuideBookType.DealerFee);
            });
            classInfoOpenBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.Get<PopupGuideBook>().OpenBook(GuideBookType.ClassInfo);
            });
            guildOpenBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FeatureComingSoon].StringToLocal, false));
            });
        }

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnClose()
        {
            base.OnClose();
        }
    }

}
