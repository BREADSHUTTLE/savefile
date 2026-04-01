using System;
using System.Collections.Generic;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using Google.Protobuf.Collections;
using UnityEngine;
using DG.Tweening;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;
using System.Linq;
using CAPYBARA.Bundles;

namespace CAPYBARA 
{

    //table의 정보를 담아두고 있는 클래스.테이블에서 이루어지는 클라(로컬)로직은 여기서 수행(서버로직 이벤트 말고) 
    public class HoldemTableSnapShot
    {
        public holdem.EnterRes RoomImfo { get { return currentRoomInfo; } }
        private holdem.EnterRes currentRoomInfo;
        
        private List<string> communityCards = new List<string>();
        private List<CardViewer> communityCardViewerList = new List<CardViewer>();
        
        private HoldemViewer holdemView;
        public RepeatedField<holdem.ResultNoti.Types.Pot>  lastPotInfo;
        private Dictionary<int,List<long> > betDict = new Dictionary<int, List<long>>();
        
        private List<Poolable> chipList = new List<Poolable>();
        
        /// <summary>
        /// 생성자 초기화 겸(게임 최초시작시)
        /// </summary>
        public void Init(HoldemViewer view)
        {
            holdemView = view;
            var ante = InGameResourcesBundle.Loaded.anteObject;
            PoolManager.CreatePool(ante,5);
            
            CPPlayer.Holdem.CardRecieved += SetUiAfterMyCardRecieved;
            CPPlayer.Option.FourCardModeChange+=CardColorModeChange;
        }
        
        //테이블 입장시(테이블 들어가서 게임 시작전 초기화)
        public void SetGameInfo(holdem.EnterRes enterRes)
        {
            currentRoomInfo = enterRes;
        }

        public void ClearDataInRoundGame()
        {
            foreach (var chip in chipList)
            {
                PoolManager.Push(chip);
            }

            foreach (var communitycard in communityCardViewerList)
            {
                communitycard.Inactive();
                PoolManager.Push(communitycard);
            }
            chipList.Clear();
            communityCardViewerList.Clear();
            communityCards.Clear();
            betDict.Clear();
        }

        public void ThrowAnte(int chairId,long ante, Transform startPos)
        {
            if(betDict.ContainsKey(chairId)==false)
                betDict.Add(chairId,new List<long>());
            betDict[chairId].Add(ante);
            
            var anteObj = PoolManager.Pop(InGameResourcesBundle.Loaded.anteObject, holdemView.transform, startPos.position);
            anteObj.transform.localScale = Vector3.one;
            anteObj.transform.position= startPos.position;
            anteObj.GetComponent<RectTransform>().DOAnchorPos(holdemView.anteArrive.anchoredPosition, 0.5f).OnComplete(() =>
            {
                PoolManager.Push(anteObj);
            }); 
        }

