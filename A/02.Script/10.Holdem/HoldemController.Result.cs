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

namespace CAPYBARA
{
    /// <summary>
    /// 게임 결과 노티 처리 및 결과 연출
    /// </summary>
    public partial class HoldemController
    {
        public void ResultNoti(holdem.ResultNoti resultNoti, int revisionId)
        {
            
            if (myPlayer.IsObserving)
            {
                ChangeGamestate(HoldemState.Idle);
                onWaitGamePopup?.Invoke(false);
                return;
            }
            
            ResultPresentEvent(resultNoti, revisionId).Forget();
        }

        async UniTask ResultPresentEvent(holdem.ResultNoti resultNoti, int revisionId)
        {
            await UniTask.NextFrame();
            ResultPresentEventAfterFrame(resultNoti, revisionId);
        }

        void ResultPresentEventAfterFrame(holdem.ResultNoti resultNoti, int revisionId)
        {
            ChangeGamestate(HoldemState.Result);
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.SetCurrentPhase(HoldemState.Result);
            snapShot.SetResultPotInfo(resultNoti.Pots);

            bool isShowdownNeed = true;
            int resultPlayerCount = 0;
            
         

            foreach (var holdemPlayerController in playerDict)
            {
                holdemPlayerController.Value.BetImageActive(false);
            }

            foreach (var player in resultNoti.Players)
            {
                if (player.HoleCards.Count > 0)
                {
                    if (!TryGetPlayer(player.ChairId, out var pc) || pc.isFolded)
                        continue;
                    resultPlayerCount++;
                }
            }

            if (revisionId != HoldemDispatchPushHub.revisionId)
            {
                //Debug.LogError($"결과 화면 스냅샷! 연출! revisionId:{revisionId}//HoldemDispatchPushHub.revisionId:{HoldemDispatchPushHub.revisionId}");
                PresentationResultEventSnapshot(resultNoti, resultPlayerCount);
            }
            else
            {
                // Debug.LogError($"결과화면  애님! 연출! revisionId:{revisionId}//HoldemDispatchPushHub.revisionId:{HoldemDispatchPushHub.revisionId}");
                PresentationResultEvent(resultNoti, resultPlayerCount).Forget();
            }
        }

