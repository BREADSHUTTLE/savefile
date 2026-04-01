using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CAPYBARA
{
    public class AccountObjView : MonoBehaviour
    {
        public AccountHistoryObj accountHistoryObjprefab;

        [HideInInspector]
        public List<AccountHistoryObj> accountObjList = new List<AccountHistoryObj>();

        float elapsedtime;
        float resetTime;

        public void SetUserInfoToUI()
        {
            // foreach (var account in CAPYBARA.CPPlayer.UserInfo.accountsArr)
            // {
            //     if (accountObjList.Count < CAPYBARA.CPPlayer.UserInfo.accountsArr.Count)
            //     {
            //         var obj = Instantiate(accountHistoryObjprefab);
            //         obj.GetComponent<Transform>().SetParent(this.transform, false);
            //         accountObjList.Add(obj);
            //     }
            // }
            //
            // int index = 0;
            // foreach (var account in CAPYBARA.CPPlayer.UserInfo.accountsArr)
            // {
            //     if (accountObjList.Count > index)
            //     {
            //         if (CAPYBARA.CPPlayer.UserInfo.accountsArr[index].is_active)
            //         {
            //             accountObjList[index].nick.text = CAPYBARA.CPPlayer.UserInfo.accountsArr[index].nickname;
            //             accountObjList[index].mymoney.text = MoneyConverter.Instance.ConvertToKoreanWon(CAPYBARA.CPPlayer.UserInfo.accountsArr[index].userTotalBalance);
            //
            //             var isDeleteProgressed = string.IsNullOrEmpty(CPPlayer.UserInfo.accountsArr[index].withdrawal);
            //         }
            //         else
            //         {
            //             accountObjList[index].gameObject.SetActive(false);
            //         }
            //
            //     }
            //
            //     index++;
            // }

        }
    }

}
