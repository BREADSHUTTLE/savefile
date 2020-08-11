// MissionInfo.cs - MissionInfo implementation file
//
// Description      : MissionInfo
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/07/11
// Last Update      : 2019/07/11
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dorothy.DataPool
{
    public enum MissionRewardType
    {
        None,
        Coin,
        CoinAndBooster,
    }

    public enum MissionType
    {
        None,
        Daily,
        Weekly,
    }

    public class MissionData
    {
        public MissionSet missionSet;
        public CurrentMission currentMission;
        public List<MissionStatus> missionStatus;
    }

    public class MissionSet
    {
        public long id;
        public long goodsId1;
        public long goodsId2;
        public long totalPoint;
        public long userPoint;
        public bool receivedSetReward1;
        public bool receivedSetReward2;
        public long reward1Point;
        public long reward2Point;
        public long endDate;
        public long dailyEndDate;
    }

    public class CurrentMission
    {
        public long id;
        public string title;
        public string type1;
        public long type1ValN;
        public long type1ValM;
        public string type2;
        public long type2Val;
        public List<long> goodsIds;
        public double leftSecond;
        public long point;
    }

    public class MissionStatus
    {
        public long missionId;
        public long val;
        public long idx;
        public bool receivedReward;
    }

    public sealed class MissionInfo : BasePool<MissionInfo>
    {
        public enum EffectType
        {
            None,
            Boom,
            Faster,
            BoomFaster,
            Turbo,
        }

        public Action ActionAdd = null;
        public Action ActionUpdate = null;
        //public Action ActionUpdateNextMission = null;
        public Action ActionUpdateDailyGoodsList = null;
        public Action ActionUpdateWeeklyGoodsList = null;
        public Action ActionClear = null;

        private bool activeLobbyIcon = true;
        public Action ActionLobbyIcon = null;

        private MissionData missionData = null;
        private List<UserGoodsInfo> dailyGoodsList = new List<UserGoodsInfo>();
        private List<UserGoodsInfo> weeklyGoodsList = new List<UserGoodsInfo>();
        private long rewardPoint = 0;

        public MissionData MissionData
        {
            get { return missionData; }
            set { missionData = value; }
        }

        public List<UserGoodsInfo> DailyGoodsList
        {
            get { return dailyGoodsList; }
            set { dailyGoodsList = value; }
        }

        public List<UserGoodsInfo> WeeklyGoodsList
        {
            get { return weeklyGoodsList; }
            set { weeklyGoodsList = value; }
        }

        public long RewardPoint
        {
            get { return rewardPoint; }
            set { rewardPoint = value; }
        }

        public bool ActiveLobbyIcon
        {
            get { return activeLobbyIcon; }
            set
            {
                activeLobbyIcon = value;
                if (ActionLobbyIcon != null)
                    ActionLobbyIcon();
            }
        }


        public void Update(MissionData data)
        {
            MissionData = data;

            if (data == null)
                return;

            if (data.currentMission != null)
                rewardPoint = data.currentMission.point;

            if (ActionUpdate != null)
                ActionUpdate();
        }
        
        //public void UpdateNextMission(CurrentMission data)
        //{
        //    if (data == null)
        //    {
        //        if (ActionUpdateNextMission != null)
        //            ActionUpdateNextMission();

        //        return;
        //    }

        //    missionData.currentMission = data;

        //    //missionData.missionStatus[missionData.missionStatus.Count - 1].receivedReward = true;
        //    //if (missionData.missionStatus.Count < 3)
        //    //{
        //    //    missionData.missionStatus.Add(new MissionStatus()
        //    //    {
        //    //        missionId = data.id,
        //    //        val = 0,    // 보상 후 다음 정보는 무조건 0 으로 표시하려고
        //    //        idx = 10,   // 무조건 인덱스가 마지막에 있기 위해서 임의의 값 설정
        //    //        receivedReward = false,
        //    //    });
        //    //}

        //    if (ActionUpdateNextMission != null)
        //        ActionUpdateNextMission();
        //}

        public void UpdateDailyGoodsList(List<UserGoodsInfo> list)
        {
            dailyGoodsList.Clear();
            dailyGoodsList.AddRange(list);

            for (int i = 0; i < list.Count; ++i)
                GoodsInfo.Instance.AddGoods(list[i]);

            if (ActionUpdateDailyGoodsList != null)
                ActionUpdateDailyGoodsList();
        }

        //public void UpdateRewardMissionPoint()
        //{
        //    rewardPoint = missionData.currentMission.point;
        //}

        public void UpdateWeeklyGoodsList(List<UserGoodsInfo> list)
        {
            weeklyGoodsList.Clear();
            weeklyGoodsList.AddRange(list);

            for (int i = 0; i < list.Count; ++i)
                GoodsInfo.Instance.AddGoods(list[i]);

            if (ActionUpdateWeeklyGoodsList != null)
                ActionUpdateWeeklyGoodsList();
        }

        //public void UpdateRewardWeeklySet()
        //{
        //    if (weeklyGoodsList.Count <= 0)
        //        return;

        //    for (int i = 0; i < weeklyGoodsList.Count; ++i)
        //    {
        //        if (missionData.missionSet.goodsId1 == weeklyGoodsList[i].goodsId)
        //            missionData.missionSet.receivedSetReward1 = true;

        //        if (missionData.missionSet.goodsId2 == weeklyGoodsList[i].goodsId)
        //            missionData.missionSet.receivedSetReward2 = true;
        //    }
        //}

        public void Clear()
        {
            missionData = null;

            if (ActionClear != null)
                ActionClear();
        }

        public bool CheckCurrentMission(long Id)
        {
            return missionData.currentMission.id == Id;
        }

        public long GetTotalDailyRewardCoin()
        {
            long coin = 0;
            for (int i = 0; i < dailyGoodsList.Count; ++i)
                coin += dailyGoodsList[i].totalCoin;

            return coin;
        }

        public long GetTotalWeeklyRewardCoin()
        {
            long coin = 0;
            for (int i = 0; i < weeklyGoodsList.Count; ++i)
                coin += weeklyGoodsList[i].totalCoin;

            return coin;
        }

        public Dictionary<string, long> GetDailyRewardEffect()
        {
            Dictionary<string, long> dic = new Dictionary<string, long>();

            for (int i= 0; i < dailyGoodsList.Count; ++i)
            {
                if (string.IsNullOrEmpty(dailyGoodsList[i].effectCode) == false)
                    dic.Add(dailyGoodsList[i].effectCode, dailyGoodsList[i].effectPeriod);
            }

            return dic;
        }

        public Dictionary<string, long> GetWeeklyRewardEffect()
        {
            Dictionary<string, long> dic = new Dictionary<string, long>();

            for (int i = 0; i < weeklyGoodsList.Count; ++i)
            {
                if (string.IsNullOrEmpty(weeklyGoodsList[i].effectCode) == false)
                    dic.Add(weeklyGoodsList[i].effectCode, weeklyGoodsList[i].effectPeriod);
            }

            return dic;
        }

        public void RemoveDailyRewardEffect(string code)
        {
            if (dailyGoodsList.Count <= 0)
                return;

            for (int i = 0; i < dailyGoodsList.Count; ++i)
            {
                if (dailyGoodsList[i].effectCode == code)
                    dailyGoodsList.Remove(dailyGoodsList[i]);
            }
        }

        public void RemoveWeeklyRewardEffect(string code)
        {
            if (weeklyGoodsList.Count <= 0)
                return;

            for (int i = 0; i < weeklyGoodsList.Count; ++i)
            {
                if (weeklyGoodsList[i].effectCode == code)
                    weeklyGoodsList.Remove(weeklyGoodsList[i]);
            }
        }

        public List<MissionStatus> Sort(List<MissionStatus> list)
        {
            return list.OrderBy(x => x.idx).ToList();
        }
    }
}