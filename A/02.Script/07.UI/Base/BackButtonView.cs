using UnityEngine;

namespace CAPYBARA
{
    public abstract class BackButtonView : MonoBehaviour, IBackButtonHandler
    {
        [Header("Back Button Settings")]
        [SerializeField] protected int viewPriority = 0;  // View별 우선순위 (높을수록 먼저 닫힘)
        [SerializeField] protected bool canCloseByBackButton = true;

        #region IBackButtonHandler
        public virtual int BackButtonPriority => viewPriority;
        public virtual bool CanHandleBackButton => gameObject.activeInHierarchy && canCloseByBackButton;
        
        public virtual void OnBackButtonPressed() => CloseView();
        #endregion

        protected virtual void OnEnable()
        {
            // 활성화될 때 BackButtonManager에 등록
            BackButtonManager.Instance.Register(this);
        }

        protected virtual void OnDisable()
        {
            // 비활성화될 때 BackButtonManager에서 해제
            if (BackButtonManager.Instance != null)
                BackButtonManager.Instance.Unregister(this);
            
            // 플레이 시간 미션 로컬 체크
            CPPlayer.OutGame.CheckPlayTimeNotiLocal?.Invoke();
        }

        public virtual void CloseView()
        {
            gameObject.SetActive(false);
        }

        public void SetCanCloseByBackButton(bool canClose)
        {
            canCloseByBackButton = canClose;
        }

        public void SetPriority(int priority)
        {
            viewPriority = priority;
        }
    }
}

