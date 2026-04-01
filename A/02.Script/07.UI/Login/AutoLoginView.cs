using System;
using System.Linq;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using CAPYBARA.Model;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class AutoLoginView : MonoBehaviour,IBackButtonSender
    {
        public CPButton closeSimpleLoginBtn;
        public LoginAccountSlot loginAccountSlot;
        public Transform accountSlotParent;
        
        [HideInInspector]public List<LoginAccountSlot> loginAccountSlotList = new List<LoginAccountSlot>();

        private void Awake()
        {
            closeSimpleLoginBtn.onClick.RemoveAllListeners();
            closeSimpleLoginBtn.onClick.AddListener(OnBackButtonPressed);
        }

        public void OpenWindow()
        {
            SceneLoadResources.OpenPopup(this);
            this.gameObject.SetActive(true);
        }

        public async UniTask<LobbyClient.PacketResult<LoginRes> > AutoLoginPcs(LoginCloudData.UserSavedInfo userinfo,lobby.User serveruserinfo,bool multipleLogin=false)
        {
            string autotoken =userinfo.userLoginToken;
            
            LobbyClient.PacketResult<LoginRes> loginRes = null;
            try
            {
                Extension.eLog($"내 아이디:{    LoginData.Cloud.loginValue.userAccountID},,내 토큰값:{    autotoken}",Color.cyan);
                
                var logintype= Extension.StringToEnum<LoginType>(serveruserinfo.LoginType);
                LoginData.Cloud.loginValue.loginType = logintype;
                LoginData.Cloud.loginValue.userAccountID = userinfo.accountID;
                
                loginRes = await Services.Lobby.AutoLoginAsync(autotoken,multipleLogin);
             
                if (loginRes.IsSuccess)
                {
                    LoginData.Cloud.loginValue.UID=loginRes.Data.Uid;
                    LoginData.Cloud.loginValue.userAutoToken = loginRes.Data.Token;
                }
             
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
                return null;
            }
            
           
            return loginRes;
        }
        
        #region login_register_fail Proecess

        void LoginFailProcess()
        {
        }

        void RegisterFailProcess()
        {
        }

        void IdentifyFailProcess()
        {
        }

        #endregion

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
