// LobbyInfo.cs - QuestInfo implementation file
//
// Description      : LobbyInfo
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/04/25
// Last Update      : 2020/03/06
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using System;
using System.Collections.Generic;
using UnityEngine;
using Dorothy.Network;
using Dorothy.UI.Popup;

namespace Dorothy.DataPool
{
    public enum LayoutType
    {
        None,
        Slot,
        Banner,
    }

    public enum JackPotUIType
    {
        None,
        Grand,
        Grand_Main,
        Grand_Sub,
        Single,
    }

    public class Layout
    {
        public long id;
        public List<LayoutItem> item;
    }

    public class LayoutItem
    {
        public LayoutType type;
        //public int jackpot;                   // 로비 DP 잭팟 1단인지 2단인지 - 2단이 양옆으로 묶여있으면 링크 활성화
        public LayoutSlotData slot;
        public LayoutBannerData banner;
    }

    public class UserTimeBonusInfo
    {
        public string bonusType;                    // 보너스 타입 COIN : 코인, MEGA : 메가
        public string bonusMode;                    // 보너스 모드 NORMAL : 기본, TURBO : 터보
        public int bonusMaxNum;                     // 보너스 최대 횟수 (제한 횟수)
        public int bonusCount;                      // 보너스 받은 횟수
        public long standbySeconds;                 // 다음 보너스까지 대기 시간 (초)
        public long standbySeconds4MegaWheel;       // 메가휠 까지 남은 쿨타임 (메가휠로 오면 무조건 메가상태로 바꿔야댐)
        public bool bonusAvailable;                 // 보너스 이용 가능 여부
        //public List<long> wheelBonusCoins;        <-- 민주님 고민 중
        public Dictionary<int, UserGoodsInfo> wheelBonusGoodsById;  // bonusType이 MEGA인 경우 key: 휠 보너스 ID, value: 휠 보너스 상품
        public int winWheelBonusId;                 // 당첨 휠 보너스 ID
        public long totalFriendBonusCoin;           // 최종 친구 보너스 코인 수
        public long totalBonusCoin;                 // 최종 보너스 코인 수, 보너스 수령시에만 내려주는 값
        public UserInfo.EffectInfo effect;          // 사용자 효력 정보
    }

    public class InboxGiftSummaryData
    {
        public int totalGiftCount;
        public int coinGiftCount;
        public int mysteryGiftCount;
    }

    public class RollingBannerData
    {
        public long bannerId;
        public string bannerImg;
        public string detailImg;
        public string link;
        public BannerSceneInfo sceneInfo;
    }

    public class BannerSceneInfo
    {
        public BannerSceneData storeLevelBonus;
    }

    public class BannerSceneData
    {
        public long level;
        public string coinRate;
        public string upCoinRate;
    }

    public class WelcomeBonusData
    {
        public long bonusId;
        public long totalBonusCoin;
    }

    public class FacebookConnectBonusData
    {
        public long bonusId;
        public long totalBonusCoin;
    }

    public class UserMissionSummaryData
    {
        public int leftMissionCount;
        public long dailyEndDate;
    }

    public class BlastEventSummaryData
    {
        public int pickCount;
    }

    public class WorldMessageConfig
    {
        public int displayCount;
        public long duration;
    }

    public class QuestSummary
    {
        public bool questEnabled;
        public long questEndDate;
    }
    
    public sealed class LobbyInfo : BasePool<LobbyInfo>
    {
        public Action ActionAdd = null;
        public Action ActionUpdateLayout = null;
        public Action ActionRemove = null;
        public Action ActionClear = null;
        public Action ActionRollingBanner = null;
        public Action ActionTimeBonus = null;
        public Action ActionWelcomBonus = null;
        public Action ActionFBConnectBonus = null;
        public Action ActionMissionSummary = null;
        public Action ActionEventSummary = null;
        public Action ActionLobbyStoreBonus = null;

        public int focusIndex = 0;

        private int lastClickMSN = -1;

        private List<LayoutSlotData> slotDatas = new List<LayoutSlotData>();
        private List<LayoutBannerData> LayoutBannerDatas = new List<LayoutBannerData>();
        private List<RollingBannerData> rollingBannerDatas = new List<RollingBannerData>();
        private List<Layout> lobbyLayout = new List<Layout>();
        private UserTimeBonusInfo timeBonusData;
        private InboxGiftSummaryData inboxGiftSummary;
        private WelcomeBonusData welcomeBonusData = new WelcomeBonusData();
        private FacebookConnectBonusData fbConnectBonusData = new FacebookConnectBonusData();
        private UserMissionSummaryData missionSummaryData = new UserMissionSummaryData();
        private BlastEventSummaryData blastEventSummaryData = new BlastEventSummaryData();
        private WorldMessageConfig worldMessageConfig = new WorldMessageConfig();
        private StoreBonus lobbyStoreBonus = new StoreBonus();
        private QuestSummary questSummaryData = new QuestSummary();

