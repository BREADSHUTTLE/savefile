using CAPYBARA.Core;
using CAPYBARA.holdem;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 게임 상태 초기화 / 리셋 (라운드 시작·종료·입장 시점)
    /// </summary>
    public partial class HoldemController
    {
        public void InitializeOnEndGame()
        {
            view.InitializeViewData();
            snapShot.ClearDataInRoundGame();
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();
            LeaveRoomBtnInit();
            MoveRoomBtnInit();
            ToggleViewInitialSetting();
            foreach (var player in playerDict)
            {
                if (startNotiInfo != null)
                {
                    if (startNotiInfo.DealerId == player.Value.chairId)
                    {
                        player.Value.view.dealerBtnObj.SetActive(true);
                    }
                    else
                    {
                        player.Value.view.dealerBtnObj.SetActive(false);
                    }
                    player.Value.view.inActiveMask.SetActive(false);
                }
            }
            isDdadangActedThisRound = false;
        }

        public void InitializeOnGameStart()
        {
            ActionToggleInActivate();
            LeaveRoomBtnInit();
            MoveRoomBtnInit();
            ToggleViewInitialSetting();

            if (myPlayer.IsObserving == false)
            {
                foreach (var player in playerDict)
                {
                    if (startNotiInfo != null)
                    {
                        if (startNotiInfo.DealerId == player.Value.chairId)
                        {
                            player.Value.view.dealerBtnObj.SetActive(true);
                        }
                        else
                        {
                            player.Value.view.dealerBtnObj.SetActive(false);
                        }
                        player.Value.view.inActiveMask.SetActive(false);
                    }
                }
            }

            isDdadangActedThisRound = false;
        }

        public void InitializeOnEnter()
        {
            view.showEmoticonViewBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.useEmoji);
            view.openJokboWindowBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.jokboInform);
            view.potAmountObject.SetActive(true);

            view.InitializeViewData();
            snapShot.ClearDataInRoundGame();
            snapShot.SetGameInfo(enterresInfo);
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();
            ToggleViewInitialSetting();

            foreach (var player in playerDict)
            {
                player.Value.EnterSet();
                if (enterresInfo.DealerId == player.Value.chairId)
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
            myPlayer.Chip = CPPlayer.UserInfo.userDatabase.User.Gold;
            CPPlayer.Holdem.currentTableId = enterresInfo.TableId;
            CPPlayer.Holdem.gapBetweenChairIdAndIndex = enterresInfo.ChairId;
            CPPlayer.InGame.haveKickVote = false;
            view.emotionView.ActiveWindow(false);

            reserveLeaveRequest = false;
            reserveMoveRoomRequest = false;
            isExiled = false;
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
            CPPlayer.InGame.isUserAFK = false;
            CPPlayer.InGame.AFKPopupActiveFlag = false;
            thisBetRoundAlreadyActed = false;
            view.jokboWindow.SetActive(false);

            view.tableAnte.text = Extension.ToKoreanFormat(CPPlayer.InGame.currentRoomInfo.Ante);
            view.SB_BBAmount.text = Extension.ToKoreanFormat(CPPlayer.InGame.currentRoomInfo.SmallBlind);
            view.currentPotAmount.text = "0";
            isDdadangActedThisRound = false;
            isShowdownPlayed = false;
        }

        private void InitializePlayersDisplay()
        {
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.SetCurrentPhase(HoldemState.End);
            foreach (var controller in playerDict)
            {
                controller.Value.InitializePlayerData();
            }
            foreach (var holdemPlayerController in playerDict)
            {
                holdemPlayerController.Value.BetImageActive(false);
                holdemPlayerController.Value.ClearCurrentRoundBetHistory();
            }
            view.leaveBtn.enabled = true;
            view.moveRoomBtn.enabled = true;
        }
    }
}