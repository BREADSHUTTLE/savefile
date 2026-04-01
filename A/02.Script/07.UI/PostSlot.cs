using System;
using UnityEngine;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using UnityEngine.UI;
using TMPro;

namespace CAPYBARA
{
    public class PostSlot : MonoBehaviour
    {
        public int id;
        public long uid;
        public long amount;
        public string message;
        public ItemID itemID;
        public int type;
        public int state;
        public lobby.Posts postsInfo;
        public Action<PostSlot> onReceive;
        
        /// <summary>
        /// RecycleScrollView에서 사용하는 데이터 인덱스
        /// </summary>
        [HideInInspector] public int dataIndex;


        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text leftTimeText;
        [SerializeField] private CPButton reciveButton;


        public void Init()
        {
            iconImage?.gameObject.SetActive(false);     // 나중에 혹시나 icon 추가될수도 있어서 일단 넣어둠

            if (messageText != null)
                messageText.text = $"{message}{Extension.ToKoreanFormat(amount)} {StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Gold].StringToLocal}";
            messageText?.gameObject.SetActive(true);

            if (leftTimeText != null)
                leftTimeText.text = GetRemainTimeText(postsInfo.LimitedAt);

            leftTimeText?.gameObject.SetActive(true);

            reciveButton?.gameObject.SetActive(true);
            if (reciveButton != null)
            {
                reciveButton.onClick.RemoveAllListeners();
                reciveButton.onClick.AddListener(() => onReceive?.Invoke(this));
            }
        }
        
        private string GetRemainTimeText(int limitedAt)
        {
            if (limitedAt == 0)
                return "";
            
            int currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int remainingSeconds = limitedAt - currentTimestamp;
            
            if (remainingSeconds <= 0)
                return StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PeriodExpired].StringToLocal;
            
            int remainDays = remainingSeconds / 86400;
            int remainHours = (remainingSeconds % 86400) / 3600;
            int remainMinutes = (remainingSeconds % 3600) / 60;
            
            if (remainDays > 0)
                return $"{remainDays}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.DaysRemaining].StringToLocal}";
            else if (remainHours > 0)
                return $"{remainHours}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.HoursRemaining].StringToLocal}";
            else
                return remainMinutes > 0 ? $"{remainMinutes}분 남음" : StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ExpiringSoon].StringToLocal;
        }
    }
}
