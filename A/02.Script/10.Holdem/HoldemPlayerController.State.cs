using CAPYBARA.Core;
using DG.Tweening;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 플레이어 상태 관리 (Fold, Allin, Phase, CardOpenBtn)
    /// </summary>
    public partial class HoldemPlayerController
    {
        public void SetCardOpenAtForfeitWin()
        {
            isForfeitWin = true;
            view.cardOpenBtnAtforfeitWin.gameObject.SetActive(true);
        }

        public void SetCurrentPhase(HoldemState _currentPhase)
        {
            currentPhase = _currentPhase;
        }

        public void SetCardOpenBtn()
        {
            if (currentPhase != HoldemState.End)
            {
                if (currentPhase != HoldemState.Showdown && currentPhase != HoldemState.Result)
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
                if (currentPhase != HoldemState.Showdown && currentPhase != HoldemState.End && currentPhase != HoldemState.Result)
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

            view.inActiveMask.SetActive(isFolded);
            if (isFolded)
            {
                float highlightDownTime = 0.3f;
                if (CPPlayer.Server.visualEffectTimeConfig.ContainsKey("SELECT_DOWN_MS"))
                {
                    highlightDownTime = (float)CPPlayer.Server.visualEffectTimeConfig["SELECT_DOWN_MS"] / 1000f;
                }

                foreach (var cardViewer in cardViewerList)
                {
                    cardViewer.InactiveSelectEffectAtFold(highlightDownTime);
                }
                view.bestCardObjInRound_me.gameObject.SetActive(false);
                view.starRankObjNew.SetActive(false);

                foreach (var cardViewer in cardViewerList)
                {
                    cardViewer.SetMaskFade();
                }
                float dieDim = (float)CPPlayer.Server.visualEffectTimeConfig["DIE_ME_DIM_MS"] / 1000f;

                var c = view.inactivemaskImage.color;
                c.a = 0f;
                view.inactivemaskImage.color = c;

                view.inactivemaskImage.DOFade(0.5f, dieDim);
                
                view.currentOwnedChip.gameObject.SetActive(false);
                view.currentOwnedChipInactive.gameObject.SetActive(true);
                view.playerNickName.gameObject.SetActive(false);
                view.playerNickNameInactive.gameObject.SetActive(true);
            }
        }
    }
}
