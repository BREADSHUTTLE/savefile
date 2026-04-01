using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CAPYBARA
{
    public class AccountObjController
    {
        AccountObjView myView;

        public AccountObjController(AccountObjView view)
        {
            myView = view;
            

            //CPPlayer.UserInfo.ProfileTouched += () => { SetUserInfo(GameManager.Instance.userId).Forget(); };
        }
        public async UniTaskVoid SetUserInfo(string _userid)
        {
            // var result = await CAPYBARA.API.CAPI.Account.GetUserInfo(_userid);
            // CPPlayer.UserInfo.user_id = _userid;
            // CPPlayer.UserInfo.SetUserInfo(result.account);

            // int index = 0;
            // foreach (var account in CAPYBARA.CPPlayer.UserInfo.accountsArr)
            // {
            //     var balanceinfo = await API.CAPI.Economy.GetUserBalance(CAPYBARA.CPPlayer.UserInfo.accountsArr[index].account_id);
            //
            //     if (balanceinfo != null)
            //         CPPlayer.UserInfo.SetUserBalance(CAPYBARA.CPPlayer.UserInfo.accountsArr[index].account_id, balanceinfo);
            //
            //     var userHistory = await API.CAPI.Record.GetUserRecordToday(CAPYBARA.CPPlayer.UserInfo.accountsArr[index].account_id);
            //
            //     if (userHistory != null)
            //         CPPlayer.UserInfo.SetUserHistory(CAPYBARA.CPPlayer.UserInfo.accountsArr[index].account_id, userHistory);
            //
            //     index++;
            // }

            myView.SetUserInfoToUI();
        }
    }
}
