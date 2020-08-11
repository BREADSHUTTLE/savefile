using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TangentFramework;
using JaykayFramework;

[Serializable]
public partial class PlayerData : MonoSingleton<PlayerData>, IListener
{
    public static readonly string playerDataSaveName = "_playerdata_";

    #region Enum
    public enum Gamemode
    {
        AdventureRun = 0,
        TimeAttack = 1,
        AdventureRunTutorial = 2,
        Max
    }

    public enum ResultBoxType
    {
        nomal = 200014,
        rare,
        eric,
        unique
    }

    public RankingTableMgr.RankingRewardType m_CurRankingType = RankingTableMgr.RankingRewardType.Daily;
    #endregion

    #region Member variable
    public bool IsInvincibleButCanReduceHPByTime = false;
    public bool IsInvincibleEditorMode = false;
    public bool IsWaitFriendCheck = false;
    public bool IsMailRequest = false;
    public bool IsServerWorking = false;
    public bool availableSendErrorLog = false;
    public int passed_iOSVersion = 0;
    public string m_WebViewURL = "";
    public string ServerSendLog = "";
    public string AdUID = "";
    public float AdOpenPrevSec = 0;
    public GamePath.EnumServerType ServerType = GamePath.EnumServerType.Dev;
    public bool usedRevival = false;
        
    /// <summary>
    /// user unique ID
    /// </summary>
    public int m_UserId = -1;
    /// <summary>
    /// -1: block, 0: normal user, 3: Tangent
    /// </summary>
    private int m_userCode = -1;

    /// <summary>
    /// dev 유저 중 dev in game view 를 옵션에서 조정 할 수 있도록 하기 위해 만듦. (ListItemInGameDevView)
    /// </summary>
    private bool isDevUserViewState = true;

    /// <summary>
    /// 유니티 에디터 상에서 in game 점수 아이템을 획득 시, 머리 위에 텍스트 나올지에 대한 여부
    /// </summary>
    private bool isShowScoreHUDText = true;

    private long m_coin = 0;
    private long m_cash = 0;
    private int m_stamina = 0;
    public DateTime m_StaminaTime = System.DateTime.Now;
    public double StaminaOldTime = 0;
    public int m_StaminaTimeSeconds;
    private int m_stamina2 = 0;
    public DateTime m_Stamina2Time = System.DateTime.Now;
    public int m_Stamina2TimeSeconds;
#if GSP_PLATFORM_GLOBAL
    public DateTime m_ServerDateTime = System.DateTime.UtcNow;
#else
    public DateTime m_ServerDateTime = System.DateTime.Now;
#endif

    public string m_NickName;
    public int m_NumChangeNickName;
    private int m_playerExp;
    private int m_playerLevel;

    private bool isLoadPush = false;
    private bool isPushOn = false;
    private bool isLocalPushStamina = false;
    private bool isLocalPushFreeDraw = false;
    private bool isLocalOptimization = false;   // 프레임 최적화
	private bool isEventPush = false;
    private int m_curEpisodeID = -1;
    private Gamemode m_curGamemode = Gamemode.Max;
    public string m_PushId = string.Empty;
    public string m_IAP_PayLoad = string.Empty;
    public int m_TodayReceiveSocialPoint = 0;
    public int m_SocialPoint = 0;
    public int m_RandomBooster = 0;
    public int rankingReward = 0;
    public int m_MedalShopTime = 0;
    public int m_AttendanceId = 0;
    public int m_AttendanceReceiveRewardCount = 0;
    public bool m_IsAllowtoreceivekakaomsg = true;

    //need to use default constant variable
    public long m_CurCharacterUID = 0;
    public int m_RelayCharacterId = 0;
    public int m_ExtendTreasureInvenNumber = 0;
    public int m_TreasureInvenNumber = 0;
    public int m_TreasureSlotNumber = 0;

    public int m_relaytype = 0;

    // set relayCharacter
    public int[] m_CurCharacterItemIDs = new int[2] { 0, 0 };
    public string m_RelayCharacterIDs = string.Empty;
    public List<PlayerData.UserCharacterItem> m_RelayCharacterList = new List<UserCharacterItem>();

    // 착용한 보물 리스트
    private List<UserTreasureItem> m_listEquipTreasure = new List<UserTreasureItem>();

    // 유저가 가지고 있는 캐릭터, 보물 리스트, 소비 아이템
    private List<UserCharacterItem> m_listCharacterItem = new List<UserCharacterItem>();
    private List<UserTreasureItem> m_listTreasureItem = new List<UserTreasureItem>();
    private List<UserBuddyItem> m_listBuddyItem = new List<UserBuddyItem>();
    private List<UserItem> m_listMaterialItem = new List<UserItem>();
    private List<UserItem> m_listConsumableItem = new List<UserItem>();

    // 가챠 무료 뽑기 시간
    private Dictionary<int, DateTime> m_listGachaFreeTime = new Dictionary<int, DateTime>();

    // ImageNotice
    private Dictionary<long, UserImageNotice> m_listImageNotice = new Dictionary<long, UserImageNotice>();

    // Market FirstPurchase check
    private Dictionary<int, int> m_firstPurchase = new Dictionary<int, int>();

    // EpisodeBestSroce ( EpisodeId, Score)
    private Dictionary<int, EpisodeBestScore> m_myEpisodeBestScore = new Dictionary<int, EpisodeBestScore>();
    private Dictionary<int, EpisodeBestScore> m_myEpisodeDistance = new Dictionary<int, EpisodeBestScore>();

    // checkKakao시 expired_or_invalid_refresh_token로 로그인 실패할때 
    // count 값이 0 : 재시도, 1 : 게임종료
    public int m_FailedKakaoLoginCount = 0;
#endregion

#region Data objects
    [Serializable]
    public class UserCharacterItem : UserItem
    {
        public int EnhanceLevel;
        public int Exp;
        
        public int NumberBreakLimit;

        public List<UserItemSkill> CharacterSkillList = new List<UserItemSkill>();

        public UserCharacterItem()
        {

        }

        // 오프라인모드
        public UserCharacterItem(int characterId)
        {
            long uid = (long)Local_PlayerDataStream.LoadLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_CharacterUID, (long)0);
            UID = uid + 1;
            ItemId = characterId;
            Number = 1;
            EnhanceLevel = ConfigTableMgr.instance.GetConfigData("DefaultCharacterLevel");
            Exp = 0;
            IsEvolve = 0;
            NumberBreakLimit = 0;
            List<ItemAbilityInfo> list = ItemTableMgr.instance.GetItemAbilityData(characterId);

