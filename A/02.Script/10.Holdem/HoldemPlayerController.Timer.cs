using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 턴 타이머 및 액션 스탬프 표시
    /// </summary>
    public partial class HoldemPlayerController
    {
        /// <summary>
        /// 턴 활성화 - 타이머 시작
        /// </summary>
        public void ActivateTurn(DateTime startTIme, bool isMe)
        {
            view.SetActivateView(isMe, true);
            if (isMe)
            {
                holdemViewer.timeSlider.fillAmount = 1;
                holdemViewer.timeSliderObj.SetActive(true);
            }

            isTurnActive = true;
            RunTimerAsync(startTIme).Forget();
        }

        private async UniTaskVoid RunTimerAsync(DateTime startTime)
        {
            timeCts = new System.Threading.CancellationTokenSource();
            Debug.Log("타이머 시작!");

            bool audioToggle = false;
            int prevRemainSec = -1;
            float speed = 100f;
            float betTimeOut = (float)CPPlayer.Server.visualEffectTimeConfig["BET_TIMEOUT_MS"] / 1000f;
            try
            {
                float elapsedTime = 0f;
                while (elapsedTime < betTimeOut)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, timeCts.Token);
                    elapsedTime = (float)(CPPlayer.Holdem.estimatedServerNowUtc - startTime).TotalSeconds;
                    float remaining = betTimeOut - elapsedTime;

                    holdemViewer.timeSlider.fillAmount = remaining / betTimeOut;
                    view.turnActiveImage.transform.Rotate(0f, 0f, speed * Time.deltaTime * 3f);

                    if (betTimeOut - elapsedTime < 3.0f && audioToggle == false)
                    {
                        audioToggle = true;
                        AudioManager.Instance.Play(AudioSourceKey.TimeCount);
                    }

                    int remainSec = Mathf.CeilToInt(remaining);
                    if (remainSec != prevRemainSec)
                    {
                        prevRemainSec = remainSec;
                        if (remainSec <= 3 && remainSec > 0)
                        {
                            if (view.timeCountObj.activeInHierarchy == false)
                            {
                                view.timeCountObj.SetActive(true);
                            }

                            view.timeCountImage.sprite = InGameResourcesBundle.Loaded.TimeCountSprites[remainSec - 1];
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("액션으로 인한 타임 끝 곧 서버 액션노티로 인한 턴 종료");
            }
        }

        public void ActionToDisplay(Partial.BetSizeType actionType)
        {
            view.betActionTypeImage.sprite = InGameResourcesBundle.Loaded.ingameActionTypeImages[(int)actionType];

            ColorUtility.TryParseHtmlString("#" + InGameResourcesBundle.Loaded.ingameActionTypeImageColor[(int)actionType], out Color color);
            var particle0 = view.betActionTypeImageParticle0.main;
            particle0.startColor = color;

            var particle1 = view.betActionTypeImageParticle1.main;
            particle1.startColor = color;

            if (view.stampParentObj.gameObject.activeInHierarchy == false)
            {
                view.stampParentObj.gameObject.SetActive(true);
            }

            view.betActionTypeImageAnimator.Play("Stamp_effect_animation");
        }

        public void SetEndTurn(bool isMe)
        {
            timeCts?.Cancel();
            timeCts?.Dispose();
            timeCts = null;

            view.SetActivateView(isMe, false);
            if (isMe)
            {
                holdemViewer.timeSlider.fillAmount = 0;
                holdemViewer.timeSliderObj.SetActive(false);
            }

            view.timeCountObj.SetActive(false);
            AudioManager.Instance.Stop(AudioSourceKey.TimeCount);
        }
    }
}
