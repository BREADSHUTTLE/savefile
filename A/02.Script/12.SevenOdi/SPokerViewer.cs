using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class SPokerViewer : MonoBehaviour
    {
        public List<InGameActionToggle> actionToggles;

        public RectTransform anteArrive;
        public RectTransform cardStartPos;
        public SPokerPlayerView[] playerViewList;

        [Header("Table UI(TableInfo)")]
        public GameObject potAmountObject;
        public TMP_Text tableAnte;
        public TMP_Text currentPotAmount;

        [Header("showDown")] 
        public GameObject showdownPanel;
        public Animator showdownPanelAnimator;

        [Header("WinnerDetail")]
        public GameObject winnerDetailPanel;
        public TMP_Text winnerCardRank;
        public TMP_Text winnerAmountChip;
        
        [Header("JackpotDetail")] 
        public GameObject jackpotDetailBackEffect;
        public GameObject jackpotDetailPanel;
        public TMP_Text jackpotCardRank;
        public TMP_Text jackpotAmountChip;
        
        [Header("LeaveRoom&Move")]
        public CPButton leaveBtn;
        public CPButton leaveReservedObj;
        public CPButton moveRoomBtn;
        public CPButton moveReservedObj;
        
        [Header("Option")]
        public CPButton optionBtn;

        [Header("TimeSlider")]
        public GameObject timeSliderObj;
        public Image timeSlider;

        public GameObject jokboWindow;
        public CPButton openJokboWindowBtn;
        public CPButton closeJokboWindowBtn;
        
        [Header("select view")]
        public SPokerSelectCardView selectCardViewer;
        

        [Space(10)] 
        [Header("test")]
        public TMP_Text test_GameState;
        public Transform chipParent;

        public CPButton showEmoticonViewBtn;
        public EmotionView emotionView;
        
        [Header("BackgroundButton")]
        public CPButton backgroundBtn;
        public GameObject selectPopupBackGround;
        
        public Dictionary<int,GameObject> currentModalObjectDict = new Dictionary<int,GameObject>();
        private int lastPopupHash;
        
           private void Awake()
        {
            OtherPlayerInfoModal.OnModalAutoClose += OnModalAutoClose;
            EmotionView.OnModalAutoClose += OnModalAutoClose;
            AutoCloseWindow.OnModalAutoClose += OnModalAutoClose;
            
            backgroundBtn.onClick.RemoveAllListeners();
            backgroundBtn.onClick.AddListener(() =>
            {
                bool isExist = currentModalObjectDict.TryGetValue(lastPopupHash, out GameObject obj);

                if (isExist)
                {
                    obj.SetActive(false);
                    currentModalObjectDict.Remove(lastPopupHash);
                    if (currentModalObjectDict.Count > 0)
                    {
                        lastPopupHash = currentModalObjectDict.Last().Key;
                    }
                    else
                    {
                        lastPopupHash = -1;
                    }
                    CheckAndCloseBackground();
                }
            });
        }

        private void OnDestroy()
        {
            OtherPlayerInfoModal.OnModalAutoClose -= OnModalAutoClose;
            EmotionView.OnModalAutoClose -= OnModalAutoClose;
            AutoCloseWindow.OnModalAutoClose -= OnModalAutoClose;
        }

        public void OpenModalObject(GameObject modal)
        {
            lastPopupHash = modal.GetInstanceID();
            currentModalObjectDict[lastPopupHash]=modal;
           
            backgroundBtn.gameObject.SetActive(true);
        }
        
        public void OnModalAutoClose(GameObject modal)
        {
            bool isExist = currentModalObjectDict.TryGetValue(modal.GetInstanceID(), out GameObject obj);
            // 스택에서 해당 모달 제거
            if (currentModalObjectDict.Count > 0 && isExist)
            {
                lastPopupHash = modal.GetInstanceID();
                obj.SetActive(false);
                currentModalObjectDict.Remove(lastPopupHash);
                if (currentModalObjectDict.Count > 0)
                {
                    lastPopupHash = currentModalObjectDict.Last().Key;
                }
                else
                {
                    lastPopupHash = -1;
                }

                CheckAndCloseBackground();
            }
        }
        
        public void CheckAndCloseBackground()
        {
            if (currentModalObjectDict.Count == 0)
            {
                backgroundBtn.gameObject.SetActive(false);
            }
        }
    }
}
