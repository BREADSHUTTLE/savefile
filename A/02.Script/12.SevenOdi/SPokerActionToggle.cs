using CAPYBARA.sevenPoker;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA 
{
    public class SPokerActionToggle : MonoBehaviour
    {
        public Toggle toggle;
        
        public Common.BetSizeType ingameBettingActionType;
        public Common.ActionType spokerActionType;

        public GameObject currentBetAmountObject;
        public TMP_Text currentBetAmount;

        public GameObject InactiveMask;

        public void ToggleActivate(long amount=-1,bool isOn=true)
        {
            this.gameObject.SetActive(true);
            toggle.interactable = isOn;
            InactiveMask.SetActive(!isOn);
            if (ingameBettingActionType ==Common. BetSizeType.BsCheck || ingameBettingActionType == Common.BetSizeType.BsFold)
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