            foreach(ItemAbilityInfo v in list)
            {
                CharacterSkillList.Add(new UserItemSkill(v));
            }

            Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_CharacterUID, UID);
        }

        public UserCharacterItem(CustomNetwork.ServerItemInfo info)
        {
#if NEW_NETWORK_MODULE
			UID = info.u;
			ItemId = info.i;
			Number = info.n;
			EnhanceLevel = info.l;
			Exp = info.x;
			IsEvolve = info.e;
			NumberBreakLimit = info.bl;
			ItemInfo characterinfo = ItemTableMgr.instance.GetItemData(ItemId);
			List<ItemAbilityInfo> list = ItemTableMgr.instance.GetItemAbilityData(ItemId);
			List<CustomNetwork.ItemSkill> skilllist = new List<CustomNetwork.ItemSkill>(info.sk);
#else
			UID = info.UID;
            ItemId = info.ItemId;
            Number = info.Number;
            EnhanceLevel = info.EnhanceLevel;
            Exp = info.Exp;
            IsEvolve = info.IsEvolve;
            NumberBreakLimit = info.NumberBreakLimit;
            ItemInfo characterinfo = ItemTableMgr.instance.GetItemData(ItemId);
            List<ItemAbilityInfo> list = ItemTableMgr.instance.GetItemAbilityData(ItemId);
            List<CustomNetwork.ItemSkill> skilllist = new List<CustomNetwork.ItemSkill>(info.Skill);
#endif
			foreach (ItemAbilityInfo v in list)
            {
#if NEW_NETWORK_MODULE
				CustomNetwork.ItemSkill skill = skilllist.Find(item => item.a == v.ItemAbilityId);
#else
				CustomNetwork.ItemSkill skill = skilllist.Find(item => item.AbilityId == v.ItemAbilityId);
#endif

				if (skill == null)
                {
                    skill = new CustomNetwork.ItemSkill();
#if NEW_NETWORK_MODULE
					skill.a = v.AbilityId;
					skill.t = UID;
					skill.l = v.DefaultLevel;
					skill.e = 0;
					skill.v = 0;
#else
					skill.AbilityId = v.AbilityId;
                    skill.User_Treasure_UID = UID;
                    skill.EnhanceLevel = v.DefaultLevel;
                    skill.IsEvolve = 0;
                    skill.Value = 0;
#endif
					skilllist.Add(skill);
                }
            }

            IncludeStat(skilllist);
            IncludeSkill(skilllist);
        }

        void IncludeStat( List<CustomNetwork.ItemSkill> SkillList)
        {
            if (SkillList.Count == 0)
                return;

            StatInfo stat = ItemTableMgr.instance.GetCharacterStat(ItemId, EnhanceLevel);
           // SetSkill(stat.GetStatInfo(ItemTableMgr.StatType.HP).ItemAbilityData, SkillList);
            SetSkill(stat.GetStatInfo(ItemTableMgr.StatType.ReduceHPSpeed).ItemAbilityData, SkillList);
            SetSkill(stat.GetStatInfo(ItemTableMgr.StatType.Speed).ItemAbilityData, SkillList);
           // SetSkill(stat.GetStatInfo(ItemTableMgr.StatType.Damage).ItemAbilityData, SkillList);
           // SetSkill(stat.GetStatInfo(ItemTableMgr.StatType.Armor).ItemAbilityData, SkillList);
            SetSkill(stat.GetStatInfo(ItemTableMgr.StatType.CriticalChance).ItemAbilityData, SkillList);
        }

        void IncludeSkill(List<CustomNetwork.ItemSkill> SkillList)
        {
            if (SkillList.Count == 0)
                return;

            SkillInfo skill = ItemTableMgr.instance.GetCharacterSkill(ItemId, 0);

            for(int i = 0; i < skill.SkillList.Count; i++)
            {
                InGameAbilityValueInfo v = skill.SkillList[i];
                SetSkill(v.ItemAbilityData, SkillList);
            }
        }

        void SetSkill(ItemAbilityInfo ability, List<CustomNetwork.ItemSkill> SkillList)
        {
            if (ability == null)
                return;

#if NEW_NETWORK_MODULE
			CustomNetwork.ItemSkill skill = SkillList.Find(item => item.a == ability.ItemAbilityId);
#else
			CustomNetwork.ItemSkill skill = SkillList.Find(item => item.AbilityId == ability.ItemAbilityId);
#endif
			if (skill == null)
                CharacterSkillList.Add(new UserItemSkill(ability));
            else
                CharacterSkillList.Add(new UserItemSkill(skill));
        }
    }

    [Serializable]
    public class UserBuddyItem 
    {

        public int CharacterId;
        public int ItemId;
        public long UID;
        public long CharacterUID;
        public bool IsEquiped;
        public bool IsSoulMate;
        public int EvolveLevel;
        public int Level0_AbilityID;
        public int Level1_AbilityID;
        public int Level2_AbilityID;
        public int Level3_AbilityID;
        public DateTime UpdateDate;
        public DateTime Evolve_sDate;
        public int Evolve_time;
        public bool isEvolve;

        public UserBuddyItem(CustomNetwork.Buddyinfo info)
        {
            SetUserBuddyItemData(info);
        }

        public void SetUserBuddyItemData(CustomNetwork.Buddyinfo info)
        {
#if NEW_NETWORK_MODULE
			ItemId = info.i;
			UID = info.u;
			CharacterId = info.ci;
			IsEquiped = info.eq == 1 ? true : false;
			Level0_AbilityID = info.a0;
			Level1_AbilityID = info.a1;
			Level2_AbilityID = info.a2;
			Level3_AbilityID = info.a3;
			UpdateDate = info.ud;
			EvolveLevel = info.l;
			CharacterUID = info.cu;
			IsSoulMate = info.s == 1 ? true : false;
			if (!DateTime.TryParse(info.ed, out Evolve_sDate))
				Evolve_sDate = DateTime.MinValue;

			Evolve_time = info.et;
			isEvolve = info.e > 0;
#else
			ItemId = info.ItemId;
            UID = info.uid;
            CharacterId = info.CharacterItemID;
            IsEquiped = info.Equiped == 1 ? true : false;
            Level0_AbilityID = info.Level0_AbilityID;
            Level1_AbilityID = info.Level1_AbilityID;
            Level2_AbilityID = info.Level2_AbilityID;
            Level3_AbilityID = info.Level3_AbilityID;
            UpdateDate = info.UpdateDate;
            EvolveLevel = info.Level;
            CharacterUID = info.Character_UID;
            IsSoulMate = info.Soul == 1 ? true : false;
            if (!DateTime.TryParse(info.Evolve_sDate, out Evolve_sDate))
                Evolve_sDate = DateTime.MinValue;

            Evolve_time = info.Evolve_time;
            isEvolve = info.isEvolve > 0;
#endif
		}
    }

    [Serializable]
    public class UserTreasureItem : UserItem
    {
        public int EnhanceLevel;
        public int EquipedSlotNumber;

        public List<UserItemSkill> TreasureSkillList = new List<UserItemSkill>();

        public UserTreasureItem()
        {
        }

        // 오프라인모드
        public UserTreasureItem(int treasureid)
        {
            long uid = (long)Local_PlayerDataStream.LoadLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_TreasureUID, (long)0);
            UID = uid + 1;
            ItemId = treasureid;
            Number = 1;
            EnhanceLevel = ConfigTableMgr.instance.GetConfigData("DefaultTreasureLevel");
            EquipedSlotNumber = 0;
            IsEvolve = 0;
            List<ItemAbilityInfo> list = ItemTableMgr.instance.GetItemAbilityData(treasureid);

            for(int i = 0; i < list.Count; i++)
            {
                ItemAbilityInfo v = list[i];
                TreasureSkillList.Add(new UserItemSkill(v));
            }

            Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_TreasureUID, UID);
        }   

        public UserTreasureItem(CustomNetwork.ServerItemInfo info)
        {
#if NEW_NETWORK_MODULE
			UID = info.u;
			ItemId = info.i;
			Number = info.n;
			EnhanceLevel = info.l;
			IsEvolve = info.e;
			EquipedSlotNumber = info.sn;

			if (info.sk != null)
			{
				for (int i = 0; i < info.sk.Length; i++)
				{
					CustomNetwork.ItemSkill v = info.sk[i];
					TreasureSkillList.Add(new UserItemSkill(v));
				}
			}
#else
			UID = info.UID;
            ItemId = info.ItemId;
            Number = info.Number;
            EnhanceLevel = info.EnhanceLevel;
            IsEvolve = info.IsEvolve;
            EquipedSlotNumber = info.EquipedSlotNumber;

            if (info.Skill != null)
            {
                for(int i = 0; i < info.Skill.Length; i++)
                {
                    CustomNetwork.ItemSkill v = info.Skill[i];
                    TreasureSkillList.Add(new UserItemSkill(v));
                }
            }
#endif
		}
        
        public UserTreasureItem(long uid, int itemid, int number)
        {
            UID = uid;
            ItemId = itemid;
            Number = number;
        } 
    }

    [Serializable]
    public class UserItem
    {
        public long UID;
        public int ItemId;
        public int IsEvolve;
        public int Number;

        public UserItem()
        {
        }

        // 오프라인모드
        public UserItem(ItemTableMgr.ItemInfo_ItemType type, int itemid, int number)
        {
            // Consumable
            if(type == ItemTableMgr.ItemInfo_ItemType.ConsumableItem ||
                type == ItemTableMgr.ItemInfo_ItemType.ExpPotion)
            {
                long uid = (long)Local_PlayerDataStream.LoadLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_ConsumableUID, (long)0);
                UID = uid + 1;
            }
            // Material
            else
            {
                UID = 0;
            }

            ItemId = itemid;
            IsEvolve = 0;
            Number = number;

            Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_ConsumableUID, UID);
        }

        public UserItem(CustomNetwork.ServerItemInfo info)
        {
#if NEW_NETWORK_MODULE
			UID = info.u;
			ItemId = info.i;
			Number = info.n;
#else
			UID = info.UID;
            ItemId = info.ItemId;
            Number = info.Number;
#endif
		}

        // 소비아이템 전용
        public UserItem(CustomNetwork.ConsumableItemInfo info)
        {
#if NEW_NETWORK_MODULE
			UID = info.u;
			ItemId = info.i;
			Number = info.n;
#else
			UID = info.UID;
            ItemId = info.ItemId;
            Number = info.Number;
#endif
		}

        // 재료전용
        public UserItem(CustomNetwork.MaterialInfo info)
        {
#if NEW_NETWORK_MODULE
			ItemId = info.i;
			Number = info.n;
#else
			ItemId = info.ItemId;
            Number = info.Number;
#endif
		}

        public UserItem(int itemid, int number)
        {
            ItemId = itemid;
            Number = number;
        }

        public UserItem(int itemid, long itemuid)
        {
            ItemId = itemid;
            UID = itemuid;
        }

        public UserItem(int itemid, long itemuid, int number)
        {
            ItemId = itemid;
            UID = itemuid;
            Number = number;
        }
    }

    [Serializable]
    public class UserItemSkill
    {
        public long UID;
        public int ItemAbilityId;
        public int EnhanceLevel;
        public int IsEvolved;
        public int Value;

        public UserItemSkill()
        {
            ItemAbilityInfo iteminfo = ItemTableMgr.instance.GetItemAbilityData(PlayerData.instance.GetCurCharacterID()).Find(item => item.ItemAbilityId == ItemAbilityId);
            if(iteminfo != null)
             EnhanceLevel = iteminfo.DefaultLevel;
        }

        public UserItemSkill(CustomNetwork.ItemSkill info)
        {
#if NEW_NETWORK_MODULE
			UID = info.u;
			ItemAbilityId = info.a;
			EnhanceLevel = info.l;
			IsEvolved = info.e;
			Value = info.v;
#else
			UID = info.UID;
            ItemAbilityId = info.AbilityId;
            EnhanceLevel = info.EnhanceLevel;
            IsEvolved = info.IsEvolve;
            Value = info.Value;
#endif
        }

        public UserItemSkill(ItemAbilityInfo info)
        {
            UID = 0;
            ItemAbilityId = info.ItemAbilityId;        
            EnhanceLevel = info.DefaultLevel;
            IsEvolved = 0;
            Value = 0;
        }
    }

    public class UserImageNotice
    {
        public long UID;
        public string ImageURL;
        public string LinkURL;

        public UserImageNotice(CustomNetwork.ImageNotice image)
        {
#if NEW_NETWORK_MODULE
			UID = image.u;
			ImageURL = image.i;
			LinkURL = image.l;
#else
			UID = image.UID;
            ImageURL = image.ImageURL;
            LinkURL = image.LinkURL;
#endif
		}
    }

    public class EpisodeBestScore
    {
        public string BestScore;
        public int LongestDistance;
        public int LastDistance;

        public EpisodeBestScore(CustomNetwork.EpisodeBestScoreInfo info)
        {
#if NEW_NETWORK_MODULE
			BestScore = info.b.ToString();
			LongestDistance = info.m;
			LastDistance = info.l;
#else
			BestScore = info.BestScore;
            LongestDistance = info.LongestDistance;
            LastDistance = info.LastDistance;
#endif
		}

        public EpisodeBestScore(string bestscore, int longestdistance, int lastdistance)
        {
            BestScore = bestscore;
            LongestDistance = longestdistance;
            LastDistance = lastdistance;
        }

    }
