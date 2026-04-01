using CAPYBARA.holdem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA 
{
    public class HoldemActionToggle : MonoBehaviour
    {
        public Toggle toggle;
        
        public Partial.BetSizeType ingameBettingActionType;
        public Partial.ActionType holdemActionType;

        public GameObject currentBetAmountObject;
        public TMP_Text currentBetAmount;

        public GameObject InactiveMask;

        public void ToggleActivate(long amount=-1,bool isOn=true)
        {
            //Extension.eLog($"toggle activate!!! amount:{amount}//ison:{isOn}",Color.cyan);
            this.gameObject.SetActive(true);
            toggle.interactable = isOn;
            InactiveMask.SetActive(!isOn);
            if (ingameBettingActionType ==Partial. BetSizeType.BsCheck || ingameBettingActionType == Partial.BetSizeType.BsFold)
            {
                currentBetAmountObject.gameObject.SetActive(false);
                return;
            }
            currentBetAmountObject.gameObject.SetActive(isOn);
            if (isOn)
            {
                if (amount < 0)
                {
                    //currentBetAmount.text= Extension.ToKoreanFormat(amount);
                }
                else
                {
                    currentBetAmount.text= Extension.ToKoreanFormat(amount);    
                }
                    
            }
        }

        public void ObjectActivate(bool active)
        {
            this.gameObject.SetActive(active);
        }
    }
}

