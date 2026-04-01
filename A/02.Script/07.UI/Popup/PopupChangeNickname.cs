using CAPYBARA.Bundles;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Linq;
using CAPYBARA.Definition;
using CAPYBARA.Core;
using CAPYBARA.badugi;

namespace CAPYBARA
{
    public class PopupChangeNickname : BasePopup
    {

        [SerializeField] private TMP_InputField nickNameInputField;
        [SerializeField] private RectTransform nickNameChangeWindow;
        [SerializeField] private RectTransform nickNameConfirmWindow;
        [SerializeField] private TMP_Text nickNameChangeErrorText;
        [SerializeField] private CPButton cancelButton;

        [SerializeField] private CPButton nicknameSetBtn;
        [SerializeField] private CPButton nicknameSetConfirmBtn;
        [SerializeField] private CPButton nicknameSetCancelBtn;

        protected override void OnInit()
        {
            nickNameInputField.characterLimit = 0;
            nickNameInputField.onValidateInput = (text, charIndex, addedChar) =>
            {
                if (text.Length >= 10)
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.CharLimitExceeded].StringToLocal, true));
                    return '\0';
                }
                return addedChar;
            };
            
            nicknameSetBtn.onClick.AddListener(() => { ChangeNickNameBtnClick().Forget(); });
            nicknameSetConfirmBtn.onClick.AddListener(() => { ChangeNickNameConfirmBtnClick().Forget(); });
            nicknameSetCancelBtn.onClick.AddListener(() => { ChangeNickNameCancelBtnClick(); });
            cancelButton.onClick.AddListener(Close);
        }

        public override void Open()
        {
            base.Open();
            nickNameChangeWindow.gameObject.SetActive(true);
            nickNameConfirmWindow.gameObject.SetActive(false);
            nickNameChangeErrorText.gameObject.SetActive(false);

            nickNameInputField.text = null;
        }

        private async UniTask ChangeNickNameBtnClick()
        {
            nickNameChangeErrorText.gameObject.SetActive(false);
            nickNameChangeErrorText.text = null;
            string nickName = nickNameInputField.text;
            
            // 클라이언트 비속어 체크 (서버 요청 전 1차 필터)
            if (BadWordFilter.ContainsBadWord(nickName))
            {
                var error = StaticData.GetLobbyErrorInfo(lobby.ErrorCode.ENickInvalid);
                nickNameChangeErrorText.text = error?.message_Kr ?? StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InvalidNickname].StringToLocal;
                nickNameChangeErrorText.gameObject.SetActive(true);
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(nickNameChangeErrorText.text, true));
                return;
            }
            
            var packet = await Services.Lobby.NickCheckAsync(nickName);
            if (!packet.IsSuccess)
            {
                LobbyErrorInfo error;
                string errorMessage = null;
                
                if (packet.Error.Code == lobby.ErrorCode.ENickInvalid)
                {
                    error = StaticData.GetLobbyErrorInfo(lobby.ErrorCode.ENickInvalid);
                    errorMessage = error.message_Kr;
                }
                else if (packet.Error.Code == lobby.ErrorCode.ENickDuplicate)
                {
                    error = StaticData.GetLobbyErrorInfo(lobby.ErrorCode.ENickDuplicate);
                    errorMessage = error.message_Kr;
                }
                else if (packet.Error.Code == lobby.ErrorCode.EInvalidParameter)
                {
                    error = StaticData.GetLobbyErrorInfo(lobby.ErrorCode.EInvalidParameter);
                    errorMessage = error.message_Kr;
                }

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    nickNameChangeErrorText.text = errorMessage;
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(errorMessage, true));
                }
                
                nickNameChangeErrorText.gameObject.SetActive(true);
                CAPYBARA.Extension.eLog($"error: {packet.Error} ");
                return;
            }

            NickNameChangeConfirmWindowOpen();
        }

        public void NickNameChangeConfirmWindowOpen()
        {
            nickNameConfirmWindow.gameObject.SetActive(true);
        }
        
        private async UniTask ChangeNickNameConfirmBtnClick()
        {
            string nickName = nickNameInputField.text;
            var packet = await Services.Lobby.NickSetAsync(nickName);
            if (!packet.IsSuccess)
            {
                var error = StaticData.GetLobbyErrorInfo(packet.Error.Code);
                string errorMessage = error?.message_Kr ?? packet.Error.ToString();
                nickNameChangeErrorText.text = errorMessage;
                nickNameChangeErrorText.gameObject.SetActive(true);
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(errorMessage, true));
                return;
            }

            var userinfo = await Services.Lobby.GetUserInfoAsync();
            CPPlayer.UserInfo.userDatabase = userinfo.Data;

            var usersInfo = await Services.Lobby.GetUserListInfoAsync(LoginData.Cloud.loginValue.userAutoToken);
            if (usersInfo.IsSuccess)
                CPPlayer.UserInfo.userDatabaseList = usersInfo.Data.Users.ToList();

            await Services.Lobby.UseInventoryItemReqAsync(ItemID.NICKNAME_CHANGE.ToString(), 1);

            CPPlayer.OutGame.nickNameChangedCallback?.Invoke();
            this.gameObject.SetActive(false);
        }
        
        private void ChangeNickNameCancelBtnClick()
        {
            nickNameConfirmWindow.gameObject.SetActive(false);
        }

        private void OnClickClose()
        {
            this.gameObject.SetActive(false);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnClose()
        {
            base.OnClose();
        }
    }
}
