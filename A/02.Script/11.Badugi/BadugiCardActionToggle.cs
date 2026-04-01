using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class BadugiCardActionToggle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Toggle toggle;

        [SerializeField] private float pressedScale = 0.95f;
        [SerializeField] private float pressDuration = 0.15f;
        [SerializeField] private float releaseDuration = 0.25f;
        [SerializeField] private Color pressedColor = Color.white;
        [SerializeField] private TMP_Text btnText;
        public bool isDiscard;
        
        public GameObject inactivemask;
       
        public void OnPointerDown(PointerEventData eventData)
        {
            if (inactivemask.activeInHierarchy)
                return;
            toggle.transform.DOScale(pressedScale, pressDuration).SetUpdate(true);
            //btnText.DOColor(pressedColor, pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (inactivemask.activeInHierarchy)
                return;
            toggle.transform.DOScale(1f, releaseDuration).SetUpdate(true);
            //btnText.DOColor( Color.white, releaseDuration);
        }
    }
}