        private List<long> openSlotIdList = new List<long>();

        public List<Layout> LobbyLayout
        {
            get { return lobbyLayout; }
            set { lobbyLayout = value; }
        }

        public List<RollingBannerData> RollingBannerData
        {
            get { return rollingBannerDatas; }
            set { rollingBannerDatas = value; }
        }

        public UserTimeBonusInfo TimeBonusData
        {
            get { return timeBonusData; }
            set
            {
                timeBonusData = value;

                if (ActionTimeBonus != null)
                    ActionTimeBonus();
            }
        }

        public InboxGiftSummaryData InboxGiftSummary
        {
            get { return inboxGiftSummary; }
            set { inboxGiftSummary = value; }
        }

        public WelcomeBonusData WelcomeBonusData
        {
            get { return welcomeBonusData; }
            set { welcomeBonusData = value; }
        }

        public FacebookConnectBonusData FBConnectBonusData
        {
            get { return fbConnectBonusData; }
            set { fbConnectBonusData = value; }
        }

        public UserMissionSummaryData MissionSummaryData
        {
            get { return missionSummaryData; }
            set
            {
                missionSummaryData = value;
                if (ActionMissionSummary != null)
                    ActionMissionSummary();
            }
        }

        public BlastEventSummaryData BlastSummaryData
        {
            get { return blastEventSummaryData; }
            set
            {
                blastEventSummaryData = value;
                if (ActionEventSummary != null)
                    ActionEventSummary();
            }
        }

        public WorldMessageConfig WorldMessageConfigData
        {
            get { return worldMessageConfig; }
            set { worldMessageConfig = value; }
        }

        public int LastClickMSN
        {
            get { return lastClickMSN; }
            set { lastClickMSN = value; }
        }

        public StoreBonus LobbyStoreBonus
        {
            get { return lobbyStoreBonus; }
            set { lobbyStoreBonus = value; }
        }

        public QuestSummary QuestSummaryData
        {
            get { return questSummaryData; }
            set { questSummaryData = value; }
        }

        public void AddListenerTimeBonus(Action callback)
        {
            ActionTimeBonus = callback;
        }

        public void Add(long layoutId, List<LayoutItem> itemList)
        {
            lobbyLayout.Add(new Layout() { id = layoutId, item = itemList });

            if (ActionAdd != null)
                ActionAdd();
        }

        public void UpdateLayout(List<List<LobbyLayout>> layoutList, List<LayoutSlotData> slotList, List<LayoutBannerData> bannerList)
        {
            lobbyLayout.Clear();
            slotDatas.Clear();
            LayoutBannerDatas.Clear();
            openSlotIdList.Clear();

            if (slotList != null)
                slotDatas.AddRange(slotList);

            if (bannerList != null)
                LayoutBannerDatas.AddRange(bannerList);

            var list = new List<Layout>();
            for (int i = 0; i < layoutList.Count; ++i)
            {
                var datas = layoutList[i];
                var itemList = new List<LayoutItem>();
                //lobbyLayout.Add(datas);
                for (int j = 0; j < datas.Count; ++j)
                {
                    var layout = datas[j];
                    var item = new LayoutItem();
                    item.type = Type(layout);
                    
                    if (item.type == LayoutType.Slot)
                    {
                        item.slot = FindSlotData(layout);
                        //item.jackpot = item.slot.jackpot;
                    }
                    else if (item.type == LayoutType.Banner)
                    {
                        item.banner = FindBannerData(layout);
                        //item.jackpot = 0;
                    }

                    if (item.slot == null && item.banner == null)
                        itemList = null;
                    else
                        itemList.Add(item);
                }

                if (itemList != null)
                    Add(i, itemList);
            }

            for (int j = 0; j < lobbyLayout.Count; ++j)
            {
                for (int x = 0; x < lobbyLayout[j].item.Count; ++x)
                {
                    if (lobbyLayout[j].item[x].slot != null)
                        SaveOpenSlotIdList((int)lobbyLayout[j].item[x].slot.id);
                }
            }

            if (ActionUpdateLayout != null)
                ActionUpdateLayout();
        }

