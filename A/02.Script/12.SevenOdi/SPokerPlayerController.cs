using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlackTree.Bundles;
using CAPYBARA.Core;
using CAPYBARA.sevenPoker;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

namespace CAPYBARA
{
    public class SPokerPlayerInfo
    {
        public sevenPoker.Player playerInfo;
        public List<long> roundBetChipList = new List<long>();
        public List<string> cardlist;
    }

    public class SPokerPlayerController
    {
        public SPokerPlayerView view;
        public ResultNoti.Types.Player resultPlayerInfo;

        [Header("myInfo")] SPokerViewer spokerViewer;
        public SPokerPlayerInfo spokerPlayerInfo;
        public bool isMe;
        public int chairId;

        public List<SPokerCardViewer> cardViewerList = new List<SPokerCardViewer>();
        public List<string> touchedCardIndexforOpenList = new List<string>();
        private CancellationTokenSource timeCts;
        private CancellationTokenSource emotCts;

        public bool isFolded;
        public bool isObserving;
        public bool isAllin;
        public SPokerState currentPhase;
        public bool isCardOpenReserved;
        public long currentTotalBet;

        private IInGameController inGameController;
        
        public long GetTotalBet
        {
            get { return currentTotalBet; }
        }

        public int chairIndexId
        {
            get
            {
                int indexId = chairId - CPPlayer.SPoker.gapBetweenChairIdAndIndex;
                if (indexId < 0)
                {
                    indexId = indexId + Constraints.MaxSpokerPlayerCount;
                }

                return indexId;
            }
        }

        public SPokerPlayerController(Transform viewParent, SPokerViewer spokeriviewer,IInGameController controller)
        {
            spokerViewer = spokeriviewer;
            inGameController=controller;
            
            spokerPlayerInfo = new SPokerPlayerInfo();


            CPPlayer.Option.FourCardModeChange += CardColorModeChange;
           // CPPlayer.InGame.CardTouchCallbackforCardOpen += CardTouchedAfterFold;
            
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
            spokerPlayerInfo = null;

            isMe = false;
            CPPlayer.Option.FourCardModeChange -= CardColorModeChange;
            //CPPlayer.InGame.CardTouchCallbackforCardOpen -= CardTouchedAfterFold;

            cardViewerList.Clear();
        }

        private void CardColorModeChange(bool isFourColor)
        {
            if (isFourColor)
            {
                for (int i = 0; i < spokerPlayerInfo.cardlist.Count; i++)
                {
                    if (string.IsNullOrEmpty(spokerPlayerInfo.cardlist[i]))
                        continue;
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(spokerPlayerInfo.cardlist[i]);
                    var CardInfo = InGameResourcesBundle.Loaded.cardResourceList.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    cardViewerList[i].cardImage.sprite = CardInfo.cardSprite;
                }
            }
            else
            {
                for (int i = 0; i < spokerPlayerInfo.cardlist.Count; i++)
                {
                    if (string.IsNullOrEmpty(spokerPlayerInfo.cardlist[i]))
                        continue;
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(spokerPlayerInfo.cardlist[i]);
                    var CardInfo = InGameResourcesBundle.Loaded.cardResourceList_TwoColor.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    cardViewerList[i].cardImage.sprite = CardInfo.cardSprite;
                }
            }
        }

        public void SetPlayer(sevenPoker.Player _player, int _chairId, bool isme = false)
        {
            spokerPlayerInfo.playerInfo = _player;
            isMe = isme;
            chairId = _chairId;

            spokerPlayerInfo.roundBetChipList.Clear();
            spokerPlayerInfo.cardlist = new List<string>();

            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }

            cardViewerList.Clear();

            view = spokerViewer.playerViewList[chairIndexId];
            view.Init(spokerPlayerInfo.playerInfo);

