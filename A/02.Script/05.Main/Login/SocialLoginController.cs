using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using CAPYBARA.Model;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class SocialLoginController : MonoBehaviour
    {
        [Header("계정정지 팝업")] [SerializeField] public AccountBanWarningWindow accountBanWarningWindow;
        [Header("스플래쉬 이미지")] [SerializeField] public CanvasGroup splashWindow;

        [Header("로딩 스크린 이미지")] public CanvasGroup loadingLoginScreen;
        public GameObject loadingScreen;
        public Slider loadingSlider;
        public TMP_Text loadingText;

        [Header("로그인 스크린")] [SerializeField] TotalLoginView totalLoginView;

        [Header("간편 로그인 스크린")] [SerializeField]
        AutoLoginView autoLoginView;

        [Header("아토즈 로그인 스크린")] [SerializeField]
        AtozLoginView atozLoginView;

        [Header("아이디/비밀번호 찾기_아이디목록")] [SerializeField]
        AtozFindUserInfo atozFindUserIdView;

        [Header("아이디/비밀번호 찾기_비번 변경")] [SerializeField]
        ChangePwdView changePwdView;

        [Header("아토즈 회원가입 스크린")] [SerializeField]
        AtozRegisterView atozRegisterView;

        [Header("로딩 스크린")] public GameObject loadingScreenPopup;
        [Space(5)] [SerializeField] GoogleLoginController googleLoginController;
        [SerializeField] AppleLoginController appleLoginController;
        [SerializeField] private OAuthWithUniWebView otherLoginController;


        public string testAccountId;

        System.Threading.CancellationTokenSource _cts = new System.Threading.CancellationTokenSource();

        [Space(10)] [Header("Editor UserId 이곳에 테스트 계정 입력")]
        public string editorUserId;

        public string editorUserPw;

        [Space(10)] [Header("IP/PORT Connection")]
        public GameObject ipPortWindow;

        public TMP_InputField IPInputField;
        public TMP_InputField PortInputField;
        public Button ConnectBtn;
        public TMP_Text errorTxt;
        public TMP_Dropdown dropdown;

        public TMP_Text errorLoginMsg;

        private bool isButtonPressed = false;


        public ConfirmPopup confirmPopup;

        [SerializeField] private MultipleLoginTry multipleLoginTryView;

        public void Init()
        {
            totalLoginView.Init();
            atozLoginView.gameObject.SetActive(false);
            atozRegisterView.gameObject.SetActive(false);

            googleLoginController.Init();
            appleLoginController.Init();

            isButtonPressed = false;

            totalLoginView.googleLoginButton.onClick.AddListener(() =>
            {
                int count = LoginData.Cloud.loginValue.uidList.list                           
                    .Count(o => o.logintype == LoginType.GOOGLE);

                if (count > 0)
                {
                    SimpleLoginWindowOpen(LoginType.GOOGLE).Forget();
                }
                else
                {
                    GoogleLoginPcs().Forget();
                }
        
            });
            totalLoginView.appleLoginButton.onClick.AddListener(() =>
            {
                int count = LoginData.Cloud.loginValue.uidList.list                           
                    .Count(o => o.logintype == LoginType.APPLE);

                if (count > 0)
                {
                    SimpleLoginWindowOpen(LoginType.APPLE).Forget();
                }
                else
                {
                    AppleLoginPcs().Forget();
                }
            
            });
            totalLoginView.naverLoginButton.onClick.AddListener(() =>
            {
                int count = LoginData.Cloud.loginValue.uidList.list                           
                    .Count(o => o.logintype == LoginType.NAVER);

                if (count > 0)
                {
                    SimpleLoginWindowOpen(LoginType.NAVER).Forget();
                }
                else
                {
                    NaverLoginPcs().Forget();
                }
              
            });
            totalLoginView.kakaoLoginButton.onClick.AddListener(() => KakaoLoginPcs().Forget());
            totalLoginView.atozLoginButton.onClick.AddListener(() =>
            {
                OpenLoginWindowOrSimpleLogin(LoginType.ATOZ).Forget();
            });

            atozLoginView.atozLoginBtn.onClick.AddListener(() => AtozLoginPcs().Forget());
            atozLoginView.atozRegisterOpenBtn.onClick.AddListener(() => { OpenAtozRegisterWindow().Forget(); });
            atozLoginView.findUserIdOpenBtn.onClick.AddListener(() => { FindUserIdProgress().Forget(); });
            atozLoginView.closeLoginWindowBtn.onClick.AddListener(() => { atozLoginView.gameObject.SetActive(false); });

            atozRegisterView.atozRegisterBtn.onClick.AddListener(() => AtozRegister().Forget());


            changePwdView.changePwdBtn.onClick.AddListener(() => { ChangeUserPwProccess().Forget(); });
            _cts.Cancel();
            _cts = null;
            _cts = new CancellationTokenSource();
            InitDropDown();
            SceneLoadResources.OnProgress += CallbackLoading;
        }

        async UniTask OpenLoginWindowOrSimpleLogin(LoginType loginType)
        {
            int count = LoginData.Cloud.loginValue.uidList.list                           
                .Count(o => o.logintype == loginType);

            if (count > 0)
            {
                SimpleLoginWindowOpen(loginType).Forget();
            }
            else
            {
                AtozLoginWindowOpen();
            }
        }

        public async UniTask FadeInHoldOutAsync(CanvasGroup canvasGroup, float holdSeconds, float duration, CancellationToken ct = default)
        {
            canvasGroup.DOKill();

            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 페이드 인
            Tween fadeIn = canvasGroup
                .DOFade(1f, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            ct.Register(() => fadeIn.Kill());

            await fadeIn.AsyncWaitForCompletion();

            // 유지 시간
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(holdSeconds),
                cancellationToken: ct,
                ignoreTimeScale: true
            );

            // 페이드 아웃
            Tween fadeOut = canvasGroup
                .DOFade(0f, duration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true);

            ct.Register(() => fadeOut.Kill());

            await fadeOut.AsyncWaitForCompletion();
        }

        public void OpenLoadingPopup()
        {
            loadingScreen.SetActive(true);
            totalLoginView.gameObject.SetActive(false);
            loadingSlider.value = 0;
            loadingText.text = "0%";
        }

        void CallbackLoading(float sliderValue, string perStr)
        {
            loadingSlider.value = sliderValue;
            loadingText.text = perStr;
        }

        void ErrorToast(string msg)
        {
            errorLoginMsg.text = msg;
        }

        public void TotalLoginWindowOpen()
        {
            totalLoginView.gameObject.SetActive(true);
        }

        async UniTask SimpleLoginWindowOpen(LoginType loginType)
        {
            if (isButtonPressed)
                return;
            isButtonPressed = true;

            if ( LoginData.Cloud.loginValue.uidList.list.Count>0)
            {
                LoginData.Cloud.loginValue.isFirstLogin = false;

                foreach (var go in autoLoginView.loginAccountSlotList)
                {
                    if (go != null)
                        GameObject.Destroy(go.gameObject);
                }

                autoLoginView.loginAccountSlotList.Clear();

                var uidList = LoginData.Cloud.loginValue.uidList.list;
                var serverUsersResult = await Services.Lobby.UserReqByUserIdsAsync(uidList.Select(x => x.uid));
                var userLookup = serverUsersResult.Data.User.ToDictionary(u => u.Uid);

                foreach (var localSavedUserInfo in uidList)
                {
                    if (!userLookup.TryGetValue(localSavedUserInfo.uid, out var serverUser))
                        continue;
                    if (!serverUser.IsActive)
                        continue;
                    if(serverUser.LoginType==loginType.ToString())
                    {
                        var obj = Instantiate(autoLoginView.loginAccountSlot);
                        obj.transform.SetParent(autoLoginView.accountSlotParent, false);
                        obj.Init(serverUser);
                        Extension.eLog($"id:{serverUser.Id}//할당된 토큰값:{localSavedUserInfo.userLoginToken}", Color.cyan);
                        obj.loginBtn.onClick.AddListener(() =>
                        {
                            SimpleLoginPcs(localSavedUserInfo,serverUser).Forget();
                        });
                        obj.deleteBtn.onClick.AddListener(() =>
                        {
                            confirmPopup.OpenConfirmPopup(() =>
                                {
                                    if (LoginData.Cloud.loginValue.uidList.list.Any(o=>o.uid==serverUser.Uid))
                                    {
                                        var data=LoginData.Cloud.loginValue.uidList.list.FirstOrDefault(x => x.uid == serverUser.Uid);
                                        LoginData.Cloud.loginValue.uidList.list.Remove(data);
                                    }

                                    LocalSaveLoader.SaveAutoLoginUserIdList();
                                    Destroy(obj.gameObject);
                                },
                                null
                            );
                        });
                        autoLoginView.loginAccountSlotList.Add(obj);
                    }
                }

                var addAccountobj = Instantiate(autoLoginView.loginAccountSlot);
                addAccountobj.transform.SetParent(autoLoginView.accountSlotParent, false);
                addAccountobj.Init(null, false);
                addAccountobj.loginOtherId.onClick.AddListener(() => { AtozLoginWindowOpen(); });
                autoLoginView.loginAccountSlotList.Add(addAccountobj);
            }

            autoLoginView.OpenWindow();

            isButtonPressed = false;
        }

        public void AtozLoginWindowOpen()
        {
            atozLoginView.OpenWindow();
            isButtonPressed = false;
        }

        async UniTask SocialLoginPcs(bool isKickPrev = false)
        {
            var socialLoginAsync = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken, isKickPrev);

            if (socialLoginAsync.IsSuccess)
            {
                await IdentifyAfterLoginAndRegister(socialLoginAsync);
                LoginData.Cloud.loginValue.loginres = socialLoginAsync.Data;
            }
            else
            {
                if (socialLoginAsync.Error.Code == ErrorCode.EUserNotExist)
                {
                    if (string.IsNullOrEmpty( LoginData.Cloud.loginValue.userAutoToken))
                    {
                        var identifySuccess = await Identify();
                        if (identifySuccess == false)
                        {
                            return;
                        }
                    }
                    var registerRes = await Services.Lobby.RegisterAsync(LoginData.Cloud.loginValue.userAccountID,
                        "",
                        "", LoginData.Cloud.loginValue.loginType.ToString(), LoginData.Cloud.loginValue.registerToken);
                    LoginData.Cloud.loginValue.isFirstLogin = true;
                    if (!registerRes.IsSuccess)
                    {
                        RegisterErrorPopupProcess(registerRes);

                    }
                    else
                    {
                        var socialLoginAsync_1 = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                            LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                            LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken, isKickPrev);
                        if (socialLoginAsync_1.IsSuccess)
                        {
                            await IdentifyAfterLoginAndRegister(socialLoginAsync_1);
                            LoginData.Cloud.loginValue.loginres = socialLoginAsync_1.Data;
                        }
                        else
                        {
                            PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SocialLoginFailed].StringToLocal}{socialLoginAsync_1.Error}"));
                      
                        }
                    }
                }
                else if (socialLoginAsync.Error.Code == ErrorCode.EAlreadyLogin)
                {
                    multipleLoginTryView.OpenPopupWithAction(() => { SocialLoginPcs(true).Forget(); }, TotalLoginWindowOpen);
                }
                else
                {
                    LoginErrorPopupProcess(socialLoginAsync);
                }
             
            }
        }

        async UniTask GoogleLoginPcs()
        {
            if (isButtonPressed)
                return;
            try
            {
                bool googleLoginSuccess = await totalLoginView.GoogleLoginPcs();

                if (googleLoginSuccess)
                {
                    var socialLoginAsync = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                        LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                        LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);

                    if (socialLoginAsync.IsSuccess)
                    {
                        await IdentifyAfterLoginAndRegister(socialLoginAsync);
                        LoginData.Cloud.loginValue.loginres = socialLoginAsync.Data;
                    }
                    else
                    {
                        if (socialLoginAsync.Error.Code == ErrorCode.EUserNotExist)
                        {
                            if (string.IsNullOrEmpty( LoginData.Cloud.loginValue.userAutoToken))
                            {
                                var identifySuccess = await Identify();
                                if (identifySuccess == false)
                                {
                                    return;
                                }
                            }


                            var registerRes = await Services.Lobby.RegisterAsync(LoginData.Cloud.loginValue.userAccountID,
                                "",
                                "", LoginData.Cloud.loginValue.loginType.ToString(), LoginData.Cloud.loginValue.registerToken);
                            LoginData.Cloud.loginValue.isFirstLogin = true;
                            if (!registerRes.IsSuccess)
                            {
                                RegisterErrorPopupProcess(registerRes);
                            }
                            else
                            {
                                var socialLoginAsync_1 = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                                    LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                                    LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);
                                if (socialLoginAsync_1.IsSuccess)
                                {
                                    await IdentifyAfterLoginAndRegister(socialLoginAsync_1);
                                    LoginData.Cloud.loginValue.loginres = socialLoginAsync_1.Data;
                                }
                                else
                                {
                                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SocialLoginFailed].StringToLocal}{socialLoginAsync_1.Error}"));
                                }
                            }
                        }
                        else if (socialLoginAsync.Error.Code == ErrorCode.EAlreadyLogin)
                        {
                            multipleLoginTryView.OpenPopupWithAction(() => { SocialLoginPcs(true).Forget(); }, TotalLoginWindowOpen);
                        }
                        else
                        {
                            LoginErrorPopupProcess(socialLoginAsync);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
            }

            isButtonPressed = false;
        }


        async UniTask AppleLoginPcs()
        {
            if (isButtonPressed)
                return;
            try
            {
                bool appleLoginSuccess = await totalLoginView.AppleLoginPcs();

                if (appleLoginSuccess)
                {
                    var socialLoginAsync = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                        LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                        LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);

                    if (socialLoginAsync.IsSuccess)
                    {
                        await IdentifyAfterLoginAndRegister(socialLoginAsync);
                        LoginData.Cloud.loginValue.loginres = socialLoginAsync.Data;
                    }
                    else
                    {
                        if (socialLoginAsync.Error.Code == ErrorCode.EUserNotExist)
                        {
                            if (string.IsNullOrEmpty( LoginData.Cloud.loginValue.userAutoToken))
                            {
                                var identifySuccess = await Identify();
                                if (identifySuccess == false)
                                {
                                    return;
                                }
                            }

                            var registerRes = await Services.Lobby.RegisterAsync(LoginData.Cloud.loginValue.userAccountID,
                                "",
                                "", LoginData.Cloud.loginValue.loginType.ToString(), LoginData.Cloud.loginValue.registerToken);
                            LoginData.Cloud.loginValue.isFirstLogin = true;
                            if (!registerRes.IsSuccess)
                            {
                                RegisterErrorPopupProcess(registerRes);
                            }
                            else
                            {
                                var socialLoginAsync_1 = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                                    LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                                    LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);
                                if (socialLoginAsync_1.IsSuccess)
                                {
                                    await IdentifyAfterLoginAndRegister(socialLoginAsync_1);
                                    LoginData.Cloud.loginValue.loginres = socialLoginAsync_1.Data;
                                }
                                else
                                {
                                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SocialLoginFailed].StringToLocal}{socialLoginAsync_1.Error}"));
                                }
                            }
                        }
                        else if (socialLoginAsync.Error.Code == ErrorCode.EAlreadyLogin)
                        {
                            multipleLoginTryView.OpenPopupWithAction(() => { SocialLoginPcs(true).Forget(); }, TotalLoginWindowOpen);
                        }
                        else
                        {
                            LoginErrorPopupProcess(socialLoginAsync);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
            }

            isButtonPressed = false;
        }

        async UniTask NaverLoginPcs()
        {
            if (isButtonPressed)
                return;
            try
            {
                bool isnaverSuccess = await totalLoginView.NaverLoginPcs();
                if (isnaverSuccess)
                {
                    var socialLoginAsync = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                        LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                        LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);
                    if (socialLoginAsync.IsSuccess)
                    {
                        await IdentifyAfterLoginAndRegister(socialLoginAsync);
                        LoginData.Cloud.loginValue.loginres = socialLoginAsync.Data;
                    }
                    else
                    {
                        if (socialLoginAsync.Error.Code == ErrorCode.EUserNotExist)
                        {
                            if (string.IsNullOrEmpty( LoginData.Cloud.loginValue.userAutoToken))
                            {
                                var identifySuccess = await Identify();
                                if (identifySuccess == false)
                                {
                                    return;
                                }
                            }

                            var registerRes = await Services.Lobby.RegisterAsync(LoginData.Cloud.loginValue.userAccountID,
                                "",
                                "", LoginData.Cloud.loginValue.loginType.ToString(), LoginData.Cloud.loginValue.registerToken);
                            LoginData.Cloud.loginValue.isFirstLogin = true;
                            if (!registerRes.IsSuccess)
                            {
                                RegisterErrorPopupProcess(registerRes);
                            }
                            else
                            {
                                var socialLoginAsync_1 = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                                    LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                                    LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);
                                if (socialLoginAsync_1.IsSuccess)
                                {
                                    await IdentifyAfterLoginAndRegister(socialLoginAsync_1);
                                    LoginData.Cloud.loginValue.loginres = socialLoginAsync_1.Data;
                                }
                                else
                                {
                                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SocialLoginFailed].StringToLocal}{socialLoginAsync_1.Error}"));
                                }
                            }
                        }
                        else if (socialLoginAsync.Error.Code == ErrorCode.EAlreadyLogin)
                        {
                            multipleLoginTryView.OpenPopupWithAction(() => { SocialLoginPcs(true).Forget(); }, TotalLoginWindowOpen);
                        }
                        else
                        {
                            LoginErrorPopupProcess(socialLoginAsync);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
            }

            isButtonPressed = false;
        }

        async UniTask KakaoLoginPcs()
        {
            if (isButtonPressed)
                return;
            try
            {
                bool isKakaoSuccess = await totalLoginView.KakaoLoginPcs();
                if (isKakaoSuccess)
                {
                    var socialLoginAsync = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                        LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                        LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);

                    if (socialLoginAsync.IsSuccess)
                    {
                        await IdentifyAfterLoginAndRegister(socialLoginAsync);
                        LoginData.Cloud.loginValue.loginres = socialLoginAsync.Data;
                    }
                    else
                    {
                        if (socialLoginAsync.Error.Code == ErrorCode.EUserNotExist)
                        {
                            if (string.IsNullOrEmpty( LoginData.Cloud.loginValue.userAutoToken))
                            {
                                var identifySuccess = await Identify();
                                if (identifySuccess == false)
                                {
                                    return;
                                }
                            }

                            var registerRes = await Services.Lobby.RegisterAsync(LoginData.Cloud.loginValue.userAccountID,
                                "",
                                "", LoginData.Cloud.loginValue.loginType.ToString(), LoginData.Cloud.loginValue.registerToken);
                            LoginData.Cloud.loginValue.isFirstLogin = true;
                            if (!registerRes.IsSuccess)
                            {
                                RegisterErrorPopupProcess(registerRes);
                            }
                            else
                            {
                                var socialLoginAsync_1 = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
                                    LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
                                    LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);
                                if (socialLoginAsync_1.IsSuccess)
                                {
                                    await IdentifyAfterLoginAndRegister(socialLoginAsync_1);
                                    LoginData.Cloud.loginValue.loginres = socialLoginAsync_1.Data;
                                }
                                else
                                {
                                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SocialLoginFailed].StringToLocal}{socialLoginAsync_1.Error}"));
                                }
                            }
                        }
                        else if (socialLoginAsync.Error.Code == ErrorCode.EAlreadyLogin)
                        {
                            multipleLoginTryView.OpenPopupWithAction(() => { SocialLoginPcs(true).Forget(); }, TotalLoginWindowOpen);
                        }
                        else
                        {
                            LoginErrorPopupProcess(socialLoginAsync);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
            }

            isButtonPressed = false;
        }

        async UniTask SimpleLoginPcs(LoginCloudData.UserSavedInfo userinfo,lobby.User serverUserinfo, bool multipleLogin = false)
        {
            if (isButtonPressed)
                return;
            try
            {
                isButtonPressed = true;
                
                LoginData.Cloud.loginValue.userAutoToken = userinfo.userLoginToken;
                var loginRes = await autoLoginView.AutoLoginPcs(userinfo, serverUserinfo,multipleLogin);

                if (loginRes.IsSuccess)
                {
                    LoginData.Cloud.loginValue.loginres = loginRes.Data;
                    if (LoginData.Cloud.loginValue.uidList.list.Any(o=>o.uid==userinfo.uid))
                    {
                        var data=LoginData.Cloud.loginValue.uidList.list.FirstOrDefault(x => x.uid == userinfo.uid);
                        data.userLoginToken = loginRes.Data.Token;
                    }
                    await IdentifyAfterLoginAndRegister(loginRes);
                }
                else
                {
                    isButtonPressed = false;
                    if (loginRes.Error.Code == ErrorCode.EAlreadyLogin)
                    {
                        multipleLoginTryView.OpenPopupWithAction(() => { SimpleLoginPcs(userinfo,serverUserinfo, true).Forget(); }, TotalLoginWindowOpen);
                    }
                    else
                    {
                        LoginErrorPopupProcess(loginRes);
                    }

                    Debug.LogError($"{userinfo.uid}이 uid로 로그인 실패");
                }

                Extension.eLog("atoz login success", Color.green);
                
               
                
                await UniTask.Yield();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
            }

            isButtonPressed = false;
        }

        async UniTask AtozLoginPcs(bool multipleLogin = false)
        {
            if (isButtonPressed)
                return;
           // try
            {
                isButtonPressed = true;
                var loginRes = await atozLoginView.AtozLoginPcs(multipleLogin);
                if (loginRes == null)
                {
                    return;
                }

                if (loginRes.IsSuccess)
                {
                    await IdentifyAfterLoginAndRegister(loginRes);

                    LoginData.Cloud.loginValue.loginres = loginRes.Data;
                    atozLoginView.gameObject.SetActive(false);
                    Extension.eLog("atoz login success", Color.green);
                    await UniTask.Yield();
                }
                else
                {
                    isButtonPressed = false;
                   
                    if (loginRes.Error.Code == ErrorCode.EAlreadyLogin)
                    {
                        multipleLoginTryView.OpenPopupWithAction(() => AtozLoginPcs(true).Forget(), TotalLoginWindowOpen);
                    }
                    else
                    {
                        LoginErrorPopupProcess(loginRes);
                    }

                    
                }
            }
            // catch (Exception e)
            // {
            //     Debug.LogError(e.Message);
            //     LoginFailProcess();
            // }

            isButtonPressed = false;
        }

        async UniTask FindUserIdProgress()
        {
            if (isButtonPressed)
                return;

            //mid를 이 기기에서 받아서 로그인 한적이 없음 (첫 시작)
            bool identifySuccess = await Identify();

            if (identifySuccess)
            {
                await atozFindUserIdView.FindUserIdProgress(ChangeUserPw);
                atozFindUserIdView.OpenWindow();
            }
            else
            {
                atozLoginView.OpenWindow();
            }
            
        }

        private User currentUserinfo;

        private void ChangeUserPw(lobby.UserWithToken currentSelectedUserinfo)
        {
            changePwdView.OpenChangeUserPwView(currentSelectedUserinfo);
        }

        private async UniTask ChangeUserPwProccess()
        {
            if (isButtonPressed)
                return;

            isButtonPressed = true;
            bool isSuccess = await changePwdView.ChangeUserPwProccess();
            if (isSuccess)
            {
                PopupManager.Instance.Open<PopupChangePwConfirm>(p => p.SetCloseCallback(() =>
                {
                    SceneLoadResources.ClosePopup();
                    SceneLoadResources.ClosePopup();    
                }));

                //AtozLoginWindowOpen();    
            }

            isButtonPressed = false;
        }

        #region Register

        async UniTask OpenAtozRegisterWindow()
        {
            if (isButtonPressed)
                return;

            bool  identifySuccess = await Identify();

            if (identifySuccess)
            {
                atozRegisterView.OpenWindow();
            }
            else
            {
                atozLoginView.OpenWindow();
            }

            isButtonPressed = false;
        }

        async UniTask AtozRegister()
        {
            if (isButtonPressed)
                return;
            try
            {
                isButtonPressed = true;
                bool isSuccess = await atozRegisterView.AtozRegister();
                if (!isSuccess)
                {
                    isButtonPressed = false;
                    return;
                }

                string id = LoginData.Cloud.loginValue.userAccountID;
                string pwd = LoginData.Cloud.loginValue.userAccountPw;
                LoginData.Cloud.loginValue.loginType = LoginType.ATOZ;
                LoginData.Cloud.loginValue.isFirstLogin = true;
                var loginRes = await Services.Lobby.LoginAsync(id, pwd);
                if (loginRes.IsSuccess)
                {
                    LoginData.Cloud.loginValue.userAccountID = id;
                    LoginData.Cloud.loginValue.userAccountPw = pwd;
                    LoginData.Cloud.loginValue.loginType = LoginType.ATOZ;
                    LoginData.Cloud.loginValue.UID = loginRes.Data.Uid;
                    LoginData.Cloud.loginValue.userAutoToken = loginRes.Data.Token;
                    LoginData.Cloud.loginValue.loginres = loginRes.Data;

                    
                    await IdentifyAfterLoginAndRegister(loginRes);
                    Extension.eLog("atoz login success", Color.green);
                }
                else
                {
                    
                   if (loginRes.Error.Code == ErrorCode.EAlreadyLogin)
                    {
                        multipleLoginTryView.OpenPopupWithAction(() => AtozLoginPcs(true).Forget(), TotalLoginWindowOpen);
                    }
                    else
                    {
                        LoginErrorPopupProcess(loginRes);
                    }
                }


                await UniTask.Yield();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                LoginFailProcess();
            }

            isButtonPressed = false;
        }

        #endregion

        #region Identify after SocialLogin

        public async UniTask IdentifyAfterLoginAndRegister(LobbyClient.PacketResult<LoginRes> loginResData)
        {
            if (!loginResData.IsSuccess)
            {
                if (loginResData.Error.Code == ErrorCode.ENeedUpdate)
                {
                    PopupManager.Instance.Open<PopupVersionUpdate>(popup => { popup.SetWindow(false); });
                }
                else if (loginResData.Error.Code == ErrorCode.EMaintaining)
                {
                    PopupManager.Instance.Open<PopupServerMaintenance>(popup => { popup.SetMaintenanceAtLogin(loginResData.Error.Detail); });
                }
            }
            if (loginResData.IsSuccess == false)
                return;

            if (string.IsNullOrEmpty(loginResData.Data.LatestVersion) == false)
            {
               await PopupManager.Instance.OpenAsync<PopupVersionUpdate>(popup => { popup.SetWindow(true); });
            }

            if (loginResData.Data.Maintenance != null)
            {
                PopupManager.Instance.Open<PopupServerMaintenance>(popup=>popup.SetMaintenanceTime(loginResData.Data.Maintenance));
                return;
            }
            
            if (LoginData.Cloud.loginValue.uidList.list.Any(o=>o.uid==loginResData.Data.Uid)==false)
            {
                LoginCloudData.UserSavedInfo userinfo = new LoginCloudData.UserSavedInfo()
                {
                    uid = loginResData.Data.Uid,userLoginToken = loginResData.Data.Token,accountID = LoginData.Cloud.loginValue.userAccountID,userNickName = loginResData.Data.Nick
                    ,logintype = LoginData.Cloud.loginValue.loginType
                };
                LoginData.Cloud.loginValue.uidList.list.Add(userinfo);
            }

            if (loginResData.Data.IsIdentityVerify == 0)
            {
                bool identifySuccess = false;
                if (string.IsNullOrEmpty( LoginData.Cloud.loginValue.userAutoToken))
                {
                    identifySuccess = await Identify();
                }
                else
                {
                    identifySuccess = true;
                }

                if (identifySuccess == false)
                {
                    //logout후 다시 본인인증 시도 바람
                    return;
                }

                if (LoginData.Cloud.loginValue.loginType == LoginType.ATOZ)
                {
                }
                else
                {
                    LoginData.Cloud.loginValue.UID = loginResData.Data.Uid;
                    LoginData.Cloud.loginValue.userAutoToken = loginResData.Data.Token;
                }

                SceneLoadResources.callbackAfterNewLogin?.Invoke();
            }
            else
            {
                LoginData.Cloud.loginValue.isFirstLogin = false;
                if (LoginData.Cloud.loginValue.loginType == LoginType.ATOZ)
                {
                }
                else
                {
                    LoginData.Cloud.loginValue.UID = loginResData.Data.Uid;
                    LoginData.Cloud.loginValue.userAutoToken = loginResData.Data.Token;
                }
                
                SceneLoadResources.callbackAfterNewLogin?.Invoke();
            }
            
            #region legacy code

            //legacy
            //본인인증 전에 가입이력있는지 확인하고 이미 존재하면 그냥 로그인으로 넘김
            // var existUserInfo = await Services.Lobby.UserExistReqAsync(LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.loginType.ToString());
            // if (existUserInfo.IsSuccess)
            // {
            //     if (existUserInfo.Data.IsExists == 1)
            //     {
            //         try
            //         {
            //             Debug.LogError("[identify] identify success and user exist ");
            //             LoginData.Cloud.loginValue.userMemberId = existUserInfo.Data.Mid;
            //             LoginData.Cloud.loginValue.userIdentifyToken = existUserInfo.Data.Token;
            //             if (LoginData.Cloud.loginValue.loginType == LoginType.ATOZ)
            //             {
            //                 //이미 atozlogin이 되어있는 상태이므로 바로 화면 전환
            //                 SceneLoadResources.callbackAfterNewLogin?.Invoke();
            //             }
            //             else
            //             {
            //                 var socialLoginAsync = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
            //                     LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
            //                     LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);
            //                 if (socialLoginAsync.IsSuccess)
            //                 {
            //                     LoginData.Cloud.loginValue.isFirstLogin = false;
            //                     LoginData.Cloud.loginValue.UID = socialLoginAsync.Data.Uid;
            //                     LoginData.Cloud.loginValue.loginToken = socialLoginAsync.Data.Autotoken;
            //                     LoginData.Cloud.loginValue.jwtToken = socialLoginAsync.Data.Token;
            //                     SceneLoadResources.callbackAfterNewLogin?.Invoke();
            //                 }
            //                 else
            //                 {
            //                     Debug.LogError("[identify] social login fail");
            //                 }
            //
            //                 await UniTask.Yield();
            //             }
            //         }
            //         catch (Exception e)
            //         {
            //             Console.WriteLine(e);
            //             LoginFailProcess();
            //             throw;
            //         }
            //     }
            //     else //본인인증을 진행하도록 하여 이 유저가 본인인증 확인 됐는지 체크하도록 함
            //     {
            //         Debug.LogError("[identify] new auth process start  ");
            //         try
            //         {
            //             bool isAlreadyIdentified = LoginData.Cloud.loginValue.isAlreadyIdentified;
            //             bool identifySuccess = false;
            //             if (isAlreadyIdentified == false)
            //             {
            //                 if (LoginData.Cloud.loginValue.userMemberId < 0)
            //                 {
            //                     identifySuccess = await Identify();
            //                 }
            //                 else
            //                 {
            //                     identifySuccess = true;
            //                 }
            //             }
            //
            //             if (identifySuccess == false)
            //                 return;
            //
            //             //본인인증 후 같은 mid로 가입이력이 있고 추가계정 생성할 경우
            //             if (LoginData.Cloud.loginValue.loginType == LoginType.ATOZ)
            //             {
            //                 await Services.Lobby.RegisterAsync(LoginData.Cloud.loginValue.userAccountID,
            //                     LoginData.Cloud.loginValue.userAccountPw,
            //                     "", "ATOZ", LoginData.Cloud.loginValue.userMemberId);
            //
            //                 LoginData.Cloud.loginValue.isFirstLogin = true;
            //             }
            //             else
            //             {
            //                 Debug.LogError("[identify] processing_0");
            //                 var registerRes = await Services.Lobby.RegisterAsync(LoginData.Cloud.loginValue.userAccountID,
            //                     "",
            //                     "", LoginData.Cloud.loginValue.loginType.ToString(), LoginData.Cloud.loginValue.userMemberId);
            //                 if (registerRes.IsSuccess == false)
            //                 {
            //                     Debug.LogError("[register] social register fail");
            //                     return;
            //                 }
            //
            //                 Debug.LogError("[identify] processing_1");
            //                 Debug.LogError(
            //                     $"{LoginData.Cloud.loginValue.loginType}//{LoginData.Cloud.loginValue.userAccountID} //{LoginData.Cloud.loginValue.userSocialEmail}//{LoginData.Cloud.loginValue.userSocialToken}//" +
            //                     $"{LoginData.Cloud.loginValue.accessToken}");
            //                 var loginAsync = await Services.Lobby.SocialLoginAsync(LoginData.Cloud.loginValue.loginType.ToString(),
            //                     LoginData.Cloud.loginValue.userAccountID, LoginData.Cloud.loginValue.userSocialEmail,
            //                     LoginData.Cloud.loginValue.userSocialToken, LoginData.Cloud.loginValue.accessToken);
            //                 if (loginAsync.IsSuccess == false)
            //                 {
            //                     Debug.LogError("[identify] social login fail");
            //                     return;
            //                 }
            //
            //                 LoginData.Cloud.loginValue.UID = loginAsync.Data.Uid;
            //                 LoginData.Cloud.loginValue.loginToken = loginAsync.Data.Autotoken;
            //                 LoginData.Cloud.loginValue.jwtToken = loginAsync.Data.Token;
            //
            //                 Debug.LogError("[identify] processing_2");
            //                 await UniTask.Yield();
            //                 Debug.LogError($"{LoginData.Cloud.loginValue.loginType} register and login success");
            //
            //                 LoginData.Cloud.loginValue.isFirstLogin = true;
            //                 SceneLoadResources.callbackAfterNewLogin?.Invoke();
            //             }
            //         }
            //         catch (Exception e)
            //         {
            //             Console.WriteLine(e);
            //             RegisterFailProcess();
            //             throw;
            //         }
            //     }
            // }
            // else
            // {
            //     Debug.LogError("[identify] user exist check fail");
            // }
            //

            #endregion
        }

        #endregion


