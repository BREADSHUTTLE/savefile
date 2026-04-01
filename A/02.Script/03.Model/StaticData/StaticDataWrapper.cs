using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Purchasing;

namespace CAPYBARA.Definition
{
    [Serializable]
    public class StaticDataWrapper
    {
        public IAPProduct[] iAPProducts;

        public WebViewURL[] webviewurl;
        public APIUtil aPIUtil;


        public RewardMissionInfo[] rewardMissionInfo;
        public PointsRewardInfo[] rewardPointsInfo;

        public LobbyErrorInfo[] lobbyErrorInfo;
        public ItemNameInfo[] itemNameInfo;
        
        public Localizedname[] localizedname;
        public Localizeddesc[] localizeddesc;
        
        public Dictionary<LocalizeDescKeys,Localizeddesc> localizeddescDict;
        public Dictionary<LocalizeNameKeys,Localizedname> localizednameDict;
    }

    [Serializable]
    public class APIUtil
    {
        public int PaymentLimit;
    }

    [Serializable]
    public class WebViewURL
    {
        public string Announcement;
        public string QAUrl;
    }

    [Serializable]
    public class IAPProduct
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ShopMainTapType tapType;

        [JsonConverter(typeof(StringEnumConverter))]
        public ShopSubTapType subTapType;
        public string productId;
        public string title_Kr;
        public string title_En;
        public string desc_Kr;
        public string desc_En;
        public int discount;
    }



    [Serializable]
    public class RewardMissionInfo
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RewardMissionType rewardId;

        [JsonConverter(typeof(StringEnumConverter))]
        public RewardGameType rewardGameType;

        public string message_Kr;
        public string message_En;
        public string value_Kr;
        public string value_En;
    }

    [Serializable]
    public class MissionRewardValue
    {
        public int GAME_MONEY;
        public string ITEM_ID;
    }
    [Serializable]
    public class MissionRewardCondition
    {
        public int MAX;
        public string PERIOD;
        public bool TYPE_MAX;
    }

    [Serializable]
    public class PointsRewardInfo
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public PointsRewardType rewardId;
        public string message_Kr;
        public string message_En;
    }

    [Serializable]
    public class LobbyErrorInfo
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public lobby.ErrorCode errorCode;
        
        public string message_Kr;
        public string message_En;
    }

    [Serializable]
    public class ItemNameInfo
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemID itemID;
        public string message_Kr;
        public string message_En;
    }

  














    [Serializable]
    public class Localizedname
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LocalizeNameKeys key;
        
        public string kr;
        public string en;

        public string StringToLocal
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Korean)
                {
                    return kr;
                }
                if (Application.systemLanguage == SystemLanguage.English)
                {
                    return en;
                }
                return kr;
            }
        }
    }

    [Serializable]
    public class Localizeddesc
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LocalizeDescKeys key;
        
        public string kr;
        public string en;
        
        public string StringToLocal
        {
            get
            {
                if (Application.systemLanguage == SystemLanguage.Korean)
                {
                    return kr;
                }
                if (Application.systemLanguage == SystemLanguage.English)
                {
                    return en;
                }

                return kr;
            }
        }
        
        
    }
}