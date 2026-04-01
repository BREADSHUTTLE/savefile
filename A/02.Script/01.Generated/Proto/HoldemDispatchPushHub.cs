using System;
using UnityEngine;

namespace CAPYBARA
{
    public class HoldemDispatchPushHub : MonoBehaviour
    {
        public static bool IsHoldemActive { get; set; } = false;

        
        public static event Action<CAPYBARA.holdem.EnterNoti,int>  OnEnterNoti;
        public static event Action<CAPYBARA.holdem.LeaveNoti,int>  OnLeaveNoti;
        public static event Action<CAPYBARA.holdem.StartNoti,int>  OnStartNoti;
        public static event Action<CAPYBARA.holdem.HoleCardNoti,int>  OnHoleCardNoti;
        public static event Action<CAPYBARA.holdem.HoleCardNotiOther,int>  OnHoleCardNotiOther;
        public static event Action<CAPYBARA.holdem.TurnNoti,int>  OnTurnNoti;
        public static event Action<CAPYBARA.holdem.ActionNoti,int>  OnActionNoti;
        public static event Action<CAPYBARA.holdem.CommunityCardsNoti,int>  OnCommunityCardsNoti;
        public static event Action<holdem.ShowdownNoti,int> OnShowdownNoti;
        public static event Action<holdem.ResultNoti,int> OnResultNoti;
        public static event Action<holdem.KickVoteNoti,int> OnKickedNoti;
        public static event Action<holdem.EmoteNoti,int> OnEmoteNoti;
        public static event Action<holdem.CardOpenNoti,int> OnCardOpenNoti;
        public static event Action<holdem.CardNoti,int> OnCardNoti;
        public static event Action<holdem.LeaveReservedNoti,int> OnLeaveReserveNoti;
        
        
        public static int revisionId=0;
        public void Init()
        {
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.EnterNoti, pkt => { if (!IsHoldemActive) return; revisionId=0;OnEnterNoti?.Invoke(pkt.EnterNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.LeaveNoti, pkt => {if (!IsHoldemActive) return;  OnLeaveNoti?.Invoke(pkt.LeaveNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.StartNoti, pkt => { if (!IsHoldemActive) return; revisionId++;OnStartNoti?.Invoke(pkt.StartNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.HoleCardNoti, pkt => { if (!IsHoldemActive) return; revisionId++;OnHoleCardNoti?.Invoke(pkt.HoleCardNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.HoleCardNotiOther, pkt => { if (!IsHoldemActive) return; revisionId++;OnHoleCardNotiOther?.Invoke(pkt.HoleCardNotiOther,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.TurnNoti, pkt => {if (!IsHoldemActive) return;  revisionId++;OnTurnNoti?.Invoke(pkt.TurnNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.ActionNoti, pkt => {if (!IsHoldemActive) return; revisionId++; OnActionNoti?.Invoke(pkt.ActionNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.CommunityCardsNoti, pkt => {if (!IsHoldemActive) return; revisionId++; OnCommunityCardsNoti?.Invoke(pkt.CommunityCardsNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.ShowdownNoti, pkt => {if (!IsHoldemActive) return; revisionId++; OnShowdownNoti?.Invoke(pkt.ShowdownNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.KickVoteNoti, pkt => { if (!IsHoldemActive) return; revisionId++;OnKickedNoti?.Invoke(pkt.KickVoteNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.ResultNoti, pkt => { if (!IsHoldemActive) return; revisionId++;OnResultNoti?.Invoke(pkt.ResultNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.EmoteNoti, pkt => { if (!IsHoldemActive) return; revisionId++;OnEmoteNoti?.Invoke(pkt.EmoteNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.CardOpenNoti, pkt => {if (!IsHoldemActive) return;  revisionId++;OnCardOpenNoti?.Invoke(pkt.CardOpenNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.CardNoti, pkt => { if (!IsHoldemActive) return; revisionId++;OnCardNoti?.Invoke(pkt.CardNoti,revisionId); });
            Services.HoldemDispatcher.AddEvent((int)CAPYBARA.holdem.Packet.PayloadOneofCase.LeaveReservedNoti, pkt => { if (!IsHoldemActive) return; revisionId++;OnLeaveReserveNoti?.Invoke(pkt.LeaveReservedNoti,revisionId); });
            
        }

        public void Dispose()
        {
            OnEnterNoti = null;
            OnLeaveNoti = null;
            OnStartNoti = null;
            OnHoleCardNoti = null;
            OnHoleCardNotiOther = null;
            OnTurnNoti = null;
            OnActionNoti = null;
            OnCommunityCardsNoti = null;
            OnShowdownNoti = null;
            OnResultNoti = null;
            OnKickedNoti = null;
            OnCardOpenNoti = null;
            OnCardNoti = null;
            OnLeaveReserveNoti = null;
            OnEmoteNoti = null;
            revisionId = 0;
        }
        
        // Update is called once per frame
        void Update()
        {
            if (CPPlayer.Server.holdemConnection != null)
            {
                if (CPPlayer.Server.holdemConnection.isConnected)
                {
                    if (Services.HoldemDispatcher != null)
                    {
                        Services.HoldemDispatcher.Pump(64);    
                    }
                }
            }
          
            
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
    }

}
