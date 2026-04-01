using System;
using CAPYBARA.Bundles;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class InGameActionToggle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Toggle toggle;
        public GameObject parentObj;

        [SerializeField] private float pressedScale = 0.9f;
        [SerializeField] private float pressDuration = 0.1f;
        [SerializeField] private float releaseDuration = 0.1f;
        [SerializeField] private Color pressedColor = Color.white;
        public TMP_Text betTypeText;
        public TMP_Text betInActiveText;
        
        public Partial.BetSizeType ingameBettingActionType;
        public Partial.ActionType ingameActionType;

        public GameObject currentBetAmountObject;
        public TMP_Text currentBetAmount;

        public GameObject InactiveMask;

        void Awake()
        {
            toggle.onValueChanged.AddListener(ison =>
            {
                if (ison)
                {
                    betTypeText.colorGradient = new VertexGradient(
                        InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].reservedColor.topColor,                 
                        InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].reservedColor.topColor,
                        InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].reservedColor.bottomColor,                                        
                        InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].reservedColor.bottomColor
                    );     
                }
                else
                {
                    betTypeText.colorGradient = new VertexGradient(
                        InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.topColor,                 
                        InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.topColor,
                        InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.bottomColor,                                        
                        InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.bottomColor
                    );     
                }
            });
            betTypeText.colorGradient = new VertexGradient(
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.topColor,                 
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.topColor,
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.bottomColor,                                        
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.bottomColor
            );   
            betInActiveText.colorGradient = new VertexGradient(
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].inactiveColor.topColor,                 
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].inactiveColor.topColor,
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].inactiveColor.bottomColor,                                        
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].inactiveColor.bottomColor
            );     
        }

        public void ToggleActivate(long amount,bool isOn)
        {
            if (isOn == false)
            {
//                Extension.eLog($"{ingameActionType.ToString()}..{isOn}",Color.red);
            }
            this.gameObject.SetActive(true);
            // var prevTransition = toggle.transition;
            // toggle.transition = Selectable.Transition.None;
            // toggle.interactable = isOn;
            // toggle.transition = prevTransition;
            toggle.enabled = isOn;
            InactiveMask.SetActive(!isOn);
           
            if (ingameBettingActionType ==Partial.BetSizeType.BsCheck || ingameBettingActionType ==Partial. BetSizeType.BsFold)
            {
                currentBetAmountObject.gameObject.SetActive(false);
                return;
            }
            currentBetAmountObject.gameObject.SetActive(isOn);
            if (isOn)
            {
                currentBetAmount.text= Extension.ToKoreanFormat(amount);    
            }
            
            if (isOn)
            {
                betTypeText.colorGradient = new VertexGradient(
                    InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.topColor,                 
                    InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.topColor,
                    InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.bottomColor,                                        
                    InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.bottomColor
                );     
            }
            else
            {
                betTypeText.colorGradient = new VertexGradient(
                    InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].inactiveColor.topColor,                 
                    InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].inactiveColor.topColor,
                    InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].inactiveColor.bottomColor,                                        
                    InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].inactiveColor.bottomColor
                );     
            }
        }

        public void TextColorToDefault()
        {
            betTypeText.colorGradient = new VertexGradient(
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.topColor,                 
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.topColor,
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.bottomColor,                                        
                InGameResourcesBundle.Loaded.actionButtonTextColorInfo[(int)ingameBettingActionType].defaultColor.bottomColor
            );     
        }
        

        public void ObjectActivate(bool active)
        {
            this.gameObject.SetActive(active);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (InactiveMask.activeInHierarchy)
                return;
            parentObj.transform.DOScale(pressedScale, pressDuration).SetUpdate(true);
            betTypeText.DOColor(pressedColor, pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (InactiveMask.activeInHierarchy)
                return;
            parentObj.transform.DOScale(1f, releaseDuration).SetUpdate(true);
            betTypeText.DOColor( Color.white, releaseDuration);
        }
    }
   
}
