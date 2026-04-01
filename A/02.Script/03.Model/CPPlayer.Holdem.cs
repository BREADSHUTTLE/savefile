using CAPYBARA.Definition;
using System;
using UnityEngine;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        public static class Holdem
        {
            /// holdem서버 연결시 받는 홀덤서버 고유 id값
            public static long ingameUid;
            /// 방입장시 시작 ante값
            public static long initialBuyIn;
            //서버에서 정해준 chairid값과 실제 자리에 앉힌(ingame속 viewer로 보이는)index간에 차이
            public static int gapBetweenChairIdAndIndex;

            public static int currentTableId=0;
            
            
            //방 입장시 콜백
            public static Action<holdem.EnterRes> EnterRoom;

            //ante 줄시 콜백
            public static Action<int,long, Transform> ThrowAnte;
            //chip 건낼시 줄시 콜백

            //카드 받은뒤 랭크 받기 위해 스냅샷에 전달
            public static Func<HoldemPlayerController,bool,string> CardRecieved;

            public static DateTime serverTime;
            public static TimeSpan timeGap;

            public static DateTime estimatedServerNowUtc
            {
                get
                {
                    return DateTime.UtcNow - timeGap;
                }
            }

            public static void Dispose()
            {
                EnterRoom = null;
                ThrowAnte = null;
                CardRecieved = null;
            }
         
        }

    }
}