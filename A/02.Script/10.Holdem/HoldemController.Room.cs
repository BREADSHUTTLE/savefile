using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Common;
namespace CAPYBARA
{
    /// <summary>
    /// 방 입장 / 퇴장 / 예약 퇴장 처리
    /// </summary>
    public partial class HoldemController
    {
        private void EnterGameTable_Reponse(holdem.EnterRes enterRes)
        {
            view.emotionView.Init();
            HoldemDispatchPushHub.IsHoldemActive = true;
            foreach (var data in enterRes.Config)
            {
                CPPlayer.Server.visualEffectTimeConfig[data.Key] = data.Value;
            }
            Extension.eLog($"방 입장!", Color.yellow);
            enterresInfo = enterRes;
            if (enterRes.Pots.Count > 0)
            {
                view.currentPotAmount.text = enterRes.Pots[0].ToString();
            }
            else
            {
                view.currentPotAmount.text = "0";
            }
            InitializeOnEnter();
            SetPlayersInfo();
            SetCommunityCardOnEnter();
            SetDealerBtnObjOnEnter();

            ChangeGamestate(HoldemState.Idle);

            if (enterresInfo.InGame)
            {
                if (myPlayer.IsObserving)
                {
                    onWaitGamePopup?.Invoke(true);
                }
                else
                {
                    onWaitGamePopup?.Invoke(false);
                }
            }
            else
            {
                onWaitGamePopup?.Invoke(false);
            }
        }

        public void SetPlayersInfo()
        {
            var mychair = enterresInfo.Chairs.FirstOrDefault(o => o.Id == enterresInfo.ChairId);
            if (mychair != null)
            {
                myPlayer = mychair.Player;
            }

            if (playerDict.ContainsKey(enterresInfo.ChairId))
            {
                mePlayerController = playerDict[enterresInfo.ChairId];
                mePlayerController.SetPlayer(myPlayer, enterresInfo.ChairId, true);
                snapShot.SetSnapShotPlayer(mePlayerController);
            }
            else
            {
                mePlayerController = new HoldemPlayerController(mainObject.transform, view,this);
                mePlayerController.SetPlayer(myPlayer, enterresInfo.ChairId, true);
                snapShot.SetSnapShotPlayer(mePlayerController);
                playerDict.Add(enterresInfo.ChairId, mePlayerController);
            }
            if (mychair.Player.IsObserving == false)
            {
                if (mychair.Player.CardCount > 0 && enterresInfo.InGame)
                {
                    for (int i = 0; i < mychair.Player.CardCount; i++)
                    {
                        snapShot.SetCardToMeAtEnter(mePlayerController, enterresInfo.HoleCards[i]);
                    }
                }
            }

            myChairId = enterresInfo.ChairId;

            foreach (var enterResChair in enterresInfo.Chairs)
            {
                if (enterResChair.Player == null)
                    continue;
                if (enterResChair.Id == myChairId)
                    continue;
                HoldemPlayerController other;
                if (playerDict.ContainsKey(enterResChair.Id))
                {
                    other = playerDict[enterResChair.Id];
                    other.SetPlayer(enterResChair.Player, enterResChair.Id);
                    snapShot.SetSnapShotPlayer(other);
                }
                else
                {
                    other = new HoldemPlayerController(mainObject.transform, view,this);
                    other.SetPlayer(enterResChair.Player, enterResChair.Id);
                    snapShot.SetSnapShotPlayer(other);
                    playerDict.Add(enterResChair.Id, other);
                }

                if (enterResChair.Player.CardCount > 0 && enterresInfo.InGame)
                {
                    if (mychair.Player.IsObserving == false)
                    {
                        for (int i = 0; i < enterResChair.Player.CardCount; i++)
                        {
                            snapShot.SetCardToOtherPlayerAtEnter(other);
                        }
                    }
                }
            }
        }

        public void SetCommunityCardOnEnter()
        {
            if (myPlayer.IsObserving)
                return;
            if (enterresInfo.InGame)
            {
                snapShot.SetCommunityCardsOnEnter(enterresInfo);
            }
        }

