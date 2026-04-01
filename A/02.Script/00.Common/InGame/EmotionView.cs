using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class EmotionView : MonoBehaviour
    {
        public EmoticonSlot emotionviewPrefab;
        public Transform emoticonBoxTr;
        
        private CancellationTokenSource autoCloseCts;

        public static event System.Action<GameObject> OnModalAutoClose;
        
        List<EmoticonSlot> currentEmotionSlots = new List<EmoticonSlot>();
        public void Init()
        {
            EmoticonKindType currentEquipedEmoticon=EmoticonKindType.Rabbit;
            switch (CPPlayer.InGame.currentEquippedEmoticon)
            {
                case ItemID.EMOTICON_1:
                    currentEquipedEmoticon = EmoticonKindType.Rabbit;
                    break;
                case ItemID.EMOTICON_INVITE_FRIEND:
                    currentEquipedEmoticon = EmoticonKindType.Rabbit;
                    break;
                case ItemID.EMOTICON_PLAY_POINT:
                    currentEquipedEmoticon = EmoticonKindType.GreyHood;
                    break;
            }
            for(int i=0;i<currentEmotionSlots.Count;i++) 
                Destroy(currentEmotionSlots[i].gameObject);
            currentEmotionSlots.Clear();
            
            var emoticonInfo = InGameResourcesBundle.Loaded.emotionInfoList.Where(o => o.emoticonKind == currentEquipedEmoticon).ToList();
            for (int j = 0; j < emoticonInfo.Count; j++)
            {
                var slot = Instantiate(emotionviewPrefab);
                slot.transform.SetParent(emoticonBoxTr,false);
                slot.SetSlot(emoticonInfo[j]);
                currentEmotionSlots.Add(slot);
            }
         
            CPPlayer.InGame.emotionExpressEvent += (o,v) => this.gameObject.SetActive(false);
        }


        public void ActiveWindow(bool active)
        {
            this.gameObject.SetActive(active);
            
            if (active)
            {
                UpdateActiveWindow().Forget();
            }
            else
            {
                // 비활성화 시 자동 종료 취소
                autoCloseCts?.Cancel();
                autoCloseCts?.Dispose();
                autoCloseCts = null;
            }

        }
        
        async UniTask UpdateActiveWindow()
        {
            // 이전 자동 종료 태스크가 있다면 취소
            autoCloseCts?.Cancel();
            autoCloseCts?.Dispose();
            autoCloseCts = null;
            // 새로운 CancellationTokenSource 생성
            autoCloseCts = new CancellationTokenSource();
            
            try
            {
                // 5초 대기
                await UniTask.Delay(4000, cancellationToken: autoCloseCts.Token);
                
                // 5초 후 자동으로 창 닫기
                this.gameObject.SetActive(false);
                OnModalAutoClose?.Invoke(gameObject);
            }
            catch (OperationCanceledException)
            {
                // 취소된 경우 (수동으로 창이 닫히거나 새로 열린 경우)
                // 아무것도 하지 않음
            }
            finally
            {
                // CancellationTokenSource 정리
                autoCloseCts?.Cancel();
                autoCloseCts?.Dispose();
                autoCloseCts = null;
            }
        }

        private void OnDisable()
        {
            autoCloseCts?.Cancel();
            autoCloseCts?.Dispose();
            autoCloseCts = null;
        }

        private void OnDestroy()
        {
            // 오브젝트가 파괴될 때 CancellationTokenSource 정리
            autoCloseCts?.Cancel();
            autoCloseCts?.Dispose();
            autoCloseCts = null;
        }

    }
    
    [Serializable]
    public class EmotionInfo
    {
        public EmoticonKindType emoticonKind;
        public EmoticonExpressType emoticonExpress;
        public Sprite[] sprites;
        public Sprite thumbnail;
    }
}
