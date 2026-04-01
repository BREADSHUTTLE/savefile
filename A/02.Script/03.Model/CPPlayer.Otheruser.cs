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
        public static class Otheruser
        {
            //다른 유저의 정보를 받기 위해 
            public static string currentOtherAccountId;
            
            public static Action<string> OpenOtherUserInfo;
            public static Action OpenDMWindow;


            public static void Dispose()
            {
                OpenOtherUserInfo = null;
                OpenDMWindow = null;
            }

            public static void Init()
            {
                
            }

            public static async UniTask SetUserInfo(string accountId)
            {
                await LoadUserMatchRecord(accountId);
            }

            private static async UniTask LoadUserMatchRecord(string accountid)
            {
               
            }
        }
    }
}
