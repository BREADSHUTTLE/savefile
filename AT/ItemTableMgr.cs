using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using JaykayFramework;
using System.Linq;
using TangentFramework;

#region CSV objects
public class ItemCSV
{
    public int ItemId;
    public string Name;
    public string EvolveName;
    public string ItemType;
    public string ItemTypeName;
    public int Grade;
    public string InGameItemType;
    public string InGameItemSubType;
    public int LevelLimit;
    public string QuestLimit;
    public string QuestLimitItemType;
    public int QuestLimitItemCount;
    public int AbsoluteQuestLimit;
    public int PriceItemId;
    public int Price;
    public long CashPrices;
    public long ExtractsPrices;
    public long SalePrices;
    public int Salable;
    public string PrefabName;
    public int ColliderSize;
    public int ScaleSize;
    public string AssetBundleName;
    public string Icon;
    public string EvolveIcon;
    public int Sort;
    public string Description;
    public int CoinPrices_EventFactor;
    public float InGamePosY;
    public string CollectMethod;
    public int EventMark;
    public int NewMark;
    public string OriginalCharacterName;
   
}

public class ItemAbilityCSV
{
    public int ItemAbilityId;
    public int ItemId;
    public int AbilityId;
    public string EnhanceType;
    public int OpenLevel;
    public int OpenIfEvolved;
    public string AbilitySkillDescription;
    public string AbilitySkillCaptionDescription;
    public int DefaultLevel;
    public int MaxLevel;
}

public class ItemAbilityValueCSV
{
    public int ItemAbilityId;
    public int EnhanceLevel;
    public int TriggerConditionValue1;
    public int TriggerConditionValue2;
    public int Value1;
    public int Value2;
    public int UseAblePercent;
    public int AvailableNumber;
    public int ExecuteChance;
    public int MinCoolTime;
    public int MaxCoolTime;
    public int EffectiveTime;
    public int AbilityMaxTime;
}

public class AbilityCSV
{
    public int AbilityId;
    public string Name;
    public string TriggerType;
    public string Effect;
    public string TriggerCondition;
    public string Target;
    public int SpecialTarget_ItemId;
    public int Grade;
    public string Icon;
}


public class WhatNeedToEnhanceCSV
{
    public string ItemType;
    public int Grade;
    public int EnhanceLevel;
    public int CoinPrices;
    public int CashPrices;
}

public class ChanceOfGreatSuccessCSV
{
    public string ItemType;
    public int Grade;
    public int Level;
    public int ChanceFromCoin;
    public int ChanceFromCash;
}

public class TreasureEvolveResultCSV
{
    public int ItemId;
    public int ItemAbilityId;
    public string ValueType;
    public int Value;
    public int Chance;
    public string ItemEvolveDescription;
}

#endregion


#region Data objects
/// <summary>
/// 아이템 정보
/// 아이템의 기본정보와 Ability, 아이템 등급별 Enhance와 Evolve조건, 레벨업 대성공확률을 가진다.
/// </summary>
public class ItemInfo
{
    public int ItemId;
    public string Name;
    public string EvolveName;
    public ItemTableMgr.ItemInfo_ItemType ItemType;
    public string ItemTypeName;
    public int Grade;
    public TomInGameObject.TYPE InGameItemType;
    public TomInGameObject.ItemSubType InGameItemSubType;
    public ItemTableMgr.CharacterType characterType;
    public int LevelLimit;
    public int[] QuestLimit;
    public ItemTableMgr.ItemInfo_ItemType QuestLimitItemType;
    public int QuestLimitItemCount;
    public int AbsoluteQuestLimit;
    public int PriceItemId;
    public int Price;
    public long CashPrices;
    public long ExtractsPrices;
    public long SalePrices;
    public int Salable;
    public string PrefabName;
    public int ColliderSize;
    public int ScaleSize;
    public string AssetBundleName;
    public string Icon;
    public string EvloveIcon;
    public int Sort;
    public string Description;
    public int CoinPrices_EventFactor;
    public float InGamePosY;
    public List<string> CollectMethod;
    public int EventMark;
    public int NewMark;
    public ItemTableMgr.OriginalCharacter OriginalCharacterName;
    public bool isFemale;

    public List<ItemAbilityInfo> ListItemAbility;
    public List<WhatNeedToEnhanceInfo> ListWhatNeedToEnhance;
    public List<WhatNeedToEvolveInfo> ListWhatNeedToEvolve_Coin;
    public List<WhatNeedToEvolveInfo> ListWhatNeedToEvolve_Cash;
    public List<WhatNeedToBreakLimitInfo> ListWhatNeedToBreakLimit;
    public List<ChanceOfGreatSuccessInfo> ListChanceOfGreatSuccess;

    // OnlyTreasure
    public TreasureEvolveSupporterInfo TreasureEvolveSupporter;
    public List<TreasureEvolveResultInfo> ListTreasureEvolveResult;

    public ItemInfo(ItemCSV csvinfo)
    {
        ItemId = csvinfo.ItemId;
        Name = csvinfo.Name;
        EvolveName = csvinfo.EvolveName;
        if(TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.ItemInfo_ItemType), csvinfo.ItemType))
        {
            ItemType = (ItemTableMgr.ItemInfo_ItemType)Enum.Parse(typeof(ItemTableMgr.ItemInfo_ItemType), csvinfo.ItemType);
        }
        else
            ItemType = ItemTableMgr.ItemInfo_ItemType.None;

        ItemTypeName = csvinfo.ItemTypeName;
        Grade = csvinfo.Grade;
        if(csvinfo.InGameItemType == null || csvinfo.InGameItemType == string.Empty)
        {
            InGameItemType = TomInGameObject.TYPE.None;
        }
        else
        {
            if (TangentFramework.Utils.IsEnumParseName(typeof(TomInGameObject.TYPE), csvinfo.InGameItemType))
            {
                InGameItemType = (TomInGameObject.TYPE)Enum.Parse(typeof(TomInGameObject.TYPE), csvinfo.InGameItemType);
            }
            else
            {
                InGameItemType = TomInGameObject.TYPE.None;
            }
        }

        if(csvinfo.InGameItemSubType == null || csvinfo.InGameItemSubType == string.Empty)
        {
            InGameItemSubType = TomInGameObject.ItemSubType.None;
        }
        else
        {
            if (TangentFramework.Utils.IsEnumParseName(typeof(TomInGameObject.ItemSubType), csvinfo.InGameItemSubType))
            {
                InGameItemSubType = (TomInGameObject.ItemSubType)Enum.Parse(typeof(TomInGameObject.ItemSubType), csvinfo.InGameItemSubType);
            }
            else
            {
                InGameItemSubType = TomInGameObject.ItemSubType.None;
            }
        }

        LevelLimit = csvinfo.LevelLimit;
        QuestLimit = TangentFramework.Utils.ConvertToIntList(csvinfo.QuestLimit);
        if (csvinfo.QuestLimitItemType == null || csvinfo.QuestLimitItemType == string.Empty)
        {
            QuestLimitItemType = ItemTableMgr.ItemInfo_ItemType.None;
        }
        else
        {
            if (TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.ItemInfo_ItemType), csvinfo.QuestLimitItemType))
            {
                QuestLimitItemType = (ItemTableMgr.ItemInfo_ItemType)Enum.Parse(typeof(ItemTableMgr.ItemInfo_ItemType), csvinfo.QuestLimitItemType);
            }
            else
            {
                QuestLimitItemType = ItemTableMgr.ItemInfo_ItemType.None;
            }
        }
        QuestLimitItemCount = csvinfo.QuestLimitItemCount;
        AbsoluteQuestLimit = csvinfo.AbsoluteQuestLimit;
        PriceItemId = csvinfo.PriceItemId;
        Price = csvinfo.Price;
        CashPrices = csvinfo.CashPrices;
        ExtractsPrices = csvinfo.ExtractsPrices;
        SalePrices = csvinfo.SalePrices;
        Salable = csvinfo.Salable;
        PrefabName = csvinfo.PrefabName;
        ColliderSize = csvinfo.ColliderSize;
        ScaleSize = csvinfo.ScaleSize;
        AssetBundleName = csvinfo.AssetBundleName;
        Icon = csvinfo.Icon;
        EvloveIcon = csvinfo.EvolveIcon;
        Sort = csvinfo.Sort;
        Description = csvinfo.Description;
        CoinPrices_EventFactor = csvinfo.CoinPrices_EventFactor;
        InGamePosY = csvinfo.InGamePosY;

        CollectMethod = new List<string>();
        int[] collect = TangentFramework.Utils.ConvertToIntList(csvinfo.CollectMethod);
        foreach (int v in collect)
            CollectMethod.Add(v.ToString());

        EventMark = csvinfo.EventMark;
        NewMark = csvinfo.NewMark;
        if (TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.OriginalCharacter), csvinfo.OriginalCharacterName))
            OriginalCharacterName = (ItemTableMgr.OriginalCharacter)Enum.Parse(typeof(ItemTableMgr.OriginalCharacter), csvinfo.OriginalCharacterName);
        else
            OriginalCharacterName = ItemTableMgr.OriginalCharacter.None;

        if (OriginalCharacterName != ItemTableMgr.OriginalCharacter.None)
        {
            switch (OriginalCharacterName)
            {
                case ItemTableMgr.OriginalCharacter.Finn:
                    characterType = ItemTableMgr.CharacterType.Finn;
                    isFemale = false;
                    break;

                case ItemTableMgr.OriginalCharacter.Jake:
                    characterType = ItemTableMgr.CharacterType.Jake;
                    isFemale = false;
                    break;

                case ItemTableMgr.OriginalCharacter.burble:
                    characterType = ItemTableMgr.CharacterType.Bublegum;
                    isFemale = true;
                    break;

                case ItemTableMgr.OriginalCharacter.Marceline:
                    characterType = ItemTableMgr.CharacterType.Marceline;
                    isFemale = true;
                    break;

                case ItemTableMgr.OriginalCharacter.flameprincess:
                    characterType = ItemTableMgr.CharacterType.Flame;
                    isFemale = true;
                    break;
                case ItemTableMgr.OriginalCharacter.lemongrab:
                    characterType = ItemTableMgr.CharacterType.LemonGrab;
                    isFemale = false;
                    break;
                case ItemTableMgr.OriginalCharacter.Fionna:
                    characterType = ItemTableMgr.CharacterType.Fionna;
                    isFemale = true;
                    break;
            }
        }
        else
            characterType = ItemTableMgr.CharacterType.None;

        ListItemAbility = new List<ItemAbilityInfo>();
        ListWhatNeedToEnhance = new List<WhatNeedToEnhanceInfo>();
        ListWhatNeedToEvolve_Coin = new List<WhatNeedToEvolveInfo>();
        ListWhatNeedToEvolve_Cash = new List<WhatNeedToEvolveInfo>();
        ListWhatNeedToBreakLimit = new List<WhatNeedToBreakLimitInfo>();
        ListChanceOfGreatSuccess = new List<ChanceOfGreatSuccessInfo>();
    }

    public long CoinPricesApplyEventFactor
    {
        get
        {
            if (CoinPrices_EventFactor == 0)
                return Price;
            else
                return Price * CoinPrices_EventFactor / 100;
        }
    }

    public float Radius
    {
        get
        {
            return (float)ColliderSize * 0.01f;
        }
    }
}

/// <summary>
/// 아이템이 가지고 있는 Ability들의 종류
/// </summary>
public class ItemAbilityInfo
{
    public int ItemAbilityId;
    public int ItemId;
    public int AbilityId;
    public ItemTableMgr.ItemAbility_EnhanceType EnhanceType;
    public int OpenLevel;
    public int OpenIfEvolved;
    public string AbilitySkillDescription;
    public string AbilitySkillCaptionDescription;
    public int DefaultLevel;
    public int MaxLevel;

    public AbilityInfo Ability;
    public List<ItemAbilityValueInfo> ListItemAbilityEnhanceValue;

    public ItemAbilityInfo(ItemAbilityCSV csvinfo)
    {
        ItemAbilityId = csvinfo.ItemAbilityId;
        ItemId = csvinfo.ItemId;
        AbilityId = csvinfo.AbilityId;
        if(TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.ItemAbility_EnhanceType), csvinfo.EnhanceType))
        {
            EnhanceType = (ItemTableMgr.ItemAbility_EnhanceType)Enum.Parse(typeof(ItemTableMgr.ItemAbility_EnhanceType), csvinfo.EnhanceType);
        }
        OpenLevel = csvinfo.OpenLevel;
        OpenIfEvolved = csvinfo.OpenIfEvolved;
        AbilitySkillDescription = csvinfo.AbilitySkillDescription;
        AbilitySkillCaptionDescription = csvinfo.AbilitySkillCaptionDescription;
        ListItemAbilityEnhanceValue = new List<ItemAbilityValueInfo>();
        DefaultLevel = csvinfo.DefaultLevel;
        MaxLevel = csvinfo.MaxLevel;
    }

    public ItemAbilityInfo(ItemAbilityInfo v)
    {
        ItemAbilityId = v.ItemAbilityId;
        ItemId = v.ItemId;
        AbilityId = v.AbilityId;
        Ability = v.Ability;
        EnhanceType = v.EnhanceType;
        OpenLevel = v.OpenLevel;
        OpenIfEvolved = v.OpenIfEvolved;
        AbilitySkillDescription = v.AbilitySkillDescription;
        AbilitySkillCaptionDescription = v.AbilitySkillCaptionDescription;
        ListItemAbilityEnhanceValue = v.ListItemAbilityEnhanceValue;
        DefaultLevel = v.DefaultLevel;
        MaxLevel = v.MaxLevel;
    }

     public ItemAbilityValueInfo GetAbilityValueInfo(int enhancelevel)
     {
         ItemAbilityValueInfo info = ListItemAbilityEnhanceValue.Find(item => item.EnhanceLevel == enhancelevel);
         return info;
     }

    public ItemAbilityValueInfo GetAbilityValueInfo()
    {
        return (ListItemAbilityEnhanceValue.Count == 0 ? null : ListItemAbilityEnhanceValue[0]);
    }

    public int GetAbilityValue()
    {
        ItemAbilityValueInfo info = GetAbilityValueInfo();
        if (info == null)
            return 0;
        return info.Value2;
    }
}

