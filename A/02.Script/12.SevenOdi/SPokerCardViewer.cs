using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class SPokerCardViewer : CardViewer
    {
        public Image hideCardImage;

        public void SetCardHide(bool isHide)
        {
            if (isHide)
            {     
                hideCardImage.gameObject.SetActive(true);
                Color c =hideCardImage.color;
                c.a = 0f;
                hideCardImage.color = c;
                hideCardImage.DOFade(1.0f, 0.1f);
            }
            else
            {
                hideCardImage.gameObject.SetActive(false);
            }
        }

        public override void Inactive()
        {
            base.Inactive();
            hideCardImage.gameObject.SetActive(false);
        }

        private Sequence seq;
        public async UniTask SetCardImageResult(bool isAlreadyOpended,bool isme,bool isRanked)
        {
            var seq = DOTween.Sequence();

            float hidecardOpenTime =(float) CPPlayer.Server.visualEffectTimeConfig["RESULT_OPEN_MS"] / 1000f;

            // if (!isAlreadyOpended)
            // {
            //     seq.Append(hideCardImage.DOFade(0.0f, hidecardOpenTime));    
            // }
            seq.Append(hideCardImage.DOFade(0.0f, hidecardOpenTime));    
            
            if (isme == false)
            {
                var c = cardImage.color;
                c.a = 0f;
                cardImage.color = c;
            }
            seq.Join(cardImage.DOFade(1.0f, hidecardOpenTime));
            if (!isRanked)
            {
                mask.gameObject.SetActive(true);
                var c = mask.color;
                c.a = 0f;
                mask.color = c;
                seq.Join(mask.DOFade(0.8f, hidecardOpenTime));
            }
            else
            {
                mask.gameObject.SetActive(false);
            }
            
            await seq.AsyncWaitForCompletion();
            
        }
    }
}
