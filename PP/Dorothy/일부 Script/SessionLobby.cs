// SessionLobby.cs - SessionLobby implementation file
//
// Description      : SessionLobby main instance
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2018/02/14
// Last Update      : 2019/06/11
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using PlayEarth.Unity;
using PlayEarth.Unity.Auth;
using Voyager.Unity.Network;
using Ugo.Unity;
using Dorothy.Network;
using Dorothy.DataPool;
using Dorothy.UI.Popup;
using UnityEditor;

namespace Dorothy
{
    public sealed class SessionLobby: BaseSystem<SessionLobby>
    {
        private State currentState = State.Ready;
        private Dictionary<string, Action<string>> NTF = new Dictionary<string, Action<string>>();
        private readonly string lobbyService = "st-voyager-ugo/v1/";
        private readonly string userService = "st-content-user/v1/";
        private readonly string storeService = "st-content-store/v1/";
        private readonly string rewardService = "st-content-reward/v1/";
        private readonly string missionService = "st-content-mission/v1/";
        private readonly string eventService = "st-content-event/v1/";

        private readonly string notiCoinFree = "LobbyContents.earth-asset.user.coin-log.O_FREE";
        private readonly string notiCoinPaid = "LobbyContents.earth-asset.user.coin-log.O_PAID";
        private readonly string notiCoinAdmin = "LobbyContents.earth-asset.user.coin-log.ADMIN";

        private readonly string notiLevelUp = "LobbyContents.content-user.level.up";
        private readonly string notiGift = "LobbyContents.content-reward.user.inbox.gift-count";
        private readonly string notiMaintenance = "LobbyContents.voyager-dashboard.cs.notification";
        private readonly string notiWorldMessage = "LobbyContents.content-game.jackpot.world-message";
        private readonly string notiUserEffect = "LobbyContents.content-user.user.effect.give";

        public Action<State> ActionChangeState = delegate { };
        public Action ActionJoinLobby = delegate { };
        public Action ActionLoadLobby = delegate { };
        public Action<int, bool, AssetBundleSystem.AssetBundleInfo.State, float> ActionSlotDownload = delegate { };

        public Action<object> ActionLevelup = delegate { };
        
        private Action<RpcMessageArgs, Action<object>> OnMessageCallback;
        private Dictionary<string, Action<object>> OnMessageDic = new Dictionary<string, Action<object>>();
        private List<Action<object>> OnMessageList = new List<Action<object>>();

        private Action ActionUpdateGift = null;
        private bool isApplicationQuit = false;
        private bool isRequestCompleted = true;
       
        private bool isUpdateBonusTime = false;
        private float bonusTime = 0.0f;
        public float BonusTime
        {
            get { return bonusTime; }
            set { bonusTime = value; }
        }

        public enum State
        {
            Ready,
            JoinLobbyWait,
            JoinLobbySuccess,
            JoinLobbyFail,
        }

        public bool InitIntro
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

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void Awake()
        {
            base.Awake();

            Session.Instance.ActionChangeState += OnChangeSessionState;
        }

        void Update()
        {
            if (isUpdateBonusTime == true)
                bonusTime += Time.deltaTime;

            if (isRequestCompleted == true)
            {
                if (PopupSystem.Instance.IsPopupOpened("POPUP_Loading") == true)
                    PopupSystem.Instance.OnKeyUpEscape();
            }
        }

        void OnApplicationQuit()
        {
            isApplicationQuit = true;
        }

        private void OnChangeSessionState(Session.State state)
        {
            switch (state)
            {
                case Session.State.IntroFade:
                    Intro(() =>
                    {
                        JoinLobby(() =>
                        {
                            //TODO:: 1회 접속시 동영상 광고 노출 횟수 호출
                            RequestAdStatus(()=> 
                            {
                                RewardAdSystem.Instance.Load(null);
                            });

                            //TODO:: cancel test 확인
                            LocalNotification.Instance.CancelPushMessage(LocalNotification.PushMessageType.Gift);

                            double second = CommonTools.PushMessageTime();
                            LocalNotification.Instance.AddPushMessage(LocalNotification.PushMessageType.Gift, "<b>SLOTODAY</b>", "A Gift from Dorothy! Dorothy's Free Coin gift is delivered! Only one chance a day!", (long)second);

                            ActionJoinLobby();
                        });
                    });
                    break;
                case Session.State.GameToInLobby:
                    int id = -1;
                    if (PlayerPrefs.HasKey(PlayerPrefs.SLOT_LAST_PLAYED))
                        id = PlayerPrefs.GetInt(PlayerPrefs.SLOT_LAST_PLAYED);
                    
                    JoinLobby(() => 
                    {
                        //TODO:: TEST
                        Debug.Log("OPEN SLOT INGAME IN LOBBY : " + LobbyInfo.Instance.GetOpenSlotIdList());

                        UserTracking.Instance.LogLobbyEvent(UserTracking.LobbyWhich.ingame, id);

                        OnLoadLobby();
                    });
                    break;
                case Session.State.InLobby:
                    //TODO:: TEST
                    Debug.Log("OPEN SLOT INTRO IN LOBBY : " + LobbyInfo.Instance.GetOpenSlotIdList());

                    UserTracking.Instance.LogLobbyEvent(UserTracking.LobbyWhich.intro);
                    NotiSystem.Instance.StartWorldMessageTimer();

                    OnLoadLobby();
                    break;
                default:
                    break;
            }
        }

