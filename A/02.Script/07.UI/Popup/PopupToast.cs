using System;
using CAPYBARA.Bundles;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public enum ToastDepthMode
    {
        BelowAllPopups,     // 모든 팝업보다 뒤에 (View보다는 앞)
        AboveAllPopups      // 모든 팝업보다 앞에
    }

    public class PopupToast : BasePopup
    {
        // Depth 상수 정의
        public const int DEPTH_BELOW_POPUPS = -100;  // 모든 팝업보다 뒤
        public const int DEPTH_ABOVE_POPUPS = 10000; // 모든 팝업보다 앞

        [Header("Mid Popup With Title")]
        public GameObject midPopupWindowwithTitle;
        public CPButton popupokBtn;
        public TMP_Text popupTitle;
        public Text popupDesc;

        [Header("Mid Popup")]
        public GameObject midPopupWindow;
        public CPButton btnCancel;
        public GameObject goOneButton;
        public CPButton btnClose;
        public GameObject goTwoButton;
        public CPButton btnYes;
        public CPButton btnNo;
        public TMP_Text txtTitle;
        public TMP_Text txtDesc;
        
        [Header("Mid Text Popup")]
        public GameObject midTextPopup;
        public TMP_Text textPopupTitle;
        
        [Header("Bottom Popup")]
        public CPButton bottomPopup;
        public TMP_Text bottomPopupTitle;
        public Image imgErrorIcon;
        public Vector2 startPos;
        public Vector2 endPos;
        public Vector2 ingameEndPos;
        [HideInInspector] public RectTransform popupRect;
        
        [Header("Top Alarm Popup")]
        public CPButton topAlarmPopup;
        public TMP_Text topAlarmPopupTitle;
        public Vector2 topAlarmStartPos;
        public Vector2 topAlarmEndPos;
        [HideInInspector] public RectTransform topAlarmPopupRect;

        [Header("Loading")]
        public GameObject loadingWindow;
        public GameObject serverloadingWindow;

        private ToastDepthMode _currentDepthMode = ToastDepthMode.AboveAllPopups;
        private int _currentDepth = DEPTH_ABOVE_POPUPS;

        // 내부 팝업이 열려있는지 확인
        public bool IsAnyPopupOpen => midPopupWindow.activeSelf || midPopupWindowwithTitle.activeSelf || midTextPopup.activeSelf;

        public override bool CanCloseByDimClick => false;
        
        public override bool CanCloseByBackButton => false;

        public override bool CanHandleBackButton => false;

        protected override void OnInit()
        {
            base.OnInit();
            btnCancel?.onClick.AddListener(() => midPopupWindow.SetActive(false));
            btnClose?.onClick.AddListener(() => midPopupWindow.SetActive(false));
            btnNo?.onClick.AddListener(() => midPopupWindow.SetActive(false));

            popupRect = bottomPopup.GetComponent<RectTransform>();
            popupRect.anchoredPosition = startPos;
            bottomPopup.onClick.AddListener(BottomPopupReturnToStart);
            
            topAlarmPopupRect = topAlarmPopup.GetComponent<RectTransform>();
            topAlarmPopupRect.anchoredPosition = startPos;
            topAlarmPopup.onClick.AddListener(TopAlarmPopupReturnToStart);
            
            bottomPopup.gameObject.SetActive(false);
            topAlarmPopup.gameObject.SetActive(false);
        }
        private void BottomPopupReturnToStart()
        {
            currentTween?.Kill();
            popupRect.DOAnchorPos(startPos, 0.3f).OnComplete(() =>
            {
                bottomPopup.gameObject.SetActive(false);
            });
        }
        
        private void TopAlarmPopupReturnToStart()
        {
            //alarmTween?.Kill();
            topAlarmPopupRect.DOAnchorPos(topAlarmStartPos, 0.3f).OnComplete(() =>
            {
                topAlarmPopup.gameObject.SetActive(false);
            });
        }
        
        public void SetAboveAllPopups()
        {
            _currentDepthMode = ToastDepthMode.AboveAllPopups;
            _currentDepth = DEPTH_ABOVE_POPUPS;
            transform.SetAsLastSibling();
        }

        public void SetBelowAllPopups()
        {
            _currentDepthMode = ToastDepthMode.BelowAllPopups;
            _currentDepth = DEPTH_BELOW_POPUPS;
            transform.SetAsFirstSibling();
        }

        public ToastDepthMode CurrentDepthMode => _currentDepthMode;

        public override void OnBackButtonPressed()
        {
            // 개별 팝업만 닫기
            if (midPopupWindow.activeSelf)
                midPopupWindow.SetActive(false);
            else if (midPopupWindowwithTitle.activeSelf)
                midPopupWindowwithTitle.SetActive(false);
            else if (midTextPopup.activeSelf)
                midTextPopup.SetActive(false);
        }

        // Close()가 호출되어도 전체를 닫지 않고 내부 팝업만 닫음
        public override void Close()
        {
            OnBackButtonPressed();
            gameObject.SetActive(false);
        }

        public void ForceCloseAll()
        {
            base.Close();
        }

        protected override void ConfigurePopupContent(IPopupParameter parameter)
        {
            base.ConfigurePopupContent(parameter);
            if (parameter is ToastPopupParameter toastPopupParameter)
            {
                switch (toastPopupParameter.toastPopupType)
                {
                    case ToastPopupType.Top:
                        break;
                    case ToastPopupType.Mid:
                        break;
                    case ToastPopupType.Bottom:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        
        //popup
        public void ShowPopupOneButton(string _title)
        {
            ShowPopupOneButton(_title, string.Empty,null);
        }
        public void ShowPopupOneButton(string _title, string _desc)
        {
            ShowPopupOneButton(_title, _desc, null);
        }
        public void ShowPopupOneButton(string _title, string _desc, Action onClose)
        {
            SetObjects();
            SetAboveAllPopups();
            gameObject.SetActive(true);
            midPopupWindow.SetActive(true);
            txtTitle.text = _title;
            txtDesc.text = _desc ?? string.Empty;
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(() => onClose?.Invoke());
            btnClose.onClick.AddListener(() => midPopupWindow.SetActive(false));
            if (goOneButton != null)
                goOneButton.SetActive(true);
            if (goTwoButton != null)
                goTwoButton.SetActive(false);
        }
        
        public void ActivateBigwindowTwoBtn(string _title, string _desc, Action okCallback)
        {
            SetObjects();
            SetBelowAllPopups();
            gameObject.SetActive(true);
            midPopupWindowwithTitle.SetActive(true);

            popupTitle.text = _title;
            popupDesc.text = _desc;
            popupokBtn.onClick.RemoveAllListeners();
            popupokBtn.onClick.AddListener(() => okCallback?.Invoke());
            popupokBtn.onClick.AddListener(() => midPopupWindowwithTitle.SetActive(false));

            popupokBtn.gameObject.SetActive(true);
        }

        
        public void ShowPopupTwoButtons(string _title, string _desc, Action okCallback, Action cancelCallback)
        {
            SetObjects();
            SetAboveAllPopups();
            gameObject.SetActive(true);
            midPopupWindow.SetActive(true);

            txtTitle.text = _title;
            txtDesc.text = _desc ?? string.Empty;
            btnYes.onClick.RemoveAllListeners();
            btnYes.onClick.AddListener(() => okCallback?.Invoke());
            btnYes.onClick.AddListener(() => midPopupWindow.SetActive(false));
            btnNo.onClick.RemoveAllListeners();
            btnNo.onClick.AddListener(() => cancelCallback?.Invoke());
            btnNo.onClick.AddListener(() => midPopupWindow.SetActive(false));
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(() => cancelCallback?.Invoke());
            btnCancel.onClick.AddListener(() => midPopupWindow.SetActive(false));

            if (goOneButton != null)
                goOneButton.SetActive(false);
            if (goTwoButton != null)
                goTwoButton.SetActive(true);
        }
        
        Tween currentTween;
        public void ShowBottomPopup(string _title, bool isError,bool isLoadingScene=false)
        {
            SetObjects();
            SetBelowAllPopups();
            gameObject.SetActive(true);
            bottomPopup.gameObject.SetActive(true);
            bottomPopupTitle.text = _title;
            imgErrorIcon.gameObject.SetActive(isError);

            currentTween?.Kill();
            Vector2 endpos;
            if (isLoadingScene)
            {
                endpos = endPos;
            }
            else
            {
                bool isIngame = ViewCanvas.Get<ViewCanvasInGame>().badugiView.gameObject.activeInHierarchy || ViewCanvas.Get<ViewCanvasInGame>().HoldemView.gameObject.activeInHierarchy ||
                                ViewCanvas.Get<ViewCanvasInGame>().sevenpokerView.gameObject.activeInHierarchy;
         
                if (isIngame)
                {
                    endpos = ingameEndPos;
                }
                else
                {
                    endpos = endPos;
                }
            }
           
            
            popupRect.anchoredPosition = startPos;

            currentTween = DOTween.Sequence()
                .Append(popupRect.DOAnchorPos(endpos, 0.3f))
                .AppendInterval(3f) // DelayedCall 대신 AppendInterval 사용
                .Append(popupRect.DOAnchorPos(startPos, 0.3f))
                .AppendCallback(() => {
                    if (bottomPopup != null)
                    {
                        bottomPopup.gameObject.SetActive(false);
                    }
                })
                .SetAutoKill(true); // 완료 시 자동 정리

        }

        public void LoadingInGamePopupActive(bool _isActive)
        {
            if (_isActive)
            {
                SetAboveAllPopups();
                gameObject.SetActive(true);
            }
            loadingWindow.SetActive(_isActive);
        }
        
        public void LoadingOutGamePopupActive(bool _isActive)
        {
            if (_isActive)
            {
                SetAboveAllPopups();
                gameObject.SetActive(true);
            }
            loadingWindow.SetActive(_isActive);
        }

        public void ServerLoadingPopupActive(bool _isActive)
        {
            if (_isActive)
            {
                SetAboveAllPopups();
                gameObject.SetActive(true);
            }

            serverloadingWindow.SetActive(_isActive);
        }
        
        void SetObjects()
        {
            midPopupWindow.SetActive(false);
            midPopupWindowwithTitle.SetActive(false);
            midTextPopup.SetActive(false);
            bottomPopup.gameObject.SetActive(false);
            topAlarmPopup.gameObject.SetActive(false);
            loadingWindow.SetActive(false);
            serverloadingWindow.SetActive(false);
            if (goOneButton != null)
                goOneButton.SetActive(false);
            if (goTwoButton != null)
                goTwoButton.SetActive(false);
        }
       
    }
}

