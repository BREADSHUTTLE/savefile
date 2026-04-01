using System;
using CAPYBARA.Bundles;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class UserIdAccountSlot : MonoBehaviour
    {
        public Image icon;
        public TMP_Text userId;
        public CPButton changePWBtn;

        public void Init(lobby.UserWithToken userRes)
        {
            if (userRes.LoginType == "NAVER")
            {
                userId.text=userRes.Email;
            }
            else
            {
                userId.text=userRes.Id;    
            }
            

            var logintype= Extension.StringToEnum<LoginType>(userRes.LoginType);
            icon.sprite = InGameResourcesBundle.Loaded.loginTypeIconSprites[(int)logintype];
         

        }
    }
}