#endregion


#region Init
    public override void Init()
    {
        if(ListenerManager.instance != null)
            ListenerManager.instance.AddListener(this as IListener);
    }
#endregion

#region Function

    private void ClearItemList(ItemTableMgr.ItemInfo_ItemType itemtype)
    {
        switch (itemtype)
        {
            // 캐릭터는 삭제없음
            case ItemTableMgr.ItemInfo_ItemType.Treasure: { m_listTreasureItem.Clear(); } break;
            case ItemTableMgr.ItemInfo_ItemType.Material: { m_listMaterialItem.Clear(); } break;
            case ItemTableMgr.ItemInfo_ItemType.ConsumableItem: { m_listConsumableItem.Clear(); } break;

            default: break;
        }
    }

    public void SyncUserItem(CustomNetwork.ServerItemInfo info)
    {
#if NEW_NETWORK_MODULE
		ItemInfo iteminfo = ItemTableMgr.instance.GetItemData(info.i);
#else
		ItemInfo iteminfo = ItemTableMgr.instance.GetItemData(info.ItemId);
#endif
		if (iteminfo == null)
            return;

        switch (iteminfo.ItemType)
        {
            case ItemTableMgr.ItemInfo_ItemType.Character: { AddCharacter(new UserCharacterItem(info)); } break;
            case ItemTableMgr.ItemInfo_ItemType.Treasure: { AddTreasure(new UserTreasureItem(info)); } break;
            case ItemTableMgr.ItemInfo_ItemType.Material: { AddMaterial(new UserItem(info)); } break;
            case ItemTableMgr.ItemInfo_ItemType.ConsumableItem: case ItemTableMgr.ItemInfo_ItemType.ExpPotion: { AddConsumable(new UserItem(info)); } break;
            default: { } break;
        }
    }

    public void SyncUserBuddy(CustomNetwork.Buddyinfo[] info)
    {
        if (info == null)
            return;

        for(int i = 0; i < info.Length; i++)
        {
            AddBuddy(new UserBuddyItem(info[i]));
        }
       
    }

    public void SyncUserItem(CustomNetwork.ServerItemInfo[] infos, ItemTableMgr.ItemInfo_ItemType itemtype)
    {
        ClearItemList(itemtype);

        if (infos != null)
        {
            for(int i = 0; i < infos.Length; i++)
            {
                CustomNetwork.ServerItemInfo v = infos[i];
                if (itemtype != ItemTableMgr.ItemInfo_ItemType.Treasure && 
                    itemtype != ItemTableMgr.ItemInfo_ItemType.Character)
                {
#if NEW_NETWORK_MODULE
					if (v.n == 0)
#else
					if (v.Number == 0)
#endif
						continue;
                }

                SyncUserItem(v);
            }
        }    
    }

    public void SyncUserItem(CustomNetwork.MaterialInfo[] infos, ItemTableMgr.ItemInfo_ItemType itemtype)
    {
        ClearItemList(itemtype);

        if(infos != null)
        {
            for(int i = 0; i < infos.Length; i++)
            {
                CustomNetwork.MaterialInfo v = infos[i];
#if NEW_NETWORK_MODULE
				if (v.n == 0)
#else
				if (v.Number == 0)
#endif
					continue;

                CustomNetwork.ServerItemInfo item = new CustomNetwork.ServerItemInfo();
#if NEW_NETWORK_MODULE
				item.i = v.i;
				item.n = v.n;
#else
				item.ItemId = v.ItemId;
                item.Number = v.Number;
#endif
				SyncUserItem(item);
            }
        }
    }

    public void SyncUserItem(CustomNetwork.ConsumableItemInfo[] infos, ItemTableMgr.ItemInfo_ItemType itemtype)
    {
        ClearItemList(itemtype);

        if(infos != null)
        {
            for(int i = 0; i < infos.Length; i++)
            {
                CustomNetwork.ConsumableItemInfo v = infos[i];
#if NEW_NETWORK_MODULE
				if (v.n == 0)
#else
				if (v.Number == 0)
#endif
					continue;

                CustomNetwork.ServerItemInfo item = new CustomNetwork.ServerItemInfo();
#if NEW_NETWORK_MODULE
				item.u = v.u;
				item.i = v.i;
				item.n = v.n;
#else
				item.UID = v.UID;
                item.ItemId = v.ItemId;
                item.Number = v.Number;
#endif
				SyncUserItem(item);
            }
        }
    }

    public void SyncUserEquipTreasure(CustomNetwork.ServerItemInfo[] items)
    {
        m_listEquipTreasure.Clear();

        if(items != null && items.Length > 0)
        {
            for(int i = 0; i < items.Length; i++)
            {
                CustomNetwork.ServerItemInfo v = items[i];
                if (m_listEquipTreasure.Count <= m_TreasureSlotNumber &&
#if NEW_NETWORK_MODULE
					v.sn > 0)
#else
					v.EquipedSlotNumber > 0)
#endif
				{
                    m_listEquipTreasure.Add(new UserTreasureItem(v));
                }
                else
                {
                    // need to do 현재 내가 착용가능한 보물 슬롯 개수 한계치를 넘어감
                }
            }
        }
    }

    public void SyncUserEquipTreasure(int slotnumber, UserTreasureItem treasure)
    {
        if (treasure == null)
            return;

        if(slotnumber == 0)
        {
            GetUserTreasureInfo(treasure.UID).EquipedSlotNumber = slotnumber;
            int index = m_listEquipTreasure.FindIndex(item => item.UID == treasure.UID);

            if(index != -1)
            {
                m_listEquipTreasure.RemoveAt(index);
            }

        }
        else
        {
            int index = m_listEquipTreasure.FindIndex(item => item.EquipedSlotNumber == slotnumber);
            if (index != -1)
            {
                m_listEquipTreasure.RemoveAt(index);
            }

            UserTreasureItem equiptreasure = m_listTreasureItem.Find(item => item.EquipedSlotNumber == slotnumber);
            if(equiptreasure != null)
            {
                equiptreasure.EquipedSlotNumber = 0;
            }

            treasure.EquipedSlotNumber = slotnumber;
            m_listEquipTreasure.Add(treasure);
        }
    }

    public void SyncToTreasureInven(int treasureInven, int treasureSlot)
    {
        m_TreasureInvenNumber = treasureInven;
        m_TreasureSlotNumber = treasureSlot;
    }

    public void SyncToFreeGacha(CustomNetwork.GachaData[] freegacha)
    {
        m_listGachaFreeTime.Clear();

        if (freegacha != null)
        {
            for(int i = 0; i < freegacha.Length; i++)
            {
                CustomNetwork.GachaData v = freegacha[i];
#if NEW_NETWORK_MODULE
				m_listGachaFreeTime[v.g] = v.t;
				PushFreegacha(v.g);
#else
				m_listGachaFreeTime[v.GachaId] = v.CreateTime;
                PushFreegacha(v.GachaId);
#endif
			}
        }
    }

    public void SetFreeGacha(int gachaid, DateTime freecooltime)
    {
        m_listGachaFreeTime[gachaid] = freecooltime;
        PushFreegacha(gachaid);
    }

    void PushFreegacha(int gachaid)
    {
        string _strPushMsg = LocalizationMgr.GetMessage(11901);
        if (gachaid == 3)
            _strPushMsg = LocalizationMgr.GetMessage(11902);

        System.DateTime time = PlayerData.instance.GetFreeGachaCoolTime(gachaid);
        GachaInfo info = GachaTableMgr.instance.GetGachaInfo(gachaid);
        System.TimeSpan _coolTime = time.AddSeconds((double)info.FreeCoolTime) - System.DateTime.Now;
        if (_coolTime.TotalSeconds > 0)
            NativeHelperContainer.pushHelper.CreateScheduledNotifications((int)_coolTime.TotalSeconds, GameDefine.AppName, _strPushMsg, (gachaid == 1 ? PushHelper.PushId.FreeDrawTreasure : PushHelper.PushId.FreeDrawCharacter), null, GameDefine.UTNotificationProfile);
    }


    public void SyncToImageNotice(CustomNetwork.ImageNotice[] images)
    {
        m_listImageNotice.Clear();

        if(images != null)
        {
            for(int i = 0;  i < images.Length; i++)
            {
                CustomNetwork.ImageNotice v = images[i];
#if NEW_NETWORK_MODULE
				m_listImageNotice[v.u] = new UserImageNotice(v);
#else
				m_listImageNotice[v.UID] = new UserImageNotice(v);
#endif
			}
        }
    }

