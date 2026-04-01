using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CAPYBARA.badugi;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using BetSizeType = CAPYBARA.Partial.BetSizeType;
using Common;
namespace CAPYBARA
{
    public enum BadugiState
    {
        None = -5,
        Idle = 0,
        Start,
        PreMorning,
        Morning,
        Lunch,
        Evening,
        AfterEvening,
        ShowDown,
        Result,
        End,
    }

    public enum ChangeActionType
    {
        Pass,
        Change,
        End
    }

    public class BadugiController:IInGameController
    {
        private BadugiViewer view;
        private BadugiTableSnapShot snapShot;

        private GameObject mainObject;
        public BadugiState badugiState;

        private Dictionary<int, BadugiPlayerController> playerDict = new Dictionary<int, BadugiPlayerController>();

        private bool TryGetPlayer(int chairId, out BadugiPlayerController player)
            => playerDict.TryGetValue(chairId, out player);

        private int myChairId;
        badugi.Player myPlayer;

        private Partial.BetSizeType _bettingActionToggleState;
        private Partial.ActionType actionStateForServer;
        private ChangeActionType _changeActionToggleState;

        private bool isActionProcessing = false;
        private bool isChangeActionProcessing = false;

        private bool isActedInMyturn = false;

        private badugi.Phase phaseState = Phase.PhNone;
        private DrawPhase drawPhaseState = DrawPhase.DpNone;
        private bool isDdadangActedThisRound = false;

        private bool thisBetRoundAlreadyActed = false;
        private bool thisRoundChangeAlreadyActed = false;


        private Dictionary<Partial.BetSizeType, long> betAmountDict =
            new Dictionary<Partial.BetSizeType, long>();

        private bool reserveLeaveRequest = false;
        private bool reserveMoveRoomRequest = false;

        private bool isExiled = false;
        private readonly Queue<Action> serverWaitQ = new();

        private PlayerActionHistory currentRoundActionHistory = new PlayerActionHistory();

        private Action<GameType, EmotionInfo> _onEmotionExpress;
        private Action<bool> _onEmojiUseChange;
        private Action<bool> _onJokboUseChange;

        private IGameRuleProvider ruleProvider;
        private List<GameRuleConfig> currentGameRules;
        private GameRuleConfig currentRoundGameRule;
        int ruleProvideindex = 0;

        public BadugiController(GameObject UpdateObject, BadugiViewer _view, CancellationTokenSource cts, IGameRuleProvider provider)
        {
            mainObject = UpdateObject;
            view = _view;
            Init();

            ruleProvider = provider;
            currentGameRules = ruleProvider.GetRuleConfig(GameType.LOW_BADUGI);
            currentRoundGameRule = currentGameRules[0];

            RegisterCallbackNoti();

            CPPlayer.Server.CallbackAfterBadugiConnect += RegisterCallbackNoti;
            CPPlayer.InGame.AFKPopupActive += MeUserBackFromInactive;

            AFKUserDetector().Forget();
        }

        public void StartSet()
        {
        }

        public void Dispose()
        {
            BadugiDispatchPushHub.OnEnterNoti -= EnterGameOtherPlayer_Noti;
            BadugiDispatchPushHub.OnReadyNoti -= ReadyGame_Noti;
            BadugiDispatchPushHub.OnLeaveNoti -= LeaveGameOtherPlayer_Noti;
            BadugiDispatchPushHub.OnStartNoti -= StartGame;
            BadugiDispatchPushHub.OnHoleCardNoti -= HoleCardNoti;
            BadugiDispatchPushHub.OnHoleCardNotiOther -= HoleCardNotiOther;
            BadugiDispatchPushHub.OnTurnNoti -= TurnChangedNoti;
            BadugiDispatchPushHub.OnActionNoti -= AcionNoti;
            BadugiDispatchPushHub.OnDrawTurnNoti -= DrawTurnChangedNoti;
            BadugiDispatchPushHub.OnDrawNoti -= DrawActionNoti;
            BadugiDispatchPushHub.OnShowdownNoti -= ShowDownNoti;
            BadugiDispatchPushHub.OnResultNoti -= ResultNoti;
            BadugiDispatchPushHub.OnKickedNoti -= KickedForSomeReason;
            BadugiDispatchPushHub.OnEmoteNoti -= EmoticonExpressNoti;
            BadugiDispatchPushHub.OnCardOpenNoti -= CardOpenNoti;
            BadugiDispatchPushHub.OnLeaveReserveNoti -= LeaveReservedNoti;

            CPPlayer.Server.CallbackAfterBadugiConnect -= RegisterCallbackNoti;
            CPPlayer.InGame.AFKPopupActive -= MeUserBackFromInactive;
            CPPlayer.Badugi.EnterRoom -= EnterGameTable_Result;
            CPPlayer.Option.ReserveBetChange -= ToggleViewSetForReserveBetOption;
            CPPlayer.Badugi.CardTouchCallback2 -= CallbackUserTouchCard;
            CPPlayer.InGame.emotionExpressEvent -= _onEmotionExpress;
            CPPlayer.Option.EmojiUseChange -= _onEmojiUseChange;
            CPPlayer.Option.JokboUseChange -= _onJokboUseChange;
        }

        private void RegisterCallbackNoti()
        {
            BadugiDispatchPushHub.OnEnterNoti -= EnterGameOtherPlayer_Noti;
            BadugiDispatchPushHub.OnReadyNoti -= ReadyGame_Noti;
            BadugiDispatchPushHub.OnLeaveNoti -= LeaveGameOtherPlayer_Noti;
            BadugiDispatchPushHub.OnStartNoti -= StartGame;
            BadugiDispatchPushHub.OnHoleCardNoti -= HoleCardNoti;
            BadugiDispatchPushHub.OnHoleCardNotiOther -= HoleCardNotiOther;
            BadugiDispatchPushHub.OnTurnNoti -= TurnChangedNoti;
            BadugiDispatchPushHub.OnActionNoti -= AcionNoti;
            BadugiDispatchPushHub.OnDrawTurnNoti -= DrawTurnChangedNoti;
            BadugiDispatchPushHub.OnDrawNoti -= DrawActionNoti;
            BadugiDispatchPushHub.OnShowdownNoti -= ShowDownNoti;
            BadugiDispatchPushHub.OnResultNoti -= ResultNoti;
            BadugiDispatchPushHub.OnKickedNoti -= KickedForSomeReason;
            BadugiDispatchPushHub.OnEmoteNoti -= EmoticonExpressNoti;
            BadugiDispatchPushHub.OnCardOpenNoti -= CardOpenNoti;
            BadugiDispatchPushHub.OnLeaveReserveNoti -= LeaveReservedNoti;


            BadugiDispatchPushHub.OnEnterNoti += EnterGameOtherPlayer_Noti;
            BadugiDispatchPushHub.OnReadyNoti += ReadyGame_Noti;
            BadugiDispatchPushHub.OnLeaveNoti += LeaveGameOtherPlayer_Noti;
            BadugiDispatchPushHub.OnStartNoti += StartGame;
            BadugiDispatchPushHub.OnHoleCardNoti += HoleCardNoti;
            BadugiDispatchPushHub.OnHoleCardNotiOther += HoleCardNotiOther;

            BadugiDispatchPushHub.OnTurnNoti += TurnChangedNoti;
            BadugiDispatchPushHub.OnActionNoti += AcionNoti;

            BadugiDispatchPushHub.OnDrawTurnNoti += DrawTurnChangedNoti;
            BadugiDispatchPushHub.OnDrawNoti += DrawActionNoti;

            BadugiDispatchPushHub.OnShowdownNoti += ShowDownNoti;
            BadugiDispatchPushHub.OnResultNoti += ResultNoti;

            BadugiDispatchPushHub.OnKickedNoti += KickedForSomeReason;
            BadugiDispatchPushHub.OnEmoteNoti += EmoticonExpressNoti;

            BadugiDispatchPushHub.OnCardOpenNoti += CardOpenNoti;
            BadugiDispatchPushHub.OnLeaveReserveNoti += LeaveReservedNoti;
        }

        public void Init()
        {
            ChangeGamestate(BadugiState.None);
            snapShot = new BadugiTableSnapShot();
            snapShot.Init(view);
            phaseState = Phase.PhNone;
            drawPhaseState= DrawPhase.DpNone;
            
            CPPlayer.Badugi.EnterRoom += EnterGameTable_Result;
            betAmountDict.Clear();

            for (int i = 0; i < (int)Partial.BetSizeType.BsEnd; i++)
            {
                betAmountDict.Add((Partial.BetSizeType)i, 0);
            }

            myPlayer = new Player()
            {
                Uid = CPPlayer.Badugi.ingameUid,
                Nick = CPPlayer.UserInfo.userDatabase.User.Nick,
            };
            _bettingActionToggleState = Partial.BetSizeType.BsEnd;

            view.optionBtn.onClick.AddListener(() =>
            {
                var optionWindow = ViewCanvas.Get<ViewCanvasInGame>().ingameOptionWindow_badugi;
                if (ViewCanvas.Get<ViewCanvasInGame>().ingameOptionWindow_badugi.gameObject.activeInHierarchy == false)
                {
                    optionWindow.OpenWindow();
                    view.OpenModalObject(optionWindow.gameObject);
                }
                else
                {
                    ViewCanvas.Get<ViewCanvasInGame>().ingameOptionWindow_badugi.CloseWindow();
                    view.OnModalAutoClose(optionWindow.gameObject);
                }
            });
            view.showEmoticonViewBtn.onClick.AddListener(() =>
            {
                bool active = view.emotionView.gameObject.activeInHierarchy;
                view.emotionView.ActiveWindow(!active);
                if (active)
                {
                    view.OnModalAutoClose(view.emotionView.gameObject);
                }
                else
                {
                    view.OpenModalObject(view.emotionView.gameObject);
                }
            });

            view.openJokboWindowBtn.onClick.AddListener(() =>
            {
                if (view.jokboWindow.activeInHierarchy)
                {
                    view.jokboWindow.SetActive(false);
                    view.OnModalAutoClose(view.jokboWindow);
                }
                else
                {
                    view.jokboWindow.SetActive(true);
                    view.OpenModalObject(view.jokboWindow.gameObject);
                }
            });
            view.closeJokboWindowBtn.onClick.AddListener(() =>
            {
                view.jokboWindow.SetActive(false);
                view.OnModalAutoClose(view.jokboWindow);
            });
            view.readyBtn.onClick.AddListener(ClickReadyBtn);


            view.InitDisplay();

            CPPlayer.Option.ReserveBetChange += ToggleViewSetForReserveBetOption;
            _onEmotionExpress = (o, t) => { EmoticonExpressReq(t).Forget(); };
            CPPlayer.InGame.emotionExpressEvent += _onEmotionExpress;
            CPPlayer.Badugi.CardTouchCallback2 += CallbackUserTouchCard;

            //option callback
            _onEmojiUseChange = (active) =>
            {
                view.showEmoticonViewBtn.gameObject.SetActive(active);
                if (active == false)
                {
                    view.EmotionViewClose();
                }
            };
            CPPlayer.Option.EmojiUseChange += _onEmojiUseChange;

            _onJokboUseChange = (active) =>
            {
                view.openJokboWindowBtn.gameObject.SetActive(active);
                if (active == false)
                {
                    view.JokboViewClose();
                }
            };
            CPPlayer.Option.JokboUseChange += _onJokboUseChange;
            
            view.dayRoundText.gameObject.SetActive(false);

            ViewActionListenerSet();
        }

        void ViewActionListenerSet()
        {
            view.OnActionTogglePressed += ActionTogglePressed;
            view.OnCardActionTogglePressed += CardDrawRequest;
            view.OnLeaveBtnPressed += () => LeaveThisRoomOrReserve().Forget();
            view.OnCancelLeaveBtnPressed += () => CancelReserveLeave().Forget();

            view.OnMoveBtnPressed += () => LeaveThisRoomAndMoveOtherRoomOrReserve().Forget();
            view.OnCancelMoveBtnPressed += () => CancelReserveMove().Forget();
        }

      
        
        public void OnOtherPlayerModalInactive(int chairId)
        {
            foreach (var badugiPlayerController in playerDict)
            {
                if (badugiPlayerController.Value.chairId == chairId)
                    continue;
                badugiPlayerController.Value.InfoModalInactive();
            }
        }

        async UniTask AFKUserDetector()
        {
            while (true)
            {
                if (badugiState != BadugiState.End && badugiState != BadugiState.Idle && badugiState != BadugiState.None)
                {
                    if (CPPlayer.InGame.isUserAFK)
                    {
                        if (CPPlayer.InGame.AFKPopupActiveFlag == false)
                        {
                            CPPlayer.InGame.AFKPopupActive?.Invoke(true);
                        }
                    }
                }


                await UniTask.Yield();
            }
        }


        public EnterRes enterresInfo;

        private void EnterGameTable_Result(badugi.EnterRes enterRes)
        {
            view.emotionView.Init();
            foreach (var data in enterRes.Config)
            {
                CPPlayer.Server.visualEffectTimeConfig[data.Key] = data.Value;
            }

            enterresInfo = enterRes;
            if (enterRes.Pots.Count > 0)
            {
                view.currentPotAmount.text = Extension.ToKoreanFormat(enterRes.Pots[0], Extension.KoreanFormatMode.Planning);
            }
            else
            {
                view.currentPotAmount.text = "0";
            }
            
            ChangeGamestate(BadugiState.Idle);
   
            InitializeOnEnter();
            CPPlayer.Badugi.currentTableId = enterRes.TableId;
            Debug.Log($"current table id: {CPPlayer.Badugi.currentTableId}");
            SetPlayersInfo();
            BossBtnSet();
       
            bool isTwovs = CPPlayer.InGame.currentGameMode == GameMode.TwoVS;
            int playercount = 0;
            foreach (var player in playerDict)
            {
                if (player.Value.view.gameObject.activeInHierarchy)
                {
                    playercount++;
                }
            }

            bool playerfull = playercount >= 2;
            view.readyBtn.gameObject.SetActive(isTwovs && playerfull);
        }

        void ClickReadyBtn()
        {
            ReadyAndWaitStart().Forget();
        }

