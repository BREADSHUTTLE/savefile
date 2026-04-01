#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;
using Common;

namespace CAPYBARA.holdem
{
    public class HoldemClient:BaseClient<Packet,Base>
    {
        public HoldemClient(ProtoConnection<Packet> conn, uint protocolVersion) : base(conn, protocolVersion)
        {
        }

        protected override Base NewBase(string req, uint ver, string trace) =>
            new Base() {  TxId = trace};
        protected override void SetBase(Packet pkt, Base @base)
        {
            pkt.Base = @base;
        }
        protected override string GetError(Packet res) => res.Base?.Error==null ? "EOk" : res.Base.Error.Code.ToString();
        protected override byte[] ToBytes(Packet pkt) => pkt.ToByteArray();

        private Packet NewReq(string reqName)
        {
            var b = NewBase(reqName, _protocolVersion, NewTraceId());
            return new Packet() { Base = b };
        }
        
        public class PacketResult<T>
        {
            private static readonly Common.Error _defaultOk = new Error { Code = ErrorCode.EOk };
            private Error? _error;

            public bool IsSuccess => Error.Code == ErrorCode.EOk;
            public Error Error
            {
                get => _error ?? _defaultOk;
                set => _error = value;
            }
            public T? Data;
        }
     
        
        #region HoldemAPI send/get
        public async UniTask<PacketResult<ConnectRes>> ConnectAsync(string token, CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ConnectReq));
            reqPkt.ConnectReq = new ConnectReq() { Token = token};

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ConnectRes>(){Error = res.Base.Error,Data = res.ConnectRes};
        }
        
        
        public async UniTask<PacketResult<EnterRes>> EnterRoomAsync(int room_id,long buy_in,int excludedTableId,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(EnterReq));
            reqPkt.EnterReq = new EnterReq() {RoomId =room_id,BuyIn =buy_in,ExcludedTableId =excludedTableId };

            var res = await RequestAsync(reqPkt, 3000, ct);
            
            return new PacketResult<EnterRes>(){Error = res.Base.Error,Data = res.EnterRes};
        }
        
        public async UniTask<PacketResult<LeaveRes>> LeaveRoomAsync(int table_id,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(LeaveReq));
            reqPkt.LeaveReq = new LeaveReq() { TableId = table_id};

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<LeaveRes>(){Error = res.Base.Error,Data = res.LeaveRes};
        }
        
        public async UniTask<PacketResult<LeaveCancelRes>> LeaveRoomCacnelAsync(int table_id,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(LeaveCancelReq));
            reqPkt.LeaveCancelReq = new LeaveCancelReq() { TableId = table_id};

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<LeaveCancelRes>(){Error = res.Base.Error,Data = res.LeaveCancelRes};
        }
        
        public async UniTask<PacketResult<ActionRes>> ActionAsync(int table_id,Common. ActionType _aType,long amount,Common.BetSizeType sizeType,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ActionReq));
            reqPkt.ActionReq = new ActionReq() { TableId = table_id,Action = _aType, Amount = amount,BetSizeType = sizeType};
            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<ActionRes>(){Error = res.Base.Error,Data = res.ActionRes};
        }
        
        public async UniTask<PacketResult<KickVoteRes>> KickVoteAsync(int tableId,long targetUid,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(KickVoteReq));
            reqPkt.KickVoteReq = new KickVoteReq() { TableId = tableId,TargetUid = (int)targetUid};

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<KickVoteRes>(){Error = res.Base.Error,Data = res.KickVoteRes};
        }
        
        public async UniTask<PacketResult<ActivateRes>> ActivateAsync(int table_id,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(ActivateReq));
            reqPkt.ActivateReq = new ActivateReq() { TableId = table_id};
            var res = await RequestAsync(reqPkt, 3000, ct);

            return new PacketResult<ActivateRes>(){Error = res.Base.Error,Data = res.ActivateRes};
        }

        public async UniTask<PacketResult<UserInfoRes>> UserInfoReqAsync(long userId,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(UserInfoReq));
            reqPkt.UserInfoReq = new UserInfoReq() { Uid = userId};
            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<UserInfoRes>(){Error = res.Base.Error,Data = res.UserInfoRes};
        }
        
        
        public async UniTask<PacketResult<EmoteRes>> EmoteReqAsync(int table_id,int chairId,string emotion ,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(EmoteReq));
            reqPkt.EmoteReq = new EmoteReq() { TableId = table_id,ChairId = chairId,Emotion = emotion};
            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<EmoteRes>(){Error = res.Base.Error,Data = res.EmoteRes};
        }
        
                
        public async UniTask<PacketResult<CardOpenRes>> CardOpenReqAsync(int table_id,List<string> holecards,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(CardOpenReq));
            reqPkt.CardOpenReq = new CardOpenReq() { TableId = table_id};
            reqPkt.CardOpenReq.HoleCards.AddRange(holecards);
            var res = await RequestAsync(reqPkt, 3000, ct);

            return new PacketResult<CardOpenRes>(){Error = res.Base.Error,Data = res.CardOpenRes};
        }
        
        public async UniTask<PacketResult<CardCloseRes>> CardCloseReqAsync(int table_id,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(CardCloseReq));
            reqPkt.CardCloseReq = new CardCloseReq() { TableId = table_id};
            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<CardCloseRes>(){Error = res.Base.Error,Data = res.CardCloseRes};
        }
        
        public async UniTask<PacketResult<bool>> PingReqAsync(int elapsedMilsec,CancellationToken ct = default)
        {
            var reqPkt = NewReq(nameof(PingReq));
            reqPkt.PingReq = new PingReq(){Elapsed =elapsedMilsec };

            var res = await RequestAsync(reqPkt, 3000, ct);
            return new PacketResult<bool>() { Error = res.Base.Error,Data = false};
        }

        #endregion
    }
}
