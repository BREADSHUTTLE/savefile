using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CAPYBARA
{
    public class PopupAccountCreateLimitWarning : BasePopup
    {
        [SerializeField] private CPButton confirmButton;

        protected override void OnInit()
        {
            base.OnInit();
            confirmButton.onClick.AddListener(Close);
        }
    }

}
