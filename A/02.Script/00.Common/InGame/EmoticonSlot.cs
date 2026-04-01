using System.Linq;
using CAPYBARA.Bundles;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class EmoticonSlot : MonoBehaviour
    {
        public Image thumbnail;
        private EmotionInfo emotionInfo;
        public CPButton expressButton;

        public void SetSlot(EmotionInfo _emotionInfo)
        {
            emotionInfo = _emotionInfo;
            thumbnail.sprite = emotionInfo.thumbnail;
            expressButton.onClick.AddListener(ExpressEmotion);
        }

        void ExpressEmotion()
        {

            CPPlayer.InGame.emotionExpressEvent?.Invoke(CPPlayer.InGame.currentGameType,emotionInfo);
        }
    }
}