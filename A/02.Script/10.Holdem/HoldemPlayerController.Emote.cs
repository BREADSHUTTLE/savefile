using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 이모티콘, 킥투표, AFK 예약 표시
    /// </summary>
    public partial class HoldemPlayerController
    {
        public void KickVoteRecieveEvent(int count)
        {
            view.kickvoteCanvasGroup.gameObject.SetActive(true);
            view.kickvoteCanvasGroup.alpha = 1;
            view.kickvoteText.text = count.ToString();

            KickVoteEventAnim().Forget();
        }

        private async UniTask KickVoteEventAnim()
        {
            int kickUiDissapearTime = (int)CPPlayer.Server.visualEffectTimeConfig["VOTE_SHOW_MS"];
            await UniTask.Delay(kickUiDissapearTime);
            view.kickvoteCanvasGroup.DOFade(0, 1.0f).OnComplete(() => { view.kickvoteCanvasGroup.gameObject.SetActive(false); });
        }

        public void ReserveOut(bool isReserveOut)
        {
            view.reservedOut.SetActive(isReserveOut);
        }

        public void EmoticonExpress(EmotionInfo emotionInfo)
        {
            view.emotionObj.SetActive(true);
            EmoticonExpressAsync(emotionInfo).Forget();
        }

        private async UniTaskVoid EmoticonExpressAsync(EmotionInfo emotionInfo)
        {
            EmoticonExpressCancel();
            view.emotionObj.SetActive(true);
            emotCts = new System.Threading.CancellationTokenSource();
            int index = 0;
            float frameInterval = 0.05f;
            try
            {
                while (true)
                {
                    view.emotionImage.sprite = emotionInfo.sprites[index];
                    await UniTask.Delay(TimeSpan.FromSeconds(frameInterval), cancellationToken: emotCts.Token);
                    index++;
                    if (index >= emotionInfo.sprites.Length)
                        break;
                }
                view.emotionObj.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.Log("이모티콘 강제 종료");
            }
        }

        private void EmoticonExpressCancel()
        {
            emotCts?.Cancel();
            emotCts?.Dispose();
            emotCts = null;

            view.emotionObj.SetActive(false);
        }
    }
}