#region OfflineMode

    public void SyncToCharacter_Local(UserCharacterItem[] characters)
    {
        for(int i = 0; i < characters.Length; i++)
        {
            UserCharacterItem v = characters[i];
            AddCharacter(v);
        }
    }

    public void SyncToTreasure_Local(UserTreasureItem[] treasures)
    {
        m_listTreasureItem.Clear();

        for(int i = 0; i < treasures.Length; i++)
        {
            UserTreasureItem v = treasures[i];
            AddTreasure(v);
        }
    }

    public void SyncToEquipTreasure_Local(List<UserTreasureItem> treasureEquip)
    {
        m_listEquipTreasure = treasureEquip;
    }

    public void SyncToConsumable_Local(UserItem[] consumables)
    {
        m_listConsumableItem.Clear();

        for(int i = 0; i < consumables.Length; i++)
        {
            UserItem v = consumables[i];
            AddConsumable(v);
        }
    }

    public void SyncToMaterial_Local(UserItem[] materials)
    {
        m_listMaterialItem.Clear();

        for(int i = 0; i < materials.Length; i++)
        {
            UserItem v = materials[i];
            AddMaterial(v);
        }
    }

    public void SyncToFirstPurchase(CustomNetwork.MarketOrder[] firstpurchase)
    {
        m_firstPurchase.Clear();

        if(firstpurchase != null)
        {
            for(int i = 0; i < firstpurchase.Length; i++)
            {
                CustomNetwork.MarketOrder v = firstpurchase[i];
#if NEW_NETWORK_MODULE
				m_firstPurchase[v.i] = v.f; //v.FirstPurchaseBonus;
#else
				m_firstPurchase[v.Id] = v.FirstPurchaseBonus;
#endif

			}
        }
    }

    public void SyncToEpisodeBestScore(CustomNetwork.EpisodeBestScoreInfo[] bestscore)
    {
        m_myEpisodeBestScore.Clear();

        if(bestscore != null)
        {
            for(int i = 0; i < bestscore.Length; i++)
            {
                CustomNetwork.EpisodeBestScoreInfo v = bestscore[i];
#if NEW_NETWORK_MODULE
				if (m_myEpisodeBestScore.ContainsKey(v.i) == false)
					m_myEpisodeBestScore.Add(v.i, new EpisodeBestScore(v));
				else
				{
					m_myEpisodeBestScore[v.i].BestScore = v.b.ToString();
				}
#else
				if (m_myEpisodeBestScore.ContainsKey(v.EpisodeId) == false)
                    m_myEpisodeBestScore.Add(v.EpisodeId, new EpisodeBestScore(v));
                else
                {
                    m_myEpisodeBestScore[v.EpisodeId].BestScore = v.BestScore;
                }
#endif
			}
        }
    }

    public void SyncToEpisodeBestScore (CustomNetwork.EpisodeBestScoreInfo bestscore)
    {
		//for (int i = 0; i < m_myEpisodeBestScore.Count; i++)
		{
#if NEW_NETWORK_MODULE
			if(m_myEpisodeBestScore.ContainsKey(bestscore.i))
            {
                EpisodeBestScore scoreInfo = new EpisodeBestScore(bestscore.b.ToString(), bestscore.m, bestscore.l);
                m_myEpisodeBestScore[bestscore.i] = scoreInfo;
            }
            else
            {
                m_myEpisodeBestScore.Add(bestscore.i, new EpisodeBestScore(bestscore.b.ToString(), bestscore.m, bestscore.l));
            }
#else
			if (m_myEpisodeBestScore.ContainsKey(bestscore.EpisodeId))
			{
				EpisodeBestScore scoreInfo = new EpisodeBestScore(bestscore.BestScore.ToString(), bestscore.LongestDistance, bestscore.LastDistance);
				m_myEpisodeBestScore[bestscore.EpisodeId] = scoreInfo;
			}
            else
            {
                m_myEpisodeBestScore.Add(bestscore.EpisodeId, new EpisodeBestScore(bestscore.BestScore.ToString(), bestscore.LongestDistance, bestscore.LastDistance));
            }
#endif
        }
    }

    public void SyncToEpisodeDistance(CustomNetwork.EpisodeBestScoreInfo[] bestscore)
    {
        m_myEpisodeDistance.Clear();

        if (bestscore != null)
        {
            for (int i = 0; i < bestscore.Length; i++)
            {
                CustomNetwork.EpisodeBestScoreInfo v = bestscore[i];
#if NEW_NETWORK_MODULE
				if (m_myEpisodeDistance.ContainsKey(v.i) == false)
					m_myEpisodeDistance.Add(v.i, new EpisodeBestScore(v));
				else
				{
					m_myEpisodeDistance[v.i].LongestDistance = v.m;
					m_myEpisodeDistance[v.i].LastDistance = v.l;
				}
#else
				if (m_myEpisodeDistance.ContainsKey(v.EpisodeId) == false)
                    m_myEpisodeDistance.Add(v.EpisodeId, new EpisodeBestScore(v));
                else
                {
                    m_myEpisodeDistance[v.EpisodeId].LongestDistance = v.LongestDistance;
                    m_myEpisodeDistance[v.EpisodeId].LastDistance = v.LastDistance;
                }
#endif
			}
        }
    }

    void Update()
    {
        if (PlayerDataMgr.instance.OnlineMode == false && PlayerData.instance.Stamina < ConfigTableMgr.instance.GetConfigData("RechargeableMaxStamina"))
        {
            PlayerData.instance.StaminaOldTime += Time.deltaTime;
        }
    }
    void OnApplicationQuit()
    {
        //if (PlayerDataMgr.instance.OnlineMode == false && PlayerData.instance.Stamina < ConfigTableMgr.instance.GetConfigData("RechargeableMaxStamina"))
            Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.StaminaOldTime, (long)PlayerData.instance.StaminaOldTime);
            Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_Stamina, PlayerData.instance.Stamina);

        OngoingGameDataMgr.instance.saveData();
    }

