using System;
using System.Collections.Generic;
using CAPYBARA.lobby;
using UnityEngine;
using CAPYBARA.Model;


namespace CAPYBARA.Model
{
    public class LoginCloudData
    {
        public LoginValue loginValue = new LoginValue();


        [System.Serializable]
        public class UserSavedInfo
        {
            public LoginType logintype;
            public string accountID;
            public string userLoginToken;
            public long uid;
            public string userNickName;
        }
        [System.Serializable]
        public class UidListWrapper
        {
            public List<UserSavedInfo> list = new List<UserSavedInfo>();
        }
        [Serializable]
        public class LoginValue : UserDataBase
        {
            public LoginRes loginres;
            //atozlogin info
            public string userAccountID = null;
            public string userAccountPw = null;
            public long UID;
            public string jwtToken;
            //atozlogin info
            
            //social login
            public string userSocialToken = null;
            public string userSocialEmail = null;
            public string accessToken = null;

            //common use
            public LoginType loginType = LoginType.None;
            
            //register token
            public string registerToken = null;
            //token for login or findUser
            public string userAutoToken = null;
            
            public UidListWrapper uidList=new UidListWrapper();

            public string nickName=null;
            public bool isFirstLogin = true;
            
            //잠시 유저 본인인증 여부 확인하는거 담아둘거(삭제하면 초기화됨,서버 API로직 완성되면 대체되고 이 변수는 사라짐
            public bool isAlreadyIdentified = false;
        }
    }
}