        public void SetDealerBtnObjOnEnter()
        {
            if (myPlayer.IsObserving)
            {
                foreach (var player in playerDict)
                {
                    player.Value.EnterSet();
                    player.Value.InfoModalInactive();
                }
            }
            else
            {
                foreach (var player in playerDict)
                {
                    player.Value.EnterSet();
                    player.Value.InfoModalInactive();
                    if (enterresInfo.DealerId == player.Value.chairId)
                    {
                        player.Value.view.dealerBtnObj.SetActive(true);
                    }
                    else
                    {
                        player.Value.view.dealerBtnObj.SetActive(false);
                    }
                }
            }
        }

        private void EnterGameOtherPlayer_Noti(holdem.EnterNoti enterNoti, int revisionId)
        {
            HoldemPlayerController other;

            if (playerDict.ContainsKey(enterNoti.ChairId))
            {
                other = playerDict[enterNoti.ChairId];
                other.SetPlayer(enterNoti.Player, enterNoti.ChairId);
            }
            else
            {
                other = new HoldemPlayerController(mainObject.transform, view,this);
                other.SetPlayer(enterNoti.Player, enterNoti.ChairId);
                playerDict.Add(enterNoti.ChairId, other);
            }

            PresentPlayerView(other, revisionId).Forget();
        }

        private async UniTask PresentPlayerView(HoldemPlayerController holdemPlayerController, int revisionId)
        {
            await UniTask.NextFrame();
            snapShot.SetSnapShotPlayer(holdemPlayerController);
        }

