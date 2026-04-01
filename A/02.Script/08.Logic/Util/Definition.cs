using System;

namespace CAPYBARA
{
    public enum LoginType
    {
        GOOGLE,
        APPLE,
        NAVER,
        KAKAO,
        ATOZ,
        None
    }

    public enum NotiType
    {
        Announcement = 0,
        DM_FriendRequest,
    }

    public enum HistoryType
    {
        Total = 0,
        Today,
    }

    public enum GameMode
    {
        Default,
        TwoVS,
        END
    }

    public enum GameType
    {
        ALL = 0,
        LOW_BADUGI,
        HOLDEM,
        SEVEN_POKER,
        END
    }

    public enum IAPType
    {
        Booster = 0,
        Class_s,
        Class_a,
        Class_b,

        Class_sub_s,
        Class_sub_a,
        Class_sub_b,

        Gold,
        Luchy_Pocket,
        Msg_100,
        Msg_50,
        Msg_20,
        Nick_Change,
    }

    public enum ItemID
    {
        AVATAR_1,
        AVATAR_2,
        AVATAR_3,
        AVATAR_4,

        AVATAR_51,
        AVATAR_51_00,
        AVATAR_51_01,
        AVATAR_51_02,
        AVATAR_51_03,
        AVATAR_51_04,
        AVATAR_51_05,
        AVATAR_52,
        AVATAR_53,

        BOOSTER,

        CLASS_A,
        CLASS_B,
        CLASS_S,

        CLASS_S_PRE_RESERVATION,

        DEFAULT_CURRENCY,
        DEFAULT_CURRENCY_SALE,

        EMOTICON_1,
        EMOTICON_INVITE_FRIEND,
        EMOTICON_PLAY_POINT,

        LUCKY_POCKET,
        MESSAGE,
        NICKNAME_CHANGE,
        NICKNAME_CHANGE_FIRST,
    }

    public enum StackableItemType
    {
        None,
        Origin,
        Stack,
    }

    public enum ShopMainTapType
    {
        NONE,
        AVATAR,
        CLASS,
        ITEM,
    }

    public enum ShopSubTapType
    {
        NONE,
        ALL,
        AVATAR,
        AVATAR_NORMAL,
        AVATAR_CLASS_B,
        AVATAR_CLASS_A,
        AVATAR_CLASS_S,
        AVATAR_EVENT,
        CLASS,
        CLASS_SUB,
        MSG,
        NICK_CHANGE,
        LUCKY_POCKET,
        BOOSTER,
    }


    public enum ItemEffect
    {
        EFFECT_NOT_EFFECTABLE,
    }


    public enum RewardType
    {
        Chip,
        Emoticon,
    }


    public enum PointRewardType
    {
        PRT_NONE = 0,
        PRT_BOOST_6 = 1,
        PRT_BOOST_17 = 2,
        PRT_BOOST_39 = 3,
        PRT_BOOST_67 = 4,
        PRT_BOOST_100 = 5,

        /** PRT_ACHIEVEMENTS_05M - 50만 포인트 업적 보상 */
        PRT_ACHIEVEMENTS_05M = 6,

        /** PRT_ACHIEVEMENTS_5M - 500만 포인트 업적 보상 */
        PRT_ACHIEVEMENTS_5M = 7,

        /** PRT_ACHIEVEMENTS_50M - 5000만 포인트 업적 보상 */
        PRT_ACHIEVEMENTS_50M = 8,

        /** PRT_ACHIEVEMENTS_05B - 5억 포인트 업적 보상 */
        PRT_ACHIEVEMENTS_05B = 9,

        /** PRT_CLASS_B_1 - 클래스 B 업적 보상 800 : 140만 */
        PRT_CLASS_B_1 = 10,

        /** PRT_CLASS_B_2 - 클래스 B 업적 보상 2500 : 200만 */
        PRT_CLASS_B_2 = 11,

        /** PRT_CLASS_B_3 - 클래스 B 업적 보상 5000 : 300만 */
        PRT_CLASS_B_3 = 12,

        /** PRT_CLASS_A_1 - 클래스 A 업적 보상 6000 : 400만 */
        PRT_CLASS_A_1 = 13,

