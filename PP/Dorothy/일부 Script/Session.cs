// Session.cs - Session implementation file
//
// Description      : Session main instance
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/05/27
// Last Update      : 2020/07/01
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Voyager.Unity;
using Voyager.Unity.Solar;
using Voyager.Unity.Network;
using Voyager.Unity.Settings;
using PlayEarth.Unity;
using PlayEarth.Unity.Auth;
using PlayEarth.Unity.Terms;
using PlayEarth.Unity.Purchase;
using PlayEarth.Unity.Friend;
using PlayEarth.Unity.DSLink;
using Dorothy.DataPool;
using Dorothy.UI.Popup;
using UnityEditor;
using Ugo.CSLib;
using Ugo.Unity;

namespace Dorothy
{
    public class ConfigAppInfo
    {
        public string version;
        public string minReqVersion;
        public string installUrl;
    }

    public class ConfigUrl
    {
        public string assetbundles;
        public string api;
        public string review;
        public string terms;
    }

    public class MaintenanceInfo
    {
        public bool enable;
        public string linkUrl;
        public long startDate;
        public long endDate;
    }

    public class RequestData
    {
        public string address;
        public HttpMethod method;
        public IDictionary<string, object> formData;

        public RequestData(string _address, HttpMethod _method, IDictionary<string, object> _formData)
        {
            address = _address;
            method = _method;
            formData = _formData;
        }
    }

    public class RewardCoupon
    {
        public string couponStrForReward;
    }

    public sealed class Session : BaseSystem<Session>
    {
        private State currentState = State.Ready;
        private VoyagerWrapper.VoyagerFastTrackTask loginTask;
        private DateTime pauseTime = DateTime.MaxValue;

        public Action<State> ActionChangeState = delegate { };
        public Action ActionOpen = delegate { };
        public Action ActionClose = delegate { };
        public Action<bool> ActionLogout = delegate { };
        public Action ActionNotification = delegate { };
        public Action ActionMaintenance = delegate { };
        public Action<bool> ActionFacebookReady = delegate { };
        public Action ActionPurchase = delegate { };

        private string rewardCoupon = string.Empty;
        public string RewardCoupon
        {
            get { return rewardCoupon; }
            set { rewardCoupon = value; }
        }

        private RequestData oldRequestData = null;
        public RequestData OldRequestData
        {
            get { return oldRequestData; }
            set { oldRequestData = value; }
        }

        private int reconnectCount;
        public int ReconnectCount
        {
            get { return reconnectCount; }
            set { reconnectCount = value; }
        }

        private bool isDisconnect;
        public bool IsDisconnect
        {
            get { return isDisconnect; }
            set { isDisconnect = value; }
        }

        private bool isGameEnter;
        public bool IsGameEnter
        {
            get { return isGameEnter; }
            set { isGameEnter = value; }
        }

        public enum State
        {
            Ready = 0,
            AssetsDown,
            Login,
            Logout,
            Admin,
            FacebookReady,
            FacebookCancel,
            FacebookFail,
            FacebookLink,
            FacebookSignin,
            Anonymously,
            Progress,
            LoginFail,
            IntroFade,
            InLobby,
            InGame,
            GameToInLobby,
        }

        public enum LoginType
        {
            Auto,
            Retry,
            Guest,
            Facebook,
            GSN
        }

        public LoginType UserLoginType
        {
            get; private set;
        }

        public bool Connected
        {
            get; private set;
        }

        public bool Initialized
        {
            get; private set;
        }

        public State CurrentState
        {
            get
            {
                return currentState;
            }
            set
            {
                Log("The state changed: " + value.ToString());

                currentState = value;
                ActionChangeState(currentState);
            }
        }

        public string Location
        {
            get
            {
                //if (CurrentState == State.InLobby)
                //{
                //	return Lobby.IsVisible ? "InLobby" : "InStrip";
                //}
                //else
                {
                    return CurrentState.ToString();
                }
            }
        }

        public bool LoggedOut
        {
            private set
            {
                if (value)
                {
                    PlayerPrefs.SetInt(PlayerPrefs.LOGOUT, 1);
                    if (ActionLogout != null)
                        ActionLogout(true);
                }
                else
                {
                    PlayerPrefs.DeleteKey(PlayerPrefs.LOGOUT);
                }
            }
            get
            {
                return PlayerPrefs.HasKey(PlayerPrefs.LOGOUT);
            }
        }

        protected override void Awake()
        {
            base.Awake();

            ActionChangeState += OnChangeSessionState;

            // 개발 모드에서 FPS 표시한다.
#if UNITY_DEBUG
            gameObject.AddComponent<FPSDisplay>();
#endif
        }

        private void OnChangeSessionState(State state)
        {
            switch (state)
            {
                case State.FacebookReady:
                    if (ActionFacebookReady != null)
                        ActionFacebookReady(true);
                    break;
                case State.FacebookFail:
                case State.FacebookCancel:
                    if (ActionFacebookReady != null)
                        ActionFacebookReady(false);
                    break;
                case State.FacebookLink:
                case State.FacebookSignin:
                    SceneManager.LoadScene(Constants.SCENE_INTRO);
                    break;
                default:
                    break;
            }
        }

        private void OnLoggerWrite(VLogger.Level level, string message)
        {
            Debug.Log(level + " " + message);
        }
        
        public void SetNtfMessage()
        {
            UgoProxy.RpcTask.OnMessage = (rpcMessageArgs, callback) =>
            {
                Log("received message from server. " + rpcMessageArgs.Method + ", " + rpcMessageArgs.Pars);
                if (rpcMessageArgs.IsNotification)
                {
                    if (rpcMessageArgs.Method.StartsWith("LobbyContents."))
                    {
                        //if (rpcMessageArgs.Method.Contains("content-user.level.up"))
                        //{
                        //    SetNtfData<NtfLevelUpData>
                        //    (
                        //        rpcMessageArgs.Pars,
                        //        (data) =>
                        //        {
                        //            UserInfo.Instance.UpdateLevelUpInfo(data);
                        //        }
                        //    );
                        //}

                        Log("received message is Lobby contents, " + rpcMessageArgs.Method + ", " + rpcMessageArgs.Pars);
                        return;
                    }
                }
                else
                {
                    // Do something and response callback
                    callback(true);
                }
            };

            UgoProxy.RpcTask.OnClosed += (sender, args) =>
            {
                Debug.Log("UGO CLOSE");
                //Debug.LogError("UGO OnClosed: " + args.Code + ", Message: " + args.Reason);
                if (args.Code == 1011)
                {
                    if (args.Reason == "KICK")
                    {
                        IsDisconnect = true;

                        int order = PopupOrderList.Instance.GetOrder(PopupSystem.POPUP_TYPE.POPUP_NetworkDisconnected);
                        PopupDoubleConnection.Open(0f, order).OnOpen((handler) =>
                        {
                            if (PopupSystem.Instance.IsPopupOpened("POPUP_Loading") == true)
                                PopupLoading.CloseSelf();
                        });
                    }
                    else if (args.Reason == "INVALID_SESSIONID")
                    {
                        IsDisconnect = true;

                        int order = PopupOrderList.Instance.GetOrder(PopupSystem.POPUP_TYPE.POPUP_NetworkDisconnected);
                        PopupInvalidSessionId.Open(0f, order).OnOpen((handler) =>
                        {
                            if (PopupSystem.Instance.IsPopupOpened("POPUP_Loading") == true)
                                PopupLoading.CloseSelf();
                        });
                    }
                    else if (args.Reason == "MAINTENANCE")
                    {
                        IsDisconnect = true;

                        int order = PopupOrderList.Instance.GetOrder(PopupSystem.POPUP_TYPE.POPUP_NetworkDisconnected);
                        PopupMaintenance.Open(PopupMaintenance.Type.ALL, () =>
                        {
                            Application.Quit();
                        },
                        0f, order).OnOpen((handler) => 
                        {
                            if (PopupSystem.Instance.IsPopupOpened("POPUP_Loading") == true)
                                PopupLoading.CloseSelf();
                        });
                    }
                }
            };
        }

