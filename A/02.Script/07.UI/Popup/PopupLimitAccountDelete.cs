using CAPYBARA.Bundles;
using UnityEngine;

namespace CAPYBARA
{
    public class PopupLimitAccountDelete : BasePopup
    {
        [SerializeField] private CPButton btnClose;

        protected override void OnInit()
        {
            btnClose.onClick.AddListener(Close);
        }

        protected override void OnOpen()
        {
        }

        protected override void OnClose()
        {
        }
    }
}
