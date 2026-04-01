using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using System.Collections;
using Cysharp.Threading.Tasks.DOTween;

namespace CAPYBARA
{
    public class ViewNotification : MonoBehaviour
    {
        [SerializeField] NotiType viewnotitype;
        [SerializeField] Button thisBtn;
        public Text title;
        public Text desc;
        RectTransform descRt;
        public Image image;
        public RectTransform rt;

        Vector2 startPos;
        Vector2 endPosition;
        Vector2 startposForAnnounce;
        Vector2 endPositionForAnnounce;

        [SerializeField] float scrollSpeed;
        const float dm_frEndTime = 5.0f;
        const float announceEndTime = 20.0f;
        const float moveDuration = 1.0f;
        CancellationTokenSource moveDelayCts;

        Coroutine announceAnim;
        public void Init()
        {
            announceAnim = null;

            thisBtn.onClick.AddListener(OnUserInteraction);
            startPos = new Vector2(rt.anchoredPosition.x, rt.sizeDelta.y);
            endPosition = new Vector2(rt.anchoredPosition.x, 0);
            descRt = desc.GetComponent<RectTransform>();

            startposForAnnounce = new Vector2(430, 0);
        }
        public void NotiStart(NotiDesc noti)
        {

            if (viewnotitype == noti.notiType)
            {
                this.gameObject.SetActive(true);
                rt.anchoredPosition = startPos;

                title.text = noti.title;
                desc.text = noti.desc;

                StartMoveWithDelay(noti).Forget();
            }
        }

        public async UniTaskVoid StartMoveWithDelay(NotiDesc noti)
        {
            // 이전 대기 취소
            moveDelayCts?.Cancel();
            moveDelayCts?.Dispose();

            moveDelayCts = new CancellationTokenSource();

            try
            {
                if (noti.notiType == NotiType.Announcement)
                {
                    descRt.anchoredPosition = startposForAnnounce;
                }
                else
                {
                    descRt.anchoredPosition = Vector2.zero;
                }

                // 첫 번째 DoMove
                await this.rt.DOAnchorPos(endPosition, moveDuration).ToUniTask();

                thisBtn.interactable = true;

                if (noti.notiType == NotiType.Announcement)
                {
                    endPositionForAnnounce = new Vector2(-descRt.sizeDelta.x, descRt.anchoredPosition.y);
                    announceAnim = StartCoroutine(StartAnnounceMentAnim());
                }

                if (noti.notiType == NotiType.Announcement)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(announceEndTime), cancellationToken: moveDelayCts.Token);
                }
                else
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(dm_frEndTime), cancellationToken: moveDelayCts.Token);
                }

                // 5초 안에 취소 안 됐으면 두 번째 DoMove 실행
                await rt.DOAnchorPos(startPos, 0.5f).ToUniTask();

                if (noti.notiType == NotiType.Announcement)
                {
                    if (announceAnim != null)
                    {
                        StopCoroutine(announceAnim);
                        announceAnim = null;
                    }
                }

                this.gameObject.SetActive(false);
            }
            catch (OperationCanceledException)
            {
                Debug.Log(" 유저 상호작용으로 인해 두 번째 이동 취소됨");
                await rt.DOAnchorPos(startPos, 0.5f).ToUniTask();

                if (noti.notiType == NotiType.Announcement)
                {
                    if (announceAnim != null)
                    {
                        StopCoroutine(announceAnim);
                        announceAnim = null;
                    }
                }
                this.gameObject.SetActive(false);
            }
        }

        IEnumerator StartAnnounceMentAnim()
        {
            while (true)
            {
                if (descRt.anchoredPosition.x > endPositionForAnnounce.x - 10)
                {
                    descRt.anchoredPosition += Vector2.left * scrollSpeed * Time.deltaTime;
                }
                else
                {
                    ResetTextPosition();
                }


                yield return null;
            }
        }

        void ResetTextPosition()
        {
            descRt.anchoredPosition = startposForAnnounce;
        }

        public void OnUserInteraction()
        {
            if (thisBtn.interactable)
            {
                if (moveDelayCts?.IsCancellationRequested == false)
                {
                    moveDelayCts.Cancel();
                }
                thisBtn.interactable = false;
            }
        }

    }
}