        private Request Payload(Request request)
        {
            request.os = ClientInfo.Instance.PlatformType;
            request.store = ClientInfo.Instance.ServiceType;
            request.appVer = Application.version;
            request.locale = ClientInfo.Instance.Locale;

            return request;
        }

        private IDictionary<string, object> RequestParam(Request request)
        {
            request = Payload(request);
            var param = new Dictionary<string, object>()
            {
                { "os", request.os },
                { "store", request.store },
                { "locale", request.locale },
                { "appVer", request.appVer },
                { "param", request.param },
            };

            return param;
        }

        private void Request<T>(string service, string command, Request request, Action<Exception, T> callback, HttpMethod mathod = HttpMethod.POST) where T : Network.Answer
        {
            if (Session.Instance.IsDisconnect == true)
                return;

            if (Session.Instance.IsGameEnter == true)
            {
                if (PopupSystem.Instance.IsPopupOpened("POPUP_Loading") == true)
                    PopupLoading.CloseSelf();

                return;
            }

            Debug.LogWarning("State : " + Session.Instance.CurrentState.ToString());

            if (Environment.Instance.UseLocalTest)
            {
#if UNITY_EDITOR
                StringBuilder sb = new StringBuilder();
                sb.Length = 0;
                string dummyJsonPath = "Assets/NEXT/COMMON/Assets/DummyJson/";
                string path = sb.Append(dummyJsonPath).Append(command).Append(".json").ToString();
                //Debug.Log(path);
                var jsonText = AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset));
                T answer = CommonTools.FromJsonString<T>(((TextAsset)jsonText).text);

