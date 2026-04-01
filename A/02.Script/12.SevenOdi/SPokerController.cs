using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CAPYBARA.sevenPoker;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Common;

namespace CAPYBARA
{
    public enum SPokerState
    {
        None = -5,
        Idle = 0,
        Start,
        CardSelect,
        Round_1,
        Round_2,
        Round_3,
        Round_4,
        ShowDown,
        Result,
        End,
    }

    public class SPokerController:IInGameController
    {
        private SPokerViewer view;
        private SPokerTableSnapShot snapShot;

        private GameObject mainObject;
        public SPokerState spokerState;

        private Dictionary<int, SPokerPlayerController> playerDict = new Dictionary<int, SPokerPlayerController>();
        private bool TryGetPlayer(int chairId, out SPokerPlayerController player)
            => playerDict.TryGetValue(chairId, out player);
        private Dictionary<int, SPokerPlayerController> playingplayerDict = new Dictionary<int, SPokerPlayerController>();


        private int myChairId;
        sevenPoker.Player myPlayer;

        private Partial.BetSizeType _bettingActionToggleState;
        private Partial.ActionType actionStateForServer;

        private bool isActionProcessing = false;
        private bool isActedInMyturn = false;

        private sevenPoker.Phase phaseState = Phase.PhNone;
        private bool isDdadangActedThisRound = false;
        private bool thisBetRoundAlreadyActed = false;

        private Dictionary<Partial.BetSizeType, long> betAmountDict =
            new Dictionary<Partial.BetSizeType, long>();

        private bool reserveLeaveRequest = false;
        private bool reserveMoveRoomRequest = false;

        private bool isExiled = false;
        private int playerSelectCardCount = 0;

        private Action<GameType, EmotionInfo> _onEmotionExpress;
        private Action<bool> _onEmojiUseChange;
        private Action<bool> _onJokboUseChange;
        private Func<SPokerPlayerController, bool, string> _onCardRecieved;

        bool isMyTurn = false;

        private sevenPoker.TurnNoti myTurnNotiInfo;
        private sevenPoker.TurnNoti currentTurnNotiInfo;

        private PlayerActionHistory currentRoundActionHistory = new PlayerActionHistory();

        private IGameRuleProvider ruleProvider;
        private List<GameRuleConfig> currentGameRules;
        private GameRuleConfig currentRoundGameRule;
        int ruleProvideindex = 0;

        public SPokerController(GameObject UpdateObject, SPokerViewer _view, CancellationTokenSource cts, IGameRuleProvider provider)
        {
            mainObject = UpdateObject;
            view = _view;
            Init();

            ruleProvider = provider;
            currentGameRules = ruleProvider.GetRuleConfig(GameType.SEVEN_POKER);
            currentRoundGameRule = currentGameRules[0];

            RegisterCallbackNoti();
            CPPlayer.Server.CallbackAfterSPokerConnect += RegisterCallbackNoti;
            CPPlayer.InGame.AFKPopupActive += MeUserBackFromInactive;
            AFKUserDetector().Forget();
        }

        public void StartSet()
        {
        }

        public void Dispose()
        {
            SPokerDispatchPushHub.OnEnterNoti -= EnterGameOtherPlayer_Noti;
            SPokerDispatchPushHub.OnLeaveNoti -= LeaveGameOtherPlayer_Noti;
            SPokerDispatchPushHub.OnStartNoti -= StartGame;
            SPokerDispatchPushHub.OnSelectNoti -= SelectCardNoti;
            SPokerDispatchPushHub.OnHandCardNoti -= HandCardNoti;
            SPokerDispatchPushHub.OnHandCardNotiOther -= HandCardNotiOther;
            SPokerDispatchPushHub.OnTurnNoti -= TurnChangedNoti;
            SPokerDispatchPushHub.OnActionNoti -= AcionNoti;
            SPokerDispatchPushHub.OnShowdownNoti -= ShowDownNoti;
            SPokerDispatchPushHub.OnResultNoti -= ResultNoti;
            SPokerDispatchPushHub.OnKickedNoti -= KickedForSomeReason;
            SPokerDispatchPushHub.OnEmoteNoti -= EmoticonExpressNoti;
            SPokerDispatchPushHub.OnBossMoveNoti -= BossMoveNoti;
            SPokerDispatchPushHub.OnCardOpenNoti -= CardOpenNoti;
            SPokerDispatchPushHub.OnLeaveReserveNoti -= LeaveReservedNoti;

            CPPlayer.Server.CallbackAfterSPokerConnect -= RegisterCallbackNoti;
            CPPlayer.InGame.AFKPopupActive -= MeUserBackFromInactive;
            CPPlayer.SPoker.EnterRoom -= EnterGameTable_Result;
            CPPlayer.Option.ReserveBetChange -= ToggleViewSetForReserveBetOption;
            CPPlayer.InGame.emotionExpressEvent -= _onEmotionExpress;
            CPPlayer.Option.EmojiUseChange -= _onEmojiUseChange;
            CPPlayer.Option.JokboUseChange -= _onJokboUseChange;
            CPPlayer.SPoker.CardRecieved -= _onCardRecieved;
        }

        private void RegisterCallbackNoti()
        {
            SPokerDispatchPushHub.OnEnterNoti -= EnterGameOtherPlayer_Noti;
            SPokerDispatchPushHub.OnLeaveNoti -= LeaveGameOtherPlayer_Noti;
            SPokerDispatchPushHub.OnStartNoti -= StartGame;
            SPokerDispatchPushHub.OnSelectNoti -= SelectCardNoti;
            SPokerDispatchPushHub.OnHandCardNoti -= HandCardNoti;
            SPokerDispatchPushHub.OnHandCardNotiOther -= HandCardNotiOther;
            SPokerDispatchPushHub.OnTurnNoti -= TurnChangedNoti;
            SPokerDispatchPushHub.OnActionNoti -= AcionNoti;
            SPokerDispatchPushHub.OnShowdownNoti -= ShowDownNoti;
            SPokerDispatchPushHub.OnResultNoti -= ResultNoti;
            SPokerDispatchPushHub.OnKickedNoti -= KickedForSomeReason;
            SPokerDispatchPushHub.OnEmoteNoti -= EmoticonExpressNoti;
            SPokerDispatchPushHub.OnBossMoveNoti -= BossMoveNoti;
            SPokerDispatchPushHub.OnCardOpenNoti -= CardOpenNoti;
            SPokerDispatchPushHub.OnLeaveReserveNoti -= LeaveReservedNoti;


            SPokerDispatchPushHub.OnEnterNoti += EnterGameOtherPlayer_Noti;
            SPokerDispatchPushHub.OnLeaveNoti += LeaveGameOtherPlayer_Noti;
            SPokerDispatchPushHub.OnStartNoti += StartGame;

            SPokerDispatchPushHub.OnSelectNoti += SelectCardNoti;
            SPokerDispatchPushHub.OnHandCardNoti += HandCardNoti;
            SPokerDispatchPushHub.OnHandCardNotiOther += HandCardNotiOther;

            SPokerDispatchPushHub.OnTurnNoti += TurnChangedNoti;
            SPokerDispatchPushHub.OnActionNoti += AcionNoti;

            SPokerDispatchPushHub.OnShowdownNoti += ShowDownNoti;
            SPokerDispatchPushHub.OnResultNoti += ResultNoti;

            SPokerDispatchPushHub.OnKickedNoti += KickedForSomeReason;
            SPokerDispatchPushHub.OnEmoteNoti += EmoticonExpressNoti;

            SPokerDispatchPushHub.OnBossMoveNoti += BossMoveNoti;
            SPokerDispatchPushHub.OnCardOpenNoti += CardOpenNoti;

            SPokerDispatchPushHub.OnLeaveReserveNoti += LeaveReservedNoti;
        }

