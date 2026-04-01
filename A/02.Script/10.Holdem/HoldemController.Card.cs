using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 홀카드·커뮤니티카드·쇼다운·카드정보 노티 처리
    /// </summary>
    public partial class HoldemController
    {
        public void HoleCardNoti(holdem.HoleCardNoti myCardnoti, int revisionId)
        {
            if (myPlayer.IsObserving)
                return;
            HolecardNotiPresent(myCardnoti, revisionId).Forget();
        }

        async UniTask HolecardNotiPresent(holdem.HoleCardNoti myCardnoti, int revisionId)
        {
            await UniTask.NextFrame();
            HolecardNotiPresentAfterFrame(myCardnoti, revisionId);
        }

        void HolecardNotiPresentAfterFrame(holdem.HoleCardNoti myCardnoti, int revisionId)
        {
            if (!TryGetPlayer(myChairId, out var selfPlayer))
                return;
            if (revisionId != HoldemDispatchPushHub.revisionId)
            {
                snapShot.CardLocateToPlayerSnapshot(selfPlayer, myCardnoti);
            }
            else
            {
                snapShot.CardThrowToPlayer(selfPlayer, myCardnoti);
            }
        }

        public void HoleCardNotiOther(holdem.HoleCardNotiOther otherCardnoti, int revisionId)
        {
            if (myPlayer.IsObserving)
                return;

            HolecardNotiOtherPresent(otherCardnoti, revisionId).Forget();
        }

        async UniTask HolecardNotiOtherPresent(holdem.HoleCardNotiOther otherCardnoti, int revisionId)
        {
            await UniTask.NextFrame();
            HoleCardNotiOtherPresentAfterFrame(otherCardnoti, revisionId);
        }

        void HoleCardNotiOtherPresentAfterFrame(holdem.HoleCardNotiOther otherCardnoti, int revisionId)
        {
            if (!TryGetPlayer(otherCardnoti.ChairId, out var player))
                return;

            if (revisionId != HoldemDispatchPushHub.revisionId)
                snapShot.CardThrowToOtherPlayerSnapshot(player, otherCardnoti);
            else
                snapShot.CardThrowToPlayer(player, otherCardnoti);
        }

        public void CommunityCardsNoti(holdem.CommunityCardsNoti communityCardsNoti, int revisionId)
        {
            if (myPlayer.IsObserving)
                return;
            CommunityCardsPresentEvent(communityCardsNoti, revisionId).Forget();
        }

        async UniTask CommunityCardsPresentEvent(holdem.CommunityCardsNoti communityCardsNoti, int revisionId)
        {
            await UniTask.NextFrame();
            CommunityCardsPresentEventAfterFrame(communityCardsNoti, revisionId);
        }

        void CommunityCardsPresentEventAfterFrame(holdem.CommunityCardsNoti communityCardsNoti, int revisionId)
        {
            if (!TryGetPlayer(myChairId, out var selfPlayer))
                return;
            if (revisionId != HoldemDispatchPushHub.revisionId)
            {
                snapShot.SetCommunityCardsAndSetPlayerRank(communityCardsNoti, selfPlayer, isShowdownPlayed, false, false);
            }
            else
            {
                snapShot.SetCommunityCardsAndSetPlayerRankAsync(communityCardsNoti, selfPlayer, isShowdownPlayed, false, false).Forget();
            }
        }

        public void ShowDownNoti(holdem.ShowdownNoti showdownNoti, int revisionId)
        {
            if (myPlayer.IsObserving)
                return;
            isShowdownPlayed = true;
            ShowdownPresentEvent(showdownNoti, revisionId).Forget();
        }

        async UniTask ShowdownPresentEvent(holdem.ShowdownNoti showdownNoti, int revisionId)
        {
            await UniTask.NextFrame();
            ShowdownPresentEventAfterFrame(showdownNoti, revisionId);
        }

        void ShowdownPresentEventAfterFrame(holdem.ShowdownNoti showdownNoti, int revisionId)
        {
            ChangeGamestate(HoldemState.Showdown);
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.SetCurrentPhase(HoldemState.Showdown);

            if (revisionId != HoldemDispatchPushHub.revisionId)
            {
            }
            else
            {
                PresentationShowDown(showdownNoti).Forget();
            }
        }


        private async UniTask PresentationShowDown(holdem.ShowdownNoti showdownNoti)
        {
            foreach (var con in playerDict)
            {
                con.Value.view.starRankObjNew.SetActive(false);
                con.Value.BetImageActive(false);
            }
            view.showdownPanel.SetActive(true);
            int showdownAnimationMilSec = (int)CPPlayer.Server.visualEffectTimeConfig["SHOWDOWN_MS"];
            float showdownAnimTime = (float)showdownAnimationMilSec / 1000f;
            var animator = view.showdownPanelAnimator;
            animator.speed = 3.0f / showdownAnimTime;
            animator.Play("Showdown");

            await UniTask.Delay(showdownAnimationMilSec);
            view.showdownPanel.SetActive(false);
        }

        public void CardInfoNoti(holdem.CardNoti cardNoti, int revisionId)
        {
            if (myPlayer.IsObserving)
                return;
            CardInfoPresentEvent(cardNoti, revisionId).Forget();
        }

        async UniTask CardInfoPresentEvent(holdem.CardNoti cardNoti, int revisionId)
        {
            await UniTask.NextFrame();
            CardInfoNotiPresentEventAfterFrame(cardNoti, revisionId);
        }

        void CardInfoNotiPresentEventAfterFrame(holdem.CardNoti cardNoti, int revisionId)
        {
            ChangeGamestate(HoldemState.Result);
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.SetCurrentPhase(HoldemState.Result);

            if (revisionId != HoldemDispatchPushHub.revisionId)
            {
            }
            else
            {
                PresentationCardInfoNoti(cardNoti).Forget();
            }
        }


        private async UniTask PresentationCardInfoNoti(holdem.CardNoti cardNoti)
        {
            var tasks = new List<UniTask>();

            foreach (var gameplayer in cardNoti.Players)
            {
                if (!TryGetPlayer(gameplayer.ChairId, out var player))
                    continue;

                for (int i = 0; i < gameplayer.HoleCards.Count; i++)
                {
                    player.holdemPlayerInfo.cardlist[i] = gameplayer.HoleCards[i];
                }

                bool isCardOpen = false;
                for (int i = 0; i < gameplayer.HoleCards.Count; i++)
                {
                    if (!string.IsNullOrEmpty(gameplayer.HoleCards[i]))
                    {
                        isCardOpen = true;
                        break;
                    }
                }

                //카드 오픈하려는 사람
                if (player.isFolded)
                {
                    // if (playerDict[gameplayer.ChairId].isMe)


                    // {
                    //     if (playerDict[gameplayer.ChairId].isCardOpenReserved)
                    //     {
                    //         tasks.Add(playerDict[gameplayer.ChairId].AnimateCardInfo(gameplayer.HoleCards.ToList()));
                    //         for (int i = 0; i < playerDict[gameplayer.ChairId].cardViewerList.Count; i++)
                    //         {
                    //             playerDict[gameplayer.ChairId].cardViewerList[i].mask.gameObject.SetActive(false);
                    //         }
                    //     }
                    // }
                    // else
                    // {
                    //     if (isCardOpen)
                    //     {
                    //         tasks.Add(playerDict[gameplayer.ChairId].AnimateCardInfo(gameplayer.HoleCards.ToList()));
                    //         for (int i = 0; i < playerDict[gameplayer.ChairId].cardViewerList.Count; i++)
                    //         {
                    //             playerDict[gameplayer.ChairId].cardViewerList[i].mask.gameObject.SetActive(false);
                    //         }
                    //     }
                    // }
                }
                else
                {
                    tasks.Add(player.AnimateCardInfo(gameplayer.HoleCards.ToList()));
                    for (int i = 0; i < player.cardViewerList.Count; i++)
                    {
                        player.cardViewerList[i].mask.gameObject.SetActive(false);
                    }
                }
            }

            foreach (var gameplayer in cardNoti.Players)
            {
                if (!TryGetPlayer(gameplayer.ChairId, out var player))
                    continue;
                CardUISetState cardState = CardUISetState.AllCardOpen;
                if (gameplayer.HoleCards.Count < 2)
                    cardState = CardUISetState.CardNotOpen;
                snapShot.SetJokboRankAndCardHighlight(player, cardState, true);
            }


            await UniTask.WhenAll(tasks);
        }
    }
}
