// AnswerLobby.cs - AnswerLobby implementation file
//
// Description      : AnswerLobby
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/04/25
// Last Update      : 2019/04/25
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using System;
using System.Collections.Generic;
using Dorothy.DataPool;

namespace Dorothy.Network
{
    public class AnswerLobby : Answer
    {
        public UserProfile user;
        public List<List<LobbyLayout>> lobbyLayout;
        public List<LayoutSlotData> slot;
        public List<LayoutBannerData> layoutBanners;
        public UserTimeBonusInfo timeBonus;
        public InboxGiftSummaryData inboxGiftSummary;
        public List<BigBannerData> bigBanners;
        public List<RollingBannerData> rollingBanners;
        public WelcomeBonusData welcomeBonus;
        public FacebookConnectBonusData fbConnectBonus;
        public List<HotDealOfferData> hotDealOffers;
        public List<HotDealBonusData> hotDealBonuses;
        public UserMissionSummaryData userMissionSummary;
        public AttendBonusData attendanceBonus;
        public BlastEventSummaryData blastEventSummary;
        public WorldMessageConfig jackpotWorldMessageConfig;
        public StoreBonus storeBonus;
        public QuestSummary questSummary;
    }
}