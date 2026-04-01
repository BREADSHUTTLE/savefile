using CAPYBARA.Bundles;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class SimpleUserInfo
    {
        public string userid;
        public string userUid;
    }
    public class LoginAccountSlot : MonoBehaviour
    {
        public Image icon;
        public GameObject haveAccountObj;
        public TMP_Text userId;
        public CPButton loginBtn;
        public CPButton deleteBtn;
        
        public GameObject addAccountObj;
        public CPButton loginOtherId;

        public void Init(lobby.User userInfo,bool exist=true)
        {
            haveAccountObj.SetActive(exist);
            addAccountObj.SetActive(!exist);

            if (exist)
            {
                userId.text = userInfo.Id;
                var logintype= Extension.StringToEnum<LoginType>(userInfo.LoginType);
                icon.sprite = InGameResourcesBundle.Loaded.loginTypeIconSprites[(int)logintype];
            }
            
            
        }
    }

}
