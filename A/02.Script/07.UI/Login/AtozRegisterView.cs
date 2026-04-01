using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class AtozRegisterView : MonoBehaviour,IBackButtonSender
    {
        [SerializeField] public TMPro.TMP_InputField regidInputField;
        [SerializeField] private TMP_Text idErrorText;
        [SerializeField] public TMPro.TMP_InputField regpwdInputField;
        [SerializeField] private TMP_Text pwErrorText;
        [SerializeField] public TMPro.TMP_InputField regpwdConfirmInputField;
        [SerializeField] private TMP_Text repwErrorText;
        [SerializeField] public Button atozRegisterBtn;
        [SerializeField] public Button closeRegisterWindowBtn;
        
        [SerializeField] private Sprite errorInputfieldImage;
        [SerializeField] private Sprite defaultInputfieldImage;
        private void Awake()
        {
            closeRegisterWindowBtn.onClick.RemoveAllListeners();
            closeRegisterWindowBtn.onClick.AddListener(OnBackButtonPressed);
            regidInputField.onSelect.AddListener((s) =>
            {
                regidInputField.image.sprite = defaultInputfieldImage;
            });
            regidInputField.onValueChanged.AddListener((text) =>
            {
                bool validChars = Regex.IsMatch(text, @"^[a-z0-9]*$");
                bool validLength = text.Length >= 6 && text.Length <= 15;

                if (string.IsNullOrEmpty(text))
                {
                    regidInputField.image.sprite = defaultInputfieldImage;
                    idErrorText.text = null;
                }
                else if (!validChars)
                {
                    regidInputField.image.sprite = errorInputfieldImage;
                    idErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OnlyAlphanumericLower].StringToLocal;
                }
                else if (!validLength)
                {
                    regidInputField.image.sprite = errorInputfieldImage;
                    idErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Length6to15].StringToLocal;
                }
                else
                {
                    regidInputField.image.sprite = defaultInputfieldImage;
                    idErrorText.text = null;
                }
                    
            });
            regpwdInputField.onSelect.AddListener((s) =>
            {
                regpwdInputField.image.sprite = defaultInputfieldImage;
            });
            regpwdInputField.onValueChanged.AddListener((text) =>
            {
                bool validLength = text.Length >= 6 && text.Length <= 20;
                bool validChars =Regex.IsMatch(text, @"^[a-zA-Z0-9!@#$%^&*()_\+\-=\[\]{};:'"",.<>?/\\]+$");

                regpwdInputField.image.sprite = defaultInputfieldImage;
                pwErrorText.text = null;
                
                if (string.IsNullOrEmpty(text))
                {
                    regpwdInputField.image.sprite = defaultInputfieldImage;
                    pwErrorText.text = null;
                }
                else if (!validChars)
                {
                    regpwdInputField.image.sprite = errorInputfieldImage;
                    pwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PasswordAlphanumericSpecial].StringToLocal;
                }
                else if (!validLength)
                {
                    regpwdInputField.image.sprite = errorInputfieldImage;
                    pwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Length6to20].StringToLocal;
                }
                else if (regpwdConfirmInputField.text != text)
                {
                    regpwdConfirmInputField.image.sprite = errorInputfieldImage;
                    repwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PasswordMismatch].StringToLocal;
                }
                else if (regpwdConfirmInputField.text == text)
                {
                    regpwdConfirmInputField.image.sprite = defaultInputfieldImage;
                    repwErrorText.text = null;
                }
                else
                {
                    regpwdInputField.image.sprite = defaultInputfieldImage;
                    pwErrorText.text = null;
                }
            });
            regpwdConfirmInputField.onSelect.AddListener((s) =>
            {
                regpwdConfirmInputField.image.sprite = defaultInputfieldImage;
            });
            regpwdConfirmInputField.onValueChanged.AddListener((text) =>
            {
                bool validLength = text.Length >= 6 && text.Length <= 20;
                bool validChars =Regex.IsMatch(text, @"^[a-zA-Z0-9!@#$%^&*()_\+\-=\[\]{};:'"",.<>?/\\]+$");

                regidInputField.image.sprite = defaultInputfieldImage;
                repwErrorText.text = null;
                
                if (string.IsNullOrEmpty(text))
                {
                    regpwdConfirmInputField.image.sprite = defaultInputfieldImage;
                    repwErrorText.text = null;
                }
                else if (regpwdInputField.text != text)
                {
                    regpwdConfirmInputField.image.sprite = errorInputfieldImage;
                    repwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PasswordMismatch].StringToLocal;
                }
                else
                {
                    regpwdConfirmInputField.image.sprite = defaultInputfieldImage;
                    repwErrorText.text = null;
                }
                    
            });
        }
        public void OpenWindow()
        {
            SceneLoadResources.OpenPopup(this);
            this.gameObject.SetActive(true);
        }
        [Tooltip("editor용 테스트 mid")]    
        [SerializeField] private int testMidValue = 0;
        public async UniTask<bool> AtozRegister()
        {
            if (string.IsNullOrEmpty(regidInputField.text) || string.IsNullOrEmpty(regpwdInputField.text) ||
                string.IsNullOrEmpty(regpwdConfirmInputField.text))
            {
                regidInputField.image.sprite = errorInputfieldImage;
                regpwdInputField.image.sprite = errorInputfieldImage;
                regpwdConfirmInputField.image.sprite = errorInputfieldImage;
                idErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IDAndPasswordRequired].StringToLocal;
                pwErrorText.text =  StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IDAndPasswordRequired].StringToLocal;
                repwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IDAndPasswordRequired].StringToLocal;
                return false;
            }

            if (regpwdInputField.text != regpwdConfirmInputField.text)
            {
                regidInputField.image.sprite = defaultInputfieldImage;
                regpwdInputField.image.sprite = defaultInputfieldImage;
                regpwdConfirmInputField.image.sprite = errorInputfieldImage;
                idErrorText.text = null;
                pwErrorText.text = null;
                repwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PasswordMismatch].StringToLocal;
                return false;
            }

            string id = regidInputField.text;
            string pwd = regpwdInputField.text;

            idErrorText.text = null;
            pwErrorText.text = null;
            repwErrorText.text = null;
            
            var registerRes = await Services.Lobby.RegisterAsync(id, pwd, "", "ATOZ", LoginData.Cloud.loginValue.registerToken);

            if (registerRes.IsSuccess)
            {
                LoginData.Cloud.loginValue.isFirstLogin = true;
                

                LoginData.Cloud.loginValue.userAccountID = id;
                LoginData.Cloud.loginValue.userAccountPw = pwd;
                LoginData.Cloud.loginValue.loginType = LoginType.ATOZ;

                return true;
            }
            else
            {
                if (registerRes.Error.Code == ErrorCode.ECharError)
                {
                    bool isPwMatch = Regex.IsMatch(
                        pwd,
                        @"^[a-zA-Z0-9!@#$%^&*()_\+\-=\[\]{};:'"",.<>?/\\]+$");
                    if (!isPwMatch)
                    {
                        regidInputField.image.sprite = defaultInputfieldImage;
                        regpwdInputField.image.sprite = errorInputfieldImage;
                        regpwdConfirmInputField.image.sprite = errorInputfieldImage;
                        pwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OnlyAlphanumericSpecial].StringToLocal;
                        repwErrorText.text= StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OnlyAlphanumericSpecial].StringToLocal;
                    }
                    else
                    {
                        regidInputField.image.sprite = errorInputfieldImage;
                        regpwdInputField.image.sprite = defaultInputfieldImage;
                        regpwdConfirmInputField.image.sprite = defaultInputfieldImage;
                        idErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.OnlyAlphanumericLower].StringToLocal;
                    }
               
             
                }
                else if (registerRes.Error.Code == ErrorCode.ELengthError)
                {
                 
                    if (regpwdInputField.text.Length < 6 || regpwdInputField.text.Length > 20)//pw length error
                    {
                        regidInputField.image.sprite = defaultInputfieldImage;
                        regpwdInputField.image.sprite = errorInputfieldImage;
                        regpwdConfirmInputField.image.sprite = defaultInputfieldImage;
                        pwErrorText.text  = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Length6to20].StringToLocal;
                        repwErrorText.text=  null;
                    }
                    else
                    {
                        regidInputField.image.sprite = errorInputfieldImage;
                        regpwdInputField.image.sprite = defaultInputfieldImage;
                        regpwdConfirmInputField.image.sprite = defaultInputfieldImage;
                        idErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Length6to15].StringToLocal;
                    }
                  
                }
                else if (registerRes.Error.Code == ErrorCode.EAlreadyRegister)
                {
                    regidInputField.image.sprite = errorInputfieldImage;
                    regpwdInputField.image.sprite = defaultInputfieldImage;
                    regpwdConfirmInputField.image.sprite = defaultInputfieldImage;
                    idErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IdAlreadyInUse].StringToLocal;
                }
                else if (registerRes.Error.Code == ErrorCode.ERegister5AccountLimit)
                {
                    PopupManager.Instance.Open<PopupAccountCreateLimitWarning>();
                }
                else
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.RegisterFailed].StringToLocal}{registerRes.Error}"));
                }
                
            }
            return false;
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
