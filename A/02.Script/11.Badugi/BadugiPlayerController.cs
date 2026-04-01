using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlackTree.Bundles;
using CAPYBARA.Core;
using CAPYBARA.badugi;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.DOTween;
using UnityEngine;
using DG.Tweening;
using Unity.Collections;

namespace CAPYBARA
{
    public class BadugiPlayerInfo
    {
        public badugi.Player playerInfo;
        public List<long> roundBetChipList = new List<long>();
        public List<string> cardlist=new List<string>();
    }

    public class BadugiPlayerController : ICardTouchCallbackListen,ICardBadugiCardRecommend
    {
        public BadugiPlayerView view;
        public ResultNoti.Types.Player resultPlayerInfo;

        [Header("myInfo")] BadugiViewer badugiViewer;
        public BadugiPlayerInfo badugiPlayerInfo;
        public bool isMe;
        public int chairId;

        public List<BadugiCardViewer> cardViewerList = new List<BadugiCardViewer>();
        public List<string> touchedCardIndexList = new List<string>();
        public List<string> touchedCardIndexforOpenList = new List<string>();

        private CancellationTokenSource timeCts;
        private CancellationTokenSource emotCts;

        public bool isFolded;
        public bool isAllin;
        public bool isObserving;
        public bool isWin;
        public BadugiState currentPhase;
        public bool isCardOpenReserved;
        public long currentTotalBet;

        public List<int> sortedindex = new List<int>();
        public List<string> sortedCardList = new List<string>();
        public List<BadugiCardViewer> sortedCardViewerList = new List<BadugiCardViewer>();

        private IInGameController inGameController;
        
        public long GetTotalBet
        {
            get { return currentTotalBet; }
        }

        public int chairIndexId
        {
            get
            {
                int indexId = chairId - CPPlayer.Badugi.gapBetweenChairIdAndIndex;
                if (CPPlayer.InGame.currentGameMode == GameMode.TwoVS)
                {
                    if (indexId == 0)
                    {
                        // 내 자리, 그대로
                    }
                    else
                    {
                        // 상대 자리, 랜덤으로 지정된 인덱스 사용
                        return CPPlayer.Badugi.twoVSOpponentViewIndex;
                    }
                }

                if (indexId < 0)
                {
                    indexId = indexId + Constraints.MaxBadugiPlayerCount;
                }

                return indexId;
            }
        }

        public BadugiPlayerController(Transform viewParent, BadugiViewer badugiviewer,IInGameController controller)
        {
            badugiViewer = badugiviewer;
            inGameController = controller;
            
            badugiPlayerInfo = new BadugiPlayerInfo();

            CPPlayer.Option.FourCardModeChange += CardColorModeChange;

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
            //badugiPlayerInfo = null;

            isMe = false;
            CPPlayer.Option.FourCardModeChange -= CardColorModeChange;

            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }

            cardViewerList.Clear();
            touchedCardIndexList.Clear();
            touchedCardIndexforOpenList.Clear();
        }

        public void HighlightCardforChange(int cardRankIndex, bool activeForChange)
        {
            if (isMe == false)
                return;
            if ((int)CPPlayer.Badugi.currentBadugiState > (int)BadugiState.Evening)
                return;
            if (isFolded)
                return;

            int index = -1;
            string cardString = "";
            for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
            {
                int cardRank = CardRankCalculater.GetCardIndex(badugiPlayerInfo.cardlist[i]);
                if (cardRank == cardRankIndex)
                {
                    index = i;
                    cardString = badugiPlayerInfo.cardlist[i];
                    break;
                }
            }

            if (index >= 0)
            {
                if (touchedCardIndexList.Contains(cardString) && activeForChange == false)
                {
                    touchedCardIndexList.Remove(cardString);
                }

                if (!touchedCardIndexList.Contains(cardString) && activeForChange)
                {
                    touchedCardIndexList.Add(cardString);
                }

                if (isFolded)
                {
                    cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, activeForChange);
                }
                else
                {
                    cardViewerList[index].HighLightCardCallbackAtBadugi(cardRankIndex, activeForChange);
                }
            }

