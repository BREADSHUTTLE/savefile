using System;
using CAPYBARA.badugi;
using CAPYBARA.holdem;
using CAPYBARA.lobby;
using CAPYBARA.sevenPoker;
using CPAYBARA;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA 
{
    public static class Services
    {
        public static LobbyClient Lobby { get; set; }
        public static HoldemClient Holdem { get; set; }
        public static BadugiClient Badugi { get; set; }
        public static SPokerClient SevenPoker { get; set; }
        public static PushDispatcher<holdem.Packet> HoldemDispatcher { get; set; }
        public static PushDispatcher<lobby.Packet> LobbyDispatcher { get; set; }
        public static PushDispatcher<badugi.Packet> BadugiDispatcher { get; set; }
        public static PushDispatcher<sevenPoker.Packet> SevenPokerDispatcher { get; set; }
    }
    public static class ProtoBootStrap
    {
        [Header("Lobby TCP")]
        public static string LobbyHost = "lobby.dev.atozgames.net";
        public static int LobbyPort = 1111;
        public static string HoldemHost = "lobby.dev.atozgames.net";
        public static int HoldemPort = 1111;
        public static string BadugiHost = "lobby.dev.atozgames.net";
        public static int BadugiPort = 1111;
        public static uint ProtocolVersion = 1;
        

        public static async UniTask InitLobby(string host, int port)
        {
            if (CPPlayer.Server.lobbyConnection == null)
            {
                CPPlayer.Server.lobbyConnection=new ProtoConnection<lobby.Packet>(host, port,CAPYBARA.lobby.Packet.Parser,
                    pkt=>pkt.Base?.TxId??"", "Lobby");    
            }

            if (CPPlayer.Server.lobbyConnection.isConnected == false)
            {
                await CPPlayer.Server.lobbyConnection.ConnectAsync();    
            }
            

            if (Services.Lobby == null)
            {
                var _lobby = new LobbyClient(CPPlayer.Server.lobbyConnection, ProtocolVersion);
                Services.Lobby=_lobby;
            }

            if (Services.LobbyDispatcher == null)
            {
                var lobbyDispatcher=new PushDispatcher<lobby.Packet>(CPPlayer.Server.lobbyConnection, pkt=>(int)pkt.PayloadCase);   
                Services.LobbyDispatcher = lobbyDispatcher;
            }
            
            Extension.eLog("lobby connected",Color.cyan);
        }
     

        public static async UniTask InitHoldem(string host, int port)
        {
            CPPlayer.Server.holdemConnection=new ProtoConnection<holdem.Packet>(host,port,CAPYBARA.holdem.Packet.Parser,
                 pkt=>pkt.Base?.TxId??"", "holdem");
            
             await  CPPlayer.Server.holdemConnection.ConnectAsync();
             var _holdem=new HoldemClient( CPPlayer.Server.holdemConnection, ProtocolVersion);
            
             var holdemDispatcher=new PushDispatcher<holdem.Packet>( CPPlayer.Server.holdemConnection, pkt=>(int)pkt.PayloadCase);
            
            Services.Holdem=_holdem;
            Services.HoldemDispatcher=holdemDispatcher;

            Extension.eLog("holdem connected",Color.cyan);
        }
        
        public static async UniTask InitBadugi(string host, int port)
        {
            CPPlayer.Server.badugiConnection=new ProtoConnection<badugi.Packet>(host,port,CAPYBARA.badugi.Packet.Parser,
                pkt=>pkt.Base?.TxId??"", "badugi");
            
            await CPPlayer.Server.badugiConnection.ConnectAsync();
            var _badugi=new BadugiClient(CPPlayer.Server.badugiConnection, ProtocolVersion);
            
            var badugiDispatcher=new PushDispatcher<badugi.Packet>(CPPlayer.Server.badugiConnection, pkt=>(int)pkt.PayloadCase);
            
            Services.Badugi=_badugi;
            Services.BadugiDispatcher=badugiDispatcher;

            Extension.eLog("badugi connected",Color.cyan);
        }
        
        public static async UniTask InitSPoker(string host, int port)
        {
            CPPlayer.Server.sevenPokerConnection=new ProtoConnection<sevenPoker.Packet>(host,port,CAPYBARA.sevenPoker.Packet.Parser,
                pkt=>pkt.Base?.TxId??"", "sevenpoker");
            
            await CPPlayer.Server.sevenPokerConnection.ConnectAsync();
            var _sp=new SPokerClient(CPPlayer.Server.sevenPokerConnection, ProtocolVersion);
            
            var badugiDispatcher=new PushDispatcher<sevenPoker.Packet>(CPPlayer.Server.sevenPokerConnection, pkt=>(int)pkt.PayloadCase);
            
            Services.SevenPoker=_sp;
            Services.SevenPokerDispatcher=badugiDispatcher;

            Extension.eLog("badugi connected",Color.cyan);
        }
        
        
    }

}
