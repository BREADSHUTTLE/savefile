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
    public class AtozLoginView : MonoBehaviour,IBackButtonSender
    {
        [SerializeField] public Button closeLoginWindowBtn;
        [SerializeField] public TMPro.TMP_InputField idInputField;
        [SerializeField] private TMP_Text idErrorText;
        [SerializeField] public TMPro.TMP_InputField pwdInputField;
        [SerializeField] private TMP_Text pwErrorText;
        [SerializeField] public CPButton atozLoginBtn;
        [SerializeField] public GameObject loginDim;
        [SerializeField] public Button findUserIdOpenBtn;
        [SerializeField] public Button atozRegisterOpenBtn;

        [SerializeField] private Sprite errorInputfieldImage;
        [SerializeField] private Sprite defaultInputfieldImage;
        private void Awake()
        {
            closeLoginWindowBtn.onClick.RemoveAllListeners();
            closeLoginWindowBtn.onClick.AddListener(OnBackButtonPressed);
            idInputField.onSelect.AddListener((s) =>
            {
                idInputField.image.sprite = defaultInputfieldImage;
                pwdInputField.image.sprite = defaultInputfieldImage;
            });
            idInputField.onValueChanged.AddListener((text) =>
            {
                bool cantLogin = string.IsNullOrEmpty(idInputField.text) || string.IsNullOrEmpty(pwdInputField.text);
                loginDim.SetActive(cantLogin);
            });
        
            pwdInputField.onSelect.AddListener((s) =>
            {
                idInputField.image.sprite = defaultInputfieldImage;
                pwdInputField.image.sprite = defaultInputfieldImage;
            });
            pwdInputField.onValueChanged.AddListener((text) =>
            {
                bool cantLogin = string.IsNullOrEmpty(idInputField.text) || string.IsNullOrEmpty(pwdInputField.text);
                loginDim.SetActive(cantLogin);
            });
        }

        public void OpenWindow()
        {
            SceneLoadResources.OpenPopup(this);
            this.gameObject.SetActive(true);
            
            idInputField.image.sprite = defaultInputfieldImage;
            pwdInputField.image.sprite = defaultInputfieldImage;
            
            idInputField.text = "";
            pwdInputField.text = "";
            
            loginDim.SetActive(true);
        }
        public async UniTask<LobbyClient.PacketResult<LoginRes>> AtozLoginPcs(bool multipleLogin=false)
        {
            string id = idInputField.text;
            string pwd = pwdInputField.text;

            LobbyClient.PacketResult<LoginRes> loginRes = null;
            //try
            {
                idErrorText.text = null;
                pwErrorText.text = null;
                loginRes = await Services.Lobby.LoginAsync(id, pwd,multipleLogin);
                if (!loginRes.IsSuccess)
                {
                    if (loginRes.Error.Code == ErrorCode.EInvalidUid)//id error
                    {
                        idInputField.image.sprite = errorInputfieldImage;
                        pwdInputField.image.sprite = errorInputfieldImage;
                        idErrorText.text = null;
                        pwErrorText.text = null;
                    }
                    if (loginRes.Error.Code == ErrorCode.EUserNotExist)
                    {
                        idInputField.image.sprite = errorInputfieldImage;
                        pwdInputField.image.sprite = errorInputfieldImage;
                        idErrorText.text = null;
                        pwErrorText.text = null;
                    }
              
                    return loginRes;
                }
                    
                LoginData.Cloud.loginValue.userAccountID = id;
                LoginData.Cloud.loginValue.userAccountPw = pwd;
                LoginData.Cloud.loginValue.loginType = LoginType.ATOZ;
                LoginData.Cloud.loginValue.UID = loginRes.Data.Uid;
                LoginData.Cloud.loginValue.userAutoToken = loginRes.Data.Token;
                
            
            }
            // catch (Exception e)
            // {
            //     Debug.LogError(e.Message);
            //     LoginFailProcess();
            // }
            
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
