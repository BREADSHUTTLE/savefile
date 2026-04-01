using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class AtozFindUserInfo : MonoBehaviour,IBackButtonSender
    {
        [SerializeField] private Button closeidListWindowBtn;
        [SerializeField] private GameObject userIdEmpty;
        [SerializeField] private GameObject userIdList;
        public UserIdAccountSlot userIdAccountSlot;
        public Transform userIdSlotParent;
        
        private List<UserIdAccountSlot> idAccountSlotList = new List<UserIdAccountSlot>();
        private void Awake()
        {
            closeidListWindowBtn.onClick.RemoveAllListeners();
            closeidListWindowBtn.onClick.AddListener(OnBackButtonPressed);
        }
        public void OpenWindow()
        {
            SceneLoadResources.OpenPopup(this);
            this.gameObject.SetActive(true);
        }
        public async UniTask FindUserIdProgress(Action<lobby.UserWithToken> changePwOpen)
        {
            var usersInfos = await Services.Lobby.GetUserListInfoAsync(LoginData.Cloud.loginValue.userAutoToken);
            
            if (usersInfos.IsSuccess)
            {
                if (usersInfos.Data.Users.Count > 0)
                {
                    foreach (var go in idAccountSlotList)
                    {
                        if (go != null)
                            GameObject.Destroy(go.gameObject);
                    }

                    idAccountSlotList.Clear();

                    foreach (var usersRe in usersInfos.Data.Users)
                    {
                        if(usersRe.IsActive==false)
                            continue;
                        
                        var obj = Instantiate(userIdAccountSlot);
                        obj.transform.SetParent(userIdSlotParent, false);
                        obj.Init(usersRe);
                        obj.changePWBtn.gameObject.SetActive(false);
                        idAccountSlotList.Add(obj);
                        if (usersRe.LoginType == "ATOZ")
                        {
                            obj.changePWBtn.gameObject.SetActive(true);
                            obj.changePWBtn.onClick.AddListener(() => { changePwOpen(usersRe); });
                        }
                    }

                    userIdList.SetActive(true);
                    userIdEmpty.SetActive(false);
                }
                else
                {
                    userIdList.SetActive(false);
                    userIdEmpty.SetActive(true);
                }
            }
        }
        
        public void OnBackButtonPressed()
        {
            SceneLoadResources.ClosePopup();
        }
        
        public void CloseThisWindow()
        {
            this.gameObject.SetActive(false);
        }
    }
}
