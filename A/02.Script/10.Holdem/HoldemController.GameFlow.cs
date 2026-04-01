using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 게임 시작 노티 처리 및 게임 시작 연출
    /// </summary>
    public partial class HoldemController
    {
        private void StartGame(holdem.StartNoti startNoti, int revisionId)
        {
            startNotiInfo = startNoti;

            if (myPlayer.IsObserving)
            {
                myPlayer.IsObserving = false;
                onWaitGamePopup?.Invoke(false);
            }

            foreach (var data in startNotiInfo.Config)
            {
                CPPlayer.Server.visualEffectTimeConfig[data.Key] = data.Value;
            }

            ChangeGamestate(HoldemState.Start);
            InitializeOnGameStart();

            PresentAtStartSet(revisionId).Forget();
        }

        private async UniTask PresentAtStartSet(int revisionId)
        {
            await UniTask.NextFrame();
            PresentAtStartSetAfterFrame(revisionId);
        }

        private void PresentAtStartSetAfterFrame(int revisionId)
        {
            view.potAmountObject.SetActive(true);
            view.currentPotAmount.text = Extension.ToKoreanFormat(startNotiInfo.PotAmount, Extension.KoreanFormatMode.Planning);

            if (TryGetPlayer(myChairId, out var selfPlayer))
            {
                selfPlayer.SetCurrentPhase(HoldemState.Start);
                selfPlayer.SetFold(false);
                selfPlayer.SetAllin(false);
            }

            if (revisionId != HoldemDispatchPushHub.revisionId)
            {
                foreach (var eachPlayer in startNotiInfo.Players)
                {
                    if (!TryGetPlayer(eachPlayer.ChairId, out var player)) continue;
                    player.SetCurrentOwnedChip(eachPlayer.Chip);
                    snapShot.LocateAnteSnapShot(eachPlayer.ChairId, eachPlayer.Ante, player.view.throwChipStartPos);
                    player.SetTotalBet(eachPlayer.Ante);
                    if (eachPlayer.ChairId == myChairId)
                    {
                        CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                        CPPlayer.InGame.haveKickVote = eachPlayer.CanKickVote;
                    }
                }
            }
            else
            {
                foreach (var eachPlayer in startNotiInfo.Players)
                {
                    if (!TryGetPlayer(eachPlayer.ChairId, out var player)) continue;
                    player.SetCurrentOwnedChip(eachPlayer.Chip);
                    snapShot.ThrowAnte(eachPlayer.ChairId, eachPlayer.Ante, player.view.throwChipStartPos);
                    player.SetTotalBet(eachPlayer.Ante);
                    if (eachPlayer.ChairId == myChairId)
                    {
                        CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                        CPPlayer.InGame.haveKickVote = eachPlayer.CanKickVote;
                    }
                }
            }

            int index = -1;
            for (int i = 0; i < startNotiInfo.Players.Count; i++)
            {
                if (startNotiInfo.Players[i].ChairId == startNotiInfo.DealerId)
                {
                    index = i;
                    break;
                }
            }

            int count = startNotiInfo.Players.Count;
            int sbIndex = (index + 1) % count;
            int bbIndex = (index + 2) % count;

            int sbChairId = startNotiInfo.Players[sbIndex].ChairId;
            int bbChairId = startNotiInfo.Players[bbIndex].ChairId;

            if (TryGetPlayer(sbChairId, out var sbPlayer)) sbPlayer.SetBlind(true);
            if (TryGetPlayer(bbChairId, out var bbPlayer)) bbPlayer.SetBlind(false);
        }
    }
}