using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class ChangePwdView : MonoBehaviour,IBackButtonSender
    {
        [SerializeField]public Button closeChangePwWindowBtn;
        [SerializeField] private TMPro.TMP_InputField pwdChangeInputField;
        [SerializeField] private TMP_Text pwErrorText;
        [SerializeField] private TMPro.TMP_InputField repwdChangeInputField;
        [SerializeField] private TMP_Text repwErrorText;
        [SerializeField] public Button changePwdBtn;
        
        private lobby.UserWithToken currentUserinfo;
        [SerializeField] private Sprite errorInputfieldImage;
        [SerializeField] private Sprite defaultInputfieldImage;
        private bool IsAllValid()
        {
            string pwd = pwdChangeInputField.text;
            if (string.IsNullOrEmpty(pwd) || string.IsNullOrEmpty(repwdChangeInputField.text))
                return false;
            bool validLength = pwd.Length >= 6 && pwd.Length <= 20;
            bool validChars = Regex.IsMatch(pwd, @"^[a-zA-Z0-9!@#$%^&*()_\+\-=\[\]{};:'"",.<>?/\\]+$");
            return validLength && validChars && pwd == repwdChangeInputField.text;
        }

        private void Awake()
        {
            changePwdBtn.interactable = false;
            closeChangePwWindowBtn.onClick.RemoveAllListeners();
            closeChangePwWindowBtn.onClick.AddListener(OnBackButtonPressed);
            pwdChangeInputField.onSelect.AddListener((s) =>
            {
                pwdChangeInputField.image.sprite = defaultInputfieldImage;
            });
            repwdChangeInputField.onSelect.AddListener((s) =>
            {
                repwdChangeInputField.image.sprite = defaultInputfieldImage;
            });
            
            pwdChangeInputField.onValueChanged.AddListener((text) =>
            {
                bool validLength = text.Length >= 6 && text.Length <= 20;
                bool validChars =Regex.IsMatch(text, @"^[a-zA-Z0-9!@#$%^&*()_\+\-=\[\]{};:'"",.<>?/\\]+$");

                pwdChangeInputField.image.sprite = defaultInputfieldImage;
                pwErrorText.text = null;
                
                if (string.IsNullOrEmpty(text))
                {
                    pwdChangeInputField.image.sprite = defaultInputfieldImage;
                    pwErrorText.text = null;
                }
                else if (!validChars)
                {
                    pwdChangeInputField.image.sprite = errorInputfieldImage;
                    pwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PasswordAlphanumericSpecial].StringToLocal;
                }
                else if (!validLength)
                {
                    pwdChangeInputField.image.sprite = errorInputfieldImage;
                    pwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Length6to20].StringToLocal;
                }
                else if (repwdChangeInputField.text != text)
                {
                    repwdChangeInputField.image.sprite = errorInputfieldImage;
                    repwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PasswordMismatch].StringToLocal;
                }
                else if (repwdChangeInputField.text == text)
                {
                    repwdChangeInputField.image.sprite = defaultInputfieldImage;
                    repwErrorText.text = null;
                }
                else
                {
                    pwdChangeInputField.image.sprite = defaultInputfieldImage;
                    pwErrorText.text = null;
                }
                changePwdBtn.interactable = IsAllValid();
            });

            repwdChangeInputField.onValueChanged.AddListener((text) =>
            {
                bool validLength = text.Length >= 6 && text.Length <= 20;
                bool validChars =Regex.IsMatch(text, @"^[a-zA-Z0-9!@#$%^&*()_\+\-=\[\]{};:'"",.<>?/\\]+$");

                repwErrorText.text = null;
                
                if (string.IsNullOrEmpty(text))
                {
                    repwdChangeInputField.image.sprite = defaultInputfieldImage;
                    repwErrorText.text = null;
                }
                else if (pwdChangeInputField.text != text)
                {
                    repwdChangeInputField.image.sprite = errorInputfieldImage;
                    repwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PasswordMismatch].StringToLocal;
                }
                else
                {
                    repwdChangeInputField.image.sprite = defaultInputfieldImage;
                    repwErrorText.text = null;
                }
                changePwdBtn.interactable = IsAllValid();
            });
        }
        public void OpenChangeUserPwView(lobby.UserWithToken currentSelectedUserinfo)
        {
            currentUserinfo = currentSelectedUserinfo;
            changePwdBtn.interactable = false;
            SceneLoadResources.OpenPopup(this);
            this.gameObject.SetActive(true);
        }
        
        public async UniTask<bool> ChangeUserPwProccess()
        {
            if (string.IsNullOrEmpty(pwdChangeInputField.text) || string.IsNullOrEmpty(repwdChangeInputField.text))
            {
                pwdChangeInputField.text = null;
                repwdChangeInputField.text = null;
                return false;
            }

            string pwd = pwdChangeInputField.text;
            string repwd = repwdChangeInputField.text;
            if (pwd != repwd)
            {
                pwdChangeInputField.text = null;
                repwdChangeInputField.text = null;
                return false;
            }

            pwErrorText.text = null;
            repwErrorText.text = null;
            var changepwdRes = await Services.Lobby.SetPwdAsync(currentUserinfo.Uid,pwd);
            LoginData.Cloud.loginValue.loginType = LoginType.ATOZ;
            if (changepwdRes.IsSuccess)
            {
                LoginData.Cloud.loginValue.userAccountPw = pwd;
                
                pwdChangeInputField.text = null;
                repwdChangeInputField.text = null;
                return true;
            }
            else
            {
                pwdChangeInputField.image.sprite = errorInputfieldImage;
                repwdChangeInputField.image.sprite = errorInputfieldImage;
                pwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PasswordInfoWrong].StringToLocal;
                repwErrorText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PasswordInfoWrong].StringToLocal;
            }
            pwdChangeInputField.text = null;
            repwdChangeInputField.text = null;
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