/// <summary>
/// 아이템이 가지고 있는 Ability들의 강화등급별 능력수치(Value)
/// </summary>
public class ItemAbilityValueInfo
{
    public int itemid;
    public long UID;

    public int ItemAbilityId;
    public int EnhanceLevel;
    public int TriggerConditionValue1;
    public int TriggerConditionValue2;
    public int Value1;
    public int Value2;
    public int UseAblePercent;
    public int AvailableNumber;
    public float ExecuteChance;
    public float MinCoolTime;
    public float MaxCoolTime;
    public float EffectiveTime;
    public float AbilityMaxTime;

    public ItemAbilityValueInfo()
    {  
    }

    public ItemAbilityValueInfo(int v, int percent)
    {
        Value2 = v;
        UseAblePercent = percent;
    }

    public ItemAbilityValueInfo(int v1, int v2, int percent)
    {
        Value1 = v1;
        Value2 = v2;
        UseAblePercent = percent;
    }

    public ItemAbilityValueInfo(ItemAbilityValueCSV csvinfo)
    {
        ItemAbilityId = csvinfo.ItemAbilityId;
        EnhanceLevel = csvinfo.EnhanceLevel;
        TriggerConditionValue1 = csvinfo.TriggerConditionValue1;
        TriggerConditionValue2 = csvinfo.TriggerConditionValue2;
        Value1 = csvinfo.Value1;
        Value2 = csvinfo.Value2;
        UseAblePercent = csvinfo.UseAblePercent;
        AvailableNumber = csvinfo.AvailableNumber;
        ExecuteChance = Utils.RoundValue(csvinfo.ExecuteChance * 0.01f);
        MinCoolTime = Utils.RoundValue(csvinfo.MinCoolTime * 0.01f);
        MaxCoolTime = Utils.RoundValue(csvinfo.MaxCoolTime * 0.01f);
        EffectiveTime = Utils.RoundValue(csvinfo.EffectiveTime * 0.01f);
        AbilityMaxTime = Utils.RoundValue(csvinfo.AbilityMaxTime * 0.01f);
    }

    public ItemAbilityValueInfo(ItemAbilityValueInfo v)
    {
        ItemAbilityId = v.ItemAbilityId;
        EnhanceLevel = v.EnhanceLevel;
        TriggerConditionValue1 = v.TriggerConditionValue1;
        TriggerConditionValue2 = v.TriggerConditionValue2;
        Value1 = v.Value1;
        Value2 = v.Value2;
        UseAblePercent = v.UseAblePercent;
        AvailableNumber = v.AvailableNumber;
        ExecuteChance = v.ExecuteChance;
        MinCoolTime = v.MinCoolTime;
        MaxCoolTime = v.MaxCoolTime;
        EffectiveTime = v.EffectiveTime;
        AbilityMaxTime = v.AbilityMaxTime;
    }
}

/// <summary>
/// Ability 정보
/// Ability의 기본정보와 Ability 등급별 Enhance와 Evolve조건, 레벨업 대성공확률을 가진다.
/// </summary>
public class AbilityInfo
{
    public int AbilityId;
    public string Name;
    public ItemTableMgr.Ability_TriggerType TriggerType;
    public ItemTableMgr.Ability_Effect Effect;   // enum으로 변경
    public ItemTableMgr.Ability_TriggerCondition TriggerCondition;
    public ItemTableMgr.Ability_Target Target;
    public int SpecialTarget_ItemId;
    public int Grade;
    public string Icon;

    public List<WhatNeedToEnhanceInfo> ListWhatNeedToEnhance;
    public List<ChanceOfGreatSuccessInfo> ListChanceOfGreatSuccess;

    public AbilityInfo(AbilityCSV csvinfo)
    {
        AbilityId = csvinfo.AbilityId;
        Name = csvinfo.Name;
        if (TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.Ability_TriggerType), csvinfo.TriggerType))
        {
            TriggerType = (ItemTableMgr.Ability_TriggerType)Enum.Parse(typeof(ItemTableMgr.Ability_TriggerType), csvinfo.TriggerType);
        }
        else
            TriggerType = ItemTableMgr.Ability_TriggerType.None;

        if (TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.Ability_TriggerCondition), csvinfo.TriggerCondition))
        {
            TriggerCondition = (ItemTableMgr.Ability_TriggerCondition)Enum.Parse(typeof(ItemTableMgr.Ability_TriggerCondition), csvinfo.TriggerCondition);
        }
        else
            TriggerCondition = ItemTableMgr.Ability_TriggerCondition.None;

        if (TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.Ability_Effect), csvinfo.Effect))
        {
            Effect = (ItemTableMgr.Ability_Effect)Enum.Parse(typeof(ItemTableMgr.Ability_Effect), csvinfo.Effect);
        }
        else
        {
            Effect = ItemTableMgr.Ability_Effect.None;
        }

        //if (TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.Ability_Target), csvinfo.Target))
        {
            //Target = (ItemTableMgr.Ability_Target)Enum.Parse(typeof(ItemTableMgr.Ability_Target), csvinfo.Target);
        }

        SpecialTarget_ItemId = csvinfo.SpecialTarget_ItemId;
        Grade = csvinfo.Grade;
        Icon = csvinfo.Icon;
        ListWhatNeedToEnhance = new List<WhatNeedToEnhanceInfo>();
        ListChanceOfGreatSuccess = new List<ChanceOfGreatSuccessInfo>();
    }
}

/// <summary>
/// 인게임에서 사용하는 AbilityValue 데이터
/// </summary>
public class InGameAbilityValueInfo 
{
    public int ItemId;
    public int EnhanceLevel;
    public ItemTableMgr.StatType AbilityType;
    public ItemAbilityInfo ItemAbilityData;

    public InGameAbilityValueInfo(int itemid, int enhanceLevel, ItemAbilityInfo info)
    {
        ItemId = itemid;
        EnhanceLevel = enhanceLevel;
        ItemAbilityData = new ItemAbilityInfo(info);

        // need to do 
        // 현재 Docs 데이터 설정이 안되있기 때문에 레벨이 넘어가도 마지막 데이터 값으로 재설정해준다
        if(ItemAbilityData.EnhanceType == ItemTableMgr.ItemAbility_EnhanceType.EnemyLevel)
        {
            if (ItemAbilityData.ListItemAbilityEnhanceValue.Count == 0)
                return;

            if (ItemAbilityData.ListItemAbilityEnhanceValue[ItemAbilityData.ListItemAbilityEnhanceValue.Count - 1].EnhanceLevel < EnhanceLevel)
                EnhanceLevel = ItemAbilityData.ListItemAbilityEnhanceValue[ItemAbilityData.ListItemAbilityEnhanceValue.Count - 1].EnhanceLevel;
        }
    }

    public ItemAbilityValueInfo GetAibilityValueInfo(int enhanceLevel)
    {
        ItemAbilityValueInfo info = ItemAbilityData.ListItemAbilityEnhanceValue.Find(item => item.EnhanceLevel == enhanceLevel);

        if (info == null)
            return null;

        return new ItemAbilityValueInfo(info);
    }


    int baseLevel = 0;
    public ItemAbilityValueInfo GetAibilityValueInfo()
    {
        ItemAbilityValueInfo info = GetAibilityValueInfo(EnhanceLevel);
        if (info == null)
        {
            //CLog.Log(string.Format("ItemTableMgr, GetAibilityValueInfo, Not Exist SkillID {0}_EnhanceLevel : {1}", ItemAbilityData.AbilityId, EnhanceLevel));
            //return null;
            info = GetAibilityValueInfo(baseLevel);

            if(info == null)
                info = GetAibilityValueInfo(baseLevel + 1);
        }

        return info;
    }

    public int GetAbilityValue()
    {
        ItemAbilityValueInfo info = GetAibilityValueInfo();

        if (info == null)
            return 0;

        return info.Value2;
    }

    public float GetAbilityMinCoolTime()
    {
        ItemAbilityValueInfo info = GetAibilityValueInfo();

        if (info == null)
            return 0;

        return info.MinCoolTime;
    }

    public float GetAbilityMaxCoolTime()
    {
        ItemAbilityValueInfo info = GetAibilityValueInfo();

        if (info == null)
            return 0;

        return info.MaxCoolTime;
    }
    public float GetAbilityEffectTime()
    {
        ItemAbilityValueInfo info = GetAibilityValueInfo();

        if (info == null)
            return 0;

        return info.EffectiveTime;
    }

    public float GetExecuteChange()
    {
        ItemAbilityValueInfo info = GetAibilityValueInfo();

        if (info == null)
            return 0;

        return info.ExecuteChance;
    }
}

/// <summary>
/// 캐릭터가 가지는 기본 Base 능력치(스킬)
/// </summary>
public struct StatInfo
{
    public int ItemId;
    public int EnhanceLevel;
    public Dictionary<ItemTableMgr.StatType, InGameAbilityValueInfo> itemabilityInfo;

    public StatInfo(int itemid, int enhanceLevel, List<ItemAbilityInfo> infos, ItemTableMgr.ItemAbility_EnhanceType enhanceType = ItemTableMgr.ItemAbility_EnhanceType.CharacterLevel)
    {
        ItemId = itemid;
        EnhanceLevel = enhanceLevel;
        itemabilityInfo = new Dictionary<ItemTableMgr.StatType, InGameAbilityValueInfo>();

        PlayerData.UserCharacterItem character = PlayerData.instance.GetUserCharacterInfo(itemid);

        foreach (ItemAbilityInfo v in infos)
        {
            if(v.AbilityId != 0 && v.AbilityId <= 10)
            {
                if(character != null)
                {
                    PlayerData.UserItemSkill skill = character.CharacterSkillList.Find(item => item.ItemAbilityId == v.ItemAbilityId);
                    enhanceLevel = (skill == null ? enhanceLevel : skill.EnhanceLevel);
                }
                
                itemabilityInfo[(ItemTableMgr.StatType)v.AbilityId] = new InGameAbilityValueInfo(itemid, enhanceLevel, v);
            }
        }
    }

    public InGameAbilityValueInfo GetStatInfo(ItemTableMgr.StatType type)
    {
        if (!itemabilityInfo.ContainsKey(type))
            return null;

        return itemabilityInfo[type]; 
    }

    public ItemAbilityInfo GetStatAbilityInfo(ItemTableMgr.StatType type)
    {
        if (GetStatInfo(type) == null)
            return null;

        return GetStatInfo(type).ItemAbilityData;
    }

    public int GetStatAbilityValue(ItemTableMgr.StatType type)
    {
        InGameAbilityValueInfo info = GetStatInfo(type);

        if (info == null)
            return 0;

        return info.GetAbilityValue();
    }

    public float GetAbilityMinCoolTime(ItemTableMgr.StatType type)
    {
        InGameAbilityValueInfo info = GetStatInfo(type);

        if (info == null)
            return 0;

        return info.GetAbilityMinCoolTime();
    }

    public float GetAbilityMaxCoolTime(ItemTableMgr.StatType type)
    {
        InGameAbilityValueInfo info = GetStatInfo(type);

        if (info == null)
            return 0;

        return info.GetAbilityMaxCoolTime();
    }

    public float GetAbilityEffectiveTime(ItemTableMgr.StatType type)
    {
        InGameAbilityValueInfo info = GetStatInfo(type);

        if (info == null)
            return 0;

        return info.GetAbilityEffectTime();
    }

    public float GetExecuteChange(ItemTableMgr.StatType type)
    {
        InGameAbilityValueInfo info = GetStatInfo(type);

        if (info == null)
            return 0;

        return info.GetExecuteChange();
    }
}


/// <summary>
/// 캐릭터가 가지는 스킬
/// </summary>
public struct SkillInfo
{
    public List<InGameAbilityValueInfo> SkillList;

