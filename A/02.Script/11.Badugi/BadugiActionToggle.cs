using System;
using CAPYBARA.badugi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class BadugiActionToggle : MonoBehaviour
    {
        public Toggle toggle;
        
        public Common.BetSizeType ingameBettingActionType;
        public Common.ActionType badugiActionType;

        public GameObject currentBetAmountObject;
        public TMP_Text currentBetAmount;

        public GameObject InactiveMask;

        public void ToggleActivate(long amount,bool isOn)
        {
            this.gameObject.SetActive(true);
            toggle.interactable = isOn;
            InactiveMask.SetActive(!isOn);
            if (ingameBettingActionType ==Common.BetSizeType.BsCheck || ingameBettingActionType ==Common.BetSizeType.BsFold)
            {
                currentBetAmountObject.gameObject.SetActive(false);
                return;
            }
            currentBetAmountObject.gameObject.SetActive(isOn);
            if (isOn)
            {
                currentBetAmount.text= Extension.ToKoreanFormat(amount);    
            }
        }

        public void ObjectActivate(bool active)
        {
            this.gameObject.SetActive(active);
        }
    }
}