        private async UniTask PresentationResultEvent(holdem.ResultNoti resultNoti, int resultPlayerCount)
        {
            // 이전 취소 토큰이 있으면 취소하고 새로 생성
            _resultPresentationCts?.Cancel();
            _resultPresentationCts = new CancellationTokenSource();
            var token = _resultPresentationCts.Token;

            try
            {
                int delayforResultScreen = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_SHOW_WAIT_MS"];
                await UniTask.Delay(delayforResultScreen, cancellationToken: token);
                
                //기존 UI display 처리(pot 사라짐, 족보 안내버튼 사라짐 등)
                SetTableUIForResult();
                //기존 UI display 처리(pot 사라짐, 족보 안내버튼 사라짐 등)
                
                ResultDisplayEventAsync(resultNoti);

                var mainWinner = resultNoti.Pots[0].Wins;
                holdem.ResultNoti.Types.Player mainWinnerPlayer = null;

                //snapshot으로 전체 구성 데이터 전달 및 ui 구성하기
                bool isJackpotExist = false;

                string mainWinRankText = "";
                long jackpotAmount = 0;

                foreach (var gameplayer in resultNoti.Players)
                {
                    bool isWinner = mainWinner.Any(o => o.ChairId == gameplayer.ChairId);
                    if (isWinner)
                        mainWinnerPlayer = gameplayer;

                    if (!TryGetPlayer(gameplayer.ChairId, out var player))
                        continue;

                    player.SetWinnerUI(isWinner);

                    if (gameplayer.HoleCards.Count <= 0)
                        continue;
                    //각자의 UI 갱신
                    string mainwinrank = snapShot.SetJokboRankAndCardHighlight(player, CardUISetState.AfterResult, false);
                    if (isWinner)
                    {
                        mainWinRankText = mainwinrank;
                        player.view.bestCardObjInResult_Loser.gameObject.SetActive(false);
                    }
                    else
                    {
                        foreach (var cardViewer in player.cardViewerList)
                        {
                            cardViewer.SetMaskFade();
                        }
                        float dieDim = (float)CPPlayer.Server.visualEffectTimeConfig["DIE_ME_DIM_MS"]/1000f;

                        player.view.inActiveMask.SetActive(true);

                        var c = player.view.inactivemaskImage.color;
                        c.a = 0f;
                        player.view.inactivemaskImage.color = c;

                        player.view.inactivemaskImage.DOFade(0.5f, dieDim);
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
                    view.jackpotCardRank.text = "로얄 스트레이트 플러시";
                }
                else
                {
                    view.winnerDetailPanel.SetActive(true);
                    view.winnerWindowPanelAnimator.Play("WinnerWindow");
                }


                //가운데 ui 연출 구성
                if (resultPlayerCount == 0)
                {
                    view.winnerCardRank.text = "기권승";
                    //카드 펼쳐주는 연출도 같이 실행
                    if (TryGetPlayer(mainWinnerPlayer.ChairId, out var winnerPlayer))
                    {
                        List<string> cardList = winnerPlayer.holdemPlayerInfo.cardlist;
                        winnerPlayer.CardSettoResultPos(cardList);
                        if (mainWinnerPlayer.ChairId == myChairId)
                            winnerPlayer.SetCardOpenAtForfeitWin();
                    }
                    //카드 펼쳐주는 연출도 같이 실행
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
                    view.jackpotAmountChip.text = $"+{winAmount}\n잭팟:{jackpotAmountStr}";
                }

                //결과 각 플레이어들의 판돈 체크
                foreach (var showdownplayer in resultNoti.Players)
                {
                    if (TryGetPlayer(showdownplayer.ChairId, out var spPlayer))
                        spPlayer.SetCurrentOwnedChip(showdownplayer.Chip);
                }
                //Debug.Log(e.Message);
                // CPPlayer.InGame.errorToastPopup?.Invoke($"서버 에러가 발생하였습니다.");

                CheckAchievement(resultNoti);

              
                int delayforResultInit = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_SHOW_MS"];
                int cardOpenInActiveTime = (int)CPPlayer.Server.visualEffectTimeConfig["OPEN_HIDE_MS"];
                await UniTask.Delay(delayforResultInit-cardOpenInActiveTime, cancellationToken: token);
                if (TryGetPlayer(myChairId, out var selfPlayer))
                    selfPlayer.CardOpenBtnObjActive(false);
               
                await UniTask.Delay(cardOpenInActiveTime, cancellationToken: token);

                SetEndState().Forget();
                
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

            foreach (var con in playerDict)
            {
                con.Value.view.starRankObjNew.SetActive(false);
            }
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.SetCardOpenBtn();
            
            view.potAmountObject.SetActive(false);
            view.openJokboWindowBtn.gameObject.SetActive(false);

        }
        void SetTableUIForResultEnd()
        {
            view.potAmountObject.SetActive(true);
            view.openJokboWindowBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.jokboInform);
        }

        private void PresentationResultEventSnapshot(holdem.ResultNoti resultNoti, int showdownPlayerCount)
        {
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.SetCardOpenBtn();
            ResultDisplayEvent(resultNoti);

            var mainWinner = resultNoti.Pots[0].Wins;
            holdem.ResultNoti.Types.Player mainWinnerPlayer = null;

            //snapshot으로 전체 구성 데이터 전달 및 ui 구성하기
            bool isJackpotExist = false;
            

            string mainWinRankText = "";
            long jackpotAmount = 0;

            foreach (var gameplayer in resultNoti.Players)
            {
                bool isWinner = mainWinner.Any(o => o.ChairId == gameplayer.ChairId);
                if (isWinner)
                    mainWinnerPlayer = gameplayer;

                if (!TryGetPlayer(gameplayer.ChairId, out var player))
                    continue;

                player.SetWinnerUI(isWinner);

                if (gameplayer.HoleCards.Count <= 0)
                    continue;
                //각자의 UI 갱신
                string mainwinrank = snapShot.SetJokboRankAndCardHighlight(player, CardUISetState.AfterResult, false);
                if (isWinner)
                    mainWinRankText = mainwinrank;

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
                view.jackpotCardRank.text = "로얄 스트레이트 플러시";
            }
            else
            {
                view.winnerDetailPanel.SetActive(true);
            }


            //가운데 ui 연출 구성
            if (showdownPlayerCount == 0)
            {
                view.winnerCardRank.text = "기권승";
            
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
                view.jackpotAmountChip.text = $"+{winAmount}\n잭팟:{jackpotAmountStr}";
            }

            //결과 각 플레이어들의 판돈 체크
            foreach (var showdownplayer in resultNoti.Players)
            {
                if (TryGetPlayer(showdownplayer.ChairId, out var spPlayer))
                    spPlayer.SetCurrentOwnedChip(showdownplayer.Chip);
            }
            //Debug.Log(e.Message);
            // CPPlayer.InGame.errorToastPopup?.Invoke($"서버 에러가 발생하였습니다.");

            CheckAchievement(resultNoti);

          
            InitializeOnEndGame();
            ChangeGamestate(HoldemState.Idle);
        }

        void CheckAchievement(holdem.ResultNoti resultNoti)
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

        //카드 보여주는 애니메이션
        private void ResultDisplayEventAsync(holdem.ResultNoti resultnoti)
        {
            var mainWinner = resultnoti.Pots[0].Wins;
            holdem.ResultNoti.Types.Player mainWinnerPlayer = null;

            foreach (var gameplayer in resultnoti.Players)
            {
                bool ismainPotWin = mainWinner.Any(o => o.ChairId == gameplayer.ChairId);
                if (ismainPotWin)
                    mainWinnerPlayer = gameplayer;

                if (!TryGetPlayer(gameplayer.ChairId, out var player))
                    continue;

                int viewIndex = Array.IndexOf(view.playerViewList, player.view);
                player.SetInfoForResultAsync(gameplayer, ismainPotWin, isShowdownPlayed, viewIndex < 5);
            }
        }

        void ResultDisplayEvent(holdem.ResultNoti resultnoti)
        {
            var mainWinner = resultnoti.Pots[0].Wins;
            holdem.ResultNoti.Types.Player mainWinnerPlayer = null;

            foreach (var gameplayer in resultnoti.Players)
            {
                bool ismainPotWin = mainWinner.Any(o => o.ChairId == gameplayer.ChairId);
                if (ismainPotWin)
                    mainWinnerPlayer = gameplayer;

                if (!TryGetPlayer(gameplayer.ChairId, out var player))
                    continue;

                player.SetInfoForResult(gameplayer, ismainPotWin);
            }

            //여기까지 하여 개인별 데이터 전달 및 ui 구성 완료
        }

        private async UniTask SetEndState()
        {
            ChangeGamestate(HoldemState.End);
        }
    }
}
