using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public enum HoldemState
    {
        None = -5,
        Idle = 0,
        Start,
        PH_PRE_FLOP,
        PH_FLOP,
        PH_TURN,
        PH_RIVER,
        Showdown,
        Result,
        End,
    }

    public partial class HoldemController:IInGameController
    {
        // ── View / Model ───────────────────────────────────────────────
        public HoldemGameModel _model;
        public HoldemViewer view;

        // ── 게임 상태 ──────────────────────────────────────────────────
        public HoldemState holdemState;
        private holdem.Phase phaseState = Phase.PhNone;
        private bool isShowdownPlayed = false;

        // ── 플레이어 ────────────────────────────────────────────────────
        private Dictionary<int, HoldemPlayerController> playerDict = new Dictionary<int, HoldemPlayerController>();

        bool TryGetPlayer(int chairId, out HoldemPlayerController player)
            => playerDict.TryGetValue(chairId, out player);
        private int myChairId;
        private holdem.Player myPlayer;
        private HoldemPlayerController mePlayerController;

        // ── 베팅 / 액션 ────────────────────────────────────────────────
        private Partial.BetSizeType _bettingActionToggleState;
        private Partial.ActionType actionStateForServer;
        private bool isActionProcessing = false;
        private bool isActedInMyturn = false;
        private bool isDdadangActedThisRound = false;
        private bool thisBetRoundAlreadyActed = false;
        private Dictionary<Partial.BetSizeType, long> betAmountDict = new Dictionary<Partial.BetSizeType, long>();
        private bool isMyTurn = false;
        private holdem.TurnNoti myTurnNotiInfo;
        private holdem.TurnNoti currentTurnNotiInfo;

        // ── 방 입장/퇴장 ───────────────────────────────────────────────
        public EnterRes enterresInfo;
        private StartNoti startNotiInfo;
        private bool reserveLeaveRequest = false;
        private bool reserveMoveRoomRequest = false;
        private bool isExiled = false;

        // ── 스냅샷 / 씬 ────────────────────────────────────────────────
        private HoldemTableSnapShot snapShot;
        private GameObject mainObject;

        // ── 비동기 취소 ────────────────────────────────────────────────
        private CancellationTokenSource _cts;
        private CancellationTokenSource _resultPresentationCts;

        // ── 이벤트 핸들러 캐시 ─────────────────────────────────────────
        private Action<GameType, EmotionInfo> _onEmotionExpress;
        private Action<bool> _onEmojiUseChange;
        private Action<bool> _onJokboUseChange;
        
        public Action<bool> onWaitGamePopup;   

        private float elapsedTime = 0;

        // ══════════════════════════════════════════════════════════════
        //  생명주기
        // ══════════════════════════════════════════════════════════════

        public HoldemController(GameObject UpdateObject, HoldemViewer _view, CancellationTokenSource cts)
        {
            mainObject = UpdateObject;
            view = _view;
            _cts = cts;

            Init();
            elapsedTime = 0;
            AFKUserDetector().Forget();
        }

        public void StartSet()
        {
            snapShot.Init(view);
            CPPlayer.Holdem.EnterRoom += EnterGameTable_Reponse;

            view.winnerDetailPanel.SetActive(false);
            view.jackpotDetailPanel.SetActive(false);
            view.jackpotDetailBackEffect.SetActive(false);
            view.showdownPanel.SetActive(false);
            view.jokboWindow.SetActive(false);

            CPPlayer.Option.ReserveBetChange += ToggleViewSetForReserveBetOption;
            _onEmotionExpress = (o, t) => { EmoticonExpressReq(t).Forget(); };
            CPPlayer.InGame.emotionExpressEvent += _onEmotionExpress;

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

            RegisterCallbackNoti();
            CPPlayer.Server.CallbackAfterHoldemConnect += RegisterCallbackNoti;
            CPPlayer.InGame.AFKPopupActive += MeUserBackFromInactive;
        }

        public void Dispose()
        {
            HoldemDispatchPushHub.OnEnterNoti -= EnterGameOtherPlayer_Noti;
            HoldemDispatchPushHub.OnLeaveNoti -= LeaveGameOtherPlayer_Noti;
            HoldemDispatchPushHub.OnStartNoti -= StartGame;
            HoldemDispatchPushHub.OnKickedNoti -= KickedForSomeReason;
            HoldemDispatchPushHub.OnEmoteNoti -= EmoticonExpressNoti;
            HoldemDispatchPushHub.OnCardOpenNoti -= CardOpenNoti;
            HoldemDispatchPushHub.OnLeaveReserveNoti -= LeaveReservedNoti;
            HoldemDispatchPushHub.OnHoleCardNoti -= HoleCardNoti;
            HoldemDispatchPushHub.OnHoleCardNotiOther -= HoleCardNotiOther;
            HoldemDispatchPushHub.OnTurnNoti -= TurnChangedNoti;
            HoldemDispatchPushHub.OnActionNoti -= ActionNoti;
            HoldemDispatchPushHub.OnCommunityCardsNoti -= CommunityCardsNoti;
            HoldemDispatchPushHub.OnShowdownNoti -= ShowDownNoti;
            HoldemDispatchPushHub.OnCardNoti -= CardInfoNoti;
            HoldemDispatchPushHub.OnResultNoti -= ResultNoti;

            CPPlayer.Server.CallbackAfterHoldemConnect -= RegisterCallbackNoti;
            CPPlayer.InGame.AFKPopupActive -= MeUserBackFromInactive;
            CPPlayer.Holdem.EnterRoom -= EnterGameTable_Reponse;
            CPPlayer.Option.ReserveBetChange -= ToggleViewSetForReserveBetOption;
            CPPlayer.InGame.emotionExpressEvent -= _onEmotionExpress;
            CPPlayer.Option.EmojiUseChange -= _onEmojiUseChange;
            CPPlayer.Option.JokboUseChange -= _onJokboUseChange;
        }

        private void RegisterCallbackNoti()
        {
            HoldemDispatchPushHub.OnEnterNoti -= EnterGameOtherPlayer_Noti;
            HoldemDispatchPushHub.OnLeaveNoti -= LeaveGameOtherPlayer_Noti;
            HoldemDispatchPushHub.OnStartNoti -= StartGame;
            HoldemDispatchPushHub.OnKickedNoti -= KickedForSomeReason;
            HoldemDispatchPushHub.OnEmoteNoti -= EmoticonExpressNoti;
            HoldemDispatchPushHub.OnCardOpenNoti -= CardOpenNoti;
            HoldemDispatchPushHub.OnLeaveReserveNoti -= LeaveReservedNoti;

            HoldemDispatchPushHub.OnEnterNoti += EnterGameOtherPlayer_Noti;
            HoldemDispatchPushHub.OnLeaveNoti += LeaveGameOtherPlayer_Noti;
            HoldemDispatchPushHub.OnStartNoti += StartGame;
            HoldemDispatchPushHub.OnKickedNoti += KickedForSomeReason;
            HoldemDispatchPushHub.OnEmoteNoti += EmoticonExpressNoti;
            HoldemDispatchPushHub.OnCardOpenNoti += CardOpenNoti;
            HoldemDispatchPushHub.OnLeaveReserveNoti += LeaveReservedNoti;

            HoldemDispatchPushHub.OnHoleCardNoti -= HoleCardNoti;
            HoldemDispatchPushHub.OnHoleCardNotiOther -= HoleCardNotiOther;
            HoldemDispatchPushHub.OnTurnNoti -= TurnChangedNoti;
            HoldemDispatchPushHub.OnActionNoti -= ActionNoti;
            HoldemDispatchPushHub.OnCommunityCardsNoti -= CommunityCardsNoti;
            HoldemDispatchPushHub.OnShowdownNoti -= ShowDownNoti;
            HoldemDispatchPushHub.OnCardNoti -= CardInfoNoti;
            HoldemDispatchPushHub.OnResultNoti -= ResultNoti;

            HoldemDispatchPushHub.OnHoleCardNoti += HoleCardNoti;
            HoldemDispatchPushHub.OnHoleCardNotiOther += HoleCardNotiOther;
            HoldemDispatchPushHub.OnTurnNoti += TurnChangedNoti;
            HoldemDispatchPushHub.OnActionNoti += ActionNoti;
            HoldemDispatchPushHub.OnCommunityCardsNoti += CommunityCardsNoti;
            HoldemDispatchPushHub.OnShowdownNoti += ShowDownNoti;
            HoldemDispatchPushHub.OnCardNoti += CardInfoNoti;
            HoldemDispatchPushHub.OnResultNoti += ResultNoti;
        }

        private async UniTask AFKUserDetector()
        {
            while (true)
            {
                if (holdemState != HoldemState.End && holdemState != HoldemState.Idle && holdemState != HoldemState.None)
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

        public void Init()
        {
            ChangeGamestate(HoldemState.None);
            snapShot = new HoldemTableSnapShot();
            phaseState = Phase.PhNone;

            betAmountDict.Clear();

            for (int i = 0; i < view.actionToggles.Count; i++)
            {
                var toggleItem = view.actionToggles[i];
                var hat = toggleItem.ingameActionType;
                var bet = toggleItem.ingameBettingActionType;
                var toggle = toggleItem.toggle;
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(ison =>
                {
                    ActionTogglePressed(hat, bet, toggle, ison);
                });
                betAmountDict[bet] = 0;
            }

            myPlayer = new Player()
            {
                Uid = CPPlayer.Holdem.ingameUid,
                Nick = CPPlayer.UserInfo.userDatabase.User.Nick,
            };
            _bettingActionToggleState = Partial.BetSizeType.BsEnd;

            view.leaveBtn.onClick.AddListener(() => LeaveThisRoomOrReserve(isExiled).Forget());
            view.leaveReservedObj.onClick.AddListener(() => CancelReserveLeave().Forget());

            view.moveRoomBtn.onClick.AddListener(() => LeaveThisRoomAndMoveOtherRoomOrReserve().Forget());
            view.moveReservedObj.onClick.AddListener(() => CancelReserveMove().Forget());

            view.optionBtn.onClick.AddListener(() =>
            {
                var optionWindow = ViewCanvas.Get<ViewCanvasInGame>().ingameOptionWindow;
                if (ViewCanvas.Get<ViewCanvasInGame>().ingameOptionWindow.gameObject.activeInHierarchy == false)
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
        }

        private void ChangeGamestate(HoldemState state)
        {
            holdemState = state;
            Extension.eLog($"{state.ToString()},상태", Color.cyan);
            view.test_GameState.text = holdemState.ToString();
        }
        
        public void OnOtherPlayerModalInactive(int chairId)                              
        {                                                                                
            foreach (var player in playerDict)                                           
            {                                                                            
                if (player.Value.chairId == chairId) continue;                           
                player.Value.InfoModalInactive();                                              }                                                                            
        }                                                                                

    }
}
