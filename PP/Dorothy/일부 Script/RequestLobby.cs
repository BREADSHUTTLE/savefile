// RequestMyInfo.cs - RequestMyInfo implementation file
//
// Description      : RequestMyInfo
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       : 
// Created          : 2019/04/25
// Last Update      : 2019/08/14
// Known bugs       : 
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//

using System.Collections.Generic;

namespace Dorothy.Network
{
    public class RequestLobby : Request
    {
        public RequestLobby ReceiveWelcomeBonus()
        {
            return this;
        }

        public RequestLobby ReceiveFacebookConnectBonus()
        {
            return this;
        }

        public RequestLobby ReceiveRewardCoupon(string couponCode)
        {
            param = new Dictionary<string, object>()
            {
                {"couponCode", couponCode },
            };

            return this;
        }
    }
}