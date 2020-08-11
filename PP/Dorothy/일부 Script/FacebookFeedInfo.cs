// FacebookFeedInfo.cs - FacebookFeedInfo implementation file
//
// Description      : FacebookFeedInfo main instance
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2020/06/19
// Last Update      : 2020/06/22
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dorothy.Network;
using NEXT.GAMES;

namespace Dorothy.DataPool
{
    public sealed class FacebookFeedInfo : BasePool<FacebookFeedInfo>
    {
        private string linkListenerCode = string.Empty;

        public string LinkListenerCode
        {
            get
            {
                return linkListenerCode;
            }
            set
            {
                string code = value;
                string temp = string.Empty;

                if (code.Contains("&target_url="))
                {
                    Debug.Log("contains true");
                    int index = code.IndexOf("&target_url=");
                    temp = code.Substring(0, index);
                }
                else
                {
                    Debug.Log("contains false");
                    temp = code;
                }

                Debug.Log("temp code : " + temp);

                linkListenerCode = temp;
            }
        }

        public enum FacebookFeedType
        {
            NONE,
            LEVEL_UP,
            FIVE_OF_KINDS,
            BIG_WIN,
            SUPER_WIN,
            MEGA_WIN,
            MINI_JACKPOT,
            MINOR_JACKPOT,
            MAXI_JACKPOT,
            MAJOR_JACKPOT,
            MEGA_JACKPOT,
            GRAND_JACKPOT,
            MYSTERY_BOX,
            MEGA_BONUS,
        }

        public FacebookFeedType GetFeedType(FacebookManager.SHARE_TYPE type)
        {
            switch (type)
            {
                case FacebookManager.SHARE_TYPE.LEVEL_UP:
                    return FacebookFeedType.LEVEL_UP;
                case FacebookManager.SHARE_TYPE.FIVE_OF_KINDS:
                    return FacebookFeedType.FIVE_OF_KINDS;
                case FacebookManager.SHARE_TYPE.BIG_WIN:
                    return FacebookFeedType.BIG_WIN;
                case FacebookManager.SHARE_TYPE.SUPER_WIN:
                    return FacebookFeedType.SUPER_WIN;
                case FacebookManager.SHARE_TYPE.MEGA_WIN:
                    return FacebookFeedType.MEGA_WIN;
                case FacebookManager.SHARE_TYPE.MINI_JACKPOT:
                    return FacebookFeedType.MINI_JACKPOT;
                case FacebookManager.SHARE_TYPE.MINOR_JACKPOT:
                    return FacebookFeedType.MINOR_JACKPOT;
                case FacebookManager.SHARE_TYPE.MAXI_JACKPOT:
                    return FacebookFeedType.MAXI_JACKPOT;
                case FacebookManager.SHARE_TYPE.MAJOR_JACKPOT:
                    return FacebookFeedType.MAJOR_JACKPOT;
                case FacebookManager.SHARE_TYPE.MEGA_JACKPOT:
                    return FacebookFeedType.MEGA_JACKPOT;
                case FacebookManager.SHARE_TYPE.GRAND_JACKPOT:
                    return FacebookFeedType.GRAND_JACKPOT;
                default:
                    return FacebookFeedType.NONE;
            }
        }
    }
}