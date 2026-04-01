using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 회원 탈퇴 확인 팝업 (DeleteAccountView에서 이전)
    /// </summary>
    public class PopupCheckQuitSign : BasePopup
    {
        [SerializeField] private CPButton confirmButton;
        [SerializeField] private GameObject deleteFailWindow;
        [SerializeField] private CPButton deleteFailWindowClose;

        private float exitTimer;
        private const float maxTime = 5f;
        private bool deleteAccepted = false;

        protected override void OnInit()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnClickConfirm);
            }

            if (deleteFailWindowClose != null)
            {
                deleteFailWindowClose.onClick.RemoveAllListeners();
                deleteFailWindowClose.onClick.AddListener(() => deleteFailWindow?.SetActive(false));
            }
        }

        public void Show()
        {
            deleteAccepted = false;
            exitTimer = 0f;
            deleteFailWindow?.SetActive(false);
            
            Open();
        }

        private void OnClickConfirm()
        {
            DeleteAccount().Forget();
        }

        private async UniTaskVoid DeleteAccount()
        {
            // 탈퇴 API 호출

            deleteAccepted = true;
            Close();
        }

        private void Update()
        {
            if (deleteAccepted)
            {
                exitTimer += Time.deltaTime;
                if (exitTimer >= maxTime)
                {
                    deleteAccepted = false;
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }
        }

        protected override void OnClose()
        {
            base.OnClose();
        }
    }
}