        async UniTask ReadyAndWaitStart()
        {
            var readyRes = await Services.Badugi.ReadyReqAsync(CPPlayer.Badugi.currentTableId, true);
            if (readyRes.IsSuccess == false)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ServerResponseFailed].StringToLocal}\n{readyRes.Error}"));
                return;
            }
            else
            {
                if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                    return;
                //서버 수신 순서때문에 예외처리
                if (badugiState >= BadugiState.Start && badugiState <= BadugiState.Result)
                {
                    view.readyBtn.gameObject.SetActive(false);
                    myPlayerCtrl.view.readyCompleteObj.SetActive(false);
                }
                else
                {
                    view.readyBtn.gameObject.SetActive(false);
                    myPlayerCtrl.view.readyCompleteObj.SetActive(true);
                }
            }
        }

        public void SetPlayersInfo()
        {
            var mychair = enterresInfo.Chairs.FirstOrDefault(o => o.Id == enterresInfo.ChairId);
            if (mychair != null)
            {
                myPlayer = mychair.Player;
            }

            BadugiPlayerController me;
            if (playerDict.ContainsKey(enterresInfo.ChairId))
            {
                me = playerDict[enterresInfo.ChairId];
                me.SetPlayer(myPlayer, enterresInfo.ChairId, true);
            }
            else
            {
                me = new BadugiPlayerController(mainObject.transform, view,this);
                me.SetPlayer(myPlayer, enterresInfo.ChairId, true);
                playerDict.Add(enterresInfo.ChairId, me);
            }

            myChairId = enterresInfo.ChairId;
            Extension.eLog($"mychair ID: {myChairId}", Color.magenta);

            foreach (var enterResChair in enterresInfo.Chairs)
            {
                if (enterResChair.Player == null)
                    continue;
                if (enterResChair.Id == myChairId)
                    continue;
                BadugiPlayerController other;
                if (playerDict.ContainsKey(enterResChair.Id))
                {
                    other = playerDict[enterResChair.Id];
                    other.SetPlayer(enterResChair.Player, enterResChair.Id);
                }
                else
                {
                    other = new BadugiPlayerController(mainObject.transform, view,this);
                    other.SetPlayer(enterResChair.Player, enterResChair.Id);
                    playerDict.Add(enterResChair.Id, other);
                }

                if (enterResChair.Player.CardCount > 0 && enterresInfo.InGame)
                { 
                    for (int i = 0; i < enterResChair.Player.CardCount; i++)
                    {
                        snapShot.SetCardToOtherPlayerAtEnter(other);
                    }
                }

                if (enterResChair.Player.DrawCounts.Count > 0)
                {
                    for (int i = 0; i < enterResChair.Player.DrawCounts.Count; i++)
                    {
                        int index = i;
                        int num = enterResChair.Player.DrawCounts[i];
                        if (num > 0)
                        {
                            other.view.roundInfo[index].SetNum(num);
                        }
                    }
                }

                bool isTwovs = CPPlayer.InGame.currentGameMode == GameMode.TwoVS;

                if (isTwovs)
                {
                    if (enterResChair.Player.IsReady)
                    {
                        other.view.readyCompleteObj.SetActive(true);
                    }
                }
            }
        }


        private void EnterGameOtherPlayer_Noti(EnterNoti enterNoti, int revisionId)
        {
            if (badugiState < BadugiState.Start)
            {
                CPPlayer.Badugi.twoVSOpponentViewIndex = UnityEngine.Random.Range(0, 2) == 0 ? 2 : 3;  
            }
            BadugiPlayerController other;
            if (playerDict.ContainsKey(enterNoti.ChairId))
            {
                other = playerDict[enterNoti.ChairId];
                other.SetPlayer(enterNoti.Player, enterNoti.ChairId);
            }
            else
            {
                other = new BadugiPlayerController(mainObject.transform, view,this);
                other.SetPlayer(enterNoti.Player, enterNoti.ChairId);
                playerDict.Add(enterNoti.ChairId, other);
            }
            
            BossBtnSet();
            
            bool isTwovs = CPPlayer.InGame.currentGameMode == GameMode.TwoVS;
            int playercount = 0;
            foreach (var player in playerDict)
            {
                if (player.Value.view.gameObject.activeInHierarchy)
                {
                    playercount++;
                }
            }

            bool playerfull = playercount >= 2;
            view.readyBtn.gameObject.SetActive(isTwovs && playerfull);
        }

        private void ReadyGame_Noti(ReadyNoti enterNoti, int revisionId)
        {
            if (badugiState >= BadugiState.Start && badugiState <= BadugiState.Result)
                return;
            int chairId = enterNoti.ChairId;

            if (CPPlayer.InGame.currentGameMode == GameMode.TwoVS)
            {
                if (playerDict.ContainsKey(chairId))
                {
                    playerDict[chairId].view.readyCompleteObj.SetActive(true);
                }
            }
        }

        private void LeaveGameOtherPlayer_Noti(LeaveNoti leaveNoti, int revisionId)
        {
            if (playerDict.ContainsKey(leaveNoti.ChairId))
            {
                if (leaveNoti.ChairId == myChairId)
                {
                    if (leaveNoti.Reason == KickReason.KrNone)
                    {
                        if (reserveMoveRoomRequest)
                        { 
                            CPPlayer.InGame.MoveTable?.Invoke(GameType.LOW_BADUGI);
                          
                        }
                        else
                        {
                            CPPlayer.Badugi.currentTableId = 0;
                            CPPlayer.InGame.LeaveGame?.Invoke(GameType.LOW_BADUGI);
                        }
                    }
                    else
                    {
                        CPPlayer.Badugi.currentTableId = 0;
                        CPPlayer.InGame.LeaveGame?.Invoke(GameType.LOW_BADUGI);
                    }

                    view.leaveBtn.enabled = false;
                    view.moveRoomBtn.enabled = false;
                    LeaveGameDataInitialize();
                }
                else
                {
                    playerDict[leaveNoti.ChairId].RemovePlayer();
                }
            }

            foreach (var player in playerDict)
            {
                if (leaveNoti.BossId == player.Value.chairId)
                {
                    player.Value.view.dealerBtnObj.SetActive(true);
                }
                else
                {
                    player.Value.view.dealerBtnObj.SetActive(false);
                }
            }

            if (CPPlayer.InGame.currentGameMode == GameMode.TwoVS)
            {
                if (leaveNoti.ChairId != myChairId)
                {
                    if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                        myPlayerCtrl.view.readyCompleteObj.SetActive(false);
                    view.readyBtn.gameObject.SetActive(true);
                }
            }

            bool isTwovs = CPPlayer.InGame.currentGameMode == GameMode.TwoVS;
            int playercount = 0;
            foreach (var player in playerDict)
            {
                if (player.Value.view.gameObject.activeInHierarchy)
                {
                    playercount++;
                }
            }

            bool playerfull = playercount >= 2;
            view.readyBtn.gameObject.SetActive(isTwovs && playerfull);
        }

        private StartNoti startNotiInfo;

        private void StartGame(StartNoti startNoti, int revisionId)
        {
            startNotiInfo = startNoti;
            foreach (var data in startNotiInfo.Config)
            {
                CPPlayer.Server.visualEffectTimeConfig[data.Key] = data.Value;
            }

            int currentRid = revisionId;

            ChangeGamestate(BadugiState.Start);
            InitializeOnGameStart();
            
            serverWaitQ.Enqueue(() => PresentAtStartSetAfterFrame(currentRid));

            PresentAtStartSet(revisionId).Forget();
        }

        async UniTask PresentAtStartSet(int revisionId)
        {
            await UniTask.NextFrame();

            var _action = serverWaitQ.Dequeue();
            _action?.Invoke();
        }

        void PresentAtStartSetAfterFrame(int revisionId)
        {
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            view.currentPotAmount.text = Extension.ToKoreanFormat(startNotiInfo.PotAmount, Extension.KoreanFormatMode.Planning);


            myPlayerCtrl.SetCurrentPhase(BadugiState.Start);
            myPlayerCtrl.SetCardOpenBtn();
            myPlayerCtrl.SetFold(false);
            myPlayerCtrl.SetAllin(false);

            //player setting
            foreach (var startNotiPlayer in startNotiInfo.Players)
            {
                if (!TryGetPlayer(startNotiPlayer.ChairId, out var startPlayer))
                    continue;
                //ante수집
                startPlayer.BetAnte(startNotiPlayer.Ante, startNotiPlayer.Chip);
                if (startNotiPlayer.ChairId == myChairId)
                {
                    startPlayer.SetTotalBet(startNotiPlayer.Ante);
                    CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                    CPPlayer.InGame.haveKickVote = startNotiPlayer.CanKickVote;
                }
            }
        }


        private void HoleCardNoti(HoleCardNoti myCardnoti, int revisionId)
        {
            int currentRid = revisionId;
            serverWaitQ.Enqueue(() => HolecardNotiPresentAfterFrame(myCardnoti, currentRid));
            HolecardNotiPresent(myCardnoti, revisionId).Forget();
        }

        async UniTask HolecardNotiPresent(badugi.HoleCardNoti myCardnoti, int revisionId)
        {
            await UniTask.NextFrame();

            var _action = serverWaitQ.Dequeue();
            _action?.Invoke();
        }

        void HolecardNotiPresentAfterFrame(badugi.HoleCardNoti myCardnoti, int revisionId)
        {
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            if (revisionId != BadugiDispatchPushHub.revisionId)
            {
                snapShot.CardLocateToPlayerSnapshot(myPlayerCtrl, myCardnoti);
            }
            else
            {
                snapShot.CardThrowToPlayer(myPlayerCtrl, myCardnoti);
            }
        }

        private void HoleCardNotiOther(HoleCardNotiOther otherCardnoti, int revisionId)
        {
            int currentRid = revisionId;

            serverWaitQ.Enqueue(() => HoleCardNotiOtherPresentAfterFrame(otherCardnoti, currentRid));
            HolecardNotiOtherPresent(otherCardnoti, revisionId).Forget();
        }

        async UniTask HolecardNotiOtherPresent(HoleCardNotiOther otherCardnoti, int revisionId)
        {
            await UniTask.NextFrame();

            var _action = serverWaitQ.Dequeue();
            _action?.Invoke();
        }

        void HoleCardNotiOtherPresentAfterFrame(HoleCardNotiOther otherCardnoti, int revisionId)
        {
            if (!TryGetPlayer(otherCardnoti.ChairId, out var otherCardPlayer))
                return;
            if (revisionId != BadugiDispatchPushHub.revisionId)
            {
                snapShot.CardThrowToOtherPlayerSnapshot(otherCardPlayer, otherCardnoti);
            }
            else
            {
                snapShot.CardThrowToPlayer(otherCardPlayer, otherCardnoti);
            }
        }

        bool isMyTurn = false;

        private badugi.TurnNoti myTurnNotiInfo;
        private badugi.TurnNoti currentTurnNotiInfo;

        private void TurnChangedNoti(TurnNoti turnNoti, int revisionId)
        {
            int currentRid = revisionId;
            serverWaitQ.Enqueue(() => TurnNotiPresentAfterFrame(turnNoti, currentRid));
            TurnNotiPresent(turnNoti, revisionId).Forget();
        }

        async UniTask TurnNotiPresent(TurnNoti turnNoti, int revisionId)
        {
            await UniTask.NextFrame();
            var _action = serverWaitQ.Dequeue();
            _action?.Invoke();
        }

        void TurnNotiPresentAfterFrame(TurnNoti turnNoti, int revisionId)
        {
            if (!TryGetPlayer(turnNoti.ChairId, out var turnPlayer))
                return;
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            if (CPPlayer.InGame.currentGameMode != GameMode.TwoVS)
            {
                if (turnPlayer.isAllin)
                    return;
            }


            currentTurnNotiInfo = turnNoti;
            isMyTurn = currentTurnNotiInfo.ChairId == myChairId;

            view.actionToggleObject.SetActive(true);
            view.cardactionToggleParent.SetActive(false);

            if (currentTurnNotiInfo.Phase == Phase.PhEvening)
            {
                for (int i = 0; i < myPlayerCtrl.cardViewerList.Count; i++)
                {
                    var cardview = myPlayerCtrl.cardViewerList[i];
                    myPlayerCtrl.TouchedCardforChangeSetAtEvening(cardview.cardInfoIndex, false);
                }
            }

            ruleProvideindex = 0;
            switch (turnNoti.Phase)
            {
                case Phase.PhPreMorning:
                    ChangeGamestate(BadugiState.PreMorning);
                    ruleProvideindex = 0;
                    break;
                case Phase.PhMorning:
                    ChangeGamestate(BadugiState.Morning);
                    ruleProvideindex = 1;
                    break;
                case Phase.PhLunch:
                    ChangeGamestate(BadugiState.Lunch);
                    ruleProvideindex = 2;
                    break;
                case Phase.PhEvening:
                    ChangeGamestate(BadugiState.AfterEvening);
                    ruleProvideindex = 3;
                    break;
            }

            currentRoundGameRule = currentGameRules[ruleProvideindex];

            if (isMyTurn)
            {
                myPlayerCtrl.SetTotalBet(turnNoti.TotalBet);
                myTurnNotiInfo = turnNoti;

            }

            turnPlayer.BetImageActive(false);

            //turn넘어가다가 phase 바꾸면 바꿔줌 
            if (phaseState != turnNoti.Phase)
            {
                currentRoundActionHistory.ResetForNewRound();
                phaseState = turnNoti.Phase;
                //DrawTurnEventStartSet();
                isDdadangActedThisRound = false;
                thisBetRoundAlreadyActed = false;
                
                _changeActionToggleState = ChangeActionType.End;
                Extension.eLog("턴 바뀜", Color.yellow);
                foreach (var badugiPlayerController in playerDict)
                {
                    badugiPlayerController.Value.BetImageActive(false);
                    badugiPlayerController.Value.ClearCurrentRoundBetHistory();
                }

                _changeActionToggleState = ChangeActionType.End;
            }
    

            if (revisionId != BadugiDispatchPushHub.revisionId)
            {
                if (isMyTurn && thisBetRoundAlreadyActed)
                {
                    thisBetRoundAlreadyActed = false;
                }

                //toggle view 세팅
                if (thisBetRoundAlreadyActed == false)
                {
                    Extension.eLog($"TotalBet:{turnNoti.TotalBet}/CallChip:{turnNoti.CallChip}/maxBet:{Constraints.MaxBetChip}", Color.green);
                    ToggleViewAndBetAmountSetting(turnNoti, turnNoti.ChairId == myChairId, thisBetRoundAlreadyActed);
                }

                var startTime = turnNoti.Ts.ToDateTime();
                //playerDict[turnNoti.ChairId].ActivateTurn(startTime,isMyTurn);

                isActionProcessing = false;
                //액션 토글 설정
                if (isMyTurn)
                {
                    if (_bettingActionToggleState != Partial.BetSizeType.BsEnd) //미리 toggle 클릭해놈
                    {
                        if (view.actionToggles[(int)_bettingActionToggleState].toggle.interactable)
                        {
                            view.actionToggles[(int)_bettingActionToggleState].toggle.SetIsOnWithoutNotify(false);
                        }
                    }

                    _bettingActionToggleState = Partial.BetSizeType.BsEnd;
                }
            }
            else
            {
                if (isMyTurn && thisBetRoundAlreadyActed)
                {
                    thisBetRoundAlreadyActed = false;
                }

                //toggle view 세팅
                if (thisBetRoundAlreadyActed == false)
                {
                    Extension.eLog($"TotalBet:{turnNoti.TotalBet}/CallChip:{turnNoti.CallChip}/maxBet:{Constraints.MaxBetChip}", Color.green);
                    ToggleViewAndBetAmountSetting(turnNoti, turnNoti.ChairId == myChairId, thisBetRoundAlreadyActed);
                }

                var startTime = turnNoti.Ts.ToDateTime();
                turnPlayer.ActivateTurn(startTime, isMyTurn);

                isActionProcessing = false;
                //액션 토글 설정
                if (isMyTurn)
                {
                    if (_bettingActionToggleState != Partial.BetSizeType.BsEnd) //미리 toggle 클릭해놈
                    {
                        if (view.actionToggles[(int)_bettingActionToggleState].toggle.interactable)
                        {
                            ActionProcessToggle(actionStateForServer, _bettingActionToggleState, view.actionToggles[(int)_bettingActionToggleState].toggle,
                                true).Forget();
                        }
                        else
                        {
                            _bettingActionToggleState = Partial.BetSizeType.BsEnd;
                        }
                    }

                    if (CPPlayer.Cloud.optionValue.myTurnViberate)
                    {
#if UNITY_ANDROID || UNITY_IOS
                        Handheld.Vibrate();
#endif
                    }
                }
            }
        }


        private void CallbackUserTouchCard()
        {
            if (thisRoundChangeAlreadyActed)
                return;
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            bool canNotChange = myPlayerCtrl.touchedCardIndexList.Count == 0;
            view.changeInactive.SetActive(canNotChange);
            view.changeToggle.toggle.interactable = !canNotChange;
            view.passToggle.toggle.SetIsOnWithoutNotify(false);
            view.changeToggle.toggle.SetIsOnWithoutNotify(false);

            if (canNotChange)
            {
                _changeActionToggleState = ChangeActionType.End;
            }
        }

        private DrawTurnNoti currentDrawTurnNotiInfo;

        private void DrawTurnChangedNoti(DrawTurnNoti drawTurnNoti, int revisionId)
        {
            int currentRid = revisionId;
            serverWaitQ.Enqueue(() => DrawTurnChangedNotiPresentAfterFrame(drawTurnNoti, currentRid));
            DrawTurnChangedNotiPresent(drawTurnNoti, revisionId).Forget();
        }

        async UniTask DrawTurnChangedNotiPresent(DrawTurnNoti drawTurnNoti, int revisionId)
        {
            await UniTask.NextFrame();
            var _action = serverWaitQ.Dequeue();
            _action?.Invoke();
        }

        private void DrawTurnChangedNotiPresentAfterFrame(DrawTurnNoti drawTurnNoti, int revisionId)
        {
            if (!TryGetPlayer(drawTurnNoti.ChairId, out var drawTurnPlayer))
                return;
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            currentDrawTurnNotiInfo = drawTurnNoti;
            isMyTurn = drawTurnNoti.ChairId == myChairId;

            if (!myPlayerCtrl.isObserving&&!myPlayerCtrl.isFolded)
            {
                view.passToggle.gameObject.SetActive(true);
                view.changeToggle.gameObject.SetActive(true);

                view.actionToggleObject.SetActive(false);
                view.cardactionToggleParent.SetActive(true);
            }
                
        

            if (isMyTurn)
            {
                //myPlayerCtrl.BetImageActive(false);
            }

            bool isdrawturnchanged = drawPhaseState != drawTurnNoti.Phase;

            if (drawPhaseState != drawTurnNoti.Phase)
            {
                drawPhaseState= drawTurnNoti.Phase;
                thisRoundChangeAlreadyActed = false;
                Extension.eLog("드로우 턴 바뀜", Color.yellow);

                RoundImageActiveAnimation(drawPhaseState).Forget();

                foreach (var badugiPlayerController in playerDict)
                {
                    badugiPlayerController.Value.SetDrawActionText(0,false);
                }
            }

            foreach (var badugiPlayerController in playerDict)
            {
                badugiPlayerController.Value.BetImageActive(false);
            }

            Extension.eLog($"mychair ID: {myChairId} draw turn chairId:{drawTurnNoti.ChairId}.", Color.cyan);

            if (isMyTurn)
            {
                bool canNotChange = myPlayerCtrl.touchedCardIndexList.Count == 0;
                view.changeInactive.SetActive(canNotChange);
                view.passInactive.SetActive(false);
                view.changeToggle.toggle.interactable = !canNotChange;
            }
            else
            {
                if (CPPlayer.Cloud.optionValue.reserveBet)
                {
                    if (thisRoundChangeAlreadyActed)
                    {
                        view.changeInactive.SetActive(true);
                        view.passInactive.SetActive(true);
                    }
                    else
                    {
                        bool canNotChange = myPlayerCtrl.touchedCardIndexList.Count == 0;
                        view.changeInactive.SetActive(canNotChange);
                        view.changeToggle.toggle.interactable = !canNotChange;
                        view.passInactive.SetActive(false);
                    }
                }
                else
                {
                    view.changeInactive.SetActive(true);
                    view.passInactive.SetActive(true);
                }
            }

            var startTime = drawTurnNoti.Ts.ToDateTime();
            drawTurnPlayer.ActivateTurn(startTime, isMyTurn);

            isChangeActionProcessing = false;

            //DayRoundSetting(drawTurnNoti.Phase);
           

            isActionProcessing = false;

            if (revisionId != BadugiDispatchPushHub.revisionId)
            {
                if (isMyTurn)
                {
                    if (_changeActionToggleState == ChangeActionType.Change)
                    {
                        CardDrawRequestAsync(ChangeActionType.Change, view.changeToggle.toggle, true, true).Forget();
                    }
                    else if (_changeActionToggleState == ChangeActionType.Pass)
                    {
                        CardDrawRequestAsync(ChangeActionType.Pass, view.passToggle.toggle, false, true).Forget();
                    }
                    else
                    {
                        _changeActionToggleState = ChangeActionType.End;
                    }
                    if (CPPlayer.Cloud.optionValue.myTurnViberate)
                    {
#if UNITY_ANDROID || UNITY_IOS
                        Handheld.Vibrate();
#endif
                    }
                }
            }
            else
            {
                if (isMyTurn)
                {
                    if (_changeActionToggleState == ChangeActionType.Change)
                    {
                        CardDrawRequestAsync(ChangeActionType.Change, view.changeToggle.toggle, true, true).Forget();
                    }
                    else if (_changeActionToggleState == ChangeActionType.Pass)
                    {
                        CardDrawRequestAsync(ChangeActionType.Pass, view.passToggle.toggle, false, true).Forget();
                    }
                    else
                    {
                        _changeActionToggleState = ChangeActionType.End;
                    }


                    if (CPPlayer.Cloud.optionValue.myTurnViberate)
                    {
#if UNITY_ANDROID || UNITY_IOS
                        Handheld.Vibrate();
#endif
                    }
                }
            }
        }

        private void DayRoundSetting(DrawPhase drawPhase)
        {
            view.dayRoundParentObj.SetActive(true);
            view.dayRoundParentAnimator.enabled = true;
            for (int i = 0; i < view.dayRoundOnObjs.Length; i++)
            {
                view.dayRoundOnObjs[i].SetActive(false);
            }

            if (drawPhase == DrawPhase.DpMorning)
            {
                view.dayRoundParentAnimator.Play("Round_Morning");
            }

            if (drawPhase == DrawPhase.DpLunch)
            {
                view.dayRoundParentAnimator.Play("Round_Lunch");
            }
            if (drawPhase == DrawPhase.DpEvening)
            {
                view.dayRoundParentAnimator.Play("Round_Evening");
            }
                
        }

        private void DayRoundUIInit()
        {
            view.dayRoundParentAnimator.Play("Round_Idle");
            view.dayRoundParentAnimator.enabled = false;
            for (int i = 0; i < view.dayRoundOnObjs.Length; i++)
            {
                view.dayRoundOnObjs[i].SetActive(false);
            }
        }

        private CancellationTokenSource _roundAnimationCts;    
        
        private async UniTaskVoid RoundImageActiveAnimation(DrawPhase drawPhase)
        {
          
           
            DayRoundSetting(drawPhaseState);
         
        }
        
        private void DrawTurnEventStartSet()                                                  
        {
            // 진행 중인 애니메이션 강제 종료                                            
            _roundAnimationCts?.Cancel();
            view.dayRoundText.gameObject.SetActive(false);
            DayRoundSetting(drawPhaseState);                                                 
        }

        private void CardDrawRequest(ChangeActionType _atype, Toggle changedToggle, bool isDiscard, bool isOn)
        {
            CardDrawRequestAsync(_atype, changedToggle, isDiscard, isOn).Forget();
        }

        private async UniTask CardDrawRequestAsync(ChangeActionType _atype, Toggle changedToggle, bool isDiscard, bool isOn)
        {
            if (isChangeActionProcessing)
                return;

            if (changedToggle != view.passToggle.toggle)
                view.passToggle.toggle.SetIsOnWithoutNotify(false);
            if (changedToggle != view.changeToggle.toggle)
                view.changeToggle.toggle.SetIsOnWithoutNotify(false);

            if (isOn)
            {
                _changeActionToggleState = _atype;
            }
            else
            {
                _changeActionToggleState = ChangeActionType.End;
            }

            if (isMyTurn == false)
                return;
            if (isOn == false)
                return;
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;

            isChangeActionProcessing = true;
            var currentCards = myPlayerCtrl.badugiPlayerInfo.cardlist;
            var touchedCards = myPlayerCtrl.touchedCardIndexList;

            int countToChange = touchedCards.Count;
            List<bool> touchedIndexList;
            if (!isDiscard)
            {
                touchedIndexList = new List<bool>() { false, false, false, false };
            }
            else
            {
                touchedIndexList = currentCards.Select(x => touchedCards.Contains(x)).ToList();
            }

            var indexList = currentCards
                .Select((value, index) => new { value, index })
                .Where(x => touchedCards.Contains(x.value))
                .Select(x => x.index)
                .ToList();

            thisRoundChangeAlreadyActed = true;



            var cardDrawResPacket = await Services.Badugi.DrawReqAsync(CPPlayer.Badugi.currentTableId, touchedIndexList);

            int roundIndex = 0;
            switch (currentDrawTurnNotiInfo.Phase)
            {
                case DrawPhase.DpMorning:
                    roundIndex = 0;
                    break;
                case DrawPhase.DpLunch:
                    roundIndex = 1;
                    break;
                case DrawPhase.DpEvening:
                    roundIndex = 2;
                    break;
            }

            myPlayerCtrl.view.roundInfo[roundIndex].SetNum(cardDrawResPacket.Data.NewCards.Count);


    

            if (!cardDrawResPacket.IsSuccess)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"Server error Occured.\nMessage:{cardDrawResPacket.Error}"));
                return;
            }

            if (isDiscard)
            {
                //card교환 데이터 정리
                var list = myPlayerCtrl.touchedCardIndexList;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    string cardString = list[i];
                    int cardRank = CardRankCalculater.GetCardIndex(cardString);

                    myPlayerCtrl.HighlightCardforChange(cardRank, false);
                }


                //카드 버리는 연출
                snapShot.CardThrowToTop(myPlayerCtrl, indexList,true).Forget();
                //카드 버리는 연출

                int waitCardThrowTotalTime = (int)CPPlayer.Server.visualEffectTimeConfig["DRAW_CARD_IN_MS"];
                await UniTask.Delay(waitCardThrowTotalTime);

                if (indexList.Count != cardDrawResPacket.Data.NewCards.Count)
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"card exchange error retry again"));
                    return;
                }


                for (int i = 0; i < indexList.Count; i++)
                {
                    var cardinfo = myPlayerCtrl.badugiPlayerInfo.cardlist;
                    cardinfo[indexList[i]] = cardDrawResPacket.Data.NewCards[i];
                }

                //card 다시 가져오는 연출
                var newcardList = cardDrawResPacket.Data.NewCards.ToList();
                snapShot.CardChangeToPlayer(myPlayerCtrl, indexList, newcardList,true).Forget();
            }
            else
            {
                if (currentDrawTurnNotiInfo.Phase == DrawPhase.DpEvening)
                {
                    for (int i = 0; i < myPlayerCtrl.cardViewerList.Count; i++)
                    {
                        var cardview = myPlayerCtrl.cardViewerList[i];
                        myPlayerCtrl.TouchedCardforChangeSetAtEvening(cardview.cardInfoIndex, false);
                    }
                }
            }
            view.changeInactive.SetActive(true);
            view.passInactive.SetActive(true);


            view.passToggle.toggle.SetIsOnWithoutNotify(false);
            view.changeToggle.toggle.SetIsOnWithoutNotify(false);

            _changeActionToggleState = ChangeActionType.End;

            myPlayerCtrl.SetEndTurn(true);
        }

        private void DrawActionNoti(DrawNoti drawActionNoti, int revisionId)
        {
            int currentRid = revisionId;
            serverWaitQ.Enqueue(() => DrawActionNotiPresentAfterFrame(drawActionNoti, currentRid));
            DrawActionNotiPresent(drawActionNoti, revisionId).Forget();
        }

        async UniTask DrawActionNotiPresent(DrawNoti drawActionNoti, int revisionId)
        {
            await UniTask.NextFrame();

            var _action = serverWaitQ.Dequeue();
            _action?.Invoke();
        }

        private void DrawActionNotiPresentAfterFrame(DrawNoti drawActionNoti, int revisionId)
        {
            if (!TryGetPlayer(drawActionNoti.ChairId, out var drawActionPlayer))
                return;
            if (drawActionNoti.ChairId == myChairId)
            {
                CPPlayer.InGame.AFKPopupActive?.Invoke(true);
                CPPlayer.InGame.isUserAFK = true;
                
                thisRoundChangeAlreadyActed = true;
                
                view.changeInactive.SetActive(true);
                view.passInactive.SetActive(true);

                view.passToggle.toggle.SetIsOnWithoutNotify(false);
                view.changeToggle.toggle.SetIsOnWithoutNotify(false);
            }
            
            int roundIndex = 0;
            switch (currentDrawTurnNotiInfo.Phase)
            {
                case DrawPhase.DpMorning:
                    roundIndex = 0;
                    break;
                case DrawPhase.DpLunch:
                    roundIndex = 1;
                    break;
                case DrawPhase.DpEvening:
                    roundIndex = 2;
                    break;
            }

            drawActionPlayer.view.roundInfo[roundIndex].SetNum(drawActionNoti.DrawCount);

            if (revisionId != BadugiDispatchPushHub.revisionId)
            {
                //자리비움시 카드 이동애님은 아무것도 안보여줘도 되고
                //내 noti라도 모두 pass하기 떄문에 아무것도 안해도됨.
                DrawActionNotiEventAsync(drawActionNoti).Forget();
            }
            else
            {
                DrawActionNotiEventAsync(drawActionNoti).Forget();
            }

            drawActionPlayer.SetEndTurn(drawActionNoti.ChairId == myChairId);
        }

        private async UniTask DrawActionNotiEventAsync(DrawNoti drawActionNoti)
        {
            int userChairId = drawActionNoti.ChairId;
            if (!TryGetPlayer(userChairId, out var userPlayer))
                return;

            List<int> indexList = new List<int>();
            List<string> newcardList = new List<string>();

            int playerviewIndex = Array.IndexOf(view.playerViewList, userPlayer.view);

            for (int i = 0; i < drawActionNoti.DrawCount; i++)
            {
                int index = 0;
                if (playerviewIndex >= 3)
                {
                    index = i;
                }
                else
                {
                    index = (userPlayer.cardViewerList.Count - 1) - i;
                }

                indexList.Add(index);
                newcardList.Add("");
            }

            int drawcardCount = indexList.Count;
            if (drawActionNoti.ChairId != myChairId)
            {
                if (drawcardCount == 0)
                {
                    userPlayer.SetDrawActionText(0, true);
                }
            }
            


            await snapShot.CardThrowToTop(userPlayer, indexList, playerviewIndex < 3);
            await UniTask.Delay(100);

            await snapShot.CardChangeToPlayer(userPlayer, indexList, newcardList, playerviewIndex < 3);

            if (drawActionNoti.ChairId != myChairId)
            {
                if (drawcardCount > 0)
                {
                    userPlayer.SetDrawActionText(drawcardCount, true);
                }
            }
        }

        private void CardOpenNoti(CardOpenNoti cardopenNoti, int revisionId)
        {
            Extension.eLog($"{cardopenNoti.ChairId} card open//{string.Join(",", cardopenNoti.HoleCards.ToList())}");

            // if (playerDict[cardopenNoti.ChairId].isForfeitWin == false)
            // {
            //
            // }
            if (!TryGetPlayer(cardopenNoti.ChairId, out var cardOpenPlayer))
                return;
            cardOpenPlayer.OpenFoldUserCards(cardopenNoti.HoleCards.ToList()).Forget();
        }

        void ToggleViewAndBetAmountSetting(badugi.TurnNoti turnNoti, bool isMyTurn, bool alreadyActed = false)
        {
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            if (myPlayerCtrl.isFolded)
                return;
            if (myPlayerCtrl.isObserving)
                return;
      
            if (CPPlayer.InGame.currentGameMode != GameMode.TwoVS)
            {
                if (myPlayerCtrl.isAllin)
                    return;
            }
            
            for (int i = 0; i < view.actionToggles.Count; i++)
            {
                view.actionToggles[i].TextColorToDefault();
            }
            
            //각 toggle별 금액 세팅
            for (int i = 0; i < view.actionToggles.Count; i++)
            {
                int index = i;
                if (view.actionToggles[index].gameObject.activeInHierarchy)
                {
                    view.actionToggles[index].ToggleActivate(0, false);
                }

                Partial.BetSizeType bettingActionType = view.actionToggles[index].ingameBettingActionType;
                switch (bettingActionType)
                {
                    case Partial.BetSizeType.BsFold:
                        betAmountDict[bettingActionType] = 0;
                        break;
                    case Partial.BetSizeType.BsCheck:
                        betAmountDict[bettingActionType] = 0;
                        break;
                    case Partial.BetSizeType.BsBbing:
                        betAmountDict[bettingActionType] = CPPlayer.Badugi.initialBuyIn;
                        break;
                    case Partial.BetSizeType.BsCall:
                        betAmountDict[bettingActionType] = turnNoti.CallChip;
                        break;
                    case Partial.BetSizeType.BsDdadang:
                        betAmountDict[bettingActionType] = turnNoti.CallChip * 2;
                        break;
                    case Partial.BetSizeType.BsHalf:
                        betAmountDict[bettingActionType] =
                            (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 2);
                        break;
                    case Partial.BetSizeType.BsQuater:
                        betAmountDict[bettingActionType] =
                            (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 4);
                        break;
                    case Partial.BetSizeType.BsAllin:
                        betAmountDict[bettingActionType] = CPPlayer.UserInfo.userDatabase.User.Gold;
                        break;
                    case Partial.BetSizeType.BsMax:
                        betAmountDict[bettingActionType] =
                            Constraints.MaxBetChip - myPlayerCtrl.GetTotalBet;
                        break;
                    default:
                        break;
                }
            }

            ToggleViewInitialSetting();
            view.actionToggleObject.transform.SetParent(view.bettingActiveParent, false);
            //toggle activate setting
            if (CPPlayer.InGame.currentGameMode == GameMode.TwoVS)
            {
                SetToggleActiveInTwoMode_New(turnNoti);
            }
            else
            {
                SetToggleActiveInDefaultMode_New(turnNoti);
            }


            if (isMyTurn == false && CPPlayer.Cloud.optionValue.reserveBet == false)
            {
                AllActionTogglesDeactivate();
            }
        }

        void SetToggleActiveInDefaultMode_New(badugi.TurnNoti turnNoti)
        {
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            //bet 가능 금액 계산
            var possibleChipforMax = Constraints.MaxBetChip - myPlayerCtrl.GetTotalBet;
            var possibleChipforMyChip = CPPlayer.UserInfo.userDatabase.User.Gold;

            var possibleBetChip = System.Math.Min(possibleChipforMax, possibleChipforMyChip);

            if (possibleBetChip <= 0)
            {
                if (possibleChipforMax <= 0)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(0, true);
                    view.actionToggles[(int)Partial.BetSizeType.BsAllin].ObjectActivate(false);
                }
                else
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(0, true);
                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ObjectActivate(false);
                }
            }
            else
            {
                view.actionToggles[(int)Partial.BetSizeType.BsFold].ToggleActivate(0, true);

                if (turnNoti.CallChip <= 0)
                {
                    if (possibleBetChip > CPPlayer.Badugi.initialBuyIn && myPlayerCtrl.GetTotalBet + CPPlayer.Badugi.initialBuyIn < Constraints.MaxBetChip)
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], true);
                        view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], true);
                    }
                    else
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], false);
                        view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], false);
                    }

                    view.actionToggles[(int)Partial.BetSizeType.BsCall].ObjectActivate(false);
                    view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ObjectActivate(false);
                }
                else
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], false);
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ObjectActivate(false);
                    view.actionToggles[(int)Partial.BetSizeType.BsCheck].ObjectActivate(false);
                    if (possibleBetChip > turnNoti.CallChip)
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], true);
                        //다른 유저의 베팅 가능 여부
                        bool otherCanRaise = false;
                        foreach (var badugiplayer in playerDict)
                        {
                            if (badugiplayer.Value.isAllin || badugiplayer.Value.isFolded)
                            {
                                continue;
                            }
                            else
                            {
                                otherCanRaise = true;
                                break;
                            }
                        }

                        otherCanRaise = turnNoti.MaxChip > 0;
                        //다른 유저의 베팅 가능 여부
                        if (turnNoti.IsLast && otherCanRaise == false)
                        {
                            return;
                        }
                        else
                        {
                            if (possibleBetChip > turnNoti.CallChip * 2 && myPlayerCtrl.GetTotalBet + turnNoti.CallChip * 2 < Constraints.MaxBetChip && thisBetRoundAlreadyActed == false)
                            {
                                view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], true);
                            }
                        }
                    }
                    else
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], false);
                    }
                }

                //쿼터,하프, 활성화 처리
                long quateramount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 4);
                bool QuaterActive = (possibleBetChip > quateramount && quateramount < turnNoti.MaxBet);

                view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], QuaterActive);

                long halfamount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 2);
                bool halfActive = (possibleBetChip > halfamount && halfamount <= turnNoti.MaxBet);
                view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], halfActive);
                //쿼터,하프, 활성화 처리

                //맥스를 걸수가 있나?
                if (possibleBetChip >= possibleChipforMax)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsAllin].ObjectActivate(false);
                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                }
                else
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ObjectActivate(false);
                    view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], true);
                }
            }

            if (!IsActionAllowed(Partial.ActionType.AtRaise))
            {
                view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], false);
                view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], false);
                if (view.actionToggles[(int)Partial.BetSizeType.BsDdadang].gameObject.activeInHierarchy)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], false);
                }

                if (view.actionToggles[(int)Partial.BetSizeType.BsBbing].gameObject.activeInHierarchy)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], false);
                }

                if (view.actionToggles[(int)Partial.BetSizeType.BsMax].gameObject.activeInHierarchy)
                {
                    if (betAmountDict[Partial.BetSizeType.BsMax] <= betAmountDict[Partial.BetSizeType.BsCall])
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                    }
                    else
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], false);
                    }
                }

                if (view.actionToggles[(int)Partial.BetSizeType.BsAllin].gameObject.activeInHierarchy)
                {
                    if (betAmountDict[Partial.BetSizeType.BsAllin] <= betAmountDict[Partial.BetSizeType.BsCall])
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], true);
                    }
                    else
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], false);
                    }
                }
            }
            else
            {
                //4라운드보다 작을때
                if (ruleProvideindex < 3)
                {
                    if (betAmountDict[Partial.BetSizeType.BsHalf] < betAmountDict[Partial.BetSizeType.BsMax])
                    {
                        if (view.actionToggles[(int)Partial.BetSizeType.BsMax].gameObject.activeInHierarchy)
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], false);
                        }
                    }

                    if (betAmountDict[Partial.BetSizeType.BsHalf] < betAmountDict[Partial.BetSizeType.BsAllin])
                    {
                        if (view.actionToggles[(int)Partial.BetSizeType.BsAllin].gameObject.activeInHierarchy)
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], false);
                        }
                    }
                }
                else
                {
                }
            }
        }

        void SetToggleActiveInTwoMode_New(badugi.TurnNoti turnNoti)
        {
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            view.actionToggles[(int)Partial.BetSizeType.BsFold].ToggleActivate(0, true);

            //bet 가능 금액 계산
            var possibleChipforMax = Constraints.MaxBetChip - myPlayerCtrl.GetTotalBet;
            var possibleChipforMyChip = CPPlayer.UserInfo.userDatabase.User.Gold;

            var possibleBetChip = System.Math.Min(possibleChipforMax, possibleChipforMyChip);

            //콜값 유무에 따른 체크,삥,콜,따당 활성화 처리
            if (turnNoti.CallChip <= 0)
            {
                view.actionToggles[(int)Partial.BetSizeType.BsCall].ObjectActivate(false);
                view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ObjectActivate(false);

                if (possibleBetChip > CPPlayer.Badugi.initialBuyIn
                    && myPlayerCtrl.GetTotalBet + CPPlayer.Badugi.initialBuyIn < Constraints.MaxBetChip)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], true);
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], true);
                }
                else
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], true);
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], false);
                }

                if (possibleBetChip <= 0)
                    return;
            }
            else
            {
                view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], false);
                view.actionToggles[(int)Partial.BetSizeType.BsBbing].ObjectActivate(false);
                view.actionToggles[(int)Partial.BetSizeType.BsCheck].ObjectActivate(false);
                if (possibleBetChip > turnNoti.CallChip)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], true);

                    if (possibleBetChip > turnNoti.CallChip * 2 && myPlayerCtrl.GetTotalBet + turnNoti.CallChip * 2 < Constraints.MaxBetChip && thisBetRoundAlreadyActed == false)
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], true);
                    }
                }
                else
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], false);
                }
            }
            
            //쿼터,하프, 활성화 처리
            long quateramount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 4);
            bool QuaterActive = (possibleBetChip > quateramount && quateramount < turnNoti.MaxBet);

            view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], QuaterActive);

            long halfamount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 2);
            bool halfActive = (possibleBetChip > halfamount && halfamount <= turnNoti.MaxBet);
            view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], halfActive);
            //쿼터,하프, 활성화 처리

            //맥스를 걸수가 있나?
            if (possibleBetChip >= possibleChipforMax)
            {
                view.actionToggles[(int)Partial.BetSizeType.BsAllin].ObjectActivate(false);
                view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
            }
            else
            {
                view.actionToggles[(int)Partial.BetSizeType.BsMax].ObjectActivate(false);
                view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], true);
            }
        }


        void AllActionTogglesDeactivate()
        {
            for (int i = 0; i < view.actionToggles.Count; i++)
            {
                if (view.actionToggles[i].gameObject.activeInHierarchy)
                {
                    view.actionToggles[i].ToggleActivate(-1, false);
                }

                view.actionToggles[i].toggle.SetIsOnWithoutNotify(false);
            }

            view.actionToggleObject.transform.SetParent(view.bettingInActiveParent, false);
        }

        void SetToggleActiveInTwoMode(badugi.TurnNoti turnNoti)
        {
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            view.actionToggles[(int)Partial.BetSizeType.BsFold].ToggleActivate(0, true);
            //콜값 유무에 따른 체크,삥,콜,따당 활성화 처리
            if (turnNoti.CallChip <= 0)
            {
                //내 판돈이 바이인보단 큰데 총 베팅금액+바이인이 맥스보다 작을때
                if (CPPlayer.UserInfo.userDatabase.User.Gold >= CPPlayer.Badugi.initialBuyIn
                    && myPlayerCtrl.GetTotalBet + CPPlayer.Badugi.initialBuyIn < Constraints.MaxBetChip)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], true);
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], true);
                }
                else
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], true);
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], false);
                }

                view.actionToggles[(int)Partial.BetSizeType.BsCall].ObjectActivate(false);
                view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ObjectActivate(false);

                //쿼터,하프, 활성화 처리
                long allchipIbet = myPlayerCtrl.GetTotalBet;
                long quateramount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 4);
                bool QuaterActive = (CPPlayer.UserInfo.userDatabase.User.Gold >= quateramount
                                     && allchipIbet + quateramount <=
                                     Constraints.MaxBetChip);
                view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], QuaterActive);

                long halfamount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 2);
                bool halfActive = (CPPlayer.UserInfo.userDatabase.User.Gold >= halfamount
                                   && allchipIbet + halfamount <= Constraints.MaxBetChip);
                view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], halfActive);
                //쿼터,하프, 활성화 처리
            }
            else
            {
                bool callActivate = (CPPlayer.UserInfo.userDatabase.User.Gold >= turnNoti.CallChip);

                view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], callActivate);

                bool ddadangActive = CPPlayer.UserInfo.userDatabase.User.Gold >= turnNoti.CallChip * 2
                                     && myPlayerCtrl.GetTotalBet + turnNoti.CallChip * 2 <= Constraints.MaxBetChip
                                     && isDdadangActedThisRound == false;
                view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], ddadangActive);

                view.actionToggles[(int)Partial.BetSizeType.BsCheck].ObjectActivate(false);
                view.actionToggles[(int)Partial.BetSizeType.BsBbing].ObjectActivate(false);

                //쿼터,하프, 활성화 처리
                long allchipIbet = myPlayerCtrl.GetTotalBet;
                long quateramount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 4);
                bool QuaterActive = (CPPlayer.UserInfo.userDatabase.User.Gold >= quateramount
                                     && allchipIbet + quateramount <=
                                     Constraints.MaxBetChip);
                view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], QuaterActive);

                long halfamount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 2);
                bool halfActive = (CPPlayer.UserInfo.userDatabase.User.Gold >= halfamount
                                   && allchipIbet + halfamount <= Constraints.MaxBetChip);
                view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], halfActive);
                //쿼터,하프, 활성화 처리
            }

            long possibleBetMoney = Constraints.MaxBetChip - myPlayerCtrl.GetTotalBet;

            bool isPossiblebet = possibleBetMoney > 0 && CPPlayer.UserInfo.userDatabase.User.Gold > 0;

            if (turnNoti.CallChip <= 0 && isPossiblebet == false)
            {
                if (CPPlayer.UserInfo.userDatabase.User.Gold + myPlayerCtrl.GetTotalBet >= Constraints.MaxBetChip)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], false);
                    view.actionToggles[(int)Partial.BetSizeType.BsAllin].ObjectActivate(false);
                }
                else
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], false);
                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ObjectActivate(false);
                }
            }
            else
            {
                if (CPPlayer.UserInfo.userDatabase.User.Gold + myPlayerCtrl.GetTotalBet >= Constraints.MaxBetChip)
                {
                    if (turnNoti.IsLast)
                    {
                        if (turnNoti.CallChip + myPlayerCtrl.GetTotalBet >= Constraints.MaxBetChip) //maxcall
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                            if (view.actionToggles[(int)Partial.BetSizeType.BsCall].gameObject.activeInHierarchy)
                                view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], false);
                        }
                        else
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                            if (view.actionToggles[(int)Partial.BetSizeType.BsCall].gameObject.activeInHierarchy)
                                view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], true);
                        }
                    }
                    else
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                    }

                    view.actionToggles[(int)Partial.BetSizeType.BsAllin].ObjectActivate(false);
                }
                else
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], true);
                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ObjectActivate(false);
                }
            }
        }

        void SetToggleActiveInDefaultMode(badugi.TurnNoti turnNoti)
        {
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;
            if (CPPlayer.UserInfo.userDatabase.User.Gold <= 0)
            {
                view.actionToggles[(int)Partial.BetSizeType.BsFold].ToggleActivate(0, true);
                view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(0, true);
                view.actionToggles[(int)Partial.BetSizeType.BsMax].ObjectActivate(false);
            }
            else
            {
                view.actionToggles[(int)Partial.BetSizeType.BsFold].ToggleActivate(0, true);
                //콜값 유무에 따른 체크,삥,콜,따당 활성화 처리
                if (turnNoti.CallChip <= 0)
                {
                    //내 판돈이 바이인보단 큰데 총 베팅금액+바이인이 맥스보다 작을때
                    if (CPPlayer.UserInfo.userDatabase.User.Gold >= CPPlayer.Badugi.initialBuyIn
                        && myPlayerCtrl.GetTotalBet + CPPlayer.Badugi.initialBuyIn < Constraints.MaxBetChip)
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], true);
                        view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], true);
                    }
                    else
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], false);
                        view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], false);
                    }

                    view.actionToggles[(int)Partial.BetSizeType.BsCall].ObjectActivate(false);
                    view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ObjectActivate(false);

                    //쿼터,하프, 활성화 처리
                    long allchipIbet = myPlayerCtrl.GetTotalBet;
                    long quateramount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 4);
                    bool QuaterActive = (CPPlayer.UserInfo.userDatabase.User.Gold >= quateramount
                                         && allchipIbet + quateramount <=
                                         Constraints.MaxBetChip);
                    view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], QuaterActive);

                    long halfamount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 2);
                    bool halfActive = (CPPlayer.UserInfo.userDatabase.User.Gold >= halfamount
                                       && allchipIbet + halfamount <= Constraints.MaxBetChip);
                    view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], halfActive);
                    //쿼터,하프, 활성화 처리
                }
                else
                {
                    bool callActivate = (CPPlayer.UserInfo.userDatabase.User.Gold >= turnNoti.CallChip);

                    view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], callActivate);

                    bool ddadangActive = CPPlayer.UserInfo.userDatabase.User.Gold >= turnNoti.CallChip * 2
                                         && myPlayerCtrl.GetTotalBet + turnNoti.CallChip * 2 <= Constraints.MaxBetChip
                                         && isDdadangActedThisRound == false;
                    view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], ddadangActive);

                    view.actionToggles[(int)Partial.BetSizeType.BsCheck].ObjectActivate(false);
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ObjectActivate(false);

                    //쿼터,하프, 활성화 처리
                    long allchipIbet = myPlayerCtrl.GetTotalBet;
                    long quateramount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 4);
                    bool QuaterActive = (CPPlayer.UserInfo.userDatabase.User.Gold >= quateramount
                                         && allchipIbet + quateramount <=
                                         Constraints.MaxBetChip);
                    view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], QuaterActive);

                    long halfamount = (turnNoti.CallChip + (turnNoti.CallChip + turnNoti.PotAmount) / 2);
                    bool halfActive = (CPPlayer.UserInfo.userDatabase.User.Gold >= halfamount
                                       && allchipIbet + halfamount <= Constraints.MaxBetChip);
                    view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], halfActive);
                    //쿼터,하프, 활성화 처리
                }

                //맥스를 걸수가 있나?
                if (CPPlayer.UserInfo.userDatabase.User.Gold + myPlayerCtrl.GetTotalBet >= Constraints.MaxBetChip)
                {
                    if (CPPlayer.InGame.currentGameMode == GameMode.TwoVS)
                    {
                        if (turnNoti.CallChip <= 0)
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                            //view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], true);
                        }
                        else
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                            view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], false);
                        }
                    }
                    else
                    {
                        if (turnNoti.IsLast)
                        {
                            if (turnNoti.CallChip + myPlayerCtrl.GetTotalBet >= Constraints.MaxBetChip) //maxcall
                            {
                                view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                                view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], false);
                            }
                            else
                            {
                                view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], false);
                                view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], true);
                            }
                        }
                        else
                        {
                            if (turnNoti.CallChip >= turnNoti.MaxBet) //maxcall
                            {
                                if (view.actionToggles[(int)Partial.BetSizeType.BsMax].gameObject.activeInHierarchy)
                                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                                if (view.actionToggles[(int)Partial.BetSizeType.BsCall].gameObject.activeInHierarchy)
                                    view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], false);
                            }
                            else
                            {
                                if (view.actionToggles[(int)Partial.BetSizeType.BsCall].gameObject.activeInHierarchy)
                                {
                                    view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], true);
                                    if (view.actionToggles[(int)Partial.BetSizeType.BsMax].gameObject.activeInHierarchy)
                                        view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                                }
                                else
                                {
                                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                                }
                            }
                        }
                    }

                    view.actionToggles[(int)Partial.BetSizeType.BsAllin].ObjectActivate(false);
                }
                else
                {
                    if (CPPlayer.InGame.currentGameMode == GameMode.TwoVS)
                    {
                        if (turnNoti.CallChip <= 0)
                        {
                            if (CPPlayer.UserInfo.userDatabase.User.Gold > 0)
                            {
                                view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], true);
                            }
                            else
                            {
                                view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], false);
                            }
                            //view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], true); 
                        }
                        else
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], true);
                        }
                    }
                    else
                    {
                        //맥스 못거는데 마지막 배팅?
                        if (turnNoti.IsLast)
                        {
                            if (CPPlayer.UserInfo.userDatabase.User.Gold > 0 && CPPlayer.UserInfo.userDatabase.User.Gold < turnNoti.CallChip)
                            {
                                view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], true);
                            }
                            else
                            {
                                view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], true);
                                view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], false);
                                view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], false);
                                view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], false);
                                view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], false);
                            }
                        }
                        else
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], true);
                            if (CPPlayer.UserInfo.userDatabase.User.Gold < turnNoti.CallChip)
                            {
                                view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], false);
                            }
                        }
                    }

                    view.actionToggles[(int)Partial.BetSizeType.BsMax].ObjectActivate(false);
                }
            }

            if (!IsActionAllowed(Partial.ActionType.AtRaise))
            {
                view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], false);
                view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], false);
                if (view.actionToggles[(int)Partial.BetSizeType.BsDdadang].gameObject.activeInHierarchy)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], false);
                }

                if (view.actionToggles[(int)Partial.BetSizeType.BsBbing].gameObject.activeInHierarchy)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], false);
                }

                if (view.actionToggles[(int)Partial.BetSizeType.BsMax].gameObject.activeInHierarchy)
                {
                    if (betAmountDict[Partial.BetSizeType.BsMax] <= betAmountDict[Partial.BetSizeType.BsCall])
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], true);
                    }
                    else
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], false);
                    }
                    //view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], false);
                }

                if (view.actionToggles[(int)Partial.BetSizeType.BsAllin].gameObject.activeInHierarchy)
                {
                    if (betAmountDict[Partial.BetSizeType.BsAllin] <= betAmountDict[Partial.BetSizeType.BsCall])
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], true);
                    }
                    else
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], false);
                    }
                }
            }
            else
            {
                //4라운드보다 작을때
                if (ruleProvideindex < 3)
                {
                    if (betAmountDict[Partial.BetSizeType.BsHalf] < betAmountDict[Partial.BetSizeType.BsMax])
                    {
                        if (view.actionToggles[(int)Partial.BetSizeType.BsMax].gameObject.activeInHierarchy)
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsMax].ToggleActivate(betAmountDict[Partial.BetSizeType.BsMax], false);
                        }
                    }

                    if (betAmountDict[Partial.BetSizeType.BsHalf] < betAmountDict[Partial.BetSizeType.BsAllin])
                    {
                        if (view.actionToggles[(int)Partial.BetSizeType.BsAllin].gameObject.activeInHierarchy)
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(betAmountDict[Partial.BetSizeType.BsAllin], false);
                        }
                    }
                }
                else
                {
                }
            }
        }

        void ActionTogglePressed(Partial.ActionType actionType,
            Partial.BetSizeType betSizeType, Toggle changedToggle,
            bool ison)
        {
            ActionProcessToggle(actionType, betSizeType, changedToggle, ison).Forget();
        }

        private bool IsActionAllowed(Partial.ActionType actionType)
        {
            switch (actionType)
            {
                case Partial.ActionType.AtRaise:

                    if (!currentRoundGameRule.allowRaiseAfterCheck && currentRoundActionHistory.hasChecked)
                    {
                        return false;
                    }

                    // 콜을 한 경우 레이즈 불가능 규칙
                    if (!currentRoundGameRule.allowRaiseAfterCall && currentRoundActionHistory.hasCalled)
                    {
                        return false;
                    }

                    // 레이즈 횟수 제한 (예: 라운드당 3회)
                    if (currentRoundActionHistory.raiseCount >= currentRoundGameRule.maxRaisesPerRound)
                    {
                        return false;
                    }

                    break;

                case Partial.ActionType.AtCall:
                    break;

                case Partial.ActionType.AtCheck:
                    break;
            }

            return true;
        }


        private async UniTask ActionProcessToggle(Partial.ActionType actionType,
            Partial.BetSizeType betSizeType, Toggle changedToggle,
            bool ison)
        {
            if (isActionProcessing)
                return;

            for (int i = 0; i < view.actionToggles.Count; i++)
            {
                if (changedToggle != view.actionToggles[i].toggle)
                    view.actionToggles[i].toggle.SetIsOnWithoutNotify(false);
            }

            if (ison)
            {
                _bettingActionToggleState = betSizeType;
                actionStateForServer = actionType;
            }
            else
            {
                _bettingActionToggleState = Partial.BetSizeType.BsEnd;
                actionStateForServer = Partial.ActionType.AtNone;
            }

            if (isMyTurn == false)
                return;
            if (ison == false)
                return;
            if (!TryGetPlayer(myChairId, out var myPlayerCtrl))
                return;

            var tempBetActiontoggleType = _bettingActionToggleState;
            if (betSizeType == Partial.BetSizeType.BsMax)
            {
                if (myTurnNotiInfo.TotalBet + myTurnNotiInfo.CallChip >= Constraints.MaxBetChip)
                {
                    if (myTurnNotiInfo.CallChip == 0)
                    {
                        actionStateForServer = Partial.ActionType.AtCheck;
                    }
                    else
                    {
                        actionStateForServer = Partial.ActionType.AtCall;
                    }
                }
                else
                {
                    actionStateForServer = Partial.ActionType.AtRaise;
                }
            }

            long amount = betAmountDict[tempBetActiontoggleType];
            //버튼 누르는 것에 따라 추후 계산하여 amount

            var tempbetSizeType = ProtoMapper.BadugiBettingActionType(tempBetActiontoggleType);
            var tempactionType = ProtoMapper.BadugiActionType(actionStateForServer);
            var actionRes = await Services.Badugi.ActionAsync(snapShot.RoomImfo.TableId, tempactionType, amount, tempbetSizeType);
            if (actionRes.IsSuccess)
            {
                myPlayerCtrl.SetTotalBet(actionRes.Data.TotalBet);
                myPlayerCtrl.SetAction(actionType, tempBetActiontoggleType, amount, actionRes.Data.Chip);
                view.currentPotAmount.text = Extension.ToKoreanFormat(actionRes.Data.PotAmount);

                CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
            }
            else
            {
                for (int i = 0; i < view.actionToggles.Count; i++)
                {
                    if (changedToggle != view.actionToggles[i].toggle)
                        view.actionToggles[i].toggle.SetIsOnWithoutNotify(false);
                }

                _bettingActionToggleState = Partial.BetSizeType.BsEnd;
                actionStateForServer = Partial.ActionType.AtNone;
                Extension.eLog($"Server error Occured.\nMessage:{actionRes.Error}");
                return;
            }


            if (betSizeType == Partial.BetSizeType.BsDdadang)
            {
                isDdadangActedThisRound = true;
            }

            if (betSizeType == BetSizeType.BsBbing)
            {
                currentRoundActionHistory.RecordAction(Partial.ActionType.AtRaise);
            }
            else
            {
                currentRoundActionHistory.RecordAction(actionType);
            }


            switch (betSizeType)
            {
                case Partial.BetSizeType.BsFold:
                    AudioManager.Instance.Play(AudioSourceKey.Die);
                    break;
                case Partial.BetSizeType.BsCheck:
                    AudioManager.Instance.Play(AudioSourceKey.Check);
                    break;
                case Partial.BetSizeType.BsBbing:
                    AudioManager.Instance.Play(AudioSourceKey.Bing);
                    break;
                case Partial.BetSizeType.BsCall:
                    AudioManager.Instance.Play(AudioSourceKey.Call);
                    break;
                case Partial.BetSizeType.BsDdadang:
                    AudioManager.Instance.Play(AudioSourceKey.Dadang);
                    break;
                case Partial.BetSizeType.BsQuater:
                    AudioManager.Instance.Play(AudioSourceKey.Quarter);
                    break;
                case Partial.BetSizeType.BsHalf:
                    AudioManager.Instance.Play(AudioSourceKey.Half);
                    break;
                case Partial.BetSizeType.BsAllin:
                    AudioManager.Instance.Play(AudioSourceKey.Allin);
                    break;
                case Partial.BetSizeType.BsMax:
                    AudioManager.Instance.Play(AudioSourceKey.Max);
                    break;
                case Partial.BetSizeType.BsEnd:
                    break;
                default:
                    break;
            }

            thisBetRoundAlreadyActed = true;
            isActionProcessing = true;

            _bettingActionToggleState = Partial.BetSizeType.BsEnd;

            AllActionTogglesDeactivate();

            isMyTurn = false;
            myPlayerCtrl.ActionToDisplay(tempBetActiontoggleType);
            myPlayerCtrl.SetEndTurn(true);

            myPlayerCtrl.SetFold(actionType == Partial.ActionType.AtFold);

            if (actionType == Partial.ActionType.AtAllin)
            {
                myPlayerCtrl.SetAllin(true);
            }

            if (actionType == Partial.ActionType.AtFold)
            {
                LeaveRequestProcess().Forget();
            }
        }

        public async UniTask LeaveRequestProcess()
        {
            if (reserveLeaveRequest)
            {
                await LeaveThisRoomOrReserve();
            }

            if (reserveMoveRoomRequest)
            {
                await LeaveThisRoomAndMoveOtherRoomOrReserve();
            }
        }

        async UniTask ActionProcess(Partial.ActionType actionType, Partial.BetSizeType ingameBettingActionType)
        {
            long amount = betAmountDict[ingameBettingActionType];
            //버튼 누르는 것에 따라 추후 계산하여 amount

            var betSizeType = ProtoMapper.BadugiBettingActionType(ingameBettingActionType);
            var _actionType = ProtoMapper.BadugiActionType(actionType);
            var actionRes = await Services.Badugi.ActionAsync(snapShot.RoomImfo.TableId, _actionType, amount, betSizeType);
            if (actionRes.IsSuccess)
            {
                if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                {
                    myPlayerCtrl.SetTotalBet(actionRes.Data.TotalBet);
                    myPlayerCtrl.SetAction(actionType, ingameBettingActionType, amount, actionRes.Data.Chip);
                }
                view.currentPotAmount.text = Extension.ToKoreanFormat(actionRes.Data.PotAmount);

                CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
            }
            else
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"Server error Occured.\nMessage:{actionRes.Error}"));
            }
        }

        private void AcionNoti(ActionNoti actionNoti, int revisionId)
        {
            view.currentPotAmount.text = Extension.ToKoreanFormat(actionNoti.PotAmount);
            Partial.BetSizeType tempType = actionNoti.betSizeType;
            if (actionNoti.Action == ActionType.AtFold)
            {
                tempType = Partial.BetSizeType.BsFold;
            }

            if (actionNoti.Action == ActionType.AtCheck)
            {
                tempType = Partial.BetSizeType.BsCheck;
            }

            if (actionNoti.Action == ActionType.AtAllin)
            {
                tempType = Partial.BetSizeType.BsAllin;
            }

            isMyTurn = false;
            if (TryGetPlayer(actionNoti.ChairId, out var actionEndTurnPlayer))
                actionEndTurnPlayer.SetEndTurn(actionNoti.ChairId == myChairId);

            int currentRid = revisionId;
            serverWaitQ.Enqueue(() => ActionNotiPresentAfterFrame(actionNoti, tempType, currentRid));
            ActionNotiPresent(actionNoti, revisionId).Forget();
        }

        async UniTask ActionNotiPresent(ActionNoti actionNoti, int revisionId)
        {
            await UniTask.NextFrame();

            var _action = serverWaitQ.Dequeue();
            _action?.Invoke();
        }

        void ActionNotiPresentAfterFrame(ActionNoti actionNoti, Partial.BetSizeType tempType, int revisionId)
        {
            if (!TryGetPlayer(actionNoti.ChairId, out var actionPlayer))
                return;
            if (revisionId != BadugiDispatchPushHub.revisionId)
            {
                actionPlayer.SetActionData(actionNoti.actionType, tempType, actionNoti.Amount, actionNoti.Chip);
                actionPlayer.ActionToDisplay(actionNoti.betSizeType);
                if (actionNoti.Action == ActionType.AtAllin)
                {
                    actionPlayer.SetAllin(true);
                }

                actionPlayer.SetFold(actionNoti.Action == ActionType.AtFold);
                if (actionNoti.ChairId == myChairId)
                {
                    thisBetRoundAlreadyActed = true;
                    _bettingActionToggleState = Partial.BetSizeType.BsEnd;

                    AllActionTogglesDeactivate();

                    if (actionNoti.betSizeType == BetSizeType.BsBbing)
                    {
                        currentRoundActionHistory.RecordAction(Partial.ActionType.AtRaise);
                    }
                    else
                    {
                        currentRoundActionHistory.RecordAction(actionNoti.actionType);
                    }

                    //자리비움으로 인해 서버에서 자동으로 액션노티 해줌
                    CPPlayer.InGame.AFKPopupActive?.Invoke(true);
                    CPPlayer.InGame.isUserAFK = true;
                }
                else
                {
                    if (actionNoti.Action == ActionType.AtRaise)
                    {
                        thisBetRoundAlreadyActed = false;
                    }
                }
            }
            else
            {
                actionPlayer.SetActionData(actionNoti.actionType, tempType, actionNoti.Amount, actionNoti.Chip);
                snapShot.ThrowChip(actionNoti.ChairId, actionNoti.Chip, actionPlayer.view.throwChipStartPos);
                actionPlayer.ActionToDisplay(actionNoti.betSizeType);

                if (actionNoti.Action == ActionType.AtAllin)
                {
                    actionPlayer.SetAllin(true);
                }

                actionPlayer.SetFold(actionNoti.Action == ActionType.AtFold);
                if (actionNoti.ChairId == myChairId)
                {
                    thisBetRoundAlreadyActed = true;
                    _bettingActionToggleState = Partial.BetSizeType.BsEnd;

                    AllActionTogglesDeactivate();

                    if (actionNoti.betSizeType == BetSizeType.BsBbing)
                    {
                        currentRoundActionHistory.RecordAction(Partial.ActionType.AtRaise);
                    }
                    else
                    {
                        currentRoundActionHistory.RecordAction(actionNoti.actionType);
                    }

                    CPPlayer.InGame.AFKPopupActive?.Invoke(true);
                    CPPlayer.InGame.isUserAFK = true;
                }
                else
                {
                    switch (actionNoti.betSizeType)
                    {
                        case Partial.BetSizeType.BsFold:
                            AudioManager.Instance.Play(AudioSourceKey.Die);
                            break;
                        case Partial.BetSizeType.BsCheck:
                            AudioManager.Instance.Play(AudioSourceKey.Check);
                            break;
                        case Partial.BetSizeType.BsBbing:
                            AudioManager.Instance.Play(AudioSourceKey.Bing);
                            break;
                        case Partial.BetSizeType.BsCall:
                            AudioManager.Instance.Play(AudioSourceKey.Call);
                            break;
                        case Partial.BetSizeType.BsDdadang:
                            AudioManager.Instance.Play(AudioSourceKey.Dadang);
                            break;
                        case Partial.BetSizeType.BsQuater:
                            AudioManager.Instance.Play(AudioSourceKey.Quarter);
                            break;
                        case Partial.BetSizeType.BsHalf:
                            AudioManager.Instance.Play(AudioSourceKey.Half);
                            break;
                        case Partial.BetSizeType.BsAllin:
                            AudioManager.Instance.Play(AudioSourceKey.Allin);
                            break;
                        case Partial.BetSizeType.BsMax:
                            AudioManager.Instance.Play(AudioSourceKey.Max);
                            break;
                        case Partial.BetSizeType.BsEnd:
                            break;
                        default:
                            break;
                    }
                    if (actionNoti.Action == ActionType.AtRaise)
                    {
                        thisBetRoundAlreadyActed = false;
                    }
                }
            }
        }

        private void ShowDownNoti(ShowdownNoti showdownNoti, int revisionId)
        {
            int currentRid = revisionId;
            serverWaitQ.Enqueue(() => ShowdownPresentEventAfterFrame(showdownNoti, currentRid));
            ShowdownPresentEvent(showdownNoti, revisionId).Forget();
        }

        async UniTask ShowdownPresentEvent(ShowdownNoti showdownNoti, int revisionId)
        {
            await UniTask.NextFrame();
            var _action = serverWaitQ.Dequeue();
            _action?.Invoke();
        }

        void ShowdownPresentEventAfterFrame(ShowdownNoti showdownNoti, int revisionId)
        {
            ChangeGamestate(BadugiState.ShowDown);
            if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                myPlayerCtrl.SetCurrentPhase(BadugiState.ShowDown);
            if (revisionId != BadugiDispatchPushHub.revisionId)
            {
            }
            else
            {
                PresentationShowDown(showdownNoti).Forget();
            }
        }


        const int showdownAnimationMilSec = 3000;

        private async UniTask PresentationShowDown(badugi.ShowdownNoti showdownNoti)
        {
            view.showdownPanel.SetActive(true);
            view.showdownPanelAnimator.speed = 1;
            view.showdownPanelAnimator.Play("Showdown");
            await UniTask.Delay(showdownAnimationMilSec);
            view.showdownPanel.SetActive(false);
        }


        private void ResultNoti(ResultNoti resultNoti, int revisionId)
        {
            int currentRid = revisionId;
            serverWaitQ.Enqueue(() => ResultPresentEventAfterFrame(resultNoti, currentRid));
            ResultPresentEvent(resultNoti, revisionId).Forget();
        }

        async UniTask ResultPresentEvent(ResultNoti resultNoti, int revisionId)
        {
            await UniTask.NextFrame();

            var _action = serverWaitQ.Dequeue();
            _action?.Invoke();
        }

        void ResultPresentEventAfterFrame(ResultNoti resultNoti, int revisionId)
        {
            ChangeGamestate(BadugiState.Result);
            if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                myPlayerCtrl.SetCurrentPhase(BadugiState.Result);
            snapShot.SetShowDownPotInfo(resultNoti.Pots);


            if (revisionId != BadugiDispatchPushHub.revisionId)
            {
                PresentationResultSnapshot(resultNoti);
            }
            else
            {
                PresentationResult(resultNoti).Forget();
            }
        }

        private CancellationTokenSource _resultPresentationCts;

        private async UniTask PresentationResult(badugi.ResultNoti resultNoti)
        {
            _resultPresentationCts?.Cancel();
            _resultPresentationCts = new CancellationTokenSource();
            var token = _resultPresentationCts.Token;

            //서버에서 RESULT_SHOW_WAIT_MS만큼 기달렸다가 보내주기 때문에 클라에서 기다릴 필요 없음
            // int delayfoResult = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_SHOW_WAIT_MS"];
            // await UniTask.Delay(delayfoResult, cancellationToken: token);

            bool isShowdownNeed = true;
            int showdownPlayerCount = 0;
            foreach (var resultPlayer in resultNoti.Players)
            {
                if (resultPlayer.HoleCards.Count > 0)
                {
                    if (!TryGetPlayer(resultPlayer.ChairId, out var resultCheckPlayer))
                        continue;
                    if (resultCheckPlayer.isFolded)
                    {
                        continue;
                    }

                    showdownPlayerCount++;
                }
            }

            //기존 UI display 처리(pot 사라짐, 족보 안내버튼 사라짐 등)
            SetTableUIForResult();
            //기존 UI display 처리(pot 사라짐, 족보 안내버튼 사라짐 등)

            float highlightDownTime=0.3f;
            if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_DOWN_MS"))
            {
                highlightDownTime= (float)CPPlayer.Server.visualEffectTimeConfig["SELECT_DOWN_MS"]/1000f; 
            }
            foreach (var p in playerDict)
            {
                if (p.Value.cardViewerList.Count > 0 && p.Value.isFolded == false)
                {
                    for (int i = 0; i < p.Value.cardViewerList.Count; i++)
                    {
                        p.Value.cardViewerList[i].InactiveSelectEffectAtFold(highlightDownTime);
                    }
                }

                p.Value.view.roundBetChipObj.SetActive(false);
                p.Value.view.betActionTypeImageParentObj.gameObject.SetActive(false);
                p.Value.SetDrawActionText(0,false);
            }
            
            view.dayRoundText.gameObject.SetActive(false);
            
            await ResultDisplayEventAsync(resultNoti, true, token);

            ResultDisplayEventAsync(resultNoti, false, token).Forget();
            //메인팟의 winner 배열 가져와서 정하기
            var mainWinner = resultNoti.Pots[0].Wins;
            badugi.ResultNoti.Types.Player mainWinnerPlayer = null;

            bool isJackpotExist = false;

            //view.winnerDetailPanel.SetActive(true);
            string mainWinRankText = "";
            long jackpotAmount = 0;
            foreach (var gameplayer in resultNoti.Players)
            {
                bool isMainPotWin = mainWinner.Any(o => o.ChairId == gameplayer.ChairId);
                if (isMainPotWin)
                {
                    mainWinnerPlayer = gameplayer;
                }

                if (!TryGetPlayer(gameplayer.ChairId, out var gamePlayerController))
                    continue;

                gamePlayerController.SetWinnerUI(isMainPotWin);

                if (showdownPlayerCount == 0)
                {
                    gamePlayerController.view.winJokboName.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Forfeit].StringToLocal;
                }

                gamePlayerController.SetResultInfo(gameplayer, isMainPotWin);
                if (gameplayer.HoleCards.Count <= 0)
                    continue;


                string mainwinrank = gamePlayerController.resultRankString;
                //snapShot.SetResultUIAndMainWinnerRank(gamePlayerController, true);
                if (isMainPotWin)
                {
                    mainWinRankText = mainwinrank;
                    gamePlayerController.view.winJokboName.text = mainWinRankText;
                }
                else
                {
                    foreach (var cardViewer in gamePlayerController.cardViewerList)
                    {
                        cardViewer.SetMaskFade();
                    }
                    float dieDim = (float)CPPlayer.Server.visualEffectTimeConfig["DIE_ME_DIM_MS"] / 1000f;

                    gamePlayerController.view.inActiveMask.SetActive(true);

                    var c = gamePlayerController.view.inactivemaskImage.color;
                    c.a = 0f;
                    gamePlayerController.view.inactivemaskImage.color = c;

                    gamePlayerController.view.inactivemaskImage.DOFade(0.5f, dieDim);
                }

                if (gameplayer.Jackpot > 0)
                {
                    isJackpotExist = true;
                    jackpotAmount = gameplayer.Jackpot;
                }
            }

            if (isJackpotExist)
            {
                view.jackpotDetailBackEffect.SetActive(true);
                view.jackpotDetailPanel.SetActive(true);
                view.jackpotCardRank.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.RoyalStraightFlush].StringToLocal;
            }
            else
            {
                //view.winnerDetailPanel.SetActive(true);
            }

            if (showdownPlayerCount == 0)
            {
                view.winnerCardRank.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Forfeit].StringToLocal;
                //await UniTask.Delay(500);
                if (mainWinnerPlayer != null && mainWinnerPlayer.ChairId == myChairId)
                {
                    if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                        myPlayerCtrl.SetCardOpenAtForfeitWin();
                }
            }
            else
            {
                view.winnerCardRank.text = mainWinRankText;
            }
            //string winAmount= Extension.ToKoreanFormat(resultNoti.Pots[0].Amount);

            long realWinAmount = mainWinnerPlayer.Win;
            var winAmount = Extension.ToKoreanFormat(realWinAmount, Extension.KoreanFormatMode.Planning);

            view.winnerAmountChip.text = $"+{winAmount}";

            if (isJackpotExist)
            {
                string jackpotAmountStr = Extension.ToKoreanFormat(jackpotAmount, Extension.KoreanFormatMode.Planning);
                Extension.eLog($"+{winAmount}//잭팟:{jackpotAmountStr}/{jackpotAmount}");
                view.jackpotAmountChip.text = $"+{winAmount} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Jackpot].StringToLocal}{jackpotAmountStr}";
            }

            foreach (var resultplayer in resultNoti.Players)
            {
                if (!TryGetPlayer(resultplayer.ChairId, out var resultOwnedPlayer))
                    continue;
                resultOwnedPlayer.SetCurrentOwnedChip(resultplayer.Chip);
            }

            int delayfoResultEnd = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_SHOW_MS"];
            int cardOpenInActiveTime = (int)CPPlayer.Server.visualEffectTimeConfig["OPEN_HIDE_MS"];
            await UniTask.Delay(delayfoResultEnd-cardOpenInActiveTime, cancellationToken: token);
            if (TryGetPlayer(myChairId, out var myPlayerCtrlForCardOpen))
                myPlayerCtrlForCardOpen.CardOpenBtnObjActive(false);
               
            await UniTask.Delay(cardOpenInActiveTime, cancellationToken: token);
            
            //라운드 정보 초기화
            foreach (var badugiPlayerController in playerDict)
            {
                badugiPlayerController.Value.ResetRoundInfo();
            }


            //showdown 끝날때까지 대기시간 위 연출 종료까지 총 4초 아래에서 딜레이 후 idle로 넘어감
            ChangeGamestate(BadugiState.End);

            //result에서 업적확인 noti
            CheckAchievement(resultNoti);
            //result에서 업적확인 noti

            //기존 UI display 다시 재생성 처리
            SetTableUIForResultEnd();
            //기존 UI display 다시 재생성 처리

            await LeaveRequestProcess();

            InitializeOnEndGame();

            _resultPresentationCts?.Dispose();
            _resultPresentationCts = null;
        }

        void SetTableUIForResult()
        {
            //toggle 비활성
            AllActionTogglesDeactivate();

            view.potAmountObject.SetActive(false);
            view.dayRoundParentObj.SetActive(false);
            drawPhaseState= DrawPhase.DpNone;
            view.openJokboWindowBtn.gameObject.SetActive(false);

            DayRoundUIInit();
         

            if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                myPlayerCtrl.SetCardOpenBtn();
        }

        void SetTableUIForResultEnd()
        {
            view.dayRoundParentObj.SetActive(true);
            view.potAmountObject.SetActive(true);
            view.openJokboWindowBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.jokboInform);
        }

        private void PresentationResultSnapshot(badugi.ResultNoti resultNoti)
        {
            bool isShowdownNeed = true;
            int showdownPlayerCount = 0;
            foreach (var showdownplayer in resultNoti.Players)
            {
                if (showdownplayer.HoleCards.Count > 0)
                {
                    showdownPlayerCount++;
                }
            }

            ResultDisplayEvent(resultNoti);

            //메인팟의 winner 배열 가져와서 정하기
            var mainWinner = resultNoti.Pots[0].Wins;
            badugi.ResultNoti.Types.Player mainWinnerPlayer = null;

            bool isJackpotExist = false;

            //view.winnerDetailPanel.SetActive(true);
            string mainWinRankText = "";
            long jackpotAmount = 0;
            foreach (var gameplayer in resultNoti.Players)
            {
                bool isMainPotWin = mainWinner.Any(o => o.ChairId == gameplayer.ChairId);
                if (isMainPotWin)
                {
                    mainWinnerPlayer = gameplayer;
                }

                if (!TryGetPlayer(gameplayer.ChairId, out var snapGamePlayer))
                    continue;

                snapGamePlayer.SetWinnerUI(isMainPotWin);

                if (showdownPlayerCount == 0)
                {
                    snapGamePlayer.view.winJokboName.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Forfeit].StringToLocal;
                }

                if (gameplayer.HoleCards.Count <= 0)
                    continue;

                snapGamePlayer.SetResultInfo(gameplayer, isMainPotWin);

                string mainwinrank = snapGamePlayer.resultRankString;
                //snapShot.SetResultUIAndMainWinnerRank(snapGamePlayer, true);
                if (isMainPotWin)
                {
                    mainWinRankText = mainwinrank;
                    snapGamePlayer.view.winJokboName.text = mainWinRankText;
                }

                if (gameplayer.Jackpot > 0)
                {
                    isJackpotExist = true;
                    jackpotAmount = gameplayer.Jackpot;
                }
            }


            if (isJackpotExist)
            {
                view.jackpotDetailBackEffect.SetActive(true);
                view.jackpotDetailPanel.SetActive(true);
                view.jackpotCardRank.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.RoyalStraightFlush].StringToLocal;
            }
            else
            {
                //view.winnerDetailPanel.SetActive(true);
            }

            if (showdownPlayerCount == 0)
            {
                view.winnerCardRank.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Forfeit].StringToLocal;
            }
            else
            {
                view.winnerCardRank.text = mainWinRankText;
            }

            long realWinAmount = mainWinnerPlayer.Win;
            var winAmount = Extension.ToKoreanFormat(realWinAmount, Extension.KoreanFormatMode.Planning);

            view.winnerAmountChip.text = $"+{winAmount}";

            if (isJackpotExist)
            {
                string jackpotAmountStr = Extension.ToKoreanFormat(jackpotAmount, Extension.KoreanFormatMode.Planning);
                Extension.eLog($"+{winAmount}//잭팟:{jackpotAmountStr}/{jackpotAmount}");
                view.jackpotAmountChip.text = $"+{winAmount} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Jackpot].StringToLocal}{jackpotAmountStr}";
            }


            foreach (var resultplayer in resultNoti.Players)
            {
                if (!TryGetPlayer(resultplayer.ChairId, out var snapResultPlayer))
                    continue;
                snapResultPlayer.SetCurrentOwnedChip(resultplayer.Chip);
            }


            //라운드 정보 초기화
            foreach (var badugiPlayerController in playerDict)
            {
                badugiPlayerController.Value.ResetRoundInfo();
            }

            //showdown 끝날때까지 대기시간 위 연출 종료까지 총 4초 아래에서 딜레이 후 idle로 넘어감
            ChangeGamestate(BadugiState.End);

            //result에서 업적확인 noti
            CheckAchievement(resultNoti);
            //result에서 업적확인 noti

            InitializeOnEndGame();

            if (view.gameObject.activeInHierarchy)
            {
                //  await Services.Badugi.ResultAnimationEndReqAsync(CPPlayer.Badugi.currentTableId);
            }
        }

        void CheckAchievement(badugi.ResultNoti resultNoti)
        {
            var myinfo = resultNoti.Players.FirstOrDefault(o => o.ChairId == myChairId);
            if (myinfo == null || myinfo.Quests.Count <= 0)
                return;

            // 업적 달성 알림 배지 표시
            CPPlayer.OutGame.newAchievementNotiCallback?.Invoke(true);

            foreach (var quest in myinfo.Quests)
            {
                // 포인트 업적 (type: "ACHIEVEMENTS")
                if (quest.Type == "ACHIEVEMENTS")
                    continue;
            }
        }

        async UniTask ResultDisplayEventAsync(badugi.ResultNoti resultNoti, bool toLiveUser, CancellationToken token)
        {
            var tasks = new List<UniTask>();
            var mainWinner = resultNoti.Pots[0].Wins;
            badugi.ResultNoti.Types.Player mainWinnerPlayer = null;

            foreach (var gameplayer in resultNoti.Players)
            {
                if (!TryGetPlayer(gameplayer.ChairId, out var displayPlayer))
                    continue;
                if (toLiveUser == displayPlayer.isFolded)
                {
                    continue;
                }

                bool ismainWinner = mainWinner.Any(o => o.ChairId == gameplayer.ChairId);
                tasks.Add(displayPlayer.SetInfoForResultAsync(gameplayer, ismainWinner, token));

                if (toLiveUser)
                {
                    int cardOpenEventTime = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_MS"];
                    int cardOpenEventWaitTime = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_WAIT_MS"];
                    await UniTask.Delay(cardOpenEventTime + cardOpenEventWaitTime, cancellationToken: token);
                }
            }

            //여기까지 하여 개인별 데이터 전달 및 ui 구성 완료
            if (toLiveUser)
            {
                await UniTask.WhenAll(tasks).AttachExternalCancellation(token);
            }
        }

        void ResultDisplayEvent(badugi.ResultNoti resultNoti)
        {
            var mainWinner = resultNoti.Pots[0].Wins;
            badugi.ResultNoti.Types.Player mainWinnerPlayer = null;

            foreach (var gameplayer in resultNoti.Players)
            {
                bool ismainWinner = mainWinner.Any(o => o.ChairId == gameplayer.ChairId);
                if (!TryGetPlayer(gameplayer.ChairId, out var displayEventPlayer))
                    continue;
                displayEventPlayer.SetInfoForResult(gameplayer, ismainWinner);
            }
        }

        void ChangeGamestate(BadugiState state)
        {
            badugiState = state;
            CPPlayer.Badugi.currentBadugiState = state;
            view.test_GameState.text = badugiState.ToString();
        }


        private async UniTask LeaveThisRoomOrReserve()
        {
            CPPlayer.InGame.isMovingTable = false;
            var leaveResPacket = await Services.Badugi.LeaveRoomAsync(CPPlayer.Badugi.currentTableId);
            if (leaveResPacket.IsSuccess)
            {
                Extension.eLog($"나갈때 응답 테이블id:{leaveResPacket.Data.TableId} 현재 테이블 id:{CPPlayer.Badugi.currentTableId}");
                if (leaveResPacket.Data.IsReserved)
                {
                    reserveLeaveRequest = true;
                    
                    view.UpdateAfterLeaveBtnPressed(reserveLeaveRequest);
            
                    MoveRoomBtnInit();
                    
                    if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                        myPlayerCtrl.ReserveOut(true);
                    return;
                }
            }

            LeaveGameDataInitialize();
            CPPlayer.Badugi.currentTableId = 0;
            CPPlayer.InGame.LeaveGame?.Invoke(GameType.LOW_BADUGI);

            await UniTask.Yield();
        }

        private async UniTask LeaveThisRoomAndMoveOtherRoomOrReserve()
        {
            if (isExiled)
                return;
            if (CPPlayer.InGame.isMovingTable)
                return;

            CPPlayer.InGame.isMovingTable = true;
            
            var leaveResPacket = await Services.Badugi.LeaveRoomAsync(CPPlayer.Badugi.currentTableId);
            if (leaveResPacket.IsSuccess)
            {
                Extension.eLog($"응답 테이블id:{leaveResPacket.Data.TableId} 현재 테이블 id:{CPPlayer.Badugi.currentTableId}");
                if (leaveResPacket.Data.IsReserved)
                {
                    reserveMoveRoomRequest = true;
                    
                    view.UpdateAfterMoveBtnPressed(reserveMoveRoomRequest);
                
                    LeaveRoomBtnInit();
                    
                    if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                        myPlayerCtrl.ReserveOut(true);
                    
                    CPPlayer.InGame.isMovingTable = false;

                    return;
                }
            }

            LeaveGameDataInitialize();
            CPPlayer.InGame.MoveTable?.Invoke(GameType.LOW_BADUGI);
        }

        async UniTask CancelReserveLeave()
        {
            if (isExiled)
                return;
            CPPlayer.InGame.isMovingTable = false;

            var leaveResPacket = await Services.Badugi.LeaveRoomCacnelAsync(CPPlayer.Badugi.currentTableId);
            if (leaveResPacket.IsSuccess)
            {
                LeaveRoomBtnInit();
            }
        }

        async UniTask CancelReserveMove()
        {
            if (isExiled)
                return;
            
            CPPlayer.InGame.isMovingTable = false;

            var leaveResPacket = await Services.Badugi.LeaveRoomCacnelAsync(CPPlayer.Badugi.currentTableId);
            if (leaveResPacket.IsSuccess)
            {
                MoveRoomBtnInit();
            }
        }

        void LeaveRoomBtnInit()
        {
            reserveLeaveRequest = false;
            
            isExiled = false;
            view.UpdateAfterCancelLeaveBtnPressed();
            if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                myPlayerCtrl.ReserveOut(false);
        }

        void MoveRoomBtnInit()
        {
            reserveMoveRoomRequest = false;
            
            CPPlayer.InGame.isMovingTable = false;
            view.UpdateAfterCancelMoveBtnPressed();
            
            if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                myPlayerCtrl.ReserveOut(false);
        }

        void ToggleViewSetForReserveBetOption(bool isReservePossible)
        {
            if (currentTurnNotiInfo == null)
                return;

            if (isReservePossible)
            {
                if (thisBetRoundAlreadyActed == false)
                {
                    Extension.eLog($"TotalBet:{currentTurnNotiInfo.TotalBet}/CallChip:{currentTurnNotiInfo.CallChip}/maxBet:{Constraints.MaxBetChip}", Color.green);
                    ToggleViewAndBetAmountSetting(currentTurnNotiInfo, currentTurnNotiInfo.ChairId == myChairId, thisBetRoundAlreadyActed);
                }
            }
            else
            {
                AllActionTogglesDeactivate();
            }
        }

        public void InitializeOnEndGame()
        {
            snapShot.ClearDataInRoundGame();
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();
        }

        public void InitializeOnGameStart()
        {
            snapShot.ClearDataInRoundGame();
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();
            ToggleViewInitialSetting();
            view.readyBtn.gameObject.SetActive(false);
            
            
            foreach (var player in playerDict)
            {
                player.Value.StartSet();

                if (startNotiInfo.BossId == player.Value.chairId)
                {
                    player.Value.view.dealerBtnObj.SetActive(true);
                }
                else
                {
                    player.Value.view.dealerBtnObj.SetActive(false);
                }

                player.Value.view.inActiveMask.SetActive(false);
            }

            currentRoundActionHistory.ResetForNewRound();
            isDdadangActedThisRound = false;
        }

        public void LeaveGameDataInitialize()
        {
            snapShot.ClearDataInRoundGame();
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();

            Debug.Log("퇴장 완료");
            //정보 초기화
            reserveLeaveRequest = false;
            reserveMoveRoomRequest = false;
            view.leaveBtn.enabled = true;
            view.moveRoomBtn.enabled = true;
            isExiled = false;
            view.leaveBtn.gameObject.SetActive(true);
            view.leaveReservedObj.gameObject.SetActive(false);
            if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                myPlayerCtrl.ReserveOut(false);
            view.moveRoomBtn.gameObject.SetActive(true);
            view.moveReservedObj.gameObject.SetActive(false);

            var removeKeys = new List<int>();

            foreach (var kv in playerDict)
            {
                kv.Value.Release();
                removeKeys.Add(kv.Key);
            }

            foreach (var key in removeKeys)
            {
                playerDict.Remove(key);
            }

            playerDict.Clear();

            if (_resultPresentationCts != null && !_resultPresentationCts.Token.IsCancellationRequested)
            {
                _resultPresentationCts.Cancel();
            }

            CPPlayer.InGame.AFKPopupActive?.Invoke(false);
        }

        public void InitializeOnEnter()
        {
            view.showEmoticonViewBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.useEmoji);
            view.openJokboWindowBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.jokboInform);
            view.potAmountObject.SetActive(true);
            view.dayRoundParentObj.SetActive(true);
            drawPhaseState= DrawPhase.DpNone;
            snapShot.ClearDataInRoundGame();
            snapShot.SetGameInfo(enterresInfo);
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();
            ToggleViewInitialSetting();
          

            for (int i = 0; i < view.playerViewList.Length; i++)
            {
                view.playerViewList[i].gameObject.SetActive(false);
            }

            myPlayer.Chip = CPPlayer.UserInfo.userDatabase.User.Gold;
            CPPlayer.Badugi.currentTableId = enterresInfo.TableId;
            CPPlayer.Badugi.gapBetweenChairIdAndIndex = enterresInfo.ChairId;
            CPPlayer.Badugi.twoVSOpponentViewIndex = UnityEngine.Random.Range(0, 2)==0?2:3;
            CPPlayer.InGame.haveKickVote = false;
            reserveLeaveRequest = false;
            reserveMoveRoomRequest = false;
            isExiled = false;

            view.emotionView.ActiveWindow(false);
        }

        private void BossBtnSet()
        {
            foreach (var player in playerDict)
            {
                player.Value.EnterSet();
                player.Value.InfoModalInactive();
                if (enterresInfo.BossId == player.Value.chairId)
                {
                    player.Value.view.dealerBtnObj.SetActive(true);
                }
                else
                {
                    player.Value.view.dealerBtnObj.SetActive(false);
                }
            }
        }

        private void ActionToggleInActivate()
        {
            AllActionTogglesDeactivate();

            _bettingActionToggleState = Partial.BetSizeType.BsEnd;
        }

        private void SetInGameDisplayInitialize()
        {
            view.winnerDetailPanel.SetActive(false);
            view.jackpotDetailPanel.SetActive(false);
            view.jackpotDetailBackEffect.SetActive(false);

            view.showdownPanel.SetActive(false);
            view.passToggle.gameObject.SetActive(false);
            view.changeToggle.gameObject.SetActive(false);
            view.actionToggleObject.SetActive(true);
            view.cardactionToggleParent.SetActive(false);
            view.jokboWindow.SetActive(false);

            //CPPlayer.InGame.AFKPopupActive?.Invoke(false);
            CPPlayer.InGame.isUserAFK = false;
            CPPlayer.InGame.AFKPopupActiveFlag = false;
            thisBetRoundAlreadyActed = false;

            _changeActionToggleState = ChangeActionType.End;
            thisRoundChangeAlreadyActed = false;

            DayRoundUIInit();

            view.tableAnte.text = Extension.ToKoreanFormat(CPPlayer.InGame.currentRoomInfo.Ante);
            view.currentPotAmount.text = "0";
        }


        void ToggleViewInitialSetting()
        {
            for (int i = 0; i < view.actionToggles.Count; i++)
            {
                int index = i;
                Partial.BetSizeType bettingActionType = view.actionToggles[index].ingameBettingActionType;
                switch (bettingActionType)
                {
                    case Partial.BetSizeType.BsFold:
                        view.actionToggles[index].ToggleActivate(0, false);
                        break;
                    case Partial.BetSizeType.BsCheck:
                        view.actionToggles[index].ObjectActivate(false);
                        break;
                    case Partial.BetSizeType.BsBbing:
                        view.actionToggles[index].ToggleActivate(0, false);
                        break;
                    case Partial.BetSizeType.BsCall:
                        view.actionToggles[index].ToggleActivate(0, false);
                        break;
                    case Partial.BetSizeType.BsDdadang:
                        view.actionToggles[index].ObjectActivate(false);
                        break;
                    case Partial.BetSizeType.BsHalf:
                        view.actionToggles[index].ToggleActivate(0, false);
                        break;
                    case Partial.BetSizeType.BsQuater:
                        view.actionToggles[index].ToggleActivate(0, false);
                        break;
                    case Partial.BetSizeType.BsAllin:
                        view.actionToggles[index].ObjectActivate(false);
                        break;
                    case Partial.BetSizeType.BsMax:
                        view.actionToggles[index].ToggleActivate(0, false);
                        break;
                    default:
                        break;
                }
            }
        }

        private void InitializePlayersDisplay()
        {
            if (TryGetPlayer(myChairId, out var myPlayerCtrl))
            {
                myPlayerCtrl.SetCurrentPhase(BadugiState.End);
                myPlayerCtrl.SetCardOpenBtn();
            }
            
            foreach (var controller in playerDict)
            {
                controller.Value.InitializePlayerData();
            }

            foreach (var badugiPlayerController in playerDict)
            {
                badugiPlayerController.Value.BetImageActive(false);
                badugiPlayerController.Value.ClearCurrentRoundBetHistory();
            }

            foreach (var badugiPlayerController in playerDict)
            {
                Transform[] children = new Transform[badugiPlayerController.Value.view.myCardPos.Length];

                for (int i = 0; i < badugiPlayerController.Value.view.myCardPos.Length; i++)
                {
                    children[i] = badugiPlayerController.Value.view.myCardPos[i];
                }

                for (int i = 0; i < badugiPlayerController.Value.view.myCardPos.Length; i++)
                {
                    int index = i;
                    Transform child = children[index];
                    child.SetSiblingIndex(index);
                    child.GetComponent<RectTransform>().anchoredPosition = badugiPlayerController.Value.view.cardPositions[index];
                }
            }


            view.leaveBtn.enabled = true;
            view.moveRoomBtn.enabled = true;
        }

        private void KickedForSomeReason(KickVoteNoti kickVoteNoti, int revisionId)
        {
            if (myChairId == kickVoteNoti.TargetChairId)
            {
                if (kickVoteNoti.VoteCount >= 3)
                {
                    isExiled = true;
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.KickVoteWarning].StringToLocal, kickVoteNoti.VoteCount), true));
                }
                else
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.KickVoteReceived].StringToLocal, kickVoteNoti.VoteCount), true));
                }
            }

            if (TryGetPlayer(kickVoteNoti.TargetChairId, out var kickTargetPlayer))
                kickTargetPlayer.KickVoteRecieveEvent(kickVoteNoti.VoteCount);
        }

        async UniTask EmoticonExpressReq(EmotionInfo emotionInfo)
        {
            if (CPPlayer.InGame.currentGameType != GameType.LOW_BADUGI)
                return;
            string emoteStr = $"{emotionInfo.emoticonKind}_{emotionInfo.emoticonExpress}";
            var res = await Services.Badugi.EmoteReqAsync(CPPlayer.Badugi.currentTableId, myChairId, emoteStr);
            if (res.IsSuccess)
            {
                if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                    myPlayerCtrl.EmoticonExpress(emotionInfo);
            }
        }

        private void EmoticonExpressNoti(EmoteNoti emotenoti, int revisionId)
        {
            if (CPPlayer.Cloud.optionValue.useEmoji == false)
                return;
            Partial.IEmoteNoti _emoteNoti = emotenoti;

            string[] parts = _emoteNoti.emoteName.Split('_');
            EmoticonKindType kind = Extension.StringToEnum<EmoticonKindType>(parts[0]);
            EmoticonExpressType express = Extension.StringToEnum<EmoticonExpressType>(parts[1]);

            var emoticon = InGameResourcesBundle.Loaded.emotionInfoList.FirstOrDefault(o => o.emoticonKind == kind && o.emoticonExpress == express);
            if (emoticon != null)
            {
                if (TryGetPlayer(_emoteNoti.fromChairId, out var emotePlayer))
                    emotePlayer.EmoticonExpress(emoticon);
            }
        }

        private void LeaveReservedNoti(LeaveReservedNoti leaveReserved, int revisionId)
        {
            if (!TryGetPlayer(leaveReserved.ChairId, out var leaveReservedPlayer))
                return;
            leaveReservedPlayer.ReserveOut(!leaveReserved.Cancel);
            if (leaveReserved.ChairId == myChairId)
            {
                if (reserveMoveRoomRequest)
                {
                    MoveRoomBtnInit();
                }
                view.leaveReservedObj.gameObject.SetActive(true);
                view.leaveBtn.gameObject.SetActive(false);
            }

            Extension.eLog($"LeaveReservedNoti to {leaveReserved.ChairId}and my chairID:{myChairId}", Color.magenta);
        }

        private void MeUserBackFromInactive(bool active)
        {
            if (CPPlayer.Server.currentConnectedGameType != GameType.LOW_BADUGI)
                return;
            if (active)
                return;
            if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                myPlayerCtrl.ReserveOut(false);

            view.leaveReservedObj.gameObject.SetActive(false);
            view.leaveBtn.gameObject.SetActive(true);
        }

    
    }
}