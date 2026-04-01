using System;
using CAPYBARA.Bundles;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 회원 탈퇴 확인 팝업
    /// </summary>
    public class PopupQuitSign : BasePopup
    {
        [SerializeField] private CPButton confirmButton;

        private Action onConfirmCallback;

        protected override void OnInit()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnClickConfirm);
            }
        }

        public void Setup(Action onConfirm)
        {
            onConfirmCallback = onConfirm;
            Open();
        }

        private void OnClickConfirm()
        {
            onConfirmCallback?.Invoke();
            Close();
        }

        protected override void OnClose()
        {
            base.OnClose();
            onConfirmCallback = null;
        }
    }
}