                callback(null, answer);
#endif
            }
            else
            {
                isRequestCompleted = false;
                Log("command : " + Ugo.Unity.Client.Instance.Url + "/" + service + command);
                //Session.Instance.OldRequestData = new RequestData(Ugo.Unity.Client.Instance.Url + "/" + service + command, mathod, RequestParam(request));
                UgoProxy.RpcTask.RequestToContentAsync<T>(Ugo.Unity.Client.Instance.Url + "/" + service + command, mathod, RequestParam(request)).SetCallback((e, result) => 
                {
                    isRequestCompleted = true;
                    if (OnError(command, e))
                    {
                        if (callback != null)
                            callback(e, null);
                    }
                    else
                    {
                        if (result == null)
                        {
                            if (e != null)
                                UnityEngine.Debug.LogException(e);

                            if (e is NetworkException || e is Ugo.CSLib.TimeoutException || e is Ugo.CSLib.UgoException)
                            {
                                //Debug.LogError("TimeOut " + e.ToString());

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
                                    bool showPopupLoading = false;

                                    int order = PopupOrderList.Instance.GetOrder(PopupSystem.POPUP_TYPE.POPUP_Default);
                                    PopupMessage.Open(LocalizationSystem.Instance.Localize("NETWORKDISCONNECTION.Popup.Title"),
                                                      LocalizationSystem.Instance.Localize("NETWORKDISCONNECTION.Popup.Description"), 0f, PopupMessage.Type.Reconnect, (cmd) =>
                                    {
                                        if (cmd == PopupMessage.Command.Ok)
                                        {
                                            if (showPopupLoading == true)
                                                PopupLoading.Open();
                                            else
                                                PopupSystem.Instance.SetIndicatorActive(true);

                                            ++Session.Instance.ReconnectCount;

                                            Request<T>(service, command, request, callback, mathod);
                                        }
                                    }, order).OnOpen((handler) =>
                                    {
                                        if (PopupSystem.Instance.IsPopupOpened("POPUP_Loading") == true)
                                        {
                                            showPopupLoading = true;
                                            PopupLoading.CloseSelf();
                                        }
                                    });
                                }
                            }
                            else
                            {
                                if (callback != null)
                                    callback(e, null);
                            }
                        }
                        else
                        {
                            Session.Instance.ReconnectCount = 0;

                            if (callback != null)
                                callback(e, result);
                        }
                    }
                });
            }
        }

        private void RequestCallback<T>(Exception e, T result)
        {

        }
        
        private bool OnError(string command, Exception exception)
        {
            if (OnPassError(command))
                return false;

            int resultCode = 0;
            string message = string.Empty;

            if (exception is PeApiException)
            {
                var pae = exception as PeApiException;
                resultCode = pae.ResultCode;
                message = pae.Message;
            }

            if (resultCode == 0)
                return false;

            switch (resultCode)
            {
                //TODO:: 여기에 특정 에러코드가 있을 경우에 대한 처리를 넣는다.
                default:
                    {
#if RELEASE
                        if (Environment.Instance.Target == EnvironmentField.Target.Live)
                            PopupMessage.Open("ERROR", "An error occurred during the game.\nWe will fix the error as soon as possible.");
                        else
                            PopupMessage.Open(string.Format("Error Code {0}", resultCode), string.Format("Error Message : {0}", message));
#else
                        PopupMessage.Open(string.Format("Error Code {0}", resultCode), string.Format("Error Message : {0}", message));
#endif

                        return true;
                    }
            }
        }

        private bool OnPassError(string command)
        {
            switch (command)
            {
                //TODO:: 여기에 특정 api 커맨드가 있을 경우, true시켜서 에러를 통과시킨다.
                default:
                    {
                        return false;
                    }
            }
        }

        public void AddNotificationMessageListener()
        {
            AddNotificationMessageCallback(notiGift, OnNotiGift);
            AddNotificationMessageCallback(notiMaintenance, OnNotiMaintenance);
            AddNotificationMessageCallback(notiWorldMessage, OnNotiWorldMessage);

            AddNotificationMessageCallback(notiCoinFree, OnNotiUserCoin);
            AddNotificationMessageCallback(notiCoinPaid, OnNotiUserCoin);
            AddNotificationMessageCallback(notiCoinAdmin, OnNotiUserCoin);


            AddNotificationMessageCallback(notiUserEffect, OnNotiUserEffect);
        }

        public void AddNotificationMessageCallback(string methodName, Action<object> callback)
        {
            if (OnMessageCallback == null)
            {
                OnMessageCallback = NotificationMessageProc;
                UgoProxy.RpcTask.OnMessage += OnMessageCallback;
            }

            OnMessageDic.Add(methodName, callback);
        }

        public void RemoveNotificationMessageCallback(string methodName)
        {
            OnMessageDic.Remove(methodName);

            if (OnMessageDic.Count == 0)
            {
                if (OnMessageCallback != null)
                    UgoProxy.RpcTask.OnMessage -= OnMessageCallback;

                OnMessageCallback = null;
            }
        }
        
        public void RemonveAllNotificationMessageCallback()
        {
            OnMessageDic.Clear();

            if ((UgoProxy.RpcTask.OnMessage != null) && (OnMessageCallback != null))
                UgoProxy.RpcTask.OnMessage -= OnMessageCallback;

            OnMessageCallback = null;
            UserInfo.Instance.RemoveNtfUserCoinData();
        }

        private void NotificationMessageProc(RpcMessageArgs rpcMessageArgs, Action<object> callback)
        {
            if (rpcMessageArgs.IsNotification)
            {
                if (OnMessageDic.ContainsKey(rpcMessageArgs.Method))
                {
                    object data = null;
                    Warning(rpcMessageArgs.Method);

                    if (rpcMessageArgs.Method == notiLevelUp)
                    {
                        data = CommonTools.FromJsonString<NtfLevelUpData>(rpcMessageArgs.Pars.ToString());
                    }
                    else if (rpcMessageArgs.Method == notiGift)
                    {
                        data = CommonTools.FromJsonString<NtfGift>(rpcMessageArgs.Pars.ToString());
                    }
                    else if (rpcMessageArgs.Method == notiMaintenance)
                    {
                        data = CommonTools.FromJsonString<NtfMaintenance>(rpcMessageArgs.Pars.ToString());
                    }
                    else if (rpcMessageArgs.Method == notiWorldMessage)
                    {
                        data = CommonTools.FromJsonString<NtfWorldMessage>(rpcMessageArgs.Pars.ToString());
                    }
                    else if (rpcMessageArgs.Method == notiCoinFree)
                    {
                        data = CommonTools.FromJsonString<NtfUserCoin>(rpcMessageArgs.Pars.ToString());
                    }
                    else if (rpcMessageArgs.Method == notiCoinPaid)
                    {
                        data = CommonTools.FromJsonString<NtfUserCoin>(rpcMessageArgs.Pars.ToString());
                    }
                    else if (rpcMessageArgs.Method == notiCoinAdmin)
                    {
                        data = CommonTools.FromJsonString<NtfUserCoin>(rpcMessageArgs.Pars.ToString());
                    }
                    else if (rpcMessageArgs.Method == notiUserEffect)
                    {
                        data = CommonTools.FromJsonString<NtfUserEffect>(rpcMessageArgs.Pars.ToString());
                    }

                    Action<object> ntfCallback = OnMessageDic[rpcMessageArgs.Method];
                    ntfCallback(data);
                }
            }
        }

        public void AddListenerUpdateGift(Action callback)
        {
            ActionUpdateGift = callback;
        }

        public void RemoveListenerUpdateGift()
        {
            ActionUpdateGift = null;
        }

        public void LoadAssetBundles(int msn, string[] assetList)
        {
            ResourceManager.Instance.LoadAssetBundles(assetList, (dlName, dlResult, state, dlProgress) =>
            {
                Debug.LogWarning(dlName + " Download processing : " + dlProgress);

                if (ActionSlotDownload != null)
                    ActionSlotDownload(msn, dlResult, state, dlProgress);
            });
        }

        public void Intro(Action callback)
        {
            Log();

            //TODO:: 현재 인트로로 나갔을 경우(재로그인시), 무조건 인트로를 호출해달라는 요구로 인해 조건 풀어둡니다.
            // 대신, 로그아웃이 아닌 인트로로 가야할 경우가 생긴다면, 그 부분은 예외처리 작업이 필요합니다. 체크요망.
            //if (InitIntro)
            //{
            //    if (callback != null)
            //        callback();
            //}
            //else
            //{
                Request<AnswerIntro>
                (
                    lobbyService,
                    "intro",
                    new RequestIntro(),
                    (e, answer) =>
                    {
                        InitIntro = true;

                        ClientInfo.Instance.ServerTime = CommonTools.JavaTime(answer.server.date);
                        
                        if (callback != null)
                            callback();
                    }
                );
            //}
        }

        public void JoinLobby(Action callback)
        {
            Log();

            CurrentState = State.JoinLobbyWait;

            Request<AnswerLobby>
            (
                lobbyService,
                "lobby",
                new RequestLobby(),
                (e, answer) =>
                {
                    OnUserAsset(answer.user);
                    LobbyInfo.Instance.UpdateLayout(answer.lobbyLayout, answer.slot, answer.layoutBanners);
                    LobbyInfo.Instance.UpdateRollingBanner(answer.rollingBanners);
                    LobbyInfo.Instance.TimeBonusData = answer.timeBonus;
                    LobbyInfo.Instance.InboxGiftSummary = answer.inboxGiftSummary;
                    LobbyInfo.Instance.MissionSummaryData = answer.userMissionSummary != null ? answer.userMissionSummary : null;

                    LobbyInfo.Instance.BlastSummaryData = answer.blastEventSummary != null ? answer.blastEventSummary : null;
                    LobbyInfo.Instance.WorldMessageConfigData = answer.jackpotWorldMessageConfig != null ? answer.jackpotWorldMessageConfig : null;

                    BigBannerInfo.Instance.Update(answer.bigBanners != null ? answer.bigBanners : null);

                    LobbyInfo.Instance.UpdateWelcomeBonus(answer.welcomeBonus != null ? answer.welcomeBonus : null);
                    LobbyInfo.Instance.UpdateFBConnectBonus(answer.fbConnectBonus != null ? answer.fbConnectBonus : null);

                    HotDealInfo.Instance.UpdateOffer(answer.hotDealOffers != null ? answer.hotDealOffers : null);
                    HotDealInfo.Instance.UpdateBonus(answer.hotDealBonuses != null ? answer.hotDealBonuses : null);

                    AttendInfo.Instance.UpdateAttendBonus(answer.attendanceBonus != null ? answer.attendanceBonus : null);

                    LobbyInfo.Instance.LobbyStoreBonus = answer.storeBonus != null ? answer.storeBonus : null;

                    LobbyInfo.Instance.QuestSummaryData = answer.questSummary != null ? answer.questSummary : null;

                    isUpdateBonusTime = true;
                    BonusTime = 0.0f;

                    CurrentState = State.JoinLobbySuccess;

                    if (callback != null)
                        callback();
                }
            );
        }

        public void RefreshLobby(Action callback)
        {
            Log();

            CurrentState = State.JoinLobbyWait;

            Request<AnswerLobby>
            (
                lobbyService,
                "lobby",
                new RequestLobby(),
                (e, answer) =>
                {
                    OnRefreshUserAsset(answer.user);
                    LobbyInfo.Instance.UpdateLayout(answer.lobbyLayout, answer.slot, answer.layoutBanners);
                    LobbyInfo.Instance.UpdateRollingBanner(answer.rollingBanners);
                    LobbyInfo.Instance.TimeBonusData = answer.timeBonus;
                    LobbyInfo.Instance.InboxGiftSummary = answer.inboxGiftSummary;
                    LobbyInfo.Instance.MissionSummaryData = answer.userMissionSummary != null ? answer.userMissionSummary : null;

                    LobbyInfo.Instance.BlastSummaryData = answer.blastEventSummary != null ? answer.blastEventSummary : null;
                    LobbyInfo.Instance.WorldMessageConfigData = answer.jackpotWorldMessageConfig != null ? answer.jackpotWorldMessageConfig : null;
                    
                    AttendInfo.Instance.UpdateAttendBonus(answer.attendanceBonus != null ? answer.attendanceBonus : null);

                    LobbyInfo.Instance.LobbyStoreBonus = answer.storeBonus != null ? answer.storeBonus : null;

                    LobbyInfo.Instance.QuestSummaryData = answer.questSummary != null ? answer.questSummary : null;

                    isUpdateBonusTime = true;
                    BonusTime = 0.0f;

                    CurrentState = State.JoinLobbySuccess;

                    if (callback != null)
                        callback();
                }
            );
        }

        private void OnLoadLobby()
        {
            if (Session.Instance.CurrentState == Session.State.InLobby ||
                Session.instance.CurrentState == Session.State.GameToInLobby)
			{
                UserTracking.Instance.LogFirstLoading();

                SceneManager.LoadScene(Constants.SCENE_LOBBY);
                ActionLoadLobby();
                //StartCoroutine(LoadLobbySceneCoroutine());
            }
            else
            {
                Error("Not Session State InLobby");
                return;
            }
        }
		
		private IEnumerator<YieldInstruction> LoadLobbySceneCoroutine()
		{
			Scene currentScene = SceneManager.GetActiveScene();

			yield return SceneManager.LoadSceneAsync(Constants.SCENE_LOBBY, LoadSceneMode.Additive);

			SceneManager.UnloadSceneAsync(currentScene);

			ActionLoadLobby();
		}

        public void OnUserBaseInfo(LoginResult loginInfo)
        {
            if (loginInfo == null)
                return;

            UserInfo.Instance.BaseInfo = new UserInfo.UserBaseInfo()
            {
                GSN = loginInfo.gsn,
                UID = loginInfo.uid,
                userType = loginInfo.userType,
                nickName = loginInfo.nickname,
                email = loginInfo.email,
                profileImage = loginInfo.profileImage,
            };
        }

        public void OnUserAsset(UserProfile info)
        {
            if (info != null)
            {
                if (info.effects != null)
                    UserInfo.Instance.UpdateEffectInfo(info.effects);

                if (info.assetInfo != null)
                {
                    UserInfo.Instance.Asset = new UserInfo.AssetInfo()
                    {
                        coin = info.assetInfo.coin,
                    };
                }

                if (info.levelInfo != null)
                {
                    UserInfo.Instance.Level = new UserInfo.LevelInfo()
                    {
                        level = info.levelInfo.level,
                        exp = info.levelInfo.exp,
                        minExp = info.levelInfo.minExp,
                        nextExp = info.levelInfo.nextExp,
                        maxLevel = UserInfo.Instance.Level.maxLevel,
                        levelUpRewardCoin = info.levelInfo.levelUpRewardCoin,
                    };
                }
                
                UserInfo.Instance.GSN = info.gsn;
                UserInfo.Instance.FBID = info.fbId;
                UserInfo.Instance.RegistrationDate = info.registrationDate;
            }
        }

        public void OnRefreshUserAsset(UserProfile info)
        {
            if (info != null)
            {
                if (info.assetInfo != null)
                {
                    UserInfo.Instance.Asset = new UserInfo.AssetInfo()
                    {
                        coin = info.assetInfo.coin,
                    };
                }

                if (info.levelInfo != null)
                {
                    UserInfo.Instance.Level = new UserInfo.LevelInfo()
                    {
                        level = info.levelInfo.level,
                        exp = info.levelInfo.exp,
                        minExp = info.levelInfo.minExp,
                        nextExp = info.levelInfo.nextExp,
                        maxLevel = UserInfo.Instance.Level.maxLevel,
                        levelUpRewardCoin = info.levelInfo.levelUpRewardCoin,
                    };
                }

                UserInfo.Instance.GSN = info.gsn;
                UserInfo.Instance.FBID = info.fbId;
                UserInfo.Instance.RegistrationDate = info.registrationDate;
            }
        }

        public void RequestUser(Action callback)
        {
            Request<AnswerUser>
            (
                userService,
                "user",
                new RequestUser().UserInfo(),
                (e, answer) =>
                {
                    OnUserAsset(answer.user);

                    if (callback != null)
                        callback();
                }
            );
        }

        public void RequestUserEffect(Action callback)
        {
            Request<AnswerUserEffect>
            (
                userService,
                "effect",
                new RequestUser().UserEffect(),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        if (answer.effects != null)
                            UserInfo.Instance.UpdateEffectInfo(answer.effects);
                    }

                    if (callback != null)
                        callback();
                }
            );
        }
        
        public void RequestPiggyBank(Action callback)
        {
            Request<AnswerPiggy>
            (
                userService,
                "user/piggy",
                new RequestUser().PiggyInfo(),
                (e, answer) =>
                {
                    UserInfo.Instance.PiggyInfo = answer.userPiggy;

                    if (callback != null)
                        callback();
                }
            );
        }

        public void RequestPiggyBankReward(Action<AnswerPiggyReward> callback)
        {
            Request<AnswerPiggyReward>
            (
                userService,
                "user/piggy/reward",
                new RequestUser().PiggyReward(),
                (e, answer) =>
                {
                    if (callback != null)
                        callback(answer);
                }
            );
        }

        public void RequestStoreSales(Action callback)
        {
            Request<AnswerStore>
            (
                storeService,
                "user/store/sales",
                new RequestStore(),
                (e, answer) =>
                {
                    StoreInfo.Instance.Update(answer.store, answer.storeBonus);

                    if (callback != null)
                        callback();
                }
            );
        }

        public void RequestPurchaseValid(long id, Action<Exception, AnswerPurchaseValid> callback)
        {
            Request<AnswerPurchaseValid>
            (
                storeService,
                "user/goods/valid",
                new RequestStore().CheckValid(id),
                (e, answer) =>
                {
                    if (callback != null)
                        callback(e, answer);
                }
            );
        }

        public void RequestStoreBonus(Action<Exception> callback)
        {
            Request<AnswerStoreBonus>
            (
                storeService,
                "user/storebonus/receive",
                new RequestStore(),
                (e, answer) =>
                {
                    if (answer.storeBonus != null)
                    {
                        StoreInfo.Instance.UpdateBonus(answer.storeBonus);
                        LobbyInfo.Instance.UpdateStoreBonus(answer.storeBonus);
                    }
                    else
                    {
                        StoreInfo.Instance.UpdateBonus(null);
                        LobbyInfo.Instance.UpdateStoreBonus(null);
                    }

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestGiftInbox(Action<Exception> callback)
        {
            Request<AnswerGift>
            (
                rewardService,
                "user/inbox/gifts",
                new RequestGift().Inbox(),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        GiftInfo.Instance.Update(answer.inboxGifts);
                        GiftInfo.Instance.inboxGiftSummary = answer.inboxGiftSummary;
                        GiftInfo.Instance.InRefresh = true;
                        GiftInfo.Instance.InTime = 0.0f;

                        if (answer.resultCode == 0)
                            GiftInfo.Instance.result = true;
                        else
                            GiftInfo.Instance.result = false;
                    }
                    else
                    {
                        GiftInfo.Instance.result = false;
                    }

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestGiftOutbox(Action<Exception> callback)
        {
            Request<AnswerGift>
            (
                rewardService,
                "user/outbox/gifts",
                new RequestGift().Outbox(),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        GiftInfo.Instance.Update(answer.outboxGifts);
                        GiftInfo.Instance.outboxGiftSummary = answer.outboxGiftSummary;
                        GiftInfo.Instance.OutRefresh = true;
                        GiftInfo.Instance.OutTime = 0.0f;

                        if (answer.resultCode == 0)
                            GiftInfo.Instance.result = true;
                        else
                            GiftInfo.Instance.result = false;
                    }
                    else
                    {
                        GiftInfo.Instance.result = false;
                    }

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestMysteryGift(Action<Exception> callback)
        {
            Request<AnswerGift>
            (
                rewardService,
                "user/mysterygift",
                new RequestGift().Mysterygift(),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        GiftInfo.Instance.mysteryGift = answer.mysteryGift;

                        if (answer.resultCode == 0)
                            GiftInfo.Instance.result = true;
                        else
                            GiftInfo.Instance.result = false;
                    }
                    else
                    {
                        GiftInfo.Instance.result = false;
                    }

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestGiftReceive(long inboxSrl, Action<Exception> callback)
        {
            Request<AnswerGift>
            (
                rewardService,
                "user/inbox/gifts/receive",
                new RequestGift().Receive(inboxSrl),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        GiftInfo.Instance.Goods = answer.goods;
                        GiftInfo.Instance.MysteryGiftGoodsList = answer.mysteryGiftGoodsList;

                        if (answer.resultCode == 0)
                            GiftInfo.Instance.resultReceive = true;
                        else
                            GiftInfo.Instance.resultReceive = false;
                    }
                    else
                    {
                        GiftInfo.Instance.resultReceive = false;
                    }

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestGiftSend(string giftType, List<long> receiverGsns, bool giftBack, Action<Exception> callback)
        {
            Request<AnswerGift>
            (
                rewardService,
                "user/outbox/gifts/send",
                new RequestGift().Send(giftType, receiverGsns, giftBack),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        if (answer.resultCode == 0)
                            GiftInfo.Instance.resultSend = true;
                        else
                            GiftInfo.Instance.resultSend = false;
                    }
                    else
                    {
                        GiftInfo.Instance.resultSend = false;
                    }

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestMission(Action<Exception> callback)
        {
            Request<AnswerMission>
            (
                missionService,
                "user/mission",
                new RequestMission(),
                (e, answer) =>
                {
                    if (answer != null)
                        MissionInfo.Instance.Update(answer.userMission != null ? answer.userMission : null);

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestMissionDailyReward(Action callback)
        {
            Request<AnswerMissionDailyReward>
            (
                missionService,
                "user/mission/reward/daily",
                new RequestMission().DailyReward(),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        //MissionInfo.Instance.UpdateNextMission(answer.nextMission == null ? null : answer.nextMission);
                        MissionInfo.Instance.UpdateDailyGoodsList(answer.goodsList);
                    }

                    if (callback != null)
                        callback();
                }
            );
        }

        public void RequestMissionWeeklyReward(Action callback)
        {
            Request<AnswerMissionWeeklyReward>
            (
                missionService,
                "user/mission/reward/weekly",
                new RequestMission().WeeklyReward(),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        MissionInfo.Instance.UpdateWeeklyGoodsList(answer.goodsList);
                        //MissionInfo.Instance.UpdateRewardWeeklySet();
                    }

                    if (callback != null)
                        callback();
                }
            );
        }

        public void RequestTimeBonus(Action<Exception> callback)
        {
            Request<AnswerLobby>
            (
                rewardService,
                "user/timebonus",
                new RequestTimebonus(),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        LobbyInfo.Instance.TimeBonusData = answer.timeBonus;
                        isUpdateBonusTime = true;
                        BonusTime = 0.0f;
                    }

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestTimeBonusReceive(Action<Exception> callback)
        {
            Request<AnswerLobby>
            (
                rewardService,
                "user/timebonus/receive",
                new RequestTimebonus().Receive(),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        LobbyInfo.Instance.TimeBonusData = answer.timeBonus;

                        if (LobbyInfo.Instance.TimeBonusData != null)
                            LocalNotification.Instance.AddPushMessage(LocalNotification.PushMessageType.TimeBonus, "<b>SLOTODAY</b>", "Time Bonus! Time Bonus is ready! Don't miss your FREE COINS!", 
                                                                        LobbyInfo.Instance.TimeBonusData.standbySeconds);

                        isUpdateBonusTime = true;
                        BonusTime = 0.0f;
                    }

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestWelcomeBonusReceive(Action<Exception> callback)
        {
            Request<Answer>
            (
                rewardService,
                "user/welcomebonus/receive",
                new RequestLobby().ReceiveWelcomeBonus(),
                (e, answer) =>
                {
                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestFacebookConnectBonusReceive(Action<Exception> callback)
        {
            Request<Answer>
            (
                rewardService,
                "user/fbconnectbonus/receive",
                new RequestLobby().ReceiveFacebookConnectBonus(),
                (e, answer) => 
                {
                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestHotDealOffer(Action<Exception> callback)
        {
            Request<AnswerHotDealOffer>
            (
                storeService,
                "user/hotdeal/offers",
                new RequestHotDeal().RequestOffer(),
                (e, answer) => 
                {
                    HotDealInfo.Instance.UpdateOffer(answer.hotDealOffers != null ? answer.hotDealOffers : null);

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestHotDealBonus(long offerId, Action<Exception> callback)
        {
            Request<AnswerHotDealBonusID>
            (
                storeService,
                "user/hotdeal/bonus",
                new RequestHotDeal().RequestBonus(offerId),
                (e, answer) => 
                {
                    HotDealInfo.Instance.ClearBonus();

                    if (answer.hotDealOfferBonus != null)
                        HotDealInfo.Instance.AddBonus(answer.hotDealOfferBonus);

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestHotDealBonusReceive(long offerId, Action<Exception> callback)
        {
            Request<Answer>
            (
                storeService,
                "user/hotdeal/bonus/receive",
                new RequestHotDeal().ReceiveBonus(offerId),
                (e, answer) => 
                {
                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestBlast(Action<Exception> callback)
        {
            Request<AnswerBlast>
            (
                eventService,
                "user/event/blast",
                new RequestBlast().BlastInfo(),
                (e, answer) =>
                {
                    BlastInfo.Instance.Update(answer.blastEvent != null ? answer.blastEvent : null);

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestBlastPick(Action<bool, long, long, Exception> callback)
        {
            Request<AnswerBlastPick>
            (
                eventService,
                "user/event/blast/pick",
                new RequestBlast().BlastPick(),
                (e, answer) => 
                {
                    BlastInfo.Instance.PickCount = answer.pickCount;

                    if (callback != null)
                        callback(answer.win, answer.totalWinCoin, answer.totalSetAchieveCoin, e);
                }
            );
        }

        public void RequestAttendance(Action<Exception> callback)
        {
            Request<AnswerAttendance>
            (
                rewardService,
                "user/attendancebonus/receive",
                new RequestAttendance().ReciveAttendance(),
                (e, answer) =>
                {
                    if (answer != null)
                        AttendInfo.Instance.UpdateAttendResultInfo(answer);

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestAdStatus(Action callback)
        {
            Request<AnswerRewardAd>
            (
                rewardService,
                "ad/video/status",
                new RequestRewardAd().RequestStatus(),
                (e, answer) =>
                {
                    if (answer != null)
                    {
                        RewardAdInfo.Instance.UpdateStatus(answer);
                        TimerSystem.Instance.StartRewardAdState();
                    }

                    if (callback != null)
                        callback();
                }
            );
        }

        public void RequestAdReward(Action<Exception> callback)
        {
            Request<AnswerRewardAd>
            (
                rewardService,
                "ad/video/admob/reward",
                new RequestRewardAd().RequestReward(),
                (e, answer) =>
                {
                    if (answer != null)
                        RewardAdInfo.Instance.UpdateStatus(answer);

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void RequestRewardCoupon(string coupon, Action<Exception, AnswerRewardCoupon> callback)
        {
            Request<AnswerRewardCoupon>
            (
                rewardService,
                "coupon/reward",
                new RequestLobby().ReceiveRewardCoupon(coupon),
                (e, answer) => 
                {
                    if (callback != null)
                        callback(e, answer);
                }
            );
        }

        public void RequestFacebookFeedIssue(string feedType, Action<Exception, AnswerFacebookFeedIssue> callback)
        {
            Request<AnswerFacebookFeedIssue>
            (
                rewardService,
                "user/facebook/feed/issue",
                new RequestFacebookFeed().Issue(feedType),
                (e, answer) => 
                {
                    if (callback != null)
                        callback(e, answer);
                }
            );
        }

        public void RequestFacebookFeedReward(string shareCode, Action<Exception, AnswerFacebookFeedReward> callback)
        {
            Request<AnswerFacebookFeedReward>
            (
                rewardService,
                "user/facebook/feed/reward/receive",
                new RequestFacebookFeed().Reward(shareCode),
                (e, answer) => 
                {
                    if (callback != null)
                        callback(e, answer);
                }
            );
        }

        public void RequestQuest(Action<Exception> callback)
        {
            Request<AnswerQuest>
            (
                missionService,
                "user/quest",
                new RequestQuest().QuestInfo(),
                (e, answer) => 
                {
                    QuestInfo.Instance.Update(answer.quests);
                    QuestInfo.Instance.QuestEndDate = answer.questEndDate;

                    if (callback != null)
                        callback(e);
                }
            );
        }

        public void OnNotiGift(object data)
        {
            NtfGift giftNoti = data as NtfGift;

            Debug.Log("gsn : " + giftNoti.gsn);
            Debug.Log("coinGiftCount : " + giftNoti.coinGiftCount);
            Debug.Log("mysteryGiftCount : " + giftNoti.mysteryGiftCount);
            Debug.Log("pubDate : " + giftNoti.pubDate);

            // 잘못된 gsn으로 노티가 왔을때 무시
            if (giftNoti.gsn != UserInfo.Instance.GSN)
                return;

            LobbyInfo.Instance.InboxGiftSummary.totalGiftCount += giftNoti.coinGiftCount;
            LobbyInfo.Instance.InboxGiftSummary.totalGiftCount += giftNoti.mysteryGiftCount;
            if (ActionUpdateGift != null)
                ActionUpdateGift();

            if (PopupSystem.Instance.IsPopupOpened("POPUP_Gift") == true)
            {
                if (GiftInfo.Instance.updateCallback != null)
                    GiftInfo.Instance.updateCallback();
            }
        }

        public void OnNotiMaintenance(object data)
        {
            NtfMaintenance noti = data as NtfMaintenance;

            if (noti.location != "All" && noti.location != "Lobby")
                return;

            if (noti.type == "Notice")
            {
                //PopupNoti.Open(noti.message);
                NotiSystem.Instance.Show(noti.message);
            }
            else if (noti.type == "Maintenance")
            {
                PopupMaintenance.Open(PopupMaintenance.Type.ALL, () =>
                {
                    Application.Quit();
                });
            }
        }

        public void OnNotiWorldMessage(object data)
        {
            NtfWorldMessage noti = data as NtfWorldMessage;
            if (noti == null)
                return;

            NotiSystem.Instance.ShowWorldMessage(noti);
        }

        public void OnNotiUserCoin(object data)
        {
            NtfUserCoin noti = data as NtfUserCoin;

            if (noti == null)
                return;


            //TODO:: 재작업 필요
            //UserInfo.Instance.Coin = noti.afterCoin;
            //if (UserInfo.Instance.UpdateCoin == true)
            //{
            //    //TODO:: 노티가 더 늦게오면 그때는 그냥 코인으로 반영
            //    UserInfo.Instance.Coin = noti.afterCoin;
            //}
            //else
            {
                UserInfo.Instance.NtfUserCoinData = noti;
            }
        }

        public void OnNotiUserEffect(object data)
        {
            //TODO:: API 형태로 바뀌기로 했기 때문에 변경됨.
            //NtfUserEffect noti = data as NtfUserEffect;
            //if (noti == null)
            //    return;

            // TODO:: 이펙트 갱신
            Log("NOTI USER EFFECT");

            RequestUserEffect(() =>
            {
                Log("Update User Effect");
            });
        }

        public void Close()
        {
            Log();

            // TODO: ...
        }
    }
}
