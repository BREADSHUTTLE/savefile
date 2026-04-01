using System.Linq;
using CAPYBARA.Bundles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class UserClassInfoWindow : MonoBehaviour
    {
        public CPButton cancelClass;
        public CPButton[] pointGold;
        public GameObject eventAvatarButton;
        public TMP_Text txtEndTime;
        public TMP_Text txtRemainingTime;

        private void OnEnable()
        {
            var eventInfo = CPPlayer.OutGame.eventList?.Where(x => x.EventCode.Contains("SHOP"));
            if (eventAvatarButton != null)
                eventAvatarButton.SetActive(eventInfo != null && eventInfo.ToList().Count > 0);

            if (CPPlayer.Inventory.classInfo != null)
            {
                if (txtRemainingTime != null)
                {
                    int nowTimestamp = (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    txtRemainingTime.text = Extension.ToRemainingTimeStringFull(nowTimestamp, CPPlayer.Inventory.classInfo.EffectEndAt);
                }

                if (txtEndTime != null)
                    txtEndTime.text = Extension.ToEndDateTimeString(CPPlayer.Inventory.classInfo.EffectEndAt);
            }
        }
    }
}