        /** PRT_CLASS_A_2 - 클래스 A 업적 보상 14000 : 600만 */
        PRT_CLASS_A_2 = 14,

        /** PRT_CLASS_A_3 - 클래스 A 업적 보상 25000 : 1000만 */
        PRT_CLASS_A_3 = 15,

        /** PRT_CLASS_S_1 - 클래스 S 업적 보상 20000 : 1000만 */
        PRT_CLASS_S_1 = 16,

        /** PRT_CLASS_S_2 - 클래스 S 업적 보상 65000 : 1500만 */
        PRT_CLASS_S_2 = 17,

        /** PRT_CLASS_S_3 - 클래스 S 업적 보상 100000 : 5000만 */
        PRT_CLASS_S_3 = 18,
        UNRECOGNIZED = -1,
    }

    public enum Suit
    {
        Hearts, // ♥
        Diamonds, // ♦
        Clubs, // ♣
        Spades // ♠
    };

    public enum Rank
    {
        Two = 2,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King,
        Ace
    };

    public enum HoldemBtnType
    {
        Fold, Check,
        Bbing, Call, Ddadang, Quater, Half, Allin, Max, End
    }


    public enum LocalizeNameKeys
    {
        None,
        
    }

    public enum LocalizeDescKeys
    {
        None,
        ClassB,
        ClassA,
        ClassS,
        Class,
        Days30Ticket,
        Subscription,
        UnknownError,
        GameExit,
        GameExitConfirm,
        PaymentLimitExceeded,
        MonthlyLimitExceeded,
        SubscriptionCancelNotice,
        Subscription30DayCancelMsg,
        Notice,
        ExistingSubscriptionWarning,
        ClassChangeNotice,
        GradeWillDisappear,
        PurchaseFailed,
        StoreInitializing,
        InitFailed,
        AvatarGoldPurchased,
        NicknameChangePurchased,
        BoosterPurchased,
        ClassB30Purchased,
        ClassA30Purchased,
        ClassS30Purchased,
        ClassBSubPurchased,
        ClassASubPurchased,
        ClassSSubPurchased,
        InviteFriend,
        InviteLinkCopied,
        InviteFriendCompleted,
        AlreadyInvited,
        InvalidInviteCode,
        DownloadFailed,
        ServerConnectionFailed,
        ServerConnectionFailedMsg,
        Holdem,
        Badugi,
        SevenPoker,
        GamePermanentBan,
        GameTemporaryBan,
        InvalidIdOrPassword,
        LoginFailed,
        IdentityVerificationRequired,
        IdentityVerificationExpiredReauth,
        Close,
        EmojiSendFailed,
        CannotMessageDeactivatedUser,
        BlankOnlyNotAllowed,
        MessageItemInsufficient,
        MessageTooLong,
        EnterMessage,
        RecipientNotFound,
        UserNotInChatRoom,
        BlockedByRecipient,
        MessageSendFailed,
        ChatDeleted,
        ChatDeleteFailedServerError,
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        FriendList,
        BlockList,
        MinTwoCharactersRequired,
        SearchError,
        NoSearchResults,
        BlockReleased,
        BlockReleaseFailed,
        ItemExpired,
        ItemExpiredMoveToVault,
        ReceiveAll,
        NoGoldToReceive,
        ReceiveFailed,
        FailedToLoadGameRoom,
        ItemExpiredEnterRoom,
        IdentityVerificationExpiredLogin,
        IdentityVerificationExpiredTitle,
        DuplicateAccess,
        DuplicateAccessMsg,
        IdentityVerificationNeeded,
        HowMuchDeposit,
        HowMuchWithdraw,
        ItemAcquired,
        CheckItemInVault,
        ExpiresInDays,
        ExpiresInHours,
        ExpiresInMinutes,
        ExpiringSoon,
        AM,
        PM,
        PermanentOwnership,
        ReceiveAvailable,
        ReceiveCompleted,
        ReceiveNotAvailable,
        Avatar,
        Emot,
        PeriodExpired,
        Won,
        CannotPurchase,
        AllRewardsCompleted,
        Completed,
        GetReward,
        MinGoldRequired,
        MaxGoldExceeded,
        CharLimitExceeded,
        InvalidNickname,
        PerMonth,
        SelectionComplete,
        CustomerService,
        FAQ,
        InquiryList,
        Inquiry,
        FAQLoadFailed,
        NoFAQRegistered,
        InquiryHistoryLoadFailed,
        AnswerCompleted,
        AnswerPending,
        InquiryDeleteFailed,
        JustNow,
        TitleMinLength,
        TitleMaxLength,
        ContentMinLength,
        ContentMaxLength,
        InquiryRegisterFailed,
        InquiryRegistered,
        GalleryPermissionRequired,
        FileNotFound,
        FileSizeLimit,
        OnlyImageOrVideo,
        FileAddError,
        FeatureComingSoon,
        LossLimitChanged,
        NoticeLoadFailed,
        NoticeLoadError,
        AccountDeleteLimitTitle,
        AccountDeleteLimitMsg,
        OnlyAlphanumericLower,
        Length6to15,
        PasswordAlphanumericSpecial,
        Length6to20,
        PasswordMismatch,
        IDAndPasswordRequired,
        OnlyAlphanumericSpecial,
        IdAlreadyInUse,
        PasswordInfoWrong,
        AdWatchVerifyFailed,
        AdRewardRequestFailed,
        CannotGetReward,
        InvalidTime,
        PeriodEnded,
        RemainingDays,
        RemainingHours,
        RemainingMinutes,
        LessThanOneMinute,
        Man,
        Eok,
        Jo,
        Gyeong,
        Hae,
        RoyalStraightFlush,
        StraightFlush,
        FourCard,
        FullHouse,
        Flush,
        FlushKorean,
        Straight,
        Triple,
        TwoPair,
        OnePair,
        High,
        Unknown,
        BackStraightFlush,
        BackStraight,
        Golf,
        Second,
        Third,
        MaidTop,
        Base,
        TwoBase,
        NoPattern,
        Forfeit,
        DealerFee,
        ReportAccepted,
        AlreadyReported,
        NoVoteTicket,
        KickVoteCompleted,
        FriendRequestCompleted,
        ServerResponseFailed,
        SelectCardToDiscard,
        SelectCardToOpen,
        Max100Chars,
        Google,
        Apple,
        Kakao,
        Naver,
        Atoz,
        TestAccount,
        FriendDeleteCompleted,
        BlockFailed,
        OrMore,
        OneOnOne,
        Other,
        LocalBundleUsed,
        CheckingVersion,
        Initializing,
        CheckingUpdates,
        DaysRemaining,
        HoursRemaining,
        MinutesRemaining,
        Win,
        Lose,
        WinRate,
        Total,
        MinutesAgo,
        HoursAgo,
        DaysAgo,
        MonthsAgo,
        YearsAgo,
        GoldMinEntry,
        GoldMaxEntry,
        Jackpot,
        KickVoteWarning,
        KickVoteReceived,
        ServerErrorWithReason,
        ChatDeleteFailed,
        ServerConnectionDisconnected,
        SocialLoginFailed,
        RegisterFailed,
        KickVoteFailed,
        FriendRequestFailed,
        AbsenceActivationError,
        MonthlyPaymentLimit,
        IdentityVerifyExpiry,
        DailyResetRule,
        AttachmentLimitMsg,
        MaintenanceTimeMsg,
        MaintenanceReason,
        MaintenanceStart,
        MaintenanceEnd,
        CheckingResources,
        WinLoseRecord,
        WinLoseWinrateRecord,
        DateFormat,
        PurchaseNote,
        PurchaseNoteWithQuantity,
        Gold,
        OrLess,
        NicknameChangePurchasedWithQuantity,
        InviteFriendMessage,
        BlockedUserMsg,
        WinLoseRateRecord,
        QuickLogin,
        Login,
        Logout,
        Register,
        NoRegisteredAccounts,
        QuickLoginDesc,
        AtozLoginBtn,
        AtozRegisterBtn,
        AppleLoginBtn,
        GoogleLoginBtn,
        NaverLoginBtn,
        KakaoLoginBtn,
        SelectLoginMethod,
        UserId,
        PasswordLabel,
        ChangePassword,
        FindPassword,
        FindIdPassword,
        IdList,
        DeleteId,
        LoginWithDifferentId,
        EnterPasswordHint,
        ReEnterPasswordHint,
        EnterPasswordPlaceholder,
        EnterIdHint,
        EnterIdPlaceholder,
        ConfirmPassword,
        NewPasswordLabel,
        ConfirmNewPasswordLabel,
        DisableAutoLoginConfirm,
        IdFormatHint,
        PasswordFormatHint,
        Reason,
        Yes,
        No,
        Guidebook,
        Shop,
        Announcements,
        Achievement,
        TodaysMission,
        VideoBonus,
        VideoBonusShort,
        LuckyBag,
        BoosterLabel,
        ItemLabel,
        VaultLabel,
        NicknameChangeTicket,
        MessageItem,
        NormalLabel,
        Day30Label,
        ProfileLabel,
        MyInfo,
        AccountInfoLabel,
        AccountWithdraw,
        ChangeLossLimit,
        SelectAvatar,
        ChangeAvatar,
        TodayRecord,
        AllRecord,
        WinRateLabel,
        TodayLimitLabel,
        ThisMonthPayment,
        GameRecordLabel,
        AccountDeleteConfirmTitle,
        SettingsLabel,
        BGM,
        SFX,
        SoundLabel,
        VoiceLabel,
        AllSoundLabel,
        ChatLabel,
        UseEmoticon,
        MyTurnVibration,
        ReserveBetting,
        VerticalMode,
        FourColorCard,
        GameOption,
        TermsOfService,
        PrivacyPolicy,
        PrivacyPolicySpace,
        OperationPolicy,
        FriendLabel,
        AddFriend,
        ReceivedRequest,
        SentRequest,
        FindUser,
        EmoticonGrant,
        FriendListEmpty,
        NoReceivedRequest,
        NoSentRequest,
        BlockListEmpty,
        InviteFiveCondition,
        StartChatDesc,
        WithdrawnUser,
        FriendRequestArrived,
        GameInProgress,
        GameReady,
        ReadyComplete,
        RecordLabel,
        OtherReason,
        Cheating,
        ReportBtn,
        Die,
        BtnDdadang,
        BtnBbing,
        BtnCheck,
        BtnCall,
        BtnQuarter,
        BtnHalf,
        BtnAllin,
        BtnMax,
        HoldemHandGuide,
        BadugiHandGuide,
        SevenPokerHandGuide,
        LowBadugi,
        SevenPokerTitle,
        GoldVault,
        OwnedGold,
        DepositBtn,
        WithdrawBtn,
        VaultEmpty,
        CategoryLabel,
        EmailLabel,
        NoInquiryFound,
        Diamond,
        Spade,
        Clover,
        Heart,
        ErrorOccurred,
        NetworkError,
        ConnectionUnstable,
        DownloadingResources,
        ClassAFull,
        ClassBFull,
        ClassSFull,
        ClassAExclusive,
        ClassBExclusive,
        ClassSExclusive,
        NormalUser,
        HoldingLimit,
        HoldingLimitIncrease,
        DealerFeeDiscount,
        ExclusiveShop,
        ExclusiveAvatar,
        TotalBenefit,
        UseLabel,
        IncreaseLabel,
        GrantLabel,
        DiscountLabel,
        TenTimes,
        ClassSSubscription,
        DealerFee2Percent,
        DealerFee3Percent,
        DealerFee4Percent,
        HoldingLimit20B,
        HoldingLimit50B,
        HoldingLimit100B,
        TotalBenefitClassB,
        TotalBenefitClassA,
        TotalBenefitClassS,
        ClassBExclusiveAvatarDesc,
        ClassAExclusiveAvatarDesc,
        ClassSExclusiveAvatarDesc,
        PriceLabel,
        QuantityLabel,
        DetailLabel,
        SearchLabel,
        DeleteBtn,
        Ruby,
        Equipped,
        DefaultLabel,
        AllLabel,
        InstantGold,
        Online,
        Offline,
        NicknamePlaceholder,
        ChangeNicknamePlaceholder,
        DailyMissionLabel,
        DealerFeeLabel,
        UserProfileTitle,
        TodaysTotalLabel,
        GuideHoldemRank1,
        GuideHoldemRank2,
        GuideHoldemRank3,
        GuideHoldemRank4,
        GuideHoldemRank5,
        GuideHoldemRank6,
        GuideHoldemRank7,
        GuideHoldemRank8,
        GuideHoldemRank9,
        GuideHoldemRank10,
        GuideHoldemRank11,
        GuideHoldemRank12,
        GuideHoldemRank10H,
        GuideHoldemStep1,
        GuideHoldemStep2,
        GuideHoldemStep3,
        GuideHoldemStep4,
        GuideHoldemDesc1,
        GuideHoldemDesc2,
        GuideHoldemDesc3,
        GuideHoldemDesc4,
        GuideHoldemDesc5,
        GuideHoldemDesc6,
        GuideHoldemPokerDesc,
        GuideHoldemFlushSuit,
        GuideHoldemFlushStraight,
        GuideHoldemRoyalA,
        GuideHoldemRoyalLow,
        GuideHoldem1Pair,
        GuideHoldem2Pair,
        GuideHoldemTriple,
        GuideHoldemFullHouse,
        GuideHoldemFourCard,
        GuideHoldemHighCard,
        GuideHoldemStraight,
        GuideHoldemStraight2,
        GuideBadugiRank1,
        GuideBadugiRank2,
        GuideBadugiRank3,
        GuideBadugiRank4,
        GuideBadugiRank5,
        GuideBadugiRank6,
        GuideBadugiRank7,
        GuideBadugiRank8,
        GuideBadugiBest,
        GuideBadugiFour,
        GuideBadugiFive,
        GuideBadugiSix,
        GuideBadugi3Cards,
        GuideBadugi2Cards,
        GuideBadugiAllDiff,
        GuideBadugiAllSame,
        GuideBadugiStep1,
        GuideBadugiStep2,
        GuideBadugiStep3,
        GuideBadugiDiscard,
        GuideSevenRank3,
        GuideSevenRank4,
        GuideSevenRank5,
        GuideSevenRank6,
        GuideSevenRank7,
        GuideSevenDesc,
        GuideSevenStep2,
        GuideSevenStep3,
        GuideSevenStep4,
        GuideSevenStep5,
        GuideSevenMorning,
        GuideSevenCards4,
        GuideSevenCards4R,
        GuideSevenCards2,
        GuideSevenEnd,
        GuideSevenLunch,
        GuideSevenHidden,
        GuideSevenOpen,
        GuideSevenRoyalFlush,
        GuideSevenRoyalPot,
        GuideSevenGolf,
        GuideSevenAnte,
        GuideSevenAnte2,
        GuideSevenOpen1,
        GuideNormalRoom,
        GuideOneOnOneMatch,
        GuideOneOnOneDesc,
        GuideClassSExclusive,
        MissionPlay20,
        MissionProgress,
        PointAchieve50k,
        PointAchieveLabel,
        PointLabel,
        WinGoldRate,
        NoStackAllowed,
        ExcludeOneOnOne,
        PurchaseCondition,
        BoosterDesc,
        AgeRestrictionNotice,
        PermanentBanMsg,
        ServerMaintenanceTime,
        MaintenanceStartTime,
        MaintenanceEndTime,
        NoMessageArrived,
        HandRankName,
        MaidTopDisplay,
        NNNN_GoldEntry,
        MonthlyLimitLabel,
        VaultAmount,
        InstantReceive,
        ImmediateGoldDesc,
        OtherDeviceConnectedTokenInvalid,
        Confirm,
        PopupLossLimitDesc,
        PopupLossLimitCheckTitle,
        PopupLossLimitCheckDesc,
        Change,
    }
    