    public SkillInfo(PlayerData.UserCharacterItem character, bool checkOpenLevel = false)
    {
        SkillList = new List<InGameAbilityValueInfo>();
        List<ItemAbilityInfo> infos = ItemTableMgr.instance.GetItemAbilityData(character.ItemId);
        infos = infos.FindAll(item => item.EnhanceType == ItemTableMgr.ItemAbility_EnhanceType.SkillLevel && item.AbilityId != 0);

        for(int i = 0; i < infos.Count; i++)
        {
            ItemAbilityInfo v = infos[i];
            PlayerData.UserItemSkill skill = PlayerData.instance.GetUserCharacterSkill(character.ItemId, v.ItemAbilityId);

            if (skill != null)
                SkillList.Add(new InGameAbilityValueInfo(character.ItemId, skill.EnhanceLevel, v));
            else
            {
                if(!checkOpenLevel)
                    SkillList.Add(new InGameAbilityValueInfo(character.ItemId, 0, v));
            }
        }
        SkillList.Sort(delegate (InGameAbilityValueInfo x, InGameAbilityValueInfo y) { return x.ItemAbilityData.OpenLevel.CompareTo(y.ItemAbilityData.OpenLevel); });
    }

    public SkillInfo(int itemid, int enhanceLevel, List<ItemAbilityInfo> infos, ItemTableMgr.ItemAbility_EnhanceType enhancetype)
    {
        SkillList = new List<InGameAbilityValueInfo>();
        infos = infos.FindAll(item => item.EnhanceType == enhancetype && item.AbilityId != 0);
        for(int i = 0; i < infos.Count; i++)
        {
            ItemAbilityInfo v = infos[i];
            SkillList.Add(new InGameAbilityValueInfo(itemid, enhanceLevel, v));
        }
        SkillList.Sort(delegate (InGameAbilityValueInfo x, InGameAbilityValueInfo y) { return x.ItemAbilityData.OpenLevel.CompareTo(y.ItemAbilityData.OpenLevel); });
    }

    int Compare(InGameAbilityValueInfo v1, InGameAbilityValueInfo v2)
    {
        if (v1.ItemAbilityData.OpenLevel <= v2.ItemAbilityData.OpenLevel)
            return -1;

        return 1;
    }

    public ItemAbilityValueInfo GetSkillAbilityValueInfo(ItemTableMgr.Ability_Effect effect, int level)
    {
        InGameAbilityValueInfo info = SkillList.Find(item => item.EnhanceLevel == level && item.ItemAbilityData.Ability.Effect == effect);
        if (info == null)
            return null;

        return info.ItemAbilityData.ListItemAbilityEnhanceValue.Find(item => item.EnhanceLevel == level);
    }
}

/// <summary>
/// Item 등급별 강화에 필요한 조건(게임머니, 캐쉬)
/// </summary>
public class WhatNeedToEnhanceInfo
{
    public ItemTableMgr.ItemInfo_ItemType ItemType;
    public int Grade;
    public int EnhanceLevel;
    public int CoinPrices;
    public int CashPrices;

    public WhatNeedToEnhanceInfo(WhatNeedToEnhanceCSV csvinfo)
    {
        if (TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.ItemInfo_ItemType), csvinfo.ItemType))
        {
            ItemType = (ItemTableMgr.ItemInfo_ItemType)Enum.Parse(typeof(ItemTableMgr.ItemInfo_ItemType), csvinfo.ItemType);
        }
        else
            ItemType = ItemTableMgr.ItemInfo_ItemType.None;
        Grade = csvinfo.Grade;
        EnhanceLevel = csvinfo.EnhanceLevel;
        CoinPrices = csvinfo.CoinPrices;
        CashPrices = csvinfo.CashPrices;
    }
}

/// <summary>
/// Item 등급별 진화(or 초월)에 필요한 조건(아이템 종류 및 개수, 확률)
/// </summary>
public class WhatNeedToEvolveInfo
{
    public int ItemId;
    public int SubType;
    public int SourceItemId;
    public int ItemValue;
    public int Chance;
}

/// <summary>
/// Item 레벨 한계돌파에 필요한 조건(캐릭터)
/// </summary>
public class WhatNeedToBreakLimitInfo
{
    public int ItemId;
    public int Step;
    public int SourceItemId;
    public int ItemValue;
}

/// <summary>
/// Item 등급별 레벨업시 대성공확률(게임머니, 캐쉬별로 나뉨)
/// </summary>
public class ChanceOfGreatSuccessInfo
{
    public ItemTableMgr.ItemInfo_ItemType ItemType;
    public int Grade;
    public int Level;
    public int ChanceFromCoin;
    public int ChanceFromCash;

    public ChanceOfGreatSuccessInfo(ChanceOfGreatSuccessCSV csvinfo)
    {
        if (TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.ItemInfo_ItemType), csvinfo.ItemType))
        {
            ItemType = (ItemTableMgr.ItemInfo_ItemType)Enum.Parse(typeof(ItemTableMgr.ItemInfo_ItemType), csvinfo.ItemType);
        }
        else
            ItemType = ItemTableMgr.ItemInfo_ItemType.None;
        Grade = csvinfo.Grade;
        Level = csvinfo.Level;
        ChanceFromCoin = csvinfo.ChanceFromCoin;
        ChanceFromCash = csvinfo.ChanceFromCash;
    }
}

/// <summary>
/// 해당 등급에 맞는 보물이 진화할때 성공확률 증가 아이템정보
/// </summary>
public class TreasureEvolveSupporterInfo
{
    public int Grade;
    public int SourceItemId;
    public int BonusChance;
}

/// <summary>
/// 현재 한계돌파를 할수 있는지 여부와 내 레벨에서 한계돌파가 가능한 레벨
/// </summary>
public struct ItemEnhanceStateInfo
{
    public ItemTableMgr.ItemEnhanceState State;
    public int AbleBreakLimitLevel;

    public ItemEnhanceStateInfo(ItemTableMgr.ItemEnhanceState state, int breaklevel)
    {
        State = state;
        AbleBreakLimitLevel = breaklevel;
    }
}

public class TreasureEvolveResultInfo
{
    public int ItemId;
    public int ItemAbilityId;
    public ItemTableMgr.TreasureEvolveResult_ValueType ValueType;
    public int Value;
    public int Chance;
    public string ItemEvolveDescription;

    public TreasureEvolveResultInfo(TreasureEvolveResultCSV csvinfo)
    {
        ItemId = csvinfo.ItemId;
        ItemAbilityId = csvinfo.ItemAbilityId;
        if (TangentFramework.Utils.IsEnumParseName(typeof(ItemTableMgr.TreasureEvolveResult_ValueType), csvinfo.ValueType))
        {
            ValueType = (ItemTableMgr.TreasureEvolveResult_ValueType)Enum.Parse(typeof(ItemTableMgr.TreasureEvolveResult_ValueType), csvinfo.ValueType);
        }
        else
            ValueType = ItemTableMgr.TreasureEvolveResult_ValueType.MAX;

        Value = csvinfo.Value;
        Chance = csvinfo.Chance;
        ItemEvolveDescription = csvinfo.ItemEvolveDescription;
    }
}

#endregion

public class ItemTableMgr : MonoSingleton<ItemTableMgr>, ILoadData
{
    #region Enum
    public enum CharacterType
    {
        None = 0,
        Finn = 101,
        Jake = 104,
        Bublegum = 107,
        Marceline = 110,
        Flame,
        LemonGrab,
        Fionna,
        MAX = 999,
    }

    public enum ItemInfo_ItemType
    {
        Coin,
        Cash,
        Stamina,
        Stamina2,
        Exp,
        ExpPotion,
        SocialPoint = 10,
        Medal = 11,
        MysticMaterial = 12,
        EvolveCatalyst,
        Character,
        Treasure,
        Material,
        InGameCoin,
        InGameScore,
        InGame,
        ConsumableItem,
        Skill,
        Enemy,
        Object,
        InGameRandomBox,
        TiketGacha,

        PrefabItem,
        Buddy,
        BuddyItem,
        BuddySkill,
        OnlyGacha,
        None
    }

   

    public enum ItemGrade
    {
        Common = 0,
        Rare,
        SuperRare,
        Epic,
    }

    public enum Ability_TriggerType
    {
        Stat,
        Passive,
        Buff,
        Active,
        CommonAtk,
        TreasurePassive,
        EffectiveWhileHave,
        EnemySkill,
        ObjSkill,
        IngameItemSkill,
        BoosterItemSkill,
        UseInOutGame,
        TeamEffect,
        ActiveClick,
        ActivePress,

        None
    }

    public enum Ability_Effect
    {
        AddHp,
        AddMaxHp = 1,
        AddReduceHPSpeed = 2,
        AddSpeed = 3,
        AddDamage = 4,
        AddArmor = 5,
        AddJumpHeight = 6,
        AddTime,

        AddReducePoisonDamage,
        AddReduceCrushDamage,

        Rush,
        AddRush,
        AddRushFinn,
        Invincible,
        AddSkillLevel,
        Mighty,
        RevivalWithHP,
        LoginReward,
        AddBoosterEffect,
        AddIngameBoosterEffect,
        GainCoin,
        GainScore,
        AddCoinYieldDuringIngame,
        AddScoreYieldDuringIngame,
        AddTeamExpYieldDuringIngame,
        AddCoinYield,
        AddScoreYield,
        AddTeamExp,
        AddMagnet,
        Magnet,
        CreateObject1,
        CreateObject2,
        AbsorbHP,
        GetShieldPoint,
        GainTeamExp,
        GainCharacterExp,
        ChangeAllObject,
        ChangeMonster,
        ChangeObject,
        AddCriticalChance,
        Move,
        MoveLine,
        EnemyAttackTerm,
        PoisonResist,
        ObjcrushResist,
        SpeedUp,

        AddAttackRange,
        AddDuration,
        BuffCoolTime,

        DoubleCoin,
        DoubleScore,

        ActiveSkill,

        AddScoreItemYieldDuringIngame,
        AddCoinItemYieldDuringIngame,
        DoubleJump,
        AddFoodHp,
        ChangeAllBaddge,
        GainDiamond,
        AddDropCoin,

        AddItemTimeIncrease,
        GainScoreCrush,
        AddPogoDistance,
        Revival,
        AddTagHPRecovery,
        ChangeScore,
        ChangeMoney,
        AddHpShield,

        TagBooster,
        RushMagnet,
        
        ArriveDelaytime,
        HpRegen,
        SpeedAccelerate,
        ScoreMagnet,

        WheatherEffectDeffence,
        CrushObj,
        AddHpTagFriend,

        ChangeHp,
        RushMagnetCharacter,
        AddrandItemBooster,
        TransferHpScore,
        TransferScoreHp,
        AddInvincible,
        None,
        MAX
    }

    public enum Ability_TriggerCondition
    {
        None,
        KillCount_Monsters,
        BehaviorCount_Jump,
        BehaviorCount_Roll,
        Behavior_Rolling,
        Behavior_CoolTime,
        Behavior_Pogo,
        Behavior_SpeedUp,
        Behavior_TagAction,
        Behavior_Jumping,
        Behavior_Jump,
        Behavior_Night,
        Behavior_Day,
        Behavior_JumpOver,
        Behavior_SpeedItem,
        Behavior_SlideAvoid,
        Behavior_BattleMode,
        Behavior_BattleMode_KillMonster,
        Behavior_RushMode,
        Behavior_ChaseMode_Avoid,
        Behavior_Crash,
        Behavior_DayCoolTime,
        Behavior_NightCoolTime,
        Behavior_SpeedUpCrash,
        Behavior_Click,
        Behavior_Press,
        
        Behavior_Magnet,
        Behavior_GainItem,
        Behavior_Girl,
        Behavior_WaterMode,

        Behavior_Tag,
        Behavior_BigScoreGet,
        Behavior_FoodGet,
        Behavior_FoodCount,
        Behavior_LowHp,

        MAX
    }

    public enum Ability_Target
    {

    }

    public enum ItemAbility_EnhanceType
    {
        None,
        CharacterLevel,
        SkillLevel,
        TreasureLevel,
        EnemyLevel,
        BuddyEvolve,
    }

    public enum ItemEventMarkType
    {
        None = 0,
    }

    public enum ItemEnhanceState    // 캐릭터(한계돌파, 각성), 보물(강화, 진화)
    {
        None,                   // X
        NotEnoughLevel,         // 레벨 부족
        NotEnoughMaterial,      // 재료 부족
        MaxEnhance,             // 한계돌파 / 강화 최대
        AbleEnhance,            // 한계돌파 / 강화 가능(캐쉬, 코인 둘다)
        AbleEnhance_Cash,       // 강화 가능(캐쉬)
        AbleEnhance_Coin,       // 강화 가능(캐쉬, 코인)
        AbleEvolve_Cash,        // 진화or각성 가능 - 캐쉬
        AbleEvolve_Coin,        // 진화or각성 가능 - 코인
        AbleEvolve_CoinCash,    // 진화or각성 가능 - 캐쉬, 코인 둘다
    }

    public enum ItemKeepState
    {
        NotEnoughCoin,
        NotEnoughCash,
        NotEnoughItem,
        KeepItem,
    }

    // 캐릭터 기본 능력치(Status)
    public enum StatType
    {
        None = 0,
        HP = 1,
        ReduceHPSpeed,
        Speed,
        Damage,
        Armor,
        JumpHeight,
        AttackRange, 
        Score,
        CriticalChance,

        MAX
    }