        private void LeaveGameOtherPlayer_Noti(holdem.LeaveNoti leaveNoti, int revisionId)
        {
            if (playerDict.ContainsKey(leaveNoti.ChairId))
            {
                if (leaveNoti.ChairId == myChairId)
                {
                    if (leaveNoti.Reason == KickReason.KrNone)
                    {
                        if (reserveMoveRoomRequest)
                        {
                            CPPlayer.InGame.MoveTable?.Invoke(GameType.HOLDEM);
                        }
                        else
                        {
                            CPPlayer.Holdem.currentTableId = 0;
                            CPPlayer.InGame.LeaveGame?.Invoke(GameType.HOLDEM);
                        }
                    }
                    else
                    {
                        CPPlayer.Holdem.currentTableId = 0;
                        CPPlayer.InGame.LeaveGame?.Invoke(GameType.HOLDEM);
                    }
                    LeaveGameDataInitialize();
                    view.leaveBtn.enabled = false;
                    view.moveRoomBtn.enabled = false;
                }
                else
                {
                    playerDict[leaveNoti.ChairId].RemovePlayer();
                }
            }

            foreach (var player in playerDict)
            {
                if (leaveNoti.DealerId == player.Value.chairId)
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
            else if (reserveMoveRoomRequest)
            {
                await LeaveThisRoomAndMoveOtherRoomOrReserve();
            }
        }

        private async UniTask LeaveThisRoomOrReserve(bool isExile, bool isNotiOut = false)
        {
            CPPlayer.InGame.isMovingTable = false;
            if (isNotiOut == false)
            {
                var leaveResPacket = await Services.Holdem.LeaveRoomAsync(CPPlayer.Holdem.currentTableId);
                if (leaveResPacket.IsSuccess)
                {
                    Extension.eLog($"나갈때 응답 테이블id:{leaveResPacket.Data.TableId} 현재 테이블 id:{CPPlayer.Holdem.currentTableId}");
                    if (leaveResPacket.Data.IsReserved)
                    {
                        if (isExile)
                        {
                            isExiled = true;
                        }
                        reserveLeaveRequest = true;

                        view.leaveBtn.gameObject.SetActive(!reserveLeaveRequest);
                        view.leaveReservedObj.gameObject.SetActive(reserveLeaveRequest);
                        if (TryGetPlayer(myChairId, out var selfPlayer))
                            selfPlayer.ReserveOut(true);
                        
                        MoveRoomBtnInit();

                        return;
                    }
                }
            }

            view.leaveBtn.enabled = false;
            view.moveRoomBtn.enabled = false;

            LeaveGameDataInitialize();
            CPPlayer.Holdem.currentTableId = 0;
            CPPlayer.InGame.LeaveGame?.Invoke(GameType.HOLDEM);

            await UniTask.Yield();
        }

        private async UniTask LeaveThisRoomAndMoveOtherRoomOrReserve()
        {
            if (isExiled)
                return;
            if (CPPlayer.InGame.isMovingTable)
                return;

            CPPlayer.InGame.isMovingTable = true;

            var leaveResPacket = await Services.Holdem.LeaveRoomAsync(CPPlayer.Holdem.currentTableId);
            if (leaveResPacket.IsSuccess)
            {
                Extension.eLog($"나갈때 응답 테이블id:{leaveResPacket.Data.TableId} 현재 테이블 id:{CPPlayer.Holdem.currentTableId}");
                if (leaveResPacket.Data.IsReserved)
                {
                    reserveMoveRoomRequest = true;
                    if (reserveMoveRoomRequest)
                    {
                        view.moveRoomBtn.gameObject.SetActive(!reserveMoveRoomRequest);
                        view.moveReservedObj.gameObject.SetActive(reserveMoveRoomRequest);
                        if (TryGetPlayer(myChairId, out var selfPlayer))
                            selfPlayer.ReserveOut(true);
                        LeaveRoomBtnInit();
                    }
                    CPPlayer.InGame.isMovingTable = false;
                    return;
                }
            }
            view.leaveBtn.enabled = false;
            view.moveRoomBtn.enabled = false;

            LeaveGameDataInitialize();
            CPPlayer.InGame.MoveTable?.Invoke(GameType.HOLDEM);
        }

        public void LeaveGameDataInitialize()
        {
            HoldemDispatchPushHub.IsHoldemActive = false;

            view.InitializeViewData();
            snapShot.ClearDataInRoundGame();
            ActionToggleInActivate();
            SetInGameDisplayInitialize();
            InitializePlayersDisplay();

            Debug.Log("퇴장 완료");
            reserveLeaveRequest = false;
            reserveMoveRoomRequest = false;
            view.leaveBtn.enabled = true;
            view.moveRoomBtn.enabled = true;
            isExiled = false;
            view.leaveBtn.gameObject.SetActive(true);
            view.leaveReservedObj.gameObject.SetActive(false);
            if (TryGetPlayer(myChairId, out var selfPlayer))
                selfPlayer.ReserveOut(false);
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

            startNotiInfo = null;
            enterresInfo = null;

            if (_resultPresentationCts != null && !_resultPresentationCts.Token.IsCancellationRequested)
            {
                _resultPresentationCts.Cancel();
            }
            CPPlayer.InGame.AFKPopupActive?.Invoke(false);
        }

        private async UniTask CancelReserveLeave()
        {
            if (isExiled)
                return;

            var leaveResPacket = await Services.Holdem.LeaveRoomCacnelAsync(CPPlayer.Holdem.currentTableId);
            if (leaveResPacket.IsSuccess)
            {
                LeaveRoomBtnInit();
                if (TryGetPlayer(myChairId, out var selfPlayer))
                    selfPlayer.ReserveOut(false);
            }
        }

        private async UniTask CancelReserveMove()
        {
            if (isExiled)
                return;
            CPPlayer.InGame.isMovingTable = false;
            
            var leaveResPacket = await Services.Holdem.LeaveRoomCacnelAsync(CPPlayer.Holdem.currentTableId);
            if (leaveResPacket.IsSuccess)
            {
                MoveRoomBtnInit();
                if (TryGetPlayer(myChairId, out var selfPlayer))
                    selfPlayer.ReserveOut(false);
            }
        }

        private void LeaveRoomBtnInit()
        {
            reserveLeaveRequest = false;

            view.leaveBtn.enabled = true;
            isExiled = false;
            view.leaveBtn.gameObject.SetActive(true);
            view.leaveReservedObj.gameObject.SetActive(false);
    
        }

        private void MoveRoomBtnInit()
        {
            reserveMoveRoomRequest = false;
            CPPlayer.InGame.isMovingTable = false;

            view.moveRoomBtn.enabled = true;
            view.moveRoomBtn.gameObject.SetActive(true);
            view.moveReservedObj.gameObject.SetActive(false);

        }
    }
}
