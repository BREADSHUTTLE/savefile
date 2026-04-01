using System;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class TotalLoginView : MonoBehaviour
    {
        [Header("로그인 스크린")] 
        [SerializeField] public Button googleLoginButton;
        [SerializeField] public Button appleLoginButton;
        [SerializeField] public Button naverLoginButton;
        [SerializeField] public Button kakaoLoginButton;
        [SerializeField] public Button atozLoginButton;

        [Space(5)] [SerializeField] GoogleLoginController googleLoginController;
        [SerializeField] AppleLoginController appleLoginController;
        [SerializeField] private OAuthWithUniWebView otherLoginController;

        public void Init()
        {
            this.gameObject.SetActive(false);

#if UNITY_ANDROID
            appleLoginButton.gameObject.SetActive(false);
#elif UNITY_IOS
            appleLoginButton.gameObject.SetActive(true);
#endif
        }

        public async UniTask<bool> GoogleLoginPcs()
        {
            bool isSuccess = false;
            try
            {
                isSuccess=await googleLoginController.GoogleLoginStart();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
                return false;
            }

            return isSuccess;
        }

        public async UniTask<bool> AppleLoginPcs()
        {
            try
            {
                var result = await appleLoginController.SignInAsync();

                LoginData.Cloud.loginValue.userAccountID = result.UserId;
                if (string.IsNullOrEmpty(result.Email))
                {
                    LoginData.Cloud.loginValue.userSocialEmail = "notExist";
                }
                else
                {
                    LoginData.Cloud.loginValue.userSocialEmail = result.Email;
                }

                LoginData.Cloud.loginValue.userSocialToken = result.IdentityToken;
                LoginData.Cloud.loginValue.accessToken = result.IdentityToken;
                LoginData.Cloud.loginValue.loginType = LoginType.APPLE;
                

                Debug.LogError($"apple login infos::{result.UserId} ///{result.Email}//// {result.IdentityToken}");
                
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
                return false;
            }
            return true;
        }

        public async UniTask<bool> NaverLoginPcs()
        {
            bool isSuccess = false;

            try
            {
                isSuccess = await otherLoginController.StartLogin(isnaver: true);
                if (isSuccess)
                {
                }
                else
                {
                    Debug.LogError("login fail");
                    LoginFailProcess();
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
                return false;
            }
            return true;
        }

        public async UniTask<bool> KakaoLoginPcs()
        {
            bool isSuccess = false;

            try
            {
                isSuccess = await otherLoginController.StartLogin(isnaver: false);
                if (isSuccess)
                {
                }
                else
                {
                    Debug.LogError("login fail");
                    LoginFailProcess();
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
                return false;
            }

            return true;
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
    }
}