    public enum RewardMissionType
    {
        ALL_IN_NORMAL,
        ALL_IN_SPECIAL,
        ATTENDANCE,

        CONTINUOUS_ACCESS,
        CONTINUOUS_ACCESS_7,
        CONTINUOUS_ACCESS_30,
        CONTINUOUS_ACCESS_100,
        CONTINUOUS_ACCESS_365,

        CREATE_AVATAR,
        FIRST_RESERVATION,

        INVITE_FRIEND,
        INVITE_FRIEND_1,
        INVITE_FRIEND_2,
        INVITE_FRIEND_3,
        INVITE_FRIEND_4,
        INVITE_FRIEND_5,

        PLAY_GAMES_1K,
        PLAY_GAMES_10K,
        PLAY_GAMES_100K,
        PLAY_GAMES_1M,

        PLAY_GAMES,
        PLAY_GAMES_20,
        PLAY_GAMES_RANDOM_10,

        PLAY_HOUR_1,

        PLAY_POINT,
        PLAY_POINT_50,
        PLAY_POINT_50K,
        PLAY_POINT_50M,
        PLAY_POINT_5M,

        WATCH_AD,
        WIN_GAMES_5,
        PLAY_GAMES_RANDOM_DAILY,
    }

    public enum RewardGameType
    {
        NONE,
        HOLDEM,
        BADUGI,
        SEVENPOKER,
    }