            var avatarBundle = ItemBundle.Loaded;
            if (string.IsNullOrEmpty( spokerPlayerInfo.playerInfo.Avatar.Id))
            {
                view.playerImage.sprite = avatarBundle.GetAvatarInGameIcon("AVATAR_1");    
                view.inactivemaskImage.sprite = avatarBundle.GetAvatarInGameIcon("AVATAR_1");    
            }
            else
            {
                view.playerImage.sprite = avatarBundle.GetAvatarInGameIcon( spokerPlayerInfo.playerInfo.Avatar.Id);
                view.inactivemaskImage.sprite = avatarBundle.GetAvatarInGameIcon( spokerPlayerInfo.playerInfo.Avatar.Id);
            }

            view.cardOpenBtn.onClick.RemoveAllListeners();
            view.cardCloseBtn.onClick.RemoveAllListeners();
            view.cardOpenBtnAtforfeitWin.onClick.RemoveAllListeners();
            
            view.cardOpenBtn.onClick.AddListener(() => OpenCardReserve().Forget());
            view.cardCloseBtn.onClick.AddListener(() => CloseCardReserve().Forget());
            view.cardOpenBtnAtforfeitWin.onClick.AddListener(() => OpenCardReserve().Forget());
            
            InfoModalInactive();

            view.cardOpenBtn.gameObject.SetActive(false);
            view.cardCloseBtn.gameObject.SetActive(false);
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(false);
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
                        spokerViewer.OnModalAutoClose(view.mePlayerInfoModal.gameObject);    
                    }
                    else
                    {
                        view.mePlayerInfoModal.Set_OpenWindow(spokerPlayerInfo.playerInfo);
                        spokerViewer.OpenModalObject(view.mePlayerInfoModal.gameObject);    
                    }
                }
                else
                {
             
                    bool isactive= view.otherPlayerInfoModal.gameObject.activeInHierarchy;
                    if (isactive)
                    {
                        spokerViewer.OnModalAutoClose(view.otherPlayerInfoModal.gameObject);    
                    }
                    else
                    {
                        view.otherPlayerInfoModal.Set_OpenWindow(spokerPlayerInfo.playerInfo);
                        spokerViewer.OpenModalObject(view.otherPlayerInfoModal.gameObject);
                        view.transform.parent.SetAsLastSibling();
                    }
                }
                
            });
            spokerPlayerInfo.cardlist = new List<string>();
            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }

            cardViewerList.Clear();

            // for (int i = 0; i < spokerPlayerInfo.cardlist.Count; i++)
            // {
            //     spokerPlayerInfo.cardlist.Add(_player.ca);    
            // }
            
            view.ViewSetting();
            view.cardOpenBtn.gameObject.SetActive(false);
            view.cardCloseBtn.gameObject.SetActive(false);

            bool inactiveMaskOn = _player.IsObserving || _player.IsFolded;
            isObserving = _player.IsObserving;
            view.inActiveMask.SetActive(inactiveMaskOn);
            SetCurrentOwnedChip(_player.Chip);
        }

        public bool isForfeitWin = false;
        public void SetCardOpenAtForfeitWin()
        {
            isForfeitWin = true;
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(true);
        }

        
        public void SetCurrentPhase(SPokerState _currentPhase)
        {
            currentPhase = _currentPhase;
           
        }

        public void SetCardOpenBtn()
        {
            if (currentPhase != SPokerState.End)
            {
                if (currentPhase != SPokerState.ShowDown && currentPhase != SPokerState.Result)
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
                if (currentPhase != SPokerState.ShowDown && currentPhase != SPokerState.End && currentPhase != SPokerState.Result)
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
            }

            if (isFolded)
            {
                //card setting
                CardAnimationAtFold().Forget();
                
                float dieDim = (float)CPPlayer.Server.visualEffectTimeConfig["DIE_ME_DIM_MS"]/1000f;
            
                view.inActiveMask.SetActive(true);
                var c =  view.inactivemaskImage.color;
                c.a = 0f;
                view.inactivemaskImage.color = c;
                
                view.inactivemaskImage.DOFade(0.5f, dieDim);
            }

        }

        async UniTask CardAnimationAtFold()
        {
            if (!isMe)
            {
                float highlightDownTime=0.4f;
                if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("DIE_OTHER_DIM_MS"))
                {
                    highlightDownTime= (float)CPPlayer.Server.visualEffectTimeConfig["DIE_OTHER_DIM_MS"]/1000f; 
                }
                
                foreach (var CardViewer in cardViewerList)
                {
                    CardViewer.hideCardImage.gameObject.SetActive(false);
                }

                Sequence seq = DOTween.Sequence();
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    if (i == 0 || i == 1)
                        continue;
                    if (cardViewerList.Count == Constraints.SevenOdiMaxCardCount && i == cardViewerList.Count - 1)
                        continue;
                    var cardViewer = cardViewerList[i];
                    seq.Join(cardViewer.transform.DOScaleX(0f, highlightDownTime/2f));
                }

                seq.AppendCallback(() =>
                {
                    var sprite = InGameResourcesBundle.Loaded.noneImageForCard;
                    for (int i = 0; i < cardViewerList.Count; i++)
                        cardViewerList[i].cardImage.sprite = sprite;
                });
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    if (i == 0 || i == 1)
                        continue;
                    if (cardViewerList.Count == Constraints.SevenOdiMaxCardCount && i == cardViewerList.Count - 1)
                        continue;
                    var cardViewer = cardViewerList[i];
                    seq.Join(cardViewer.transform.DOScaleX(1f, highlightDownTime/2f));
                }
                
                foreach (var CardViewer in cardViewerList)
                {
                    CardViewer.InactiveSelectEffectAtFold(highlightDownTime);
                    CardViewer.SetMaskFade();
                }
            }
            else
            {
                float highlightDownTime=0.3f;
                if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("DIE_ME_DIM_MS"))
                {
                    highlightDownTime= (float)CPPlayer.Server.visualEffectTimeConfig["DIE_ME_DIM_MS"]/1000f; 
                }
                foreach (var CardViewer in cardViewerList)
                {
                    CardViewer.hideCardImage.gameObject.SetActive(false);
                    CardViewer.InactiveSelectEffectAtFold(highlightDownTime);
                    CardViewer.SetMaskFade();
                }
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

        public void InitializePlayerData()
        {
            spokerPlayerInfo.cardlist.Clear();
            touchedCardIndexforOpenList.Clear();

            isAllin = false;
            isFolded = false;
            isCardOpenReserved = false;
            currentPhase = SPokerState.End;
            resultRankString = null;

            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }

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
            view.dotLoadingObj.SetActive(false);

            view.cardOpenBtn.gameObject.SetActive(false);
            view.cardCloseBtn.gameObject.SetActive(false);
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(false);
            
            view.readyCompleteObj.SetActive(false);

            view.inActiveMask.SetActive(isFolded);
        }

        public void StartSet()
        {
            spokerPlayerInfo.cardlist.Clear();

            foreach (var cardViewer in cardViewerList)
            {
                cardViewer.Inactive();
                PoolManager.Push(cardViewer);
            }

            cardViewerList.Clear();
            view.bestCardObj.gameObject.SetActive(false);
            view.betActionTypeImageParentObj.gameObject.SetActive(false);
            view.winChipObject.SetActive(false);
            view.winFontImageObj.SetActive(false);
            view.loseObject.SetActive(false);
            view.currentOwnedChip.gameObject.SetActive(true);
            view.readyCompleteObj.SetActive(false);
            view.allinObject.SetActive(false);
            view.dotLoadingObj.SetActive(false);
        }

        public void ClearCurrentRoundBetHistory()
        {
            spokerPlayerInfo.roundBetChipList.Clear();
            view.roundBetChipObj.SetActive(false);
        }

        public void AddBetThisRound(long bet)
        {
            spokerPlayerInfo.roundBetChipList.Add(bet);
        }

        public void BetAnte(long ante, long OwnedChip)
        {
            spokerPlayerInfo.roundBetChipList.Add(ante);
            SetCurrentOwnedChip(OwnedChip);

            //TODO ui작동!
            ThrowAnte(ante);
            //TODO ui작동!
        }

        void ThrowAnte(long ante)
        {
            CPPlayer.SPoker.ThrowAnte(chairId, ante, view.throwChipStartPos);
        }
        public void LocateAnteSnapShot(int chairId, long ante, Transform startPos)
        {
            //anteobject는 어차피 사라지므로 아무것도 안해도 됨
        }

        public void BetChip(long bet, long OwnedChip)
        {
            spokerPlayerInfo.roundBetChipList.Add(bet);
            SetCurrentOwnedChip(OwnedChip);

            //TODO ui작동!
            ThrowChip(chairId, bet);
            //TODO ui작동!
            view.roundBetChipObj.SetActive(true);
            view.roundBetChiptext.text = Extension.ToKoreanFormat(GetRoundBetChip());
        }

        long GetRoundBetChip()
        {
            long totalBet = 0;
            for (int i = 0; i < spokerPlayerInfo.roundBetChipList.Count; i++)
            {
                totalBet += spokerPlayerInfo.roundBetChipList[i];
            }

            return totalBet;
        }

        void ThrowChip(int chairId, long chip)
        {
            CPPlayer.SPoker.ThrowChip(chairId, chip, view.throwChipStartPos);
        }

        public void AddCard(string card, SPokerCardViewer _viewer)
        {
            spokerPlayerInfo.cardlist.Add(card);
            cardViewerList.Add(_viewer);
            _viewer.InitCardSet();
            
            Extension.eLog($"체어아이디:{chairId}//가진 카드:{string.Join(",",spokerPlayerInfo.cardlist)}//카드뷰 카운트:{cardViewerList.Count}",Color.bisque);
        }

        //card select setting(remove and hideimage active)
        public void SetCardAfterSelectState(int dropcardIndex, string opencardStr)
        {
            spokerPlayerInfo.cardlist.RemoveAt(dropcardIndex);
            cardViewerList.RemoveAt(dropcardIndex);

            int opencardIndex = spokerPlayerInfo.cardlist.IndexOf(opencardStr);
            if (opencardIndex < 0||opencardIndex >= spokerPlayerInfo.cardlist.Count)
            {
                opencardIndex = spokerPlayerInfo.cardlist.Count-1;
            }
            
            
            

            string cardStr = spokerPlayerInfo.cardlist[opencardIndex];
            var cardView = cardViewerList[opencardIndex];

            spokerPlayerInfo.cardlist.RemoveAt(opencardIndex);
            cardViewerList.RemoveAt(opencardIndex);

            if (string.IsNullOrEmpty(cardStr))
            {
                spokerPlayerInfo.cardlist.Add(opencardStr);
            }
            else
            {
                spokerPlayerInfo.cardlist.Add(cardStr);
            }

            cardViewerList.Add(cardView);

            Transform[] children = new Transform[view.myCardPos.Length];
            for (int i = 0; i < view.myCardPos.Length; i++)
            {
                children[i] = view.myCardPos[i];
            }

            for (int newPos = 0; newPos < cardViewerList.Count; newPos++)
            {
                Transform child = children[newPos];

                cardViewerList[newPos].transform.SetParent(child, false);
                cardViewerList[newPos].transform.localPosition = Vector3.zero;
                cardViewerList[newPos].transform.localRotation = Quaternion.identity;
                cardViewerList[newPos].transform.localScale = Vector3.one;
            }

            if (isMe)
            {
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    int index = i;
                    cardViewerList[i].SetCardHide(index < 2);
                }
            }
        }

        public void SetCardImageAfterAllSelect()
        {
            float fadeTime = (float)CPPlayer.Server.visualEffectTimeConfig["OPEN_ALL_MS"]/1000f;
            for (int i = 0; i < spokerPlayerInfo.cardlist.Count; i++)
            {
                string cardString = spokerPlayerInfo.cardlist[i];
                if (string.IsNullOrEmpty(cardString))
                    continue;
                (int _rank, Suit _suit) = CardRankCalculater.ParseCard(cardString);
                List<CardInfo> _cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                if (CPPlayer.Cloud.optionValue.fourColor)
                {
                    _cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                }
                else
                {
                    _cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                }

                var _CardInfo = _cardlist.Find(o => o.cardSuit == _suit && o.rankValue == _rank);

                int index = i;
                if (index < 2)
                {
                    cardViewerList[i].cardImage.sprite = _CardInfo.cardSprite;
                    var c = cardViewerList[i].cardImage.color;
                    c.a = 0f;
                    cardViewerList[i].cardImage.color = c;
                    cardViewerList[i].cardImage.DOFade(1f, fadeTime);
                }
                else
                {
                    var c = cardViewerList[i].cardImage.color;
                    c.a = 1f;
                    cardViewerList[i].cardImage.color = c;

                    Sequence seq = DOTween.Sequence();
                    seq.Append(cardViewerList[index].transform.DOScaleX(0, fadeTime/2f));
                    seq.AppendCallback(() => { cardViewerList[index].cardImage.sprite = _CardInfo.cardSprite; });
                    seq.Append(cardViewerList[index].transform.DOScaleX(1, fadeTime/2f));
                }
            }
        }

        //카드 받고 족보 호출하기 위함
        public void GetCard(string card, CardViewer _viewer)
        {
            if (isMe)
            {
                Extension.eLog($"currentCardCount:{spokerPlayerInfo.cardlist.Count}", Color.magenta);
                if (spokerPlayerInfo.cardlist.Count >= 4)
                {
                    CPPlayer.SPoker.CardRecieved?.Invoke(this, false);
                }
            }
        }

        public Transform GetPlayerCardTr()
        {
            Transform tr;
            tr = view.myCardPos[spokerPlayerInfo.cardlist.Count];

            return tr;
        }

        public Transform GetPlayerCardTr(int index)
        {
            Transform tr;
            tr = view.myCardPos[index];

            return tr;
        }

        //bet image active
        public void BetImageActive(bool isactive = false)
        {
            if (spokerPlayerInfo.playerInfo == null)
                return;
            view.betActionTypeImageParentObj.gameObject.SetActive(isactive);
        }

        public void SetTotalBet(long totalBet)
        {
            currentTotalBet = totalBet;
        }

        public void SetCurrentOwnedChip(long OwnedChip)
        {
            if (spokerPlayerInfo.playerInfo == null)
                return;
            spokerPlayerInfo.playerInfo.Chip = OwnedChip;

            if (isMe)
            {
                CPPlayer.UserInfo.userDatabase.User.Gold = OwnedChip;
            }


            //TODO ui작동!
            view.currentOwnedChip.text = Extension.ToKoreanFormat(spokerPlayerInfo.playerInfo.Chip, Extension.KoreanFormatMode.Planning);
            //TODO ui작동!
        }

        public void SetAction(Partial.ActionType actionType, Partial.BetSizeType _betactionType, long bet,
            long amount)
        {
            //   ..  actionRes
            ActionEvent(actionType, _betactionType);
            //   ..  actionRes
            BetChip(bet, amount);
        }

        void ActionEvent(Partial.ActionType actionType, Partial.BetSizeType _betactionType)
        {
            //TODO ui작동!
            view.betActionTypeImage.sprite = InGameResourcesBundle.Loaded.ingameActionTypeImages_badugi[(int)_betactionType];
            //TODO ui작동!
        }

        public void RemovePlayer()
        {
            spokerPlayerInfo.playerInfo = null;

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
            view.betActionTypeImageAnimator.Play("Stamp_rec_effect_animation");
        }

        public void SetEndTurn(bool isMe)
        {
            timeCts?.Cancel();
            timeCts?.Dispose();
            timeCts = null;

            view.SetActivateView(isMe, false);
            if (isMe)
            {
                spokerViewer.timeSlider.fillAmount = 0;
                spokerViewer.timeSliderObj.SetActive(false);
            }

            view.timeCountObj.SetActive(false);
            AudioManager.Instance.Stop(AudioSourceKey.TimeCount);
        }

        public string resultRankString;

        public void SetRankViewer(string rank, List<string> cardRankStringList)
        {
            bool isEventPlay = view.bestCardObj.gameObject.activeInHierarchy == false;
            
            bool isDifferentRank = resultRankString != rank;
            // if (isDifferentRank)
            //     isEventPlay = true;
            
            view.bestCardText.text = rank;
            resultRankString = rank;
            
            CanvasGroup jokboRank= view.bestCardObj;
            view.bestCardObj.gameObject.SetActive(true);

            if (isEventPlay)
            {
                float durationtime = (float)CPPlayer.Server.visualEffectTimeConfig["CARD_RANK_MS"]/1000f;
            
                jokboRank.alpha = 0f;
                jokboRank.DOFade(1f, durationtime); 
            }
            
            if (isEventPlay)
            {
                jokboRank.GetComponent<Animator>().Play("JokboLabel_PunchScale_2",-1,0f);
                // jokboRank.alpha = 0f;
                // jokboRank.DOFade(1f, durationtime);
            }
            
            // view.bestCardObj.SetActive(true);
            // view.bestCardText.text = rank;
            // resultRankString = rank;
        }

        private bool isTurnActive = false;

        /// <summary>
        /// 턴쪽의 플레이어 활성화 됄때 이펙트 등등 이미지 세팅
        /// </summary>
        public void ActivateTurn(DateTime startTime, bool isMe)
        {
            view.SetActivateView(isMe, true);
            if (isMe)
            {
                spokerViewer.timeSlider.fillAmount = 1;
                spokerViewer.timeSliderObj.SetActive(true);
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
            float turnTime = (float)CPPlayer.Server.visualEffectTimeConfig["BET_TIMEOUT_MS"]/1000f;
            try
            {
                float elapsedTime = 0f;
                while (elapsedTime < turnTime)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, timeCts.Token);
                    elapsedTime = (float)(CPPlayer.SPoker.estimatedServerNowUtc - startTime).TotalSeconds;
                    float remaining =turnTime - elapsedTime;

                    spokerViewer.timeSlider.fillAmount = remaining / turnTime;
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

        public void CardOpenBtnObjActive(bool isActive)
        {
            view.cardOpenBtn.gameObject.SetActive(isActive);
            view.cardCloseBtn.gameObject.SetActive(isActive);
            
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(isActive);
        }
        
        public async UniTask SetInfoForResult(ResultNoti.Types.Player _resultPlayerInfo, bool isMainPotWin,CancellationToken token)
        {
            if (spokerPlayerInfo.playerInfo == null)
                return;

            resultPlayerInfo = _resultPlayerInfo;
            
       

            //카드 연출 작업
            if (_resultPlayerInfo.HandCards.Count > 0)
            {
                // for (int i = 0; i < _resultPlayerInfo.HandCards.Count; i++)
                // {
                //     spokerPlayerInfo.cardlist[i] = _resultPlayerInfo.HandCards[i];
                // }
                FoldUserCardSet(_resultPlayerInfo.HandCards.ToList());
                
                var cardEventTasks = new List<UniTask>();
                bool isHaveEmptyCard = spokerPlayerInfo.cardlist.Any(o => string.IsNullOrEmpty(o));
                if (isHaveEmptyCard||isFolded)
                {
                    cardEventTasks.Add(OpenFoldUserCards());
                }
                else
                {
                    (string cardRankString, var cardrankStringList) = SetUiAfterCardRecieved();

                    for (int i = 0; i < cardViewerList.Count; i++)
                    {
                        int index = i;
                        if (string.IsNullOrEmpty(spokerPlayerInfo.cardlist[index]))
                            continue;
                        (int rank, Suit suit) = CardRankCalculater.ParseCard(spokerPlayerInfo.cardlist[index]);
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

              
                    for (int i = 0; i < spokerPlayerInfo.cardlist.Count; i++)
                    {
                        int index = i;
                        if (string.IsNullOrEmpty(spokerPlayerInfo.cardlist[index]))
                            continue;

                        bool isExist = cardrankStringList.Any(o => o == spokerPlayerInfo.cardlist[index]);
                        bool hideImageActive = index >= 2 && index < spokerPlayerInfo.cardlist.Count- 1;

                        cardEventTasks.Add(cardViewerList[index].SetCardImageResult(hideImageActive,isMe,isExist));
                    }

                    await UniTask.WhenAll(cardEventTasks).AttachExternalCancellation(token);

                    bool isCardExistEmpty = spokerPlayerInfo.cardlist.Any(o => string.IsNullOrEmpty(o));
                    if (!isCardExistEmpty)
                    {
                        SetRankViewer(cardRankString, cardrankStringList);
                    }
                }
            }

            
        }

        private (string, List<string>) SetUiAfterCardRecieved()
        {
            List<int> mycardIndexList = new List<int>();
            for (int i = 0; i < spokerPlayerInfo.cardlist.Count; i++)
            {
                int rankindex = CardRankCalculater.GetCardIndex(spokerPlayerInfo.cardlist[i]);
                mycardIndexList.Add(rankindex);
                Extension.eLog($"string value:{spokerPlayerInfo.cardlist[i]}//rankindex:{rankindex}", Color.yellow);
            }

            List<int> allCards = new List<int>(mycardIndexList);

            (string cardRankString, var cardValueList) = CardRankCalculater.EvaluateSevenPokerHand(allCards);
            Extension.eLog($"cardRank!{cardRankString}//{string.Join(",", cardValueList)}", Color.cyan);

            List<string> cardrankStringList = new List<string>();
            for (int i = 0; i < cardValueList.Count; i++)
            {
                cardrankStringList.Add(CardRankCalculater.GetCardString(cardValueList[i]));
            }


            //SetRankViewer(cardRankString, cardrankStringList);
            return (cardRankString, cardrankStringList);
        }

        public async UniTask SetResultInfo(ResultNoti.Types.Player _resultPlayerInfo, bool isMainPotWin)
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
               // view.dealerFee.text = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.DealerFee].StringToLocal} -{Extension.ToKoreanFormat(_resultPlayerInfo.Fee)}";
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

        public void DotAnimationStart()
        {
            view.StartLoop();
        }

        public void DotAnimationEnd()
        {
            view.StopLoop();
        }

        public void CardTouchedAfterFold(int cardRankIndex, bool activeForChange)
        {
            if (isMe == false)
                return;
            if (!isFolded&&isForfeitWin==false)
                return;

            if (currentPhase >= SPokerState.ShowDown&&isCardOpenReserved)
                return;

            
            int index = -1;
            string cardString = "";
            for (int i = 0; i < spokerPlayerInfo.cardlist.Count; i++)
            {
                int cardRank = CardRankCalculater.GetCardIndex(spokerPlayerInfo.cardlist[i]);
                if (cardRank == cardRankIndex)
                {
                    index = i;
                    cardString = spokerPlayerInfo.cardlist[i];
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

            bool isCardNotSelected = true;
            for (int i = 0; i < touchedCardIndexforOpenList.Count; i++)
            {
                if (!string.IsNullOrEmpty(touchedCardIndexforOpenList[i]))
                {
                    isCardNotSelected = false;
                    break;
                }
            }
            //PostOpenCardlistInfoAsync().Forget();
            if (isCardOpenReserved && isCardNotSelected)
            {
                CloseCardReserve().Forget();
            }
            if (isCardOpenReserved && !isCardNotSelected)
            {
                PostOpenCardlistInfoAsync().Forget();
            }
        }

        async UniTask OpenCardReserve()
        {
            isCardOpenReserved = true;

            string cardString = "";
            
            if (touchedCardIndexforOpenList.Count > 0)
            {
                
            }
            else
            {
                for (int i = 0; i < spokerPlayerInfo.cardlist.Count; i++)
                {
                    int index = i;
                    int cardRankIndex = CardRankCalculater.GetCardIndex(spokerPlayerInfo.cardlist[i]);

                    touchedCardIndexforOpenList.Add(spokerPlayerInfo.cardlist[i]);
                    cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, true);
                }
            }
            
           

            if (currentPhase != SPokerState.ShowDown && currentPhase != SPokerState.Result)
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
            for (int i = 0; i < spokerPlayerInfo.cardlist.Count; i++)
            {
                int index = i;
                int cardRankIndex = CardRankCalculater.GetCardIndex(spokerPlayerInfo.cardlist[i]);

                cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, false);
            }

            view.cardOpenBtn.gameObject.SetActive(true);
            view.cardCloseBtn.gameObject.SetActive(false);

            PostOpenCardlistInfoAsync().Forget();
        }

        async UniTask PostOpenCardlistInfoAsync()
        {
            if (touchedCardIndexforOpenList.Count == 0)
            {
                var closeReqAsync = await Services.SevenPoker.CardCloseReqAsync(CPPlayer.SPoker.currentTableId);
                if (closeReqAsync.IsSuccess)
                {
                    Extension.eLog("card 오픈 취소!", Color.green);
                }

                return;
            }

            //card open 패킷인데 나중에 수정예정임
            var openPacketRes = await Services.SevenPoker.CardOpenReqAsync(CPPlayer.SPoker.currentTableId, touchedCardIndexforOpenList);
            if (openPacketRes.IsSuccess)
            {
                Extension.eLog($"card open post success{string.Join(",", touchedCardIndexforOpenList)}", Color.cyan);
            }
        }

        public void FoldUserCardSet(List<string> cardsInfo)
        {
            if (spokerPlayerInfo.playerInfo == null)
                return;
            
            Debug.Log($"기존 카드리스트:{string.Join(",",spokerPlayerInfo.cardlist )}//서버 카드 리스트:{string.Join(",",cardsInfo)}");
            spokerPlayerInfo.cardlist = cardsInfo;
            // for (int i = 0; i < cardsInfo.Count; i++)
            // {
            //     spokerPlayerInfo.cardlist[i] = cardsInfo[i];
            // }
        }

        public async UniTask OpenFoldUserCards()
        {
            if (spokerPlayerInfo.playerInfo == null)
                return;
            
      
            
            float openCardTime = (float)CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_MS"]/1000f;
            //카드 이동후 보여주기
            for (int i = 0; i < cardViewerList.Count; i++)
            {
                int index = i;
                if (string.IsNullOrEmpty(spokerPlayerInfo.cardlist[i]))
                    continue;
                cardViewerList[index].transform.SetParent(view.myCardPos[index], true);

                cardViewerList[index].transform.DOLocalRotate(Vector3.zero, openCardTime/2f);
                cardViewerList[index].transform.DOLocalMove(Vector2.zero, openCardTime/2f);
                cardViewerList[index].transform.DOScale(Vector3.one, openCardTime/2f);
                
                
                Color maskcolor = cardViewerList[index].mask.color;
                maskcolor.a = 0.5f;
                cardViewerList[index].mask.color = maskcolor;

            }
            int delayms = (int)CPPlayer.Server.visualEffectTimeConfig["OPEN_ALL_MS"] / 2;
            await UniTask.Delay(delayms);


            var tweenTasks = new List<UniTask>();
            for (int i = 0; i < cardViewerList.Count; i++)
            {
                if (string.IsNullOrEmpty(spokerPlayerInfo.cardlist[i]))
                    continue;
                int index = i;
                cardViewerList[index].mask.gameObject.SetActive(true);
                
                Sequence seq = DOTween.Sequence();
                seq.Append(cardViewerList[index].transform.DOScaleX(0, openCardTime/4f));
                seq.AppendCallback(() =>
                {
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(spokerPlayerInfo.cardlist[index]);
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
                seq.Append(cardViewerList[index].transform.DOScaleX(1, openCardTime/4f));
                seq.OnComplete(() =>
                {
                   
                });
                tweenTasks.Add(seq.AsyncWaitForCompletion().AsUniTask());
            }

            await UniTask.WhenAll(tweenTasks);
        }
    }
}