        private void SetNtfData<T>(object pars, Action<T> callback)
        {
            if (callback != null)
            {
                T data = NtfMessageFromJson<T>(pars);
                callback(data);
            }
        }

        private T NtfMessageFromJson<T>(object pars)
        {
            string json = CommonTools.ToJsonString(pars);
            return CommonTools.FromJsonString<T>(json);
        }

        public void Initialize(Action callback)
        {
            Log("Initialized : " + Initialized);
            if (Initialized)
            {
                if (callback != null)
                    callback();
            }
            else
            {
                SetupEnvironment();

#if UNITY_EDITOR || UNITY_DEBUG
                VLogger.LogLevel = VLogger.Level.DEBUG;
                VLogger.OnWrite += OnLoggerWrite;
                Client.Instance.Config.Log.Level = ULogger.Level.DEBUG;
#else
                VLogger.LogLevel = VLogger.Level.NEVER;
                Client.Instance.Config.Log.Level = ULogger.Level.NEVER;
#endif

                VSdk.Init((exception) =>
                {
                    Log("voyager SDK Init");

                    // 다이나믹 링크 리스너
                    FirebaseDynamicLinkWrapper.SetDynamicLinkListener((url) =>
                    {
                        Log("SetDynamicLinkListener: " + url.OriginalString);

                        Uri uri = new Uri(url.OriginalString);
                        ReceiveFacebookFeed(url.Query.Replace("?code=", ""), null);

                    });

                    FirebaseMessageWrapper.SetMessageReceivedListener((message) =>
                    {
                        Log(string.Format("ReceivedMessage: From: {0}, To: {1}, Type: {2}, Id: {3}", message.From, message.To, message.MessageType, message.MessageId));

                        if (message.Data.ContainsKey(FirebaseMessageWrapper.JSON_DATA_FOR_MESSAGE_OPEN))
                        {
                            //message.Data[FirebaseMessageWrapper.JSON_DATA_FOR_MESSAGE_OPEN]는 JSON이다.
                            //해당 json에 couponStrForReward이 쿠폰 문자열이다.
                            Log(string.Format("ReceivedData: jsonDataForMessageOpen: {0}", message.Data[FirebaseMessageWrapper.JSON_DATA_FOR_MESSAGE_OPEN]));

                            RewardCoupon coupon = CommonTools.FromJsonString<RewardCoupon>(message.Data[FirebaseMessageWrapper.JSON_DATA_FOR_MESSAGE_OPEN]);

                            Log("coupon : " + coupon.couponStrForReward);

                            rewardCoupon = coupon.couponStrForReward;
                        }
                    });

                    Initialized = true;
                    if (callback != null)
                        callback();
                });
            }
        }

        /// <summary>
        /// 계정 정보 삭제 - 에디터에서 캐싱 정보가 남아 있는 현상 때문에 한 번 정보 클리어를 해 주어야 합니다.
        /// </summary>
        public void ResetAuth()
        {
            Log("Auth.ResetAuth");
            Auth.ResetAuth();
        }

        public void Open(Session.LoginType type, long gsn = 0)
        {
            Log();

            Session.Instance.Login(type, gsn, (exception, result) =>
            {
                SetMaintenance();

                if (exception == null)
                {
                    Session.Instance.ReconnectCount = 0;

                    SetConfig();

#if !UNITY_EDITOR
                    string strAppVersion = Application.version;
                    string strMinReqVersion = ConfigInfo.Instance.Find("minReqVersion"); //Dorothy.PlayerPrefs.GetString(Dorothy.PlayerPrefs.MIN_REQ_VERSION, "");
                    if (string.IsNullOrEmpty(strMinReqVersion) == false)
                    {
                        //if (strAppVersion.CompareTo(ConfigInfo.Instance.Find("minReqVersion")) != 0)
                        if (VersionCompare(strMinReqVersion, strAppVersion) == false)
                        {
                            // 버전이 맞지 않으므로 Update 표시
                            PopupUpdate.Open();
                            return;
                        }
                    }

                    //Dorothy.PlayerPrefs.SetString(Dorothy.PlayerPrefs.MIN_REQ_VERSION, ConfigInfo.Instance.Find("minReqVersion"));
#endif
                    LoggedOut = false;
                    SetNtfMessage();
                    SessionLobby.instance.OnUserBaseInfo(result);

                    ActionOpen();
                    UserTracking.Instance.LogSuccessLogin();

                    UserTracking.Instance.LogFirstOpen();
                    //CurrentState = State.InLobby;
                    CurrentState = State.IntroFade;
                }
                else
                {
                    OnLoginFail(exception);
                }
            });
        }

