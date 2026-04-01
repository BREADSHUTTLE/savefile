using CodeStage.AntiCheat.ObscuredTypes;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {

        public static class UserInfo
        {
            //나중에 추방할때이걸로 쓰게 잠시 남겨둠
            public static Action<string> textToastPopupActive;
            public static int kickCount;
            public static List<float> kickElapsedTimerList;
            public static int canKickVoteCount;
            public static int gameCountPerKick;

            
            public static bool hasBooster=false;
            
            // 광고 횟수
            public static int watchAdCount = 0;
            
            /////////////////////////////////새로운 로직에 필요한 정보들
            public static CAPYBARA.lobby.UserRes userDatabase;
            public static List< CAPYBARA.lobby.UserWithToken> userDatabaseList;
            public static lobby.MemberRes memberDatabase;
            public static lobby.PurchaseMonthlyRes purchaseMonthlyDatabase;
            
            //temp(서버 작업이 안되어 임시로 쓰는 변수)
            public static ClassGrade ClassGrade = 0;
            
            
            public static void Init()
            {
                kickElapsedTimerList = new List<float>();
            }

            public static void Dispose()
            {
                textToastPopupActive = null;
                kickElapsedTimerList = null;
                ClassGrade = 0;
                userDatabaseList = null;
                watchAdCount = 0;
            }
        }
    }
}
