using System.Collections.Generic;
using UnityEngine;

namespace CAPYBARA.Core
{
    public interface IGameController
    {
        //플레이어 세팅
        void SetPlayersInfo();
        
        //enter table result
        //void EnterGameTable_Result(holdem.EnterRes enterRes);
        //leave table result
        //void LeaveGameOtherPlayer_Noti(holdem.LeaveNoti leaveNoti);
        //startgame Noti
        //void StartGame(holdem.StartNoti startNoti);
        
        //action toggle initial setting
        void ToggleViewInitialSetting();
        
    }
}