    public enum TreasureEvolveResult_ValueType
    {
        None = 0,
        TriggerConditionValue1,
        TriggerConditionValue2,
        Value1,
        Value2,
        UseAblePercent,
        AvailableNumber,
        MinCoolTime,
        MaxCoolTime,
        EffectiveTime,
        ExecuteChance,

        MAX
    }

    public enum StartIdOfItemType
    {
        Character = 101,
        Treasure = 10001,
        Material = 100001,
        PieceOfCharacter = 150001,
        Ingame = 200001,
        ConsumableItem = 300001,
        Enemy = 400001,
        Object = 500001,
       
        MAX = 99999999,         
    }

    public enum OriginalCharacter
    {
        None,
        Finn,
        Jake,
        Marceline,
        burble,
        flameprincess,
        lemongrab,
        Fionna,
    }

    #endregion

    #region Include Member vaiables
    private Dictionary<int, AbilityInfo> m_abilityTable;
    private Dictionary<int, List<ItemAbilityInfo>> m_itemAbilityTable;
    private Dictionary<int, List<ItemAbilityValueInfo>> m_itemAbilityValueTable;
    private Dictionary<string, List<WhatNeedToEnhanceInfo>> m_whatNeedToEnhanceTable;
    private Dictionary<int, List<WhatNeedToEvolveInfo>> m_whatNeedToEvolveTable;
    private Dictionary<int, List<WhatNeedToBreakLimitInfo>> m_whatNeedToBreakLimitTable;
    private Dictionary<string, List<ChanceOfGreatSuccessInfo>> m_chanceOfGreatSuccessTable;
    private List<TreasureEvolveSupporterInfo> m_treasureEvolveSupporterTable;
    private Dictionary<int, List<TreasureEvolveResultInfo>> m_treasureEvolveResultTable;
    #endregion

    #region Member variables
    private Dictionary<int, ItemInfo> m_itemTable;
    private Dictionary<string, ItemInfo> m_itemTablebyPrefabName;
    #endregion

