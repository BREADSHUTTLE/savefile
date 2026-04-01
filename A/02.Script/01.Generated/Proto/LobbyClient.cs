#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using JetBrains.Annotations;
using UnityEngine;


namespace CAPYBARA.lobby
{
    public sealed class LobbyClient : BaseClient<lobby.Packet, lobby.Base>
    {
        public LobbyClient(ProtoConnection<lobby.Packet> conn, uint version) : base(conn, version)
        {
        }

        protected override Base NewBase(string req, uint ver, string trace) =>
            new Base() {  TxId = trace };

        protected override void SetBase(Packet pkt, Base @base)
        {
            pkt.Base = @base;
        }

        protected override string GetError(Packet res) => res.Base?.Error==null ? "EOk" : res.Base.Error.Code.ToString();

        protected override byte[] ToBytes(Packet pkt) => pkt.ToByteArray();

        private Packet NewReq(string reqName)
        {
            var b = NewBase(reqName, _protocolVersion, NewTraceId());
            return new Packet { Base = b };
        }

        public class PacketResult<T>
        {
            private static readonly Error _defaultOk = new Error { Code = ErrorCode.EOk };
            private Error? _error;

            public bool IsSuccess => Error.Code == ErrorCode.EOk;
            public Error Error
            {
                get => _error ?? _defaultOk;
                set => _error = value;
            }
            public T? Data;
        }

        #region OutGameAPI

        public async UniTask<PacketResult<LoginRes>> LoginAsync(string id, string pwd, bool kick_prev=false,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(LoginReq));

            var os = DeviceInfoCollector.GetPlatformtoInt();
            var manufacturer = DeviceInfoCollector.GetManufacturer();
            var model = DeviceInfoCollector.GetModel();
            var adid = DeviceInfoCollector.GetAdvertisingId();
            var uuid = DeviceInfoCollector.GetDeviceUniqueId();
            var bssid = DeviceInfoCollector.GetBSSID();

            reqPkt.LoginReq = new LoginReq()
            {
                Id = id,
                Pwd = pwd,
                Os = os,
                Manufacturer = manufacturer,
                Adid = adid,
                Model = model,
                Guid = uuid,
                Bssid = bssid,
                KickPrev = kick_prev,
                Version = Application.version,
            };

            var res = await RequestAsync(reqPkt, 3000, ct);
            var result = new PacketResult<LoginRes>() { Error = res.Base.Error, Data = res.LoginRes };
            if (result.IsSuccess)
            {
                Token = res.LoginRes.Token;
                CPPlayer.UserInfo.watchAdCount = res.LoginRes.WatchAd;
            }
            return result;
        }

        public void TemplateFunctionData()
        {
            Token = "dd";
        }

