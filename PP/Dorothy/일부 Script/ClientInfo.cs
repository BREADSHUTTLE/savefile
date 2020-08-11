// ClientInfo.cs - ClientInfo implementation file
//
// Description      : ClientInfo
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/06/03
// Last Update      : 2019/06/03
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO. All rights reserved.
//

using UnityEngine;
using UnityEngine.Networking;
using System;


namespace Dorothy.DataPool
{
    public sealed class ClientInfo : BasePool<ClientInfo>
    {
        private TimeSpan serverTime = new TimeSpan();

        public string PlatformType
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.Android:
                        return "a";
                    case RuntimePlatform.IPhonePlayer:
                        return "i";
                    case RuntimePlatform.WSAPlayerARM:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WindowsPlayer:
                        //return "w"; // CHECK:: 현재 PC 빌드에 문제가 생겨서 a 로 고정합니다. (플랫폼이 android로 통일되어 있음.) - api 호출 안됨
                    // CHECK:: mac 사용자가 있어서 추가 합니다. - 주희
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        return "a";
                    default:
                        return string.Empty;
                }
            }
        }

        public string ServiceType
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.Android:
                        return "g";
                    case RuntimePlatform.IPhonePlayer:
                        return "a";
                    case RuntimePlatform.WSAPlayerARM:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WindowsPlayer:
                        //return "w";
                        return "g"; // CHECK:: 현재 PC 빌드에 문제가 생겨서 g 로 고정합니다. (store가 google 로 통일되어 있음.) - api 호출 안됨
                    // CHECK:: mac 사용자가 있어서 추가 합니다. - 주희
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                        return "a";
                    default:
                        return string.Empty;
                }
            }
        }

        public string Locale
        {
            get
            {
                return LocalizationSystem.Instance.Localize("Language.Code");
            }
        }

        public string DeviceInfo
        {
            get { return SystemInfo.deviceModel; }
        }

        public string DeviceID
        {
            get { return SystemInfo.deviceUniqueIdentifier; }
        }

        public int Language
        {
            get
            {
                int language = 2;
                int.TryParse(LocalizationSystem.Instance.Localize("Language.Number"), out language);
                return language;
            }
        }

        public DateTime ServerTime
        {
            get { return DateTime.Now.Add(this.serverTime); }
            set { this.serverTime = value - DateTime.Now; }
        }

        public string AccessToken
        {
            get; set;
        }

        public string ExternalIP
        {
            get { return NetworkManager.singleton.networkAddress; }
        }
    }
}
