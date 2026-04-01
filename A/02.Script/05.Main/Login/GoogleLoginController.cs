using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
//using Firebase;
//using Firebase.Auth;
using Google;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class GoogleLoginController : MonoBehaviour
    {
        private GoogleSignInConfiguration configuration;

        const string webclientID = "984761693118-ki359ltv09lp67ufeopiehmqesuatf3c.apps.googleusercontent.com";
        public string IdToken = null;
        [HideInInspector] public string userID = null;
        [HideInInspector] public string userEmail = null;

        //private FirebaseApp app;
        //private FirebaseAuth auth;
        //private FirebaseUser user = null;

        [SerializeField] private OAuthWithUniWebView loginWebview;
        public void Init()
        {
            //Screen.orientation = ScreenOrientation.Portrait;
            InitGoogleConfig();
        }

        public void InitGoogleConfig()
        {
            if (configuration == null)
            {
                configuration = new GoogleSignInConfiguration
                {
                    WebClientId = webclientID,
                    RequestEmail = true,
                    RequestIdToken = true,
                    RequestAuthCode = true,
                    UseGameSignIn = false
                };
            }

            // DefaultInstance 생성 후에는 설정 변경 불가하므로 최초 1회만 세팅
            if (GoogleSignIn.Configuration == null)
                GoogleSignIn.Configuration = configuration;

            #if UNITY_EDITOR
            #elif UNITY_ANDROID
            SignOutAndClear();
            #endif
        }
 
        public void SignOutAndClear()
        {
            // SignOut은 단순히 현재 세션을 종료합니다.
            GoogleSignIn.DefaultInstance.SignOut();
    
            // Disconnect는 앱에 부여된 권한을 철회하고 연결을 끊어 
            // 다음 로그인 시 계정 선택 창을 강제로 띄우게 합니다.
            GoogleSignIn.DefaultInstance.Disconnect();
    
            Debug.Log("로그아웃 및 연결 해제 완료");
        }

        public async UniTask<bool> GoogleLoginStart()
        {
            return await GooglesigninProcess();
        }
        //google login
        public async UniTask<bool> GooglesigninProcess()
        {
            IdToken = null;
            userID = null;
            
            try
            {
                var user = await GoogleSignIn.DefaultInstance.SignIn();

                IdToken = user.IdToken;
                userID = user.UserId;
                userEmail = user.Email;

                LoginData.Cloud.loginValue.userAccountID = userID;
                LoginData.Cloud.loginValue.userSocialEmail = userEmail;
                LoginData.Cloud.loginValue.userSocialToken = IdToken;
                LoginData.Cloud.loginValue.accessToken = IdToken;
                LoginData.Cloud.loginValue.loginType = LoginType.GOOGLE;

                Debug.Log($"idtoken:{IdToken}, userId:{userID}");
                return true;
            }
            catch (GoogleSignIn.SignInException e)
            {
                Debug.LogError($"Google SignIn Failed: {e.Status} {e.Message}");
                return false;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Google SignIn Canceled");
                return false;
            }

        }

        void OnAuthenticationFinished(System.Threading.Tasks.Task<GoogleSignInUser> task)
        {
            if (task.IsFaulted)
            {
                using (IEnumerator<Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
                    }
                }
            }
            else if (task.IsCanceled)
            {
            }
            else
            {
                IdToken = task.Result.IdToken;
                userID = task.Result.UserId;
                userEmail = task.Result.Email;
                
                LoginData.Cloud.loginValue.userAccountID = userID;
                LoginData.Cloud.loginValue.userSocialEmail = userEmail;
                LoginData.Cloud.loginValue.userSocialToken = IdToken;
                LoginData.Cloud.loginValue.accessToken = IdToken;
                LoginData.Cloud.loginValue.loginType = LoginType.GOOGLE;
                
                Debug.Log($"idtoken:{IdToken},,userId: {userID}");
            }
        }


        bool isProcessSuccess = false;
        public async UniTask Signin(System.Action<bool> callback)
        {
            // Firebase.Auth.Credential credential = GoogleAuthProvider.GetCredential(IdToken, null);
            // await auth.SignInWithCredentialAsync(credential).ContinueWith(task => {
            //     if (task.IsCanceled)
            //     {
            //         isProcessSuccess = false;
            //         return;
            //     }
            //     if (task.IsFaulted)
            //     {
            //         isProcessSuccess = false;
            //         return;
            //     }

            //     isProcessSuccess = true;
            //     //resultText.text = $"google login result!!:{o}\nGoogle ID: {IdToken}";
            //     //Debug.Log($"google login result!!:{o}\nGoogle ID: {IdToken}");
            // });
            isProcessSuccess = true;
            callback?.Invoke(isProcessSuccess);
        }

    }
}
