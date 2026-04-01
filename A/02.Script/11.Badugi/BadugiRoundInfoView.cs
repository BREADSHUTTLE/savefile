using CAPYBARA.Bundles;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    
    public class BadugiRoundInfoView : MonoBehaviour
    {
        public badugi.DrawPhase badugiPhase;
        public GameObject RoundImage;
        public TMP_Text numText;
        [SerializeField]private Animator numTextAnimator; 

        public void Init()
        {
            numText.gameObject.SetActive(false);
            RoundImage.gameObject.SetActive(true);
        }
        
        public void SetNum(int roundNum)
        {
            numText.gameObject.SetActive(true);
            RoundImage.gameObject.SetActive(false);
            int index = roundNum - 1;
            Extension.eLog($"교환갯수{roundNum}",Color.red);
            numText.text=roundNum.ToString();
            
            float startscale = (float)CPPlayer.Server.visualEffectTimeConfig["TEMP_1_MS"]/1000f;
            float endscale = (float)CPPlayer.Server.visualEffectTimeConfig["TEMP_2_MS"]/1000f;
            float time = (float)CPPlayer.Server.visualEffectTimeConfig["TEMP_3_MS"]/1000f;

            numTextAnimator.Play("Number_animation");
            // numText.transform.DOKill();
            // numText.transform.localScale = new Vector2(startscale,startscale);
            // numText.transform.DOScale(endscale, time)
            //     .SetEase(Ease.OutBack)
            //     .OnComplete(() =>
            //     {
            //         
            //     });
        }
    }

}
