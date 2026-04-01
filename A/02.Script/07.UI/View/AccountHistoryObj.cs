using System.Linq;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class AccountHistoryObj : MonoBehaviour
    {
        public string uid;
        public TMP_Text nick;
        public TMP_Text loginType;
        public TMP_Text mymoney;
        public TMP_Text gameHistory; 

        public GameObject arrow;

        public async UniTask Init(lobby.UserWithToken userInfo,bool isMe=false)
        {
            uid = userInfo.Id;
            
            nick.text = userInfo.Nick;
            loginType.text = GetLoginTypeKorean(userInfo.LoginType);
            mymoney.text = Extension.ToKoreanFormat(userInfo.Gold, Extension.KoreanFormatMode.Planning) + " " + StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Gold].StringToLocal;
            
            long userid=userInfo.Uid;
            var matchRecordPacket = await Services.Lobby.GetMatchRecordAsync(userid);

            if (!matchRecordPacket.IsSuccess)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ServerErrorWithReason].StringToLocal}{matchRecordPacket.Error}"));
                return;
            }

            int win = 0;
            int lose = 0;
            if (matchRecordPacket.Data?.MatchRecord != null)
            {
                foreach (var record in matchRecordPacket.Data.MatchRecord.Where(r => r.MatchStats == "TOTAL"))
                {
                    win += record.Win;
                    lose += record.Lose;
                }
            }
            int total = win + lose;
            float winRate = total == 0 ? 0f : (float)win / total * 100f;

            gameHistory.text = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.WinLoseRateRecord].StringToLocal, total, win, lose, winRate);
            
            
            arrow.SetActive(isMe);

            // TMPNeonEffect를 통해 글로우 효과 제어
            SetGlowEffect(nick, isMe);
            SetGlowEffect(loginType, isMe);
            SetGlowEffect(mymoney, isMe);
            SetGlowEffect(gameHistory, isMe);
            
            this.gameObject.SetActive(true);
        }

        private string GetLoginTypeKorean(string loginType)
        {
            if (string.IsNullOrEmpty(loginType))
                return "-";

            return loginType.ToUpper() switch
            {
                "GOOGLE" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Google].StringToLocal,
                "APPLE" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Apple].StringToLocal,
                "KAKAO" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Kakao].StringToLocal,
                "NAVER" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Naver].StringToLocal,
                "ATOZ" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Atoz].StringToLocal,
                "TEST" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.TestAccount].StringToLocal,
                _ => loginType
            };
        }
        
        private void SetGlowEffect(TMP_Text text, bool enableGlow)
        {
            if (text == null) return;
            
            var neonEffect = text.GetComponent<TMPNeonEffect>();
            if (neonEffect != null)
            {
                neonEffect.enabled = enableGlow;
                if (enableGlow)
                    neonEffect.Refresh();
            }
        }
    }
}