        public void SaveOpenSlotIdList(int id)
        {
            for (int i = 0; i < openSlotIdList.Count; ++i)
            {
                if (openSlotIdList[i] == id)
                    return;
            }

            if (id >= 999)
                return;

            var slot = FindSlotData(id);
            if (slot == null)
                return;

            if (UserInfo.Instance.Level.level < (ulong)slot.level)
                return;

            openSlotIdList.Add(id);
        }

        public string GetOpenSlotIdList()
        {
            string list = "";
            openSlotIdList.Sort();

            for (int i = 0; i < openSlotIdList.Count; ++i)
            {
                if (i > 0)
                    list += "/";

                list += openSlotIdList[i].ToString();
            }

            return list;
        }

        public void UpdateRollingBanner(List<RollingBannerData> bannerList)
        {
            rollingBannerDatas.Clear();

            if (bannerList != null)
                rollingBannerDatas.AddRange(bannerList);

            if (ActionRollingBanner != null)
                ActionRollingBanner();
        }

        public void UpdateWelcomeBonus(WelcomeBonusData data)
        {
            welcomeBonusData = data;

            if (ActionWelcomBonus != null)
                ActionWelcomBonus();
        }

        public void UpdateFBConnectBonus(FacebookConnectBonusData data)
        {
            fbConnectBonusData = data;

            if (ActionFBConnectBonus != null)
                ActionFBConnectBonus();
        }

        public void UpdateStoreBonus(StoreBonus data)
        {
            lobbyStoreBonus = data;

            if (ActionLobbyStoreBonus != null)
                ActionLobbyStoreBonus();
        }

        public int GetLayoutIndex(int slotId)
        {
            for (int i = 0; i < lobbyLayout.Count; ++i)
            {
                for (int j = 0; j < lobbyLayout[i].item.Count; ++i)
                {
                    if (lobbyLayout[i].item[j].type == LayoutType.Banner)
                    {
                        continue;
                    }
                    else
                    {
                        if (lobbyLayout[i].item[j].slot.id == slotId)
                            return i;
                    }
                }
            }

            return -1;
        }

        public LayoutSlotData FindSlotData(int slotId)
        {
            for (int i = 0; i < lobbyLayout.Count; ++i)
            {
                for (int j = 0; j < lobbyLayout[i].item.Count; ++i)
                {
                    if (lobbyLayout[i].item[j].type == LayoutType.Banner)
                    {
                        continue;
                    }
                    else
                    {
                        if (lobbyLayout[i].item[j].slot.id == slotId)
                            return lobbyLayout[i].item[j].slot;
                    }
                }
            }

            return null;
        }

        private LayoutSlotData FindSlotData(LobbyLayout data)
        {
            for (int i = 0; i < slotDatas.Count; ++i)
            {
                if (slotDatas[i].id == data.id)
                    return slotDatas[i];
            }

            return null;
        }

        private LayoutBannerData FindBannerData(LobbyLayout data)
        {
            for (int i = 0; i < LayoutBannerDatas.Count; ++i)
            {
                if (LayoutBannerDatas[i].bannerId == data.id)
                    return LayoutBannerDatas[i];
            }

            return null;
        }

        private LayoutType Type(LobbyLayout item)
        {
            string type = item.type.ToLower();
            if (type.CompareTo("slot") == 0)
                return LayoutType.Slot;
            else if (type.CompareTo("banner") == 0)
                return LayoutType.Banner;

            return LayoutType.None;
        }

        public void OpenBannerLink(string link, bool isLayout = false)
        {
            if (string.IsNullOrEmpty(link))
            {
                Debug.LogWarning("Null Banner Link");
            }
            else
            {
                Uri uri = new Uri(link);
                string scheme = uri.Scheme;

                switch (scheme)
                {
                    case "facebook":
                        PopupLoading.Open();
                        Session.Instance.LinkFacebook((exception) =>
                        {
                            UserTracking.Instance.LogOutGameImp(UserTracking.EventWhich.freecoin, UserTracking.EventWhat.fb_connect, "M", UserTracking.Location.pop_layout);
                            PopupLoading.CloseSelf();
                            PopupSystem.Instance.UnregisterAll();
                        });
                        break;

                    case "piggybank":
                        PopupPiggyBank.Open(UserTracking.Location.pop_rolling);
                        break;

                    case "store":
                        if (isLayout)
                            PopupStore.Open(UserTracking.Location.pop_layout);
                        else
                            PopupStore.Open();
                        break;
                    case "blast":
                        PopupBlast.Open(null, null, UserTracking.Location.pop_rolling);
                        break;
                    case "event":
                        break;
                    case "rewardad":
                        if (RewardAdInfo.Instance.RewardAdStatus != null)
                        {
                            if (RewardAdInfo.Instance.RewardAdStatus.rewardedCnt >= RewardAdInfo.Instance.RewardAdStatus.rewardLimitCnt)
                            {
                                // 오늘 시청 가능 횟수 초과
                                Log("Exceeded number of views");
                                PopupRewardAdLimit.Open();
                            }
                            else
                            {
                                PopupRewardAd.Open(RewardAdSystem.ShowType.OutGame);
                            }
                        }
                        break;
                    default:
                        Application.OpenURL(link);
                        break;
                }
            }
        }