#endregion

    public void AddCharacter(UserCharacterItem character)
    {
        int index = m_listCharacterItem.FindIndex(characteritem => characteritem.ItemId == character.ItemId);

        // 있으면 갱신 없으면 추가
        if (index == -1)
        {
            m_listCharacterItem.Add(character);
        }
        else
        {
            m_listCharacterItem[index] = character;
        }
    }

    public void AddTreasure(UserTreasureItem treasure)
    {
        int index = m_listTreasureItem.FindIndex(item => item.UID == treasure.UID);

        // 있으면 갱신 없으면 추가
        if (index == -1)
        {
            m_listTreasureItem.Add(treasure);
        }
        else
        {
            m_listTreasureItem[index] = treasure;
        }
    }

    public void AddBuddy(UserBuddyItem buddy)
    {
        int index = m_listBuddyItem.FindIndex(item => item.UID == buddy.UID);

        if(index == -1)
        {
            m_listBuddyItem.Add(buddy);
        }
        else
        {
            m_listBuddyItem[index] = buddy;
        }
    }

    private void RemoveTreasure(UserTreasureItem treasure)
    {
        int index = m_listTreasureItem.FindIndex(item => item.UID == treasure.UID);

        if(index != -1)
        {
            m_listTreasureItem[index].Number -= treasure.Number;

            if(m_listTreasureItem[index].Number <= 0)
                m_listTreasureItem.RemoveAt(index);
        }
    }

    public void AddMaterial(UserItem material)
    {
        int index = m_listMaterialItem.FindIndex(item => item.ItemId == material.ItemId);

        if(index == -1)
        {
            m_listMaterialItem.Add(material);
        }
        else
        {
            m_listMaterialItem[index] = material;
        }
    }

    private void RemoveMaterial(UserItem material)
    {
        int index = m_listMaterialItem.FindIndex(item => item.ItemId == material.ItemId);

        if (index != -1)
        {
            m_listMaterialItem[index].Number -= material.Number;

            if (m_listMaterialItem[index].Number <= 0)
                m_listMaterialItem.RemoveAt(index);
        }
    }

    public void AddConsumable(UserItem consumable)
    {
        int index = m_listConsumableItem.FindIndex(item => item.UID == consumable.UID);
        
        if(index == -1)
        {
            m_listConsumableItem.Add(consumable);
        }
        else
        {
            m_listConsumableItem[index] = consumable;
        } 
    }

    private void RemoveConsumable(UserItem consumable)
    {
        int index = m_listConsumableItem.FindIndex(item => item.UID == consumable.UID);

        if(index != -1)
        {
            m_listConsumableItem[index].Number -= consumable.Number;

            if (m_listConsumableItem[index].Number <= 0)
                m_listConsumableItem.RemoveAt(index);
        }
    }

    // 보물용
    public void RemoveItem(UserTreasureItem item)
    {
        RemoveTreasure(item);
    }

    public void RemoveItem(UserItem item)
    {
        ItemInfo info = ItemTableMgr.instance.GetItemData(item.ItemId);

        switch(info.ItemType)
        {
            case ItemTableMgr.ItemInfo_ItemType.ConsumableItem:
            case ItemTableMgr.ItemInfo_ItemType.ExpPotion:
                RemoveConsumable(item);
                break;

            case ItemTableMgr.ItemInfo_ItemType.Material:
                RemoveMaterial(item);
                break;
        }
    }

    private void changeStatus(string changestatus)
    {
        ListenerManager.instance.SendDirectEvent("changeStatus", (object)changestatus);
    }

    public bool HandleEvent(string eventName, object data)
    {
        if (eventName == "changeStatus")
        {
            //CLog.Log(eventName);
        }
        return true;
    }

    public ItemTableMgr.ItemKeepState CheckKeepItem(int itemid, int number)
    {
        ItemInfo info = ItemTableMgr.instance.GetItemData(itemid);

        switch (info.ItemType)
        {
            case ItemTableMgr.ItemInfo_ItemType.Coin:
                {
                    if (m_coin < number)
                        return ItemTableMgr.ItemKeepState.NotEnoughCoin;
                }
                break;

            case ItemTableMgr.ItemInfo_ItemType.Cash:
                {
                    if (m_cash < number)
                        return ItemTableMgr.ItemKeepState.NotEnoughCash;
                }
                break;

            default:
                {
                    UserItem item = GetUserItem(itemid);
                    if (item == null)
                    {
                        return ItemTableMgr.ItemKeepState.NotEnoughItem;
                    }

                    if(info.ItemType != ItemTableMgr.ItemInfo_ItemType.Treasure)
                    {
                        if (item.Number < number)
                        {
                            return ItemTableMgr.ItemKeepState.NotEnoughItem;
                        }
                    }
                }
                break;
        }

        return ItemTableMgr.ItemKeepState.KeepItem;
    }

    public string ConvertEquipTreasureIdToString(List<UserTreasureItem> treasures)
    {
        StringBuilder treasure = new StringBuilder();

        for(int i = 0; i < treasures.Count; i++)
        {
            if (treasures[i] == null)
                continue;

            if(treasures[i].ItemId > 0 && treasures[i].UID > 0)
            {
                treasure.AppendFormat("{0}", treasures[i].ItemId);

                if(i != treasures.Count - 1)
                    treasure.Append(",");
            }
        }

        return treasure.ToString();
    }

    public string[] ConvertItemIdToStringList(List<UserItem> items)
    {
        const int COUNT = 2;
        // Index 0 - 아이템ID, 1 - 아이템 UID or Number
        StringBuilder[] item = new StringBuilder[COUNT];
        for (int i = 0; i < item.Length; i++)
            item[i] = new StringBuilder();

        for (int i = 0; i < items.Count; i++)
        {
            item[0].AppendFormat("{0}", items[i].ItemId);

            // 아이템 타입별로 UID, Number를 넣어주는게 다르다. 
            // need to do (아이템 타입이 전부 정해지면 수정)
            switch (ItemTableMgr.instance.GetItemData(items[i].ItemId).ItemType)
            {
                case ItemTableMgr.ItemInfo_ItemType.Treasure:
                    item[1].AppendFormat("{0}", items[i].UID);
                    break;

                default:
                    item[1].AppendFormat("{0}", items[i].Number);
                    break;
            }

            if (i != items.Count - 1)
            {
                foreach (StringBuilder v in item)
                {
                    v.Append(",");
                }
            }
        }

        string[] itemlist = { item[0].ToString(), item[1].ToString() };
        return itemlist;
    }
    Dictionary<int, GameObject> dicDisplayedCharacter = new Dictionary<int, GameObject>();

    public Dictionary<int, GameObject> GetdicDisplayedCharacter()
    {
        return dicDisplayedCharacter;
    }

    public void SetdicDisplayedCharacter(Dictionary<int, GameObject> dic)
    {
         dicDisplayedCharacter = dic;
    }

    public GameObject GetCharacterGameObject(int charid)
    {
        return GetdicDisplayedCharacter()[charid];

    }

    public void SetRelayCharacterList()
    {
        //if (m_RelayCharacterList.Count <= 0)
        //    m_RelayCharacterList.Add(GetUserCharacterList().Find(Item => Item.UID ==m_CurCharacterUID));
        if(m_RelayCharacterList.Count <= 0)
        {
            m_RelayCharacterList.Add(null);
            m_RelayCharacterList.Add(null);
        }
     

        for (int i = 0; i < m_CurCharacterItemIDs.Length; i++)
        {
            UserCharacterItem item = GetUserCharacterList().Find(Item => Item.ItemId == m_CurCharacterItemIDs[i]);

            if (m_RelayCharacterList.Count > i)
            {
                if (item != null)
                    m_RelayCharacterList[i] = (item);
                else
                    m_RelayCharacterList[i] = (null);

            }
        }

    }

    public string GetChooseCharacters()
    {
        StringBuilder str = new StringBuilder();
        str.AppendFormat("{0}", GetCurCharacterID());

        List<UserCharacterItem> list = m_RelayCharacterList.FindAll(item => item != null);
        if (list.Count > 0)
            str.Append(",");

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
            {
                str.AppendFormat("{0}", list[i].ItemId);

                if (i != list.Count - 1)
                    str.Append(",");
            }
        }

        return str.ToString();
    }

    public List<UserBuddyItem> GetUserBuddyList()
    {
        return m_listBuddyItem;
    }

    public UserBuddyItem GetBuddyData(int buddyId)
    {
        return m_listBuddyItem.Find(item => item.ItemId == buddyId);
    }

    public UserBuddyItem GetBuddyDataByUID(long buddyUID)
    {
        return m_listBuddyItem.Find(item => item.UID == buddyUID);
    }

    public UserBuddyItem GetEvolvingBuddy()
    {
        return m_listBuddyItem.Find(item => item.isEvolve);
    }

    public UserBuddyItem GetEquipBuddy(int itemid)
    {
        return m_listBuddyItem.Find(item => item.CharacterId == itemid && item.IsEquiped);
    }
#endregion

