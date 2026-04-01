using System;
using UnityEngine;

namespace CAPYBARA
{
    public class BadugiDispatchPushHub:MonoBehaviour
    {
        public static event Action<CAPYBARA.badugi.EnterNoti,int> OnEnterNoti;
        public static event Action<CAPYBARA.badugi.ReadyNoti,int> OnReadyNoti;
        public static event Action<CAPYBARA.badugi.LeaveNoti,int> OnLeaveNoti;
        public static event Action<CAPYBARA.badugi.StartNoti,int> OnStartNoti;
        public static event Action<CAPYBARA.badugi.HoleCardNoti,int> OnHoleCardNoti;
        public static event Action<CAPYBARA.badugi.HoleCardNotiOther,int> OnHoleCardNotiOther;
        public static event Action<CAPYBARA.badugi.TurnNoti,int> OnTurnNoti;
        public static event Action<CAPYBARA.badugi.ActionNoti,int> OnActionNoti;
        public static event Action<CAPYBARA.badugi.DrawNoti,int> OnDrawNoti;
        public static event Action<CAPYBARA.badugi.DrawTurnNoti,int> OnDrawTurnNoti;
        public static event Action<badugi.ResultNoti,int> OnResultNoti;
        public static event Action<badugi.KickVoteNoti,int> OnKickedNoti;
        public static event Action<badugi.EmoteNoti,int> OnEmoteNoti;
        public static event Action<badugi.CardOpenNoti,int> OnCardOpenNoti;
        
        public static event Action<badugi.ShowdownNoti,int> OnShowdownNoti;
        public static event Action<badugi.LeaveReservedNoti,int> OnLeaveReserveNoti;


        public static int revisionId=0;
        public void Init()
        {
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.EnterNoti, pkt =>
            {
                revisionId = 0;
                OnEnterNoti?.Invoke(pkt.EnterNoti,revisionId);
            });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.LeaveNoti, pkt =>
            {
                OnLeaveNoti?.Invoke(pkt.LeaveNoti,revisionId);
            });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.StartNoti, pkt => {  revisionId++;OnStartNoti?.Invoke(pkt.StartNoti,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.HoleCardNoti, pkt => {  revisionId++;OnHoleCardNoti?.Invoke(pkt.HoleCardNoti,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.HoleCardNotiOther, pkt => {  revisionId++;OnHoleCardNotiOther?.Invoke(pkt.HoleCardNotiOther,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.TurnNoti, pkt => {  revisionId++;OnTurnNoti?.Invoke(pkt.TurnNoti,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.ActionNoti, pkt => {  revisionId++;OnActionNoti?.Invoke(pkt.ActionNoti,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.DrawNoti, pkt => {  revisionId++;OnDrawNoti?.Invoke(pkt.DrawNoti,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.DrawTurnNoti, pkt => {  revisionId++;OnDrawTurnNoti?.Invoke(pkt.DrawTurnNoti,revisionId); });
            
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.ShowdownNoti, pkt => {  revisionId++;OnShowdownNoti?.Invoke(pkt.ShowdownNoti,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.ResultNoti, pkt => {  revisionId++;OnResultNoti?.Invoke(pkt.ResultNoti,revisionId); });
            
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.KickVoteNoti, pkt => {  revisionId++;OnKickedNoti?.Invoke(pkt.KickVoteNoti,revisionId); });
            
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.ReadyNoti, pkt => { revisionId++; OnReadyNoti?.Invoke(pkt.ReadyNoti,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.EmoteNoti, pkt => {  revisionId++;OnEmoteNoti?.Invoke(pkt.EmoteNoti,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.CardOpenNoti, pkt => {  revisionId++;OnCardOpenNoti?.Invoke(pkt.CardOpenNoti,revisionId); });
            Services.BadugiDispatcher.AddEvent((int)CAPYBARA.badugi.Packet.PayloadOneofCase.LeaveReservedNoti, pkt => {  revisionId++;OnLeaveReserveNoti?.Invoke(pkt.LeaveReservedNoti,revisionId); });
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
            OnDrawNoti = null;
            OnDrawTurnNoti = null;
            OnResultNoti = null;
            OnShowdownNoti = null;
            OnCardOpenNoti = null;
            revisionId = 0;
        }


        // Update is called once per frame
        void Update()
        {
            if (CPPlayer.Server.badugiConnection != null)
            {
                if (CPPlayer.Server.badugiConnection.isConnected)
                {
                    if (Services.BadugiDispatcher != null)
                    {
                        Services.BadugiDispatcher.Pump(64);    
                    }
                }
            }
      
        }
    }
}