        public async UniTask<PacketResult<RegisterRes>> RegisterAsync(string id, string pwd, string nick, string loginType, string registerToken, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(RegisterReq));
            reqPkt.RegisterReq = new RegisterReq() { Id = id, Pwd = pwd, Nick = nick, LoginType = loginType,RegisterToken = registerToken};

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<RegisterRes>() { Error = res.Base.Error, Data = res.RegisterRes };
        }

        public async UniTask<PacketResult<NickCheckRes>> NickCheckAsync(string nickname, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(NickCheckReq));
            reqPkt.NickCheckReq = new NickCheckReq() { Nickname = nickname };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<NickCheckRes>() { Error = res.Base.Error, Data = res.NickCheckRes };
        }

        public async UniTask<PacketResult<NickSetRes>> NickSetAsync(string nickname, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(NickSetReq));
            reqPkt.NickSetReq = new NickSetReq() { Nickname = nickname };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<NickSetRes>() { Error = res.Base.Error, Data = res.NickSetRes };
        }

        public async UniTask<PacketResult<ServerListRes>> GetServerListAsync(Common.GameType type, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ServerListReq));
            reqPkt.ServerListReq = new ServerListReq() { GameType = type };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ServerListRes>() { Error = res.Base.Error, Data = res.ServerListRes };
        }

        public async UniTask<PacketResult<InventoryRes>> GetInventoryAsync(bool includeItem, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(InventoryReq));
            reqPkt.InventoryReq = new InventoryReq() { IncludeItem = includeItem };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<InventoryRes>() { Error = res.Base.Error, Data = res.InventoryRes };
        }

        public async UniTask<PacketResult<InventorySetRes>> AddItemToInventoryAsync(string itemId, int amount, string productId = "", CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(InventorySetReq));
            reqPkt.InventorySetReq = new InventorySetReq() { ItemId = itemId, Amount = amount, ProductId = productId };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<InventorySetRes>() { Error = res.Base.Error, Data = res.InventorySetRes };
        }

        public async UniTask<PacketResult<InventoryChangeRes>> InventoryChangeAsync(string itemId, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(InventoryChangeReq));
            reqPkt.InventoryChangeReq = new InventoryChangeReq() { ItemId = itemId };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<InventoryChangeRes>() { Error = res.Base.Error, Data = res.InventoryChangeRes };
        }

        public async UniTask<PacketResult<InventoryUseRes>> UseInventoryItemReqAsync(string itemId, long amount, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(InventoryUseReq));
            reqPkt.InventoryUseReq = new InventoryUseReq() { ItemId = itemId, Amount = amount };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<InventoryUseRes>() { Error = res.Base.Error, Data = res.InventoryUseRes };
        }

        /// <summary>
        /// 접속된 유저1개의 정보 가져오기
        /// </summary>
        /// <param name="ct">비동기 끊기 위한것</param>
        public async UniTask<PacketResult<UserRes>> GetUserInfoAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserReq));
            reqPkt.UserReq = new UserReq() { };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserRes>() { Error = res.Base.Error, Data = res.UserRes };
        }

        /// <summary>
        /// 접속된 계정의 모든 유저정보 가져오기
        /// </summary>
        /// <param name="ct">비동기 끊기 위한것</param>
        public async UniTask<PacketResult<UsersRes>> GetUserListInfoAsync(string loginToken, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UsersReq));
            reqPkt.UsersReq = new UsersReq() { Token = loginToken};

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UsersRes>() { Error = res.Base.Error, Data = res.UsersRes };
        }

        public async UniTask<PacketResult<SafeInRes>> SafeInAsync(long amount, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(SafeInReq));
            reqPkt.SafeInReq = new SafeInReq() { Amount = amount };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<SafeInRes>() { Error = res.Base.Error, Data = res.SafeInRes };
        }

        public async UniTask<PacketResult<SafeOutRes>> SafeOutAsync(long amount, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(SafeOutReq));
            reqPkt.SafeOutReq = new SafeOutReq() { Amount = amount };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<SafeOutRes>() { Error = res.Base.Error, Data = res.SafeOutRes };
        }

        public async UniTask<PacketResult<LoginRes>> SocialLoginAsync(string login_type, string id, string email, string auth_token, string accessToken, bool kick_prev=false,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(LoginSocialReq));

            var os = DeviceInfoCollector.GetPlatformtoInt();
            var manufacturer = DeviceInfoCollector.GetManufacturer();
            var model = DeviceInfoCollector.GetModel();
            var adid = DeviceInfoCollector.GetAdvertisingId();
            var uuid = DeviceInfoCollector.GetDeviceUniqueId();
            var bssid = DeviceInfoCollector.GetBSSID();

            reqPkt.LoginSocialReq = new LoginSocialReq()
            {
                LoginType = login_type,
                Id = id,
                Email = email,
                AuthToken = auth_token,
                AccessToken = accessToken,
                Os = os,
                Manufacturer = manufacturer,
                Adid = adid,
                Model = model,
                Guid = uuid,
                Bssid = bssid,
                KickPrev = kick_prev,
                Version = Application.version,
            };

            var res = await RequestAsync(reqPkt, 3000, ct);
            var result = new PacketResult<LoginRes>() { Error = res.Base.Error, Data = res.LoginRes };
            if (result.IsSuccess)
            {
                Token = res.LoginRes.Token;
                CPPlayer.UserInfo.watchAdCount = res.LoginRes.WatchAd;
            }

            return result;
        }

        public async UniTask<PacketResult<PostsRes>> PostsReqAsync(lobby.PostsType postsType, lobby.PostsState postsState, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PostsReq));
            reqPkt.PostsReq = new PostsReq() { State = postsState, Type = postsType };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<PostsRes>() { Error = res.Base.Error, Data = res.PostsRes };
        }

        public async UniTask<PacketResult<PostsRecvRes>> PostsRecvAsync(List<int> idlist, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PostsRecvReq));

            reqPkt.PostsRecvReq = new PostsRecvReq() { Id = { idlist } };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<PostsRecvRes>() { Error = res.Base.Error, Data = res.PostsRecvRes };
        }

        public async UniTask<PacketResult<PostsDelRes>> PostsDelAsync(List<int> idlist, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PostsDelReq));

            reqPkt.PostsDelReq = new PostsDelReq() { Id = { idlist } };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<PostsDelRes>() { Error = res.Base.Error, Data = res.PostsDelRes };
        }

        public async UniTask<PacketResult<PointsRes>> PointsReqAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PointsReq));

            reqPkt.PointsReq = new PointsReq();


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<PointsRes>() { Error = res.Base.Error, Data = res.PointsRes };
        }

        public async UniTask<PacketResult<PointsRewardRes>> PointsRewardReqAsync(string rewardType, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PointsRewardReq));

            reqPkt.PointsRewardReq = new PointsRewardReq() { RewardType = rewardType };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<PointsRewardRes>() { Error = res.Base.Error, Data = res.PointsRewardRes };
        }

        public async UniTask<PacketResult<PointsDoneListRes>> PointsRewardDoneAsync(int startdate, int enddate, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PointsDoneListReq));

            reqPkt.PointsDoneListReq = new PointsDoneListReq() { StartDate = startdate, EndDate = enddate };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<PointsDoneListRes>() { Error = res.Base.Error, Data = res.PointsDoneListRes };
        }

        public async UniTask<PacketResult<UserGameOnRes>> UserGameOnReqAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserGameOnReq));
            reqPkt.UserGameOnReq = new UserGameOnReq();

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserGameOnRes>() { Error = res.Base.Error, Data = res.UserGameOnRes };
        }
        

        public async UniTask<PacketResult<FriendsListRes>> FriendsListReqAsync(FriendsListType friendsListType, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(FriendsReq));

            reqPkt.FriendsReq = new FriendsReq() { FriendsListType = friendsListType };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<FriendsListRes>() { Error = res.Base.Error, Data = res.FriendsListRes };
        }

        public async UniTask<PacketResult<FriendsRequestRes>> FriendsRequestAsync(FriendsRequestType friendsreqType, long friendsUid, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(FriendsRequestReq));

            reqPkt.FriendsRequestReq = new FriendsRequestReq() { FriendsRequestType = friendsreqType, FriendsUid = friendsUid };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<FriendsRequestRes>() { Error = res.Base.Error, Data = res.FriendsRequestRes };
        }
        public async UniTask<PacketResult<FriendsListRes>> FriendsFindReqAsync(string keyword, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(FriendsFindReq));

            reqPkt.FriendsFindReq = new FriendsFindReq() { Keywords = keyword };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<FriendsListRes>() { Error = res.Base.Error, Data = res.FriendsListRes };
        }

        public async UniTask<PacketResult<FriendsListRes>> FriendsTopPointsReqAsync(int num = 20, int page = 0, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(FriendsTopPointsReq));

            reqPkt.FriendsTopPointsReq = new FriendsTopPointsReq() { Num = num, Page = page };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<FriendsListRes>() { Error = res.Base.Error, Data = res.FriendsListRes };
        }

        public async UniTask<PacketResult<FriendsJoinRes>> FriendsJoinReqAsync(string code, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(FriendsJoinReq));

            reqPkt.FriendsJoinReq = new FriendsJoinReq() { Code = code };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<FriendsJoinRes>() { Error = res.Base.Error, Data = res.FriendsJoinRes };
        }

        public async UniTask<PacketResult<FriendsJoinListRes>> FriendsJoinListReqAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(FriendsJoinListReq));

            reqPkt.FriendsJoinListReq = new FriendsJoinListReq();


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<FriendsJoinListRes>() { Error = res.Base.Error, Data = res.FriendsJoinListRes };
        }

        public async UniTask<PacketResult<ChatDefaultRes>> ChatCreateAsync(long chatMyid, long chatOtherId, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ChatCreateReq));

            reqPkt.ChatCreateReq = new ChatCreateReq() { Uid1 = chatMyid, Uid2 = chatOtherId };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ChatDefaultRes>() { Error = res.Base.Error, Data = res.ChatDefaultRes };
        }

        public async UniTask<PacketResult<ChatDefaultRes>> ChatRoomReqAsync(long roomId, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ChatRoomReq));

            reqPkt.ChatRoomReq = new ChatRoomReq() { RoomId = roomId };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ChatDefaultRes>() { Error = res.Base.Error, Data = res.ChatDefaultRes };
        }

        public async UniTask<PacketResult<ChatListRes>> ChatReqAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ChatListReq));

            reqPkt.ChatListReq = new ChatListReq();


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ChatListRes>() { Error = res.Base.Error, Data = res.ChatListRes };
        }

        public async UniTask<PacketResult<ChatExitRes>> ChatExitReqAsync(long roomid, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ChatExitReq));

            reqPkt.ChatExitReq = new ChatExitReq() { RoomId = roomid };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ChatExitRes>() { Error = res.Base.Error, Data = res.ChatExitRes };
        }

        public async UniTask<PacketResult<MessageSendRes>> MessageSendReqAsync(long roomid, string msg, string emotion = "", long messageId = 0, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(MessageSendReq));

            reqPkt.MessageSendReq = new MessageSendReq() { RoomId = roomid, Message = msg, Emotion = emotion, MessageId = messageId };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<MessageSendRes>() { Error = res.Base.Error, Data = res.MessageSendRes };
        }

        public async UniTask<PacketResult<MessageRecvRes>> MessageRecvReqAsync(long roomid, long page, long countperpage, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(MessageRecvReq));

            reqPkt.MessageRecvReq = new MessageRecvReq() { RoomId = roomid, Page = page, CountPerPage = countperpage };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<MessageRecvRes>() { Error = res.Base.Error, Data = res.MessageRecvRes };
        }

        public async UniTask<PacketResult<MessageListRes>> MessageListReqAsync(long roomid, long page, long countperpage, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(MessageListReq));

            reqPkt.MessageListReq = new MessageListReq() { RoomId = roomid, Page = page, CountPerPage = countperpage };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<MessageListRes>() { Error = res.Base.Error, Data = res.MessageListRes };
        }

        public async UniTask<PacketResult<MessageNewCountRes>> NewMessageListCountAsync(IEnumerable<long> roomIds, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(MessageNewCountReq));

            reqPkt.MessageNewCountReq = new MessageNewCountReq();
            reqPkt.MessageNewCountReq.RoomId.AddRange(roomIds);

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<MessageNewCountRes>() { Error = res.Base.Error, Data = res.MessageNewCountRes };
        }

        #region UserQuest API (신규)

        public async UniTask<PacketResult<UserQuestListRes>> UserQuestListAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserQuestListReq));
            reqPkt.UserQuestListReq = new UserQuestListReq();

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserQuestListRes>() { Error = res.Base.Error, Data = res.UserQuestListRes };
        }

        public async UniTask<PacketResult<UserQuestAddRes>> UserQuestAddAsync(string questType, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserQuestAddReq));
            reqPkt.UserQuestAddReq = new UserQuestAddReq() { Type = questType };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserQuestAddRes>() { Error = res.Base.Error, Data = res.UserQuestAddRes };
        }

        public async UniTask<PacketResult<UserQuestListRes>> UserQuestRequestAsync(string questId, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserQuestRequestReq));
            reqPkt.UserQuestRequestReq = new UserQuestRequestReq() { QuestId = questId };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserQuestListRes>() { Error = res.Base.Error, Data = res.UserQuestListRes };
        }

        #endregion

        public async UniTask<PacketResult<LossLimitSetRes>> LossLimitSetAsync(int losslimit, bool isErrorToast = true, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(LossLimitSetReq));

            reqPkt.LossLimitSetReq = new LossLimitSetReq() { LossLimit = losslimit };


            var res = await RequestAsync(reqPkt, 3000, ct, isErrorToast);
            return new PacketResult<LossLimitSetRes>() { Error = res.Base.Error, Data = res.LossLimitSetRes };
        }

        public async UniTask<PacketResult<ClassInfoRes>> ClassInfoAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ClassInfoReq));
            reqPkt.ClassInfoReq = new ClassInfoReq();

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ClassInfoRes>() { Error = res.Base.Error, Data = res.ClassInfoRes };
        }

        public async UniTask<PacketResult<MemberRes>> MemberReqAsync(string autoToken, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(MemberReq));

            reqPkt.MemberReq = new MemberReq() { AutoToken = autoToken};


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<MemberRes>() { Error = res.Base.Error, Data = res.MemberRes };
        }

        public async UniTask<PacketResult<MatchRecordRes>> GetMatchRecordAsync(long uid, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(MatchRecordReq));

            reqPkt.MatchRecordReq = new MatchRecordReq() { Uid = uid };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<MatchRecordRes>() { Error = res.Base.Error, Data = res.MatchRecordRes };
        }

        public async UniTask<PacketResult<PurchaseRes>> PurchaseReqAsync(string productId, string receipt, InAppPlatform inAppPlatform, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PurchaseReq));
            if (inAppPlatform != InAppPlatform.Onestore)
            {
#if UNITY_ANDROID
                inAppPlatform = InAppPlatform.Google;
#else
             inAppPlatform = InAppPlatform.Apple;
#endif
            }


            reqPkt.PurchaseReq = new PurchaseReq() { InAppPlatform = inAppPlatform, ProductId = productId, RawReceipt = receipt };

            var res = await RequestAsync(reqPkt, 5000, ct);
            return new PacketResult<PurchaseRes>() { Error = res.Base.Error, Data = res.PurchaseRes };
        }

        public async UniTask<PacketResult<PurchaseMonthlyRes>> PurchaseMonthlyInfoAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PurchaseMonthlyReq));

            reqPkt.PurchaseMonthlyReq = new PurchaseMonthlyReq() { };


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<PurchaseMonthlyRes>() { Error = res.Base.Error, Data = res.PurchaseMonthlyRes };
        }

        public async UniTask<PacketResult<UserByUidRes>> UserReqByUserIdsAsync(IEnumerable<long> userIds, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserByUidReq));

            reqPkt.UserByUidReq = new UserByUidReq();
            reqPkt.UserByUidReq.Uid.AddRange(userIds);

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserByUidRes>() { Error = res.Base.Error, Data = res.UserByUidRes };
        }

        public async UniTask<PacketResult<UserSettingsSetRes>> UserSettingsSetReq(string settings, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserSettingsSetReq));

            reqPkt.UserSettingsSetReq = new UserSettingsSetReq() { Settings = settings };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserSettingsSetRes>() { Error = res.Base.Error, Data = res.UserSettingsSetRes };
        }

        public async UniTask<PacketResult<UserSettingsInfoRes>> UserSettingsInfoReq(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserSettingsInfoReq));

            reqPkt.UserSettingsInfoReq = new UserSettingsInfoReq();


            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserSettingsInfoRes>() { Error = res.Base.Error, Data = res.UserSettingsInfoRes };
        }

        public async UniTask<PacketResult<UserReportRes>> UserReportReqAsync(long reporterUid, long reportedUid, ReportReason reporterReason, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserReportReq));

            reqPkt.UserReportReq = new UserReportReq() { ReporterUid = reporterUid, ReportedUid = reportedUid, ReportReason = reporterReason };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserReportRes>() { Error = res.Base.Error, Data = res.UserReportRes };
        }

        public async UniTask<PacketResult<RoomsRes>> GameRoomsReq(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(RoomsReq));

            reqPkt.RoomsReq = new RoomsReq();

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<RoomsRes>() { Error = res.Base.Error, Data = res.RoomsRes };
        }

        public async UniTask<PacketResult<WithdrawalRes>> WithdrawalAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(WithdrawalReq));

            reqPkt.WithdrawalReq = new WithdrawalReq();

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<WithdrawalRes>() { Error = res.Base.Error, Data = res.WithdrawalRes };
        }

        public async UniTask<PacketResult<UserPwdSetRes>> SetPwdAsync(long uid,string pwd, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserPwdSetReq));

            reqPkt.UserPwdSetReq = new UserPwdSetReq() {Uid =uid, Pwd = pwd };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserPwdSetRes>() { Error = res.Base.Error, Data = res.UserPwdSetRes };
        }
        // 응답 안 기다리고 전송만
        public void AppStateFireAndForget(bool isForeground)
        {
            var reqPkt = NewReq(nameof(AppStateReq));
            reqPkt.AppStateReq = new AppStateReq() { IsForeground = isForeground };
            SendOnly(reqPkt);
        }

        public async UniTask<PacketResult<ConfigVersionGetRes>> ConfigVersionGetReqAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ConfigVersionGetReq));
            reqPkt.ConfigVersionGetReq = new ConfigVersionGetReq();

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ConfigVersionGetRes>() { Error = res.Base.Error, Data = res.ConfigVersionGetRes };
        }

        public async UniTask<PacketResult<ConfigPointsGetRes>> ConfigPointsGetReqAsync(long version, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ConfigPointsGetReq));
            reqPkt.ConfigPointsGetReq = new ConfigPointsGetReq() { Version = version };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ConfigPointsGetRes>() { Error = res.Base.Error, Data = res.ConfigPointsGetRes };
        }

        public async UniTask<PacketResult<UserQuestListRes>> ConfigQuestsGetReqAsync(long version, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ConfigQuestsGetReq));
            reqPkt.ConfigQuestsGetReq = new ConfigQuestsGetReq() { Version = version };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserQuestListRes>() { Error = res.Base.Error, Data = res.UserQuestListRes };
        }

        public async UniTask<PacketResult<ConfigItemsGetRes>> ConfigItemsGetReqAsync(long version, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ConfigItemsGetReq));
            reqPkt.ConfigItemsGetReq = new ConfigItemsGetReq() { Version = version };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ConfigItemsGetRes>() { Error = res.Base.Error, Data = res.ConfigItemsGetRes };
        }

        public async UniTask<PacketResult<ConfigInAppItemsGetRes>> ConfigInAppItemsGetReqAsync(long version, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ConfigInAppItemsGetReq));
            reqPkt.ConfigInAppItemsGetReq = new ConfigInAppItemsGetReq() { Version = version };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ConfigInAppItemsGetRes>() { Error = res.Base.Error, Data = res.ConfigInAppItemsGetRes };
        }

        public async UniTask<PacketResult<LoginRes>> AutoLoginAsync(string autoToken,bool kick_prev=false,CancellationToken ct = default)
        {
            Debug.Log("자동 로그인 호출");
            var reqPkt = NewReq(nameof(LoginAutoReq));

            var os = DeviceInfoCollector.GetPlatformtoInt();
            var manufacturer = DeviceInfoCollector.GetManufacturer();
            var model = DeviceInfoCollector.GetModel();
            var adid = DeviceInfoCollector.GetAdvertisingId();
            var uuid = DeviceInfoCollector.GetDeviceUniqueId();
            var bssid = DeviceInfoCollector.GetBSSID();
            
            reqPkt.LoginAutoReq = new LoginAutoReq()
            {
                Token = autoToken,
                Manufacturer = manufacturer,
                Adid = adid,
                Model = model,
                Guid = uuid,
                Bssid = bssid,
                KickPrev = kick_prev,
                Os =   os,
                Version = Application.version,
            };

            var res = await RequestAsync(reqPkt, 3000, ct);
            var result = new PacketResult<LoginRes>() { Error = res.Base.Error, Data = res.LoginRes };
            if (result.IsSuccess)
            {
                Token = res.LoginRes.Token;
                CPPlayer.UserInfo.watchAdCount = res.LoginRes.WatchAd;
            }
            return result;
        }

        public async UniTask<PacketResult<EventGetRes>> EventGetAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(EventGetReq));
            reqPkt.EventGetReq = new EventGetReq();
            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<EventGetRes>() { Error = res.Base.Error, Data = res.EventGetRes };
        }

        public async UniTask<PacketResult<ChatBlockRes>> ChatBlockAsync(long blockUid, int unblock, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ChatBlockReq));
            reqPkt.ChatBlockReq = new ChatBlockReq() { BlockUid = blockUid, Unblock = unblock };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ChatBlockRes>() { Error = res.Base.Error, Data = res.ChatBlockRes };
        }

        public async UniTask<PacketResult<ChatBlockListRes>> ChatBlockListAsync(CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ChatBlockListReq));
            reqPkt.ChatBlockListReq = new ChatBlockListReq();

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ChatBlockListRes>() { Error = res.Base.Error, Data = res.ChatBlockListRes };
        }
        
        public async UniTask<PacketResult<bool>> PingReqAsync(int elapsedMilsec,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PingReq));
            reqPkt.PingReq = new PingReq(){Elapsed =elapsedMilsec };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<bool>() { Error = res.Base.Error,Data = false};
        }

        public async UniTask<PacketResult<AllinRewardRes>> AllinRewardReqAsync(lobby.GameJoinType gameJoinType, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(AllinRewardReq));
            reqPkt.AllinRewardReq = new AllinRewardReq() { GameJoinType = gameJoinType };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<AllinRewardRes>() { Error = res.Base.Error, Data = res.AllinRewardRes };
        }
        
        
        #endregion
    }
}