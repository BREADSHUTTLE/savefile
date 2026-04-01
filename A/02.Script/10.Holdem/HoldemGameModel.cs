using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public enum CardUISetState
    {
        GetPreflopCard,
        GetCommunityCard,
        AllCardOpen,
        CardNotOpen,
        AfterResult,
        TurnNotiChanged,
    }
    public class HoldemGameModel
    {
        // 1. 데이터: 방 정보, 팟 금액, 커뮤니티 카드 데이터
        public holdem.EnterRes CurrentRoomInfo { get; private set; }
        public long CurrentPotAmount { get; private set; }
        private List<string> CommunityCards = new List<string>();
        
        // 2. 이벤트: 데이터가 바뀌면 알려줄 Action
        public System.Action<long> OnPotChanged;

        public void Init(holdem.EnterRes roomInfo)
        {
            CurrentRoomInfo = roomInfo;
            CurrentPotAmount = 0;
            CommunityCards.Clear();
        }
        
        public void UpdatePot(long amount)
        {
            CurrentPotAmount = amount;
            OnPotChanged?.Invoke(CurrentPotAmount); // 구독자들에게 알림
        }
    }

}