            CPPlayer.Badugi.CardTouchCallback2?.Invoke();
        }

        public void TouchedCardforChangeSetAtEvening(int cardRankIndex, bool activeForChange)
        {
            if (isMe == false)
                return;
            // if ((int)CPPlayer.Badugi.currentBadugiState >= (int)BadugiState.AfterEvening)
            //     return;
            if (isFolded)
                return;

            int index = -1;
            string cardString = "";
            for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
            {
                int cardRank = CardRankCalculater.GetCardIndex(badugiPlayerInfo.cardlist[i]);
                if (cardRank == cardRankIndex)
                {
                    index = i;
                    cardString = badugiPlayerInfo.cardlist[i];
                    break;
                }
            }

            if (index >= 0)
            {
                if (touchedCardIndexList.Contains(cardString) && activeForChange == false)
                {
                    touchedCardIndexList.Remove(cardString);
                }

                if (!touchedCardIndexList.Contains(cardString) && activeForChange)
                {
                    touchedCardIndexList.Add(cardString);
                }


                cardViewerList[index].HighLightCardCallbackAtBadugi(cardRankIndex, activeForChange);
            }

            CPPlayer.Badugi.CardTouchCallback2?.Invoke();
        }

        public void CardTouchCallback(int cardRankIndex, bool activeForChange)
        {
            if (isMe == false)
                return;
            if (!isFolded && isForfeitWin == false)
                return;

            if (currentPhase >= BadugiState.ShowDown && isCardOpenReserved)
                return;

            int index = -1;
            string cardString = "";
            //touchedCardIndexforOpenList.Clear();
            for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
            {
                int cardRank = CardRankCalculater.GetCardIndex(badugiPlayerInfo.cardlist[i]);
                if (cardRank == cardRankIndex)
                {
                    index = i;
                    break;
                }
            }

            for (int i = 0; i < sortedCardList.Count; i++)
            {
                int cardRank = CardRankCalculater.GetCardIndex(sortedCardList[i]);
                if (cardRank == cardRankIndex)
                {
                    cardString = sortedCardList[i];
                    if (i >= touchedCardIndexforOpenList.Count)
                    {
                        if (activeForChange)
                        {
                            touchedCardIndexforOpenList.Add(cardString);
                        }
                        else
                        {
                            touchedCardIndexforOpenList.Add("");
                        }
                    }
                    else
                    {
                        if (activeForChange)
                        {
                            touchedCardIndexforOpenList[i] = cardString;
                        }
                        else
                        {
                            touchedCardIndexforOpenList[i] = "";
                        }
                    }
                }
                else
                {
                    if (i >= touchedCardIndexforOpenList.Count)
                    {
                        touchedCardIndexforOpenList.Add("");
                    }
                }
            }

            cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, activeForChange);

            Extension.eLog($"touch card and this list:{string.Join(",", touchedCardIndexforOpenList)}", Color.cyan);

            bool isCardNotSelected = true;
            for (int i = 0; i < touchedCardIndexforOpenList.Count; i++)
            {
                if (!string.IsNullOrEmpty(touchedCardIndexforOpenList[i]))
                {
                    isCardNotSelected = false;
                    break;
                }
            }

            if (isCardOpenReserved && isCardNotSelected)
            {
                CloseCardReserve().Forget();
            }

            if (isCardOpenReserved && !isCardNotSelected)
            {
                PostOpenCardlistInfoAsync().Forget();
            }
            //PostOpenCardlistInfoAsync().Forget();
        }

        public void CardTouchedAfterFold(int cardRankIndex, bool activeForChange)
        {
            if (isMe == false)
                return;
            if (!isFolded && isForfeitWin == false)
                return;

            if (currentPhase >= BadugiState.ShowDown && isCardOpenReserved)
                return;

            int index = -1;
            string cardString = "";
            //touchedCardIndexforOpenList.Clear();
            for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
            {
                int cardRank = CardRankCalculater.GetCardIndex(badugiPlayerInfo.cardlist[i]);
                if (cardRank == cardRankIndex)
                {
                    index = i;
                    break;
                }
            }

            for (int i = 0; i < sortedCardList.Count; i++)
            {
                int cardRank = CardRankCalculater.GetCardIndex(sortedCardList[i]);
                if (cardRank == cardRankIndex)
                {
                    cardString = sortedCardList[i];
                    if (i >= touchedCardIndexforOpenList.Count)
                    {
                        if (activeForChange)
                        {
                            touchedCardIndexforOpenList.Add(cardString);
                        }
                        else
                        {
                            touchedCardIndexforOpenList.Add("");
                        }
                    }
                    else
                    {
                        if (activeForChange)
                        {
                            touchedCardIndexforOpenList[i] = cardString;
                        }
                        else
                        {
                            touchedCardIndexforOpenList[i] = "";
                        }
                    }
                }
                else
                {
                    if (i >= touchedCardIndexforOpenList.Count)
                    {
                        touchedCardIndexforOpenList.Add("");
                    }
                }
            }

            cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, activeForChange);

            Extension.eLog($"touch card and this list:{string.Join(",", touchedCardIndexforOpenList)}", Color.cyan);

            bool isCardNotSelected = true;
            for (int i = 0; i < touchedCardIndexforOpenList.Count; i++)
            {
                if (!string.IsNullOrEmpty(touchedCardIndexforOpenList[i]))
                {
                    isCardNotSelected = false;
                    break;
                }
            }

            if (isCardOpenReserved && isCardNotSelected)
            {
                CloseCardReserve().Forget();
            }

            if (isCardOpenReserved && !isCardNotSelected)
            {
                PostOpenCardlistInfoAsync().Forget();
            }
            //PostOpenCardlistInfoAsync().Forget();
        }

        async UniTask PostOpenCardlistInfoAsync()
        {
            if (touchedCardIndexforOpenList.Count == 0)
            {
                var closeReqAsync = await Services.Badugi.CardCloseReqAsync(CPPlayer.Badugi.currentTableId);
                if (closeReqAsync.IsSuccess)
                {
                    Extension.eLog("card 오픈 취소!", Color.green);
                }

                return;
            }

            //card open 패킷인데 나중에 수정예정임
            Extension.eLog($"card open list{string.Join(",", touchedCardIndexforOpenList)}", Color.cyan);
            var openPacketRes = await Services.Badugi.CardOpenReqAsync(CPPlayer.Badugi.currentTableId, touchedCardIndexforOpenList);
            if (openPacketRes.IsSuccess)
            {
                Extension.eLog($"card open post success{string.Join(",", touchedCardIndexforOpenList)}", Color.cyan);
            }
        }

        private void CardColorModeChange(bool isFourColor)
        {
            if (isFourColor)
            {
                for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
                {
                    if (string.IsNullOrEmpty(badugiPlayerInfo.cardlist[i]))
                        continue;
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(badugiPlayerInfo.cardlist[i]);
                    var CardInfo = InGameResourcesBundle.Loaded.cardResourceList.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    cardViewerList[i].cardImage.sprite = CardInfo.cardSprite;
                }
            }
            else
            {
                for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
                {
                    if (string.IsNullOrEmpty(badugiPlayerInfo.cardlist[i]))
                        continue;
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(badugiPlayerInfo.cardlist[i]);
                    var CardInfo = InGameResourcesBundle.Loaded.cardResourceList_TwoColor.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    cardViewerList[i].cardImage.sprite = CardInfo.cardSprite;
                }
            }
        }

        public void SetPlayer(badugi.Player _player, int _chairId, bool isme = false)
        {
            
            badugiPlayerInfo.playerInfo = _player;
            isMe = isme;
            chairId = _chairId;

            badugiPlayerInfo.roundBetChipList.Clear();
            badugiPlayerInfo.cardlist = new List<string>();

            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }

            cardViewerList.Clear();

            view = badugiViewer.playerViewList[chairIndexId];
            view.Init(badugiPlayerInfo.playerInfo);

            var avatarBundle = ItemBundle.Loaded;
            if (string.IsNullOrEmpty(badugiPlayerInfo.playerInfo.Avatar.Id))
            {
                view.playerImage.sprite = avatarBundle.GetAvatarInGameIcon("AVATAR_1");
                view.inactivemaskImage.sprite = avatarBundle.GetAvatarInGameIcon("AVATAR_1");
            }
            else
            {
                view.playerImage.sprite = avatarBundle.GetAvatarInGameIcon(badugiPlayerInfo.playerInfo.Avatar.Id);
                view.inactivemaskImage.sprite = avatarBundle.GetAvatarInGameIcon(badugiPlayerInfo.playerInfo.Avatar.Id);
            }

            view.cardOpenBtn.onClick.RemoveAllListeners();
            view.cardCloseBtn.onClick.RemoveAllListeners();
            view.cardOpenBtnAtforfeitWin.onClick.RemoveAllListeners();

            view.cardOpenBtn.onClick.AddListener(() => OpenCardReserve().Forget());
            view.cardOpenBtnAtforfeitWin.onClick.AddListener(() => OpenCardReserve().Forget());
            view.cardCloseBtn.onClick.AddListener(() => CloseCardReserve().Forget());

            InfoModalInactive();

            view.cardOpenBtn.gameObject.SetActive(false);
            view.cardCloseBtn.gameObject.SetActive(false);
            view.drawActionParentObj.gameObject.SetActive(false);
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(false);
            view.reservedOut.SetActive(false);

            view.seePlayerInfoBtn.onClick.RemoveAllListeners();
            view.seePlayerInfoBtn.onClick.AddListener(() =>
            {
                inGameController.OnOtherPlayerModalInactive(chairId);
                if (isme)
                {
                    bool isactive = view.mePlayerInfoModal.gameObject.activeInHierarchy;
                    if (isactive)
                    {
                        badugiViewer.OnModalAutoClose(view.mePlayerInfoModal.gameObject);
                    }
                    else
                    {
                        view.mePlayerInfoModal.Set_OpenWindow(badugiPlayerInfo.playerInfo);
                        badugiViewer.OpenModalObject(view.mePlayerInfoModal.gameObject);
                    }
                }
                else
                {
                    bool isactive = view.otherPlayerInfoModal.gameObject.activeInHierarchy;
                    if (isactive)
                    {
                        badugiViewer.OnModalAutoClose(view.otherPlayerInfoModal.gameObject);
                    }
                    else
                    {
                        view.otherPlayerInfoModal.Set_OpenWindow(badugiPlayerInfo.playerInfo);
                        badugiViewer.OpenModalObject(view.otherPlayerInfoModal.gameObject);
                        view.transform.parent.SetAsLastSibling();
                    }
                }
            });

            badugiPlayerInfo.cardlist = new List<string>();
    

            for (int i = 0; i < view.roundInfo.Length; i++)
            {
                view.roundInfo[i].Init();
            }
            
            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }
            cardViewerList.Clear();

            view.ViewSetting();
            view.cardOpenBtn.gameObject.SetActive(false);
            view.cardCloseBtn.gameObject.SetActive(false);

            isObserving = _player.IsObserving;

            bool inactiveMaskOn = _player.IsObserving || _player.IsFolded;

            view.inActiveMask.SetActive(inactiveMaskOn);
            SetCurrentOwnedChip(_player.Chip);
        }

        public bool isForfeitWin = false;

        public void SetCardOpenAtForfeitWin()
        {
            isForfeitWin = true;
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(true);
        }

        public void SetCurrentPhase(BadugiState _currentPhase)
        {
            currentPhase = _currentPhase;
        }

        public void SetCardOpenBtn()
        {
            if (currentPhase != BadugiState.End)
            {
                if (currentPhase != BadugiState.ShowDown && currentPhase != BadugiState.Result)
                {
                    if (isFolded)
                    {
                        if (isCardOpenReserved)
                        {
                            view.cardOpenBtn.gameObject.SetActive(false);
                            view.cardCloseBtn.gameObject.SetActive(false);
                        }
                        else
                        {
                            view.cardOpenBtn.gameObject.SetActive(true);
                            view.cardCloseBtn.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        view.cardOpenBtn.gameObject.SetActive(false);
                        view.cardCloseBtn.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (isFolded)
                    {
                        if (isCardOpenReserved)
                        {
                            view.cardOpenBtn.gameObject.SetActive(false);
                            view.cardCloseBtn.gameObject.SetActive(false);
                        }
                        else
                        {
                            view.cardOpenBtn.gameObject.SetActive(true);
                            view.cardCloseBtn.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        view.cardOpenBtn.gameObject.SetActive(false);
                        view.cardCloseBtn.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                view.cardOpenBtn.gameObject.SetActive(false);
                view.cardCloseBtn.gameObject.SetActive(false);
            }
        }

        public void SetAllin(bool _isAllin)
        {
            isAllin = _isAllin;
        }

        public void SetFold(bool isfold)
        {
            isFolded = isfold;
            if (isMe)
            {
                if (currentPhase != BadugiState.ShowDown && currentPhase != BadugiState.End && currentPhase != BadugiState.Result)
                {
                    if (isFolded)
                    {
                        if (isCardOpenReserved)
                        {
                            view.cardOpenBtn.gameObject.SetActive(false);
                            view.cardCloseBtn.gameObject.SetActive(true);
                        }
                        else
                        {
                            view.cardOpenBtn.gameObject.SetActive(true);
                            view.cardCloseBtn.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        view.cardOpenBtn.gameObject.SetActive(false);
                        view.cardCloseBtn.gameObject.SetActive(false);
                    }
                }
                else
                {
                    view.cardOpenBtn.gameObject.SetActive(false);
                    view.cardCloseBtn.gameObject.SetActive(false);
                }

                if (isFolded)
                {
                    float highlightDownTime = 0.3f;
                    if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_DOWN_MS"))
                    {
                        highlightDownTime = (float)CPPlayer.Server.visualEffectTimeConfig["SELECT_DOWN_MS"] / 1000f;
                    }

                    //card setting
                    foreach (var badugiCardViewer in cardViewerList)
                    {
                        badugiCardViewer.InactiveSelectEffectAtFold(highlightDownTime);
                    }

                    touchedCardIndexList.Clear();
                }
            }

            if (isFolded)
            {
                //card setting
                foreach (var badugiCardViewer in cardViewerList)
                {
                    badugiCardViewer.SetMaskFade();
                }

                float dieDim = (float)CPPlayer.Server.visualEffectTimeConfig["DIE_ME_DIM_MS"] / 1000f;

                view.inActiveMask.SetActive(true);
                var c = view.inactivemaskImage.color;
                c.a = 0f;
                view.inactivemaskImage.color = c;

                view.inactivemaskImage.DOFade(0.5f, dieDim);
            }
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

        public void ResetRoundInfo()
        {
            for (int i = 0; i < view.roundInfo.Length; i++)
            {
                view.roundInfo[i].Init();
            }
        }

        public void InitializePlayerData()
        {
            badugiPlayerInfo.cardlist.Clear();
            touchedCardIndexList.Clear();
            touchedCardIndexforOpenList.Clear();

            isAllin = false;
            isFolded = false;
            isWin = false;
            isCardOpenReserved = false;
            currentPhase = BadugiState.End;
            resultRankString = null;

        

            for (int i = 0; i < view.roundInfo.Length; i++)
            {
                view.roundInfo[i].Init();
            }

            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }
            sortedindex.Clear();
            cardViewerList.Clear();
            SetTotalBet(0);

            view.bestCardObj.gameObject.SetActive(false);
            view.betActionTypeImageParentObj.gameObject.SetActive(false);
            view.winChipObject.SetActive(false);
            view.loseObject.SetActive(false);
            view.currentOwnedChip.gameObject.SetActive(true);
            view.winFontImageObj.SetActive(false);
            view.roundBetChipObj.SetActive(false);
            view.allinObject.SetActive(false);

            view.cardOpenBtn.gameObject.SetActive(false);
            view.cardCloseBtn.gameObject.SetActive(false);
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(false);
            isForfeitWin = false;

            view.readyCompleteObj.SetActive(false);

            view.inActiveMask.SetActive(isFolded);
        }

        public void StartSet()
        {
            badugiPlayerInfo.cardlist.Clear();
            touchedCardIndexList.Clear();

            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }

            badugiPlayerInfo.playerInfo.IsObserving = false;
            isObserving = false;

            cardViewerList.Clear();
            view.bestCardObj.gameObject.SetActive(false);
            view.betActionTypeImageParentObj.gameObject.SetActive(false);
            view.winChipObject.SetActive(false);
            view.winFontImageObj.SetActive(false);
            view.loseObject.SetActive(false);
            view.currentOwnedChip.gameObject.SetActive(true);
            view.readyCompleteObj.SetActive(false);
            view.allinObject.SetActive(false);
        }

        public void ClearCurrentRoundBetHistory()
        {
            badugiPlayerInfo.roundBetChipList.Clear();
            view.roundBetChipObj.SetActive(false);
        }

        public void BetAnte(long ante, long OwnedChip)
        {
            badugiPlayerInfo.roundBetChipList.Add(ante);
            SetCurrentOwnedChip(OwnedChip);

            //TODO ui작동!
            ThrowAnte(ante);
            //TODO ui작동!
        }

        void ThrowAnte(long ante)
        {
            CPPlayer.Badugi.ThrowAnte(chairId, ante, view.throwChipStartPos);
        }

        public void BetChip(long bet, long OwnedChip)
        {
            badugiPlayerInfo.roundBetChipList.Add(bet);
            SetCurrentOwnedChip(OwnedChip);

            //TODO ui작동!
            ThrowChip(chairId, bet);
            //TODO ui작동!
            view.roundBetChipObj.SetActive(true);
            view.roundBetChiptext.text = Extension.FormatGold(GetRoundBetChip());
        }

        public void BetChipData(long bet, long OwnedChip)
        {
            badugiPlayerInfo.roundBetChipList.Add(bet);
            SetCurrentOwnedChip(OwnedChip);

            view.roundBetChipObj.SetActive(true);
            view.roundBetChiptext.text = Extension.FormatGold(GetRoundBetChip());
        }

        long GetRoundBetChip()
        {
            long totalBet = 0;
            for (int i = 0; i < badugiPlayerInfo.roundBetChipList.Count; i++)
            {
                totalBet += badugiPlayerInfo.roundBetChipList[i];
            }

            return totalBet;
        }

        void ThrowChip(int chairId, long chip)
        {
            CPPlayer.Badugi.ThrowChip(chairId, chip, view.throwChipStartPos);
        }

        public void AddCard(string card, BadugiCardViewer _viewer)
        {
            badugiPlayerInfo.cardlist.Add(card);
            _viewer.AddListenerForCardTouch(this);
            _viewer.AddListenerForCardRecommendInBadugi(this);
            //_viewer.InitCardSet();
            cardViewerList.Add(_viewer);
        }

        public void GetCard(string card, CardViewer _viewer)
        {
            if (isMe)
            {
                //  Debug.LogError($"{badugiPlayerInfo.cardlist.Count}개 카드 받았을때 호출 됨");
                //카드 다 받은 후 콜백 함수 호출
                if (badugiPlayerInfo.cardlist.Count >= 4)
                {
                    var rank = SetUiAfterCardRecieved(false);
                    BestRankSet(rank);
                }
            }
        }

        public Transform GetPlayerCardTr()
        {
            Transform tr;
            tr = view.myCardPos[badugiPlayerInfo.cardlist.Count];

            return tr;
        }

        public Transform GetPlayerCardTr(int index)
        {
            Transform tr;
            tr = view.myCardPos[index];

            return tr;
        }
        
        public void SetDrawActionText(int count, bool active)
        {
            view.drawActionParentObj.SetActive(active);
            for (int i = 0; i < view.drawActionImgs.Length; i++)
            {
                view.drawActionImgs[i].gameObject.SetActive(false);
            }
            
            view.drawActionImgs[count].gameObject.SetActive(active);
        }

        //bet image active
        public void BetImageActive(bool isactive = false)
        {
            if (badugiPlayerInfo.playerInfo == null)
                return;
            
            if (!isactive)
            {
                view.betActionTypeImageAnimator.enabled = false;
            }
            view.betActionTypeImageParentObj.gameObject.SetActive(isactive);
        }

        public void SetTotalBet(long totalBet)
        {
            currentTotalBet = totalBet;
        }

        public void SetCurrentOwnedChip(long OwnedChip)
        {
            if (badugiPlayerInfo.playerInfo == null)
                return;
            badugiPlayerInfo.playerInfo.Chip = OwnedChip;

            if (isMe)
            {
                CPPlayer.UserInfo.userDatabase.User.Gold = OwnedChip;
            }

            //TODO ui작동!
            view.currentOwnedChip.text = Extension.ToKoreanFormat(badugiPlayerInfo.playerInfo.Chip, Extension.KoreanFormatMode.Planning);
            //TODO ui작동!
        }

        public void SetAction(Partial.ActionType actionType, Partial.BetSizeType badugiactionType, long bet,
            long amount)
        {
            //   ..  actionRes
            ActionEvent(actionType, badugiactionType);
            //   ..  actionRes
            BetChip(bet, amount);
        }

        public void SetActionData(Partial.ActionType actionType, Partial.BetSizeType badugiactionType, long bet,
            long amount)
        {
            //   ..  actionRes
            ActionEvent(actionType, badugiactionType);
            //   ..  actionRes
            BetChipData(bet, amount);
        }

        void ActionEvent(Partial.ActionType actionType, Partial.BetSizeType badugiactionType)
        {
            //TODO ui작동!
            view.betActionTypeImage.sprite = InGameResourcesBundle.Loaded.ingameActionTypeImages_badugi[(int)badugiactionType];
            //TODO ui작동!
        }

        public void RemovePlayer()
        {
            //badugiPlayerInfo.playerInfo = null;

            //플레이어 viewer 세팅
            view.gameObject.SetActive(false);
        }

        public void ActionToDisplay(Partial.BetSizeType actionType)
        {
            view.betActionTypeImage.sprite = InGameResourcesBundle.Loaded.ingameActionTypeImages_badugi[(int)actionType];
            
            ColorUtility.TryParseHtmlString("#" + InGameResourcesBundle.Loaded.ingameActionTypeImageColor[(int)actionType], out Color color);
            var particle0 = view.betActionTypeImageParticle0.main;
            particle0.startColor = color;

            var particle1 = view.betActionTypeImageParticle1.main;
            particle1.startColor = color;
            
            if (view.betActionTypeImageParentObj.gameObject.activeInHierarchy == false)
            {
                view.betActionTypeImageParentObj.gameObject.SetActive(true);
            }

            view.betActionTypeImageAnimator.enabled = true;
            view.betActionTypeImageAnimator.Play("Stamp_rec_effect_animation");
        }

        public void SetEndTurn(bool isMe)
        {
            Extension.eLog($"타임 업데이트 종료!", Color.cyan);
            timeCts?.Cancel();
            timeCts?.Dispose();
            timeCts = null;

            view.SetActivateView(isMe, false);
            if (isMe)
            {
                badugiViewer.timeSlider.fillAmount = 1;
                badugiViewer.timeSliderObj.SetActive(false);
            }

            view.timeCountObj.SetActive(false);
            AudioManager.Instance.Stop(AudioSourceKey.TimeCount);
        }

        public string resultRankString;

        public void SetRankViewer(string rank, List<string> cardRankStringList)
        {
            view.bestCardObj.gameObject.SetActive(true);
            view.bestCardText.text = rank;
            resultRankString = rank;

            CanvasGroup jokboRank = view.bestCardObj;

            float durationtime = (float)CPPlayer.Server.visualEffectTimeConfig["CARD_RANK_MS"] / 1000f;
            jokboRank.alpha = 0f;
            jokboRank.DOFade(1f, durationtime);

            // view.bestCardObj.gameObject.SetActive(true);
            // view.bestCardText.text = rank;
            // resultRankString = rank;
        }

        private bool isTurnActive = false;

        /// <summary>
        /// 턴쪽의 플레이어 활성화 됄때 이펙트 등등 이미지 세팅
        /// </summary>
        public void ActivateTurn(DateTime startTime, bool isMe)
        {
            if (isMe)
            {
                if (CPPlayer.InGame.isUserAFK)
                    return;
            }

            view.SetActivateView(isMe, true);
            if (isMe)
            {
                badugiViewer.timeSlider.fillAmount = 1;
                badugiViewer.timeSliderObj.SetActive(true);
            }

            isTurnActive = true;
            RunTimerAsync(startTime).Forget();
        }

        private async UniTaskVoid RunTimerAsync(DateTime startTime)
        {
            timeCts = new CancellationTokenSource();
            Debug.Log("타이머 시작!");

            bool audioToggle = false;
            int prevRemainSec = -1;
            float speed = 100f;
            float turnTime = (float)CPPlayer.Server.visualEffectTimeConfig["BET_TIMEOUT_MS"] / 1000f;
            try
            {
                float elapsedTime = 0f;

                while (elapsedTime < turnTime)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, timeCts.Token);
                    elapsedTime = (float)(CPPlayer.Badugi.estimatedServerNowUtc - startTime).TotalSeconds;
                    float remaining = turnTime - elapsedTime;

                    badugiViewer.timeSlider.fillAmount = remaining / turnTime;
                    view.turnActiveImage.transform.Rotate(0f, 0f, speed * Time.deltaTime * 3f);

                    if (turnTime - elapsedTime < 3.0f && audioToggle == false)
                    {
                        audioToggle = true;
                        AudioManager.Instance.Play(AudioSourceKey.TimeCount);
                    }

                    int remainSec = Mathf.CeilToInt(remaining);
                    if (remainSec != prevRemainSec)
                    {
                        prevRemainSec = remainSec;
                        if (remainSec <= 3 && remainSec > 0)
                        {
                            if (view.timeCountObj.activeInHierarchy == false)
                            {
                                view.timeCountObj.SetActive(true);
                            }

                            view.timeCountImage.sprite = InGameResourcesBundle.Loaded.TimeCountSprites[remainSec - 1];
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("액션으로 인한 타임 끝 곧 서버 액션노티로 인한 턴 종료");
            }
            finally
            {
                //timeCts.Dispose();
            }
        }

        public async UniTask SetInfoForResultAsync(ResultNoti.Types.Player _resultPlayerInfo, bool isMainPotWin, CancellationToken token)
        {
            if (badugiPlayerInfo.playerInfo == null)
                return;

            float cardOpenEventTime = (float)CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_MS"] / 1000f;

            Extension.eLog($"{_resultPlayerInfo.ChairId} player//card info list:{string.Join(",", _resultPlayerInfo.HoleCards)}", Color.cyan);
            resultPlayerInfo = _resultPlayerInfo;

            //카드 연출 작업
            if (_resultPlayerInfo.HoleCards.Count > 0)
            {
                if (!isMe)
                {
                    int index = 0;
                    for (int i = 0; i < _resultPlayerInfo.HoleCards.Count; i++)
                    {
                        if (string.IsNullOrEmpty(_resultPlayerInfo.HoleCards[i]))
                            continue;
                        badugiPlayerInfo.cardlist[index] = _resultPlayerInfo.HoleCards[i];
                        index++;
                    }
                }

                //카드 이동후 보여주기
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    int index = i;
                    cardViewerList[index].transform.SetParent(view.winnerCardPos[index], true);
                    cardViewerList[index].transform.DOLocalRotate(Vector3.zero, cardOpenEventTime / 2f);
                    cardViewerList[index].transform.DOLocalMove(Vector2.zero, cardOpenEventTime / 2f);
                    cardViewerList[index].transform.DOScale(Vector3.one, cardOpenEventTime / 2f);
                }

                int cardOpenEventTimei = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_MS"];
                await UniTask.Delay(cardOpenEventTimei / 2, cancellationToken: token);

                var tweenTasks = new List<UniTask>();
                var sequences = new List<Sequence>(); // 취소 시 정리용

                string cardrank = SetUiAfterCardRecieved();

                try
                {
                    for (int i = 0; i < cardViewerList.Count; i++)
                    {
                        // 취소 체크
                        token.ThrowIfCancellationRequested();
                        int index = i;

                        if (isFolded)
                        {
                            cardViewerList[index].mask.gameObject.SetActive(true);
                        }

                        Sequence seq = DOTween.Sequence();
                        sequences.Add(seq); // 정리용 리스트에 추가

                        seq.Append(cardViewerList[index].transform.DOScaleX(0, cardOpenEventTime / 4f));
                        seq.AppendCallback(() =>
                        {
                            (int rank, Suit suit) = CardRankCalculater.ParseCard(badugiPlayerInfo.cardlist[index]);
                            List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                            if (CPPlayer.Cloud.optionValue.fourColor)
                            {
                                cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                            }
                            else
                            {
                                cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                            }

                            var CardInfo = cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
                            cardViewerList[index].cardImage.sprite = CardInfo.cardSprite;

                            bool isExist = _resultPlayerInfo.HoleCards.Any(o => o == badugiPlayerInfo.cardlist[index]);
                            if (isExist && !isFolded)
                            {
                                cardViewerList[index].mask.gameObject.SetActive(false);
                            }
                        });
                        seq.Append(cardViewerList[index].transform.DOScaleX(1, cardOpenEventTime / 4f));

                        tweenTasks.Add(seq.ToUniTask(cancellationToken: token));
                    }

                    await UniTask.WhenAll(tweenTasks).AttachExternalCancellation(token);
                }
                catch (Exception e)
                {
                    Debug.Log("애니메이션이 취소되었습니다.");

                    // 실행 중인 모든 Sequence 정리
                    foreach (var seq in sequences)
                    {
                        if (seq != null && seq.IsActive())
                        {
                            seq.Kill(); // 애니메이션 즉시 정지
                        }
                    }
                }

                BestRankSet(cardrank);
            }
        }

        public async UniTask SetInfoForResultForFoldUserAsync(ResultNoti.Types.Player _resultPlayerInfo, bool isMainPotWin, CancellationToken token)
        {
            if (badugiPlayerInfo.playerInfo == null)
                return;
            if (isFolded)
                return;

            float cardOpenEventTime = (float)CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_MS"] / 1000f;

            Extension.eLog($"{_resultPlayerInfo.ChairId} player//card info list:{string.Join(",", _resultPlayerInfo.HoleCards)}", Color.cyan);
            resultPlayerInfo = _resultPlayerInfo;

            //카드 연출 작업
            if (_resultPlayerInfo.HoleCards.Count > 0)
            {
                if (!isMe)
                {
                    int index = 0;
                    for (int i = 0; i < _resultPlayerInfo.HoleCards.Count; i++)
                    {
                        if (string.IsNullOrEmpty(_resultPlayerInfo.HoleCards[i]))
                            continue;
                        badugiPlayerInfo.cardlist[index] = _resultPlayerInfo.HoleCards[i];
                        index++;
                    }
                }

                //카드 이동후 보여주기
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    int index = i;
                    cardViewerList[index].transform.SetParent(view.winnerCardPos[index], true);

                    cardViewerList[index].transform.DOLocalRotate(Vector3.zero, cardOpenEventTime / 2f);
                    cardViewerList[index].transform.DOLocalMove(Vector2.zero, cardOpenEventTime / 2f);
                    cardViewerList[index].transform.DOScale(Vector3.one, cardOpenEventTime / 2f);
                }

                int cardOpenEventTimei = (int)CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_MS"];
                await UniTask.Delay(cardOpenEventTimei / 2, cancellationToken: token);

                var tweenTasks = new List<UniTask>();
                var sequences = new List<Sequence>(); // 취소 시 정리용

                string cardrank = SetUiAfterCardRecieved();

                try
                {
                    for (int i = 0; i < cardViewerList.Count; i++)
                    {
                        // 취소 체크
                        token.ThrowIfCancellationRequested();

                        int index = i;

                        Sequence seq = DOTween.Sequence();
                        sequences.Add(seq); // 정리용 리스트에 추가

                        seq.Append(cardViewerList[index].transform.DOScaleX(0, cardOpenEventTime / 4f));
                        seq.AppendCallback(() =>
                        {
                            (int rank, Suit suit) = CardRankCalculater.ParseCard(badugiPlayerInfo.cardlist[index]);
                            List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                            if (CPPlayer.Cloud.optionValue.fourColor)
                            {
                                cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                            }
                            else
                            {
                                cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                            }

                            var CardInfo = cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
                            cardViewerList[index].cardImage.sprite = CardInfo.cardSprite;

                            bool isExist = _resultPlayerInfo.HoleCards.Any(o => o == badugiPlayerInfo.cardlist[index]);
                            if (isExist)
                            {
                                cardViewerList[index].mask.gameObject.SetActive(false);
                            }
                        });
                        seq.Append(cardViewerList[index].transform.DOScaleX(1, cardOpenEventTime / 4f));

                        tweenTasks.Add(seq.ToUniTask(cancellationToken: token));
                    }

                    await UniTask.WhenAll(tweenTasks).AttachExternalCancellation(token);
                }
                catch (Exception e)
                {
                    Debug.Log("애니메이션이 취소되었습니다.");

                    // 실행 중인 모든 Sequence 정리
                    foreach (var seq in sequences)
                    {
                        if (seq != null && seq.IsActive())
                        {
                            seq.Kill(); // 애니메이션 즉시 정지
                        }
                    }
                }

                BestRankSet(cardrank);
            }
        }


        public void SetInfoForResult(ResultNoti.Types.Player _resultPlayerInfo, bool isMainPotWin)
        {
            if (badugiPlayerInfo.playerInfo == null)
                return;

            Extension.eLog($"{_resultPlayerInfo.ChairId} player//card info list:{string.Join(",", _resultPlayerInfo.HoleCards)}", Color.cyan);
            resultPlayerInfo = _resultPlayerInfo;

            //카드 연출 작업
            if (_resultPlayerInfo.HoleCards.Count > 0)
            {
                if (!isMe)
                {
                    int index = 0;
                    for (int i = 0; i < _resultPlayerInfo.HoleCards.Count; i++)
                    {
                        if (string.IsNullOrEmpty(_resultPlayerInfo.HoleCards[i]))
                            continue;
                        badugiPlayerInfo.cardlist[index] = _resultPlayerInfo.HoleCards[i];
                        index++;
                    }
                }


                //카드 이동후 보여주기
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    int index = i;
                    cardViewerList[index].transform.SetParent(view.winnerCardPos[index], true);

                    cardViewerList[index].transform.rotation = Quaternion.identity;
                    cardViewerList[index].transform.localScale = Vector3.one;
                    cardViewerList[index].transform.localPosition = Vector3.zero;
                }

                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    int index = i;

                    (int rank, Suit suit) = CardRankCalculater.ParseCard(badugiPlayerInfo.cardlist[index]);
                    List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    if (CPPlayer.Cloud.optionValue.fourColor)
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    }
                    else
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                    }

                    var CardInfo = cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    cardViewerList[index].cardImage.sprite = CardInfo.cardSprite;
                }

                var cardRank = SetUiAfterCardRecieved();
                BestRankSet(cardRank);
            }

            if (!isMainPotWin)
            {
                view.inActiveMask.SetActive(true);
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    cardViewerList[i].mask.gameObject.SetActive(true);
                }
            }
        }

        public void CardOpenBtnObjActive(bool isActive)
        {
            view.cardOpenBtn.gameObject.SetActive(isActive);
            view.cardCloseBtn.gameObject.SetActive(isActive);

            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(isActive);
        }

        public async UniTask OpenFoldUserCards(List<string> cardsInfo)
        {
            if (badugiPlayerInfo.playerInfo == null)
                return;

            float openCardTime = (float)CPPlayer.Server.visualEffectTimeConfig["OPEN_ALL_MS"] / 1000f;
            //카드 연출 작업
            if (!isMe)
            {
                int index = 0;
                Debug.Log($"기존 카드리스트:{string.Join(",",badugiPlayerInfo.cardlist )}//서버 카드 리스트:{string.Join(",",cardsInfo)}");
                badugiPlayerInfo.cardlist = cardsInfo;
                // for (int i = 0; i < cardsInfo.Count; i++)
                // {
                //     if (string.IsNullOrEmpty(cardsInfo[i]))
                //         continue;
                //     badugiPlayerInfo.cardlist[index] = cardsInfo[i];
                //     index++;
                // }
            }


            //카드 이동후 보여주기
            for (int i = 0; i < cardViewerList.Count; i++)
            {
                int index = i;
                if (string.IsNullOrEmpty(badugiPlayerInfo.cardlist[i]))
                    continue;
                cardViewerList[index].transform.SetParent(view.winnerCardPos[index], true);

                cardViewerList[index].transform.DOLocalRotate(Vector3.zero, openCardTime / 2f);
                cardViewerList[index].transform.DOLocalMove(Vector2.zero, openCardTime / 2f);
                cardViewerList[index].transform.DOScale(Vector3.one, openCardTime / 2f);
            }

            int delayms = (int)CPPlayer.Server.visualEffectTimeConfig["OPEN_ALL_MS"] / 2;
            await UniTask.Delay(delayms);

            var tweenTasks = new List<UniTask>();
            string cardrank = SetUiAfterCardRecieved();
            for (int i = 0; i < cardViewerList.Count; i++)
            {
                if (string.IsNullOrEmpty(badugiPlayerInfo.cardlist[i]))
                    continue;
                int index = i;
                if (isFolded)
                {
                    cardViewerList[index].mask.gameObject.SetActive(true);
                }

                Sequence seq = DOTween.Sequence();
                seq.Append(cardViewerList[index].transform.DOScaleX(0, openCardTime / 4f));
                seq.AppendCallback(() =>
                {
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(badugiPlayerInfo.cardlist[index]);
                    List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    if (CPPlayer.Cloud.optionValue.fourColor)
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    }
                    else
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                    }

                    var CardInfo = cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    cardViewerList[index].cardImage.sprite = CardInfo.cardSprite;
                });
                seq.Append(cardViewerList[index].transform.DOScaleX(1, openCardTime / 4f));
                seq.OnComplete(() =>
                {
                    bool isExist = cardsInfo.Any(o => o == badugiPlayerInfo.cardlist[index]);
                    if (isExist && !isFolded)
                    {
                        cardViewerList[index].mask.gameObject.SetActive(false);
                    }

                    //cardViewerList[index].mask.gameObject.SetActive(false);
                });
                tweenTasks.Add(seq.AsyncWaitForCompletion().AsUniTask());
            }

            await UniTask.WhenAll(tweenTasks);
            BestRankSet(cardrank);
        }

        public string SetUiAfterCardRecieved(bool isShowDown = false)
        {
            bool isCardOpenAll = false;
            for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
            {
                if (string.IsNullOrEmpty(badugiPlayerInfo.cardlist[i]))
                {
                    isCardOpenAll = false;
                    break;
                }
                else
                {
                    isCardOpenAll = true;
                }
            }

            if (isCardOpenAll == false)
                return null;

            List<int> mycardIndexList = new List<int>();
            for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
            {
                int rankindex = CardRankCalculater.GetCardIndex(badugiPlayerInfo.cardlist[i]);
                mycardIndexList.Add(rankindex);
                Extension.eLog($"string value:{badugiPlayerInfo.cardlist[i]}//rankindex:{rankindex}", Color.yellow);
            }

            List<int> allCards = new List<int>(mycardIndexList);

            (string cardRankString, var cardValueList) = CardRankCalculater.EvaluateBadugiHand(allCards);
            Extension.eLog($"cardRank!{cardRankString}//{string.Join(",", cardValueList)}", Color.cyan);

            List<string> cardrankStringList = new List<string>();
            for (int i = 0; i < cardValueList.Count; i++)
            {
                cardrankStringList.Add(CardRankCalculater.GetCardString(cardValueList[i]));
            }

            //카드 정렬 로직
            int mod = 13;
            sortedindex = allCards.Select((v, i) => new { Value = (v % mod) + 1 == 13 ? 0 : (v % mod) + 1, Index = i })
                .OrderBy(x => x.Value)
                .Select(x => x.Index)
                .ToList();
            Extension.eLog($"정렬!!!!{string.Join(",", badugiPlayerInfo.cardlist)}//{string.Join(",", mycardIndexList)}", Color.cyan);
            Extension.eLog($"정렬!!!!{string.Join(",", allCards)}//{string.Join(",", sortedindex)}", Color.green);

            sortedCardList = sortedindex.Select(i => badugiPlayerInfo.cardlist[i]).ToList();
            sortedCardViewerList = sortedindex.Select(i => cardViewerList[i]).ToList();

            Transform[] children = new Transform[view.myCardPos.Length];
            for (int i = 0; i < view.myCardPos.Length; i++)
            {
                children[i] = view.myCardPos[i];
            }

            for (int newPos = 0; newPos < sortedindex.Count; newPos++)
            {
                int originalIndex = sortedindex[newPos]; // 원래 자식 인덱스
                Transform child = children[originalIndex];

                child.SetSiblingIndex(newPos);
                child.GetComponent<RectTransform>().anchoredPosition = view.cardPositions[newPos];
            }

            //카드 정렬 로직
            for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
            {
                int index = i;
                bool isExist = cardrankStringList.Any(o => o == badugiPlayerInfo.cardlist[index]);
                if (!isExist)
                {
                    if ((int)CPPlayer.Badugi.currentBadugiState < (int)BadugiState.Evening)
                    {
                        HighlightCardforChange(mycardIndexList[index], true);
                    }
                    else
                    {
                        HighlightCardforChange(mycardIndexList[index], false);
                    }
                }
            }

            bool isEventPlay = view.bestCardObj.gameObject.activeInHierarchy == false;
            CanvasGroup jokboRank = view.bestCardObj;
            view.bestCardObj.gameObject.SetActive(true);
            float durationtime = (float)CPPlayer.Server.visualEffectTimeConfig["CARD_RANK_MS"] / 1000f;
            
            if (isEventPlay)
            {
                jokboRank.GetComponent<Animator>().Play("JokboLabel_PunchScale_2",-1,0f);
                // jokboRank.alpha = 0f;
                // jokboRank.DOFade(1f, durationtime);
            }
          
          

            return cardRankString;
        }

        public void BestRankSet(string rank)
        {
            view.bestCardObj.gameObject.SetActive(true);
            view.bestCardText.text = rank;
            resultRankString = rank;
        }

        public void SetResultInfo(ResultNoti.Types.Player _resultPlayerInfo, bool isMainPotWin)
        {
            view.roundBetChipObj.SetActive(false);
            view.betActionTypeImageParentObj.gameObject.SetActive(false);

            view.winChipObject.SetActive(isMainPotWin);
            view.loseObject.SetActive(!isMainPotWin);
            long realWinAmount = _resultPlayerInfo.Win - _resultPlayerInfo.Fee;
            var winAmount = Extension.ToKoreanFormat(realWinAmount);
            long realLoseAmount = _resultPlayerInfo.Chip - _resultPlayerInfo.Chip0;
            string loseAmount = Extension.ToKoreanFormat(realLoseAmount);
            //승리
            if (isMainPotWin)
            {
                view.winChipAmount.text = $"+{winAmount}";
            }
            else //패배
            {
                if (_resultPlayerInfo.Fee > 0)
                {
                    if (realWinAmount > 0)
                    {
                        view.loseChipAmount.text = $"+{winAmount}";
                    }
                    else
                    {
                        view.loseChipAmount.text = $"{winAmount}";
                    }
                }
                else
                {
                    view.loseChipAmount.text = $"{loseAmount}";
                }

                if (isAllin && _resultPlayerInfo.Chip <= 0)
                {
                    view.allinObject.SetActive(true);
                    view.allinAnimator.Play("All_In_effect",-1,0f);
                }

                view.currentOwnedChip.gameObject.SetActive(false);
            }

            view.currentOwnedChip.text = Extension.ToKoreanFormat(_resultPlayerInfo.Chip, Extension.KoreanFormatMode.Planning);
        }


        public void SetWinnerUI(bool isWinner)
        {
            if (isWinner)
            {
                view.winFontImageObj.SetActive(true);
            }
        }

        async UniTask OpenCardReserve()
        {
            isCardOpenReserved = true;

            string cardString = "";

            List<string> tempcardIndexList = new List<string>();
            if (touchedCardIndexforOpenList.Count > 0)
            {
            }
            else
            {
                for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
                {
                    int index = i;
                    int cardRankIndex = CardRankCalculater.GetCardIndex(badugiPlayerInfo.cardlist[i]);

                    tempcardIndexList.Add(badugiPlayerInfo.cardlist[i]);
                    cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, true);
                }

                touchedCardIndexforOpenList = sortedindex.Select(i => tempcardIndexList[i]).ToList();
            }


            if (currentPhase != BadugiState.ShowDown && currentPhase != BadugiState.Result)
            {
                view.cardOpenBtn.gameObject.SetActive(false);
                view.cardCloseBtn.gameObject.SetActive(true);
            }
            else
            {
                view.cardOpenBtn.gameObject.SetActive(false);
                view.cardCloseBtn.gameObject.SetActive(false);
            }

            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(false);

            PostOpenCardlistInfoAsync().Forget();
        }

        async UniTask CloseCardReserve()
        {
            isCardOpenReserved = false;

            string cardString = "";
            touchedCardIndexforOpenList.Clear();
            for (int i = 0; i < badugiPlayerInfo.cardlist.Count; i++)
            {
                int index = i;
                int cardRankIndex = CardRankCalculater.GetCardIndex(badugiPlayerInfo.cardlist[i]);

                cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, false);
            }

            view.cardOpenBtn.gameObject.SetActive(true);
            view.cardCloseBtn.gameObject.SetActive(false);

            PostOpenCardlistInfoAsync().Forget();
        }


        public void KickVoteRecieveEvent(int count)
        {
            view.kickvoteCanvasGroup.gameObject.SetActive(true);
            view.kickvoteCanvasGroup.alpha = 1;
            view.kickvoteText.text = count.ToString();

            KickVoteEventAnim().Forget();
        }

        async UniTask KickVoteEventAnim()
        {
            int kickUiDissapearTime = (int)CPPlayer.Server.visualEffectTimeConfig["VOTE_SHOW_MS"];
            await UniTask.Delay(kickUiDissapearTime);
            view.kickvoteCanvasGroup.DOFade(0, 1.0f).OnComplete(() => { view.kickvoteCanvasGroup.gameObject.SetActive(false); }
            );
        }

        public void ReserveOut(bool isReserveOut)
        {
            view.reservedOut.SetActive(isReserveOut);
        }

        public void EmoticonExpress(EmotionInfo emotionInfo)
        {
            view.emotionObj.SetActive(true);
            EmoticonExpressAsync(emotionInfo).Forget();
        }

        private async UniTaskVoid EmoticonExpressAsync(EmotionInfo emotionInfo)
        {
            EmoticonExpressCancel();
            view.emotionObj.SetActive(true);
            emotCts = new CancellationTokenSource();
            int index = 0;
            float frameInterval = 0.05f;
            try
            {
                while (true)
                {
                    view.emotionImage.sprite = emotionInfo.sprites[index];
                    await UniTask.Delay(TimeSpan.FromSeconds(frameInterval), cancellationToken: emotCts.Token);
                    index++;
                    if (index >= emotionInfo.sprites.Length)
                        break;
                }

                view.emotionObj.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.Log("이모티콘 강제 종료");
            }
        }

        private void EmoticonExpressCancel()
        {
            emotCts?.Cancel();
            emotCts?.Dispose();
            emotCts = null;
            view.emotionObj.SetActive(false);
        }
    }
}