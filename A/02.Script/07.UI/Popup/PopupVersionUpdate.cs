using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace CAPYBARA
{
    public class PopupVersionUpdate : BasePopup
    {
        [SerializeField] private GameObject askWindow;
        [SerializeField] private CPButton noBtnInAsk;
        [SerializeField] private CPButton yesBtnInAsk;
        
        [SerializeField] private GameObject forceWindow;
        [SerializeField] private CPButton yesBtnInForce;

        private string storeUrl = "https://www.atozgames.net/";
        protected override void OnInit()
        {
            noBtnInAsk.onClick.RemoveAllListeners();
            noBtnInAsk.onClick.AddListener(Close);
            
            yesBtnInAsk.onClick.RemoveAllListeners();
            yesBtnInAsk.onClick.AddListener(GotoStoreUrl);
            
            yesBtnInForce.onClick.RemoveAllListeners();
            yesBtnInForce.onClick.AddListener(GotoStoreUrl);
        }

        private void GotoStoreUrl()
        {
            Application.OpenURL(storeUrl);
            Application.Quit();
        }
        public void SetWindow(bool isAsk)
        {
            askWindow.SetActive(isAsk);
            forceWindow.SetActive(!isAsk);
        }
        
    }

}
