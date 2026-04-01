using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 폴드 이후 카드 오픈/클로즈 예약 및 애니메이션
    /// </summary>
    public partial class HoldemPlayerController
    {
        public void FoldUserCardSet(List<string> cardsInfo, bool isChairLeft = true)
        {
            if (holdemPlayerInfo.playerInfo == null)
                return;

            int index = 0;
            for (int i = 0; i < cardsInfo.Count; i++)
            {
                if (string.IsNullOrEmpty(cardsInfo[i]))
                    continue;
                if (isChairLeft)
                {
                    holdemPlayerInfo.cardlist[index] = cardsInfo[i];
                }
                else
                {
                    holdemPlayerInfo.cardlist[holdemPlayerInfo.cardlist.Count - 1 - index] = cardsInfo[i];
                }

                index++;
            }
        }

        public async UniTask OpenFoldUserCards()
        {
            if (holdemPlayerInfo.playerInfo == null)
                return;

            float openCardTime = (float)CPPlayer.Server.visualEffectTimeConfig["OPEN_ALL_MS"] / 1000f;

            for (int i = 0; i < cardViewerList.Count; i++)
            {
                int index = i;
                cardViewerList[index].mask.gameObject.SetActive(true);
                cardViewerList[index].CardHighlightActive(false);

                cardViewerList[index].transform.SetParent(view.winnerCardPos[index], true);

                cardViewerList[index].transform.DOLocalRotate(Vector3.zero, openCardTime / 2f);
                cardViewerList[index].transform.DOLocalMove(Vector2.zero, openCardTime / 2f);
                cardViewerList[index].transform.DOScale(Vector3.one, openCardTime / 2f);
            }

            int delayms = (int)CPPlayer.Server.visualEffectTimeConfig["OPEN_ALL_MS"] / 2;
            await UniTask.Delay(delayms);

            var tweenTasks = new List<UniTask>();
            for (int i = 0; i < cardViewerList.Count; i++)
            {
                if (string.IsNullOrEmpty(holdemPlayerInfo.cardlist[i]))
                    continue;
                int index = i;

                Sequence seq = DOTween.Sequence();
                seq.Append(cardViewerList[index].transform.DOScaleX(0, openCardTime / 4f));
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
                seq.Append(cardViewerList[index].transform.DOScaleX(1, openCardTime / 4f));

                tweenTasks.Add(seq.AsyncWaitForCompletion().AsUniTask());
            }

            await UniTask.WhenAll(tweenTasks);
        }

        public void CardTouchCallback(int cardRankIndex, bool activeForChange)
        {
              if (isMe == false)
                return;
            if (!isFolded && isForfeitWin == false)
                return;

            if (currentPhase >= HoldemState.Showdown && isCardOpenReserved)
                return;

            int index = -1;
            string cardString = "";
            for (int i = 0; i < holdemPlayerInfo.cardlist.Count; i++)
            {
                int cardRank = CardRankCalculater.GetCardIndex(holdemPlayerInfo.cardlist[i]);
                if (cardRank == cardRankIndex)
                {
                    index = i;
                    cardString = holdemPlayerInfo.cardlist[i];
                    if (i >= touchedCardIndexforOpenList.Count)
                    {
                        touchedCardIndexforOpenList.Add(activeForChange ? cardString : "");
                    }
                    else
                    {
                        touchedCardIndexforOpenList[i] = activeForChange ? cardString : "";
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

            if (currentPhase < HoldemState.End)
            {
                cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, activeForChange);
            }

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
        }
        public void CardTouchedAfterFold(int cardRankIndex, bool activeForChange)
        {
            if (isMe == false)
                return;
            if (!isFolded && isForfeitWin == false)
                return;

            if (currentPhase >= HoldemState.Showdown && isCardOpenReserved)
                return;

            int index = -1;
            string cardString = "";
            for (int i = 0; i < holdemPlayerInfo.cardlist.Count; i++)
            {
                int cardRank = CardRankCalculater.GetCardIndex(holdemPlayerInfo.cardlist[i]);
                if (cardRank == cardRankIndex)
                {
                    index = i;
                    cardString = holdemPlayerInfo.cardlist[i];
                    if (i >= touchedCardIndexforOpenList.Count)
                    {
                        touchedCardIndexforOpenList.Add(activeForChange ? cardString : "");
                    }
                    else
                    {
                        touchedCardIndexforOpenList[i] = activeForChange ? cardString : "";
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

            if (currentPhase < HoldemState.End)
            {
                cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, activeForChange);
            }

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
        }

        private async UniTask PostOpenCardlistInfoAsync()
        {
            if (touchedCardIndexforOpenList.Count == 0)
            {
                var closeReqAsync = await Services.Holdem.CardCloseReqAsync(CPPlayer.Holdem.currentTableId);
                if (closeReqAsync.IsSuccess)
                {
                    Extension.eLog("card 오픈 취소!", Color.green);
                }

                return;
            }

            var openPacketRes = await Services.Holdem.CardOpenReqAsync(CPPlayer.Holdem.currentTableId, touchedCardIndexforOpenList);
            if (openPacketRes.IsSuccess)
            {
                Extension.eLog($"card open post success{string.Join(",", touchedCardIndexforOpenList)}", Color.cyan);
            }
        }

        private async UniTask OpenCardReserve()
        {
            isCardOpenReserved = true;

            if (touchedCardIndexforOpenList.Count == 0)
            {
                for (int i = 0; i < holdemPlayerInfo.cardlist.Count; i++)
                {
                    int index = i;
                    int cardRankIndex = CardRankCalculater.GetCardIndex(holdemPlayerInfo.cardlist[i]);

                    touchedCardIndexforOpenList.Add(holdemPlayerInfo.cardlist[i]);
                    cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, true);
                }
            }

            if (currentPhase != HoldemState.Showdown && currentPhase != HoldemState.Result)
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

        public void CardOpenBtnObjActive(bool isActive)
        {
            view.cardOpenBtn.gameObject.SetActive(isActive);
            view.cardCloseBtn.gameObject.SetActive(isActive);

            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(isActive);
            view.cardCloseBtnAtforfeitWin.gameObject.SetActive(isActive);
        }

        private async UniTask CloseCardReserve()
        {
            isCardOpenReserved = false;

            touchedCardIndexforOpenList.Clear();
            for (int i = 0; i < holdemPlayerInfo.cardlist.Count; i++)
            {
                int index = i;
                int cardRankIndex = CardRankCalculater.GetCardIndex(holdemPlayerInfo.cardlist[i]);

                cardViewerList[index].HighLightCardCallbackAtTouch(cardRankIndex, false);
            }

            view.cardOpenBtn.gameObject.SetActive(true);
            view.cardCloseBtn.gameObject.SetActive(false);

            PostOpenCardlistInfoAsync().Forget();
        }
    }
}
