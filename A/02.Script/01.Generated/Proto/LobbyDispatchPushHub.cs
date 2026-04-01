using System;
using UnityEngine;

namespace CAPYBARA
{
    public class LobbyDispatchPushHub : MonoBehaviour
    {
        public static event Action<CAPYBARA.lobby.PostsNoti>  onPostsNoti;
        public static event Action<CAPYBARA.lobby.FriendsNoti>  onFriendsNoti;
        public static event Action<CAPYBARA.lobby.MessageNoti>  onMessageNoti;
        public static event Action<CAPYBARA.lobby.KickNoti>  onKickNoti;
        public static event Action<CAPYBARA.lobby.MaintenanceNoti>  onMaintenanceNoti;
        public void Init()
        {
            Services.LobbyDispatcher.AddEvent((int)CAPYBARA.lobby.Packet.PayloadOneofCase.PostsNoti, pkt => { onPostsNoti?.Invoke(pkt.PostsNoti); });
            Services.LobbyDispatcher.AddEvent((int)CAPYBARA.lobby.Packet.PayloadOneofCase.FriendsNoti, pkt => { onFriendsNoti?.Invoke(pkt.FriendsNoti); });
            Services.LobbyDispatcher.AddEvent((int)CAPYBARA.lobby.Packet.PayloadOneofCase.MessageNoti, pkt => { onMessageNoti?.Invoke(pkt.MessageNoti); });
            Services.LobbyDispatcher.AddEvent((int)CAPYBARA.lobby.Packet.PayloadOneofCase.KickNoti, pkt => { onKickNoti?.Invoke(pkt.KickNoti); });
            Services.LobbyDispatcher.AddEvent((int)CAPYBARA.lobby.Packet.PayloadOneofCase.MaintenanceNoti, pkt => { onMaintenanceNoti?.Invoke(pkt.MaintenanceNoti); });

        }

        public void Dispose()
        {
            onPostsNoti = null;
            onFriendsNoti = null;
            onMessageNoti = null;
            onKickNoti = null;
            onMaintenanceNoti = null;
        }

        // Update is called once per frame
        void Update()
        {
            if (CPPlayer.Server.lobbyConnection != null)
            {
                if (CPPlayer.Server.lobbyConnection.isConnected)
                {
                    Services.LobbyDispatcher?.Pump(int.MaxValue);    
                }
            }
            
        }
    }

}