        public void LocateAnteSnapShot(int chairId, long ante, Transform startPos)
        {
            //anteobject는 어차피 사라지므로 아무것도 안해도 됨
        }
        
        
        public void ThrowChip(int chairId,long _chip, Transform startPos)
        {
            return;
            if(betDict.ContainsKey(chairId)==false)
                betDict.Add(chairId,new List<long>());
            betDict[chairId].Add(_chip);

            long chipamount = 0;
            
            chipamount=_chip/CPPlayer.Holdem.initialBuyIn;
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
                
                    var chip = PoolManager.Pop(chipprefab, holdemView.chipParent.transform, startPos.position);
                    chip.transform.position = startPos.position;
                    chip.transform.localScale = Vector3.one;
                    Vector2 randomPos= 
                        holdemView.anteArrive.anchoredPosition + Random.insideUnitCircle * 140f;
                    //Extension.eLog($"chip start pos: {startPos.GetComponent<RectTransform>().anchoredPosition}",Color.cyan);
                    //Extension.eLog($"chip arrive pos: {randomPos}",Color.cyan);
                    chip.GetComponent<RectTransform>().DOAnchorPos(randomPos, 0.5f).OnComplete(() =>
                    {
                        //PoolManager.Push(chip);
                    }); 
                    chipList.Add(chip);
                }
            }
          
        }

        public void CardThrowToPlayer(HoldemPlayerController player, HoleCardNoti myCardnoti)
        {
            
            var obj = InGameResourcesBundle.Loaded.cardPrefab;
            var mycard = PoolManager.Pop(obj, holdemView.transform,
                holdemView.cardStartPos.position);
            
            mycard.transform.position = holdemView.cardStartPos.position;
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr ,false);
            
            player.AddCard(myCardnoti.Card, mycard);
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            mycard.cardInfoIndex = CardRankCalculater.GetCardIndex(myCardnoti.Card);
            
            AudioManager.Instance.Play(AudioSourceKey.Dealcard_0);
            
         
            
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOLocalMove(Vector2.zero, 0.2f));
            seq.Join(rt.DOScale(Vector3.one, 0.2f));
            seq.Join(rt.DOLocalRotate(Vector3.zero, 0.2f));
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
                
                var CardInfo = cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
                mycard.cardImage.sprite = CardInfo.cardSprite;
                player.GetCard(myCardnoti.Card, mycard);
            });
        }

        public void CardLocateToPlayerSnapshot(HoldemPlayerController player, HoleCardNoti myCardnoti)
        {
            var obj = InGameResourcesBundle.Loaded.cardPrefab;
            var mycard = PoolManager.Pop(obj, holdemView.transform,
                holdemView.cardStartPos.position);
           
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr ,false);
            
            player.AddCard(myCardnoti.Card, mycard);
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            mycard.cardInfoIndex = CardRankCalculater.GetCardIndex(myCardnoti.Card);
            AudioManager.Instance.Play(AudioSourceKey.Dealcard_0);
      
           
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
            var CardInfo = cardlist.Find(o => o.cardSuit == suit && o.rankValue == rank);
            mycard.cardImage.sprite = CardInfo.cardSprite;
            player.GetCard(myCardnoti.Card, mycard);
        }
        
        public void CardThrowToPlayer(HoldemPlayerController player, HoleCardNotiOther otherCardnoti)
        {
            var obj = InGameResourcesBundle.Loaded.cardPrefab;
            var mycard = PoolManager.Pop(obj, holdemView.transform,
                holdemView.cardStartPos.position);
      
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr,false);
            
            player.AddCard("", mycard);
                        
            mycard.transform.position = holdemView.cardStartPos.position;
            mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOLocalMove(Vector2.zero, 0.2f));
            seq.Join(rt.DOScale(Vector3.one, 0.2f));
            seq.Join(rt.DOLocalRotate(Vector3.zero, 0.2f));
            seq.SetEase(Ease.OutQuad);
            seq.OnComplete(() =>
            {
                player.GetCard("",mycard);    
            });
        }
        public void CardThrowToOtherPlayerSnapshot(HoldemPlayerController player, HoleCardNotiOther otherCardnoti)
        {
            var obj = InGameResourcesBundle.Loaded.cardPrefab;
            var mycard = PoolManager.Pop(obj, holdemView.transform,
                holdemView.cardStartPos.position);
      
            
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr,false);
            
            player.AddCard("", mycard);
            player.GetCard("",mycard);    
            
            mycard.transform.position = holdemView.cardStartPos.position;
            mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            
        
            rt.transform.localPosition = Vector3.zero;
            rt.transform.localScale = Vector3.one;
            rt.transform.localRotation = Quaternion.identity;
     
        }

        public void SetCardToMeAtEnter(HoldemPlayerController player, string cardinfo)
        {
            var obj = InGameResourcesBundle.Loaded.cardPrefab;
            var mycard = PoolManager.Pop(obj, holdemView.transform,
                holdemView.cardStartPos.position);
  
            
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr,false);
            
            player.AddCard(cardinfo, mycard);
            
            mycard.gameObject.SetActive(true);
            mycard.transform.position = holdemView.cardStartPos.position;
            mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            rt.localPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation=Quaternion.identity;
            
            (int rank, Suit suit) = CardRankCalculater.ParseCard(cardinfo);
                
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
            
            
            player.GetCard(cardinfo,mycard);
        }
        
        public void SetCardToOtherPlayerAtEnter(HoldemPlayerController player)
        {
            var obj = InGameResourcesBundle.Loaded.cardPrefab;
            var mycard = PoolManager.Pop(obj, holdemView.transform,
                holdemView.cardStartPos.position);
            
            Transform cardTr = player.GetPlayerCardTr();
            mycard.transform.SetParent(cardTr,false);
            
            player.AddCard("", mycard);
            
            mycard.gameObject.SetActive(true);
            mycard.transform.position = holdemView.cardStartPos.position;
            mycard.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
            
            var rt = mycard.transform.GetComponent<RectTransform>();
            rt.localPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation=Quaternion.identity;
            
            player.GetCard("",mycard);
        }

        public async UniTask SetCommunityCardsAndSetPlayerRankAsync(holdem.CommunityCardsNoti _communityCardsNoti,HoldemPlayerController _meController,bool isShowdown,bool isResult,bool cardNori)
        {
            //ui Set!!!
            await PresntationCommunityCardsAsync(_communityCardsNoti);
            SetJokboRankAndCardHighlight(_meController,CardUISetState.GetCommunityCard,isShowdown);
        }
        
        public void SetCommunityCardsAndSetPlayerRank(holdem.CommunityCardsNoti _communityCardsNoti,HoldemPlayerController _meController,bool isShowdown,bool isResult,bool cardNori)
        {
            //ui Set!!!
            PresntationCommunityCards(_communityCardsNoti);
            SetJokboRankAndCardHighlight(_meController,CardUISetState.GetCommunityCard,isShowdown);
        }

        //족보랭크보여주고 카드 하이라이트
        public string SetJokboRankAndCardHighlight(HoldemPlayerController _Controller,CardUISetState cardUIstate,bool isShowDown)
        {
            bool isResult = cardUIstate == CardUISetState.AfterResult;
            (string cardRankString, List<string> cardrankStringList) = CalculateCardRankAndCommunityCardHighlight(_Controller, isResult);
            //rank viewer set!!!
            
            _Controller.SetJokboRankAndCardHighlight(cardRankString,cardrankStringList,cardUIstate,isShowDown,isResult);    
            
            return cardRankString;
        }

        
        //처음 카드 받았을때 콜백 함수(2장 받았을시의 UI갱신)
        public string SetUiAfterMyCardRecieved(HoldemPlayerController _meController,bool isResult=false)
        {
            (string cardRankString, List<string> cardrankStringList) = CalculateCardRankAndCommunityCardHighlight(_meController,isResult);
            //rank viewer set!!!
            if (isResult == false)
            {
                if (_meController.isMe)
                {
                    _meController.SetJokboRankAndCardHighlight(cardRankString,cardrankStringList,CardUISetState.GetPreflopCard,false);
                    _meController.SetCardStarRank(CardUISetState.GetPreflopCard);
                }
            }
            else
            {
                _meController.SetJokboRankAndCardHighlight(cardRankString,cardrankStringList,CardUISetState.GetPreflopCard,false);    
            }
            return cardRankString;
        }
        

        private (string cardRankString, List<string> cardValueList) CalculateCardRankAndCommunityCardHighlight(HoldemPlayerController _meController,bool isResult=false)
        {
             bool isCardOpenAll = false;
            for (int i = 0; i < _meController.holdemPlayerInfo.cardlist.Count; i++)
            {
                if (string.IsNullOrEmpty(_meController.holdemPlayerInfo.cardlist[i]))
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
                return (null,null);
            List<int> mycardIndexList = new List<int>();
            for (int i = 0; i < _meController.holdemPlayerInfo.cardlist.Count; i++)
            {
                int rankindex=CardRankCalculater.GetCardIndex(_meController.holdemPlayerInfo.cardlist[i]);  
                mycardIndexList.Add(rankindex);
               //Extension.eLog($"string value:{_meController.holdemPlayerInfo.cardlist[i]}//rankindex:{rankindex}",Color.yellow);
            }
            List<int> communitycardIndexList = new List<int>();
            for (int i = 0; i < communityCards.Count; i++)
            {
                int rankindex=CardRankCalculater.GetCardIndex(communityCards[i]);  
                communitycardIndexList.Add(rankindex);
            }

            List<int> allCards = new List<int>(mycardIndexList);
            allCards.AddRange(communitycardIndexList);
            
            (string cardRankString,var cardValueList)=CardRankCalculater.EvaluateHandLocalizedInHoldem(allCards);
            Extension.eLog($"cardRank!{cardRankString}//{string.Join(",",cardValueList)}",Color.cyan);
            
            List<string> cardrankStringList=new List<string>();
            for (int i = 0; i < cardValueList.Count; i++)
            {
                cardrankStringList.Add(CardRankCalculater.GetCardString(cardValueList[i]));
            }
            var matchedIndices = communityCards
                .Select((value, index) => new { value, index })
                .Where(x => cardrankStringList.Contains(x.value))
                .Select(x => x.index)
                .ToList();
            
            if(_meController.isFolded)
                return (cardRankString, cardrankStringList);

            if (cardrankStringList.Count > 1)
            {
                if (isResult)
                {
                    if (_meController.isWin)
                    {
                        for (int i = 0; i < communityCardViewerList.Count; i++)
                        {
                            if (matchedIndices.Contains(i))
                            {
                                communityCardViewerList[i].HighLightCardCallback(communityCardViewerList[i].cardInfoIndex,true);
                            }
                            else
                            {
                                communityCardViewerList[i].HighLightCardCallback(communityCardViewerList[i].cardInfoIndex,false);    
                            }
                        }
                    
                    
                    }
                
                }
                else
                {
                    if (_meController.isMe)
                    {
                        for (int i = 0; i < communityCardViewerList.Count; i++)
                        {
                            if (matchedIndices.Contains(i))
                            {
                                communityCardViewerList[i].HighLightCardCallback(communityCardViewerList[i].cardInfoIndex,true);
                            }
                            else
                            {
                                communityCardViewerList[i].HighLightCardCallback(communityCardViewerList[i].cardInfoIndex,false);    
                            }
                        }

                    
                    }  
                }
            }
          

            return (cardRankString, cardrankStringList);
        }

        
        
        private async UniTask PresntationCommunityCardsAsync(holdem.CommunityCardsNoti _communityCardsNoti)
        {
            bool isFirst = communityCards.Count == 0;

            List<CardViewer> currentCardList = new List<CardViewer>();
            if (isFirst)
            {
                List<CardInfo> tempcardInfoList=new List<CardInfo>();
                int tempindex = 0;
                foreach (var card in _communityCardsNoti.Cards)
                {
                    var obj = PoolManager.Pop(InGameResourcesBundle.Loaded.cardPrefab, holdemView.transform,
                        holdemView.cardStartPos.position);
                    obj.InitCardSet();
                    
                    obj.transform.SetParent(holdemView.communityCardParent,false);
                    obj.transform.localScale = Vector3.one;
                    obj.transform.position= holdemView.cardStartPos.position;
                    
                    var c = obj.cardImage.color;
                    c.a = 0f;
                    obj.cardImage.color = c;
                    
                    (int rank,Suit suit)= CardRankCalculater.ParseCard(card);
                    List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    if (CPPlayer.Cloud.optionValue.fourColor)
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    }
                    else
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                    }

                    var cardInfo=cardlist.Find(o => o.cardSuit == suit&&o.rankValue==rank);
                    obj.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;

                    var startpos=holdemView.communityCardPos[0].GetComponent<RectTransform>().anchoredPosition;
                    startpos.x=startpos.x+tempindex*10;
                    tempindex++;
                    obj.transform.GetComponent<RectTransform>().anchoredPosition = startpos;
                    obj.cardImage.DOFade(1.0f,(float)CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FADE_IN_MS"]/1000f);
                    
                    AudioManager.Instance.Play(AudioSourceKey.Dealcard_0);
                    
                    communityCardViewerList.Add(obj);
                    tempcardInfoList.Add(cardInfo);
                    communityCards.Add(card);
                    currentCardList.Add(obj);
                }
               
                    
                await UniTask.Delay((int)(CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FADE_IN_MS"]));

                float flipTIme = ((float)CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FLIP_MS"] / 1000f);
                //회전
                for (int i = 0; i < communityCardViewerList.Count; i++)
                {
                    int index = i;
                    communityCardViewerList[index].transform.DOScaleX(0,  flipTIme/2.0f).OnComplete(() =>
                    {
                        communityCardViewerList[index].cardImage.sprite =tempcardInfoList[index].cardSprite;
                        communityCardViewerList[index].transform.DOScaleX(1, flipTIme/2.0f);
                    });
                }
                
                await UniTask.Delay((int)(CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FLIP_MS"]));
                
                for (int i = 0; i < communityCardViewerList.Count; i++)
                {
                    int index = i;
                    communityCardViewerList[index].transform.DOMove(holdemView.communityCardPos[index].position, (float)CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FLOP_SLIDE_MS"]/1000f);
                }
                await UniTask.Delay((int)(CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FLOP_SLIDE_MS"]));
            }
            else
            {
                List<CardInfo> tempcardInfoList=new List<CardInfo>();
                int positionIndex = communityCards.Count;
                foreach (var card in _communityCardsNoti.Cards)
                {
                    var obj = PoolManager.Pop(InGameResourcesBundle.Loaded.cardPrefab, holdemView.transform,
                        holdemView.cardStartPos.position);
                    obj.InitCardSet();
                    
                    obj.transform.SetParent(holdemView.communityCardParent,false);
                    obj.transform.localScale = Vector3.one;
                    obj.transform.position= holdemView.cardStartPos.position;
                    
                    var c = obj.cardImage.color;
                    c.a = 0f;
                    obj.cardImage.color = c;
                    
                    (int rank,Suit suit)= CardRankCalculater.ParseCard(card);
                    List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    if (CPPlayer.Cloud.optionValue.fourColor)
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    }
                    else
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                    }
                    var cardInfo=cardlist.Find(o => o.cardSuit == suit&&o.rankValue==rank);
                    obj.cardImage.sprite = InGameResourcesBundle.Loaded.noneImageForCard;
                    obj.transform.transform.position = holdemView.communityCardPos[positionIndex].position;
                    obj.cardImage.DOFade(1.0f,(float)CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FADE_IN_MS"]/1000f);
                    
                    AudioManager.Instance.Play(AudioSourceKey.Dealcard_0);
                    
                    communityCardViewerList.Add(obj);
                    currentCardList.Add(obj);
                    tempcardInfoList.Add(cardInfo);
                    communityCards.Add(card);
                }
                await UniTask.Delay((int)CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FADE_IN_MS"]);
                
                float flipTIme = ((float)CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FLIP_MS"] / 1000f);
                for (int i = 0; i < currentCardList.Count; i++)
                {
                    int index = i;
                    currentCardList[index].transform.DOScaleX(0, flipTIme/2.0f).OnComplete(() =>
                    {
                        currentCardList[index].cardImage.sprite =tempcardInfoList[index].cardSprite;
                        currentCardList[index].transform.DOScaleX(1, flipTIme/2.0f);
                    });
                }
                await UniTask.Delay((int)(CPPlayer.Server.visualEffectTimeConfig["COMMUNITY_FLIP_MS"]));
            }
        }

        private void PresntationCommunityCards(holdem.CommunityCardsNoti _communityCardsNoti)
        {
            bool isFirst = communityCards.Count == 0;

            List<CardViewer> currentCardList = new List<CardViewer>();
            if (isFirst)
            {
                List<CardInfo> tempcardInfoList=new List<CardInfo>();
                int tempindex = 0;
                foreach (var card in _communityCardsNoti.Cards)
                {
                    var obj = PoolManager.Pop(InGameResourcesBundle.Loaded.cardPrefab, holdemView.transform,
                        holdemView.cardStartPos.position);
                    obj.InitCardSet();
                    
                    obj.transform.SetParent(holdemView.communityCardParent,false);
                    obj.transform.localScale = Vector3.one;
                    obj.transform.position= holdemView.cardStartPos.position;
                    
                    var c = obj.cardImage.color;
                    c.a = 1f;
                    obj.cardImage.color = c;
                    
                    (int rank,Suit suit)= CardRankCalculater.ParseCard(card);
                    List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    if (CPPlayer.Cloud.optionValue.fourColor)
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    }
                    else
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                    }

                    var cardInfo=cardlist.Find(o => o.cardSuit == suit&&o.rankValue==rank);
                    
                    communityCardViewerList.Add(obj);
                    tempcardInfoList.Add(cardInfo);
                    communityCards.Add(card);
                    currentCardList.Add(obj);
                }
                for (int i = 0; i < communityCardViewerList.Count; i++)
                {
                    int index = i;
                    communityCardViewerList[index].cardImage.sprite =tempcardInfoList[index].cardSprite;
                }
                
                for (int i = 0; i < communityCardViewerList.Count; i++)
                {
                    int index = i;
                    communityCardViewerList[index].transform.position = holdemView.communityCardPos[index].position;
                }
            }
            else
            {
                List<CardInfo> tempcardInfoList=new List<CardInfo>();
                int positionIndex = communityCards.Count;
                foreach (var card in _communityCardsNoti.Cards)
                {
                    var obj = PoolManager.Pop(InGameResourcesBundle.Loaded.cardPrefab, holdemView.transform,
                        holdemView.cardStartPos.position);
                    obj.InitCardSet();
                    
                    obj.transform.SetParent(holdemView.communityCardParent,false);
                    obj.transform.localScale = Vector3.one;
                    
                    var c = obj.cardImage.color;
                    c.a = 1f;
                    obj.cardImage.color = c;
                    
                    (int rank,Suit suit)= CardRankCalculater.ParseCard(card);
                    List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    if (CPPlayer.Cloud.optionValue.fourColor)
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                    }
                    else
                    {
                        cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                    }
                    var cardInfo=cardlist.Find(o => o.cardSuit == suit&&o.rankValue==rank);
                    obj.transform.position = holdemView.communityCardPos[positionIndex].position;
                    
                    communityCardViewerList.Add(obj);
                    currentCardList.Add(obj);
                    tempcardInfoList.Add(cardInfo);
                    communityCards.Add(card);
                }
                
                for (int i = 0; i < currentCardList.Count; i++)
                {
                    int index = i;
                    currentCardList[index].cardImage.sprite =tempcardInfoList[index].cardSprite;
                }
                
                for (int i = 0; i < communityCardViewerList.Count; i++)
                {
                    int index = i;
                    communityCardViewerList[index].transform.position = holdemView.communityCardPos[index].position;
                }
            }
        }
        
        public void SetCommunityCardsOnEnter(EnterRes enterRes)
        {
            int index = 0;
            foreach (var card in enterRes.CommunityCards)
            {
                var obj = PoolManager.Pop(InGameResourcesBundle.Loaded.cardPrefab, holdemView.transform,
                    holdemView.cardStartPos.position);
                obj.InitCardSet();
                
                (int rank,Suit suit)= CardRankCalculater.ParseCard(card);
                List<CardInfo> cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                if (CPPlayer.Cloud.optionValue.fourColor)
                {
                    cardlist = InGameResourcesBundle.Loaded.cardResourceList;
                }
                else
                {
                    cardlist = InGameResourcesBundle.Loaded.cardResourceList_TwoColor;
                }
                var cardInfo=cardlist.Find(o => o.cardSuit == suit&&o.rankValue==rank);
                obj.transform.SetParent(holdemView.communityCardParent,false);
                obj.cardImage.sprite = cardInfo.cardSprite;
                
                communityCardViewerList.Add(obj);
                communityCards.Add(card);

                obj.transform.position = holdemView.communityCardPos[index].position;
                index++;
            }
        }

        public bool IsCommunityCardsCorrect(RepeatedField<string> _communityCards)
        {
            bool isCorrect = true;
            foreach (var communityCard in _communityCards)
            {
                string cardData = "";
                cardData=communityCards.Find(o => o == communityCard);
                if (string.IsNullOrEmpty(cardData))
                {
                    isCorrect = false;
                    break;
                }
            }

            return isCorrect;
        }

        public void SetResultPotInfo(RepeatedField<holdem.ResultNoti.Types.Pot>  _lastPotInfo)
        {
            lastPotInfo = _lastPotInfo;
        }
        
        private void CardColorModeChange(bool isFourColor)
        {
            if (isFourColor)
            {
                for (int i = 0; i < communityCards.Count; i++)
                {
                    if(string.IsNullOrEmpty( communityCards[i]))
                        continue;
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(communityCards[i]);
                    
                    var CardInfo = InGameResourcesBundle.Loaded.cardResourceList.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    communityCardViewerList[i].cardImage.sprite = CardInfo.cardSprite;
                }
            }
            else
            {
                for (int i = 0; i < communityCards.Count; i++)
                {      if(string.IsNullOrEmpty( communityCards[i]))
                        continue;
                    (int rank, Suit suit) = CardRankCalculater.ParseCard(communityCards[i]);
                    var CardInfo = InGameResourcesBundle.Loaded.cardResourceList_TwoColor.Find(o => o.cardSuit == suit && o.rankValue == rank);
                    communityCardViewerList[i].cardImage.sprite = CardInfo.cardSprite;
                }
            }
        }


        public void SetSnapShotPlayer(HoldemPlayerController mePlayerController)
        {
            mePlayerController.PresentPlayerInfo();
        }

   
    }
}

