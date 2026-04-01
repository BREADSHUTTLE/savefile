using CAPYBARA.Definition;
using System;
using CAPYBARA.lobby;
using UnityEngine;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        public static class Chat
        {
            public static Action<ChatRoomList> ChatRoomClickEvent;

            public static void Dispose()
            {
                ChatRoomClickEvent = null;
            }
        }
    }
}