#region Property
    /// <summary>
    /// -1: block, 0: normal user, 3: Tangent
    /// </summary>
    public int UserCode
    {
        get
        {
            if (m_userCode == -1)
            {
                m_userCode = PlayerPrefs.GetInt("UserCode", 0);
            }
            return m_userCode;
        }
        set
        {
            m_userCode = value;
            PlayerPrefs.SetInt("UserCode", m_userCode);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// dev 유저 중 dev in game view 를 옵션에서 조정 할 수 있도록 하기 위해 만듦. (ListItemInGameDevView)
    /// </summary>
    public bool DevUserViewState
    {
        get
        {
            return isDevUserViewState;
        }
        set
        {
            isDevUserViewState = value; 
        }
    }

    public bool ShowScoreHUDText
    {
        get
        {
            return isShowScoreHUDText;
        }
        set
        {
            isShowScoreHUDText = value;
        }
    }

    public long Coin
    {
        get
        {
            return m_coin;
        }
        set
        {
            bool isChanged = !(m_coin == value);
            long useCoin = m_coin - value;
            m_coin = value;
            if (isChanged)
            {
                //need to do
                changeStatus("coin");

//                 if (useCoin < 0)
//                     UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.BringCount_SpecificItem, (long)Mathf.Abs((int)useCoin), 1);
            }
        }
    }

    public long Cash
    {
        get
        {
            return m_cash;
        }
        set
        {
            bool isChanged = !(m_cash == value);
            long useCash = m_coin - value;
            m_cash = value;
            if (isChanged)
            {
                //need to do
                changeStatus("cash");

                if (useCash < 0)
                    UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.BringCount_SpecificItem, (long)Mathf.Abs((int)useCash), 2);
            }
        }
    }

    public int Stamina
    {
        get
        {
            return m_stamina;
        }
        set
        {
            bool isChanged = !(m_stamina == value);
            bool isUse = (m_stamina - 1 == value);
            m_stamina = value;
            if(isChanged)
            {
                // PlayerData.instance.m_StaminaTime = System.DateTime.Now;


                //if (isUse)
                //    UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.UseCount_Stamina, 1, 3);


                if(StageMgr.instance.CurStageName != "StageBI" && !PlayerDataMgr.instance.OnlineMode)
                {
                    if (isUse)
                    {
                        Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_StaminaRefillTime, DateTime.Now.ToString());
                        PlayerData.instance.m_StaminaTime = DateTime.Now;
                        
                    }
                    else
                    {
                        Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.StaminaOldTime, 0);
                        PlayerData.instance.StaminaOldTime = 0;

                        Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_StaminaRefillTime, DateTime.Now.ToString());
                        PlayerData.instance.m_StaminaTime = DateTime.Now;

                    }

                    checkStaminaRefillTime();
                }

                //need to do
                changeStatus("Stamina");

                int RechargeableMaxStamina = ConfigTableMgr.instance.GetConfigData("RechargeableMaxStamina");

                if (Stamina >= RechargeableMaxStamina)
                {
                    if (NativeHelperContainer.pushHelper != null)
                        NativeHelperContainer.pushHelper.CancelScheduleNotification(PushHelper.PushId.UseStamina);
                }

            }
        }
    }


    public void checkStaminaRefillTime()
    {
        int RechargeableMaxStamina = ConfigTableMgr.instance.GetConfigData("RechargeableMaxStamina");

        if (PlayerData.instance.Stamina < RechargeableMaxStamina && PlayerData.instance.LocalPushStamina)
        {
            TimeSpan remaintime = PlayerData.instance.m_StaminaTime.AddSeconds((RechargeableMaxStamina - PlayerData.instance.Stamina) * (ConfigTableMgr.instance.GetConfigData("StaminaChargingTime") * 60) - PlayerData.instance.StaminaOldTime) - DateTime.Now;

            if (remaintime.TotalSeconds > 0)
            {
             
                int CheckTime = ((RechargeableMaxStamina - PlayerData.instance.Stamina - 1) * (ConfigTableMgr.instance.GetConfigData("StaminaChargingTime") * 60));

                if (remaintime.TotalSeconds < CheckTime)
                {
                    Stamina += 1;

                    Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.StaminaOldTime, 0);
                    PlayerData.instance.StaminaOldTime = 0;

                    Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_StaminaRefillTime, DateTime.Now.ToString());
                    PlayerData.instance.m_StaminaTime = DateTime.Now;

                    return;
                }


                PlayerData.instance.m_StaminaTimeSeconds = (int)remaintime.TotalSeconds - CheckTime;
                NativeHelperContainer.pushHelper.CreateScheduledNotifications((int)remaintime.TotalSeconds, GameDefine.AppName, LocalizationMgr.GetMessage(11900), PushHelper.PushId.UseStamina, null, GameDefine.UTNotificationProfile);

        
            }
            else
            {
                Stamina = RechargeableMaxStamina;            
            }

        }
      
    }


    public int Stamina2
    {
        get
        {
           return m_stamina2;
        }
        set
        {
            bool isChanged = !(m_stamina2 == value);
            bool isUse = (m_stamina2 - 1 == value);
            m_stamina2 = value;
            if(isChanged)
            {
                //need to do
                changeStatus("Stamina2");

                if(isUse)
                    UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.UseCount_Stamina, 1, 5);

                if (isUse && !PlayerDataMgr.instance.OnlineMode)
                {
                    Local_PlayerDataStream.SaveLocalData(Local_PlayerDataStream.LocalDataSteamType.Offline_Stamina2RefillTime, DateTime.Now.ToString());
                    PlayerData.instance.m_Stamina2Time = DateTime.Now;
                }
            }
        }
    }

    public int PlayerLevel
    {
        get
        {
            return m_playerLevel;
        }
        set
        {
            bool isChanged = (m_playerLevel != value && m_playerLevel != 0);
            int levelInterval = value - m_playerLevel;
            m_playerLevel = value;

            // 레벨업 팝업 띄우기
            if (isChanged)
            {
              //  PlayerData.instance.setActive_DisplayedCharacter_Handler(false);
              
                UserBehaviorMgr.instance.OnOutGameUserBehavior(QuestTableMgr.PostconditionType.ReachLevel_Team, levelInterval);

                changeStatus("changeStatus");
            }
        }
    }

    public int PlayerExp
    {
        get
        {
            return m_playerExp;
        }
        set
        {
            bool isChanged = (m_playerExp != value && value != 0);
            m_playerExp = value;
            if(isChanged)
            {
                //changeStatus(");
            }
        }
    }

    public bool IsGoldEpisode
    {
        get
        {
            return CurEpisodeID > 10000;
        }
    }

    public int CurEpisodeID
    {
        get
        {
            if (m_curEpisodeID == -1)
            {
                string episodeId = (PlayerDataMgr.instance.OnlineMode == true ? Local_PlayerDataStream.LocalDataSteamType.EpisodeID.ToString() : Local_PlayerDataStream.LocalDataSteamType.Offline_EpisodeID.ToString());
                m_curEpisodeID = UTIL.PreGetIntWithDecrypt(episodeId, 1);
                m_curEpisodeID = OpenEpisodeCheck(m_curEpisodeID) >= m_curEpisodeID ? m_curEpisodeID : OpenEpisodeCheck(m_curEpisodeID);
            }

            return m_curEpisodeID;
        }   
        set
        {
            bool isChanged = (m_curEpisodeID != value && m_curEpisodeID != 0);
            m_curEpisodeID = value;
            SetGameMode(m_curEpisodeID);

            if (isChanged)
            {
                m_CurRankingType = RankingTableMgr.RankingRewardType.Daily;
                string episodeId = (PlayerDataMgr.instance.OnlineMode == true ? Local_PlayerDataStream.LocalDataSteamType.EpisodeID.ToString() : Local_PlayerDataStream.LocalDataSteamType.Offline_EpisodeID.ToString());
                UTIL.PreSetStringWithEncrypt(episodeId, m_curEpisodeID.ToString());
            }
        } 
    }

    public int OpenEpisodeCheck(int Id = 0)
    {
        int NowUnLockEpisodeId = 1;

        if (Id != 0 && EpisodeTableMgr.instance.CheckGoldMap(Id))
        {
            EpisodeInfo GoldMapInfo = EpisodeTableMgr.instance.GetEpisodeList()[Id];
            for (int i = 0; i < GoldMapInfo.OpenCondition.Length; i++)
            {
                QuestInfo QuestInfo = QuestTableMgr.instance.GetQuestInfo(GoldMapInfo.OpenCondition[i]);
                UserQuest UserQuestInfo = GetQuestData(QuestInfo.Id);

                if(QuestInfo.Postcondition.Value > UserQuestInfo.Value ? false : true)
                {
                    NowUnLockEpisodeId = Id;
                }
            }

        }else
        {
            foreach (EpisodeInfo Episode in EpisodeTableMgr.instance.GetEpisodeList().Values)
            {
                int Count = QuestTableMgr.instance.GetQuestClearCountByEpisode(Episode.Id);
                bool EpisodeOpenQuestClearCheck = false;

                for (int i = 0; i < Episode.OpenCondition.Length; i++)
                {
                    QuestInfo QuestInfo = QuestTableMgr.instance.GetQuestInfo(Episode.OpenCondition[i]);
                    UserQuest UserQuestInfo = GetQuestData(QuestInfo.Id);
                    EpisodeOpenQuestClearCheck = QuestInfo.Postcondition.Value > UserQuestInfo.Value ? false : true;

                    if (!EpisodeOpenQuestClearCheck)
                        break;
                }

                {
                    NowUnLockEpisodeId = Episode.Id;

                    if (Episode.AchievementMaxCount > Count && !EpisodeOpenQuestClearCheck)
                    {
                        break;
                    }
                }

            }
        }


      

        return NowUnLockEpisodeId;
    }


    public Gamemode CurGamemode
    {
        get
        {
            if(m_curGamemode == Gamemode.Max)
            {
                string gamemode = (PlayerDataMgr.instance.OnlineMode == true ? Local_PlayerDataStream.LocalDataSteamType.Gamemode.ToString() : Local_PlayerDataStream.LocalDataSteamType.Offline_Gamemode.ToString());
                gamemode = UTIL.PreGetStringWithDecrypt(gamemode, Gamemode.AdventureRun.ToString());
                if(TangentFramework.Utils.IsEnumParseName(typeof(Gamemode), gamemode))
                {
                    m_curGamemode = (Gamemode)Enum.Parse(typeof(Gamemode), gamemode);
                }
                else
                {
                    SetGameMode(CurEpisodeID);
                }
            }

            return m_curGamemode;
        }
        set
        {
            bool isChanged = (m_curGamemode != value);
            m_curGamemode = value;

            if(isChanged)
            {
                string gamemode = (PlayerDataMgr.instance.OnlineMode == true ? Local_PlayerDataStream.LocalDataSteamType.Gamemode.ToString() : Local_PlayerDataStream.LocalDataSteamType.Offline_Gamemode.ToString());
                UTIL.PreSetStringWithEncrypt(gamemode, m_curGamemode.ToString());
            }
        }
    }

    public bool PushOn
    {
        get
        {
            if (!isLoadPush)
            {
                // need to do (최초 로그인 체크가 생기면 수정)
                if (!Convert.ToBoolean(PlayerPrefs.GetInt("FirstPush")))
                {
                    PlayerPrefs.SetInt("FirstPush", 1);
                    PlayerPrefs.Save();
                    PushOn = true;
                }

                isLoadPush = true;
                isPushOn = Convert.ToBoolean(PlayerPrefs.GetInt("Push"));
            }

            return isPushOn;
        }
        set
        {
            if (isPushOn != value)
            {
                isLoadPush = true;
                PlayerPrefs.SetInt("Push", Convert.ToInt32(value));
                PlayerPrefs.Save();
                isPushOn = value;
            }
        }
    }

    public bool LocalPushStamina
    {
        get
        {
            int ispush = UTIL.PreGetIntWithDecrypt("LocalPushStamina", -1);
            // 최초는 켜져있다.
            if (ispush == -1)
            {
                ispush = 1;
                UTIL.PreSetIntWithEncrypt("LocalPushStamina", ispush);        
            }

            isLocalPushStamina = Convert.ToBoolean(ispush);
            return isLocalPushStamina;
        }
        set
        {
            if(isLocalPushStamina != value)
            {
                UTIL.PreSetIntWithEncrypt("LocalPushStamina", Convert.ToInt32(value));
            }

            isLocalPushStamina = value;
        }
    }

    public bool LocalPushFreeDraw
    {
        get
        {
            int ispush = UTIL.PreGetIntWithDecrypt("LocalPushFreeDraw", -1);
            // 최초는 켜져있다.
            if (ispush == -1)
            {
                ispush = 1;
                UTIL.PreSetIntWithEncrypt("LocalPushFreeDraw", ispush);
            }

            isLocalPushFreeDraw = Convert.ToBoolean(ispush);
            return isLocalPushFreeDraw;
        }
        set
        {
            if (isLocalPushFreeDraw != value)
            {
                UTIL.PreSetIntWithEncrypt("LocalPushFreeDraw", Convert.ToInt32(value));
            }
            isLocalPushFreeDraw = value;
        }
    }

	public bool EventPush
	{
		get
		{
			int ispush = UTIL.PreGetIntWithDecrypt("EventPush", -1);
			// 최초는 켜져있다.
			if (ispush == -1)
			{
				ispush = 1;
				UTIL.PreSetIntWithEncrypt("EventPush", ispush);
			}

			isEventPush = Convert.ToBoolean(ispush);
			return isEventPush;
		}
		set
		{
			if (isEventPush != value)
			{
				UTIL.PreSetIntWithEncrypt("EventPush", Convert.ToInt32(value));
			}
			isEventPush = value;
		}
	}

	public bool LocalOptimization
    {
        get
        {
            int isOptimization = UTIL.PreGetIntWithDecrypt("LocalOptimization", -1);
            if(isOptimization == -1)
            {
                // 처음에 꺼져있다.
                isOptimization = 0;
                UTIL.PreSetIntWithEncrypt("LocalOptimization", isOptimization);
            }
            isLocalOptimization = Convert.ToBoolean(isOptimization);

            return isLocalOptimization;
        }
        set
        {
            if(isLocalOptimization != value)
                UTIL.PreSetIntWithEncrypt("LocalOptimization", System.Convert.ToInt32(value));
            isLocalOptimization = value;
        }
    }

    public UserCharacterItem GetCharacter(long uid)
    {
        UserCharacterItem character = m_listCharacterItem.Find(item => item.UID == uid);

        if(character == null)
        {
            CLog.Log(string.Format("PlayerData, GetCharacterID, Not Exist UserCharacterUID : {0}", uid));
        }

        return character;
    }

    public int GetCharacterID(long uid)
    {
        UserCharacterItem character = m_listCharacterItem.Find(item => item.UID == uid);

        if(character == null)
        {
            CLog.Log(string.Format("PlayerData, GetCharacterID, Not Exist UserCharacterUID : {0}", uid));
            return 0;
        }

        return character.ItemId;
    }

    public long GetCharacterUID(int itemid)
    {
        UserCharacterItem character = m_listCharacterItem.Find(item => item.ItemId == itemid);

        if (character == null)
        {
            CLog.Log(string.Format("PlayerData, GetCharacterID, Not Exist UserCharacterID : {0}", itemid));
            return 0;
        }

        return character.UID;
    }

    public int GetCurCharacterID()
    {
        return GetCharacterID(m_CurCharacterUID);
    }

    public int GetRelayCharacterID()
    {
        return m_RelayCharacterId;
    }

    public List<UserCharacterItem> GetUserCharacterList()
    {
        return m_listCharacterItem;
    }

    public UserCharacterItem GetUserCharacterInfo(int itemid)
    {
        int index = m_listCharacterItem.FindIndex(character => character.ItemId == itemid);

        if (index == -1)
        {
            //CLog.Log(string.Format("PlayerData, GetUserCharacterInfo, Not Exist UserCharacter ID : {0}", itemid));
            return null;
        }

        return m_listCharacterItem[index];
    }

    public List<UserTreasureItem> GetUserTreasureList()
    {
        return m_listTreasureItem;
    }

    public UserTreasureItem GetUserTreasureInfo(long itemUId)
    {
        return m_listTreasureItem.Find(treasure => treasure.UID == itemUId);
    }

    public UserTreasureItem GetUserTreasureInfo_Id(int itemid)
    {
        return m_listTreasureItem.Find(treasure => treasure.ItemId == itemid);
    }

    public List<UserItem> GetUserMaterialList()
    {                            
        return m_listMaterialItem;
    }

    public UserItem GetUserMaterialInfo(int itemId)
    {
        return m_listMaterialItem.Find(item => item.ItemId == itemId);
    }

    public List<UserItem> GetUserConsumableList()
    {
        return m_listConsumableItem;
    }

    public UserItem GetUserConsumableInfo(long itemuid)
    {
        return m_listConsumableItem.Find(item => item.UID == itemuid);
    }

    public UserItem GetUserConsumableInfo_Id(int itemid)
    {
        return m_listConsumableItem.Find(item => item.ItemId == itemid);
    }

    public UserItem GetUserItem(int itemId)
    {
        ItemInfo info = ItemTableMgr.instance.GetItemData(itemId);
        if (info == null)
            return null;

        switch(info.ItemType)
        {
            case ItemTableMgr.ItemInfo_ItemType.Character:
                return GetUserCharacterInfo(itemId);
            case ItemTableMgr.ItemInfo_ItemType.Treasure:
                return GetUserTreasureInfo(itemId);
            case ItemTableMgr.ItemInfo_ItemType.Material:
                return GetUserMaterialInfo(itemId);
            case ItemTableMgr.ItemInfo_ItemType.ConsumableItem:
                return GetUserConsumableInfo_Id(itemId);
        }

        //CLog.Log(string.Format("Not Exist UserItem ID : {0}", itemId));
        return null;
    }

    public UserItemSkill GetUserCharacterSkill(int itemid, int itemAbilityid)
    {
        List<UserItemSkill> listskill = GetUserCharacterSkillList(itemid);

        if (listskill == null)
        {
            return new UserItemSkill();
        }

        //int itemabilityId = ItemTableMgr.instance.GetItemAbilityId(itemid, abilityid);

        return listskill.Find(item => item.ItemAbilityId == itemAbilityid);
    }

    public List<UserItemSkill> GetUserCharacterSkillList(int itemid)
    {
        UserCharacterItem useritem = GetUserCharacterInfo(itemid);

        if (useritem == null)
        {
            //CLog.Log("Not Exist SkillList itemId : " + itemid, CLog.LogType.Error);
            return null;
        }

        return useritem.CharacterSkillList;
    }

    public UserTreasureItem GetEquipUserTreasureInfo(long uid)
    {
        foreach(UserTreasureItem v in m_listEquipTreasure)
        {
            if(uid == v.UID)
            {
                return v;
            }
        }

        return null;
    }

    public List<UserTreasureItem> GetEquipUserTreasureList()
    {   
        return m_listEquipTreasure;
    }

    // 캐릭터, 보물등 진화에 부족한 아이템 리스트
    public List<UserItem> GetNotEnoughEvolveItemList(UserItem evolveitem, bool bcoin)
    {
        List<UserItem> notenoughitemlist = new List<UserItem>();

        if(evolveitem != null && evolveitem.IsEvolve == 0)
        {
            ItemInfo iteminfo = ItemTableMgr.instance.GetItemData(evolveitem.ItemId);
            List<WhatNeedToEvolveInfo> evolveinfolist;

            if (bcoin)
            {
                evolveinfolist = iteminfo.ListWhatNeedToEvolve_Coin;
            }
            else
            {
                evolveinfolist = iteminfo.ListWhatNeedToEvolve_Cash;
            }

            for(int i = 0; i < evolveinfolist.Count; i++)
            {
                WhatNeedToEvolveInfo v = evolveinfolist[i];
                ItemInfo sourceitem = ItemTableMgr.instance.GetItemData(v.SourceItemId);
                switch (sourceitem.ItemType)
                {
                    case ItemTableMgr.ItemInfo_ItemType.Coin:
                        {
                            if(PlayerData.instance.Coin < sourceitem.CoinPricesApplyEventFactor)
                            {
                                notenoughitemlist.Add(new UserItem(v.SourceItemId, (int)(sourceitem.CoinPricesApplyEventFactor - PlayerData.instance.Coin)));
                            }
                        }
                        break;

                    case ItemTableMgr.ItemInfo_ItemType.Cash:
                        {
                            if (PlayerData.instance.Coin < sourceitem.CoinPricesApplyEventFactor)
                            {
                                notenoughitemlist.Add(new UserItem(v.SourceItemId, (int)(sourceitem.CoinPricesApplyEventFactor - PlayerData.instance.Coin)));
                            }
                        }
                        break;

                    case ItemTableMgr.ItemInfo_ItemType.Treasure:
                        {
                            UserTreasureItem treausre = GetUserTreasureInfo(v.SourceItemId);
                            if(treausre == null)
                            {
                                notenoughitemlist.Add(new UserItem(v.SourceItemId, 1));
                            }
                        }
                        break;

                    default:
                        {
                            UserItem item = GetUserItem(v.SourceItemId);
                            if(item == null)
                            {
                                notenoughitemlist.Add(new UserItem(v.SourceItemId, v.ItemValue));
                            }
                            else if(v.ItemValue > item.Number)
                            {
                                notenoughitemlist.Add(new UserItem(v.SourceItemId, v.ItemValue - item.Number));
                            }
                        }
                        break;
                }
            }
        }

        return notenoughitemlist;
    }

    public List<UserItem> GetNotEnoughBreakLimitItemList(UserCharacterItem character)
    {
        List<UserItem> notenoughitemlist = new List<UserItem>();

        ItemInfo iteminfo = ItemTableMgr.instance.GetItemData(character.ItemId);

        List<WhatNeedToBreakLimitInfo> list = iteminfo.ListWhatNeedToBreakLimit.FindAll(item => item.Step == character.NumberBreakLimit + 1);

        for(int i = 0; i < list.Count; i++)
        {
            WhatNeedToBreakLimitInfo v = list[i];
            ItemInfo sourceitem = ItemTableMgr.instance.GetItemData(v.SourceItemId);
            switch (sourceitem.ItemType)
            {
                case ItemTableMgr.ItemInfo_ItemType.Coin:
                    {
                        if (PlayerData.instance.Coin < v.ItemValue)
                        {
                            notenoughitemlist.Add(new UserItem(v.SourceItemId, (int)(v.ItemValue - PlayerData.instance.Coin)));
                        }
                    }
                    break;

                case ItemTableMgr.ItemInfo_ItemType.Cash:
                    {
                        if (PlayerData.instance.Coin < v.ItemValue)
                        {
                            notenoughitemlist.Add(new UserItem(v.SourceItemId, (int)(v.ItemValue - PlayerData.instance.Coin)));
                        }
                    }
                    break;

                default:
                    {
                        UserItem item = GetUserItem(v.SourceItemId);
                        if (item == null)
                        {
                            notenoughitemlist.Add(new UserItem(v.SourceItemId, v.ItemValue));
                        }
                        else if (v.ItemValue > item.Number)
                        {
                            notenoughitemlist.Add(new UserItem(v.SourceItemId, v.ItemValue - item.Number));
                        }
                    }
                    break;
            }
        }

        return notenoughitemlist;
    }

    public DateTime GetFreeGachaCoolTime(int gachaId)
    {
        if(!m_listGachaFreeTime.ContainsKey(gachaId))
        {
            //CLog.Log(string.Format("GetFreeGachaCoolTime, Not Exist GachaId : {0}", gachaId));
            return DateTime.MinValue;
        }

        return m_listGachaFreeTime[gachaId];
    }

    public Dictionary<int,DateTime> GetFreeGachaCoolTimeList()
    {
        return m_listGachaFreeTime;
    }

    public UserImageNotice GetImageNotice(long uid)
    {
        if (!m_listImageNotice.ContainsKey(uid))
        {
            //CLog.Log(string.Format("GetImageNotice, Not Exist uid : {0}", uid));
            return null;
        }

        return m_listImageNotice[uid];
    }

    public List<UserImageNotice> GetImageNoticeList()
    {
        List<UserImageNotice> notice = new List<UserImageNotice>();

        foreach (UserImageNotice info in m_listImageNotice.Values)
        {
            notice.Add(info);
        }

        return notice;
    }

    public Dictionary<int, DateTime> GetFreeGachaList()
    {
        return m_listGachaFreeTime;
    }

    public string GetMyEpisodeBestScore(int epsodeid)
    {
        if (m_myEpisodeBestScore.ContainsKey(epsodeid))
            return m_myEpisodeBestScore[epsodeid].BestScore;

        return "-";
    }

    public long GetMyEpisode_BestScore(int epsodeid)
    {
        if (m_myEpisodeBestScore.ContainsKey(epsodeid) && m_myEpisodeBestScore[epsodeid].BestScore != "-")
            return int.Parse(m_myEpisodeBestScore[epsodeid].BestScore);

        return 0;
    }

    public EpisodeBestScore GetMyEpisodeDistance(int epsodeid)
    {
        if (m_myEpisodeDistance.ContainsKey(epsodeid))
            return m_myEpisodeDistance[epsodeid];

        return null;
    }

    public void SetMyEpisodeDistance(int epsodeid, int LongestDistance , int LastDistance)
    {
        if (m_myEpisodeDistance.ContainsKey(epsodeid))
        {
            m_myEpisodeDistance[epsodeid].LastDistance = LastDistance;
            m_myEpisodeDistance[epsodeid].LongestDistance = LongestDistance;
        }
        else
        {
            m_myEpisodeDistance[epsodeid] = new EpisodeBestScore(PlayerData.instance.GetMyEpisodeBestScore(epsodeid), LongestDistance, LastDistance);
        }
    }

    public bool IsFirstPurchase()
    {
        bool IsFirst = true;

        foreach (int id in m_firstPurchase.Keys)
        {
            InAppPurchaseInfo info = InAppPurchaseTableMgr.instance.GetInAppPurchaseInfo(id);

            if((info != null && info.Type == InAppPurchaseTableMgr.InAppPurchase_Type.Cash)
                && (info.FirstPurchaseBonus == 0 || m_firstPurchase[id] == 0))
            {
                IsFirst = false;
                break;
            }
        }

        return IsFirst;
    }

    public void SetGameMode(int episodeId)
    {
        if(episodeId <= 10000)
        {
            CurGamemode = Gamemode.AdventureRun;
        }
        else
        {
            CurGamemode = Gamemode.TimeAttack;
        }
    }
    public bool forcePreventSetOnCharacter = false;
    public bool preventedSetOnCharacter = false;
    public int preventedSetOnCharadterId = 0;
    //public void setActive_DisplayedCharacter_Handler(bool IsOn = true, int charId = 0)
    //{
    //    foreach (GameObject obj in dicDisplayedCharacter.Values)
    //    {
    //        obj.SetActive(false);
    //    }

    //    if (charId == 0)
    //        charId = PlayerData.instance.GetCharacterID(PlayerData.instance.m_CurCharacterUID);

    //    if (forcePreventSetOnCharacter && IsOn)
    //    {
    //        preventedSetOnCharacter = true;
    //        preventedSetOnCharadterId = charId;
    //    }
    //    else
    //    {
    //        if (IsOn)
    //        {
    //            preventedSetOnCharacter = false;
    //        }
    //       // if (StageMgr.instance.DicStageObj.ContainsKey("StageLobby"))
    //        //    StageLobby.instance.LobbyAnimSet(charId, IsOn);
    //    }
    //}

    public bool IsEndGachaEffect = false;
    bool isAnimationing = false;

    public void ActiveAnimation(bool active)
    {
        isAnimationing = active;
    }

    public bool IsAnimationing()
    {
        return isAnimationing;
    }

    public void GachaEffectStart(GameObject BoxEffect, GameObject WhilteEffect, ItemInfo info, int count, GameObject bkg, List<GameObject> textureobj, bool IsShopResultOn = true)
    {
        IsEndGachaEffect = false;
        isAnimationing = true;

        GachaCoroutine = GachaEffect(BoxEffect, WhilteEffect, info, count, bkg, textureobj, IsShopResultOn);
        StartCoroutine(GachaCoroutine);
     
    }

    IEnumerator GachaEffect(GameObject BoxEffect, GameObject WhilteEffect, ItemInfo info, int count, GameObject bkg, List<GameObject> textureobj, bool IsShopResultOn)
    {

        if (bkg != null)
            bkg.SetActive(true);

        BoxEffect.SetActive(true);

		// 2016.12.26 마스터 버디 연출 관련 이전 버전으로 롤백함.
		/*
		if (info != null && info.ItemType == ItemTableMgr.ItemInfo_ItemType.Buddy)
			WhilteEffect.SetActive(true);
		*/
        
        yield return new WaitForSeconds(2f);

        SoundMgr.PlaySound2D(ESOUND_TYPE.UI, "GachaBoxOpen");
        WhilteEffect.SetActive(true);


        yield return new WaitForSeconds(1.7f);

        BoxEffect.SetActive(false);
        WhilteEffect.SetActive(false);
        for (int i = 0; i < textureobj.Count; i++)
        {
            textureobj[i].SetActive(false);
        }
        if (bkg != null)
            bkg.SetActive(false);

        if (IsShopResultOn)
            ShowGachaResult(info, count, "Gacha");
        else
            isAnimationing = false;
    }

    public void ShopGachaEffectStart(GameObject BoxEffect, GameObject WhilteEffect, ItemInfo itemInfo, UserBuddyItem buddyInfo, int count, GameObject bkg, List<GameObject> textureobj, bool IsShopResultOn, int ChangeID)
    {
        for (int i = 0; i < textureobj.Count; i++)
            textureobj[i].SetActive(false);
        StageMgr.instance.OpenPopup("StageGachaAndUnLockResult", UTIL.Hash("Type", itemInfo, "BuddyInfo", buddyInfo, "Count", count, "ResultType", "Gacha", "BuyCallback", null, "ChangeID", ChangeID, "GachaCharacter", true), (string str) =>
        {


        });
    }

    public void ShowGachaResult(ItemInfo info, int count, string resultType)
    {
        StageMgr.instance.OpenPopup("StageGachaAndUnLockResult", UTIL.Hash("Type", info, "Count", count, "ResultType", resultType, "BuyCallback",null), (string str) =>
        {


        });
    }


    IEnumerator GachaCoroutine;
    public void StopGachaEffect()
    {
        if (GachaCoroutine != null)
        {
            StopCoroutine(GachaCoroutine);
            GachaCoroutine = null;
            isAnimationing = false;
        }


    }

    public int CheckCharacterIcon(int itemid)
    {
        ItemInfo characterinfo = ItemTableMgr.instance.GetItemData(itemid);

        if (characterinfo == null)
        {
            List<ItemInfo> UseCharacterInfo = ItemTableMgr.instance.GetItemsByType(ItemTableMgr.ItemInfo_ItemType.Character);
            return UnityEngine.Random.Range(UseCharacterInfo[0].ItemId, UseCharacterInfo[UseCharacterInfo.Count - 1].ItemId);
        }
        else
            return itemid;
    }


    public bool IsBuddyMaster(long BuddyUID)
    {
        UserBuddyItem data = GetBuddyDataByUID(BuddyUID);
        if (data != null)
        {
            if (data.isEvolve)
                return false;

            if (data.EvolveLevel == 3)
            {
                ItemInfo skillinfo = ItemTableMgr.instance.GetItemData(data.Level0_AbilityID);
                ItemTableMgr.Ability_Effect effect = skillinfo.ListItemAbility[0].Ability.Effect;

                bool check = false;
                ItemInfo skillinfo1 = ItemTableMgr.instance.GetItemData(data.Level1_AbilityID);
                ItemTableMgr.Ability_Effect effect1 = skillinfo1.ListItemAbility[0].Ability.Effect;
                check = effect.Equals(effect1);

                if (check)
                {
                    ItemInfo skillinfo2 = ItemTableMgr.instance.GetItemData(data.Level2_AbilityID);
                    ItemTableMgr.Ability_Effect effect2 = skillinfo2.ListItemAbility[0].Ability.Effect;
                    check = effect.Equals(effect2);
                }

                if (check)
                {
                    ItemInfo skillinfo3 = ItemTableMgr.instance.GetItemData(data.Level3_AbilityID);
                    ItemTableMgr.Ability_Effect effect3 = skillinfo3.ListItemAbility[0].Ability.Effect;
                    check = checkBuddyMaster(effect, effect3);
                }

                return check;
            }
        }

        return false;
    }

    public bool CheckAbleBuddyEvolve(long buddyUID)
    {
        UserBuddyItem evoldata = GetEvolvingBuddy();

        if (evoldata != null && evoldata.UID != buddyUID)
            return false;
        
        return true;
    }

    bool checkBuddyMaster(ItemTableMgr.Ability_Effect src, ItemTableMgr.Ability_Effect dest)
    {
        switch (dest)
        {
            case ItemTableMgr.Ability_Effect.AddHp:
                return src.Equals(ItemTableMgr.Ability_Effect.AddMaxHp);

            case ItemTableMgr.Ability_Effect.AddSpeed:
                return src.Equals(ItemTableMgr.Ability_Effect.AddSpeed);

            case ItemTableMgr.Ability_Effect.ScoreMagnet:
                return src.Equals(ItemTableMgr.Ability_Effect.AddScoreYieldDuringIngame);
        }

        return false;
    }
#endregion
}

