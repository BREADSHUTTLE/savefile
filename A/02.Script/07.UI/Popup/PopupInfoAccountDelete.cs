using CAPYBARA.Bundles;
using UnityEngine;

namespace CAPYBARA
{
    public class PopupInfoAccountDelete : BasePopup
    {
        [SerializeField] private CPButton yesButton;
        [SerializeField] private CPButton noButton;

        protected override void OnInit()
        {
            if (yesButton != null)
            {
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(OnClickYes);
            }
            
            if (noButton != null)
            {
                noButton.onClick.RemoveAllListeners();
                noButton.onClick.AddListener(Close);
            }
        }

        private void OnClickYes()
        {
            PopupManager.Instance.Open<PopupQuestionAccountDelete>();
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
