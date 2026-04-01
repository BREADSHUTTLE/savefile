using System.Linq;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class PopupCreateNickname : BasePopup
    {
        public override bool CanCloseByBackButton => false;
        
        [SerializeField] private TMP_InputField nickNameInputField;
        [SerializeField] private RectTransform nickNameChangeWindow;
        [SerializeField] private RectTransform nickNameConfirmWindow;
        [SerializeField] private TMP_Text nickNameChangeErrorText;

        [SerializeField] private CPButton nicknameSetBtn;
        [SerializeField] private CPButton nicknameSetConfirmBtn;
        [SerializeField] private CPButton nicknameSetCancelBtn;

        private ItemID nickChangeType;

        protected override void OnInit()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (nicknameSetBtn != null)
            {
                nicknameSetBtn.onClick.RemoveAllListeners();
                nicknameSetBtn.onClick.AddListener(() => ChangeNickNameBtnClick().Forget());
            }

            if (nicknameSetConfirmBtn != null)
            {
                nicknameSetConfirmBtn.onClick.RemoveAllListeners();
                nicknameSetConfirmBtn.onClick.AddListener(() => ChangeNickNameConfirmBtnClick().Forget());
            }

            if (nicknameSetCancelBtn != null)
            {
                nicknameSetCancelBtn.onClick.RemoveAllListeners();
                nicknameSetCancelBtn.onClick.AddListener(ChangeNickNameCancelBtnClick);
            }

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
        }

        public void SetData(ItemID _nickChangeType)
        {
            nickChangeType = _nickChangeType;
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            nickNameChangeWindow.gameObject.SetActive(true);
            nickNameConfirmWindow.gameObject.SetActive(false);
            nickNameChangeErrorText.gameObject.SetActive(false);

            if (closeButton != null)
            {
                if (nickChangeType == ItemID.NICKNAME_CHANGE)
                {
                    closeButton.gameObject.SetActive(true);
                }
                else
                {
                    closeButton.gameObject.SetActive(false);
                }
            }

            nickNameInputField.text = null;
        }

        protected override void OnClose()
        {
            base.OnClose();
        }

        private async UniTask ChangeNickNameBtnClick()
        {
            nickNameChangeErrorText.gameObject.SetActive(false);
            nickNameChangeErrorText.text = null;

            string nickName = nickNameInputField.text;
            
            // 클라이언트 비속어 체크 (서버 요청 전 1차 필터)
            if (BadWordFilter.ContainsBadWord(nickName))
            {
                var error = StaticData.GetLobbyErrorInfo(ErrorCode.ENickInvalid);
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
                
                if (packet.Error.Code == ErrorCode.ENickInvalid)
                {
                    error = StaticData.GetLobbyErrorInfo(ErrorCode.ENickInvalid);
                    errorMessage = error.message_Kr;
                }
                else if (packet.Error.Code == ErrorCode.ENickDuplicate)
                {
                    error = StaticData.GetLobbyErrorInfo(ErrorCode.ENickDuplicate);
                    errorMessage = error.message_Kr;
                }
                else if (packet.Error.Code == ErrorCode.EInvalidParameter)
                {
                    error = StaticData.GetLobbyErrorInfo(ErrorCode.EInvalidParameter);
                    errorMessage = error.message_Kr;
                }

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    nickNameChangeErrorText.text = errorMessage;
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(errorMessage, true));
                }
                
                nickNameChangeErrorText.gameObject.SetActive(true);
                Extension.eLog($"error: {packet.Error} ");
                return;
            }

            NickNameChangeConfirmWindowOpen();
        }

        private void NickNameChangeConfirmWindowOpen()
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
            {
                CPPlayer.UserInfo.userDatabaseList = usersInfo.Data.Users.ToList();
            }

            if (nickChangeType == ItemID.NICKNAME_CHANGE)
            {
                await Services.Lobby.UseInventoryItemReqAsync(nickChangeType.ToString(), 1);
            }

            CPPlayer.OutGame.nickNameChangedCallback?.Invoke();

            Close();
        }

        private void ChangeNickNameCancelBtnClick()
        {
            nickNameConfirmWindow.gameObject.SetActive(false);
        }
    }
}
