using System;
using System.Linq;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 기타 서버 이벤트 처리 (킥, 이모티콘, 카드오픈, 예약퇴장, AFK 복귀)
    /// </summary>
    public partial class HoldemController
    {
        private void KickedForSomeReason(KickVoteNoti kickVoteNoti,int revisionId)
        {
            if (myChairId == kickVoteNoti.TargetChairId)
            {
                if (kickVoteNoti.VoteCount >=4)
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"이번 게임 이후 {kickVoteNoti.VoteCount}회 투표수로 인해 추방됩니다.", true));    
                }
                else
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"추방 투표를 받았습니다.({kickVoteNoti.VoteCount}회 누적)", true));    
                }
            }

            if (TryGetPlayer(kickVoteNoti.TargetChairId, out var kickTarget))
                kickTarget.KickVoteRecieveEvent(kickVoteNoti.VoteCount);
        }

        async UniTask EmoticonExpressReq(EmotionInfo emotionInfo)
        {
            if (CPPlayer.InGame.currentGameType != GameType.HOLDEM)
                return;
            string emoteStr = $"{emotionInfo.emoticonKind}_{emotionInfo.emoticonExpress}";
            var res=await Services.Holdem.EmoteReqAsync(CPPlayer.Holdem.currentTableId,myChairId,emoteStr);
            if (res.IsSuccess)
            {
                if (TryGetPlayer(myChairId, out var selfPlayer))
                    selfPlayer.EmoticonExpress(emotionInfo);
            }
        }
        
        private void EmoticonExpressNoti(EmoteNoti emotenoti,int revisionId)
        {
            if (CPPlayer.Cloud.optionValue.useEmoji == false)
                return;

            if (myPlayer.IsObserving)
                return;
            
            
            Partial.IEmoteNoti _emoteNoti = emotenoti;
            
            string[] parts = _emoteNoti.emoteName.Split('_');
            EmoticonKindType kind=Extension.StringToEnum<EmoticonKindType>(parts[0]);
            EmoticonExpressType express=Extension.StringToEnum<EmoticonExpressType>(parts[1]);
            
            var emoticon=InGameResourcesBundle.Loaded.emotionInfoList.FirstOrDefault(o=>o.emoticonKind==kind&&o.emoticonExpress==express);
            if (emoticon != null && TryGetPlayer(_emoteNoti.fromChairId, out var emotePlayer))
                emotePlayer.EmoticonExpress(emoticon);
        }
        
        private void CardOpenNoti(CardOpenNoti cardopenNoti,int revisionId)
        {
            if (myPlayer.IsObserving)
                return;
            if (!TryGetPlayer(cardopenNoti.ChairId, out var cardOpenPlayer))
                return;

            Extension.eLog($"{cardopenNoti.ChairId} card open");

            int viewIndex = Array.IndexOf(view.playerViewList, cardOpenPlayer.view);

            if (cardopenNoti.ChairId != myChairId)
                cardOpenPlayer.FoldUserCardSet(cardopenNoti.HoleCards.ToList(), viewIndex < 5);
            CardOpenPresentAsync(cardopenNoti, revisionId).Forget();
        }

        void CardOpenSnapShot(CardOpenNoti cardopenNoti, int revisionId)
        {
            
        }

        async UniTask CardOpenPresentAsync(CardOpenNoti cardopenNoti,int revisionId)
        {
            await UniTask.NextFrame();

            Debug.Log($"카드오픈:{cardopenNoti.ChairId}");

            if (!TryGetPlayer(cardopenNoti.ChairId, out var player))
                return;

            if (revisionId == HoldemDispatchPushHub.revisionId)
            {
                // if (player.isForfeitWin==false)
                // {
                //
                // }
                await player.OpenFoldUserCards();

                bool fullCardList = true;
                for (int i = 0; i < player.holdemPlayerInfo.cardlist.Count; i++)
                {
                    if (string.IsNullOrEmpty(player.holdemPlayerInfo.cardlist[i]))
                    {
                        fullCardList = false;
                        break;
                    }
                }

                if (fullCardList && player.holdemPlayerInfo.cardlist.Count == 2)
                    snapShot.SetJokboRankAndCardHighlight(player, CardUISetState.AllCardOpen, false);
            }
        }

        private void LeaveReservedNoti(LeaveReservedNoti leaveReserved, int revisionId)
        {
            if (!TryGetPlayer(leaveReserved.ChairId, out var leavePlayer))
                return;
            leavePlayer.ReserveOut(!leaveReserved.Cancel);
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
            if (CPPlayer.Server.currentConnectedGameType != GameType.HOLDEM)
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
