using System;
using System.Collections.Generic;
using System.Threading;
using CAPYBARA;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CAPYBARA.Bundles
{
    [CreateAssetMenu(fileName = "InGameResourcesBundle", menuName = "Bundle/InGameResourcesBundle", order = 504)]
    public class InGameResourcesBundle : ScriptableObject
    {
        public static InGameResourcesBundle Loaded;

        [Header("bet/ante")]
        public GameObject anteObject;
        public GameObject chipObject; 
        public Sprite[] chipSprites;    

        [Header("Card")]
        public CardViewer cardPrefab;
        public BadugiCardViewer badugiCardPrefab;
        public SPokerCardViewer spokerCardPrefab;
        public List<CardInfo> cardResourceList;
        public List<CardInfo> cardResourceList_TwoColor;
        public Sprite noneImageForCard;
        public Sprite[] ingameActionTypeImages;
        public string[] ingameActionTypeImageColor;
        public Sprite[] ingameActionTypeImages_badugi;
        public Sprite[] TimeCountSprites;

        public Sprite[] BadugiChangeRoundNumImage;
        public EmotionInfo[] emotionInfoList;
        public Sprite[] loginTypeIconSprites;

        public Material cardLightEffectMat;
        
        
        [Header("액션버튼 색상 값 정보")]
        public ActionButtonTextColorInfo[] actionButtonTextColorInfo;
        
        private const string BundleResourcekey = "InGameResourcesBundle";
        
        public static AsyncOperationHandle<InGameResourcesBundle> BeginLoad()
        {
            return Addressables.LoadAssetAsync<InGameResourcesBundle>(BundleResourcekey);
        }
        public static async UniTask StartLoadAsset(CancellationTokenSource cts)
        {
            Loaded = await Addressables.LoadAssetAsync<InGameResourcesBundle>(BundleResourcekey).WithCancellation(cts.Token);
        }

        [Serializable]
        public class ActionButtonTextColorInfo
        {
            public HoldemBtnType actionType;
            public GradientColor inactiveColor;
            public GradientColor defaultColor;
            public GradientColor reservedColor;

            [Serializable]
            public class GradientColor
            {
                public Color topColor;
                public Color bottomColor;    
            }
            
        }

        [ContextMenu("Set Alpha 1 to ActionButtonTextColorInfo")]
        public void SetActionButtonColorAlpha()
        {
            if (actionButtonTextColorInfo == null) return;
            foreach (var info in actionButtonTextColorInfo)
            {
                SetAlpha(ref info.inactiveColor.topColor);
                SetAlpha(ref info.inactiveColor.bottomColor);
                SetAlpha(ref info.defaultColor.topColor);
                SetAlpha(ref info.defaultColor.bottomColor);
                SetAlpha(ref info.reservedColor.topColor);
                SetAlpha(ref info.reservedColor.bottomColor);
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log("[InGameResourcesBundle] actionButtonTextColorInfo 알파값 1로 설정 완료");
        }

        private static void SetAlpha(ref Color color) => color.a = 1f;

        [ContextMenu("Set2ColorCardInfo")]
        public void SetCardInfo()
        {
            for (int i = 0; i < 4; i++)
            {
                string path="";
                string suitstr = "";
                switch (i)
                {
                    case 0:
                        path = "cards/hearts/";
                        suitstr = "hearts_";
                        break;
                    case 1:
                        path = "cards/diamonds/4 color_off/";
                        suitstr = "diamonds_";
                        break;
                    case 2:
                        path = "cards/clubs/4 color_off/";
                        suitstr = "clubs_";
                        break;
                    case 3:
                        suitstr = "spades_";
                        path = "cards/spades/";
                        break;
                }
                string rankstr = "";

                for (int j = 2; j < 15; j++)
                {
                    rankstr = j.ToString();
                    if (j == 11)
                    {
                        rankstr = "J";
                    }
                    if (j == 12)
                    {
                        rankstr = "Q";
                    }
                    if (j == 13)
                    {
                        rankstr = "K";
                    }
                    if (j == 14)
                    {
                        rankstr = "A";
                    }
                    
                    if (i == 1 || i == 2)
                    {
                        rankstr = rankstr+"_4color_off";
                    }
                    
                    string resourcePath = path+suitstr+rankstr;
                    Sprite spr=Resources.Load<Sprite>(resourcePath);
                    var cardInfo = new CardInfo();
                    cardInfo.cardSprite = spr;
                    cardInfo.cardSuit= (CAPYBARA.Suit)i;
                    cardInfo.rankValue=j;
                    cardResourceList_TwoColor.Add(cardInfo);
                }
                
            }
        }
        
        [ContextMenu("Set4ColorCardInfo")]
        public void Set4CardInfo()
        {
            for (int i = 0; i < 4; i++)
            {
                string path="";
                string suitstr = "";
                switch (i)
                {
                    case 0:
                        path = "cards/hearts/";
                        suitstr = "hearts_";
                        break;
                    case 1:
                        path = "cards/diamonds/4 color_on/";
                        suitstr = "diamonds_";
                        break;
                    case 2:
                        path = "cards/clubs/4 color_on/";
                        suitstr = "clubs_";
                        break;
                    case 3:
                        suitstr = "spades_";
                        path = "cards/spades/";
                        break;
                }
                string rankstr = "";

                for (int j = 2; j < 15; j++)
                {
                    rankstr = j.ToString();
                    if (j == 11)
                    {
                        rankstr = "J";
                    }
                    if (j == 12)
                    {
                        rankstr = "Q";
                    }
                    if (j == 13)
                    {
                        rankstr = "K";
                    }
                    if (j == 14)
                    {
                        rankstr = "A";
                    }
                    
                    if (i == 1 || i == 2)
                    {
                        rankstr = rankstr+"_4color_on";
                    }
                    
                    string resourcePath = path+suitstr+rankstr;
                    Sprite spr=Resources.Load<Sprite>(resourcePath);
                    var cardInfo = new CardInfo();
                    cardInfo.cardSprite = spr;
                    cardInfo.cardSuit= (CAPYBARA.Suit)i;
                    cardInfo.rankValue=j;
                    cardResourceList.Add(cardInfo);
                }
                
            }
        }
    }

    [Serializable]
    public class CardInfo
    {
        public CAPYBARA.Suit cardSuit;
        public int  rankValue;
        public Sprite  cardSprite;
    }

 
}

