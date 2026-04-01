using System;
using UnityEngine;

namespace CAPYBARA
{
    public class SPokerDispatchPushHub : MonoBehaviour
    {
         public static event Action<CAPYBARA.sevenPoker.EnterNoti,int>  OnEnterNoti;
        public static event Action<CAPYBARA.sevenPoker.LeaveNoti,int>  OnLeaveNoti;
        public static event Action<CAPYBARA.sevenPoker.StartNoti,int>  OnStartNoti;
        public static event Action<CAPYBARA.sevenPoker.HandCardNoti,int>  OnHandCardNoti;
        public static event Action<CAPYBARA.sevenPoker.HandCardNotiOther,int>  OnHandCardNotiOther;
        public static event Action<CAPYBARA.sevenPoker.TurnNoti,int>  OnTurnNoti;
        public static event Action<CAPYBARA.sevenPoker.ActionNoti,int>  OnActionNoti;
        
        public static event Action<CAPYBARA.sevenPoker.SelectNoti,int>  OnSelectNoti;
        public static event Action<CAPYBARA.sevenPoker.BossMovedNoti,int>  OnBossMoveNoti;
        
        public static event Action<sevenPoker.ShowdownNoti,int> OnShowdownNoti;
        public static event Action<sevenPoker.ResultNoti,int> OnResultNoti;
        public static event Action<sevenPoker.KickVoteNoti,int> OnKickedNoti;
        public static event Action<sevenPoker.EmoteNoti,int> OnEmoteNoti;
        public static event Action<sevenPoker.CardOpenNoti,int> OnCardOpenNoti;
        public static event Action<sevenPoker.LeaveReservedNoti, int> OnLeaveReserveNoti;
        
        public static int revisionId=0;
        public void Init()
        {
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.EnterNoti, pkt => {  revisionId = 0; OnEnterNoti?.Invoke(pkt.EnterNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.LeaveNoti, pkt => {  revisionId = 0; OnLeaveNoti?.Invoke(pkt.LeaveNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.StartNoti, pkt => { revisionId++;OnStartNoti?.Invoke(pkt.StartNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.HandCardNoti, pkt => { revisionId++;OnHandCardNoti?.Invoke(pkt.HandCardNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.HandCardNotiOther, pkt => { revisionId++;OnHandCardNotiOther?.Invoke(pkt.HandCardNotiOther,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.TurnNoti, pkt => {revisionId++; OnTurnNoti?.Invoke(pkt.TurnNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.ActionNoti, pkt => { revisionId++;OnActionNoti?.Invoke(pkt.ActionNoti,revisionId); });
            
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.SelectNoti, pkt => { revisionId++;OnSelectNoti?.Invoke(pkt.SelectNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.BossMovedNoti, pkt => { revisionId++;OnBossMoveNoti?.Invoke(pkt.BossMovedNoti,revisionId); });
            
            
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.ShowdownNoti, pkt => { revisionId++;OnShowdownNoti?.Invoke(pkt.ShowdownNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.KickVoteNoti, pkt => { revisionId++;OnKickedNoti?.Invoke(pkt.KickVoteNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.ResultNoti, pkt => {revisionId++; OnResultNoti?.Invoke(pkt.ResultNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.EmoteNoti, pkt => { revisionId++;OnEmoteNoti?.Invoke(pkt.EmoteNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.CardOpenNoti, pkt => {  revisionId++;OnCardOpenNoti?.Invoke(pkt.CardOpenNoti,revisionId); });
            Services.SevenPokerDispatcher.AddEvent((int)CAPYBARA.sevenPoker.Packet.PayloadOneofCase.LeaveReservedNoti, pkt => {  revisionId++;OnLeaveReserveNoti?.Invoke(pkt.LeaveReservedNoti,revisionId); });

        }

        public void Dispose()
        {
            OnEnterNoti = null;
            OnLeaveNoti = null;
            OnStartNoti = null;
            OnHandCardNoti = null;
            OnHandCardNotiOther = null;
            OnTurnNoti = null;
            OnActionNoti = null;
            OnShowdownNoti = null;
            OnResultNoti = null;
            OnKickedNoti = null;

            OnSelectNoti = null;
            OnBossMoveNoti = null;
            OnEmoteNoti = null;
            OnCardOpenNoti = null;
            OnLeaveReserveNoti = null;
        }
        
        // Update is called once per frame
        void Update()
        {
            if (CPPlayer.Server.sevenPokerConnection != null)
            {
                if (CPPlayer.Server.sevenPokerConnection.isConnected)
                {
                    if (Services.SevenPokerDispatcher != null)
                    {
                        Services.SevenPokerDispatcher.Pump(64);    
                    }
                }
            }
            
        }
    }

}
