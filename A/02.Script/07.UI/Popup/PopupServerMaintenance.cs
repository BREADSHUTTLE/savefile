using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CAPYBARA
{
    public class PopupServerMaintenance : BasePopup
    {
        [SerializeField] private GameObject maintenanceWarningWindow;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private TMP_Text startTimeText;
        [SerializeField] private TMP_Text endTimeText;
        [SerializeField] private CPButton yesButton;
        
        [SerializeField] private GameObject maintenanceStartWindow;
        [SerializeField] private CPButton confirmLogoutButton;

        [SerializeField] private GameObject maintenanceWindowAtLogin;
        [SerializeField] private CPButton confirmQuitButtonAtLogin;
        [SerializeField] private TMP_Text maintenanceTimeTextAtLogin;
        protected override void OnInit()
        {
            if (yesButton != null)
            {
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(Close);
            }
            
            if (confirmLogoutButton != null)
            {
                confirmLogoutButton.onClick.RemoveAllListeners();
                confirmLogoutButton.onClick.AddListener(GameQuit);
            }
            if (confirmQuitButtonAtLogin != null)
            {
                confirmQuitButtonAtLogin.onClick.RemoveAllListeners();
                confirmQuitButtonAtLogin.onClick.AddListener(Application.Quit);
            }
        }

        public void SetMaintenanceKick()
        {
            maintenanceStartWindow.gameObject.SetActive(true);
            maintenanceWarningWindow.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(false);
        }

        public void SetMaintenanceAtLogin(string time)
        {
            maintenanceWindowAtLogin.SetActive(true);

            var parts = time.Trim().Split('|');
            if (parts.Length < 2) return;

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var kst = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

            if (long.TryParse(parts[0].Trim(), out long startUnix) &&
                long.TryParse(parts[1].Trim(), out long endUnix))
            {
                var startKst = TimeZoneInfo.ConvertTimeFromUtc(epoch.AddSeconds(startUnix), kst);
                var endKst = TimeZoneInfo.ConvertTimeFromUtc(epoch.AddSeconds(endUnix), kst);
                maintenanceTimeTextAtLogin.text = $"{startKst:HH:mm}~{endKst:HH:mm}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MaintenanceTimeMsg].StringToLocal}";
            }
        }

        private void GameQuit()
        {
            //Application.Quit();
            LogoutForThisAccount();
        }
        void LogoutForThisAccount()
        {
            LocalSaveLoader.DeleteCloudData();
            
            ConnectionManager.Instance.Dispose();
            
            PoolManager.Clear();
            PopupManager.Instance.CloseAll();
            
            SceneManager.LoadScene("Loading");
        }
        
        public void SetMaintenanceTime(lobby.MaintenanceNoti maintenance)
        {
            closeButton.gameObject.SetActive(true);
            maintenanceStartWindow.gameObject.SetActive(false);
            maintenanceWarningWindow.gameObject.SetActive(true);
            
            reasonText.text =$"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MaintenanceReason].StringToLocal}{maintenance.Reason}";
            
            var kst = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");    
            var startKst=TimeZoneInfo.ConvertTimeFromUtc(maintenance.Start.ToDateTime(), kst);  
            
            startTimeText.text = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MaintenanceStart].StringToLocal}{ startKst.ToString("HH:mm:ss")}";
            
            var endKst=TimeZoneInfo.ConvertTimeFromUtc(maintenance.End.ToDateTime(), kst);  
            
            endTimeText.text = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MaintenanceEnd].StringToLocal}{ endKst.ToString("HH:mm:ss")}";
        }
        
        public void SetMaintenanceTime(lobby.Maintenance maintenance)
        {
            closeButton.gameObject.SetActive(true);
            maintenanceStartWindow.gameObject.SetActive(false);
            maintenanceWarningWindow.gameObject.SetActive(true);
            
            reasonText.text =$"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MaintenanceReason].StringToLocal}{maintenance.Reason}";
            
            var kst = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");    
            var startKst=TimeZoneInfo.ConvertTimeFromUtc(maintenance.Start.ToDateTime(), kst);  
            
            startTimeText.text = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MaintenanceStart].StringToLocal}{ startKst.ToString("HH:mm:ss")}";
            
            var endKst=TimeZoneInfo.ConvertTimeFromUtc(maintenance.End.ToDateTime(), kst);  
            
            endTimeText.text = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MaintenanceEnd].StringToLocal}{ endKst.ToString("HH:mm:ss")}";
        }

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnClose()
        {
            base.OnClose();
            maintenanceWindowAtLogin.SetActive(false);
            maintenanceStartWindow.SetActive(false);
            maintenanceWarningWindow.SetActive(false);
        }
    }
}