        public void Init()
        {
            ChangeGamestate(SPokerState.None);
            snapShot = new SPokerTableSnapShot();
            snapShot.Init(view);
            phaseState = Phase.PhNone;

            CPPlayer.SPoker.EnterRoom += EnterGameTable_Result;
            betAmountDict.Clear();

            for (int i = 0; i < view.actionToggles.Count; i++)
            {
                var toggleItem = view.actionToggles[i];
                var bAt = toggleItem.ingameActionType;
                var bet = toggleItem.ingameBettingActionType;
                var toggle = toggleItem.toggle;

                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(ison => { ActionTogglePressed(bAt, bet, toggle, ison); });
                betAmountDict[bet] = 0;
            }

            myPlayer = new Player()
            {
                Uid = CPPlayer.SPoker.ingameUid,
                Nick = CPPlayer.UserInfo.userDatabase.User.Nick,
            };
            _bettingActionToggleState = Partial.BetSizeType.BsEnd;

            view.leaveBtn.onClick.AddListener(() => LeaveThisRoomOrReserve(isExiled).Forget());
            view.leaveReservedObj.onClick.AddListener(() => CancelReserveLeave().Forget());

            view.moveRoomBtn.onClick.AddListener(() => LeaveThisRoomAndMoveOtherRoomOrReserve().Forget());
            view.moveReservedObj.onClick.AddListener(() => CancelReserveMove().Forget());

            view.winnerDetailPanel.SetActive(false);
            view.showdownPanel.SetActive(false);

            view.jackpotDetailPanel.SetActive(false);
            view.jackpotDetailBackEffect.SetActive(false);

            view.optionBtn.onClick.AddListener(() =>
            {
                var optionWindow = ViewCanvas.Get<ViewCanvasInGame>().ingameOptionWindow_SPoker;
                if (optionWindow.gameObject.activeInHierarchy == false)
                {
                    optionWindow.OpenWindow();
                    view.OpenModalObject(optionWindow.gameObject);
                }
                else
                {
                    optionWindow.CloseWindow();
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
            view.jokboWindow.SetActive(false);

            CPPlayer.Option.ReserveBetChange += ToggleViewSetForReserveBetOption;
            _onEmotionExpress = (o, t) => { EmoticonExpressReq(t).Forget(); };
            CPPlayer.InGame.emotionExpressEvent += _onEmotionExpress;

            view.selectCardViewer.Init();

            _onCardRecieved = (con, isshowdown) =>
            {
                ShowSelectWindowAndHandle().Forget();
                return null;
            };
            CPPlayer.SPoker.CardRecieved += _onCardRecieved;

            //option callback
            _onEmojiUseChange = (active) =>
            {
                view.showEmoticonViewBtn.gameObject.SetActive(active);
                if (active == false)
                {
                    view.emotionView.gameObject.SetActive(false);
                    view.OnModalAutoClose(view.emotionView.gameObject);
                }
            };
            CPPlayer.Option.EmojiUseChange += _onEmojiUseChange;

            _onJokboUseChange = (active) =>
            {
                view.openJokboWindowBtn.gameObject.SetActive(active);
                if (active == false)
                {
                    view.jokboWindow.gameObject.SetActive(false);
                    view.OnModalAutoClose(view.jokboWindow);
                }
            };
            CPPlayer.Option.JokboUseChange += _onJokboUseChange;

            view.showEmoticonViewBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.useEmoji);
            view.openJokboWindowBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.jokboInform);
        }

        public void OnOtherPlayerModalInactive(int chairId)
        {
            foreach (var spokerplayer in playerDict)
            {
                if (spokerplayer.Value.chairId == chairId)
                    continue;
                spokerplayer.Value.InfoModalInactive();
            }
        }

        public EnterRes enterresInfo;

        private void EnterGameTable_Result(sevenPoker.EnterRes enterRes)
        {
            view.emotionView.Init();
            enterresInfo = enterRes;
            foreach (var data in enterRes.Config)
            {
                CPPlayer.Server.visualEffectTimeConfig[data.Key] = data.Value;
            }
            

            if (enterRes.Pots.Count > 0)
            {
                view.currentPotAmount.text = Extension.ToKoreanFormat(enterRes.Pots[0], Extension.KoreanFormatMode.Planning);
            }
            else
            {
                view.currentPotAmount.text = "0";
            }

           

            ChangeGamestate(SPokerState.Idle);
            InitializeOnEnter();
            CPPlayer.SPoker.currentTableId = enterRes.TableId;
            Debug.Log($"current table id: {CPPlayer.SPoker.currentTableId}");
            SetPlayersInfo();
        }

        private void EnterGameOtherPlayer_Noti(EnterNoti enterNoti, int revisionId)
        {
            Extension.eLog($"mychair ID: {enterNoti.ChairId}", Color.magenta);
            SPokerPlayerController other;
            if (playerDict.ContainsKey(enterNoti.ChairId))
            {
                other = playerDict[enterNoti.ChairId];
                other.SetPlayer(enterNoti.Player, enterNoti.ChairId);
            }
            else
            {
                other = new SPokerPlayerController(mainObject.transform, view,this);
                other.SetPlayer(enterNoti.Player, enterNoti.ChairId);
                playerDict.Add(enterNoti.ChairId, other);
            }
        }

        public void SetPlayersInfo()
        {
            var mychair = enterresInfo.Chairs.FirstOrDefault(o => o.Id == enterresInfo.ChairId);
            if (mychair != null)
            {
                myPlayer = mychair.Player;
            }

            SPokerPlayerController me;
            if (playerDict.ContainsKey(enterresInfo.ChairId))
            {
                me = playerDict[enterresInfo.ChairId];
                me.SetPlayer(myPlayer, enterresInfo.ChairId, true);
            }
            else
            {
                me = new SPokerPlayerController(mainObject.transform, view,this);
                me.SetPlayer(myPlayer, enterresInfo.ChairId, true);
                playerDict.Add(enterresInfo.ChairId, me);
            }

            myChairId = enterresInfo.ChairId;
            Extension.eLog($"mychair ID: {myChairId}", Color.magenta);

            foreach (var enterResChair in enterresInfo.Chairs)
            {
                if (enterResChair.Player == null)
                    continue;
                
             
                SPokerPlayerController other;
                if (enterResChair.Id != myChairId)
                {
                    if (playerDict.ContainsKey(enterResChair.Id))
                    {
                        other = playerDict[enterResChair.Id];
                        other.SetPlayer(enterResChair.Player, enterResChair.Id);
                    }
                    else
                    {
                        other = new SPokerPlayerController(mainObject.transform, view,this);
                        other.SetPlayer(enterResChair.Player, enterResChair.Id);
                        playerDict.Add(enterResChair.Id, other);
                    }
                }
                else
                {
                    other = playerDict[myChairId];
                }
              

                //카드 뿌리는 중간에 와서 카드가 이미 유저에게 있는 상태
                if (enterResChair.Player.CardCount > 0 && enterresInfo.InGame)
                {
                    for (int i = 0; i < enterResChair.Player.CardCount; i++)
                    {
                        string cardinfo = "";
                        if (i >= 2 && i < 6)
                        {
                            if(i-2<enterResChair.Player.OpenCards.Count)
                                cardinfo = enterResChair.Player.OpenCards[i-2];
                        }
                        snapShot.SetCardToOtherPlayerAtEnter(other,cardinfo,enterResChair.Player.CardCount);
                    }
                }
            }

            if (enterresInfo.InGame)
            {
                var playerCount = enterresInfo.Chairs.Where(o => o.Player != null && o.Player.CardCount > 0).Count();
                startPlayerCount = playerCount;
            }
            
        }

        private void LeaveGameOtherPlayer_Noti(LeaveNoti leaveNoti, int revisionId)
        {
            //Debug.Log("리브노티 받을때 내 체어id" + myChairId.ToString());
            Debug.Log("리브노티" + leaveNoti.ChairId.ToString());
            if (playerDict.ContainsKey(leaveNoti.ChairId))
            {
                if (leaveNoti.ChairId == myChairId)
                {
                    if (leaveNoti.Reason == KickReason.KrNone)
                    {
                        if (reserveMoveRoomRequest)
                        {
                            CPPlayer.InGame.MoveTable?.Invoke(GameType.SEVEN_POKER);
                        }
                        else
                        {
                            CPPlayer.SPoker.currentTableId = 0;
                            CPPlayer.InGame.LeaveGame?.Invoke(GameType.SEVEN_POKER);
                        }
                    }
                    else
                    {
                        CPPlayer.SPoker.currentTableId = 0;
                        CPPlayer.InGame.LeaveGame?.Invoke(GameType.SEVEN_POKER);
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
        }

        public async UniTask LeaveRequestProcess()
        {
            if (reserveLeaveRequest)
            {
                await LeaveThisRoomOrReserve(isExiled);
            }

            if (reserveMoveRoomRequest)
            {
                await LeaveThisRoomAndMoveOtherRoomOrReserve();
            }
        }

        private async UniTask LeaveThisRoomOrReserve(bool isExile, bool isNotiOut = false)
        {
            if (isNotiOut == false)
            {
                var leaveResPacket = await Services.SevenPoker.LeaveRoomAsync(CPPlayer.SPoker.currentTableId);
                if (leaveResPacket.IsSuccess)
                {
                    Extension.eLog($"나갈때 응답 테이블id:{leaveResPacket.Data.TableId} 현재 테이블 id:{CPPlayer.SPoker.currentTableId}");
                    if (leaveResPacket.Data.IsReserved)
                    {
                        if (isExile)
                        {
                            isExiled = true;
                        }

                        reserveLeaveRequest = true;
                        
                        view.leaveBtn.gameObject.SetActive(!reserveLeaveRequest);
                        view.leaveReservedObj.gameObject.SetActive(reserveLeaveRequest);
                        
                        MoveRoomBtnInit();
                        
                        if (TryGetPlayer(myChairId, out var selfPlayer))
                            selfPlayer.ReserveOut(true);

                        return;
                    }
                }
            }

            view.leaveBtn.enabled = false;
            view.moveRoomBtn.enabled = false;

            LeaveGameDataInitialize();
            CPPlayer.SPoker.currentTableId = 0;
            CPPlayer.InGame.LeaveGame?.Invoke(GameType.SEVEN_POKER);

            await UniTask.Yield();
        }

        private async UniTask LeaveThisRoomAndMoveOtherRoomOrReserve()
        {
            if (isExiled)
                return;
            if (CPPlayer.InGame.isMovingTable)
                return;

            CPPlayer.InGame.isMovingTable = true;
            
            var leaveResPacket = await Services.SevenPoker.LeaveRoomAsync(CPPlayer.SPoker.currentTableId);
            if (leaveResPacket.IsSuccess)
            {
                Extension.eLog($"응답 테이블id:{leaveResPacket.Data.TableId} 현재 테이블 id:{CPPlayer.SPoker.currentTableId}");
                if (leaveResPacket.Data.IsReserved)
                {
                    reserveMoveRoomRequest = true;
                    
                    view.moveRoomBtn.gameObject.SetActive(false);
                    
                    view.moveRoomBtn.enabled = !reserveMoveRoomRequest;
                    view.moveReservedObj.gameObject.SetActive(reserveMoveRoomRequest);
                    
                    LeaveRoomBtnInit();
                    
                    if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                        myPlayerCtrl.ReserveOut(true);
                    
                    CPPlayer.InGame.isMovingTable = false;
                    
                    return;
                }
            }

            view.leaveBtn.enabled = false;
            view.moveRoomBtn.enabled = false;


            LeaveGameDataInitialize();
            CPPlayer.InGame.MoveTable?.Invoke(GameType.SEVEN_POKER);
        }

        async UniTask CancelReserveLeave()
        {
            if (isExiled)
                return;
            
            CPPlayer.InGame.isMovingTable = false;

            var leaveResPacket = await Services.SevenPoker.LeaveRoomCacnelAsync(CPPlayer.SPoker.currentTableId);
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
            var leaveResPacket = await Services.SevenPoker.LeaveRoomCacnelAsync(CPPlayer.SPoker.currentTableId);
            if (leaveResPacket.IsSuccess)
            {
                MoveRoomBtnInit();
            }
        }


        void LeaveRoomBtnInit()
        {
            reserveLeaveRequest = false;
            if (reserveLeaveRequest == false)
            {
                view.leaveBtn.enabled = true;
                isExiled = false;
                view.leaveBtn.gameObject.SetActive(true);
                view.leaveReservedObj.gameObject.SetActive(false);
                if (TryGetPlayer(myChairId, out var selfPlayer))
                    selfPlayer.ReserveOut(false);
            }
        }

        void MoveRoomBtnInit()
        {
            reserveMoveRoomRequest = false;
            CPPlayer.InGame.isMovingTable = false;
            
            view.moveRoomBtn.enabled = true;
            view.moveRoomBtn.gameObject.SetActive(true);
            view.moveReservedObj.gameObject.SetActive(false);
            
            if (TryGetPlayer(myChairId, out var myPlayerCtrl))
                myPlayerCtrl.ReserveOut(false);
        }


        private StartNoti startNotiInfo;

        private int startPlayerCount = 0;

        private void StartGame(StartNoti startNoti, int revisionId)
        {
            startNotiInfo = startNoti;
            foreach (var data in startNotiInfo.Config)
            {
                CPPlayer.Server.visualEffectTimeConfig[data.Key] = data.Value;
            }
            ChangeGamestate(SPokerState.Start);
            InitializeOnGameStart();
            
            PresentAtStartSet(revisionId).Forget();
        }
        
        async UniTask PresentAtStartSet(int revisionId)
        {
            await UniTask.NextFrame();
            PresentAtStartSetAfterFrame(revisionId);
        }

        void PresentAtStartSetAfterFrame(int revisionId)
        {
            view.currentPotAmount.text = Extension.ToKoreanFormat(startNotiInfo.PotAmount, Extension.KoreanFormatMode.Planning);

            playingplayerDict.Clear();
            startPlayerCount = startNotiInfo.Players.Count;

            foreach (var player in playerDict)
            {
                playingplayerDict.Add(player.Key, player.Value);
            }

            if (TryGetPlayer(myChairId, out var selfPlayer))
            {
                selfPlayer.SetCurrentPhase(SPokerState.Start);
                selfPlayer.SetCardOpenBtn();
                selfPlayer.SetFold(false);
                selfPlayer.SetAllin(false);
            }
            //player setting

            if (revisionId != SPokerDispatchPushHub.revisionId)
            {
                foreach (var startNotiPlayer in startNotiInfo.Players)
                {
                    if (!TryGetPlayer(startNotiPlayer.ChairId, out var startPlayer)) continue;
                    //ante수집
                    startPlayer.SetCurrentOwnedChip(startNotiPlayer.Chip);
                    startPlayer.AddBetThisRound( startNotiPlayer.Ante );
                    if (startNotiPlayer.ChairId == myChairId)
                    {
                        startPlayer.SetTotalBet(startNotiPlayer.Ante);
                        CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                        CPPlayer.InGame.haveKickVote = startNotiPlayer.CanKickVote;
                    }
                }
            }
            else
            {
                foreach (var startNotiPlayer in startNotiInfo.Players)
                {
                    if (!TryGetPlayer(startNotiPlayer.ChairId, out var startPlayer)) continue;
                    //ante수집
                    startPlayer.SetCurrentOwnedChip(startNotiPlayer.Chip);
                    snapShot.ThrowAnte(startNotiPlayer.ChairId, startNotiPlayer.Ante, startPlayer.view.throwChipStartPos);
                    startPlayer.AddBetThisRound( startNotiPlayer.Ante );
                    if (startNotiPlayer.ChairId == myChairId)
                    {
                        startPlayer.SetTotalBet(startNotiPlayer.Ante);
                        CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                        CPPlayer.InGame.haveKickVote = startNotiPlayer.CanKickVote;
                    }
                }
            }
            ChangeGamestate(SPokerState.CardSelect);
        }
   

        private async UniTaskVoid ShowSelectWindowAndHandle()
        {
            if (CPPlayer.SPoker.currentSPokerState >= SPokerState.Round_1)
                return;

            foreach (var playerCon in playingplayerDict)
            {
                if (playerCon.Value.chairId == myChairId)
                    continue;
                playerCon.Value.DotAnimationStart();
            }

            if (!TryGetPlayer(myChairId, out var selfPlayer))
                return;
            var playerCardList = selfPlayer.spokerPlayerInfo.cardlist;

            view.selectPopupBackGround.SetActive(true);

            var selectCardList = await view.selectCardViewer.OpenViewAsync(playerCardList);

            view.selectPopupBackGround.SetActive(false);


            if (selectCardList.Count == 0)
            {
                Debug.Log($"선택카드가 없음");
                return;
            }

            string dropcard = playerCardList[selectCardList[0]];
            string openCard = playerCardList[selectCardList[1]];

            //Debug.LogError($"선택창 카드 버린카드:{dropcard},오픈카드:{openCard}");

            var res = await Services.SevenPoker.CardSelectReqAsync(CPPlayer.SPoker.currentTableId, dropcard, openCard);
            if (res.IsSuccess)
            {
                ChangeGamestate(SPokerState.Round_1);
            }
            else
            {
                
            }
        }


        private void SelectCardNoti(SelectNoti selectNoti, int revisionId)
        {
            if (selectNoti.ChairId == myChairId)
            {
                view.selectCardViewer.CloseView();

                string dropcard = selectNoti.DropCard;
                string openCard = selectNoti.OpenCard;

                Extension.eLog($" 셀렉트 노티:dropcard:{dropcard}, openCard:{openCard}");

                if (!TryGetPlayer(myChairId, out var selectSelfPlayer))
                    return;
                
                Debug.Log($"내가 가진 카드:{string.Join(",",selectSelfPlayer.spokerPlayerInfo.cardlist)}//내 체어아이디:{selectNoti.ChairId}//오픈카드:{openCard},드랍카드 {dropcard}");
                
                var currentCards = selectSelfPlayer.spokerPlayerInfo.cardlist;
                var dropcardIndex = currentCards.IndexOf(dropcard);
                snapShot.CardDrop(selectSelfPlayer, dropcardIndex);
                selectSelfPlayer.SetCardAfterSelectState(dropcardIndex, openCard);
            }
            else
            {
                string openCard = selectNoti.OpenCard;

                if (TryGetPlayer(selectNoti.ChairId, out var selectPlayer))
                {
                    var currentCards = selectPlayer.spokerPlayerInfo.cardlist;
                    var dropcardIndex = 2;
                    var openCardcardIndex = 3;
                    
                    Debug.Log($"가진 카드:{string.Join(",",selectPlayer.spokerPlayerInfo.cardlist)}//체어아이디:{selectNoti.ChairId}//오픈카드:{openCard},드랍카드인덱스 {selectNoti.DropCard}");
                    snapShot.CardDrop(selectPlayer, dropcardIndex);
                    selectPlayer.SetCardAfterSelectState(dropcardIndex, openCard);
                }
            }

            if (TryGetPlayer(selectNoti.ChairId, out var dotAnimPlayer))
                dotAnimPlayer.DotAnimationEnd();

            
            int selectUserCount = playerDict.Values.Count(p => p.spokerPlayerInfo.cardlist.Count==3);
            if (selectUserCount >= startPlayerCount)
            {
                foreach (var spokerPlayerController in playerDict)
                {
                    spokerPlayerController.Value.SetCardImageAfterAllSelect();
                }
            }

            ChangeGamestate(SPokerState.Round_1);
        }

        private void HandCardNoti(HandCardNoti myCardnoti, int revisionId)
        {
            Extension.eLog($"card draw! to me", Color.cyan);
            if (!TryGetPlayer(myChairId, out var selfPlayer))
                return;
            snapShot.CardThrowToPlayer(selfPlayer, myCardnoti);

            if (spokerState == SPokerState.Round_1)
            {
                foreach (var spokerPlayerController in playingplayerDict)
                {
                    if (spokerPlayerController.Value.isFolded)
                    {
                        spokerPlayerController.Value.DotAnimationEnd();
                        continue;
                    }

                    // if (spokerPlayerController.Value.spokerPlayerInfo.cardlist.Count <= 4)
                    // {
                    //     spokerPlayerController.Value.SetCardImageAfterAllSelect();
                    //     spokerPlayerController.Value.DotAnimationEnd();    
                    // }
                }
            }
        }

        private void HandCardNotiOther(HandCardNotiOther otherCardnoti, int revisionId)
        {
            Extension.eLog($"card draw! to {otherCardnoti.ChairId} player", Color.cyan);
            if (!TryGetPlayer(otherCardnoti.ChairId, out var otherCardPlayer)) return;
            snapShot.CardThrowToPlayer(otherCardPlayer, otherCardnoti);
        }

        private void TurnChangedNoti(TurnNoti turnNoti, int revisionId)
        {
            if (!TryGetPlayer(turnNoti.ChairId, out var turnPlayer)) return;
            if (turnPlayer.isAllin)
                return;
            Extension.eLog($"{turnNoti.ChairId} turn changed. {turnNoti.Phase}");
            currentTurnNotiInfo = turnNoti;
            isMyTurn = turnNoti.ChairId == myChairId;

            ruleProvideindex = 0;
            switch (turnNoti.Phase)
            {
                case Phase.Ph1R:
                    ruleProvideindex = 0;
                    ChangeGamestate(SPokerState.Round_1);
                    break;
                case Phase.Ph2R:
                    ruleProvideindex = 1;
                    ChangeGamestate(SPokerState.Round_2);
                    break;
                case Phase.Ph3R:
                    ruleProvideindex = 2;
                    ChangeGamestate(SPokerState.Round_3);
                    break;
                case Phase.Ph4R:
                    ruleProvideindex = 3;
                    ChangeGamestate(SPokerState.Round_4);
                    break;
            }

            currentRoundGameRule = currentGameRules[ruleProvideindex];

            if (isMyTurn)
            {
                if (TryGetPlayer(myChairId, out var selfPlayer))
                    selfPlayer.SetTotalBet(turnNoti.TotalBet);
                myTurnNotiInfo = turnNoti;
            }
            turnPlayer.BetImageActive(false);

            //turn넘어가다가 phase 바꾸면 바꿔줌
            bool isPhaseChanged = false;
            if (phaseState != turnNoti.Phase)
            {
                currentRoundActionHistory.ResetForNewRound();
                phaseState = turnNoti.Phase;
                isPhaseChanged = true;
                isDdadangActedThisRound = false;
                thisBetRoundAlreadyActed = false;
                foreach (var spokerPlayerController in playerDict)
                {
                    spokerPlayerController.Value.BetImageActive(false);
                    spokerPlayerController.Value.ClearCurrentRoundBetHistory();
                }
            }
    
            
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

        #region actionToggle setting

        void ToggleViewAndBetAmountSetting(sevenPoker.TurnNoti turnNoti, bool isMyTurn, bool alreadyActed = false)
        {
            if (playingplayerDict.ContainsKey(turnNoti.ChairId) == false)
                return;
            if (!TryGetPlayer(myChairId, out var selfPlayer))
                return;
            if (selfPlayer.isFolded)
                return;
            if (selfPlayer.isAllin)
                return;
            
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
                        betAmountDict[bettingActionType] = CPPlayer.SPoker.initialBuyIn;
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
                            Constraints.MaxBetChip - selfPlayer.GetTotalBet;
                        break;
                    default:
                        break;
                }
            }

            ToggleViewInitialSetting();
            //view.actionToggleObject.transform.SetParent(view.bettingActiveParent, false);

            //bet 가능 금액 계산
            var possibleChipforMax = Constraints.MaxBetChip - selfPlayer.GetTotalBet;
            var possibleChipforMyChip = CPPlayer.UserInfo.userDatabase.User.Gold;

            var possibleBetChip = System.Math.Min(possibleChipforMax, possibleChipforMyChip);

            
            //toggle activate setting
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
                //콜값 유무에 따른 체크,삥,콜,따당 활성화 처리
                if (turnNoti.CallChip <= 0)
                {
                    if (possibleBetChip > CPPlayer.Badugi.initialBuyIn && selfPlayer.GetTotalBet + CPPlayer.Badugi.initialBuyIn < Constraints.MaxBetChip)
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
                            if (possibleBetChip > turnNoti.CallChip * 2 && selfPlayer.GetTotalBet + turnNoti.CallChip * 2 < Constraints.MaxBetChip && thisBetRoundAlreadyActed == false)
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


            if (isMyTurn == false && CPPlayer.Cloud.optionValue.reserveBet == false)
            {
                AllActionTogglesDeactivate();
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
            //view.actionToggleObject.transform.SetParent(view.bettingInActiveParent, false);
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

        #endregion


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

            var tempbetSizeType = ProtoMapper.SevenPokerBettingActionType(tempBetActiontoggleType);
            var tempactionType = ProtoMapper.SevenPokerActionType(actionStateForServer);

            var actionRes = await Services.SevenPoker.ActionAsync(snapShot.RoomImfo.TableId, tempactionType, amount, tempbetSizeType);
            if (actionRes.IsSuccess)
            {
                if (TryGetPlayer(myChairId, out var selfPlayer))
                {
                    selfPlayer.SetTotalBet(actionRes.Data.TotalBet);
                    selfPlayer.SetAction(actionType, tempBetActiontoggleType, amount, actionRes.Data.Chip);
                }
                view.currentPotAmount.text = Extension.ToKoreanFormat(actionRes.Data.PotAmount, Extension.KoreanFormatMode.Planning);

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

                //CPPlayer.InGame.errorToastPopup?.Invoke($"Server error Occured.\nMessage:{actionResPacket.Error}");
                Extension.eLog($"Server error Occured.\nMessage:{actionRes.Error}");
                return;
            }


            if (betSizeType == Partial.BetSizeType.BsDdadang)
            {
                isDdadangActedThisRound = true;
            }


            if (betSizeType == Partial.BetSizeType.BsBbing)
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
            if (TryGetPlayer(myChairId, out var actionSelfPlayer))
            {
                actionSelfPlayer.ActionToDisplay(tempBetActiontoggleType);
                actionSelfPlayer.SetEndTurn(true);
                actionSelfPlayer.SetFold(actionType == Partial.ActionType.AtFold);
                if (actionType == Partial.ActionType.AtAllin)
                    actionSelfPlayer.SetAllin(true);
            }

            if (actionType == Partial.ActionType.AtFold)
            {
                LeaveRequestProcess().Forget();
            }
        }

        async UniTask ActionProcess(Partial.ActionType actionType, Partial.BetSizeType ingameBettingActionType)
        {
            long amount = betAmountDict[ingameBettingActionType];
            //버튼 누르는 것에 따라 추후 계산하여 amount

            var betSizeType = ProtoMapper.SevenPokerBettingActionType(ingameBettingActionType);
            var _actionType = ProtoMapper.SevenPokerActionType(actionType);

            var actionRes = await Services.SevenPoker.ActionAsync(snapShot.RoomImfo.TableId, _actionType, amount, betSizeType);
            if (actionRes.IsSuccess)
            {
                if (TryGetPlayer(myChairId, out var selfPlayer))
                {
                    selfPlayer.SetTotalBet(actionRes.Data.TotalBet);
                    selfPlayer.SetAction(actionType, ingameBettingActionType, amount, actionRes.Data.Chip);
                }
                view.currentPotAmount.text = Extension.ToKoreanFormat(actionRes.Data.PotAmount, Extension.KoreanFormatMode.Planning);

                CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
            }
            else
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"Server error Occured.\nMessage:{actionRes.Error}"));
            }
        }

        private void AcionNoti(ActionNoti actionNoti, int revisionId)
        {
            view.currentPotAmount.text = Extension.ToKoreanFormat(actionNoti.PotAmount, Extension.KoreanFormatMode.Planning);
            if (!TryGetPlayer(actionNoti.ChairId, out var actionPlayer)) return;
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

            actionPlayer.SetAction(actionNoti.actionType, tempType, actionNoti.Amount, actionNoti.Chip);
            actionPlayer.ActionToDisplay(actionNoti.betSizeType);
            actionPlayer.SetFold(actionNoti.Action == ActionType.AtFold);

            if (actionNoti.Action == ActionType.AtAllin)
            {
                actionPlayer.SetAllin(true);
            }

            if (actionNoti.ChairId == myChairId)
            {
                thisBetRoundAlreadyActed = true;
                _bettingActionToggleState = Partial.BetSizeType.BsEnd;

                AllActionTogglesDeactivate();

                CPPlayer.InGame.AFKPopupActive?.Invoke(true);
                CPPlayer.InGame.isUserAFK = true;
            }
            else
            {
                if (actionNoti.Action==ActionType.AtRaise)
                {
                    thisBetRoundAlreadyActed = false;
                }
            }

            actionPlayer.SetEndTurn(actionNoti.ChairId == myChairId);
        }

        private void ShowDownNoti(ShowdownNoti showdownNoti, int revisionId)
        {
            ChangeGamestate(SPokerState.ShowDown);
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.SetCurrentPhase(SPokerState.ShowDown);
            PresentationShowDown(showdownNoti).Forget();
        }

        private async UniTask PresentationShowDown(sevenPoker.ShowdownNoti showdownNoti)
        {
            view.showdownPanel.SetActive(true);

            int showdownAnimationMilSec = (int)CPPlayer.Server.visualEffectTimeConfig["1R_SHOWDOWN_MS"];
            float showdownAnimTime = (float)showdownAnimationMilSec / 1000f;
            var animator = view.showdownPanelAnimator;
            animator.speed = 3.0f / showdownAnimTime;
            animator.Play("Showdown");

            await UniTask.Delay(showdownAnimationMilSec);

            view.showdownPanel.SetActive(false);
        }

        private void ResultNoti(ResultNoti resultNoti, int revisionId)
        {
            ChangeGamestate(SPokerState.Result);
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.SetCurrentPhase(SPokerState.Result);
            snapShot.SetShowDownPotInfo(resultNoti.Pots);
            PresentationResult(resultNoti).Forget();
        }

        private CancellationTokenSource _resultPresentationCts;

        private async UniTask PresentationResult(sevenPoker.ResultNoti resultNoti)
        {
            _resultPresentationCts?.Cancel();
            _resultPresentationCts = new CancellationTokenSource();
            var token = _resultPresentationCts.Token;

            try
            {
                bool isShowdownNeed = true;
                int playerCount = 0;
                foreach (var resultplayer in resultNoti.Players)
                {
                    if(playerDict.ContainsKey(resultplayer.ChairId)==false)
                        continue;
                    if (resultplayer.HandCards.Count > 0)
                    {
                        if (playerDict[resultplayer.ChairId].isFolded)
                        {
                            continue;
                        }

                        playerCount++;
                    }
                }

                //기존 UI display 처리(pot 사라짐, 족보 안내버튼 사라짐 등)
                SetTableUIForResult();
                //기존 UI display 처리(pot 사라짐, 족보 안내버튼 사라짐 등)

                await ResultDisplayEvent(resultNoti, true, token);
                ResultDisplayEvent(resultNoti, false, token).Forget();

                //메인팟의 winner 배열 가져와서 정하기
                var mainWinner = resultNoti.Pots[0].Wins;
                sevenPoker.ResultNoti.Types.Player mainWinnerPlayer = null;

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
                    if (!TryGetPlayer(gameplayer.ChairId, out var gamePlayer)) continue;
                    gamePlayer.SetWinnerUI(isMainPotWin);
                    if (playerCount == 0)
                    {
                        gamePlayer.view.winJokboName.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Forfeit].StringToLocal;
                    }

                    gamePlayer.SetResultInfo(gameplayer, isMainPotWin);
                    if (gameplayer.HandCards.Count <= 0)
                        continue;

                    string mainwinrank = gamePlayer.resultRankString;

                    if (isMainPotWin)
                    {
                        mainWinRankText = mainwinrank;
                        gamePlayer.view.winJokboName.text = mainWinRankText;
                    }

                    if (gameplayer.Jackpot > 0)
                    {
                        isJackpotExist = true;
                        jackpotAmount = gameplayer.Jackpot;
                    }

                    if (!isMainPotWin)
                    {
                        
                        float dieDim = (float)CPPlayer.Server.visualEffectTimeConfig["DIE_ME_DIM_MS"]/1000f;

                        gamePlayer.view.inActiveMask.SetActive(true);

                        var c = gamePlayer.view.inactivemaskImage.color;
                        c.a = 0f;
                        gamePlayer.view.inactivemaskImage.color = c;

                        gamePlayer.view.inactivemaskImage.DOFade(0.5f, dieDim);

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
                   // view.winnerDetailPanel.SetActive(true);
                }

                if (playerCount == 0)
                {
                    view.winnerCardRank.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Forfeit].StringToLocal;
                    if (mainWinnerPlayer != null && mainWinnerPlayer.ChairId == myChairId)
                    {
                        if (TryGetPlayer(mainWinnerPlayer.ChairId, out var forfeitWinPlayer))
                            forfeitWinPlayer.SetCardOpenAtForfeitWin();
                    }
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


                foreach (var showdownplayer in resultNoti.Players)
                {
                    if(playerDict.ContainsKey(showdownplayer.ChairId))
                        playerDict[showdownplayer.ChairId].SetCurrentOwnedChip(showdownplayer.Chip);
                }


                //showdown 끝날때까지 대기시간 위 연출 종료까지 총 4초 아래에서 딜레이 후 idle로 넘어감
                ChangeGamestate(SPokerState.End);

                //result에서 업적확인 noti
                CheckAchievement(resultNoti);
                //result에서 업적확인 noti
                
                int delayforResultInit = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_SHOW_MS"];
                int cardOpenInActiveTime= (int)CPPlayer.Server.visualEffectTimeConfig["OPEN_HIDE_MS"];
                await UniTask.Delay(delayforResultInit-cardOpenInActiveTime, cancellationToken: token);
                if (TryGetPlayer(myChairId, out var selfPlayer))
                    selfPlayer.CardOpenBtnObjActive(false);
               
                await UniTask.Delay(cardOpenInActiveTime, cancellationToken: token);

                //기존 UI display 다시 재생성 처리
                SetTableUIForResultEnd();
                //기존 UI display 다시 재생성 처리

                await LeaveRequestProcess();

                InitializeOnEndGame();

                _resultPresentationCts?.Dispose();
                _resultPresentationCts = null;
            }
            catch (Exception e)
            {
                Debug.Log("PresentationResultEvent가 유저 나가기로 인해 취소되었습니다.");
            }
        }

        void SetTableUIForResult()
        {
            //toggle 비활성
            AllActionTogglesDeactivate();

            view.potAmountObject.SetActive(false);
            view.openJokboWindowBtn.gameObject.SetActive(false);

            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.SetCardOpenBtn();
        }

        void SetTableUIForResultEnd()
        {
            view.potAmountObject.SetActive(true);
            view.openJokboWindowBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.jokboInform);
        }

