using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.badugi;
using Cysharp.Threading.Tasks;
using Google.Protobuf.Collections;
using UnityEngine;
using DG.Tweening;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;
using System.Linq;

namespace CAPYBARA
{
    public class BadugiTableSnapShot
    {
        public badugi.EnterRes RoomImfo { get { return currentRoomInfo; } }
        private badugi.EnterRes currentRoomInfo;
        
        private BadugiViewer badugiView;
        public RepeatedField<badugi.ResultNoti.Types.Pot>  lastPotInfo;
        private Dictionary<int,List<long> > betDict = new Dictionary<int, List<long>>();
        
        private List<Poolable> chipList = new List<Poolable>();
        
        public void Init(BadugiViewer _view)
        {
            badugiView = _view;
        
            
            CPPlayer.Badugi.ThrowAnte += ThrowAnte;
            CPPlayer.Badugi.ThrowChip += ThrowChip;

           //CPPlayer.Badugi.CardRecieved += SetUiAfterCardRecieved;
        }
        
        public void SetGameInfo(badugi.EnterRes enterRes)
        {
            currentRoomInfo = enterRes;
        }
        
         private void ThrowAnte(int chairId,long ante, Transform startPos)
        {
            if(betDict.ContainsKey(chairId)==false)
                betDict.Add(chairId,new List<long>());
            betDict[chairId].Add(ante);
            
            var anteObj = PoolManager.Pop(InGameResourcesBundle.Loaded.anteObject, badugiView.transform, startPos.position);
            anteObj.transform.localScale = Vector3.one;
            anteObj.transform.position= startPos.position;
            anteObj.GetComponent<RectTransform>().DOMove(badugiView.anteArrive.anchoredPosition, 0.5f).OnComplete(() =>
            {
                PoolManager.Push(anteObj);
            }); 
        }
        public void ThrowChip(int chairId,long _chip, Transform startPos)
        {
            return;
            if(betDict.ContainsKey(chairId)==false)
                betDict.Add(chairId,new List<long>());
            betDict[chairId].Add(_chip);

            long chipamount = 0;
            
            chipamount=_chip/CPPlayer.Badugi.initialBuyIn;
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
                
                    var chip = PoolManager.Pop(chipprefab, badugiView.chipParent.transform, startPos.position);
                    chip.transform.position = startPos.position;
                    chip.transform.localScale = Vector3.one;
                    Vector2 randomPos= 
                        badugiView.anteArrive.anchoredPosition + Random.insideUnitCircle * 140f;
           
                    chip.GetComponent<RectTransform>().DOAnchorPos(randomPos, 0.5f).OnComplete(() =>
                    {
                        //PoolManager.Push(chip);
                    }); 
                    chipList.Add(chip);
                }
            }
          
        }
        
        public void CardLocateToPlayerSnapshot(BadugiPlayerController player, HoleCardNoti myCardnoti)
        {
            var obj = InGameResourcesBundle.Loaded.badugiCardPrefab;
            var mycard = PoolManager.Pop(obj, badugiView.transform,
                badugiView.cardStartPos.position);
  
            mycard.transform.position = badugiView.cardStartPos.position;
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr ,false);
            
            player.AddCard(myCardnoti.Card, mycard);
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            mycard.cardInfoIndex = CardRankCalculater.GetCardIndex(myCardnoti.Card);
            
            rt.transform.localPosition = Vector3.zero;
            rt.transform.localScale = Vector3.one;
            rt.transform.localRotation = Quaternion.identity;
            
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
            mycard.cardImage.sprite = CardInfo.cardSprite;
                
            player.GetCard(myCardnoti.Card, mycard);
        }
        
        
        public void CardThrowToPlayer(BadugiPlayerController player, HoleCardNoti myCardnoti)
        {
            Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] 카드 플레이어에게 드로우 chairID:me"); 
            
            var obj = InGameResourcesBundle.Loaded.badugiCardPrefab;
            var mycard = PoolManager.Pop(obj, badugiView.transform,
                badugiView.cardStartPos.position);
          
            mycard.transform.position =badugiView.cardStartPos.position;
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr ,true);
            
            player.AddCard(myCardnoti.Card, mycard);
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            mycard.cardInfoIndex = CardRankCalculater.GetCardIndex(myCardnoti.Card);
            
            float moveTime = (float)CPPlayer.Server.visualEffectTimeConfig["CARD_DEAL_MS"]/1000f;
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOLocalMove(Vector2.zero, moveTime));
            seq.Join(rt.DOScale(Vector3.one, moveTime));
            seq.Join(rt.DOLocalRotate(Vector3.zero, moveTime));
            seq.SetEase(Ease.InOutSine);
            seq.SetLoops(1, LoopType.Restart);
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
                mycard.cardImage.sprite = CardInfo.cardSprite;
                
                player.GetCard(myCardnoti.Card, mycard);
            });
        }
        
        public void CardThrowToOtherPlayerSnapshot(BadugiPlayerController player, HoleCardNotiOther myCardnoti)
        {
            var obj = InGameResourcesBundle.Loaded.badugiCardPrefab;
            var mycard = PoolManager.Pop(obj, badugiView.transform,
                badugiView.cardStartPos.position);
        
            mycard.transform.position = badugiView.cardStartPos.position;
            mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr ,false);
            
            player.AddCard("",mycard);  
            player.GetCard("",mycard);
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            
            rt.localPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation=Quaternion.identity;
            
       
         
        }
        
        public void CardThrowToPlayer(BadugiPlayerController player, HoleCardNotiOther myCardnoti)
        {
            float moveTime = (float)CPPlayer.Server.visualEffectTimeConfig["CARD_DEAL_MS"]/1000f;
            
            Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] 카드 플레이어에게 드로우 chairID:{myCardnoti.ChairId},이동시간:{moveTime}"); 
            
            var obj = InGameResourcesBundle.Loaded.badugiCardPrefab;
            var mycard = PoolManager.Pop(obj, badugiView.transform,
                badugiView.cardStartPos.position);


            mycard.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,700);
            mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr ,true);
            player.AddCard("",mycard);    
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            
            Vector3 startLocal = rt.localPosition;
            Vector3 endLocal = Vector3.zero;
            // startLocal.x > 0 이면 플레이어가 왼쪽, < 0 이면 오른쪽
    
            Vector3 midPoint_0 = new Vector3(Mathf.Lerp(endLocal.x, startLocal.x, 0.8f), Mathf.Lerp(endLocal.y, startLocal.y, 0.8f), 0); 
            Vector3 midPoint = new Vector3(Mathf.Lerp(endLocal.x, startLocal.x, 0.3f), Mathf.Lerp(endLocal.y, startLocal.y, 0.3f), 0); 
            Vector3[] curvePath = new Vector3[] { midPoint_0,midPoint, endLocal };
            
           
            
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOLocalMove(endLocal, moveTime));
            seq.Join(rt.DOScale(Vector3.one, moveTime));
            seq.Join(rt.DOLocalRotate(Vector3.zero, moveTime));
            seq.SetEase(Ease.InOutSine);
            seq.SetLoops(1, LoopType.Restart);
            seq.OnComplete(() =>
            {
                player.GetCard("",mycard);    
            });
         
        }

        
        public async UniTask CardThrowToTop(BadugiPlayerController player, List<int> indexListToChagne,bool isLeftPlayer)
        {
            int waitCardThrowTotalTime = (int)CPPlayer.Server.visualEffectTimeConfig["DRAW_CARD_IN_MS"];
            if (indexListToChagne.Count <= 0)
            {
                return;
            }
            //int waitCardThrowTime = waitCardThrowTotalTime / indexListToChagne.Count;
            float waitCardThrowTimeF=(float)waitCardThrowTotalTime/1000f;
            
            
            List<string> playerCardlist = new List<string>();
            if (player.isMe)
            {
                playerCardlist = player.sortedCardList;
            }
            else
            {
                playerCardlist = player.badugiPlayerInfo.cardlist;
            }
            List<BadugiCardViewer> playerCardViewerlist = new List<BadugiCardViewer>();
            if (player.isMe)
            {
                playerCardViewerlist = player.sortedCardViewerList;
            }
            else
            {
                playerCardViewerlist = player.cardViewerList;
            }

            if (player.isMe)
            {
                for (int i = 0; i < playerCardlist.Count; i++)
                {
                    int cardIndex=player.badugiPlayerInfo.cardlist.IndexOf(playerCardlist[i]);
                    if (indexListToChagne.Contains(cardIndex) == false)
                        continue;
                
                    var mycard=playerCardViewerlist[i];
                    var startPos = mycard.transform.position;
                
                    Transform cardTr = badugiView.cardStartPos;
                    mycard.transform.SetParent(cardTr ,false);
                    mycard.transform.position = startPos;
                
                    var rt = mycard.transform.GetComponent<RectTransform>();
                    mycard.cardInfoIndex =-1;
                
                    Sequence seq = DOTween.Sequence();
                    seq.Append(rt.DOLocalMove(Vector2.zero, waitCardThrowTimeF));
                    seq.Join(rt.DOScale(Vector3.one, waitCardThrowTimeF));
                    seq.Join(rt.DOLocalRotate(Vector3.zero, waitCardThrowTimeF));
                    seq.SetEase(Ease.OutQuad);
                    seq.OnComplete(() =>
                    {
                        //player.badugiPlayerInfo.cardlist[cardindex] = "";
                    });
                    await UniTask.Delay(waitCardThrowTotalTime);
                }
            }
            else
            {
                for (int i = playerCardlist.Count - 1; i >= 0; i--)
                {
                    int index = i;
                    if (isLeftPlayer)
                    {
                        int tempIndex = playerCardlist.Count - indexListToChagne.Count;    
                        if(i<tempIndex)
                            continue;
                    }
                    else
                    {
                        var temp = (playerCardlist.Count-1) - i;
                        if(temp>=indexListToChagne.Count)
                            continue;
                        index = temp;
                    }
                    
                    var mycard=playerCardViewerlist[index];
                    var startPos = mycard.transform.position;
                
                    Transform cardTr = badugiView.cardStartPos;
                    mycard.transform.SetParent(cardTr ,false);
                    mycard.transform.position = startPos;
                
                    var rt = mycard.transform.GetComponent<RectTransform>();
                    mycard.cardInfoIndex =-1;
                
                    Sequence seq = DOTween.Sequence();
                    seq.Append(rt.DOLocalMove(Vector2.zero, waitCardThrowTimeF));
                    seq.Join(rt.DOScale(Vector3.one, waitCardThrowTimeF));
                    seq.Join(rt.DOLocalRotate(Vector3.zero, waitCardThrowTimeF));
                    seq.SetEase(Ease.OutQuad);
                    seq.OnComplete(() =>
                    {
                        //player.badugiPlayerInfo.cardlist[cardindex] = "";
                    });
                    await UniTask.Delay(waitCardThrowTotalTime);
                }
            }
       
        }
        
        public async UniTask CardChangeToPlayer(BadugiPlayerController player, List<int> indexListToChagne,List<string> newCardList,bool leftPlayer)
        {
            int targetCount = indexListToChagne.Count;

            // 변경할 카드가 없으면 바로 완료 처리
            if (targetCount == 0)
            {
                //CPPlayer.Badugi.CardRecieved?.Invoke(player,false);
                return;
            }
            
            int completedCount = 0;
            
            int waitCardThrowTotalTime = (int)CPPlayer.Server.visualEffectTimeConfig["DRAW_CARD_OUT_MS"];
           // int waitCardThrowTime = waitCardThrowTotalTime / indexListToChagne.Count;
            float waitCardThrowTimeF=(float)waitCardThrowTotalTime/1000f;
            
            string cardRank;
            
            
            List<string> playerCardlist = new List<string>();
            if (player.isMe)
            {
                playerCardlist = player.sortedCardList;
            }
            else
            {
                playerCardlist = player.badugiPlayerInfo.cardlist;
            }
            List<BadugiCardViewer> playerCardViewerlist = new List<BadugiCardViewer>();
            if (player.isMe)
            {
                playerCardViewerlist = player.sortedCardViewerList;
            }
            else
            {
                playerCardViewerlist = player.cardViewerList;
            }
            

            int newCardIndex = 0;
            for (int i = 0; i < playerCardlist.Count; i++)
            {
            
                int index = i;
                if (player.isMe)
                {
                    int cardIndex=player.badugiPlayerInfo.cardlist.IndexOf(playerCardlist[i]);
                    if (cardIndex>=0)
                        continue;
                }
                else
                {
                    if (leftPlayer)
                    {
                        var tempIndex = playerCardlist.Count - indexListToChagne.Count;
                        if(i<tempIndex)
                            continue;
                    }
                    else
                    {
                        if(i>=indexListToChagne.Count)
                            continue;
                    }
                    index = i;
               
                }
                
                var mycard=playerCardViewerlist[index];
                int originalCardIdx = 0;
                if (player.isMe)
                {
                    originalCardIdx = player.sortedindex[index];
                }
                else
                {
                    originalCardIdx = index;
                }
               
                Transform cardTr =  player.GetPlayerCardTr( originalCardIdx);
                var startPos = badugiView.cardStartPos.position;
                
                //mycard.transform.position = startPos;
                var rt = mycard.transform.GetComponent<RectTransform>();
                //rt.anchoredPosition=Vector2.zero;
                rt.DOKill();
                mycard.transform.DOKill();
                mycard.cardImage.sprite=InGameResourcesBundle.Loaded.noneImageForCard;
                mycard.transform.position =startPos;
                await UniTask.Yield();
     
               // rt.anchoredPosition = Vector2.zero;
                
                mycard.transform.SetParent(cardTr ,true);
                
                int k = indexListToChagne.IndexOf(originalCardIdx);

                if (player.isMe)
                {
                    if (string.IsNullOrEmpty(newCardList[k ]))
                    {
                        mycard.cardInfoIndex = -1;
                    }
                    else
                    {
                        mycard.cardInfoIndex = CardRankCalculater.GetCardIndex(newCardList[k ]);    
                    }
                }
                else
                {
                    mycard.cardInfoIndex = -1;
                }
             
                Sequence seq = DOTween.Sequence();
                seq.Append(rt.DOAnchorPos(Vector2.zero, waitCardThrowTimeF));
                seq.Join(rt.DOScale(Vector3.one, waitCardThrowTimeF));
                seq.Join(rt.DOLocalRotate(Vector3.zero, waitCardThrowTimeF));
                seq.SetEase(Ease.OutQuad);
                seq.OnComplete(() =>
                {
                    if (player.isMe)
                    {
                        (int rank, Suit suit) = CardRankCalculater.ParseCard(newCardList[k ]);
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
                        mycard.cardImage.sprite = CardInfo.cardSprite;
                    }
                    
                    completedCount++;
                    if (completedCount >= targetCount)
                    {
                        //카드 모두 교환후 콜백 함수 호출
                        if (player.isMe)
                        {
                            cardRank= player.SetUiAfterCardRecieved(false);
                            player.BestRankSet(cardRank);
                        }
                    }
                });

                newCardIndex++;
                await UniTask.Delay(waitCardThrowTotalTime);
            }
            
            
            // for (int i = 0; i < indexListToChagne.Count; i++)
            // {
            //     int index = i;
            //     int cardindex = indexListToChagne[index];
            //     var mycard=player.cardViewerList[cardindex];
            //     
            //     Transform cardTr =  player.GetPlayerCardTr(cardindex);
            //     var startPos = badugiView.cardStartPos.position;
            //     
            //     mycard.transform.position = startPos;
            //     mycard.transform.SetParent(cardTr ,true);
            //     
            //     var rt = mycard.transform.GetComponent<RectTransform>();
            //
            //     if (string.IsNullOrEmpty(newCardList[index]))
            //     {
            //         mycard.cardInfoIndex = -1;
            //     }
            //     else
            //     {
            //         mycard.cardInfoIndex = CardRankCalculater.GetCardIndex(newCardList[index]);    
            //     }
            //     
            //     if (player.isMe)
            //     {
            //         (int rank, Suit suit) = CardRankCalculater.ParseCard(newCardList[index]);
            //         List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
            //         if (CPPlayer.Cloud.optionValue.fourColor)
            //         {
            //             cardlist = InGameResourcesBundle.Loaded.cardResourceList;
            //         }
            //         else
            //         {
            //             cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
            //         }
            //         var CardInfo = cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
            //         mycard.cardImage.sprite = CardInfo.cardSprite;
            //     }
            //     
            //     Sequence seq = DOTween.Sequence();
            //     seq.Append(rt.DOAnchorPos(Vector2.zero, 0.2f));
            //     seq.Join(rt.DOScale(Vector3.one, 0.2f));
            //     seq.Join(rt.DOLocalRotate(Vector3.zero, 0.2f));
            //     seq.SetEase(Ease.OutQuad);
            //     seq.OnComplete(() =>
            //     {
            //         completedCount++;
            //         if (completedCount >= targetCount)
            //         {
            //             //카드 모두 교환후 콜백 함수 호출
            //             if (player.isMe)
            //             {
            //                 cardRank= player.SetUiAfterCardRecieved(false);
            //                 player.BestRankSet(cardRank);
            //             }
            //         }
            //     });
            //     await UniTask.Delay(waitCardThrowTime);
            // }
        }
        
         public void CardChangeToPlayerImmediate(BadugiPlayerController player, List<int> indexListToChagne,List<string> newCardList)
        {
            int targetCount = indexListToChagne.Count;

            // 변경할 카드가 없으면 바로 완료 처리
            if (targetCount == 0)
            {
                //CPPlayer.Badugi.CardRecieved?.Invoke(player,false);
                return;
            }
            
            int completedCount = 0;
            
            for (int i = 0; i < indexListToChagne.Count; i++)
            {
                int index = i;
                int cardindex = indexListToChagne[index ];
                var mycard=player.cardViewerList[cardindex];
                Transform cardTr =  player.GetPlayerCardTr(cardindex);
                var startPos = badugiView.cardStartPos.position;
                
                mycard.transform.position = startPos;
                mycard.transform.SetParent(cardTr ,true);
                
                var rt = mycard.transform.GetComponent<RectTransform>();

                if (string.IsNullOrEmpty(newCardList[index]))
                {
                    mycard.cardInfoIndex = -1;
                }
                else
                {
                    mycard.cardInfoIndex = CardRankCalculater.GetCardIndex(newCardList[index]);    
                }
                
                if (player.isMe)
                {
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(newCardList[index]);
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
                    mycard.cardImage.sprite = CardInfo.cardSprite;
                }
                
                Sequence seq = DOTween.Sequence();
                seq.Append(rt.DOAnchorPos(Vector2.zero, 0.3f));
                seq.Join(rt.DOScale(Vector3.one, 0.3f));
                seq.Join(rt.DOLocalRotate(Vector3.zero, 0.3f));
                seq.SetEase(Ease.OutQuad);
                seq.OnComplete(() =>
                {
                    completedCount++;
                    if (completedCount >= targetCount)
                    {
                        //카드 모두 교환후 콜백 함수 호출
                        if (player.isMe)
                        {
                           string cardRank= player.SetUiAfterCardRecieved(false);
                           player.BestRankSet(cardRank);
                        }
                    }
                });
            }
        }
        
        public void SetCardToOtherPlayerAtEnter(BadugiPlayerController player)
        {
            var obj = InGameResourcesBundle.Loaded.badugiCardPrefab;
            var mycard = PoolManager.Pop(obj, badugiView.transform,
                badugiView.cardStartPos.position);
            
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr,false);
            
            player.AddCard("",mycard);
            player.GetCard("",mycard);
            
            mycard.gameObject.SetActive(true);
            mycard.transform.position = badugiView.cardStartPos.position;
            mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            rt.localPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation=Quaternion.identity;
      
        }
        
        public void SetShowDownPotInfo(RepeatedField<badugi.ResultNoti.Types.Pot>  _lastPotInfo)
        {
            lastPotInfo = _lastPotInfo;
        }
        
      
        public string SetUiAfterCardRecieved(BadugiPlayerController _meController,bool isShowDown=false)
        {
            List<int> mycardIndexList = new List<int>();
            for (int i = 0; i < _meController.badugiPlayerInfo.cardlist.Count; i++)
            {
                int rankindex=CardRankCalculater.GetCardIndex(_meController.badugiPlayerInfo.cardlist[i]);  
                mycardIndexList.Add(rankindex);
                Extension.eLog($"string value:{_meController.badugiPlayerInfo.cardlist[i]}//rankindex:{rankindex}",Color.yellow);
            }
         
            List<int> allCards = new List<int>(mycardIndexList);
            
            (string cardRankString,var cardValueList)=CardRankCalculater.EvaluateBadugiHand(allCards);
            Extension.eLog($"cardRank!{cardRankString}//{string.Join(",",cardValueList)}",Color.cyan);
            
            List<string> cardrankStringList=new List<string>();
            for (int i = 0; i < cardValueList.Count; i++)
            {
                cardrankStringList.Add(CardRankCalculater.GetCardString(cardValueList[i]));
            }
            
            //카드 정렬 로직
            int mod = 13;
            List<int> sortedindex=allCards.Select((v, i) => new { Value = (v % mod)+1==13?0:(v % mod)+1, Index = i })
                .OrderBy(x => x.Value)
                .Select(x => x.Index)
                .ToList();
            Extension.eLog($"정렬!!!!{string.Join(",",_meController.badugiPlayerInfo.cardlist) }//{string.Join(",",mycardIndexList)}",Color.cyan);
            Extension.eLog($"정렬!!!!{string.Join(",",allCards) }//{string.Join(",",sortedindex)}",Color.green);
            
            Transform[] children = new Transform[_meController.view.myCardPos.Length];
            for (int i = 0; i < _meController.view.myCardPos.Length; i++)
            {
                children[i] = _meController.view.myCardPos[i];
            }
            for (int newPos = 0; newPos < sortedindex.Count; newPos++)
            {
                int originalIndex = sortedindex[newPos]; // 원래 자식 인덱스
                Transform child = children[originalIndex];

                child.SetSiblingIndex(newPos);
                child.GetComponent<RectTransform>().anchoredPosition= _meController.view.cardPositions[newPos];
            }
            //카드 정렬 로직
            for (int i = 0; i < _meController.badugiPlayerInfo.cardlist.Count; i++)
            {
                int index = i;
                bool isExist=cardrankStringList.Any(o=>o== _meController.badugiPlayerInfo.cardlist[index]);
                if (!isExist)
                {
                    _meController.HighlightCardforChange(mycardIndexList[index], true);
                }
            }
            
            //rank viewer set!!!
            if (isShowDown == false)
            {
                if (_meController.isMe)
                {
                    _meController.SetRankViewer(cardRankString,cardrankStringList);    
                }
            }
            else
            {
                _meController.SetRankViewer(cardRankString,cardrankStringList);    
            }
            
            
            return cardRankString;
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
