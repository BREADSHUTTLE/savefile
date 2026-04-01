using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.sevenPoker;
using Cysharp.Threading.Tasks;
using Google.Protobuf.Collections;
using UnityEngine;
using DG.Tweening;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;
using System.Linq;

namespace CAPYBARA
{
    public class SPokerTableSnapShot
    {
        public sevenPoker.EnterRes RoomImfo { get { return currentRoomInfo; } }
        private sevenPoker.EnterRes currentRoomInfo;
        
        private SPokerViewer spokerView;
        public RepeatedField<sevenPoker.ResultNoti.Types.Pot>  lastPotInfo;
        private Dictionary<int,List<long> > betDict = new Dictionary<int, List<long>>();
        
        private List<Poolable> chipList = new List<Poolable>();
        
        public void Init(SPokerViewer _view)
        {
            spokerView = _view;
        
            
            CPPlayer.SPoker.ThrowAnte += ThrowAnte;
            CPPlayer.SPoker.ThrowChip += ThrowChip;

            CPPlayer.SPoker.CardRecieved += SetUiAfterCardRecieved;
        }
        public void SetGameInfo(sevenPoker.EnterRes enterRes)
        {
            currentRoomInfo = enterRes;
        }

         public void ThrowAnte(int chairId,long ante, Transform startPos)
        {
            if(betDict.ContainsKey(chairId)==false)
                betDict.Add(chairId,new List<long>());
            betDict[chairId].Add(ante);
            
            var anteObj = PoolManager.Pop(InGameResourcesBundle.Loaded.anteObject, spokerView.transform, startPos.position);
            anteObj.transform.localScale = Vector3.one;
            anteObj.transform.position= startPos.position;
            anteObj.GetComponent<RectTransform>().DOMove(spokerView.anteArrive.anchoredPosition, 0.5f).OnComplete(() =>
            {
                PoolManager.Push(anteObj);
            }); 
        }
        private void ThrowChip(int chairId,long _chip, Transform startPos)
        {
            return;
            if(betDict.ContainsKey(chairId)==false)
                betDict.Add(chairId,new List<long>());
            betDict[chairId].Add(_chip);

            long chipamount = 0;
            
            chipamount=_chip/CPPlayer.SPoker.initialBuyIn;
            if (_chip > 0 && chipamount <= 0)
            {
                chipamount = 0;
            }
            else
            {
                if (chipamount >= 4)
                {
                    chipamount = 4;
                }
            }
            if (_chip > 0)
            {
                for (int i = 0; i < chipamount; i++)
                {
                    GameObject chipprefab = InGameResourcesBundle.Loaded.chipObject;
                
                    var chip = PoolManager.Pop(chipprefab, spokerView.chipParent.transform, startPos.position);
                    chip.transform.position = startPos.position;
                    chip.transform.localScale = Vector3.one;
                    Vector2 randomPos= 
                        spokerView.anteArrive.anchoredPosition + Random.insideUnitCircle * 140f;
           
                    chip.GetComponent<RectTransform>().DOAnchorPos(randomPos, 0.5f).OnComplete(() =>
                    {
                        //PoolManager.Push(chip);
                    }); 
                    chipList.Add(chip);
                }
            }
          
        }

        #region card draw
        public void CardThrowToPlayer(SPokerPlayerController player, HandCardNoti myCardnoti)
        {
            var obj = InGameResourcesBundle.Loaded.spokerCardPrefab;
            var mycard = PoolManager.Pop(obj, spokerView.transform,
                spokerView.cardStartPos.position);
    
            
            mycard.transform.position = spokerView.cardStartPos.position;
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr ,false);
            