    #region DataLoad
    public void LoadData()
    {
        LoadDataMgr.instance.AddToILoadDataList(DataGID.GameDataGID.Item, this);
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.Item.ToString()] = false;
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.Ability.ToString()] = false;
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.ItemAbility.ToString()] = false;
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.ItemAbilityEnhanceValue.ToString()] = false;
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.WhatNeedToEnhance.ToString()] = false;
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.WhatNeedToEvolve.ToString()] = false;
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.WhatNeedToBreakLimit.ToString()] = false;
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.ChanceOfGreatSuccess.ToString()] = false;
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.TreasureEvolveSupporter.ToString()] = false;
        LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.TreasureEvolveResult.ToString()] = false;

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.Item, (WWWData docs) =>
        {
            ItemCSV[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<ItemCSV[]>(docs.dataString) : Utils.GetInstance_Docs<ItemCSV[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_itemTable = new Dictionary<int, ItemInfo>();
                m_itemTablebyPrefabName = new Dictionary<string, ItemInfo>();

                foreach (ItemCSV v in infos)
                {
                    m_itemTable[v.ItemId] = new ItemInfo(v);
                    if (!string.IsNullOrEmpty(v.PrefabName))
                        m_itemTablebyPrefabName[v.PrefabName] = m_itemTable[v.ItemId];
                }
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.Item.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.Item.ToString()] = true;
            }
        });

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.ItemAbility, (WWWData docs) =>
        {
            ItemAbilityCSV[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<ItemAbilityCSV[]>(docs.dataString) : Utils.GetInstance_Docs<ItemAbilityCSV[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_itemAbilityTable = SetItemAbilityData(infos);
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.ItemAbility.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.ItemAbility.ToString()] = true;
            }
        });

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.ItemAbilityEnhanceValue, (WWWData docs) =>
        {
            ItemAbilityValueCSV[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<ItemAbilityValueCSV[]>(docs.dataString) : Utils.GetInstance_Docs<ItemAbilityValueCSV[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_itemAbilityValueTable = SetItemAbilityEnhanceValueInfoData(infos);
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.ItemAbilityEnhanceValue.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.ItemAbilityEnhanceValue.ToString()] = true;
            }
        });

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.Ability, (WWWData docs) =>
        {
            AbilityCSV[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<AbilityCSV[]>(docs.dataString) : Utils.GetInstance_Docs<AbilityCSV[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_abilityTable = new Dictionary<int, AbilityInfo>();

                foreach (AbilityCSV v in infos)
                {
                    if (!m_abilityTable.ContainsKey(v.AbilityId))
                    {
                        m_abilityTable[v.AbilityId] = new AbilityInfo(v);
                    }
                }
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.Ability.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.Ability.ToString()] = true;
            }
        });

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.WhatNeedToEnhance, (WWWData docs) =>
        {
            WhatNeedToEnhanceCSV[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<WhatNeedToEnhanceCSV[]>(docs.dataString) : Utils.GetInstance_Docs<WhatNeedToEnhanceCSV[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_whatNeedToEnhanceTable = SetWhatNeedToEnhanceData(infos);
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.WhatNeedToEnhance.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.WhatNeedToEnhance.ToString()] = true;
            }
        });

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.WhatNeedToEvolve, (WWWData docs) =>
        {
            WhatNeedToEvolveInfo[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<WhatNeedToEvolveInfo[]>(docs.dataString) : Utils.GetInstance_Docs<WhatNeedToEvolveInfo[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_whatNeedToEvolveTable = SetWhatNeedToEvolveData(infos);
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.WhatNeedToEvolve.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.WhatNeedToEvolve.ToString()] = true;
            }
        });

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.WhatNeedToBreakLimit, (WWWData docs) =>
        {
            WhatNeedToBreakLimitInfo[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<WhatNeedToBreakLimitInfo[]>(docs.dataString) : Utils.GetInstance_Docs<WhatNeedToBreakLimitInfo[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_whatNeedToBreakLimitTable = SetWhatNeedToBreakLimitData(infos);
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.WhatNeedToBreakLimit.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.WhatNeedToBreakLimit.ToString()] = true;
            }
        });

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.ChanceOfGreatSuccess, (WWWData docs) =>
        {
            ChanceOfGreatSuccessCSV[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<ChanceOfGreatSuccessCSV[]>(docs.dataString) : Utils.GetInstance_Docs<ChanceOfGreatSuccessCSV[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_chanceOfGreatSuccessTable = SetChanceOfGreatSuccessData(infos);
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.ChanceOfGreatSuccess.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.ChanceOfGreatSuccess.ToString()] = true;
            }
        });

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.TreasureEvolveSupporter, (WWWData docs) =>
        {
            TreasureEvolveSupporterInfo[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<TreasureEvolveSupporterInfo[]>(docs.dataString) : Utils.GetInstance_Docs<TreasureEvolveSupporterInfo[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_treasureEvolveSupporterTable = new List<TreasureEvolveSupporterInfo>();
                foreach (TreasureEvolveSupporterInfo v in infos)
                {
                    m_treasureEvolveSupporterTable.Add(v);
                }
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.TreasureEvolveSupporter.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.TreasureEvolveSupporter.ToString()] = true;
            }
        });

        WWWData.RequestReadFromGoogleDrive((int)DataGID.GameDataGID.TreasureEvolveResult, (WWWData docs) =>
        {
            TreasureEvolveResultCSV[] infos = GameDefine.IsTableEncodedAsJson ? Utils.GetInstance_Docs<TreasureEvolveResultCSV[]>(docs.dataString) : Utils.GetInstance_Docs<TreasureEvolveResultCSV[]>(docs.Lines);
            if (infos.Length > 0)
            {
                m_treasureEvolveResultTable = SetItemAbilityData(infos);
            }

            if (LoadDataMgr.instance.m_DocsLoadComplete.ContainsKey(DataGID.GameDataGID.TreasureEvolveResult.ToString()))
            {
                LoadDataMgr.instance.m_DocsLoadComplete[DataGID.GameDataGID.TreasureEvolveResult.ToString()] = true;
            }
        });
    }

    public void BeforehandTranslate()
    {
        Dictionary<int, ItemInfo>.Enumerator iter1 = m_itemTable.GetEnumerator();
        while(iter1.MoveNext())
        {
            ItemInfo info = iter1.Current.Value;
            string strtranslate = ItemLocalizationTableMgr.instance.GetTranslateString(ItemLocalizationTableMgr.TableName.ItemName, info.ItemId);

            if (strtranslate != string.Empty)
                info.Name = strtranslate;

            strtranslate = ItemLocalizationTableMgr.instance.GetTranslateString(ItemLocalizationTableMgr.TableName.ItemName, info.ItemId, 1);

            if (strtranslate != string.Empty)
                info.EvolveName = strtranslate;

            strtranslate = ItemLocalizationTableMgr.instance.GetTranslateString(ItemLocalizationTableMgr.TableName.ItemDescription, info.ItemId);

            if (strtranslate != string.Empty)
                info.Description = strtranslate;

            strtranslate = ItemLocalizationTableMgr.instance.GetTranslateString(ItemLocalizationTableMgr.TableName.ItemTypeName, info.ItemId);

            if (strtranslate != string.Empty)
                info.ItemTypeName = strtranslate;

            for (int i = 0; i < info.CollectMethod.Count; i++)
            {
                int id = 0;
                if (Int32.TryParse(info.CollectMethod[i], out id))
                {
                    strtranslate = ItemLocalizationTableMgr.instance.GetTranslateString(ItemLocalizationTableMgr.TableName.CollectMethod, id);

                    if (strtranslate != string.Empty)
                        info.CollectMethod[i] = strtranslate;
                }

            }
        }

        Dictionary<int, List<TreasureEvolveResultInfo>>.Enumerator iter2 = m_treasureEvolveResultTable.GetEnumerator();
        while(iter2.MoveNext())
        {
            List<TreasureEvolveResultInfo> v = iter2.Current.Value;
            for (int i = 0; i < v.Count; i++)
            {
                v[i].ItemEvolveDescription = ItemLocalizationTableMgr.instance.GetTranslateString(ItemLocalizationTableMgr.TableName.ItemEvolveDescription, v[i].ItemId);
            }
        }

        Dictionary<int, AbilityInfo>.Enumerator iter3 = m_abilityTable.GetEnumerator();
        while(iter3.MoveNext())
        {
            AbilityInfo info = iter3.Current.Value;
            string strtranslate = ItemLocalizationTableMgr.instance.GetTranslateString(ItemLocalizationTableMgr.TableName.AbilityName, info.AbilityId);

            if (strtranslate != string.Empty)
                info.Name = strtranslate;
        }

        Dictionary<int, List<ItemAbilityInfo>>.Enumerator iter4 = m_itemAbilityTable.GetEnumerator();
        while(iter4.MoveNext())
        {
            List<ItemAbilityInfo> infos = iter4.Current.Value;
            for (int i = 0; i < infos.Count; i++)
            {
                string strtranslate = ItemLocalizationTableMgr.instance.GetTranslateString(ItemLocalizationTableMgr.TableName.ItemAbilitySkillDescription, infos[i].ItemAbilityId);

                if (strtranslate != string.Empty)
                    infos[i].AbilitySkillDescription = strtranslate;

                strtranslate = ItemLocalizationTableMgr.instance.GetTranslateString(ItemLocalizationTableMgr.TableName.AbilitySkillCaptionDescription, infos[i].ItemAbilityId);

                if (strtranslate != string.Empty)
                    infos[i].AbilitySkillCaptionDescription = strtranslate;
            }
        }
    }

    public void SetInCludeData()
    {
        Dictionary<int, List<ItemAbilityInfo>>.Enumerator iter1 = m_itemAbilityTable.GetEnumerator();
        while(iter1.MoveNext())
        {
            int id = iter1.Current.Key;
            if (m_itemTable.ContainsKey(id))
            {
                m_itemTable[id].ListItemAbility = new List<ItemAbilityInfo>(m_itemAbilityTable[id]);
            }
            else
            {
                //CLog.Log("Not Exist ItemID(ItemAbility) : " + id);
            }
        }

        Dictionary<int, ItemInfo>.Enumerator iter2 = m_itemTable.GetEnumerator();
        while(iter2.MoveNext())
        {
            ItemInfo v = iter2.Current.Value;

            // Include Item in Enhance, Evolve, ChanceOfSuccess
            SetIncludeItemInData(v);

            if (m_treasureEvolveResultTable.ContainsKey(v.ItemId))
                v.ListTreasureEvolveResult = m_treasureEvolveResultTable[v.ItemId];

            // Include Item in Ability
            for (int i = 0; i < v.ListItemAbility.Count; i++)
            {
                // Include ItemAbility In Ability, ItemAbilityEnhanceValue
                SetIncludeItemAbilityInData(v.ListItemAbility[i]);

                if (v.ListItemAbility[i].Ability != null)
                {
                    // Include Ability in Enhance, Evolve, ChanceOfSuccess
                    SetIncludeAbilityInData(v.ListItemAbility[i].Ability);
                }
                else
                {
                    //CLog.Log("Not Exist AbilityID : " + v.ListItemAbility[i].AbilityId);
                }
            }
        }

#if UNITY_EDITOR
        if (GameDefine.Version_Client >= 3)
            CheckEffectivenessItemAbilityValueData();
#endif
    }

    void CheckEffectivenessItemAbilityValueData()
    {
        System.Text.StringBuilder errortxt = new System.Text.StringBuilder();
        errortxt.AppendFormat("ItemAbilityValueCheck \n");
        int titlelength = errortxt.Length;

        List<string> checkString = new List<string>();
        checkString.Add("Gain");
        checkString.Add("Create");
        checkString.Add("Change");

        Dictionary<int, List<ItemAbilityInfo>>.Enumerator iter = m_itemAbilityTable.GetEnumerator();
        while(iter.MoveNext())
        {
            List<ItemAbilityInfo> list = iter.Current.Value;
            for(int i =0; i < list.Count; i++)
            {
                ItemAbilityInfo info = list[i];
                if (info.Ability == null)
                    continue;

                if(checkString.FindIndex(item => info.Ability.Effect.ToString().Contains(item)) != -1)
                {
                    if (info.Ability.Effect == Ability_Effect.ChangeHp)
                        continue;

                    if(info.ListItemAbilityEnhanceValue.Count == 0 ||
                        info.ListItemAbilityEnhanceValue[0].Value1 == 0)
                    {
                        errortxt.Append(getEffectivenessItemAbilityValueerrorcode(info.ItemId, info.AbilityId, info.ItemAbilityId));
                    }
                }
            }
        }

        if (errortxt.Length > titlelength)
            CLog.LogError(errortxt.ToString());
    }

    string getEffectivenessItemAbilityValueerrorcode(int itemid, int abilityId, int itemabilityId)
    {
        return string.Format("Disaccord ItemAbilityValue valu1, ItemId : {0}, AbilityId : {1}, ItemAbilityId : {2}\n", itemid, abilityId, itemabilityId);
    }

    public void SetGameEvent(CustomNetwork.GameEventInfo info)
    {
        if(info.Value_Json != null)
        {
            for (int i = 0; i < info.Value_Json.Length; i++)
            {
                CustomNetwork.EventValue v = info.Value_Json[i];
                ItemInfo item = GetItemData(v.ItemId);

                if (item != null)
                {
                    System.Reflection.FieldInfo field = item.GetType().GetField(info.RowName);

                    if (field != null)
                    {
                        field.SetValue(item, v.Value);
                    }
                }
            }
        }
    }

    private Dictionary<int, List<ItemAbilityInfo>> SetItemAbilityData(ItemAbilityCSV[] infos)
    {
        List<ItemAbilityInfo> lstitemability = new List<ItemAbilityInfo>();
        Dictionary<int, List<ItemAbilityInfo>> dicitemability = new Dictionary<int, List<ItemAbilityInfo>>();

        int itemid = infos[0].ItemId;
        for (int i = 0; i < infos.Length; i++)
        {
            ItemAbilityInfo data = new ItemAbilityInfo(infos[i]);
            lstitemability.Add(data);

            if (infos.Length - 1 == i ||
                infos[i + 1].ItemId != itemid)
            {  
                if(dicitemability.ContainsKey(itemid))
                {
                    List<ItemAbilityInfo> existlist = dicitemability[itemid];

                    foreach(ItemAbilityInfo v in existlist)
                    {
                        int index = lstitemability.FindIndex(item => item.ItemAbilityId == v.ItemAbilityId);
                        if (index == -1)
                            lstitemability.Add(v);
                    }
                }

                dicitemability[itemid] = lstitemability;

                if (infos.Length > i + 1)
                {
                    lstitemability = new List<ItemAbilityInfo>();
                    itemid = infos[i + 1].ItemId;
                }

            }
        }

        return dicitemability;
    }

    private Dictionary<int, List<ItemAbilityValueInfo>> SetItemAbilityEnhanceValueInfoData(ItemAbilityValueCSV[] infos)
    {
        List<ItemAbilityValueInfo> lstitemabilityenhance = new List<ItemAbilityValueInfo>();
        Dictionary<int, List<ItemAbilityValueInfo>> dicitemabilityenhance = new Dictionary<int, List<ItemAbilityValueInfo>>();

        int itemabilityid = infos[0].ItemAbilityId;
        for (int i = 0; i < infos.Length; i++)
        {
            ItemAbilityValueInfo data = new ItemAbilityValueInfo(infos[i]);
            lstitemabilityenhance.Add(data);

            if (infos.Length - 1 == i ||
                infos[i + 1].ItemAbilityId != itemabilityid)
            {

                if (dicitemabilityenhance.ContainsKey(itemabilityid))
                {
                    List<ItemAbilityValueInfo> existlist = dicitemabilityenhance[itemabilityid];

                    foreach (ItemAbilityValueInfo v in existlist)
                    {
                        int index = lstitemabilityenhance.FindIndex(item => item.ItemAbilityId == v.ItemAbilityId);

                        if (index == -1)
                            lstitemabilityenhance.Add(v);
                    }
                }

                dicitemabilityenhance[itemabilityid] = lstitemabilityenhance;

                if (infos.Length > i + 1)
                {
                    lstitemabilityenhance = new List<ItemAbilityValueInfo>();
                    itemabilityid = infos[i + 1].ItemAbilityId;
                }

            }
        }

        return dicitemabilityenhance;
    }

    private Dictionary<string, List<WhatNeedToEnhanceInfo>> SetWhatNeedToEnhanceData(WhatNeedToEnhanceCSV[] infos)
    {
        List<WhatNeedToEnhanceInfo> lstneedenhance = new List<WhatNeedToEnhanceInfo>();
        Dictionary<string, List<WhatNeedToEnhanceInfo>> dicneedenhance = new Dictionary<string, List<WhatNeedToEnhanceInfo>>();

        string itemtype = infos[0].ItemType;
        for (int i = 0; i < infos.Length; i++)
        {
            WhatNeedToEnhanceInfo data = new WhatNeedToEnhanceInfo(infos[i]);
            lstneedenhance.Add(data);

            if (infos.Length - 1 == i ||
                infos[i + 1].ItemType != itemtype)
            {
                dicneedenhance[itemtype] = lstneedenhance;

                if (infos.Length > i + 1)
                {
                    lstneedenhance = new List<WhatNeedToEnhanceInfo>();
                    itemtype = infos[i + 1].ItemType;
                }
            }
        }

        return dicneedenhance;
    }

    private Dictionary<int, List<WhatNeedToEvolveInfo>> SetWhatNeedToEvolveData(WhatNeedToEvolveInfo[] infos)
    {
        List<WhatNeedToEvolveInfo> lstneedevolve = new List<WhatNeedToEvolveInfo>();
        Dictionary<int, List<WhatNeedToEvolveInfo>> dicneedevolve = new Dictionary<int, List<WhatNeedToEvolveInfo>>();

        int itemid = infos[0].ItemId;
        for (int i = 0; i < infos.Length; i++)
        {
            WhatNeedToEvolveInfo data = infos[i];
            lstneedevolve.Add(data);

            if (infos.Length - 1 == i ||
                infos[i + 1].ItemId != itemid)
            {
                dicneedevolve[itemid] = lstneedevolve;

                if (infos.Length > i + 1)
                {
                    lstneedevolve = new List<WhatNeedToEvolveInfo>();
                    itemid = infos[i + 1].ItemId;
                }
            }
        }

        return dicneedevolve;
    }

    private Dictionary<int, List<WhatNeedToBreakLimitInfo>> SetWhatNeedToBreakLimitData(WhatNeedToBreakLimitInfo[] infos)
    {
        List<WhatNeedToBreakLimitInfo> lstneedbreaklimit = new List<WhatNeedToBreakLimitInfo>();
        Dictionary<int, List<WhatNeedToBreakLimitInfo>> dicneedbreaklimit = new Dictionary<int, List<WhatNeedToBreakLimitInfo>>();

        int itemid = infos[0].ItemId;
        for (int i = 0; i < infos.Length; i++)
        {
            WhatNeedToBreakLimitInfo data = infos[i];
            lstneedbreaklimit.Add(data);

            if (infos.Length - 1 == i ||
                infos[i + 1].ItemId != itemid)
            {
                dicneedbreaklimit[itemid] = lstneedbreaklimit;

                if (infos.Length > i + 1)
                {
                    lstneedbreaklimit = new List<WhatNeedToBreakLimitInfo>();
                    itemid = infos[i + 1].ItemId;
                }
            }
        }

        return dicneedbreaklimit;
    }

    private Dictionary<string, List<ChanceOfGreatSuccessInfo>> SetChanceOfGreatSuccessData(ChanceOfGreatSuccessCSV[] infos)
    {
        List<ChanceOfGreatSuccessInfo> lstchanceofgreatsuccess = new List<ChanceOfGreatSuccessInfo>();
        Dictionary<string, List<ChanceOfGreatSuccessInfo>> dicchanceofgreatsuccess = new Dictionary<string, List<ChanceOfGreatSuccessInfo>>();

        string itemtype = infos[0].ItemType;
        for (int i = 0; i < infos.Length; i++)
        {
            ChanceOfGreatSuccessInfo data = new ChanceOfGreatSuccessInfo(infos[i]);
            lstchanceofgreatsuccess.Add(data);

            if (infos.Length - 1 == i ||
                infos[i + 1].ItemType != itemtype)
            {
                dicchanceofgreatsuccess[itemtype] = lstchanceofgreatsuccess;

                if (infos.Length > i + 1)
                {
                    lstchanceofgreatsuccess = new List<ChanceOfGreatSuccessInfo>();
                    itemtype = infos[i + 1].ItemType;
                }
            }
        }

        return dicchanceofgreatsuccess;
    }

    private Dictionary<int, List<TreasureEvolveResultInfo>> SetItemAbilityData(TreasureEvolveResultCSV[] infos)
    {
        List<TreasureEvolveResultInfo> lstitemability = new List<TreasureEvolveResultInfo>();
        Dictionary<int, List<TreasureEvolveResultInfo>> dicitemability = new Dictionary<int, List<TreasureEvolveResultInfo>>();

        int itemid = infos[0].ItemId;
        for (int i = 0; i < infos.Length; i++)
        {
            TreasureEvolveResultInfo data = new TreasureEvolveResultInfo(infos[i]);
            lstitemability.Add(data);

            if (infos.Length - 1 == i ||
                infos[i + 1].ItemId != itemid)
            {
                if (dicitemability.ContainsKey(itemid))
                {
                    List<TreasureEvolveResultInfo> existlist = dicitemability[itemid];

                    foreach (TreasureEvolveResultInfo v in existlist)
                    {
                        int index = lstitemability.FindIndex(item => item.ItemAbilityId == v.ItemAbilityId);
                        if (index == -1)
                            lstitemability.Add(v);
                    }
                }

                dicitemability[itemid] = lstitemability;

                if (infos.Length > i + 1)
                {
                    lstitemability = new List<TreasureEvolveResultInfo>();
                    itemid = infos[i + 1].ItemId;
                }

            }
        }

        return dicitemability;
    }

    void SetIncludeItemInData(ItemInfo info)
    {
        // Enhance
        if (info.ItemType == ItemInfo_ItemType.Character)
        {
            List<WhatNeedToEnhanceInfo> listenhance = new List<WhatNeedToEnhanceInfo>();
            foreach (WhatNeedToEnhanceInfo v in m_whatNeedToEnhanceTable[ItemInfo_ItemType.Skill.ToString()])
            {
                if (info.Grade == v.Grade)
                {
                    listenhance.Add(v);
                }
            }

            info.ListWhatNeedToEnhance = listenhance;
        }
        else if (!m_whatNeedToEnhanceTable.ContainsKey(info.ItemType.ToString()))
        {
            //CLog.Log("Not Exist whatNeedToEnhanceID : " + info.ItemType.ToString(), CLog.LogType.Warning);
        }
        else
        {
            List<WhatNeedToEnhanceInfo> listenhance = new List<WhatNeedToEnhanceInfo>();
            foreach (WhatNeedToEnhanceInfo v in m_whatNeedToEnhanceTable[info.ItemType.ToString()])
            {
                if (info.Grade == v.Grade)
                {
                    listenhance.Add(v);
                }
            }

            info.ListWhatNeedToEnhance = listenhance;
        }

        // Evolve
        if (!m_whatNeedToEvolveTable.ContainsKey(info.ItemId))
        {
            //CLog.Log("Not Exist whatNeedToEvolveID : " + info.ItemType.ToString());
        }
        else
        {
            List<WhatNeedToEvolveInfo> listevolve_coin = new List<WhatNeedToEvolveInfo>();
            List<WhatNeedToEvolveInfo> listevolve_cash = new List<WhatNeedToEvolveInfo>();
            foreach (WhatNeedToEvolveInfo v in m_whatNeedToEvolveTable[info.ItemId])
            {
                switch (v.SubType)
                {
                    case 0: { listevolve_coin.Add(v); } break;
                    case 1: { listevolve_cash.Add(v); } break;
                    //default: { CLog.Log("Not Exist SubType EvoloveItemID : " + v.ItemId); } break;
                }
            }

            info.ListWhatNeedToEvolve_Coin = listevolve_coin;
            info.ListWhatNeedToEvolve_Cash = listevolve_cash;
        }

        // BreakLimit
        if (!m_whatNeedToBreakLimitTable.ContainsKey(info.ItemId))
        {
            //CLog.Log("Not Exist whatNeedToBreakLimitID : " + info.ItemType.ToString());
        }
        else
        {
            info.ListWhatNeedToBreakLimit = m_whatNeedToBreakLimitTable[info.ItemId];
        }

        // ChanceOfGreatSuccess
        if (!m_chanceOfGreatSuccessTable.ContainsKey(info.ItemType.ToString()))
        {
            //CLog.Log("Not Exist chanceOfSuccessID : " + info.ItemType.ToString(), CLog.LogType.Warning);
        }
        else
        {
            List<ChanceOfGreatSuccessInfo> listchance = new List<ChanceOfGreatSuccessInfo>();
            foreach (ChanceOfGreatSuccessInfo v in m_chanceOfGreatSuccessTable[info.ItemType.ToString()])
            {
                if (info.Grade == v.Grade)
                {
                    listchance.Add(v);
                }
            }

            info.ListChanceOfGreatSuccess = listchance;
        }

        // TreasureEvolveSupporter
        if (info.ItemType == ItemInfo_ItemType.Treasure)
        {
            info.TreasureEvolveSupporter = m_treasureEvolveSupporterTable.Find(item => item.Grade == info.Grade);
        }
    }

    void SetIncludeItemAbilityInData(ItemAbilityInfo info)
    {
        if (!m_abilityTable.ContainsKey(info.AbilityId))
        {
            //CLog.Log("Not Exist AbilityID : " + info.AbilityId, CLog.LogType.Warning);
        }
        else
        {
            info.Ability = m_abilityTable[info.AbilityId];
        }

        if (!m_itemAbilityValueTable.ContainsKey(info.ItemAbilityId))
        {
            //CLog.Log("Not Exist itemAbilityEnhanceValueID : " + info.Id);
        }
        else
        {
            info.ListItemAbilityEnhanceValue = m_itemAbilityValueTable[info.ItemAbilityId];
        }

    }

    void SetIncludeAbilityInData(AbilityInfo info)
    {
        if (!m_whatNeedToEnhanceTable.ContainsKey("Skill"))
        {
           // CLog.Log("Not Exist whatNeedToEnhanceID : Skill", CLog.LogType.Warning);
        }
        else
        {
            info.ListWhatNeedToEnhance = m_whatNeedToEnhanceTable["Skill"];
        }

        if (!m_chanceOfGreatSuccessTable.ContainsKey("Skill"))
        {
           // CLog.Log("Not Exist ChanceOfGreatSuccessID : Skill", CLog.LogType.Warning);
        }
        else
        {
            info.ListChanceOfGreatSuccess = m_chanceOfGreatSuccessTable["Skill"];
        }
    }
    #endregion

    #region Function
    // 캐릭터 한계돌파 체크
    bool IsCharacterBreakLimit(PlayerData.UserCharacterItem character)
    {
        ItemInfo info = GetItemData(character.ItemId);
        if (info.ListWhatNeedToBreakLimit.Count == 0)
            return false;

        List<WhatNeedToBreakLimitInfo> list = info.ListWhatNeedToBreakLimit.FindAll(item => item.Step == character.NumberBreakLimit + 1);
        if (list.Count != 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ItemKeepState state = PlayerData.instance.CheckKeepItem(list[i].SourceItemId, list[i].ItemValue);

                if (state != ItemKeepState.KeepItem)
                    return false;
            }

            return true;
        }

        return false;
    }

    // 각성 및 진화 체크 bcash = true 캐쉬체크, cash = false 게임머니체크
    bool IsItemEvolve(PlayerData.UserItem item, bool bcash)
    {
        ItemInfo info = GetItemData(item.ItemId);
        if (info.ListWhatNeedToEvolve_Coin.Count == 0 &&
            info.ListWhatNeedToEvolve_Cash.Count == 0)
        {
            return false;
        }

        List<WhatNeedToEvolveInfo> list;
        if (bcash)
        {
            list = info.ListWhatNeedToEvolve_Cash;
        }
        else
        {
            list = info.ListWhatNeedToEvolve_Coin;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (PlayerData.instance.CheckKeepItem(list[i].SourceItemId, list[i].ItemValue) != ItemKeepState.KeepItem)
                return false;
        }

        return true;
    }
    #endregion

    // ItemInfo
    public ItemInfo GetItemData(int id)
    {
        if (!m_itemTable.ContainsKey(id))
        {
            //CLog.Log(string.Format("Not Exist ItemData ID : {0}", id), CLog.LogType.Error);
            return null;
        }
        return m_itemTable[id];
    }

    public string[] GetItemPrefabNameByType(ItemInfo_ItemType type)
    {
        List<string> list = new List<string>();

        Dictionary<int, ItemInfo>.Enumerator iter = m_itemTable.GetEnumerator();
        while(iter.MoveNext())
        {
            if (iter.Current.Value.ItemType == type)
                list.Add(iter.Current.Value.PrefabName);
        }

        return list.ToArray();
    }

    public string GetItemPrefabName(int id)
    {
        ItemInfo info = GetItemData(id);

        if(info == null)
        {
            return string.Empty;
        }

        return info.PrefabName;
    }

    public List<ItemAbilityInfo> GetItemAbilityData(int id)
    {
        ItemInfo info = GetItemData(id);

        if (info == null)
            return new List<ItemAbilityInfo>();

        return info.ListItemAbility;
    }

    public ItemInfo GetCoinData()
    {
        return GetItemData(1);
    }

    public ItemInfo GetCashData()
    {
        return GetItemData(2);
    }

    public List<ItemInfo> GetItemsByType(ItemInfo_ItemType type)
    {
        return (from ItemInfo in m_itemTable.Values
                where ItemInfo.ItemType == type
                select ItemInfo).ToList();
    }

    // AbilityInfo
    public AbilityInfo GetAbilityData(int id)
    {
        if (!m_abilityTable.ContainsKey(id))
        {
            //CLog.Log(string.Format("Not Exist Ability ID : {0}", id), CLog.LogType.Error);
            return null;
        }
        return m_abilityTable[id];
    }

    public StatInfo GetCharacterStat(int characterid)
    {
        PlayerData.UserCharacterItem character = PlayerData.instance.GetUserCharacterInfo(characterid);
        ItemInfo characterinfo = GetItemData(characterid);
        StatInfo stat = new StatInfo(characterid, (character == null ? 0 : character.EnhanceLevel), characterinfo.ListItemAbility);
        return stat;
    }

    public StatInfo GetCharacterStat(int characterid, int level)
    {
        ItemInfo characterinfo = GetItemData(characterid);
        StatInfo stat = new StatInfo(characterid, level, characterinfo.ListItemAbility);
        return stat;
    }

    public StatInfo GetItemStat(int itemid, int level, ItemAbility_EnhanceType type)
    {
        ItemInfo itemInfo = GetItemData(itemid);
        StatInfo stat = new StatInfo(itemid, level, itemInfo.ListItemAbility, type);
        return stat;
    }

    public SkillInfo GetCharacterSkill(int characterid, bool checkOpenLevel)
    {
        PlayerData.UserCharacterItem character = PlayerData.instance.GetUserCharacterInfo(characterid);
        SkillInfo skill = (character == null ? GetCharacterSkill(characterid, 0) : new SkillInfo(character, checkOpenLevel));
        return skill;
    }

    public SkillInfo GetCharacterSkill(int characterid)
    {
        PlayerData.UserCharacterItem character = PlayerData.instance.GetUserCharacterInfo(characterid);
        SkillInfo skill = (character == null ? GetCharacterSkill(characterid, 0) : new SkillInfo(character));
        return skill;
    }

    public SkillInfo GetCharacterSkillbyItemAbilityId(int itemAbilityId)
    {
        PlayerData.UserCharacterItem character = null;
        for (int i = 0; i < PlayerData.instance.GetUserCharacterList().Count; i++)
        {
            character = PlayerData.instance.GetUserCharacterList()[i];
            PlayerData.UserItemSkill skilldata = character.CharacterSkillList.Find(item => item.ItemAbilityId == itemAbilityId);

            if (skilldata != null)
                break;
        }

        return (character == null ? GetCharacterSkill(character.ItemId, 0) : new SkillInfo(character));
    }

    public SkillInfo GetCharacterSkill(int characterid, int level)
    {
        ItemInfo characterinfo = GetItemData(characterid);
        SkillInfo skill = new SkillInfo(characterid, level, characterinfo.ListItemAbility, ItemAbility_EnhanceType.SkillLevel);
        return skill;
    }

    public SkillInfo GetEnemySkill(int enemyid, int enhanceLevel)
    {
        ItemInfo monbsterInfo = GetItemData(enemyid);
        SkillInfo skill = new SkillInfo(enemyid, enhanceLevel, monbsterInfo.ListItemAbility, ItemAbility_EnhanceType.EnemyLevel);
        return skill;
    }

    // 캐릭터 각성, 보물 진화 상태 체크
    public ItemEnhanceStateInfo GetItemEvolveState(PlayerData.UserItem useritem)
    {
        // 현재 상태
        ItemInfo itemInfo = GetItemData(useritem.ItemId);
        ItemEnhanceStateInfo enhancestate = GetItemEnhanceState(useritem);

        int LevelAbleToCharacterEvolve = ConfigTableMgr.instance.GetConfigData("LevelAbleToCharacterEvolve");

        bool bablecheckevolve = false;
        switch(itemInfo.ItemType)
        {
            case ItemInfo_ItemType.Character:
                {
                    if (enhancestate.State == ItemEnhanceState.MaxEnhance &&
                       enhancestate.AbleBreakLimitLevel == LevelAbleToCharacterEvolve)
                    {
                        bablecheckevolve = true;
                    }
                }
                break;

            case ItemInfo_ItemType.Treasure:
                {
                    bablecheckevolve = true;
                }
                break;
        }

        if (bablecheckevolve && useritem.IsEvolve == 0)
        {
            bool bcoin = IsItemEvolve(useritem, false);
            bool bcash = IsItemEvolve(useritem, true);

            if (bcoin && !bcash)
            {
                enhancestate.State = ItemEnhanceState.AbleEvolve_Coin;
            }
            else if (!bcoin && bcash)
            {
                enhancestate.State = ItemEnhanceState.AbleEvolve_Cash;
            }
            else if(bcoin && bcash)
            {
                enhancestate.State = ItemEnhanceState.AbleEvolve_CoinCash;
            }
            else
            {
                enhancestate.State = ItemEnhanceState.None;
            }
        }
        else
        {
            enhancestate.State = ItemEnhanceState.None;
        }

        return new ItemEnhanceStateInfo(enhancestate.State, enhancestate.AbleBreakLimitLevel);
    }

    public ItemEnhanceStateInfo GetItemEnhanceState(PlayerData.UserItem item)
    {
        ItemInfo iteminfo = ItemTableMgr.instance.GetItemData(item.ItemId);
        ItemEnhanceStateInfo enhancestate = new ItemEnhanceStateInfo(ItemTableMgr.ItemEnhanceState.None, 0);

        if (iteminfo.ItemType == ItemInfo_ItemType.Character)
        {
            enhancestate = GetCharacterBreakLimitState(item.ItemId);
        }
        else if(iteminfo.ItemType == ItemInfo_ItemType.Treasure)
        {
            enhancestate = GetTreasureEnhanceState(item.UID);
        }

        return enhancestate;
    }

    // 캐릭터 한계돌파 상태 체크
    private ItemEnhanceStateInfo GetCharacterBreakLimitState(int characterid)
    {
        PlayerData.UserCharacterItem character = PlayerData.instance.GetUserCharacterInfo(characterid);
        // 현재 상태 
        ItemEnhanceState state = ItemEnhanceState.NotEnoughMaterial;

        int defaultmaxlevel = ConfigTableMgr.instance.GetConfigData("DefaultCharacterMaxLevel");
        int plusCharacterMaxLevelPerBreakLimit = ConfigTableMgr.instance.GetConfigData("PlusCharacterMaxLevelPerBreakLimit");
        int LevelAbleToCharacterEvolve = ConfigTableMgr.instance.GetConfigData("LevelAbleToCharacterEvolve");

        // 현재 레벨시작이 어떻게 변할지 몰라서 1부터 시작하도록 exp값으로 환산해서 테스트
        //need to do
        int currentlevel = character.EnhanceLevel;
        int ablebreaklimitlevel = defaultmaxlevel + (character.NumberBreakLimit * plusCharacterMaxLevelPerBreakLimit);

        //PlayerData.instance.GetUserCharacterInfo(PlayerData.instance.m_CurCharacterId).EnhanceLevel = currentlevel;
        // 진화
        if (currentlevel == LevelAbleToCharacterEvolve)
        {
            state = ItemEnhanceState.MaxEnhance;
        }
        // 강화
        else
        {
            if (currentlevel < ablebreaklimitlevel)
            {
                // 레벨부족
                state = ItemEnhanceState.NotEnoughLevel;
            }
            //else if (currentlevel == ablebreaklimitlevel &&
            //        CharacterNeedExpForLevelUpTableMgr.instance.GetCharacterNeedToLevelUpExpPer(character) < 1)
            //{
            //    state = ItemEnhanceState.NotEnoughLevel;
            //}
            // 현재 필요없는 상태지만 차후에 보고 완전히 필요없어지면 소멸
            //else if(!IsCharacterBreakLimit(character)
            //{
            //    // 재료부족
            //    state = ItemEnhanceState.NotEnoughMaterial;
            //}
            else
            {
                state = ItemEnhanceState.AbleEnhance;
            }

        }

        return new ItemEnhanceStateInfo(state, ablebreaklimitlevel);
    }

    // 보물 강화 상태
    private ItemEnhanceStateInfo GetTreasureEnhanceState(long treasureuid)
    {
        PlayerData.UserTreasureItem treasure = PlayerData.instance.GetUserTreasureInfo(treasureuid);

        int MaxTreasureEnhanceStep = ConfigTableMgr.instance.GetConfigData("MaxTreasureEnhanceStep");
        // 현재 상태 
        ItemEnhanceState state = ItemEnhanceState.NotEnoughMaterial;

        if (treasure.EnhanceLevel == MaxTreasureEnhanceStep)
        {
            state = ItemEnhanceState.MaxEnhance;
        }
        else
        {
            ItemInfo treasureinfo = GetItemData(treasure.ItemId);

            WhatNeedToEnhanceInfo enhanceinfo = treasureinfo.ListWhatNeedToEnhance.Find(item => item.EnhanceLevel == treasure.EnhanceLevel + 1);

            ItemKeepState coinstate = PlayerData.instance.CheckKeepItem((int)ItemInfo_ItemType.Coin, enhanceinfo.CoinPrices);
            ItemKeepState cashstate = PlayerData.instance.CheckKeepItem((int)ItemInfo_ItemType.Cash, enhanceinfo.CashPrices);

            if (coinstate == ItemKeepState.KeepItem && cashstate != ItemKeepState.KeepItem)
            {
                state = ItemEnhanceState.AbleEnhance_Coin;
            }
            else if (coinstate != ItemKeepState.KeepItem && cashstate == ItemKeepState.KeepItem)
            {
                state = ItemEnhanceState.AbleEnhance_Cash;
            }
            else if (coinstate == ItemKeepState.KeepItem && cashstate == ItemKeepState.KeepItem)
            {
                state = ItemEnhanceState.AbleEnhance;
            }
        }

        return new ItemEnhanceStateInfo(state, 0);
    }

    public List<ItemInfo> GetTreasureEvolvedListFromThis(int treasureId)
    {
        List<ItemInfo> list = new List<ItemInfo>();

        foreach(List<WhatNeedToEvolveInfo> v in m_whatNeedToEvolveTable.Values)
        {
            for(int i = 0; i < v.Count; i++)
            {
                if (v[i].SourceItemId == treasureId)
                {
                    list.Add(GetItemData(v[i].ItemId));
                    break;
                }
            }    
        }

        return list;
    }

    public string GetItemAssetBundleNameFromPrefabName(string prefabName)
    {
        foreach(ItemInfo v in m_itemTable.Values)
        {
            if (v.PrefabName == prefabName)
                return v.AssetBundleName + ".unity3d";
        }

        return string.Empty;
    }

