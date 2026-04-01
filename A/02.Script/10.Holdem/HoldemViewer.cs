using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class HoldemViewer : MonoBehaviour
    {
        public GameObject actionToggleObject;
        public List<InGameActionToggle> actionToggles;
        public Transform bettingActiveParent;
        public Transform bettingInActiveParent;
        
        public RectTransform anteArrive;
        public RectTransform cardStartPos;
        public HoldemPlayerView[] playerViewList;

        [Header("Table UI(TableInfo)")]
        public TMP_Text tableAnte;
        public TMP_Text SB_BBAmount;
        public GameObject potAmountObject;
        public TMP_Text currentPotAmount;
        
        [Header("communityCard")]
        public Transform communityCardParent;
        public Transform[] communityCardPos;

        [Header("showDown")] 
        public GameObject showdownPanel;
        public Animator showdownPanelAnimator;

        [Header("WinnerDetail")]
        public GameObject winnerDetailPanel;
        public Animator winnerWindowPanelAnimator;
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
        [Header("BackgroundButton")]
        public CPButton backgroundBtn;
        
        [Header("TimeSlider")]
        public GameObject timeSliderObj;
        public Image timeSlider;

        public GameObject jokboWindow;
        public CPButton openJokboWindowBtn;
        public CPButton closeJokboWindowBtn; 
      
        [Space(10)] [Header("test")]
        public TMP_Text test_GameState;
        public Transform chipParent;
        public CPButton showEmoticonViewBtn;
        public EmotionView emotionView;


        private Dictionary<int,GameObject> currentModalObjectDict = new Dictionary<int,GameObject>();
        private int lastPopupHash;
        
        private List<CardViewer> communityCardViewerList = new List<CardViewer>();
        
        private void Awake()
        {
            InitPopupModalSetting();
        }
        private void OnDestroy()
        {
            ClearPopupModalSetting();
        }
        

        public void InitUI(holdem.EnterRes roomInfo)
        {
            var ante = InGameResourcesBundle.Loaded.anteObject;
            PoolManager.CreatePool(ante,5);
            currentPotAmount.text = "0";
        }

        public void InitializeViewData()
        {
            foreach (var communitycard in communityCardViewerList)
            {
                communitycard.Inactive();
                PoolManager.Push(communitycard);
            }
        }

        public void CleanUpTable()
        {
            
        }

    

        
        
        


        #region  인게임 팝업 모달 관련

        private void InitPopupModalSetting()
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

        private void ClearPopupModalSetting()
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
        
        private void CheckAndCloseBackground()
        {
            if (currentModalObjectDict.Count == 0)
            {
                backgroundBtn.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
