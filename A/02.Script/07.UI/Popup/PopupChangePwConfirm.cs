using System;
using CAPYBARA.Bundles;
using UnityEngine;

namespace CAPYBARA
{
    public class PopupChangePwConfirm : BasePopup
    {
        [SerializeField] private CPButton btnClose;

        private Action closeCallback;
        protected override void OnInit()
        {
            btnClose.onClick.AddListener(Close);
        }

        public override void Close()
        {
            base.Close();
            closeCallback?.Invoke();
            closeCallback = null;
        }

        public void SetCloseCallback(Action _closeCallback)
        {
            closeCallback = _closeCallback;
        }

        protected override void OnOpen()
        {
        }

        protected override void OnClose()
        {
        }
    }

}
