using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Common;
using TMPro;

namespace CAPYBARA
{
    /// <summary>
    /// 턴 노티 처리, 액션 토글 UI, 베팅 금액 계산, 액션 서버 전송
    /// </summary>
    public partial class HoldemController
    {
        private void ToggleViewInitialSetting()
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

        private void ToggleViewAndBetAmountSetting(holdem.TurnNoti turnNoti, bool isMyTurn, bool alreadyActed = false)
        {
            if (myPlayer.IsObserving)
                return;
            if (!TryGetPlayer(myChairId, out var selfPlayer))
                return;
            if (selfPlayer.isFolded)
                return;
            if (selfPlayer.isAllin)
                return;

            if (isMyTurn == false && CPPlayer.Cloud.optionValue.reserveBet == false)
            {
                AllActionTogglesDeactivate();
                return;
            }

            var possibleChipforMax = Constraints.MaxBetChip - selfPlayer.GetTotalBet;
            var possibleChipforMyChip = CPPlayer.UserInfo.userDatabase.User.Gold;
            var possibleBetChip = System.Math.Min(possibleChipforMax, possibleChipforMyChip);

            long bestTotalbet = turnNoti.TotalBet + turnNoti.CallChip;
            long currentCallChip = bestTotalbet - selfPlayer.GetTotalBet;

            for (int i = 0; i < view.actionToggles.Count; i++)
            {
                view.actionToggles[i].TextColorToDefault();
            }

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
                        betAmountDict[bettingActionType] = CPPlayer.Holdem.initialBuyIn;
                        break;
                    case Partial.BetSizeType.BsCall:
                        betAmountDict[bettingActionType] = currentCallChip;
                        break;
                    case Partial.BetSizeType.BsDdadang:
                        betAmountDict[bettingActionType] = currentCallChip * 2;
                        break;
                    case Partial.BetSizeType.BsHalf:
                        betAmountDict[bettingActionType] = (currentCallChip + (currentCallChip + turnNoti.PotAmount) / 2);
                        break;
                    case Partial.BetSizeType.BsQuater:
                        betAmountDict[bettingActionType] = (currentCallChip + (currentCallChip + turnNoti.PotAmount) / 4);
                        break;
                    case Partial.BetSizeType.BsAllin:
                        betAmountDict[bettingActionType] = possibleBetChip;
                        break;
                    case Partial.BetSizeType.BsMax:
                        betAmountDict[bettingActionType] = turnNoti.MaxBet;
                        break;
                    default:
                        break;
                }
            }

            view.actionToggleObject.transform.SetParent(view.bettingActiveParent, false);