            player.AddCard(myCardnoti.Card, mycard);
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            mycard.cardInfoIndex = CardRankCalculater.GetCardIndex(myCardnoti.Card);
            
    
            
            
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOLocalMove(Vector2.zero, 0.1f));
            seq.Join(rt.DOScale(Vector3.one, 0.1f));
            seq.Join(rt.DOLocalRotate(Vector3.zero, 0.1f));
            seq.SetEase(Ease.OutQuad);
            seq.OnComplete(() =>
            {
                (int rank, Suit suit) = CardRankCalculater.ParseCard(myCardnoti.Card);
                List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                if (CPPlayer.Cloud.optionValue.fourColor)
                {
                    cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                }
                else
                {
                    cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                }
                var CardInfo =cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);

                if (CPPlayer.SPoker.currentSPokerState < SPokerState.Round_1)
                {
                    mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
                }
                else
                {
                    mycard.cardImage.sprite = CardInfo.cardSprite;    
                }
                
                player.GetCard(myCardnoti.Card, mycard);
                if (player.cardViewerList.Count == 7)
                {
                    mycard.SetCardHide(true);
                }
            });
        }
        
        public void CardThrowToPlayer(SPokerPlayerController player, HandCardNotiOther cardNoti)
        {
            var obj = InGameResourcesBundle.Loaded.spokerCardPrefab;
            var mycard = PoolManager.Pop(obj, spokerView.transform,
                spokerView.cardStartPos.position);
            
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr ,false);
            
            bool isCardOpen = !string.IsNullOrEmpty(cardNoti.Card);
            string cardString = isCardOpen ? cardNoti.Card:"";
            player.AddCard(cardString,mycard);   
            
            mycard.transform.position = spokerView.cardStartPos.position;
            mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            
       
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            


            if (isCardOpen)
            {
                mycard.cardInfoIndex = CardRankCalculater.GetCardIndex(cardNoti.Card);    
            }
            else
            {
                mycard.cardInfoIndex = -1;
            }
            
        
            
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOLocalMove(Vector2.zero, 0.1f));
            seq.Join(rt.DOScale(Vector3.one, 0.1f));
            seq.Join(rt.DOLocalRotate(Vector3.zero, 0.1f));
            seq.SetEase(Ease.OutQuad);
            seq.OnComplete(() =>
            {
                if (isCardOpen)
                {
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(cardString);
                    List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    if (CPPlayer.Cloud.optionValue.fourColor)
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    }
                    else
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                    }
                    var CardInfo =cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    mycard.cardImage.sprite = CardInfo.cardSprite;
                }
                else
                {  
                    mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
                }
                
                player.GetCard(cardString, mycard);
            });
         
        }
        #endregion

        public void CardDrop(SPokerPlayerController player, int indexListToChagne)
        {
            int cardindex = indexListToChagne;
            var mycard=player.cardViewerList[cardindex];
            var startPos = mycard.transform.position;
                
            Transform cardTr = spokerView.cardStartPos;
            mycard.transform.SetParent(cardTr ,false);
            mycard.transform.position = startPos;
                
            var rt = mycard.transform.GetComponent<RectTransform>();
            mycard.cardInfoIndex =-1;
                
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOLocalMove(Vector2.zero, 0.3f));
            seq.Join(rt.DOScale(Vector3.one, 0.3f));
            seq.Join(rt.DOLocalRotate(Vector3.zero, 0.3f));
            seq.SetEase(Ease.OutQuad);
            seq.OnComplete(() =>
            {
                //player.badugiPlayerInfo.cardlist[cardindex] = "";
            });
        }
        
        public void SetCardToOtherPlayerAtEnter(SPokerPlayerController player,string cardString,int cardCount)
        {
            var obj = InGameResourcesBundle.Loaded.spokerCardPrefab;
            var mycard = PoolManager.Pop(obj, spokerView.transform,
                spokerView.cardStartPos.position);
            
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr,false);
            
            player.AddCard(cardString,mycard);
            player.GetCard(cardString,mycard);
            
            mycard.gameObject.SetActive(true);
            mycard.transform.position = spokerView.cardStartPos.position;

            if (!string.IsNullOrEmpty(cardString) &&cardCount>3)
            {
                (int rank, Suit suit) = CardRankCalculater.ParseCard(cardString);
                List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                if (CPPlayer.Cloud.optionValue.fourColor)
                {
                    cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                }
                else
                {
                    cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                }
                var CardInfo =cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
                mycard.cardImage.sprite = CardInfo.cardSprite;
            }
            else
            {
                mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            }
     
            
         
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            rt.localPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation=Quaternion.identity;
  
        }

        #region CardRank Display
        public string SetResultUIAndMainWinnerRank(SPokerPlayerController _meController,bool isShowDown)
        {
            string rankText=SetUiAfterCardRecieved(_meController,isShowDown);
            return rankText;    
        }

        public string SetUiAfterCardRecieved(SPokerPlayerController _playerController, bool isShowDown = false)
        {
            if (CPPlayer.SPoker.currentSPokerState < SPokerState.Round_1)
                return null;

            if (_playerController.view.dotLoadingObj.activeInHierarchy)
            {
                _playerController.DotAnimationEnd();
            }
            
            bool isCardOpenAll = false;
            for (int i = 0; i < _playerController.spokerPlayerInfo.cardlist.Count; i++)
            {
                if (string.IsNullOrEmpty(_playerController.spokerPlayerInfo.cardlist[i]))
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
            for (int i = 0; i < _playerController.spokerPlayerInfo.cardlist.Count; i++)
            {
                int rankindex = CardRankCalculater.GetCardIndex(_playerController.spokerPlayerInfo.cardlist[i]);
                mycardIndexList.Add(rankindex);
                Extension.eLog($"string value:{_playerController.spokerPlayerInfo.cardlist[i]}//rankindex:{rankindex}", Color.yellow);
            }

            List<int> allCards = new List<int>(mycardIndexList);

            (string cardRankString, var cardValueList) = CardRankCalculater.EvaluateSevenPokerHand(allCards);
            Extension.eLog($"cardRank!{cardRankString}//{string.Join(",", cardValueList)}", Color.cyan);

            List<string> cardrankStringList = new List<string>();
            for (int i = 0; i < cardValueList.Count; i++)
            {
                cardrankStringList.Add(CardRankCalculater.GetCardString(cardValueList[i]));
            }

            //카드 하이라이트 효과(바둑이에선 족보에 들어가지 않는 카드가 하이라이트)
            for (int i = 0; i < _playerController.spokerPlayerInfo.cardlist.Count; i++)
            {
                int index = i;
                bool isExist = cardrankStringList.Any(o => o == _playerController.spokerPlayerInfo.cardlist[index]);
                if (isExist)
                {
                
                }
            }

            //rank viewer set!!!
            if (isShowDown == false)
            {
                //showdown이 아닐때는 내껏 족보만 보여주기
                if (_playerController.isMe)
                {
                    _playerController.SetRankViewer(cardRankString, cardrankStringList);
                }
            }
            else
            {
                _playerController.SetRankViewer(cardRankString, cardrankStringList);
            }


            return cardRankString;
        }
         
        #endregion
        
        
        public void SetShowDownPotInfo(RepeatedField<sevenPoker.ResultNoti.Types.Pot>  _lastPotInfo)
        {
            lastPotInfo = _lastPotInfo;
        }
        
        public void ClearDataInRoundGame()
        {
            foreach (var chip in chipList)
            {
                PoolManager.Push(chip);
            }
            chipList.Clear();
            betDict.Clear();
        }
    }
}
