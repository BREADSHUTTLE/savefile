using System;
using System.Collections.Generic;
using CAPYBARA.Core;
using UnityEngine;
using static CAPYBARA.Model.UserCloudData;


namespace CAPYBARA.Model
{
    [Serializable]
    public class UserCloudData
    {
        public OptionValue optionValue;
        public PinChatUserinfo pinChatUserinfo;

        public UserCloudData()
        {
            optionValue = new OptionValue();
            pinChatUserinfo=new PinChatUserinfo();
        }
        [Serializable]
        public class OptionValue : UserDataBase
        {
            // 게임
            public bool reserveBet = true;
            public bool myTurnViberate = true;
            public bool fourColor = true;
            public bool useEmoji = true;
            
            // 채팅
            public bool chatVerticalMode = false;
            
            // 사운드
            public bool allSoundOnOff = true;
            public float bgmVolum = 1.0f;
            public float effectVolum = 1.0f;
            public float voiceVolum = 1.0f;

            // (레거시 - 사용안함)
            public bool alarmAchivement = true;
            public bool alarmDM = true;
            public float allSound = 100.0f;
            public bool bgmSoundOnOff = true;
            public bool effectSoundOnOff = true;
            public bool voiceSoundOnOff = true;
            public bool jokboInform = true;
            public bool handRankInform = true;
            
            public string lastSaveTime;
            public int lastSaveDay;
        }

        [Serializable]
        public class PinChatRoomInfo
        {
            public long roomId;
            public int pinned_at;
        }

        [Serializable]
        public class PinChatUserinfo:UserDataBase
        {
            public List<PinChatRoomInfo> pinnedInfo = new List<PinChatRoomInfo>();
        }

       
    }

    [Serializable]
    public class IPPortData
    {
        public IPPortInfodatas ipportinfos;
        
        public IPPortData()
        {
            ipportinfos = new IPPortInfodatas();
        }
        [Serializable]
        public class IPPortInfo
        {
            public string ip;
            public int port;
        }
        [Serializable]
        public class IPPortInfodatas : UserDataBase
        {
            public List<IPPortInfo> infos = new List<IPPortInfo>();
            public IPPortInfo LobbyInfo;
            public IPPortInfo HoldemInfo;
            public IPPortInfo BadugiInfo;
            public IPPortInfo SevenPokerInfo;
        }
    }
    public abstract class UserDataBase
    {
        private int _prevHash;
        public bool IsDirty { get; private set; }

        public UserDataBase UpdateHash()
        {
            var data = GetDataString();
            _prevHash = data.GetHashCode();
            return this;
        }
        public bool IsValidHash()
        {
            var data = GetDataString();
            return data.GetHashCode() == _prevHash;
        }
        public UserDataBase SetDirty(bool flag)
        {
            IsDirty = flag;
            return this;
        }
        public string GetDataString()
        {
            var res = JsonUtility.ToJson(this);
            return res;
        }
    }


}
