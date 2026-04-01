using CAPYBARA.Definition;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        public static class Server
        {
            public static ProtoConnection<lobby.Packet> lobbyConnection;
            public static ProtoConnection<holdem.Packet> holdemConnection;
            public static ProtoConnection<badugi.Packet> badugiConnection;
            public static ProtoConnection<sevenPoker.Packet> sevenPokerConnection;

            public static Action CallbackAfterHoldemConnect;
            public static Action CallbackAfterBadugiConnect;
            public static Action CallbackAfterSPokerConnect;
            
            public static long _resumeUntilMs = 0;
            public static bool _waitingFirstPacketAfterResume = false;
            public static bool _loadingShown = false;

            public static long NowMs => Environment.TickCount;
            
            public static Dictionary<string, double> visualEffectTimeConfig;

            public static GameType currentConnectedGameType;

            public static void Init()
            {
                visualEffectTimeConfig = new Dictionary<string, double>();
            }

            public static void Dispose()
            {
                CallbackAfterHoldemConnect = null;
                CallbackAfterBadugiConnect = null;
                CallbackAfterSPokerConnect = null;
                visualEffectTimeConfig.Clear();
            }
        } 
    }

}
