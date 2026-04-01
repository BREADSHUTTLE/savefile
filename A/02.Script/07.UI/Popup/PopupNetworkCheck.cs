using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CAPYBARA
{
    public class PopupNetworkCheck : BasePopup
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        
        [SerializeField] private CPButton yesButton;
        [SerializeField] private CPButton noButton;
       
        protected override void OnInit()
        {
            if (yesButton != null)
            {
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(()=>OnClickYes().Forget());
            }
            
            if (noButton != null)
            {
                noButton.onClick.RemoveAllListeners();
                noButton.onClick.AddListener(()=>LogoutAsync().Forget());
            }

            titleText.text = "네트워크 오류";
            descriptionText.text = "연결이 원활하지 않습니다. 다시 시도하시겠습니까?";
        }

        private async UniTaskVoid OnClickYes()
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(true));
            
            bool isReconnect =await ConnectionManager.Instance.TryReconnectLobbyOnce();

            PopupManager.Instance.Close<PopupToast>();
            if (isReconnect)
            {
                ConnectionManager.Instance.Reinitialize();
                Close();
            }
            
        }
        private const string LogOutSceneName = "Loading";
        async UniTask LogoutAsync()
        {
            LocalSaveLoader.SaveUserCloudData();

            ConnectionManager.Instance.Dispose();
            PoolManager.Clear();
            PopupManager.Instance.CloseAll();
            
            SceneManager.LoadScene(LogOutSceneName);
        }
    }

}