    public enum RewardProcessType
    {
        None = 0,
        CONTINUOUS_ACCESS,
        INVITE_FRIEND,
        PLAY_TIME,
        PLAY_GAMES_BADUGI,
        PLAY_GAMES_HOLDEM,
        PLAY_GAMES_SEVEN_POKER,
        PLAY_GAMES_DAILY,
        PLAY_GAMES,
        WIN_GAMES,
        POINTS,

    }

    public enum RewardMissionCategoryType
    {
        None,
        REWARD_ALLIN,
        REWARD_DAILY,
        REWARD,
        MISSION_DAILY,
    }

    public enum PostsTypeInRecieved
    {
        POST_NONE_STATE = 0,
        POST_BOX_NOT_RECEIVED,
        POST_BOX_RECEIVED,
        POST_BOX_EXPIRED
    }
    public enum PostsStateInRecieved
    {
        POST_NONE = 0,
        POST_SYSTEM,
        POST_REWARD,
        POST_REFUND,
        POST_OVERFLOW,
        POST_GAME_OVERFLOW,
        POST_MESSAGE,
        POST_OTHER
    }

    public enum PointsRewardType
    {
        PRT_ACHIEVEMENTS_50,
        PRT_ACHIEVEMENTS_5K,
        PRT_ACHIEVEMENTS_5M,
        PRT_ACHIEVEMENTS_50M,
        PRT_BOOST_6,
        PRT_BOOST_17,
        PRT_BOOST_39,
        PRT_BOOST_67,
        PRT_BOOST_100,
        PRT_CLASS_A_1,
        PRT_CLASS_A_2,
        PRT_CLASS_A_3,
        PRT_CLASS_B_1,
        PRT_CLASS_B_2,
        PRT_CLASS_B_3,
        PRT_CLASS_S_1,
        PRT_CLASS_S_2,
        PRT_CLASS_S_3,
        PRT_LUCKYBOX,
    }

    #region photon!!!!!

    public enum AckType : byte
    {
        HoldemGameStart = 0,
        BadugiGameStart = 1,
        HoldemSyncDealerButton,
        RpcComplete = 101, // 이 값들이 곧 RaiseEvent의 이벤트 코드가 됨 (0~199 권장)
        InventoryApplied = 102,
        // 필요에 따라 추가
    }

    #endregion

    #region API!!

    public enum Payment_type
    {
        SINGLE,
        RECURRING,
    }

    public enum ClassGrade
    {
        None = 0,
        CLASS_S,
        CLASS_A,
        CLASS_B
    }

    public enum ClassPurchaseType
    {
        NEW,
        APPEND,
        UPGRADE,
        FAIL,
    }

    public enum AdMobType
    {
        WATCH_AD,
        WATCH_AD_DAILY,
    }

    public enum EmoticonKindType
    {
        Rabbit = 0, GreyHood, End
    }

    public enum EmoticonExpressType
    {
        laugh, blue, angry, surprise, cong, happy,
    }
    
    #endregion

    public enum QuestRewardType
    {
        None,
        GAME_MONEY,
        ITEM_ID,
    }
}