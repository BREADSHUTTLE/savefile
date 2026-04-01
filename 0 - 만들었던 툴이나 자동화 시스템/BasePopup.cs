using System;
using CAPYBARA.Bundles;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public abstract class BasePopup : MonoBehaviour, IBackButtonHandler
    {
        [SerializeField] protected CPButton closeButton;

        public Action OnPopupClosed;
        public virtual bool CanCloseByBackButton => true;
        public virtual bool CanCloseByDimClick => true;

        private bool _isInitialized = false;

        #region IBackButtonHandler
        public virtual int BackButtonPriority => 100 + PopupManager.Instance.GetPopupDepth(GetType());
        
        public virtual bool CanHandleBackButton => gameObject.activeInHierarchy && CanCloseByBackButton;
        
        public virtual void OnBackButtonPressed() => Close();
        #endregion

        protected virtual void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
                
            Init();
        }

        // 초기화
        public void Init()
        {
            if (_isInitialized)
                return;
                
            _isInitialized = true;
            OnInit();
        }

        // 자식 클래스에서 초기화 로직 구현
        protected virtual void OnInit()
        {
        }

        public virtual void Open()
        {
            gameObject.SetActive(true);
            
            // BackButtonManager에 등록
            BackButtonManager.Instance?.Register(this);
            
            OnOpen();
        }
        public virtual void Open(IPopupParameter parameter)
        {
            gameObject.SetActive(true);
            
            ConfigurePopupContent(parameter);
            // BackButtonManager에 등록
            BackButtonManager.Instance?.Register(this);
            
            OnOpen();
        }

        protected virtual void ConfigurePopupContent(IPopupParameter parameter)
        {
            
        }

        protected virtual void OnOpen()
        {
        }

        public virtual void Close()
        {
            OnClose();
            OnPopupClosed?.Invoke();
            OnPopupClosed = null;
            
            // BackButtonManager에서 해제
            BackButtonManager.Instance?.Unregister(this);
            
            PopupManager.Instance?.OnPopupClosed(this);
            gameObject.SetActive(false);
            
            // 플레이 시간 미션 로컬 체크
            CPPlayer.OutGame.CheckPlayTimeNotiLocal?.Invoke();
        }

        protected virtual void OnClose()
        {
        }

        public void ForceClose()
        {
            OnClose();
            OnPopupClosed?.Invoke();
            OnPopupClosed = null;
            
            // BackButtonManager에서 해제
            BackButtonManager.Instance?.Unregister(this);
            
            gameObject.SetActive(false);
        }
    }
}
