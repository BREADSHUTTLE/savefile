using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Animations;
using UnityEngine.UI;

namespace CAPYBARA
{
    
    public class HoldemPlayerInfo
    {
        public holdem.Player playerInfo;
        public List<long> roundBetChipList = new List<long>();
        public List<string> cardlist;
    }

    public partial class HoldemPlayerController:ICardTouchCallbackListen
    {
        public HoldemPlayerView view;
        public ResultNoti.Types.Player resultPlayerInfo;

        [Header("myInfo")] HoldemViewer holdemViewer;
        public HoldemPlayerInfo holdemPlayerInfo;
        public bool isMe;
        public int chairId;

        public List<CardViewer> cardViewerList = new List<CardViewer>();
        public List<string> touchedCardIndexforOpenList = new List<string>();
        private CancellationTokenSource timeCts;
        private CancellationTokenSource emotCts;

        public bool isFolded;
        public bool isAllin;
        public bool isObserving;
        public bool isWin;
        public HoldemState currentPhase;
        public bool isCardOpenReserved;
        public long currentTotalBet;
        public long startChip;
        public bool isForfeitWin = false;
        private bool isTurnActive = false;

        private IInGameController inGameController;
            
        public long GetTotalBet
        {
            get { return currentTotalBet; }
        }

        public int chairIndexId
        {
            get
            {
                int indexId = chairId - CPPlayer.Holdem.gapBetweenChairIdAndIndex;
                if (indexId < 0)
                {
                    indexId = indexId + Constraints.MaxHoldemPlayerCount;
                }

                return indexId;
            }
        }

        public HoldemPlayerController(Transform viewParent, HoldemViewer holdemviewer,IInGameController controller)
        {
            holdemViewer = holdemviewer;
            inGameController=controller;
            holdemPlayerInfo = new HoldemPlayerInfo();
            CPPlayer.Option.FourCardModeChange += CardColorModeChange;

            CPPlayer.Option.HandRankUseChange += (active) =>
            {
                view.starRankObjNew.SetActive(false);
            };
            CPPlayer.InGame.AFKPopupActive += (reserved) =>
            {
                if (!isMe)
                    return;
                if (!reserved)
                {
                    view.reservedOut.SetActive(false);
                }
            };
        }

        public void Release()
        {
            holdemPlayerInfo = null;

            CPPlayer.Option.FourCardModeChange -= CardColorModeChange;
            isMe = false;
            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }
            cardViewerList.Clear();
            touchedCardIndexforOpenList.Clear();
        }

        public void SetPlayer(holdem.Player _player, int _chairId, bool isme = false)
        {
            holdemPlayerInfo.playerInfo = _player;
            isMe = isme;
            chairId = _chairId;

            view = holdemViewer.playerViewList[chairIndexId];
            view.Init();

            view.cardOpenBtn.onClick.RemoveAllListeners();
            view.cardCloseBtn.onClick.RemoveAllListeners();
            view.cardOpenBtnAtforfeitWin.onClick.RemoveAllListeners();

            view.cardOpenBtn.onClick.AddListener(() => OpenCardReserve().Forget());
            view.cardOpenBtnAtforfeitWin.onClick.AddListener(() => OpenCardReserve().Forget());
            view.cardCloseBtn.onClick.AddListener(() => CloseCardReserve().Forget());


            view.cardOpenBtn.gameObject.SetActive(false);
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(false);
            view.cardCloseBtn.gameObject.SetActive(false);
            view.reservedOut.SetActive(false);

            view.seePlayerInfoBtn.onClick.RemoveAllListeners();
            view.seePlayerInfoBtn.onClick.AddListener(() =>
            {
                inGameController.OnOtherPlayerModalInactive(chairId);
                if (isme)
                {
                    bool isactive= view.mePlayerInfoModal.gameObject.activeInHierarchy;
                    if (isactive)
                    {
                        holdemViewer.OnModalAutoClose(view.mePlayerInfoModal.gameObject);
                    }
                    else
                    {
                        view.mePlayerInfoModal.Set_OpenWindow(holdemPlayerInfo.playerInfo);
                        holdemViewer.OpenModalObject(view.mePlayerInfoModal.gameObject);
                    }
                }
                else
                {

                    bool isactive= view.otherPlayerInfoModal.gameObject.activeInHierarchy;
                    if (isactive)
                    {
                        holdemViewer.OnModalAutoClose(view.otherPlayerInfoModal.gameObject);
                    }
                    else
                    {
                        view.otherPlayerInfoModal.Set_OpenWindow(holdemPlayerInfo.playerInfo);
                        holdemViewer.OpenModalObject(view.otherPlayerInfoModal.gameObject);
                        view.transform.parent.SetAsLastSibling();
                    }
                }
            });

            holdemPlayerInfo.cardlist = new List<string>();
            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }
            cardViewerList.Clear();