        public JackPotUIType CheckLinkJackpot(int layoutIndex, int itemIndex, int jackpotType)
        {
            if (jackpotType <= 0)
                return JackPotUIType.None;

            //TODO:: 오른쪽을 기준으로 왼쪽이랑 비교한다. (하이어라키 뎁스로 인해서)
            if (layoutIndex == 0)
            {
                //TODO:: right (첫번째가 오른쪽이랑 비교했을때)
                if (jackpotType == 1)
                    return JackPotUIType.Single;  // 2단 이상일경우 single grand, 1단이면 single
                
                for (int j = 0; j < lobbyLayout[layoutIndex + 1].item.Count; ++j)
                {
                    if (j == itemIndex)
                    {
                        if (lobbyLayout[layoutIndex + 1].item[j].slot != null)
                        {
                            if (lobbyLayout[layoutIndex + 1].item[j].slot.progressiveJackpotInfoList == null)
                                return JackPotUIType.Grand;

                            if (lobbyLayout[layoutIndex + 1].item[j].slot.jackpot >= 2)  // 오른쪽에 있는 애가 2단 이상 일경우
                                return JackPotUIType.Grand_Sub;
                        }
                    }
                }

                return JackPotUIType.Grand;
            }

            if (layoutIndex >= lobbyLayout.Count - 1)
            {
                //TODO:: left check (마지막이 왼쪽에 있는 애랑)
                if (jackpotType == 1)
                    return JackPotUIType.Single;

                for (int i = 0; i < lobbyLayout[layoutIndex - 1].item.Count; ++i)
                {
                    if (i == itemIndex)
                    {
                        if (lobbyLayout[layoutIndex - 1].item[i].slot != null)
                        {
                            if (lobbyLayout[layoutIndex - 1].item[i].slot.progressiveJackpotInfoList == null)
                                return JackPotUIType.Grand;

                            if (lobbyLayout[layoutIndex - 1].item[i].slot.jackpot >= 2)
                                return JackPotUIType.Grand_Main;
                        }
                    }
                }

                return JackPotUIType.Grand;
            }

            if (layoutIndex > 0 && layoutIndex < lobbyLayout.Count - 1)
            {
                //TODO:: left check
                if (jackpotType == 1)
                    return JackPotUIType.Single;

                var type = JackPotUIType.Grand;
                for (int i = 0; i < lobbyLayout[layoutIndex - 1].item.Count; ++i)
                {
                    if (i == itemIndex)
                    {
                        if (lobbyLayout[layoutIndex - 1].item[i].slot != null)
                        {
                            if (lobbyLayout[layoutIndex - 1].item[i].slot.jackpot >= 2)
                            {
                                if (lobbyLayout[layoutIndex - 1].item[i].slot.progressiveJackpotInfoList != null)
                                    type = JackPotUIType.Grand_Main;
                            }
                                
                        }
                    }
                }

                //TODO:: right
                for (int j = 0; j < lobbyLayout[layoutIndex + 1].item.Count; ++j)
                {
                    if (j == itemIndex)
                    {
                        if (lobbyLayout[layoutIndex + 1].item[j].slot != null)
                        {
                            if (lobbyLayout[layoutIndex + 1].item[j].slot.jackpot >= 2)
                            {
                                if (lobbyLayout[layoutIndex + 1].item[j].slot.progressiveJackpotInfoList != null)
                                    type = JackPotUIType.Grand_Sub;
                            }
                        }
                    }
                }

                return type;
            }

            return JackPotUIType.None;
        }

        public void Remove(Layout data)
        {
            lobbyLayout.Remove(data);

            if (ActionRemove != null)
                ActionRemove();
        }

        public void Clear()
        {
            lobbyLayout.Clear();

            if (ActionClear != null)
                ActionClear();
        }
    }
}