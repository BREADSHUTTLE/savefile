using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CAPYBARA
{
    public class AccountBanWarningWindow : MonoBehaviour,IBackButtonSender
    {
        [SerializeField]private CPButton closeButton;
        [SerializeField]private CPButton cancelButton;
        [SerializeField]private CPButton openUrlButton;

        [SerializeField] private TMP_Text banTitle;
        
        [SerializeField] private GameObject banReasonObject;
        [SerializeField] private TMP_Text banReasonTxt;
        [SerializeField] private GameObject banDateTimeObject;
        [SerializeField] private TMP_Text banDateTimeTxt;
        
        private void Awake()
        {
            closeButton.onClick.AddListener(OnBackButtonPressed);
            cancelButton.onClick.AddListener(OnBackButtonPressed);
            openUrlButton.onClick.AddListener(() =>
            {
                Application.OpenURL("https://www.atozgames.net/inquiry");
            });
        }

        public void OpenWindow(lobby.Error errorDesc)
        {
            if (errorDesc.Code == ErrorCode.EBanPermanent)
            {
                banTitle.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.GamePermanentBan].StringToLocal;
                banDateTimeObject.gameObject.SetActive(false);
                banReasonTxt.text=errorDesc.Detail;
            }
            else
            {
                banTitle.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.GameTemporaryBan].StringToLocal;
                string[] results = errorDesc.Detail.Split('|');
                banDateTimeObject.gameObject.SetActive(true);
           
                long unixTimestamp = long.Parse(results[0]);
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
                DateTime kstTime = dateTimeOffset.ToOffset(TimeSpan.FromHours(9)).DateTime;
                banDateTimeTxt.text = kstTime.ToString();
                
                banReasonTxt.text = results[1];
            }
            SceneLoadResources.OpenPopup(this);
            this.gameObject.SetActive(true);
         
        }

        public void OnBackButtonPressed()
        {
            SceneLoadResources.ClosePopup();
        }
        
        public void CloseThisWindow()
        {
            this.gameObject.SetActive(false);
        }
    }

}
