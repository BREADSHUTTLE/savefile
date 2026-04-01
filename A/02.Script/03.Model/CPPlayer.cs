using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using CAPYBARA.Model;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        private static UserCloudData _cloudLoadData;
        public static UserCloudData Cloud
        {
            get
            {
                if (_cloudLoadData == null)
                    _cloudLoadData = CPPlayer.New();

                return _cloudLoadData;
            }
            set
            {
                _cloudLoadData = value;
            }
        }
        
        private static IPPortData _cloudIPData;
        public static IPPortData IpPortData
        {
            get
            {
                if (_cloudIPData == null)
                    _cloudIPData = new IPPortData();

                return _cloudIPData;
            }
            set
            {
                _cloudIPData = value;
            }
        }

        public static UserCloudData New()
        {
            var userCloudData = new UserCloudData();
            return userCloudData;
        }

        public static void Dispose()
        {
            Badugi.Dispose();
            Balance.Dispose();
            Chat.Dispose();
            Holdem.Dispose();
            InGame.Dispose();
            Inventory.Dispose();
            Noti.Dispose();
            Option.Dispose();
            Otheruser.Dispose();
            OutGame.Dispose();
            UserInfo.Dispose();
            Server.Dispose();
            SPoker.Dispose();
            
        }

    }
}