            SetCurrentOwnedChip(_player.Chip);
        }

        public void PresentPlayerInfo()
        {
            InfoModalInactive();

            var avatarBundle = ItemBundle.Loaded;
            if (string.IsNullOrEmpty( holdemPlayerInfo.playerInfo.Avatar.Id))
            {
                view.playerImage.sprite = avatarBundle.GetAvatarInGameIcon("AVATAR_1");
                view.inactivemaskImage.sprite = avatarBundle.GetAvatarInGameIcon( "AVATAR_1");
            }
            else
            {
                view.playerImage.sprite = avatarBundle.GetAvatarInGameIcon( holdemPlayerInfo.playerInfo.Avatar.Id);
                view.inactivemaskImage.sprite = avatarBundle.GetAvatarInGameIcon( holdemPlayerInfo.playerInfo.Avatar.Id);
            }
            bool inactiveMaskOn = holdemPlayerInfo.playerInfo.IsObserving || holdemPlayerInfo.playerInfo.IsFolded;
            isObserving = holdemPlayerInfo.playerInfo.IsObserving;

            view.ViewSetting(holdemPlayerInfo.playerInfo);
            view.inActiveMask.SetActive(inactiveMaskOn);
            
            view.currentOwnedChip.gameObject.SetActive(!inactiveMaskOn);
            view.currentOwnedChipInactive.gameObject.SetActive(inactiveMaskOn);
            
            view.playerNickName.gameObject.SetActive(!inactiveMaskOn);
            view.playerNickNameInactive.gameObject.SetActive(inactiveMaskOn);
        }

        public void InitializePlayerData()
        {
            holdemPlayerInfo.cardlist.Clear();
            touchedCardIndexforOpenList.Clear();
            view.starRankObjNew.SetActive(false);

            isFolded = false;
            isAllin = false;
            isWin = false;
            isCardOpenReserved = false;
            currentPhase = HoldemState.End;

            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }

            cardViewerList.Clear();
            SetTotalBet(0);

            view.bestCardObjInRound_me.gameObject.SetActive(false);
            view.bestCardObjInResult.gameObject.SetActive(false);
            view.bestCardObjInResult_Loser.gameObject.SetActive(false);
            view.stampParentObj.SetActive(false);
            view.allinObject.SetActive(false);
            view.winFontImageObj.SetActive(false);
            view.loseObject.SetActive(false);
            view.roundBetChipObj.SetActive(false);
            view.currentOwnedChip.gameObject.SetActive(true);
            view.currentOwnedChipInactive.gameObject.SetActive(false);
            view.playerNickName.gameObject.SetActive(true);
            view.playerNickNameInactive.gameObject.SetActive(false);

            view.cardOpenBtn.gameObject.SetActive(false);
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(false);
            isForfeitWin = false;
            view.cardCloseBtn.gameObject.SetActive(false);
            view.emotionObj.SetActive(false);

            view.inActiveMask.SetActive(false);
        }

        public void StartSet()
        {
            holdemPlayerInfo.cardlist.Clear();

            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }

            cardViewerList.Clear();
            view.bestCardObjInRound_me.gameObject.SetActive(false);
            view.bestCardObjInResult.gameObject.SetActive(false);
            view.bestCardObjInResult_Loser.gameObject.SetActive(false);
            view.stampParentObj.gameObject.SetActive(false);
            view.allinObject.SetActive(false);
            view.winFontImageObj.SetActive(false);
            view.currentOwnedChip.gameObject.SetActive(true);
            view.currentOwnedChipInactive.gameObject.SetActive(false);
            
            view.playerNickName.gameObject.SetActive(true);
            view.playerNickNameInactive.gameObject.SetActive(false);
            
            view.loseObject.SetActive(false);
        }

        public void EnterSet()
        {
            view.dealerBtnObj.SetActive(false);
        }

        public void InfoModalInactive()
        {
            view.mePlayerInfoModal.gameObject.SetActive(false);
            view.otherPlayerInfoModal.gameObject.SetActive(false);
        }

        public void RemovePlayer()
        {
            holdemPlayerInfo.playerInfo = null;

            //플레이어 viewer 세팅
            view.gameObject.SetActive(false);
        }
    }
}