#if UNITY_EDITOR
        public async UniTask EditorLoginProccess(bool multipleLogin = false)
        {
            string id = editorUserId;
            string pwd = editorUserPw;
            LoginData.Cloud.loginValue.loginType = LoginType.ATOZ;
            try
            {
                var loginRes = await Services.Lobby.LoginAsync(id, pwd, multipleLogin);

                if (loginRes.IsSuccess)
                {
                    LoginData.Cloud.loginValue.userAccountID = id;
                    LoginData.Cloud.loginValue.userAccountPw = pwd;
                    LoginData.Cloud.loginValue.loginType = LoginType.ATOZ;
                    LoginData.Cloud.loginValue.UID = loginRes.Data.Uid;
                    LoginData.Cloud.loginValue.userAutoToken = loginRes.Data.Token;
                    LoginData.Cloud.loginValue.loginres = loginRes.Data;

                         
          

                    await IdentifyAfterLoginAndRegister(loginRes);
                }
                else
                {
                    if (loginRes.Error.Code == ErrorCode.EAlreadyLogin)
                    {
                        multipleLoginTryView.OpenPopupWithAction(() => EditorLoginProccess(true).Forget(), TotalLoginWindowOpen);
                    }
                    else
                    {
                        LoginErrorPopupProcess(loginRes);
                    }
                }
                // var existUserInfo = await Services.Lobby.UserExistReqAsync(LoginData.Cloud.loginValue.userAccountID,
                //     LoginData.Cloud.loginValue.loginType.ToString());

                // if (existUserInfo.Data.IsExists == 1)
                // {
                //     LoginData.Cloud.loginValue.userMemberId = existUserInfo.Data.Mid;
                //     LoginData.Cloud.loginValue.userIdentifyToken = existUserInfo.Data.Token;
                // }

                Extension.eLog("atoz login success(For Editor)", Color.cyan);
                await UniTask.Yield();
            }
            catch (Exception e)
            {
                LoginFailProcess();
                Debug.LogError(e.Message);
            }
        }
