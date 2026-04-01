using System;
using System.Collections.Generic;
using System.Threading;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    //카드 선택 윈도우
    public class SPokerSelectCardView : MonoBehaviour
    {
        public SPokerCardViewForSelect[] cardView;
        public TMP_Text titleTxt;
        public Image timeSlider;

        private UniTaskCompletionSource<List<int>> cardtcs;
        private CancellationTokenSource timeoutCts;

        string dropCardText;
        string openCardText;
        
        List<string> currentCardIndexList = new List<string>();
        public void Init()
        {
            for (int i = 0; i < cardView.Length; i++)
            {
                cardView[i].Init();
            }

            titleTxt.text = dropCardText;
            CPPlayer.Option.FourCardModeChange += CardColorModeChange;
            
            dropCardText = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SelectCardToDiscard].StringToLocal;
            openCardText = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SelectCardToOpen].StringToLocal;
        }
        public UniTask<List<int>> OpenViewAsync(List<string> cardIndexList)
        {
            selected.Clear();
            titleTxt.text = dropCardText;
            cardtcs = new UniTaskCompletionSource<List<int>>();
            //Debug.LogError("선택창 카드:"+string.Join(",", cardIndexList));
            currentCardIndexList = cardIndexList;
            this.gameObject.SetActive(true);
            for (int i = 0; i < cardView.Length; i++)
            {
                (int _rank, Suit _suit) = CardRankCalculater.ParseCard(currentCardIndexList[i]);
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
                cardView[i].InitCardImage(_CardInfo.cardSprite);
             
            }

            Bind();
            RunTimerAsync().Forget();
            
            return cardtcs.Task;
        }
        
        private void CardColorModeChange(bool isFourColor)
        {
            if (gameObject.activeInHierarchy == false)
                return;
            for (int i = 0; i < cardView.Length; i++)
            {
                (int _rank, Suit _suit) = CardRankCalculater.ParseCard(currentCardIndexList[i]);
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
                cardView[i].cardImage.sprite=_CardInfo.cardSprite;
            }
        }
        
        private async UniTaskVoid RunTimerAsync()
        {
            timeoutCts = new CancellationTokenSource();

            bool audioToggle = false;
            int prevRemainSec = -1;
            float speed = 100f;
            float selectTime= (float)CPPlayer.Server.visualEffectTimeConfig["CARD_SELECT_MS"]/1000f;
            try
            {
                float elapsed = 0f;
                while (elapsed < selectTime)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, timeoutCts.Token);
                    elapsed += Time.deltaTime;
                    float remainTime = selectTime - elapsed;
                    timeSlider.fillAmount = remainTime / selectTime;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("액션으로 인한 타임 끝 곧 서버 액션노티로 인한 턴 종료");
            }
            finally
            {
                timeoutCts?.Cancel();
                timeoutCts?.Dispose();
                timeoutCts = null;
            }
        }
        
        private void Bind()
        {
            // 토글 이벤트 바인딩

            for (int i = 0; i < cardView.Length; i++)
            {
                int idx = i;
                cardView[idx].AddListenerToSelectCardStart(OnToggleChangedAtStart);
                cardView[idx].AddListenerToSelectCardEnd( OnToggleChangedAtEnd);
            }

        }

        private List<int> selected=new List<int>();
        int currentSelectCount=0;
        private void OnToggleChangedAtStart(SPokerCardViewForSelect cardview,bool isSelect)
        {
            if (cardtcs == null)
                return;

          
            for (int i = 0; i < cardView.Length; i++)
            {
                if (cardView[i].isSelected)
                {
                    if (!selected.Contains(i))
                    {
                        selected.Add(i);    
                    }
                }
                else
                {
                    if (selected.Contains(i))
                    {
                        selected.Remove(i);    
                    }
                }
            }

            currentSelectCount = selected.Count;
            
            if (selected.Count == 0)
            {
                titleTxt.text = dropCardText;
            }
            if (selected.Count == 1)
            {
                titleTxt.text = openCardText;
            }
            
            if (selected.Count == 1||selected.Count == 0)
            {
                cardview.UnselectImageActive(cardview.isSelected);
            }
            
            if (selected.Count == 2)
            {
                for (int i = 0; i < cardView.Length; i++)
                {
                    cardView[i].LockTouch();
                }
            }
        }
        private void OnToggleChangedAtEnd(SPokerCardViewForSelect cardview,bool isSelect)
        {
            if (cardtcs == null)
                return;

            // 체크된 인덱스 수집
        
            for (int i = 0; i < cardView.Length; i++)
            {
                if (cardView[i].isSelected)
                {
                    if (!selected.Contains(i))
                    {
                        selected.Add(i);    
                    }
                }
                else
                {
                    if (selected.Contains(i))
                    {
                        selected.Remove(i);    
                    }
                }
            }

            if (currentSelectCount == 2)
            {
                Complete(selected);
            }
        }
        
        private void Complete(List<int> result)
        {
            if (cardtcs == null) return;

            cardtcs.TrySetResult(result);
            cardtcs = null;

            Unbind();
            
            timeoutCts?.Cancel();
            timeoutCts?.Dispose();
            timeoutCts = null;
            

            
            this.gameObject.SetActive(false);
        }
        
        private void Unbind()
        {
            for (int i = 0; i < cardView.Length; i++)
                cardView[i].RemoveListenerToSelectCard();
        }
        
        public void CloseView()
        {
            Complete(new List<int>());
        }

    }
}
