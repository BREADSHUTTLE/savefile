using CAPYBARA.Definition;
using System;
using Google.Apis.Sheets.v4;
using UnityEngine;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        public static class Badugi
        {
            public static long ingameUid;
            public static long initialBuyIn;
            /// 현재 참가된 테이블 id값
            public static int currentTableId;
            //서버에서 정해준 chairid값과 실제 자리에 앉힌(ingame속 viewer로 보이는)index간에 차이
            public static int gapBetweenChairIdAndIndex;
            public static BadugiState currentBadugiState;

            public static Action<CAPYBARA.badugi.EnterRes> EnterRoom;
            public static Action<int,long, Transform> ThrowAnte;
            //chip 건낼시 줄시 콜백
            public static Action<int,long, Transform> ThrowChip;

            //카드 받은뒤 랭크 받기 위해 스냅샷에 전달
            public static Func<BadugiPlayerController,bool,string> CardRecieved;

            public static Action<int,bool> CardTouchCallback;
            public static Action CardTouchCallback2;
            
            public static DateTime serverTime;
            public static TimeSpan timeGap;
            
            public static int twoVSOpponentViewIndex;

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
                ThrowChip = null;
                CardRecieved = null;
                CardTouchCallback = null;
                CardTouchCallback2 = null;
            }
        }
    }
}