        public bool VersionCompare(string minV, string appV)
        {
            bool result = false;

            string[] split_minV;
            string[] split_appV;
            int minV_1, minV_2, minV_3, appV_1, appV_2, appV_3;

            split_minV = minV.Split('.');
            split_appV = appV.Split('.');

            if (split_minV.Length > 0)
                minV_1 = int.Parse(split_minV[0]);
            else
                minV_1 = 0;

            if (split_minV.Length > 1)
                minV_2 = int.Parse(split_minV[1]);
            else
                minV_2 = 0;

            if (split_minV.Length > 2)
                minV_3 = int.Parse(split_minV[2]);
            else
                minV_3 = 0;

            if (split_appV.Length > 0)
                appV_1 = int.Parse(split_appV[0]);
            else
                appV_1 = 0;

            if (split_appV.Length > 1)
                appV_2 = int.Parse(split_appV[1]);
            else
                appV_2 = 0;

            if (split_appV.Length > 2)
                appV_3 = int.Parse(split_appV[2]);
            else
                appV_3 = 0;

            if (minV_1 > appV_1)
            {
                return false;
            }
            else
            {
                if (minV_2 > appV_2)
                {
                    return false;
                }
                else
                {
                    if (minV_2 == appV_2)
                    {
                        if (appV_3 >= minV_3)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return result;
        }

        public void Login(LoginType type, long GSN = 0, Action<Exception, LoginResult> callback = null)
        {
            Log("Login Type : " + type);
            UserLoginType = type;
            CurrentState = State.Progress;

            UserTracking.Instance.LogTryLogin();

            switch (type)
            {
                case LoginType.Auto:
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                    ResetAuth();
#endif
                    Login(new Auth.AutoLoginTask(), callback);
                    break;

                case LoginType.Retry:
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                    ResetAuth();
#endif
                    Login(new Auth.AutoLoginTask(), callback);
                    break;

                case LoginType.Facebook:
                    Login(new Auth.SigninWithExistingFacebookTask(), callback);
                    break;

                case LoginType.Guest:
                    Login(new Auth.SigninAnonymouslyTask(), callback);
                    break;

                case LoginType.GSN:
                    UserInfo.Instance.GSN = GSN;
                    Login(new Auth.SigninWithGsnTask(GSN), callback);
                    break;
            }
        }

        private void Login(VoyagerTask<LoginResult> task, Action<Exception, LoginResult> callback)
        {
             Log("*********** LOGIN TASK *************");

            //loginTask = VoyagerWrapper.VoyagerFastTrackTask.Execute(3, true, task, 1);
            loginTask = VoyagerWrapper.VoyagerFastTrackTask.Execute(3, !Environment.Instance.BypassMaintenance, task, 1);
            //loginTask.FirebaseSuccessListener = () =>
            //{
            //    Log("Firebase SET");
            //};

            loginTask.ConfigSuccessListener = (result) =>
            {
                Log("Config SET: " + result.ToString());

                //SetupConfig(result);
            };

            loginTask.AuthSuccessListener = (result) =>
            {
                Log("Auth SET: " + result.ToString());

                //SetupAuth(result);
            };

            loginTask.ConnectSuccessListener = (connect) =>
            {
                Log("Connect Success");
            };

            loginTask.SetCallback((exception, result) =>
            {
                Log("Login SET: " + ((exception != null) ? exception.ToString() : "success"));

                if (callback != null)
                    callback(exception, loginTask.loginResult);
            });
        }

        private void OnLoginFail(Exception exception)
        {
            Log(exception.ToString());

            CurrentState = State.LoginFail;

            if (loginTask != null)
            	loginTask.Cancel();

            if (exception.GetType() == typeof(PeMaintenanceException))
            {
                Log("Maintenance");

                if (ActionMaintenance != null)
                    ActionMaintenance();

                return;
            }

            if (exception.GetType() == typeof(NotImplementedException) ||
                exception.GetType() == typeof(NetworkException) ||
                exception.GetType() == typeof(PeLoginRequiredException))
            {
                if (Session.Instance.ReconnectCount >= 3)
                {
                    int order = PopupOrderList.Instance.GetOrder(PopupSystem.POPUP_TYPE.POPUP_NetworkDisconnected);
                    PopupNetworkDisconnected.Open(0f, order).OnOpen((handler) =>
                    {
                        if (PopupSystem.Instance.IsPopupOpened("POPUP_Loading") == true)
                            PopupLoading.CloseSelf();
                    });
                }
                else
                {
                    int order = PopupOrderList.Instance.GetOrder(PopupSystem.POPUP_TYPE.POPUP_Default);
                    PopupMessage.Open(LocalizationSystem.Instance.Localize("NETWORKDISCONNECTION.Popup.Title"),
                                        LocalizationSystem.Instance.Localize("NETWORKDISCONNECTION.Popup.Description"), 0f, PopupMessage.Type.Reconnect, (cmd) =>
                                        {
                                            if (cmd == PopupMessage.Command.Ok)
                                            {
                                                ++Session.Instance.ReconnectCount;

                                                Open(UserLoginType, UserInfo.Instance.GSN);
                                            }
                                        });
                }

                LoggedOut = true;
                Error();
                return;
            }

            if (exception.GetType() == typeof(SolarException) ||
                exception.GetType() == typeof(ExpiredException) ||
                exception.GetType() == typeof(InternalException) ||
                exception.GetType() == typeof(InvalidParamException) ||
                exception.GetType() == typeof(InvalidStateException) ||
                exception.GetType() == typeof(NetworkException))
            {
                Error();
                return;
            }

            if (exception.GetType() == typeof(PeException) ||
                exception.GetType() == typeof(PeLoginRequiredException) ||
                exception.GetType() == typeof(PeCancelException) ||
                exception.GetType() == typeof(PeFacebookException) ||
                exception.GetType() == typeof(PeFacebookLoginRequiredException) ||
                exception.GetType() == typeof(NotImplementedException))
            {
                LoggedOut = true;
                Error();
                return;
            }

            if (exception.GetType() == typeof(PeTermsException))
            {
                Log("open popup terms");
                UserTracking.Instance.LogShowTermsAndPolicy();

                PopupTermsAndPolicy.Open(()=> 
                {
                    Log("accept terms");
                    UserTracking.Instance.LogAcceptTermsAndPolicy();

                    Open(UserLoginType, UserInfo.Instance.GSN);
                });
                
                return;
            }
        }

        public void Logout()
        {
            Log();
            LoggedOut = true;
            Logout((exception, result) => 
            {
                Close();
                CurrentState = State.Logout;
                FriendsInfo.Instance.Clear();
            });
        }

        private void Close()
        {
            Warning("Close");

            SceneManager.LoadScene(Constants.SCENE_INTRO);
        }

        public void Logout(Action<Exception, bool> callback)
        {
            Auth.SignOutTask.Execute(this).SetCallback((exception, result) =>
            {
                if (callback != null)
                {
                    if (exception != null)
                        callback(exception, false);
                    else
                        callback(null, result);
                }
            });
        }

        public void SetConfig()
        {
            if (loginTask == null)
                return;

            Log("++++++++ " + loginTask.appSettings["appInfo"].ToString());
            ConfigAppInfo appInfo = CommonTools.FromJsonString<ConfigAppInfo>(loginTask.appSettings["appInfo"].ToString());
            Log("-------- " + appInfo.version);
            Log("-------- " + appInfo.minReqVersion);
            Log("-------- " + appInfo.installUrl);
            Log("++++++++ " + loginTask.appSettings["url"].ToString());

            ConfigInfo.Instance.Add("version", appInfo.version);
            ConfigInfo.Instance.Add("minReqVersion", appInfo.minReqVersion);
            ConfigInfo.Instance.Add("installUrl", appInfo.installUrl);

            ConfigUrl url = CommonTools.FromJsonString<ConfigUrl>(loginTask.appSettings["url"].ToString());
            Log("-------- " + url.assetbundles);
            Log("-------- " + url.api);
            Log("-------- " + url.review);
            Log("-------- " + url.terms);

            ConfigInfo.Instance.Add("assetbundles", url.assetbundles);
            ConfigInfo.Instance.Add("api", url.api);
            ConfigInfo.Instance.Add("review", url.review);
            ConfigInfo.Instance.Add("terms", url.terms);
        }

        public void SetMaintenance()
        {
            if (loginTask == null)
                return;

            MaintenanceInfo maintenance = CommonTools.FromJsonString<MaintenanceInfo>(loginTask.maintenanceSettings.ToString());

            Log("++++++++ " + maintenance.enable);
            Log("++++++++ " + maintenance.linkUrl);
            Log("++++++++ " + maintenance.startDate);
            Log("++++++++ " + maintenance.endDate);

            ConfigInfo.Instance.Add("maintenance", maintenance.enable.ToString());
            ConfigInfo.Instance.Add("maintenanceLinkUrl", maintenance.linkUrl);
            ConfigInfo.Instance.Add("maintenanceStartDate", maintenance.startDate.ToString());
            ConfigInfo.Instance.Add("maintenanceEndDate", maintenance.endDate.ToString());
        }

        public void AgreeTerms()
        {
            Terms.AgreeTerms();
		}

        public void ShowRewardCoupon(Action callback)
        {
            if (Auth.IsLoggedIn == false)
                return;
            
            if (!string.IsNullOrEmpty(rewardCoupon))
            {
                //if (Auth.IsFacebook)
                {
                    SessionLobby.Instance.RequestRewardCoupon(rewardCoupon, (exception, answer) =>
                    {
                        rewardCoupon = string.Empty;

                        if (exception != null)
                        {
                            if (exception is PeApiException)
                            {
                                int code = ((PeApiException)exception).ResultCode;
                                if (code == 565)
                                {
                                //TODO:: 이미 사용한 쿠폰입니다.
                                PopupMessage.Open(LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Title"),
                                                      LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Use.Description"), 0f, PopupMessage.Type.Ok,
                                                      (command) =>
                                                      {
                                                          if (command == PopupMessage.Command.Ok)
                                                          {
                                                              if (callback != null)
                                                                  callback();
                                                          }
                                                      });
                                }
                                else if (code == 564)
                                {
                                //TODO:: 기간이 지난 쿠폰입니다.
                                PopupMessage.Open(LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Title"),
                                                      LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Expired.Description"), 0f, PopupMessage.Type.Ok,
                                                      (command) =>
                                                      {
                                                          if (command == PopupMessage.Command.Ok)
                                                          {
                                                              if (callback != null)
                                                                  callback();
                                                          }
                                                      });
                                }
                                else
                                {
                                    if (callback != null)
                                        callback();
                                }
                            }
                        }
                        else
                        {
                            PopupRewardCoupon.Open(answer.useCoupon, () =>
                            {
                                if (callback != null)
                                    callback();
                            });
                        }
                    });
                }
                //else
                //{
                //    PopupFacebookLink.Open();
                //}
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }


        public void ReceiveLobbyRewardCoupon(string coupon, Action callback)
        {
            AppsFlyerSystem.Instance.AFListenerCoupon = coupon;

            if (CurrentState == State.InLobby || CurrentState == State.InGame || CurrentState == State.GameToInLobby)
                ShowRewardAFCoupon(callback);
        }

        public void ShowRewardAFCoupon(Action callback)
        {
            if (Auth.IsLoggedIn == false)
                return;

            if (!string.IsNullOrEmpty(AppsFlyerSystem.Instance.AFListenerCoupon))
            {
                //if (Auth.IsFacebook)
                {
                    SessionLobby.Instance.RequestRewardCoupon(AppsFlyerSystem.Instance.AFListenerCoupon, (exception, answer) =>
                    {
                        AppsFlyerSystem.Instance.AFListenerCoupon = string.Empty;

                        if (exception != null)
                        {
                            if (exception is PeApiException)
                            {
                                int code = ((PeApiException)exception).ResultCode;
                                if (code == 565)
                                {
                                    //TODO:: 이미 사용한 쿠폰입니다.
                                    PopupMessage.Open(LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Title"),
                                                          LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Use.Description"), 0f, PopupMessage.Type.Ok,
                                                          (command) =>
                                                          {
                                                              if (command == PopupMessage.Command.Ok)
                                                              {
                                                                  if (callback != null)
                                                                      callback();
                                                              }
                                                          });
                                }
                                else if (code == 564)
                                {
                                    //TODO:: 기간이 지난 쿠폰입니다.
                                    PopupMessage.Open(LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Title"),
                                                          LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Expired.Description"), 0f, PopupMessage.Type.Ok,
                                                          (command) =>
                                                          {
                                                              if (command == PopupMessage.Command.Ok)
                                                              {
                                                                  if (callback != null)
                                                                      callback();
                                                              }
                                                          });
                                }
                                else
                                {
                                    if (callback != null)
                                        callback();
                                }
                            }
                        }
                        else
                        {
                            PopupRewardFacebookFeed.Open(answer.useCoupon, () =>
                            {
                                if (callback != null)
                                    callback();
                            });
                        }
                    });
                }
                //else
                //{
                //    PopupFacebookLink.Open();
                //}
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }


        public void ReceiveFacebookFeed(string shareCode, Action callback)
        {
            FacebookFeedInfo.Instance.LinkListenerCode = shareCode;

            if (CurrentState == State.InLobby || CurrentState == State.InGame || CurrentState == State.GameToInLobby)
                ShowRewardFacebookFeed(callback);
        }

        public void ShowRewardFacebookFeed(Action callback)
        {
            if (Auth.IsLoggedIn == false)
                return;

            if (!string.IsNullOrEmpty(FacebookFeedInfo.Instance.LinkListenerCode))
            {
                //if (Auth.IsFacebook)
                {
                    SessionLobby.Instance.RequestFacebookFeedReward(FacebookFeedInfo.Instance.LinkListenerCode, (exception, receive) =>
                    {
                        FacebookFeedInfo.Instance.LinkListenerCode = string.Empty;

                        if (exception != null)
                        {
                            int code = ((PeApiException)exception).ResultCode;
                            if (code == 572)
                            {
                                //TODO:: 자기 자신은 보상을 받을 수 없다.
                                PopupMessage.Open(LocalizationSystem.Instance.Localize("FEED.ERROR.Popup.Title"),
                                                      LocalizationSystem.Instance.Localize("FEED.ERROR.Popup.Description"), 0f, PopupMessage.Type.Ok,
                                                      (command) =>
                                                      {
                                                          if (command == PopupMessage.Command.Ok)
                                                          {
                                                              if (callback != null)
                                                                  callback();
                                                          }
                                                      });
                            }
                            else if (code == 512)
                            {
                                //TODO:: 이미 받은 보상입니다.
                                PopupMessage.Open(LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Title"),
                                                      LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Use.Description"), 0f, PopupMessage.Type.Ok,
                                                      (command) =>
                                                      {
                                                          if (command == PopupMessage.Command.Ok)
                                                          {
                                                              if (callback != null)
                                                                  callback();
                                                          }
                                                      });
                            }
                            else if (code == 571)
                            {
                                //TODO:: 기간이 지난 피드입니다.
                                PopupMessage.Open(LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Title"),
                                                      LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Expired.Description"), 0f, PopupMessage.Type.Ok,
                                                      (command) =>
                                                      {
                                                          if (command == PopupMessage.Command.Ok)
                                                          {
                                                              if (callback != null)
                                                                  callback();
                                                          }
                                                      });
                            }
                            else if (code == 504)
                            {
                                //TODO:: 일일 보상 횟수 (5회) 전부 받음
                                PopupMessage.Open(LocalizationSystem.Instance.Localize("REWARD.Coupon.Exception.Title"),
                                                      LocalizationSystem.Instance.Localize("FEED.ERROR.Popup.Description.Day"), 0f, PopupMessage.Type.Ok,
                                                      (command) =>
                                                      {
                                                          if (command == PopupMessage.Command.Ok)
                                                          {
                                                              if (callback != null)
                                                                  callback();
                                                          }
                                                      });
                            }
                            else
                            {
                                if (callback != null)
                                    callback();
                            }
                        }
                        else
                        {
                            PopupRewardFacebookFeed.Open(receive.goodsInfo, () =>
                            {
                                if (callback != null)
                                    callback();
                            });
                        }
                    });
                }
                //else
                //{
                //    //PopupMessage.Open("NOTI", "페이스북 로그인이 필요합니다.(추후 번역 예정)", 0f, PopupMessage.Type.YesAndNo, (command) =>
                //    //{
                //    //    if (command == PopupMessage.Command.Yes)
                //    //    {
                //    //        Session.Instance.LinkFacebook((exception) =>
                //    //        {
                //    //            UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.freecoin, UserTracking.EventWhat.fb_connect, "M", UserTracking.Location.setting);
                //    //            PopupLoading.CloseSelf();
                //    //            PopupSystem.Instance.UnregisterAll();
                //    //        });
                //    //    }
                //    //    else if (command == PopupMessage.Command.No)
                //    //    {
                //    //        FacebookFeedInfo.Instance.LinkListenerCode = string.Empty;
                //    //    }
                //    //});

                //    PopupFacebookLink.Open();
                //}
            }
            else
            {
                if (callback != null)
                    callback();
            }
        }

        public enum FacebookLoginResult
		{
			FAIL,
			SUCCESS_NEW_USER,
			SUCCESS_EXIST_USER
		}

		public void LinkFacebookByLoginResult(Action<FacebookLoginResult> callback = null)
		{
			Log();

            CurrentState = State.FacebookReady;

            LinkFacebook((exception, result) =>
			{
				FacebookLoginResult _result = FacebookLoginResult.FAIL;
				if (CurrentState == State.LoginFail)
				{
					//TODO:: 혹시 모를 예외처리
					Error();
				}
				else if (exception == null)
				{
					_result = FacebookLoginResult.SUCCESS_NEW_USER;

                    //TODO:: 페이스북 연동 시, 처리 할 부분이 있는지? (신규 연동)
                    UserTracking.Instance.LogLinkFacebook();

                    CurrentState = State.FacebookLink;
                }
                else if (exception is PeLoginFailedException)
                {
                    CurrentState = State.FacebookFail;
                }
                else if (exception is PeCancelException)
                {
                    CurrentState = State.FacebookCancel;
                }
                else if (exception is PeAccountInUseException)
				{
					_result = FacebookLoginResult.SUCCESS_EXIST_USER;

					//TODO:: 이미 연동 된 이력이 있는 사람에 대한 처리
					CurrentState = State.FacebookSignin;
                }

				if (callback != null)
					callback(_result);
			});
		}

		public void LinkFacebook(Action<Exception> callback = null)
        {
            Log();

            CurrentState = State.FacebookReady;

            LinkFacebook((exception, result) =>
            {
                if (CurrentState == State.LoginFail)
                {
                    //TODO:: 혹시 모를 예외처리
                    Error();
                }
                else if (exception == null)
				{
					if (callback != null)
						callback(exception);
					//TODO:: 페이스북 연동 시, 처리 할 부분이 있는지? (신규 연동)
					UserTracking.Instance.LogLinkFacebook();

                    CurrentState = State.FacebookLink;
                }
                else if (exception is PeLoginFailedException)
                {
                    CurrentState = State.FacebookFail;
                }
                else if (exception is PeCancelException)
                {
                    CurrentState = State.FacebookCancel;
                }
                else if (exception is PeAccountInUseException)
				{
					if (callback != null)
						callback(exception);
					//TODO:: 이미 연동 된 이력이 있는 사람에 대한 처리
					CurrentState = State.FacebookSignin;
                }

            });
        }

        public void LinkFacebook(Action<Exception, LoginResult> callback)
        {
            Auth.LinkWithFacebookTask.Execute(this).SetCallback((exception, result) =>
            {
                if (callback != null)
                {
                    if (exception != null)
                        callback(exception, null);
                    else
                        callback(null, result);
                }
            });
        }

        public void LoginFacebook(Action<Exception, LoginResult> callback)
        {
            Auth.SigninWithExistingFacebookTask.Execute(this).SetCallback((exception, result) =>
            {
                if (callback != null)
                {
                    if (exception != null)
                        callback(exception, null);
                    else
                        callback(null, result);
                }
            });
        }

        public void LoginAnonymously(Action<Exception, LoginResult> callback)
        {
            Auth.SigninAnonymouslyTask.Execute(this).SetCallback((exception, result) =>
            {
                if (callback != null)
                {
                    if (exception != null)
                        callback(exception, null);
                    else
                        callback(null, result);
                }
            });
        }

        public void InviteFacebook(Action<Exception, InviteResponse> callback)
        {
            string message = "Come play this great game!";
            string referrer = "";

            Friend.FacebookInviteTask.Execute(message, referrer, this).SetCallback((exception, value) =>
            {
                if (exception != null)
                {
                    Debug.LogError("fail: " + exception.ToString());
                    return;
                }

                InviteResponse response = value;
                Debug.Log("success: " + response.RequestId);

                if (callback != null)
                    callback(exception, value);
            });
        }

        public void ShareLinkFacebook(string feedType, Action<Exception, bool> callback)
        {
            SessionLobby.Instance.RequestFacebookFeedIssue(feedType, (e, receive) => 
            {
                if (e != null)
                {
                    Log("Error Request Facebook Issue..");
                    PopupMessage.Open("ERROR", "transient error occurrence.");

                    if (callback != null)
                        callback(e, false);
                }
                else
                {
                    if (receive == null)
                    {
                        Log("Error Receive Facebook Issue...");
                        PopupMessage.Open("ERROR", "transient error occurrence.");

                        if (callback != null)
                            callback(null, false);
                    }
                    else
                    {
                        string feedURL = "https://www.facebook.com/777Slotoday" + "?code=" + receive.shareCode;
                        Debug.Log("feedURL : " + feedURL);
                        var link = DSLink.MakeLink(receive.title, receive.description, receive.img, feedURL);

                        Friend.FacebookShareLinkTask.Execute(link, this).SetCallback((exception, value) =>
                        {
                            if (exception != null)
                            {
                                Debug.LogError("fail: " + exception.ToString());
                                return;
                            }

                            Debug.Log("success: " + value);

                            if (callback != null)
                                callback(exception, value);
                        });
                    }
                }
            });
        }

        public void PurchaseItem(string id, Action<Exception, PurchaseResponse> callback, Action<StepStatus> status, UserGoodsInfo goodsInfo, Action<bool> cancelAction = null)
        {
            Log();

            Purchase.PurchaseTask.Execute(id, status, this).SetCallback((exception, result) =>
            {
                if (exception != null)
                {
                    if (PlayerPrefs.HasKey(PlayerPrefs.STORE_PURCHASE_ITEM_COIN + UserInfo.Instance.GSN))
                        PlayerPrefs.DeleteKey(PlayerPrefs.STORE_PURCHASE_ITEM_COIN + UserInfo.Instance.GSN);

                    if (PlayerPrefs.HasKey(PlayerPrefs.STORE_PURCHASE_ITEM_EFFECT + UserInfo.Instance.GSN))
                        PlayerPrefs.DeleteKey(PlayerPrefs.STORE_PURCHASE_ITEM_EFFECT + UserInfo.Instance.GSN);

                    if (exception is PeCancelException)
                    {
                        Log("Cancel Purchase");

                        if (ActionPurchase != null)
                            ActionPurchase();

                        if (cancelAction != null)
                            cancelAction(true);

                        return;
                    }

                    //TODO:: 에러메세지 문구는 나중에 바꿔주세요
                    if (exception is PeApiException)
                    {
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else if (exception is IapSdkInitException)
                    {
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else if (exception is IapSdkFailException)
                    {
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else if (exception is DuplicatePurchaseException)
                    {
                        //TODO:: 재시도 작업
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else if (exception is PurchasePendingException)
                    {
                        //TODO:: 재시도 작업
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else if (exception is InvalidMarketReceiptException)
                    {
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else if (exception is PeCancelException)
                    {
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else if (exception is GiveItemFailException)
                    {
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else if (exception is MissMatchOwnerException)
                    {
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else
                    {
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }

                    if (ActionPurchase != null)
                        ActionPurchase();

                    if (callback != null)
                        callback(exception, null);
                }
                else
                {
                    Log("success");

                    UserTracking.instance.LogPurchase(id, goodsInfo);

                    if (ActionPurchase != null)
                        ActionPurchase();

                    if (callback != null)
                        callback(null, result);
                }
            });
        }


        public void PurchaseRetry(Action callback)
        {
            Log();

            Purchase.PurchaseRetryTask.Execute(null, this).SetCallback((exception, result) =>
            {
                if (exception != null)
                {
                    if (PlayerPrefs.HasKey(PlayerPrefs.STORE_PURCHASE_ITEM_COIN + UserInfo.Instance.GSN))
                        PlayerPrefs.DeleteKey(PlayerPrefs.STORE_PURCHASE_ITEM_COIN + UserInfo.Instance.GSN);

                    if (PlayerPrefs.HasKey(PlayerPrefs.STORE_PURCHASE_ITEM_EFFECT + UserInfo.Instance.GSN))
                        PlayerPrefs.DeleteKey(PlayerPrefs.STORE_PURCHASE_ITEM_EFFECT + UserInfo.Instance.GSN);

                    if (exception is NetworkException)
                    {
                        PopupMessage.Open("ERROR", "check network status").OnClose(() =>
                        {
                            if (callback != null)
                                callback();
                        });
                    }
                    else if (exception is PeApiException)
                    {
                        PopupMessage.Open("ERROR", "check api error code").OnClose(() =>
                        {
                            if (callback != null)
                                callback();
                        });
                    }
                    else if (exception is IapSdkFailException)
                    {
                        PopupMessage.Open("ERROR", "check Unity IAP error code, may be setup error").OnClose(() =>
                        {
                            if (callback != null)
                                callback();
                        });
                    }
                    else if (exception is MissMatchOwnerException)
                    {
                        PopupMessage.Open("ERROR", "buyer user and retry user is different. change to login owner user.").OnClose(() =>
                        {
                            if (callback != null)
                                callback();
                        });
                    }
                    else if (exception is NoPurchasePendingException)
                    {
                        Log("there is no purchase pending, this is not error that notify user");

                        if (callback != null)
                            callback();
                    }
                    else if (exception is InvalidMarketReceiptException)
                    {
                        PopupMessage.Open("ERROR", "invaild market receipt").OnClose(() =>
                        {
                            if (callback != null)
                                callback();
                        });
                    }
                }
                else
                {
                    int coin = 0;
                    string effect = string.Empty;

                    if (PlayerPrefs.HasKey(PlayerPrefs.STORE_PURCHASE_ITEM_COIN + UserInfo.Instance.GSN))
                    {
                        coin = PlayerPrefs.GetInt(PlayerPrefs.STORE_PURCHASE_ITEM_COIN + UserInfo.Instance.GSN);

                        if (PlayerPrefs.HasKey(PlayerPrefs.STORE_PURCHASE_ITEM_EFFECT + UserInfo.Instance.GSN))
                            effect = PlayerPrefs.GetString(PlayerPrefs.STORE_PURCHASE_ITEM_EFFECT + UserInfo.Instance.GSN);

                        PopupPurchase.Open(PopupPurchase.Type.Success, (long)coin, effect).OnClose(() =>
                        {
                            UserInfo.Instance.UpdateUserCoin(() => { });

                            Log("Purchase Retry : " + result);

                            if (callback != null)
                                callback();
                        });
                    }
                }
            });
        }

        public void Friends(Action<Exception, FriendsResponse> callback)
        {
            if (Environment.Instance.UseLocalTest == true)
            {
#if UNITY_EDITOR
                StringBuilder sb = new StringBuilder();
                sb.Length = 0;
                string dummyJsonPath = "Assets/NEXT/COMMON/Assets/DummyJson/myfriends.json";
                var jsonText = AssetDatabase.LoadAssetAtPath(dummyJsonPath, typeof(TextAsset));
                FriendsResponse answer = CommonTools.FromJsonString<FriendsResponse>(((TextAsset)jsonText).text);

                callback(null, answer);
#endif
            }
            else
            {
                PlayEarth.Unity.Friend.Friend.MyFriendsTask.Execute(this).SetCallback((exception, result) =>
                {
                    if (exception is PeApiException)
                    {
                        PopupMessage.Open("ERROR", "ERROR : " + exception.Message);
                    }
                    else
                    {
                        if (callback != null)
                            callback(exception, result);
                    }
                });
            }
        }

        public void TermsDetail(Action<Exception, string, string> callback)
        {
            Terms.TermsDetailTask.Execute().SetCallback((exception, result) =>
            {
                if (callback != null)
                {
                    if (exception != null)
                        callback(exception, string.Empty, string.Empty);
                    else
                        callback(null, result.privacyPolicy, result.termsOfUse);
                }
            });
        }

        public void DisagreeTerms(Action<bool, Exception> callback)
        {
            Terms.TermsWithdrawalTask.Execute().SetCallback((exception, result) =>
            {
                if (callback != null)
                    callback(result, exception);
            });
        }

        public bool isGuestLogin()
        {
            if (Auth.IsLoggedIn && Auth.IsAnonymous)
                return true;
            else
                return false;
        }

        public bool isFacebookLogin()
        {
            if (Auth.IsLoggedIn && Auth.IsFacebook)
                return true;
            else
                return false;
        }

        public bool isLogin()
        {
            return Auth.IsLoggedIn;
        }

        private void SetupEnvironment()
        {
            switch (Environment.Instance.Target)
            {
                case EnvironmentField.Target.Local:
                    VoyagerSettings.Environment = Voyager.Unity.Settings.Environment.CUSTOM;
                    break;

                case EnvironmentField.Target.Dev:
                    VoyagerSettings.Environment = Voyager.Unity.Settings.Environment.DEV;
                    break;

                case EnvironmentField.Target.DQ:
                case EnvironmentField.Target.TQ:
                case EnvironmentField.Target.Review:
                    VoyagerSettings.Environment = Voyager.Unity.Settings.Environment.QA;
                    break;

                case EnvironmentField.Target.Live:
                    VoyagerSettings.Environment = Voyager.Unity.Settings.Environment.REAL;
                    break;
            }
        }

        private void CloseConnection()
        {
            Log();
        }


        private void OnApplicationQuit()
        {
            CloseConnection();
        }

        //private void SetupConfig(JsonObject json)
        //{
        //    Log();

        //    if (json.ContainsKey("app"))
        //    {
        //        string jsonText = (json["app"] as JsonObject).ToString();
        //        Hashtable table = (Hashtable)Procurios.Public.JSON.JsonDecode(jsonText);
        //        Dictionary<string, string> configData = new Dictionary<string, string>();
        //        CommonTools.DigestObject(configData, table, string.Empty);
        //        ConfigInfo.Instance.ConfigData = configData;
        //        Dump(configData);

        //        Environment.Instance.InstallURL = ConfigInfo.Instance.Find("appInfo.installUrl");
        //        Environment.Instance.TargetProperty.APIHost = ConfigInfo.Instance.Find("url.api");
        //        Environment.Instance.TargetProperty.AssetBundleHost = ConfigInfo.Instance.Find("url.assetbundles");
        //    }
        //    else
        //    {
        //        PopupMessageBox.Create("POPUP.MESSAGE.Error.Title", "POPUP.MESSAGE.ConfigLost").ActionCommand = (command) =>
        //        {
        //            Application.Quit();
        //        };
        //    }
        //}

        private bool CheckVersion()
        {
            if (CommonTools.NeedToUpgrade())
            {
                //PopupMessageBox.Create("POPUP.MESSAGE.NeedToUpgrade.Title", "POPUP.MESSAGE.NeedToUpgrade").ActionCommand = (command) =>
                //{
                //	Application.OpenURL(Environment.Instance.InstallURL);
                //	Application.Quit();
                //};

                return false;
            }
            else
            {
                return true;
            }
        }

        //private void SetupAuth(LoginResult result)
        //{
        //	Log();

        //	MyInfo.Instance.SessionKey = result.sessionKey;
        //	MyInfo.Instance.NickName = result.nickname;
        //	MyInfo.Instance.ProfileImage = result.profileImage;
        //	MyInfo.Instance.UID = result.email;
        //	MyInfo.Instance.UserType = result.userType;
        //	MyInfo.Instance.GSN = result.gsn;

        //	Dump(result);

        //	UserTracking.LogEvent("Login", new Parameter[]
        //	{
        //		new Parameter("location", CurrentState.ToString()),
        //		new Parameter("time", CommonTools.TimeToLong(ClientInfo.Instance.ServerTime)),
        //		new Parameter("coin", MyInfo.Instance.Coin),
        //		new Parameter("gem", MyInfo.Instance.Gem),
        //		new Parameter("what", "success"),
        //		new Parameter("how", MyInfo.Instance.UserType)
        //	});
        //}

        //private void OnConnectionClose(object sender, Voyager.Unity.Solar.CloseEventArgs args)
        //{
        //	Log();

        //	CloseConnection();

        //	if (CurrentState == State.Duplicated)
        //	{
        //		PopupMessageBox.Create(
        //			"POPUP.MESSAGE.Connection.Duplicated.Title",
        //			"POPUP.MESSAGE.Connection.Duplicated").ActionCommand = (command) =>
        //			{
        //				Error(args.Code.ToString());
        //			};
        //	}
        //	else
        //	{
        //		Reconnect();
        //	}
        //}

        private void Reconnect()
        {
            Log();

            //if (CurrentState == State.InLobby || CurrentState == State.InGame)
            {
                //PopupLoading.Show(true);

                State state = CurrentState;

                //#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                //				Login(UserLoginType, MyInfo.Instance.GSN, (exception) =>
                //#else
                //                Login(LoginType.Retry, 0, (exception) =>
                //#endif
                //				{
                //					//PopupLoading.Show(false);

                //					if (CurrentState == State.LoginFail)
                //					{
                //						Error();
                //					}
                //					else
                //					{
                //						CurrentState = state;

                //						if (exception != null)
                //							OnConnectionLost();
                //					}
                //				});
            }
            //else
            {
                Error();
            }
        }

        private void OnConnectionLost()
        {
            Log();

            //if (CurrentState == State.InLobby || CurrentState == State.InGame)
            {
                //var popup = PopupMessageBox.Create("POPUP.MESSAGE.Connection.Close.Title", "POPUP.MESSAGE.Connection.Close", 0f, PopupMessageBox.Type.OkCancel);
                //popup.OkText = "POPUP.MESSAGE.Connection.Close.Reconnect";
                //popup.CancelText = "POPUP.MESSAGE.Connection.Close.Login";
                //popup.ActionCommand = (command) =>
                //{
                //	if (command == PopupMessageBox.Command.Ok)
                //	{
                //		Reconnect();
                //	}
                //	else
                //	{
                //		Error();
                //	}
                //};
            }
            //else
            {
                Error();
            }
        }

        //private void OnConnectionMessage(object sender, AnswerEventArgs args)
        //{
        //	Log(args.Answer.MessageType.ToString());

        //	switch (args.Answer.MessageType)
        //	{
        //		case AnsMessageType.ForwardFromRoomContents:
        //		case AnsMessageType.NtfErrorForwardToRoomContents:
        //		case AnsMessageType.NtfLeaveRoom:
        //		case AnsMessageType.NtfWebMessage:
        //			break;

        //		case AnsMessageType.NtfLogout:
        //			OnLogoutNtf(args);
        //			break;

        //		case AnsMessageType.NtfLobbyContent:
        //			SessionLobby.Instance.OnNotify(args);
        //			break;
        //	}
        //}

        //private void OnLogoutNtf(AnswerEventArgs args)
        //{
        //	Log();

        //	var message = args.Answer as NtfLogout;
        //	if (message == null)
        //	{
        //		Error("Invalid message.");
        //		return;
        //	}

        //	switch (message.RemoveUserReason)
        //	{
        //		case RemoveUserReason.ByPurgeRoom:
        //		case RemoveUserReason.Disconnected:
        //		case RemoveUserReason.FromRoomContents:
        //		case RemoveUserReason.FromUser:
        //		case RemoveUserReason.Internal:
        //		case RemoveUserReason.MoveRequest:
        //			// TODO: ...
        //			break;

        //		case RemoveUserReason.Duplicated:
        //			CurrentState = State.Duplicated;
        //			break;
        //	}
        //}

        //private void OnConnectionThreadMessage(object sender, AnswerEventArgs args)
        //{
        //	if (args.Answer.MessageType == AnsMessageType.NtfLogout)
        //	{
        //		var message = args.Answer as NtfLogout;
        //		if (message.RemoveUserReason == RemoveUserReason.Duplicated)
        //		{
        //			// TODO: ...
        //		}
        //	}
        //}

        private void OnMaintenance(Exception exception)
        {
            Log();

            //MaintenanceResult maintenance = (exception as PeMaintenanceException).maintenance;
            //Debug.Assert(maintenance.Enable);

            //DateTime beginDate = maintenance.StartDate;
            //DateTime endDate = maintenance.EndDate;
            //string date = string.Format("{0} ~ {1}", beginDate.ToString("MMM dd, yyyy hh:mm tt"), endDate.ToString("MMM dd, yyyy hh:mm tt"));
            ////string description = maintenance.Contents.Text + "\n\n" + date;

            //PopupMaintenance.Create(maintenance.Contents.Text, date).ActionOK = () =>
            //{
            //	Application.Quit();
            //};
        }        

        private void OnLoginSuccess()
        {
            Log();

            //LoggedOut = false;
            //CurrentState = State.ProgressConnection;

            //if (CheckVersion() == false)
            //{
            //    Log("Need to upgrade.");
            //    return;
            //}

            //OnUpdateSettings(SettingSystem.SettingTypes.Notification);
            /*
            SessionLobby.Instance.Request<AnswerIntro>("intro", new RequestIntro(), (answer) =>
            {
                if (answer.resultCode != 0)
                {
                    Error("ERRORCODE: " + answer.resultCode);
                    return;
                }

                if (answer.value.casinos != null)
                {
                    // CHECK: 이렇게 마이닝 하지 말고 데이터를 설계하세요. << 2순위라 일단 놔둘게요.

                    foreach (var data in answer.value.casinos)
                    {
                        var slotGoodsList = new List<SlotData>();
                        foreach (var slotData in data.slots)
                        {
                            SlotGoodsData goods = new SlotGoodsData();
                            if (slotData.goods != null)
                            {
                                goods = new SlotGoodsData()
                                {
                                    ID = slotData.goods.goodsId,
                                    Price = slotData.goods.dispPrice,
                                    SaleType = slotData.goods.saleType,
                                };
                            }
                            
                            SlotData slot = new SlotData()
                            {
                                Index = slotData.index,
                                Code = slotData.grcSlotCode,
                                Goods = goods,
                            };

                            slotGoodsList.Add(slot);
                        }

                        CasinoData casino = new CasinoData()
                        {
                            ID = data.casinoId,
                            Name = data.casinoName,
                            Level = data.openLevel,
                            Slots = slotGoodsList,
                        };

                        DataPool.CasinoInfo.Instance.Add(casino);
                    }
                }

                MyInfo.Instance.AssetLimit = new MyInfo.AssetLimitInfo()
                {
                    coin = answer.value.assetBase.maxCoin,
                    gem = answer.value.assetBase.maxGem,
                };

                MyInfo.Instance.Level.maxLevel = answer.value.maxLevel;
                ClientInfo.Instance.ServerTime = CommonTools.JavaTime(answer.value.server.date);

                OnOpenSession();
            });
            */
        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
                OnKeyUpEscape();
        }

        private void OnKeyUpEscape()
        {
            //if (Startup.IsNull == false)
            //{
            //    Application.Quit();
            //    return;
            //}

            //if (CurrentState != State.InLobby && CurrentState != State.InGame)
            {
            //    return;
            }

            //if (PopupBaseSystem.Instance.OnKeyUpEscape())
            //{
            //	return;
            //}

            //if (GameMain.IsNull == false)
            //{
            //	GameMain.Instance.OnKeyUpEscape();
            //	return;
            //}

            //if (EditUI.IsNull == false)
            //{
            //	EditUI.Instance.OnKeyUpEscape();
            //	return;
            //}

            //if (Lobby.IsNull == false)
            //{
            //	var popup = PopupMessageBox.Create("POPUP.MESSAGE.ApplicationQuit.Title", "POPUP.MESSAGE.ApplicationQuit", 0f, PopupMessageBox.Type.OkCancel);
            //	popup.OkText = "POPUP.MESSAGE.ApplicationQuit.Yes";
            //	popup.CancelText = "POPUP.MESSAGE.ApplicationQuit.No";
            //	popup.ActionCommand = (command) =>
            //	{
            //		if (command == PopupMessageBox.Command.Ok)
            //		{
            //			Log("Application Quit");
            //			Application.Quit();
            //			return;
            //		}
            //	};
            //}
        }

        private void OnApplicationPause(bool state)
        {
            //if (state)
            //{
            //	ConnectionClient.Instance.Pause(1000 * 60 * 3);
            //	pauseTime = ClientInfo.Instance.ServerTime;
            //}
            //else
            //{
            //	ConnectionClient.Instance.Resume();

            //	if ((ClientInfo.Instance.ServerTime - pauseTime).TotalSeconds > 60 * 10 * 3)
            //		CommonTools.RestartApp();
            //}
        }

        //		public void AutoLogin(Action<Exception, LoginResult> callback)
        //		{
        //#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        //			ResetAuth();
        //#endif
        //			Auth.AutoLoginTask.Execute(this).SetCallback((exception, result) =>
        //			{
        //				if (callback != null)
        //				{
        //					if (exception != null)
        //						callback(exception, null);
        //					else
        //						callback(null, result);
        //				}
        //			});
        //		}

        //		public void LoginGSN(long gsn, Action<Exception, LoginResult> callback)
        //		{
        //			Auth.SigninWithGsnTask.Execute(gsn, this).SetCallback((exception, result) =>
        //			{
        //				if (callback != null)
        //				{
        //					if (exception != null)
        //						callback(exception, null);
        //					else
        //						callback(null, result);
        //				}
        //			});
        //		}

        //		public void VerifySession(Action<Exception, SessionVerifyResult> callback)
        //		{
        //			Auth.VerifySessionTask.Execute(this).SetCallback((exception, result) =>
        //			{
        //				if (callback != null)
        //				{
        //					if (exception != null)
        //						callback(exception, null);
        //					else
        //						callback(null, result);
        //				}
        //			});
        //		}

        //		public void ModifyNickName(string nickname, Action<Exception, string> callback)
        //		{
        //			Auth.ModifyNicknameTask.Execute(nickname, this).SetCallback((exception, result) =>
        //			{
        //				if (callback != null)
        //				{
        //					if (exception != null)
        //						callback(exception, string.Empty);
        //					else
        //						callback(null, result);
        //				}
        //			});
        //		}

        //		public void ModifyProfileImage(string image, Action<Exception, string> callback)
        //		{
        //			Auth.ModifyProfileImgTask.Execute(image, this).SetCallback((exception, result) =>
        //			{
        //				if (callback != null)
        //				{
        //					if (exception != null)
        //						callback(exception, string.Empty);
        //					else
        //						callback(null, result);
        //				}
        //			});
        //		}

        //		public void GetProfileImages(Action<Exception, List<string>> callback)
        //		{
        //			Auth.GetProfileImgsTask.Execute(this).SetCallback((exception, result) =>
        //			{
        //				if (callback != null)
        //				{
        //					if (exception != null)
        //						callback(exception, null);
        //					else
        //						callback(null, result);
        //				}
        //			});
        //		}
        
        //public void FacebookInvite(Action<Exception, InviteResponse> callback)
        //{
        //	Friend.FacebookInviteTask.Execute(this).SetCallback((exception, result) =>
        //	{
        //		if (callback != null)
        //		{
        //			if (exception != null)
        //				callback(exception, null);
        //			else
        //				callback(null, result);
        //		}
        //	});
        //}

        //public void FacebookShareLink(string url, Action<Exception, bool> callback)
        //{
        //	Friend.FacebookShareLinkTask.Execute(url, this).SetCallback((exception, result) =>
        //	{
        //		if (callback != null)
        //		{
        //			if (exception != null)
        //				callback(exception, false);
        //			else
        //				callback(null, result);
        //		}
        //	});
        //}

        //public void GetNotification(Action<bool> callback)
        //{
        //	FirebaseMessageWrapper.GetAllowTask.Execute(this).SetCallback((exception, allow) =>
        //	{
        //		if (callback != null)
        //		{
        //			if (exception != null)
        //				callback(false);
        //			else
        //				callback(allow);
        //		}
        //	});
        //}

        //public void SetNotification(bool allow, Action<Exception, bool> callback)
        //{
        //	FirebaseMessageWrapper.SetAllowTask.Execute(allow, this).SetCallback((exception, result) =>
        //	{
        //		if (callback != null)
        //		{
        //			if (exception != null)
        //				callback(exception, false);
        //			else
        //				callback(null, result);
        //		}
        //	});
        //}
        //private Request Payload(Request request)
        //{
        //    request.os = clientinfo
        //}
	}
}
