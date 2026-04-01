using System;
using CAPYBARA.Bundles;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PopupTokenExpired : BasePopup
    {
        [SerializeField]private CPButton confirmTokenInvalidBtn;
        private Action confirmCallback;
        protected override void OnInit()
        {
            base.OnInit();
            confirmTokenInvalidBtn.onClick.AddListener(PushConfirmBtn);
        }

        private void PushConfirmBtn()
        {
            this.gameObject.SetActive(false);
            confirmCallback?.Invoke();
        }

        public void SetInvalidConfirmBtn(Action callback)
        {
            confirmCallback = callback;
        }
        
        
    }

}