            if (possibleBetChip <= 0)
            {
                view.actionToggles[(int)Partial.BetSizeType.BsAllin].ToggleActivate(0, true);
                view.actionToggles[(int)Partial.BetSizeType.BsMax].ObjectActivate(false);
                return;
            }
            else
            {
                view.actionToggles[(int)Partial.BetSizeType.BsFold].ToggleActivate(0, true);

                if (currentCallChip <= 0)
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], false);
                    view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ObjectActivate(false);
                    if (possibleBetChip >= CPPlayer.Holdem.initialBuyIn
                        && selfPlayer.GetTotalBet + CPPlayer.Holdem.initialBuyIn < Constraints.MaxBetChip)
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], true);
                        view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], true);
                        view.actionToggles[(int)Partial.BetSizeType.BsCall].ObjectActivate(false);
                    }
                    else
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsCheck].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCheck], true);
                        view.actionToggles[(int)Partial.BetSizeType.BsBbing].ToggleActivate(betAmountDict[Partial.BetSizeType.BsBbing], false);
                        view.actionToggles[(int)Partial.BetSizeType.BsCall].ObjectActivate(false);
                    }
                }
                else
                {
                    view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], false);
                    view.actionToggles[(int)Partial.BetSizeType.BsBbing].ObjectActivate(false);
                    if (possibleBetChip > currentCallChip)
                    {
                        view.actionToggles[(int)Partial.BetSizeType.BsCall].ToggleActivate(betAmountDict[Partial.BetSizeType.BsCall], true);
                        view.actionToggles[(int)Partial.BetSizeType.BsCheck].ObjectActivate(false);

                        bool otherCanRaise = turnNoti.MaxChip > 0;

                        if (turnNoti.IsLast && otherCanRaise == false)
                        {
                            return;
                        }

                        if (possibleBetChip > currentCallChip * 2 && selfPlayer.GetTotalBet + currentCallChip * 2 < Constraints.MaxBetChip && thisBetRoundAlreadyActed == false)
                        {
                            view.actionToggles[(int)Partial.BetSizeType.BsDdadang].ToggleActivate(betAmountDict[Partial.BetSizeType.BsDdadang], true);
                        }
                    }
                }

                long quateramount = (currentCallChip + (currentCallChip + turnNoti.PotAmount) / 4);
                bool QuaterActive = (possibleBetChip > quateramount && quateramount < turnNoti.MaxBet);
                view.actionToggles[(int)Partial.BetSizeType.BsQuater].ToggleActivate(betAmountDict[Partial.BetSizeType.BsQuater], QuaterActive);

                long halfamount = (currentCallChip + (currentCallChip + turnNoti.PotAmount) / 2);
                bool halfActive = (possibleBetChip > halfamount && halfamount <= turnNoti.MaxBet);
                view.actionToggles[(int)Partial.BetSizeType.BsHalf].ToggleActivate(betAmountDict[Partial.BetSizeType.BsHalf], halfActive);

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

            Extension.eLog("액션 뷰 세팅", Color.green);
        }

        private void ToggleViewSetForReserveBetOption(bool isReservePossible)
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
                if (!isMyTurn)
                {
                    AllActionTogglesDeactivate();
                }
            }
        }

        private void AllActionTogglesDeactivate()
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

        private void ActionTogglePressed(Partial.ActionType actionType, Partial.BetSizeType betSizeType, Toggle changedToggle, bool ison)
        {
            ActionProcessToggle(actionType, betSizeType, changedToggle, ison).Forget();
        }

        private async UniTask ActionProcessToggle(Partial.ActionType actionType, Partial.BetSizeType betSizeType, Toggle changedToggle, bool ison)
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
            {
                return;
            }
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
            isActionProcessing = true;
            long amount = betAmountDict[tempBetActiontoggleType];
            Debug.Log($"베팅하는 금액{amount}");
            var tempbetSizeType = ProtoMapper.HoldemBettingActionType(tempBetActiontoggleType);
            var tempactionType = ProtoMapper.HoldemActionType(actionStateForServer);
            var actionResPacket = await Services.Holdem.ActionAsync(snapShot.RoomImfo.TableId, tempactionType, amount, tempbetSizeType);
            if (actionResPacket.IsSuccess)
            {
                if (TryGetPlayer(myChairId, out var selfPlayer))
                {
                    selfPlayer.SetTotalBet(actionResPacket.Data.TotalBet);
                    selfPlayer.SetActionData(actionType, tempBetActiontoggleType, amount, actionResPacket.Data.Chip);
                }

                view.currentPotAmount.text = Extension.ToKoreanFormat(actionResPacket.Data.PotAmount, Extension.KoreanFormatMode.Planning);

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

                Extension.eLog($"Server error Occured.\nMessage:{actionResPacket.Error}");
                return;
            }

            if (betSizeType == Partial.BetSizeType.BsDdadang)
            {
                isDdadangActedThisRound = true;
            }

            switch (betSizeType)
            {
                case Partial.BetSizeType.BsFold:   AudioManager.Instance.Play(AudioSourceKey.Die);     break;
                case Partial.BetSizeType.BsCheck:  AudioManager.Instance.Play(AudioSourceKey.Check);   break;
                case Partial.BetSizeType.BsBbing:  AudioManager.Instance.Play(AudioSourceKey.Bing);    break;
                case Partial.BetSizeType.BsCall:   AudioManager.Instance.Play(AudioSourceKey.Call);    break;
                case Partial.BetSizeType.BsDdadang:AudioManager.Instance.Play(AudioSourceKey.Dadang);  break;
                case Partial.BetSizeType.BsQuater: AudioManager.Instance.Play(AudioSourceKey.Quarter); break;
                case Partial.BetSizeType.BsHalf:   AudioManager.Instance.Play(AudioSourceKey.Half);    break;
                case Partial.BetSizeType.BsAllin:  AudioManager.Instance.Play(AudioSourceKey.Allin);   break;
                case Partial.BetSizeType.BsMax:    AudioManager.Instance.Play(AudioSourceKey.Max);     break;
                default: break;
            }

            thisBetRoundAlreadyActed = true;
     

            _bettingActionToggleState = Partial.BetSizeType.BsEnd;
            if (thisBetRoundAlreadyActed)
            {
                AllActionTogglesDeactivate();
            }
            isActionProcessing = false;
            
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

        public void TurnChangedNoti(holdem.TurnNoti turnNoti, int revisionId)
        {
            if (myPlayer.IsObserving)
                return;
            currentTurnNotiInfo = turnNoti;
            TryGetPlayer(myChairId, out var selfPlayer);
            switch (currentTurnNotiInfo.Phase)
            {
                case Phase.PhPreFlop:
                    ChangeGamestate(HoldemState.PH_PRE_FLOP);
                    selfPlayer?.SetCurrentPhase(HoldemState.PH_PRE_FLOP);
                    break;
                case Phase.PhFlop:
                    ChangeGamestate(HoldemState.PH_FLOP);
                    selfPlayer?.SetCurrentPhase(HoldemState.PH_FLOP);
                    break;
                case Phase.PhTurn:
                    ChangeGamestate(HoldemState.PH_TURN);
                    selfPlayer?.SetCurrentPhase(HoldemState.PH_TURN);
                    break;
                case Phase.PhRiver:
                    ChangeGamestate(HoldemState.PH_RIVER);
                    selfPlayer?.SetCurrentPhase(HoldemState.PH_RIVER);
                    break;
            }

            TurnNotiPresent(turnNoti, revisionId).Forget();
        }

        private async UniTask TurnNotiPresent(TurnNoti turnNoti, int revisionId)
        {
            await UniTask.NextFrame();
            TurnNotiPresentAfterFrame(turnNoti, revisionId);
        }

        private void TurnNotiPresentAfterFrame(TurnNoti turnNoti, int revisionId)
        {
            isMyTurn = currentTurnNotiInfo.ChairId == myChairId;

            if (TryGetPlayer(currentTurnNotiInfo.ChairId, out var currentTurnPlayer) && currentTurnPlayer.isAllin)
                return;
            Extension.eLog($"{currentTurnNotiInfo.ChairId} turn changed. {currentTurnNotiInfo.Phase}");

           

            if (TryGetPlayer(myChairId, out var selfPlayer))
            {
                selfPlayer.SetCardOpenBtn();
                if (isMyTurn)
                {
                    selfPlayer.SetTotalBet(currentTurnNotiInfo.TotalBet);
                    myTurnNotiInfo = currentTurnNotiInfo;
                }
            }
            else if (isMyTurn)
            {
                myTurnNotiInfo = currentTurnNotiInfo;
            }

            if (TryGetPlayer(turnNoti.ChairId, out var turnPlayer))
                turnPlayer.BetImageActive(false);

            bool isPhaseChanged = false;
            if (phaseState != currentTurnNotiInfo.Phase)
            {
                phaseState = currentTurnNotiInfo.Phase;
                isPhaseChanged = true;
                isDdadangActedThisRound = false;
                thisBetRoundAlreadyActed = false;

                foreach (var holdemPlayerController in playerDict)
                {
                    holdemPlayerController.Value.BetImageActive(false);
                    holdemPlayerController.Value.ClearCurrentRoundBetHistory();
                }
            }

            if (revisionId != HoldemDispatchPushHub.revisionId)
            {
                if (isMyTurn && thisBetRoundAlreadyActed)
                {
                    thisBetRoundAlreadyActed = false;
                }

                if (thisBetRoundAlreadyActed == false)
                {
                    Extension.eLog($"TotalBet:{currentTurnNotiInfo.TotalBet}/CallChip:{currentTurnNotiInfo.CallChip}/maxBet:{Constraints.MaxBetChip}/내턴?{isMyTurn}", Color.green);
                    ToggleViewAndBetAmountSetting(currentTurnNotiInfo, currentTurnNotiInfo.ChairId == myChairId, thisBetRoundAlreadyActed);
                }

                isActionProcessing = false;

                if (isMyTurn)
                {
                    if (_bettingActionToggleState != Partial.BetSizeType.BsEnd)
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

                if (thisBetRoundAlreadyActed == false)
                {
                    Extension.eLog($"TotalBet:{currentTurnNotiInfo.TotalBet}/CallChip:{currentTurnNotiInfo.CallChip}/maxBet:{Constraints.MaxBetChip}/내턴?{isMyTurn}", Color.green);
                    ToggleViewAndBetAmountSetting(currentTurnNotiInfo, currentTurnNotiInfo.ChairId == myChairId, thisBetRoundAlreadyActed);
                }

                DateTime starttime = currentTurnNotiInfo.Ts.ToDateTime();
                if (TryGetPlayer(currentTurnNotiInfo.ChairId, out var activatePlayer))
                    activatePlayer.ActivateTurn(starttime, isMyTurn);
                isActionProcessing = false;

                if (isMyTurn)
                {
                    if (_bettingActionToggleState != Partial.BetSizeType.BsEnd)
                    {
                        if (view.actionToggles[(int)_bettingActionToggleState].toggle.interactable)
                        {
                            ActionProcessToggle(actionStateForServer, _bettingActionToggleState, view.actionToggles[(int)_bettingActionToggleState].toggle, true).Forget();
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

        public void ActionNoti(holdem.ActionNoti actionNoti, int revisionId)
        {
            if (myPlayer.IsObserving)
                return;

            view.currentPotAmount.text = Extension.ToKoreanFormat(actionNoti.PotAmount, Extension.KoreanFormatMode.Planning);
            Partial.BetSizeType tempType = Partial.BetSizeType.BsFold;
            if (actionNoti.Action == ActionType.AtFold)  tempType = Partial.BetSizeType.BsFold;
            if (actionNoti.Action == ActionType.AtCheck) tempType = Partial.BetSizeType.BsCheck;
            if (actionNoti.Action == ActionType.AtAllin) tempType = Partial.BetSizeType.BsAllin;

            isMyTurn = false;
            if (TryGetPlayer(actionNoti.ChairId, out var endTurnPlayer))
                endTurnPlayer.SetEndTurn(actionNoti.ChairId == myChairId);

            ActionNotiPresent(actionNoti, tempType, revisionId).Forget();
        }

        private async UniTask ActionNotiPresent(ActionNoti actionNoti, Partial.BetSizeType tempType, int revisionId)
        {
            await UniTask.NextFrame();
            ActionNotiPresentAfterFrame(actionNoti, tempType, revisionId);
        }

        private void ActionNotiPresentAfterFrame(ActionNoti actionNoti, Partial.BetSizeType tempType, int revisionId)
        {
            if (!TryGetPlayer(actionNoti.ChairId, out var player))
                return;

            if (revisionId != HoldemDispatchPushHub.revisionId)
            {
                player.SetActionData(actionNoti.actionType, tempType, actionNoti.Amount, actionNoti.Chip);
                player.ActionToDisplay(actionNoti.betSizeType);
                if (actionNoti.Action == ActionType.AtAllin)
                    player.SetAllin(true);
                player.SetFold(actionNoti.Action == ActionType.AtFold);
                if (actionNoti.ChairId == myChairId)
                {
                    _bettingActionToggleState = Partial.BetSizeType.BsEnd;
                    AllActionTogglesDeactivate();
                    CPPlayer.InGame.AFKPopupActive?.Invoke(true);
                    CPPlayer.InGame.isUserAFK = true;
                }
                else
                {
                    if (actionNoti.Action == ActionType.AtRaise)
                        thisBetRoundAlreadyActed = false;
                }
            }
            else
            {
                player.SetActionData(actionNoti.actionType, tempType, actionNoti.Amount, actionNoti.Chip);
                snapShot.ThrowChip(actionNoti.ChairId, actionNoti.Chip, player.view.throwChipStartPos);
                player.ActionToDisplay(actionNoti.betSizeType);
                if (actionNoti.Action == ActionType.AtAllin)
                    player.SetAllin(true);
                player.SetFold(actionNoti.Action == ActionType.AtFold);
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
                    switch (actionNoti.betSizeType)
                    {
                        case Partial.BetSizeType.BsFold:   AudioManager.Instance.Play(AudioSourceKey.Die);     break;
                        case Partial.BetSizeType.BsCheck:  AudioManager.Instance.Play(AudioSourceKey.Check);   break;
                        case Partial.BetSizeType.BsBbing:  AudioManager.Instance.Play(AudioSourceKey.Bing);    break;
                        case Partial.BetSizeType.BsCall:   AudioManager.Instance.Play(AudioSourceKey.Call);    break;
                        case Partial.BetSizeType.BsDdadang:AudioManager.Instance.Play(AudioSourceKey.Dadang);  break;
                        case Partial.BetSizeType.BsQuater: AudioManager.Instance.Play(AudioSourceKey.Quarter); break;
                        case Partial.BetSizeType.BsHalf:   AudioManager.Instance.Play(AudioSourceKey.Half);    break;
                        case Partial.BetSizeType.BsAllin:  AudioManager.Instance.Play(AudioSourceKey.Allin);   break;
                        case Partial.BetSizeType.BsMax:    AudioManager.Instance.Play(AudioSourceKey.Max);     break;
                        default: break;
                    }

                    if (actionNoti.Action == ActionType.AtRaise)
                        thisBetRoundAlreadyActed = false;
                }
            }
        }
    }
}