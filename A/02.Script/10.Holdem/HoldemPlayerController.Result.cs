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
    /// 게임 결과 처리 및 승패 UI 표시
    /// </summary>
    public partial class HoldemPlayerController
    {
        /// <summary>
        /// 쇼다운/결과 - 카드 공개 및 승패 표시
        /// </summary>
        public void SetInfoForResultAsync(ResultNoti.Types.Player _resultPlayerInfo, bool isMainPotWin, bool isShowdownPlayed, bool isChairLeft = true)
        {
            if (holdemPlayerInfo.playerInfo == null)
                return;

            view.bestCardObjInRound_me.gameObject.SetActive(false);
            view.bestCardObjInResult.gameObject.SetActive(false);

            resultPlayerInfo = _resultPlayerInfo;

            Extension.eLog($"[showdownPlayer] player chairID:{_resultPlayerInfo.ChairId}//original player cards:{string.Join(",", holdemPlayerInfo.cardlist)}//player cards:{_resultPlayerInfo.HoleCards.ToString()}", Color.cyan);

            int cardindex = 0;
            for (int i = 0; i < _resultPlayerInfo.HoleCards.Count; i++)
            {
                if (string.IsNullOrEmpty(_resultPlayerInfo.HoleCards[i]))
                    continue;
                if(isMe)
                    continue;
                if (isChairLeft)
                {
                    holdemPlayerInfo.cardlist[cardindex] = _resultPlayerInfo.HoleCards[i];
                }
                else
                {
                    holdemPlayerInfo.cardlist[holdemPlayerInfo.cardlist.Count - 1 - cardindex] = _resultPlayerInfo.HoleCards[i];
                }

                cardindex++;
            }

            bool isCardOpen = false;
            for (int i = 0; i < holdemPlayerInfo.cardlist.Count; i++)
            {
                if (!string.IsNullOrEmpty(holdemPlayerInfo.cardlist[i]))
                {
                    isCardOpen = true;
                    break;
                }
            }

            if (isFolded)
            {
                if (isMe)
                {
                    if (isCardOpenReserved)
                    {
                        AnimateCardInfo(_resultPlayerInfo.HoleCards.ToList(), true).Forget();
                    }
                }
                else
                {
                    if (isCardOpen)
                    {
                        AnimateCardInfo(_resultPlayerInfo.HoleCards.ToList(), true).Forget();
                    }
                }
            }
            else
            {
                if (!isShowdownPlayed)
                {
                    CardSettoResultPos(_resultPlayerInfo.HoleCards.ToList());
                }
            }

            view.roundBetChipObj.SetActive(false);
            view.stampParentObj.gameObject.SetActive(false);

            view.loseObject.SetActive(!isMainPotWin);

            long realWinAmount = _resultPlayerInfo.Win - _resultPlayerInfo.Fee;
            var winAmount = Extension.ToKoreanFormat(realWinAmount);
            long realLoseAmount = _resultPlayerInfo.Chip - _resultPlayerInfo.Chip0;
            string loseAmount = Extension.ToKoreanFormat(realLoseAmount);

            if (isMainPotWin)
            {
                view.winChipAmount.text = $"+{winAmount}";
                //view.dealerFee.text = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.DealerFee].StringToLocal} -{Extension.ToKoreanFormat(_resultPlayerInfo.Fee)}";
            }
            else
            {
                if (realWinAmount > 0)
                {
                    view.loseChipAmount.text = $"+{winAmount}";
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
                view.currentOwnedChipInactive.gameObject.SetActive(false);
                view.playerNickName.gameObject.SetActive(false);
                view.playerNickNameInactive.gameObject.SetActive(true);
            }
            view.currentOwnedChip.text = Extension.ToKoreanFormat(_resultPlayerInfo.Chip, Extension.KoreanFormatMode.Planning);
            view.currentOwnedChipInactive.text= Extension.ToKoreanFormat(_resultPlayerInfo.Chip, Extension.KoreanFormatMode.Planning);
        }

        public void SetInfoForResult(ResultNoti.Types.Player _resultPlayerInfo, bool isMainPotWin)
        {
            if (holdemPlayerInfo.playerInfo == null)
                return;

            resultPlayerInfo = _resultPlayerInfo;

            bool isMeFold = isMe && isFolded;
            if (_resultPlayerInfo.HoleCards.Count > 0 && isMeFold == false)
            {
                for (int i = 0; i < _resultPlayerInfo.HoleCards.Count; i++)
                {
                    holdemPlayerInfo.cardlist[i] = _resultPlayerInfo.HoleCards[i];
                }
                for (int i = 0; i < cardViewerList.Count; i++)
                {
                    int index = i;
                    if (string.IsNullOrEmpty(holdemPlayerInfo.cardlist[index]))
                        continue;
                    cardViewerList[index].transform.SetParent(view.winnerCardPos[index], true);
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
                    cardViewerList[index].mask.gameObject.SetActive(false);
                }
            }

            if (_resultPlayerInfo.HoleCards.Count > 0)
            {
                view.cardOpenBtn.gameObject.SetActive(false);
                view.cardCloseBtn.gameObject.SetActive(false);

                view.cardOpenBtnAtforfeitWin.gameObject.SetActive(false);
                view.cardCloseBtnAtforfeitWin.gameObject.SetActive(false);
            }

            view.roundBetChipObj.SetActive(false);
            view.stampParentObj.gameObject.SetActive(false);

            view.loseObject.SetActive(!isMainPotWin);

            long realWinAmount = _resultPlayerInfo.Win - _resultPlayerInfo.Fee;
            var winAmount = Extension.ToKoreanFormat(realWinAmount);
            long realLoseAmount = _resultPlayerInfo.Chip - _resultPlayerInfo.Chip0;
            string loseAmount = Extension.ToKoreanFormat(realLoseAmount);

            if (isMainPotWin)
            {
                view.winChipAmount.text = $"+{winAmount}";
                //view.dealerFee.text = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.DealerFee].StringToLocal} -{Extension.ToKoreanFormat(_resultPlayerInfo.Fee)}";
            }
            else
            {
                if (realWinAmount > 0)
                {
                    view.loseChipAmount.text = $"+{winAmount}";
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
                view.currentOwnedChipInactive.gameObject.SetActive(false);
                view.playerNickName.gameObject.SetActive(false);
                view.playerNickNameInactive.gameObject.SetActive(true);
            }

            view.currentOwnedChip.text = Extension.ToKoreanFormat(_resultPlayerInfo.Chip, Extension.KoreanFormatMode.Planning);
            view.currentOwnedChipInactive.text= Extension.ToKoreanFormat(_resultPlayerInfo.Chip, Extension.KoreanFormatMode.Planning);
        }

        public void SetWinnerUI(bool isWinner)
        {
            isWin = isWinner;
            if (isWinner)
            {
                view.winFontImageObj.SetActive(true);
                view.winFontImageAnimator.Play("WinIcon_effect");
            }
        }
    }
}
