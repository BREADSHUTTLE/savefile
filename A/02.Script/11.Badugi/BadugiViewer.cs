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
    public class BadugiViewer : MonoBehaviour
    {
        public GameObject actionToggleObject;
        public List<InGameActionToggle> actionToggles;
        public Transform bettingActiveParent;
        public Transform bettingInActiveParent;

        public GameObject cardactionToggleParent;
        public BadugiCardActionToggle passToggle;
        public GameObject passInactive;
        public BadugiCardActionToggle changeToggle;
        public GameObject changeInactive;

        public RectTransform anteArrive;
        public RectTransform cardStartPos;
        public BadugiPlayerView[] playerViewList;

        public GameObject dayRoundParentObj;
        public Animator dayRoundParentAnimator;
        public GameObject[] dayRoundOnObjs;
        public TMP_Text dayRoundText;

        [Header("Table UI(TableInfo)")] public GameObject potAmountObject;
        public TMP_Text tableAnte;
        public TMP_Text currentPotAmount;

        [Header("showDown")] public GameObject showdownPanel;
        public Animator showdownPanelAnimator;

        [Header("WinnerDetail")] public GameObject winnerDetailPanel;
        public TMP_Text winnerCardRank;
        public TMP_Text winnerAmountChip;

        [Header("JackpotDetail")] public GameObject jackpotDetailBackEffect;
        public GameObject jackpotDetailPanel;
        public TMP_Text jackpotCardRank;
        public TMP_Text jackpotAmountChip;

        [Header("LeaveRoom&Move")] 
        public CPButton leaveBtn;
        public CPButton leaveReservedObj;
        public CPButton moveRoomBtn;
        public CPButton moveReservedObj;

        [Header("Option")] public CPButton optionBtn;

        [Header("TimeSlider")] public GameObject timeSliderObj;
        public Image timeSlider;

        public GameObject jokboWindow;
        public CPButton openJokboWindowBtn;
        public CPButton closeJokboWindowBtn;

        [Header("game ready")] 
        public CPButton readyBtn;
        public TMP_Text readyText;


        [Space(10)] [Header("test")] public TMP_Text test_GameState;
        public Transform chipParent;

        public CPButton showEmoticonViewBtn;
        public EmotionView emotionView;

        [Header("BackgroundButton")] public CPButton backgroundBtn;
        

        private Dictionary<int, GameObject> currentModalObjectDict = new Dictionary<int, GameObject>();
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

            ActionToggleSet();
            CardChangeActionToggleSet();
            LeaveBtnSet();
            MoveRoomBtnSet();
        }

        private void OnDestroy()
        {
            OtherPlayerInfoModal.OnModalAutoClose -= OnModalAutoClose;
            EmotionView.OnModalAutoClose -= OnModalAutoClose;
            AutoCloseWindow.OnModalAutoClose -= OnModalAutoClose;
        }
        #region BackGroundPopupOpenClose
        public void OpenModalObject(GameObject modal)
        {
            lastPopupHash = modal.GetInstanceID();
            currentModalObjectDict[lastPopupHash] = modal;

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

        public event Action<Partial.ActionType, Partial.BetSizeType, Toggle, bool> OnActionTogglePressed;
        public event Action<ChangeActionType, Toggle, bool,bool> OnCardActionTogglePressed;


      
        
        public void InitDisplay()
        {
            jokboWindow.SetActive(false);
            
            winnerDetailPanel.SetActive(false);
            showdownPanel.SetActive(false);

            jackpotDetailPanel.SetActive(false);
            jackpotDetailBackEffect.SetActive(false);
            
            showEmoticonViewBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.useEmoji);
            openJokboWindowBtn.gameObject.SetActive(CPPlayer.Cloud.optionValue.jokboInform);
        }

        void ActionToggleSet()
        {
            for (int i = 0; i < actionToggles.Count; i++)
            {
                var toggleItem = actionToggles[i];
                var bAt = toggleItem.ingameActionType;
                var bet = toggleItem.ingameBettingActionType;
                var toggle = toggleItem.toggle;

                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(ison => { OnActionTogglePressed?.Invoke(bAt, bet, toggle, ison); });
            }
        }

        void CardChangeActionToggleSet()
        {
            passToggle.toggle.onValueChanged.AddListener(o => OnCardActionTogglePressed?.Invoke(ChangeActionType.Pass, passToggle.toggle, false, o));
            changeToggle.toggle.onValueChanged.AddListener(o => OnCardActionTogglePressed?.Invoke(ChangeActionType.Change, changeToggle.toggle, true, o));
        }
        
        public event Action OnLeaveBtnPressed;
        public event Action OnCancelLeaveBtnPressed;
        
        void LeaveBtnSet()
        {
            leaveBtn.onClick.RemoveAllListeners();
            leaveBtn.onClick.AddListener(() => { OnLeaveBtnPressed?.Invoke(); });
            
            leaveReservedObj.onClick.RemoveAllListeners();
            leaveReservedObj.onClick.AddListener(() => OnCancelLeaveBtnPressed?.Invoke());
        }
        public void UpdateAfterLeaveBtnPressed(bool reserveLeaveRequest)
        {
            leaveBtn.gameObject.SetActive(!reserveLeaveRequest);
            leaveReservedObj.gameObject.SetActive(reserveLeaveRequest);
            leaveBtn.enabled = false;
            moveRoomBtn.enabled = false;
        }
        
        public void UpdateAfterCancelLeaveBtnPressed()
        {
            leaveBtn.enabled = true;
            leaveBtn.gameObject.SetActive(true);
            leaveReservedObj.gameObject.SetActive(false);
        }
        
        public event Action OnMoveBtnPressed;
        public event Action OnCancelMoveBtnPressed;
        void MoveRoomBtnSet()
        {
            moveRoomBtn.onClick.RemoveAllListeners();
            moveRoomBtn.onClick.AddListener(() => { OnMoveBtnPressed?.Invoke(); });
            
            moveReservedObj.onClick.RemoveAllListeners();
            moveReservedObj.onClick.AddListener(() => OnCancelMoveBtnPressed?.Invoke());
        }
        public void UpdateAfterMoveBtnPressed(bool reserveMoveRoomRequest)
        {
            moveRoomBtn.gameObject.SetActive(false);
            moveRoomBtn.enabled = !reserveMoveRoomRequest;
            moveReservedObj.gameObject.SetActive(reserveMoveRoomRequest);
                   
            leaveBtn.enabled = false;
            moveRoomBtn.enabled = false;
        }
        
        public void UpdateAfterCancelMoveBtnPressed()
        {
            moveRoomBtn.enabled = true;
            moveRoomBtn.gameObject.SetActive(true);
            moveReservedObj.gameObject.SetActive(false);
        }
        

        public void EmotionViewClose()
        {
            emotionView.gameObject.SetActive(false);
            OnModalAutoClose(emotionView.gameObject);
        }
        
        public void JokboViewClose()
        {
            jokboWindow.gameObject.SetActive(false);
            OnModalAutoClose(jokboWindow);
        }
    }
}