        async UniTask ResultDisplayEvent(sevenPoker.ResultNoti resultNoti, bool toLiveUser, CancellationToken token)
        {
            var tasks = new List<UniTask>();
            var mainWinner = resultNoti.Pots[0].Wins;
            sevenPoker.ResultNoti.Types.Player mainWinnerPlayer = null;

            int cardOpenTime = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_MS"];
            int eachUserWaitTime = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_WAIT_MS"];


            foreach (var gameplayer in resultNoti.Players)
            {
                if (!TryGetPlayer(gameplayer.ChairId, out var resultDisplayPlayer)) continue;
                if (toLiveUser == resultDisplayPlayer.isFolded)
                {
                    continue;
                }

                bool ismainWinner = mainWinner.Any(o => o.ChairId == gameplayer.ChairId);
                tasks.Add(resultDisplayPlayer.SetInfoForResult(gameplayer, ismainWinner, token));

                if (toLiveUser)
                {
                    await UniTask.Delay((cardOpenTime + eachUserWaitTime), cancellationToken: token);
                }
            }

            if (toLiveUser)
            {
                //여기까지 하여 개인별 데이터 전달 및 ui 구성 완료
                await UniTask.WhenAll(tasks).AttachExternalCancellation(token);
            }
        }

        void CheckAchievement(sevenPoker.ResultNoti resultNoti)
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