//     public ItemAbilityInfo GetAbilityValueInGameItem(int itemid)
//     {
//         ItemInfo item = ItemTableMgr.instance.GetItemData(itemid);
//         if (item.ListItemAbility.Count == 0)
//             return null;
// 
//         return item.ListItemAbility[0];
//     }

    public ItemAbilityInfo GetAbilityValueInGameItem(int itemid, Ability_Effect effecttype = Ability_Effect.None)
    {
        ItemInfo item = ItemTableMgr.instance.GetItemData(itemid);
        if (item.ListItemAbility.Count == 0)
            return null;

        if (item.ListItemAbility.Count == 1 && item.ListItemAbility[0].Ability.Effect == Ability_Effect.None)
            return item.ListItemAbility[0];

        for (int i = 0; i < item.ListItemAbility.Count; i++)
        {
            if (item.ListItemAbility[i].Ability.Effect == effecttype)
                return item.ListItemAbility[i];
        }

        return null;
    }

    // 장착, 보유효과 AbilityValue 값 찾기
    List<ItemAbilityValueInfo> GetApplyItemAbilityValueInfoList(List<PlayerData.UserTreasureItem> itemlist, Ability_Effect effectType, Ability_TriggerType triggertype, Ability_TriggerCondition condition)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();

        for(int i = 0; i < itemlist.Count; i++)
        {
            PlayerData.UserTreasureItem v = itemlist[i];

            ItemInfo item = ItemTableMgr.instance.GetItemData(v.ItemId);
            if (item.ListItemAbility != null)
            {
                for (int j = 0; j < item.ListItemAbility.Count; j++)
                {
                    if(item.ListItemAbility[j].Ability.Effect != effectType)
                        continue;

                    if (item.ListItemAbility[j].Ability.TriggerCondition != condition)
                        continue;

                    if (item.ListItemAbility[j].Ability.TriggerType == triggertype)
                    {
                        ItemAbilityValueInfo valueinfo = new ItemAbilityValueInfo(item.ListItemAbility[j].GetAbilityValueInfo(v.EnhanceLevel));
                        valueinfo.itemid = item.ItemId;
                        valueinfo.UID = v.UID;
                        if (itemlist[i].IsEvolve == 1 && itemlist[i].TreasureSkillList.Count > 0)
                        {
                            if (item.ListItemAbility[j].ItemAbilityId == itemlist[i].TreasureSkillList[0].ItemAbilityId)
                                valueinfo = ApplyTreasureEvolveValue(valueinfo, item.ListTreasureEvolveResult.Find(treasureItem => treasureItem.ItemAbilityId == itemlist[i].TreasureSkillList[0].ItemAbilityId), itemlist[i].TreasureSkillList[0].Value);
                        }

                        list.Add(valueinfo);
                        //break;
                    }
                }
            }
        }

        return list;
    }
    List<ItemAbilityValueInfo> GetApplyItemAbilityValueInfoListByCondition(List<PlayerData.UserTreasureItem> itemlist, Ability_TriggerType triggertype, Ability_TriggerCondition condition)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();

        for (int i = 0; i < itemlist.Count; i++)
        {
            PlayerData.UserTreasureItem v = itemlist[i];

            ItemInfo item = ItemTableMgr.instance.GetItemData(v.ItemId);
            if (item.ListItemAbility != null)
            {
                for (int j = 0; j < item.ListItemAbility.Count; j++)
                {
                    if(triggertype != Ability_TriggerType.None)
                    {
                        if(item.ListItemAbility[j].Ability.TriggerType != triggertype)
                            continue;
                    }

                    if (item.ListItemAbility[j].Ability.TriggerCondition == condition)
                    {
                        ItemAbilityValueInfo valueinfo = new ItemAbilityValueInfo(item.ListItemAbility[j].GetAbilityValueInfo(v.EnhanceLevel));
                        if (itemlist[i].IsEvolve == 1 && itemlist[i].TreasureSkillList.Count > 0)
                        {
                            if (item.ListItemAbility[j].ItemAbilityId == itemlist[i].TreasureSkillList[0].ItemAbilityId)
                                valueinfo = ApplyTreasureEvolveValue(valueinfo, item.ListTreasureEvolveResult.Find(treasureItem => treasureItem.ItemAbilityId == itemlist[i].TreasureSkillList[0].ItemAbilityId), itemlist[i].TreasureSkillList[0].Value);
                        }

                        list.Add(valueinfo);
                        break;
                    }
                }
            }
        }

        return list;
    }

    ItemAbilityValueInfo ApplyTreasureEvolveValue(ItemAbilityValueInfo valueinfo, TreasureEvolveResultInfo evolveInfo, int value)
    {
        if (evolveInfo == null)
            return valueinfo;

        if (evolveInfo != null)
        {
            switch (evolveInfo.ValueType)
            {
                case TreasureEvolveResult_ValueType.Value2:
                    valueinfo.Value2 += value;
                    break;

                case TreasureEvolveResult_ValueType.TriggerConditionValue2:
                    valueinfo.TriggerConditionValue2 += value;
                    break;

                case TreasureEvolveResult_ValueType.MaxCoolTime:
                    valueinfo.MaxCoolTime += Utils.RoundValue(value * 0.01f);
                    break;

                case TreasureEvolveResult_ValueType.MinCoolTime:
                    valueinfo.MinCoolTime += Utils.RoundValue(value * 0.01f);
                    break;

                case TreasureEvolveResult_ValueType.ExecuteChance:
                    valueinfo.ExecuteChance += Utils.RoundValue(value * 0.01f);
                    break;
            }
        }
        return valueinfo;
    }

    public ItemInfo GetItemInfoByPrefabName(string name)
    {
        if(name != null && m_itemTablebyPrefabName.ContainsKey(name))
        {
            return m_itemTablebyPrefabName[name];
        }

        return null;
    }

    public ItemInfo GetItemInfoByInGameItemType(TomInGameObject.TYPE type)
    {
        if (type != TomInGameObject.TYPE.None)
        {
            Dictionary<int, ItemInfo>.Enumerator iter = m_itemTable.GetEnumerator();
            while (iter.MoveNext())
            {
                if (iter.Current.Value.InGameItemType == type)
                    return iter.Current.Value;
            }
        }

        return null;
    }

    public List<ItemInfo> GetItemInfoListByInGameItemType(TomInGameObject.TYPE type)
    {
        List<ItemInfo> info = new List<ItemInfo>();
        if (type != TomInGameObject.TYPE.None)
        {
            Dictionary<int, ItemInfo>.Enumerator iter = m_itemTable.GetEnumerator();
            while (iter.MoveNext())
            {
                if (iter.Current.Value.InGameItemType == type)
                    info.Add(iter.Current.Value);
            }
        }

        return info;
    }

    public int GetItemAbilityId(int itemId, int AbilityId)
    {
        ItemInfo info = GetItemData(itemId);

        if(info != null)
        {
            ItemAbilityInfo itemAbility = info.ListItemAbility.Find(item => item.AbilityId == AbilityId);
            if (itemAbility != null)
                return itemAbility.ItemAbilityId;    
        }

        return 0;
    }

    // 부스터 아이템
    public List<ItemAbilityValueInfo> GetApplyAbilityBuyBoosterItem(Ability_Effect effectType, Ability_TriggerCondition condition, int value)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();
        if (effectType == Ability_Effect.None)
            return list;

        List<OngoingGameData.ItemInfo> itemlist = new List<OngoingGameData.ItemInfo>();
        itemlist.AddRange(OngoingGameDataMgr.instance.GetUseItemList().Values);

        for(int i = 0; i < itemlist.Count; i++)
        {
#if NEW_NETWORK_MODULE
			ItemInfo item = ItemTableMgr.instance.GetItemData(itemlist[i].i);
#else
			ItemInfo item = ItemTableMgr.instance.GetItemData(itemlist[i].ItemId);
#endif
			List<ItemAbilityInfo> useitem = item.ListItemAbility;
            if(useitem.Count > 0 && useitem[0].ListItemAbilityEnhanceValue.Count > 0)
            {
                if (useitem[0].Ability.TriggerCondition != condition)
                    continue;

                if(useitem[0].Ability.Effect == effectType)
                {
                    ItemAbilityValueInfo valueInfo = new ItemAbilityValueInfo(useitem[0].ListItemAbilityEnhanceValue[0]);
                    valueInfo.itemid = item.ItemId;
                    list.Add(valueInfo);
                }
            }
        }
        
        return list;
    }

    // 부스터 랜덤 아이템
    public List<ItemAbilityValueInfo> GetApplyAbilityBuyRandomBoosterItem(Ability_Effect effectType, Ability_TriggerCondition condition)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();
        if (effectType == Ability_Effect.None)
            return list;

        if (PlayerData.instance.m_RandomBooster > 0)
        {
            InGameRandomItemInfo randominfo = InGameRandomItemTableMgr.instance.GetInGameRandomItem(PlayerData.instance.m_RandomBooster);
            if(randominfo != null)
            {
                for (int i = 0; i < randominfo.ApplyAbility.Count; i++)
                {
                    if (ItemTableMgr.instance.GetAbilityData(randominfo.ApplyAbility[i].AbilityId).Effect != effectType)
                        continue;

                    if (ItemTableMgr.instance.GetAbilityData(randominfo.ApplyAbility[i].AbilityId).TriggerCondition != condition)
                        continue;

                    ItemAbilityValueInfo info = new ItemAbilityValueInfo(randominfo.ApplyAbility[i].Value1, randominfo.ApplyAbility[i].Value2, (randominfo.ApplyAbility[i].UseAblePercent ? 1 : 0));
                    info.itemid = 300004;
                    list.Add(info);
                }
            }
        }

        return list;
    }

    // 액티브 스킬 데이터
    public List<ItemAbilityValueInfo> GetApplyActiveSkill(int characterid)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();

        SkillInfo info = GetCharacterSkill(characterid);

        for(int i = 0; i < info.SkillList.Count; i++)
        {
            if(info.SkillList[i].ItemAbilityData.Ability.TriggerType == Ability_TriggerType.Active)
            {
                list.Add(info.SkillList[i].ItemAbilityData.GetAbilityValueInfo(info.SkillList[i].EnhanceLevel));
                break;
            }
        }

        return list;
    }

    // 보물
    public List<ItemAbilityValueInfo> GetApplyAbilityTreasureItem(Ability_Effect effectType, Ability_TriggerCondition condition)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();
        list.AddRange(GetApplyItemAbilityValueInfoList(PlayerData.instance.GetEquipUserTreasureList(), effectType, (condition == Ability_TriggerCondition.None ? Ability_TriggerType.Passive : Ability_TriggerType.Buff), condition));
        list.AddRange(GetApplyItemAbilityValueInfoList(PlayerData.instance.GetUserTreasureList(), effectType, ItemTableMgr.Ability_TriggerType.EffectiveWhileHave, condition));

        return list;
    }

    public List<ItemAbilityValueInfo> GetApplyAbilityTreasureItemByCondition(Ability_TriggerCondition condition)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();
        list.AddRange(GetApplyItemAbilityValueInfoListByCondition(PlayerData.instance.GetEquipUserTreasureList(), (condition == Ability_TriggerCondition.None ? Ability_TriggerType.None : Ability_TriggerType.Passive), condition));
        list.AddRange(GetApplyItemAbilityValueInfoListByCondition(PlayerData.instance.GetUserTreasureList(), ItemTableMgr.Ability_TriggerType.EffectiveWhileHave, condition));

        return list;
    }

    // 캐릭터 스킬
    public List<ItemAbilityValueInfo> GetApplyAbilityCharacterSkill(int itemid, Ability_Effect effectType, Ability_TriggerCondition condition = Ability_TriggerCondition.None)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();

        SkillInfo skillinfo = ItemTableMgr.instance.GetCharacterSkill(itemid, true);
        List<InGameAbilityValueInfo> skillvaluelist = skillinfo.SkillList; // skillinfo.SkillList.FindAll(item => item.ItemAbilityData.Ability.TriggerType == ItemTableMgr.Ability_TriggerType.Passive);

        for(int i = 0; i < skillvaluelist.Count; i++)
        {
            if (condition != skillvaluelist[i].ItemAbilityData.Ability.TriggerCondition)
                continue;

            if (skillvaluelist[i].ItemAbilityData.Ability.Effect == effectType)
                list.Add(skillvaluelist[i].GetAibilityValueInfo(skillvaluelist[i].EnhanceLevel));
        }

        return list;
    }

    public List<ItemAbilityValueInfo> GetApplyAbilityCharacterSkillByCondition(int itemid, Ability_Effect effectType, Ability_TriggerCondition condition)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();

        SkillInfo skillinfo = ItemTableMgr.instance.GetCharacterSkill(itemid, true);
        List<InGameAbilityValueInfo> skillvaluelist = skillinfo.SkillList.FindAll(item => item.ItemAbilityData.Ability.TriggerCondition == condition);

        for (int i = 0; i < skillvaluelist.Count; i++)
        {
            if (skillvaluelist[i].ItemAbilityData.Ability.Effect == effectType)
                list.Add(skillvaluelist[i].GetAibilityValueInfo(skillvaluelist[i].EnhanceLevel));
        }

        return list;
    }

    // 팀효과
    public List<ItemAbilityValueInfo> GetApplyAbilityTeamEffect(Ability_Effect effectType)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();
        List<TeamEffectInfo> teamEffectlist = TeamEffectTableMgr.instance.GetAbleEffectiveTeamEffectList();

        for(int i = 0; i  < teamEffectlist.Count; i++)
        {
            if (ItemTableMgr.instance.GetAbilityData(teamEffectlist[i].AbilityId).Effect != effectType)
                continue;

            ItemAbilityValueInfo info = new ItemAbilityValueInfo(teamEffectlist[i].Value, 0);
            list.Add(info);
        }

        return list;
    }

    public float GetApplyAbilityValue(int targetItemId, List<ItemAbilityValueInfo> list, long value)
    {
        float val = 0;
        int percent = 0;
        for(int i = 0; i < list.Count; i ++)
        {
            if (targetItemId != list[i].Value1)
                continue;

            // Apply chance
            if(list[i].ExecuteChance > 0)
            {
                int rand = UnityEngine.Random.Range(0, 100);
                if (list[i].ExecuteChance < rand)
                    continue;
            }

            if (list[i].UseAblePercent == 0)
                val += list[i].Value2;
            else
                percent += list[i].Value2;
        }

        if(percent > 0)
        {
            val += (value * Utils.RoundValue(percent * 0.0001f));
        }

        return val;
    }

    public float GetApplyAbilityValue(ItemAbilityValueInfo info, long value)
    {
        float val = 0;
        int percent = 0;

        // Apply chance
        if (info.ExecuteChance > 0)
        {
            int rand = UnityEngine.Random.Range(0, 100);
            if (info.ExecuteChance < rand)
                return 0;
        }

        if (info.UseAblePercent == 0)
            val += info.Value2;
        else
            percent += info.Value2;

        if (percent > 0)
            val += (value * Utils.RoundValue(percent * 0.0001f));

        return val;
    }

    ItemAbilityValueInfo GetBuddyAbilityValueInfo(int id, Ability_Effect effectType, Ability_TriggerCondition condition)
    {
        ItemInfo info = GetItemData(id);
        if (info != null)
        {
            List<ItemAbilityInfo> useitem = info.ListItemAbility;
            if (useitem.Count > 0 && useitem[0].ListItemAbilityEnhanceValue.Count > 0)
            {
                if (useitem[0].Ability.TriggerCondition == condition)
                {
                    if (useitem[0].Ability.Effect == effectType)
                        return useitem[0].ListItemAbilityEnhanceValue[0];
                }
            }
        }
        return null;
    }

    public List<ItemAbilityValueInfo> GetAdditiveBuddyAbilityList(int characterId, Ability_Effect effectType, long value, Ability_TriggerCondition condition = Ability_TriggerCondition.None)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();
        if(GameDefine.Version_Client > 3)
        {
            PlayerData.UserBuddyItem buddyData = PlayerData.instance.GetEquipBuddy(characterId);
            if (buddyData != null)
            {
                if (buddyData.Level0_AbilityID > 0)
                {
                    ItemAbilityValueInfo valueinfo = GetBuddyAbilityValueInfo(buddyData.Level0_AbilityID, effectType, condition);

                    if (valueinfo != null)
                    {
                        bool ismater = PlayerData.instance.IsBuddyMaster(buddyData.UID);
                        bool issoulmate = buddyData.IsSoulMate;

                        if (ismater || issoulmate)
                        {
                            valueinfo = new ItemAbilityValueInfo(valueinfo);
                            int percent = 0;
                            if (issoulmate)
                                percent += ConfigTableMgr.instance.GetConfigData("SoulMateIncreaseValue");

                            if (ismater)
                                percent += ConfigTableMgr.instance.GetConfigData("MasterIncreaseValue");

                            if (percent > 0)
                                valueinfo.Value2 += (int)(valueinfo.Value2 * Utils.RoundValue(percent * 0.0001f));
                        }

                        list.Add(valueinfo);
                    }
                }
                if (buddyData.Level1_AbilityID > 0)
                {
                    ItemAbilityValueInfo valueinfo = GetBuddyAbilityValueInfo(buddyData.Level1_AbilityID, effectType, condition);
                    if (valueinfo != null)
                        list.Add(valueinfo);
                }
                if (buddyData.Level2_AbilityID > 0)
                {
                    ItemAbilityValueInfo valueinfo = GetBuddyAbilityValueInfo(buddyData.Level2_AbilityID, effectType, condition);
                    if (valueinfo != null)
                        list.Add(valueinfo);
                }
                if (buddyData.Level3_AbilityID > 0)
                {
                    ItemAbilityValueInfo valueinfo = GetBuddyAbilityValueInfo(buddyData.Level3_AbilityID, effectType, condition);
                    if (valueinfo != null)
                        list.Add(valueinfo);
                }
            }
        }
        return list;
    }

    public List<ItemAbilityValueInfo> GetAdditiveAbilityList(int characterId, int targetItemId, Ability_Effect effectType, long value, Ability_TriggerCondition condition = Ability_TriggerCondition.None)
    {
        List<ItemAbilityValueInfo> list = new List<ItemAbilityValueInfo>();
        // 골드맵에서는 캐릭터 능력치만 들어간다
        if(!PlayerData.instance.IsGoldEpisode)
        {
            //if (PlayerData.instance.GetCurCharacterID() == characterId)
            {
                // 부스터 아이템
                list.AddRange(GetApplyAbilityBuyBoosterItem(effectType, condition, (int)value));
                // 부스터 랜덤아이템
                list.AddRange(GetApplyAbilityBuyRandomBoosterItem(effectType, condition));
            }

            // 버디
            list.AddRange(GetAdditiveBuddyAbilityList(characterId, effectType, value, condition));
            // 보물
            list.AddRange(GetApplyAbilityTreasureItem(effectType, condition));
        }
        
        // 캐릭터 스킬
        if (characterId > 0)
            list.AddRange(GetApplyAbilityCharacterSkill(characterId, effectType, condition));
        // 팀효과
        //list.AddRange(GetApplyAbilityTeamEffect(effectType));

        return list;
    }

    // 캐릭터 기본능력치를 제외한 추가되는 ( 패시브, 버프, 팀효과 등) 어빌리티 값은 모두 여기서 추가되도록 한다
    public float GetAdditiveAbilityValue(int characterId, int targetItemId, Ability_Effect effectType, long value, Ability_TriggerCondition condition = Ability_TriggerCondition.None)
    {
        List<ItemAbilityValueInfo> list = GetAdditiveAbilityList(characterId, targetItemId, effectType, value, condition);
        return GetApplyAbilityValue(targetItemId, list, value);
    }

    public float GetAdditiveRandomItemAbilityValue(Ability_Effect effectType, Ability_TriggerCondition condition)
    {
        List<ItemAbilityValueInfo> list = GetApplyAbilityBuyRandomBoosterItem(effectType, condition);
        return GetApplyAbilityValue(0, list, 0);
    }

    public float GetAbilityCoolTime(float min, float max)
    {
        float cooltime = UnityEngine.Random.Range(min, max);
        return (float)Math.Round(cooltime, 1);
    }

    public int GetCharacterIdByCharacterPieceId(int pieceId)
    {
        List<int> characterIds = new List<int>();
        int[] id = m_itemTable.Keys.ToArray();
        for(int i = 0; i < id.Length; i++)
        {
            if (m_itemTable.ContainsKey(id[i]) && m_itemTable[id[i]].ItemType == ItemInfo_ItemType.Character)
                characterIds.Add(id[i]);
        }

        for(int i= 0; i < characterIds.Count; i++)
        {
            for(int j = 0; j < m_itemTable[characterIds[i]].ListItemAbility.Count; j++)
            {
                int itemabilityid = m_itemTable[characterIds[i]].ListItemAbility[j].ItemAbilityId;
                List<WhatNeedToSkillEnhanceInfo> enhanceInfo = WhatNeedToSkillEnhanceTableMgr.instance.GetSkillEnhanceData(itemabilityid);
                if (enhanceInfo != null && enhanceInfo.Count > 0)
                {
                    if (enhanceInfo[0].ResourceItemId == pieceId)
                        return characterIds[i];
                }
            }
        }

        return 0;
    }


    public Dictionary<int, AbilityInfo> GetAbilityTable()
    {
        return m_abilityTable;
    }
    public Dictionary<int, List<ItemAbilityInfo>> GetItemAbilityTable()
    {
        return m_itemAbilityTable;
    }
    public Dictionary<int, List<ItemAbilityValueInfo>> GetItemAbilityValueTable()
    {
        return m_itemAbilityValueTable;
    }
    public Dictionary<string, List<WhatNeedToEnhanceInfo>> GetWhatNeedToEnhanceTable()
    {
        return m_whatNeedToEnhanceTable;
    }
    public Dictionary<int, List<WhatNeedToEvolveInfo>> GetWhatNeedToEvolveTable()
    {
        return m_whatNeedToEvolveTable;
    }
    public Dictionary<int, List<WhatNeedToBreakLimitInfo>> GetWhatNeedToBreakLimitTable()
    {
        return m_whatNeedToBreakLimitTable;
    }
    public Dictionary<string, List<ChanceOfGreatSuccessInfo>> GetChanceOfGreatSuccessTable()
    {
        return m_chanceOfGreatSuccessTable;
    }
    public List<TreasureEvolveSupporterInfo> GetTreasureEvolveSupporterTable()
    {
        return m_treasureEvolveSupporterTable;
    }
    public Dictionary<int, List<TreasureEvolveResultInfo>> GetTreasureEvolveResultTable()
    {
        return m_treasureEvolveResultTable;
    }
    public Dictionary<int, ItemInfo> GetItemTable()
    {
        return m_itemTable;
    }

    public void ClearData()
    {
        m_abilityTable.Clear();
        m_itemAbilityTable.Clear();
        m_itemAbilityValueTable.Clear();
        m_whatNeedToEnhanceTable.Clear();
        m_whatNeedToEvolveTable.Clear();
        m_whatNeedToBreakLimitTable.Clear();
        m_chanceOfGreatSuccessTable.Clear();
        m_treasureEvolveSupporterTable.Clear();
        m_treasureEvolveResultTable.Clear();
        m_itemTable.Clear();
    }
}
