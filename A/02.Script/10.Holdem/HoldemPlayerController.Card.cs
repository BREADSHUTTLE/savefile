using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 카드 추가/표시/하이라이트/족보랭크/색상 변경
    /// </summary>
    public partial class HoldemPlayerController
    {
        public void AddCard(string card, CardViewer _viewer)
        {
            holdemPlayerInfo.cardlist.Add(card);
            _viewer.AddListenerForCardTouch(this);
            _viewer.InitCardSet();
            cardViewerList.Add(_viewer);
        }

        public void GetCard(string card, CardViewer _viewer)
        {
            if (isMe)
            {
                if (holdemPlayerInfo.cardlist.Count >= 2)
                {
                    CPPlayer.Holdem.CardRecieved?.Invoke(this, false);
                }
            }
        }

        public void SetJokboRankAndCardHighlight(string rank, List<string> cardRankStringList, CardUISetState cardUIstate, bool isAfterShowDwon, bool Result = false)
        {
            bool isEmptyCardExist = false;
            for (int i = 0; i < holdemPlayerInfo.cardlist.Count; i++)
            {
                if (string.IsNullOrEmpty(holdemPlayerInfo.cardlist[i]))
                {
                    isEmptyCardExist = true;
                    break;
                }
            }

            if (holdemPlayerInfo.cardlist.Count < 2 || isEmptyCardExist)
                return;

            bool isCardOpenFullCount = true;
            for (int i = 0; i < touchedCardIndexforOpenList.Count; i++)
            {
                if (string.IsNullOrEmpty(touchedCardIndexforOpenList[i]))
                {
                    isCardOpenFullCount = false;
                    break;
                }
            }

            bool bestCardActive = view.bestCardTextinRound_me.gameObject.activeInHierarchy || view.bestCardObjInResult.gameObject.activeInHierarchy ||
                                  view.bestCardObjInResult_Loser.gameObject.activeInHierarchy;

            bool isEventStart = bestCardActive==false&& cardRankStringList.Count>1;
            
            //bool isEventStart = !string.Equals(view.bestCardTextinRound_me.text, rank);
            //Extension.eLog($"card text info view:{view.bestCardTextinRound_me.text}/data:{rank}/isEventStart:{isEventStart}", Color.chocolate);

            view.bestCardTextinRound_me.text = rank;
            view.bestCardTextinRound.text = rank;
            view.bestCardTextinResult.text = rank;

            view.bestCardObjInRound_me.gameObject.SetActive(false);
            view.bestCardObjInResult.gameObject.SetActive(false);
            view.bestCardObjInResult_Loser.gameObject.SetActive(false);
            CanvasGroup jokboRank = view.bestCardObjInResult;

            switch (cardUIstate)
            {
                case CardUISetState.GetPreflopCard:
                    jokboRank = view.bestCardObjInRound_me;
                    if (!isFolded)
                        view.bestCardObjInRound_me.gameObject.SetActive(true);
                    break;
                case CardUISetState.GetCommunityCard:
                    if (isAfterShowDwon)
                    {
                        if (isMe)
                        {
                            if (isFolded)
                            {
                                if (isCardOpenReserved && isCardOpenFullCount)
                                {
                                    jokboRank = view.bestCardObjInResult;
                                    if (Result)
                                    {
                                        view.bestCardObjInResult.gameObject.SetActive(true);
                                    }
                                    else
                                    {
                                        if (!isFolded)
                                            view.bestCardObjInResult.gameObject.SetActive(true);
                                    }
                                }
                                else
                                {
                                    jokboRank = view.bestCardObjInRound_me;
                                    if (Result)
                                    {
                                        view.bestCardObjInRound_me.gameObject.SetActive(true);
                                    }
                                    else
                                    {
                                        if (!isFolded)
                                            view.bestCardObjInRound_me.gameObject.SetActive(true);
                                    }
                                }
                            }
                            else
                            {
                                jokboRank = view.bestCardObjInResult;
                                view.bestCardObjInResult.gameObject.SetActive(true);
                            }
                        }
                        else
                        {
                            jokboRank = view.bestCardObjInResult;
                            if (Result)
                            {
                                view.bestCardObjInResult.gameObject.SetActive(true);
                            }
                            else
                            {
                                if (!isFolded)
                                    view.bestCardObjInResult.gameObject.SetActive(true);
                            }
                        }
                    }
                    else
                    {
                        jokboRank = view.bestCardObjInRound_me;
                        if (Result)
                        {
                            view.bestCardObjInResult.gameObject.SetActive(true);
                        }
                        else
                        {
                            if (!isFolded)
                                view.bestCardObjInRound_me.gameObject.SetActive(true);
                        }
                    }
                    break;
                case CardUISetState.AllCardOpen:
                    //isEventStart = true;
                    jokboRank = view.bestCardObjInResult;
                    view.bestCardObjInResult.gameObject.SetActive(true);
                    break;
                case CardUISetState.CardNotOpen:
                    break;
                case CardUISetState.AfterResult:
                    isEventStart = false;
                    jokboRank = view.bestCardObjInResult_Loser;
                    view.bestCardObjInResult_Loser.gameObject.SetActive(true);
                    break;
                case CardUISetState.TurnNotiChanged:
                    jokboRank = view.bestCardObjInRound_me;
                    if (!isFolded)
                        view.bestCardObjInRound_me.gameObject.SetActive(true);
                    break;
            }

            bool isPreflopUnderState = cardUIstate <= CardUISetState.GetPreflopCard;
            Extension.eLog($"{ cardUIstate}현재의 상태", Color.blue);
            if (rank.Contains(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.High].StringToLocal) && isPreflopUnderState)
            {
                view.bestCardObjInRound_me.gameObject.SetActive(false);
            }

            if (rank.Contains(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.High].StringToLocal) && cardUIstate == CardUISetState.GetCommunityCard 
                                                                                                         &&currentPhase==HoldemState.PH_PRE_FLOP)
            {
                isEventStart = true;
            }

            if (isEventStart)
            {
                float durationtime = (float)CPPlayer.Server.visualEffectTimeConfig["OPEN_RANK_MS"] / 1000f;

                var anim = jokboRank.GetComponent<Animator>();
                anim.Rebind();
                anim.Update(0f);
                anim.Play("JokboLabel_PunchScale", -1, 0f);
            }

           

            if (isFolded)
                return;

            var matchedIndices = holdemPlayerInfo.cardlist
                .Select((value, index) => new { value, index })
                .Where(x => cardRankStringList.Contains(x.value))
                .Select(x => x.index)
                .ToList();

            Action staranim = null;

            if (cardRankStringList.Count > 1)
            {
                if (cardUIstate == CardUISetState.AfterResult)
                {
                    if (isWin)
                    {
                        for (int i = 0; i < cardViewerList.Count; i++)
                        {
                            if (matchedIndices.Contains(i))
                            {
                                cardViewerList[i].HighLightCardCallback(cardViewerList[i].cardInfoIndex, true, staranim,true);
                            }
                            else
                            {
                                cardViewerList[i].HighLightCardCallback(cardViewerList[i].cardInfoIndex, false, staranim,true);
                            }
                        }
                    }
                    else
                    {
                        foreach (var cardviewer in cardViewerList)
                        {
                            cardviewer.HighLightCardCallback(cardviewer.cardInfoIndex, false, staranim,true);
                        }
                    }
                }
                else
                {
                    if (isMe)
                    {
                        if (cardRankStringList.Count > 1)
                        {
                            for (int i = 0; i < cardViewerList.Count; i++)
                            {
                                if (matchedIndices.Contains(i))
                                {
                                    cardViewerList[i].HighLightCardCallback(cardViewerList[i].cardInfoIndex, true, staranim,true);
                                }
                                else
                                {
                                    cardViewerList[i].HighLightCardCallback(cardViewerList[i].cardInfoIndex, false, staranim,true);
                                }
                            }
                        }

                      
                    }
                }
            }
        }

        public void SetCardStarRank( CardUISetState cardUIstate)
        {
            if (isMe)
            {
                if (cardUIstate == CardUISetState.GetPreflopCard)
                {
                    int starlevel = GetHandStarCount(holdemPlayerInfo.cardlist);
                    SetStarRank(starlevel).Forget();
                }
            }
        }

        public async UniTask SetStarRank(int level)
        {
            if (CPPlayer.Cloud.optionValue.handRankInform == false)
                return;

            int highlightUpTime = 300;
            if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_UP_MS"))
            {
                highlightUpTime = (int)CPPlayer.Server.visualEffectTimeConfig["SELECT_UP_MS"];
            }

            await UniTask.Delay(highlightUpTime);
    
            Extension.eLog($"star level:{level}", Color.red);
            view.starRankObjNew.SetActive(true);
            
            view.starRankObjNewAnimator.Rebind();                                            
            view.starRankObjNewAnimator.Update(0f);
            view.starRankObjNewAnimator.Play($"Star_LevelBox {level}", -1, 0f);
        }

        public int GetHandStarCount(List<string> cards)
        {
            if (cards == null || cards.Count != 2)
                return 0;

            string key = MakeHandKey(cards[0], cards[1]);
            return Constraints.HandStars.TryGetValue(key, out int stars) ? stars : 0;
        }

        private string MakeHandKey(string c1, string c2)
        {
            char r1 = c1[0];
            char r2 = c2[0];
            char s1 = c1[1];
            char s2 = c2[1];

            string order = "AKQJT98765432";

            if (r1 == r2)
                return $"{r1}{r2}";

            if (order.IndexOf(r1) > order.IndexOf(r2))
                (r1, r2) = (r2, r1);

            bool suited = s1 == s2;
            return $"{r1}{r2}" + (suited ? "s" : "o");
        }

        public Transform GetPlayerCardTr()
        {
            Transform tr;
            if (isMe)
            {
                tr = view.myCardPos[holdemPlayerInfo.cardlist.Count];
            }
            else
            {
                tr = view.otherCardPos[holdemPlayerInfo.cardlist.Count];
            }

            return tr;
        }

        public void CardSettoResultPos(List<string> holeCardsInfo)
        {
            view.stampParentObj.gameObject.SetActive(false);

            float cardmoveTime = (float)CPPlayer.Server.visualEffectTimeConfig["SHOWDOWN_OTHER_MS"] / 1000f;

            if (holeCardsInfo.Count > 0)
            {
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
                    if (string.IsNullOrEmpty(holdemPlayerInfo.cardlist[index]))
                        continue;

                    (int rank, Suit suit) = CardRankCalculater.ParseCard(holdemPlayerInfo.cardlist[index]);
                    List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    if (!CPPlayer.Cloud.optionValue.fourColor)
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                    }

                    var CardInfo = cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    cardViewerList[index].cardImage.sprite = CardInfo.cardSprite;
                }
            }
        }

        public async UniTask AnimateCardInfo(List<string> holeCardsInfo, bool isFoldUserOpen = false)
        {
            view.stampParentObj.gameObject.SetActive(false);

            float cardmoveTime = (float)CPPlayer.Server.visualEffectTimeConfig["SHOWDOWN_OTHER_MS"] / 1000f;

            if (holeCardsInfo.Count > 0)
            {
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    int index = i;
                    cardViewerList[index].mask.gameObject.SetActive(isFoldUserOpen);
                    cardViewerList[index].transform.SetParent(view.winnerCardPos[index], true);

                    cardViewerList[index].transform.DOLocalRotate(Vector3.zero, cardmoveTime / 2f);
                    cardViewerList[index].transform.DOLocalMove(Vector2.zero, cardmoveTime / 2f);
                    cardViewerList[index].transform.DOScale(Vector3.one, cardmoveTime / 2f);
                }

                await UniTask.Delay((int)CPPlayer.Server.visualEffectTimeConfig["SHOWDOWN_OTHER_MS"] / 2);

                var tweenTasks = new System.Collections.Generic.List<UniTask>();
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    int index = i;
                    if (string.IsNullOrEmpty(holdemPlayerInfo.cardlist[index]))
                        continue;

                    Sequence seq = DOTween.Sequence();
                    seq.Append(cardViewerList[index].transform.DOScaleX(0, cardmoveTime / 4f));
                    seq.AppendCallback(() =>
                    {
                        (int rank, Suit suit) = CardRankCalculater.ParseCard(holdemPlayerInfo.cardlist[index]);
                        List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                        if (!CPPlayer.Cloud.optionValue.fourColor)
                        {
                            cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                        }

                        var CardInfo = cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
                        cardViewerList[index].cardImage.sprite = CardInfo.cardSprite;
                    });
                    seq.Append(cardViewerList[index].transform.DOScaleX(1, cardmoveTime / 4f));
                    tweenTasks.Add(seq.AsyncWaitForCompletion().AsUniTask());
                }

                await UniTask.WhenAll(tweenTasks);
            }
        }

        private void CardColorModeChange(bool isFourColor)
        {
            if (isFourColor)
            {
                for (int i = 0; i < holdemPlayerInfo.cardlist.Count; i++)
                {
                    if (string.IsNullOrEmpty(holdemPlayerInfo.cardlist[i]))
                        continue;
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(holdemPlayerInfo.cardlist[i]);
                    var CardInfo = InGameResourcesBundle.Loaded.cardResourceList.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    cardViewerList[i].cardImage.sprite = CardInfo.cardSprite;
                }
            }
            else
            {
                for (int i = 0; i < holdemPlayerInfo.cardlist.Count; i++)
                {
                    if (string.IsNullOrEmpty(holdemPlayerInfo.cardlist[i]))
                        continue;
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(holdemPlayerInfo.cardlist[i]);
                    var CardInfo = InGameResourcesBundle.Loaded.cardResourceList_TwoColor.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    cardViewerList[i].cardImage.sprite = CardInfo.cardSprite;
                }
            }
        }
    }
}