        #region initialize setting

        public void InitializeOnEnter()
        {
            view.showEmoticonViewBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.useEmoji);
            view.openJokboWindowBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.jokboInform);
            snapShot.ClearDataInRoundGame();
            snapShot.SetGameInfo(enterresInfo);
            
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();
            ToggleViewInitialSetting();
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

            for (int i = 0; i < view.playerViewList.Length; i++)
            {
                view.playerViewList[i].gameObject.SetActive(false);
            }

            view.emotionView.ActiveWindow(false);
            view.potAmountObject.SetActive(true);

            myPlayer.Chip = CPPlayer.UserInfo.userDatabase.User.Gold;
            CPPlayer.SPoker.currentTableId = enterresInfo.TableId;
            CPPlayer.SPoker.gapBetweenChairIdAndIndex = enterresInfo.ChairId;
            CPPlayer.InGame.haveKickVote = false;
            reserveLeaveRequest = false;
            reserveMoveRoomRequest = false;
            isExiled = false;
        }

        public void InitializeOnGameStart()
        {
            snapShot.ClearDataInRoundGame();
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();
            ToggleViewInitialSetting();
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

            playerSelectCardCount = 0;

            currentRoundActionHistory.ResetForNewRound();
            isDdadangActedThisRound = false;
        }

        public void InitializeOnEndGame()
        {
            snapShot.ClearDataInRoundGame();
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();
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
            view.moveRoomBtn.gameObject.SetActive(true);
            view.moveReservedObj.gameObject.SetActive(false);

            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.ReserveOut(false);


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

            var removeKeysforPlayinguser = new List<int>();
            foreach (var kv in playingplayerDict)
            {
                removeKeysforPlayinguser.Add(kv.Key);
            }

            foreach (var key in removeKeysforPlayinguser)
            {
                playingplayerDict.Remove(key);
            }

            playerDict.Clear();
            playingplayerDict.Clear();

            if (_resultPresentationCts != null && !_resultPresentationCts.Token.IsCancellationRequested)
            {
                _resultPresentationCts.Cancel();
            }
            CPPlayer.InGame.AFKPopupActive?.Invoke(false);
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
            view.jokboWindow.SetActive(false);
            view.potAmountObject.SetActive(true);

            //CPPlayer.InGame.AFKPopupActive?.Invoke(false);
            CPPlayer.InGame.isUserAFK = false;
            CPPlayer.InGame.AFKPopupActiveFlag = false;
            thisBetRoundAlreadyActed = false;

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
            if (TryGetPlayer(myChairId, out var selfPlayer))
            {
                selfPlayer.SetCurrentPhase(SPokerState.End);
                selfPlayer.SetCardOpenBtn();
            }
            
            foreach (var controller in playerDict)
            {
                controller.Value.InitializePlayerData();
            }

            foreach (var spokerplayerController in playerDict)
            {
                spokerplayerController.Value.BetImageActive(false);
                spokerplayerController.Value.ClearCurrentRoundBetHistory();
            }

            foreach (var spokerPlayerController in playerDict)
            {
                Transform[] children = new Transform[spokerPlayerController.Value.view.myCardPos.Length];

                for (int i = 0; i < spokerPlayerController.Value.view.myCardPos.Length; i++)
                {
                    children[i] = spokerPlayerController.Value.view.myCardPos[i];
                }

                for (int i = 0; i < spokerPlayerController.Value.view.myCardPos.Length; i++)
                {
                    int index = i;
                    Transform child = children[index];
                    child.SetSiblingIndex(index);
                    child.GetComponent<RectTransform>().anchoredPosition = spokerPlayerController.Value.view.cardPositions[index];
                }
            }


            view.leaveBtn.enabled = true;
            view.moveRoomBtn.enabled = true;
        }

        #endregion


        void ChangeGamestate(SPokerState state)
        {
            spokerState = state;
            CPPlayer.SPoker.currentSPokerState = state;
            view.test_GameState.text = spokerState.ToString();
        }

        async UniTask AFKUserDetector()
        {
            while (true)
            {
                if (spokerState != SPokerState.End && spokerState != SPokerState.Idle && spokerState != SPokerState.None)
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

        private void KickedForSomeReason(KickVoteNoti kickVoteNoti, int revisionId)
        {
            if (myChairId == kickVoteNoti.TargetChairId)
            {
                if (kickVoteNoti.VoteCount >= 3)
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.KickVoteWarning].StringToLocal, kickVoteNoti.VoteCount), false));
                }
                else
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.KickVoteReceived].StringToLocal, kickVoteNoti.VoteCount), false));
                }
            }

            if (TryGetPlayer(kickVoteNoti.TargetChairId, out var kickTargetPlayer))
                kickTargetPlayer.KickVoteRecieveEvent(kickVoteNoti.VoteCount);
        }

        async UniTask EmoticonExpressReq(EmotionInfo emotionInfo)
        {
            if (CPPlayer.InGame.currentGameType != GameType.SEVEN_POKER)
                return;
            string emoteStr = $"{emotionInfo.emoticonKind}_{emotionInfo.emoticonExpress}";
            var res = await Services.SevenPoker.EmoteReqAsync(CPPlayer.SPoker.currentTableId, myChairId, emoteStr);
            if (res.IsSuccess)
            {
                if (TryGetPlayer(myChairId, out var selfPlayer))
                    selfPlayer.EmoticonExpress(emotionInfo);
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

        private void BossMoveNoti(BossMovedNoti bossMovedNoti, int revisionId)
        {
            foreach (var player in playerDict)
            {
                if (bossMovedNoti.BossId == player.Value.chairId)
                {
                    player.Value.view.dealerBtnObj.SetActive(true);
                }
                else
                {
                    player.Value.view.dealerBtnObj.SetActive(false);
                }
            }
        }

        private void CardOpenNoti(CardOpenNoti cardopenNoti, int revisionId)
        {
            Extension.eLog($"{cardopenNoti.ChairId} card open");

            if (!TryGetPlayer(cardopenNoti.ChairId, out var cardOpenPlayer)) return;
            cardOpenPlayer.FoldUserCardSet(cardopenNoti.HandCards.ToList());
            CardOpenPresentAsync(cardopenNoti, revisionId).Forget();
        }

        async UniTask CardOpenPresentAsync(CardOpenNoti cardopenNoti, int revisionId)
        {
            await UniTask.NextFrame();

            //CardOpenSnapShot(cardopenNoti, revisionId);

            // if (playerDict[cardopenNoti.ChairId].isForfeitWin == false)
            // {
            //
            // }
            if (!TryGetPlayer(cardopenNoti.ChairId, out var cardOpenAsyncPlayer)) return;
            await cardOpenAsyncPlayer.OpenFoldUserCards();
            snapShot.SetUiAfterCardRecieved(cardOpenAsyncPlayer, true);
        }

        private void LeaveReservedNoti(LeaveReservedNoti leaveReserved, int revisionId)
        {
            if (TryGetPlayer(leaveReserved.ChairId, out var leaveReservedPlayer))
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
            Extension.eLog($"LeaveReservedNoti to {leaveReserved.ChairId}and my chairID:{myChairId}",Color.magenta);
        }

        private void MeUserBackFromInactive(bool active)
        {
            if (CPPlayer.Server.currentConnectedGameType != GameType.SEVEN_POKER)
                return;
            if (active)
                return;
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.ReserveOut(false);
            view.leaveReservedObj.gameObject.SetActive(false);
            view.leaveBtn.gameObject.SetActive(true);
        }
    }
}