#endif
        public async UniTask<LobbyClient.PacketResult<LoginRes>> ReLoginProccess()
        {
            string id = LoginData.Cloud.loginValue.userAccountID;
            
            LobbyClient.PacketResult<LoginRes> loginResPacket;
            Extension.eLog($"내 토큰값:{ LoginData.Cloud.loginValue.userAutoToken }", Color.cyan);
            //loginResPacket = await Services.Lobby.AutoLoginAsync(autoLoginToken);
            if (LoginData.Cloud.loginValue.loginType == LoginType.ATOZ)
            {
                loginResPacket = await Services.Lobby.AutoLoginAsync( LoginData.Cloud.loginValue.userAutoToken );
            }
            else
            {
                //loginResPacket.Data.Position;
                id = LoginData.Cloud.loginValue.userAccountID;
                string loginType = LoginData.Cloud.loginValue.loginType.ToString();
                string socialEmail = LoginData.Cloud.loginValue.userSocialEmail;
                string socialToken = LoginData.Cloud.loginValue.userSocialToken;
                string accessToken = LoginData.Cloud.loginValue.accessToken;
                loginResPacket = await Services.Lobby.SocialLoginAsync(loginType, id, socialEmail, socialToken, accessToken);
            }

            if (loginResPacket.IsSuccess)
            {
                LoginData.Cloud.loginValue.UID = loginResPacket.Data.Uid;
                LoginData.Cloud.loginValue.userAutoToken = loginResPacket.Data.Token;
                LoginData.Cloud.loginValue.loginres = loginResPacket.Data;
                
                if (LoginData.Cloud.loginValue.uidList.list.Any(o=>o.uid==loginResPacket.Data.Uid))
                {
                    var data=LoginData.Cloud.loginValue.uidList.list.FirstOrDefault(x => x.uid ==loginResPacket.Data.Uid);
                    data.userLoginToken = loginResPacket.Data.Token;
                }
                //await IdentifyAfterLoginAndRegister(loginResPacket);
            }

            if (!loginResPacket.IsSuccess)
            {
                if (loginResPacket.Error.Code == ErrorCode.EAlreadyLogin)
                {
                    if (LoginData.Cloud.loginValue.loginType == LoginType.ATOZ)
                    {
                        multipleLoginTryView.OpenPopupWithAction(() => AtozSimpleLoginPcs(LoginData.Cloud.loginValue.UID,true).Forget(), TotalLoginWindowOpen);
                    }
                    else
                    {
                        multipleLoginTryView.OpenPopupWithAction(() => { SocialLoginPcs(true).Forget(); }, TotalLoginWindowOpen);
                    }
                }
                else
                {
                    LoginErrorPopupProcess(loginResPacket);
                }
       
            }

            await UniTask.Yield();


            return loginResPacket;
        }

        private void LoginErrorPopupProcess(LobbyClient.PacketResult<LoginRes> loginResPacket)
        {
            if (loginResPacket.Error.Code == ErrorCode.EUserNotExist)
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InvalidIdOrPassword].StringToLocal, true,true));
                //PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"로그인에 실패하였습니다. 고객센터로 문의해주세요."));
            }
            else if (loginResPacket.Error.Code == ErrorCode.EInvalidToken)
            {
                PopupManager.Instance.Open<PopupTokenExpired>(popup=>popup.SetInvalidConfirmBtn(()=>SetInvalidTokenAndGotoLogin().Forget()));
            }
            else if (loginResPacket.Error.Code == ErrorCode.EBanPermanent||loginResPacket.Error.Code == ErrorCode.EBanTemporary)
            {
                accountBanWarningWindow.OpenWindow(loginResPacket.Error);
            }
            else if (loginResPacket.Error.Code == ErrorCode.ENeedUpdate)
            {
                PopupManager.Instance.Open<PopupVersionUpdate>(popup => { popup.SetWindow(false); });
            }
            else if (loginResPacket.Error.Code == ErrorCode.EMaintaining)
            {
                PopupManager.Instance.Open<PopupServerMaintenance>(popup => { popup.SetMaintenanceAtLogin(loginResPacket.Error.Detail); });
            }
            else if (loginResPacket.Error.Code == ErrorCode.ERegister5AccountLimit)
            {
                PopupManager.Instance.Open<PopupAccountCreateLimitWarning>();
            }
            else if (loginResPacket.Error.Code == ErrorCode.EInvalidUid)
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InvalidIdOrPassword].StringToLocal, true,true));
            }
            else
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.LoginFailed].StringToLocal));
            }

            Debug.LogError(loginResPacket.Error);
        }

        private async UniTaskVoid SetInvalidTokenAndGotoLogin()
        {
            var userSavedInfo=LoginData.Cloud.loginValue.uidList.list.Find(o => o.userLoginToken == LoginData.Cloud.loginValue.userAutoToken);
            if (userSavedInfo != null)
            {
                LoginData.Cloud.loginValue.uidList.list.Remove(userSavedInfo);
            }
            LoginData.Cloud.loginValue.userAutoToken = "";
            
            SceneLoadResources.CloseAllPopup();
        }

        private void RegisterErrorPopupProcess(LobbyClient.PacketResult<RegisterRes> registerRes)
        {
            if (registerRes.Error.Code == ErrorCode.ERegister5AccountLimit)
            {
                PopupManager.Instance.Open<PopupAccountCreateLimitWarning>();
            }
            else
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.LoginFailed].StringToLocal));
            }
        }
        
        
        public async UniTask<bool> AtozSimpleLoginPcs(long uid,bool multipleLogin=false)
        {
            var usersInfos = await Services.Lobby.GetUserListInfoAsync(LoginData.Cloud.loginValue.userAutoToken);
            if (usersInfos.IsSuccess == false)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.LoginFailed].StringToLocal));
                return false;
            }

            var targetUser = usersInfos.Data.Users.FirstOrDefault(o => o.Uid == uid);
            if (targetUser == null)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.LoginFailed].StringToLocal));
                return false;
            }

            string autotoken = LoginData.Cloud.loginValue.userAutoToken;
            
            LobbyClient.PacketResult<LoginRes> loginRes = null;
            try
            {
                Extension.eLog($"내 아이디:{    LoginData.Cloud.loginValue.userAccountID},,내 토큰값:{    autotoken}",Color.cyan);
                loginRes = await Services.Lobby.AutoLoginAsync(autotoken,multipleLogin);
                LoginData.Cloud.loginValue.loginType = LoginType.ATOZ;
                if (loginRes.IsSuccess)
                {
                    LoginData.Cloud.loginValue.userAccountID = targetUser.Id;
                    LoginData.Cloud.loginValue.UID=loginRes.Data.Uid;
                    LoginData.Cloud.loginValue.userAutoToken = loginRes.Data.Token;
                    
                    await IdentifyAfterLoginAndRegister(loginRes);

                    LoginData.Cloud.loginValue.loginres = loginRes.Data;
                    atozLoginView.gameObject.SetActive(false);
                }
                else
                {
                    if (loginRes.Error.Code == ErrorCode.EAlreadyLogin)
                    {
                        if (LoginData.Cloud.loginValue.loginType == LoginType.ATOZ)
                        {
                            multipleLoginTryView.OpenPopupWithAction(() => AtozSimpleLoginPcs(LoginData.Cloud.loginValue.UID,true).Forget(), TotalLoginWindowOpen);
                        }
                        else
                        {
                            multipleLoginTryView.OpenPopupWithAction(() => { SocialLoginPcs(true).Forget(); }, TotalLoginWindowOpen);
                        }
                    }
                    else
                    {
                        LoginErrorPopupProcess(loginRes);
                    }
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


        async UniTask<bool> Identify()
        {
            if (Application.isEditor || IsIdentityVerificationSkipByBuild())
            {
                LoginData.Cloud.loginValue.isAlreadyIdentified = true;
                return true;
            }

            try
            {
                isSuccess = false;
                isIdentifyReceived = false;
                Debug.LogError("identify ready_0");
                OpenIdentifyUrl().Forget();

                Debug.LogError("identify ready_1");
                await UniTask.WaitUntil(() => isSuccess);
                //mid setting 완료
                LoginData.Cloud.loginValue.isAlreadyIdentified = true;

                Debug.LogError("identify success");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                IdentifyFailProcess();
            }

            bool isIdentifySuccess = isIdentifyReceived &&  !string.IsNullOrEmpty( LoginData.Cloud.loginValue.userAutoToken);
            return isIdentifySuccess;
        }

        bool IsIdentityVerificationSkipByBuild()
        {
#if SKIP_IDENTITY_VERIFICATION
            return true;
#else
            return false;
#endif
        }

        public async UniTask<bool> IdentifyForReverify()
        {
            return await Identify();
        }

        #region TestForIpPort

        public void IPPortWindowOpen()
        {
            ipPortWindow.SetActive(true);
        }

        void OnDropdownChange(int index)
        {
            IPInputField.text = CPPlayer.IpPortData.ipportinfos.infos[index].ip;
            PortInputField.text = CPPlayer.IpPortData.ipportinfos.infos[index].port.ToString();
        }

        void InitDropDown()
        {
            CPPlayer.IpPortData = LocalSaveLoader.LoadIPPortData();
            if (CPPlayer.IpPortData.ipportinfos.infos.Count > 0)
            {
                for (int i = 0; i < CPPlayer.IpPortData.ipportinfos.infos.Count; i++)
                {
                    var info = CPPlayer.IpPortData.ipportinfos.infos[i];
                    dropdown.options.Add(new TMP_Dropdown.OptionData(info.ip));
                }

                var infodatas = CPPlayer.IpPortData.ipportinfos.infos;
                IPInputField.text = infodatas[infodatas.Count - 1].ip;
                PortInputField.text = infodatas[infodatas.Count - 1].port.ToString();

                dropdown.value = infodatas.Count - 1;
                dropdown.RefreshShownValue();
            }
            else
            {
                IPInputField.text = "lobby.dev.atozgames.net";
                PortInputField.text = "1111";
            }

            dropdown.onValueChanged.AddListener(OnDropdownChange);
        }

        #endregion

        #region UNIWebview

        ///UNIWEBVIEW SETTING
        private string expectedWV;

        GameObject webViewObject;
        UniWebView webView;


        async UniTask OpenIdentifyUrl()
        {
            if (webView == null)
            {
                if (webViewObject == null)
                    webViewObject = new GameObject("WebViewObject_identify");
                webView = webViewObject.AddComponent<UniWebView>();
                webView.Frame = new Rect(0, 0, Screen.width, Screen.height);
                webView.SetShowToolbar(true);
                webView.SetSupportMultipleWindows(true);
                webView.SetAllowBackForwardNavigationGestures(true);
                webView.AddUrlScheme("uniwebview");

                webView.OnPageStarted += OnPageStarted;
                webView.OnPageFinished += OnPageFinished;
                webView.OnLoadingErrorReceived += OnPageError;
                webView.OnMessageReceived += OnMessageReceived;
                webView.OnMultipleWindowOpened += OnPopupOpened;
                webView.OnMultipleWindowClosed += OnPopupClosed;
                webView.OnShouldClose += (view) =>
                {
                    // 사용자가 네이티브 Back 등으로 닫을 때
                    CleanupWebView();
                    return true;
                };
                webView.SetSupportMultipleWindows(true, true);
                webView.SetAcceptThirdPartyCookies(true);
            }

            var jwtToken = await GetJWT();
            var authUrl = "https://gw.dev.atozgames.net/identity-verification" +
                          $"?wv={jwtToken.token}";

            webView.Load(authUrl);
            webView.Show();
        }

        void OnPopupOpened(UniWebView parent, string windowId)
        {
            Debug.Log($"[UniWebView] popup opened: {windowId}");
            // 팝업도 OnMessageReceived 이벤트는 동일하게 들어옴.
        }

        void OnPopupClosed(UniWebView parent, string windowId)
        {
            Debug.Log($"[UniWebView] popup closed: {windowId}");
        }

        void OnPageStarted(UniWebView view, string url)
        {
            Debug.Log($"Start Page::{url}");
            TryHandleRedirect(url);
        }

        void OnPageFinished(UniWebView view, int statusCode, string url)
        {
            Debug.Log($"Finish Page::{statusCode}//{url}");
            // 팝업도 OnMessageReceived 이벤트는 동일하게 들어옴.
            TryHandleRedirect(url);
        }

        void OnPageError(UniWebView view, int code, string message, UniWebViewNativeResultPayload payload)
        {
            Debug.LogError($"Error Page::{code}//{message}");
            // 팝업도 OnMessageReceived 이벤트는 동일하게 들어옴.
        }

        void TryHandleRedirect(string url)
        {
            Debug.Log($"Redirect !!::{url}");
            // 팝업도 OnMessageReceived 이벤트는 동일하게 들어옴.

            var qs = ParseQuery(url);
            if (qs.TryGetValue("code", out var code)) Debug.Log($"[OAuth] code={code}");
            if (qs.TryGetValue("state", out var state)) Debug.Log($"[OAuth] state={state}");
            if (qs.TryGetValue("wv", out var wv)) Debug.Log($"[OAuth] wv={wv}");
        }


        Dictionary<string, string> ParseQuery(string fullUrl)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var uri = new Uri(fullUrl);
                var q = uri.Query; // "?a=1&b=2"
                if (string.IsNullOrEmpty(q) || q.Length <= 1) return dict;

                foreach (var pair in q.Substring(1).Split('&'))
                {
                    if (string.IsNullOrEmpty(pair)) continue;
                    var kv = pair.Split(new[] { '=' }, 2);
                    var k = Uri.UnescapeDataString(kv[0]);
                    var v = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
                    dict[k] = v;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OAuth] ParseQuery fail: {e.Message}");
            }

            return dict;
        }

        bool isSuccess = false;
        bool isIdentifyReceived = false;

        [Serializable]
        public class AuthRoot
        {
            public string registerToken;
            public string autoToken;
            public string comType;
        }

        [Serializable]
        public class WebPayload
        {
            public string code;
            public AuthRoot auth;
            public string wv;
        }

        void OnMessageReceived(UniWebView view, UniWebViewMessage msg)
        {
            Debug.Log($"[UniWebView] path: {msg.Path}");
            Debug.Log($"[UniWebView] args: {msg.Args}");
            if (msg.Args.Count > 0)
            {
                foreach (var arg in msg.Args)
                {
                    Debug.Log($"[UniWebView] args: {arg}");
                }
            }

            WebPayload webpayload = null;

            switch (msg.Path)
            {
                case "identify":
                    if (msg.Args.TryGetValue("payload", out var identifyjson))
                    {
                        webpayload = JsonUtility.FromJson<WebPayload>(identifyjson);
                    }

                    if (webpayload != null)
                    {
                        if (webpayload.auth != null)
                        {
                            Debug.Log($"[PayLoadData] register-data : {webpayload.auth.registerToken}");
                            Debug.Log($"[PayLoadData] autotoken-data : {webpayload.auth.autoToken}");
                            Debug.Log($"[PayLoadData] auth-comtype : {webpayload.auth.comType}");
                        }
                        else
                        {
                            Debug.Log($"[PayLoadData] is not exist");
                        }

                        Debug.Log($"[PayLoadData] wv : {webpayload.wv}");
                    }

                    //본인인증 완료
                    isIdentifyReceived = true;
                    LoginData.Cloud.loginValue.registerToken = webpayload.auth.registerToken;
                    LoginData.Cloud.loginValue.userAutoToken = webpayload.auth.autoToken;
                    break;
                case "close":
                {
                    Debug.Log($"[UniWebView] close path: {msg.Path}");
                    CloseWithFade();
                    break;
                }

                default:
                    Debug.Log($"[UniWebView] Unknown path: {msg.Path}");
                    break;
            }

            CleanupWebView();
        }

        private void FailAndClose()
        {
            // 실패 처리 UI 필요하면 표시
            CloseWithFade();
        }

        private void CloseWithFade()
        {
            if (webView == null) return;
            webView.Hide(true, UniWebViewTransitionEdge.Bottom, 0.25f, () => { CleanupWebView(); });
        }

        private void CleanupWebView()
        {
            if (webView != null)
            {
                webView.OnMessageReceived -= OnMessageReceived;
                webView.CleanCache();
                Destroy(webView);
                webView = null;

                Debug.Log($"Login All Success!!! destroy webview!!");
            }

            isSuccess = true;
        }

        public class JWTRes
        {
            public string token;
        }

        private const string JWT_URL = "https://gw.dev.atozgames.net/api/ingame-token";

        async UniTask<JWTRes> GetJWT()
        {
            using var req = new UnityWebRequest(JWT_URL, UnityWebRequest.kHttpVerbPOST);
            req.downloadHandler = new DownloadHandlerBuffer();

            await req.SendWebRequest().ToUniTask(cancellationToken: _cts.Token);

            if (req.result != UnityWebRequest.Result.Success ||
                req.responseCode < 200 || req.responseCode >= 300)
                throw new Exception($"HTTP {(int)req.responseCode} : {req.error}\n{req.downloadHandler?.text}");

            var json = req.downloadHandler.text;
            var res = JsonUtility.FromJson<JWTRes>(json);
            return res;
        }

        public class TokenReq
        {
            public string token;
        }

        public class MemberIdReq
        {
            public string comType;
            public TokenReq data;
        }

        public class MemBerIdFullReq
        {
            public MemberIdReq auth;
        }

        public class MemberIdRes
        {
            public bool success;
            public long mid;
        }

        private const string MEMBER_ID_URL = "http://lobby.dev.atozgames.net:3001/accounts/CreateUser";

        async UniTask<MemberIdRes> GetMemberId(object body)
        {
            var loadString = Newtonsoft.Json.JsonConvert.SerializeObject(body);

            Debug.Log($"[Identify Request]:::{loadString}///");
            using var req = new UnityWebRequest(MEMBER_ID_URL, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(loadString));
            req.downloadHandler = new DownloadHandlerBuffer();

            try
            {
                await req.SendWebRequest().ToUniTask(cancellationToken: _cts.Token);

                if (req.result != UnityWebRequest.Result.Success ||
                    req.responseCode < 200 || req.responseCode >= 300)
                {
                    Debug.LogError($"[IDENTIFY] FAIL!!!:{req.error}///");
                    throw new Exception($"HTTP {(int)req.responseCode} : {req.error}\n{req.downloadHandler?.text}");
                }


                var json = req.downloadHandler.text;
                var res = JsonUtility.FromJson<MemberIdRes>(json);
                return res;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw;
            }
        }

        #endregion
    }
}