using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PopupLossLimit : BasePopup
    {
        [SerializeField] private GameObject defPopup;
        [SerializeField] private Toggle threeToggle;
        [SerializeField] private GameObject threeToggleSelected;
        [SerializeField] private Toggle tenToggle;
        [SerializeField] private GameObject tenToggleSelected;
        [SerializeField] private Image imgDisableConfirm;

        [SerializeField] private CPButton confirmLossLimit;

        [SerializeField] private GameObject checkPopup;
        [SerializeField] private CPButton realCancel;
        [SerializeField] private CPButton realConfirm;

        [SerializeField] private GameObject successPopup;
        [SerializeField] private CPButton successCloseButton;
        [SerializeField] private GameObject samePopup;
        [SerializeField] private Text txtSame;
        [SerializeField] private CPButton btnSame;
        [SerializeField] private GameObject notiPopup;
        [SerializeField] private Text txtNoti;
        [SerializeField] private CPButton btnNoti;
        [SerializeField] private GameObject changePopup;
        [SerializeField] private Text txtChange;
        [SerializeField] private CPButton btnChange;

        protected override void OnInit()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (confirmLossLimit != null)
            {
                confirmLossLimit.onClick.RemoveAllListeners();
                confirmLossLimit.onClick.AddListener(PopupCheckWindow);
            }

            if (realCancel != null)
            {
                realCancel.onClick.RemoveAllListeners();
                realCancel.onClick.AddListener(() => checkPopup.SetActive(false));
            }

            if (realConfirm != null)
            {
                realConfirm.onClick.RemoveAllListeners();
                realConfirm.onClick.AddListener(() => SetLossLimitConfirmClick().Forget());
            }

            if (successCloseButton != null)
            {
                successCloseButton.onClick.RemoveAllListeners();
                successCloseButton.onClick.AddListener(() => successPopup.SetActive(false));
            }

            if (btnSame != null)
            {
                btnSame.onClick.RemoveAllListeners();
                btnSame.onClick.AddListener(() => samePopup.SetActive(false));
            }

            if (btnNoti != null)
            {
                btnNoti.onClick.RemoveAllListeners();
                btnNoti.onClick.AddListener(() => notiPopup.SetActive(false));
            }

            if (btnChange != null)
            {
                btnChange.onClick.RemoveAllListeners();
                btnChange.onClick.AddListener(() => changePopup.SetActive(false));
            }

            threeToggle.onValueChanged.AddListener(_ => UpdateConfirmState());
            tenToggle.onValueChanged.AddListener(_ => UpdateConfirmState());

            defPopup.SetActive(true);
            successPopup.SetActive(false);
            samePopup.SetActive(false);
            notiPopup.SetActive(false);
            changePopup.SetActive(false);
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            checkPopup.SetActive(false);

            bool isLossLimitThree = CPPlayer.UserInfo.memberDatabase.LossLimit == Constraints.LossLimitThree;

            threeToggle.interactable = !isLossLimitThree;
            threeToggleSelected.SetActive(isLossLimitThree);
            tenToggle.interactable = isLossLimitThree;
            tenToggleSelected.SetActive(!isLossLimitThree);
            
            if (isLossLimitThree)
            {
                threeToggle.isOn=true;
            }
            else
            {
                tenToggle.isOn=true;
            }

            defPopup.SetActive(true);
            
            UpdateConfirmState();
        }

        protected override void OnClose()
        {
            base.OnClose();
        }

        private void UpdateConfirmState()
        {
            int selectedValue = threeToggle.isOn ? Constraints.LossLimitThree : Constraints.LossLimitTen;
            bool isSame = selectedValue == CPPlayer.UserInfo.memberDatabase.LossLimit;
            imgDisableConfirm.gameObject.SetActive(isSame);
        }

        private void PopupCheckWindow()
        {
            checkPopup.SetActive(true);
        }

        private async UniTask SetLossLimitConfirmClick()
        {
            int losslimitValue = 0;
            if (threeToggle.isOn)
            {
                losslimitValue = Constraints.LossLimitThree;
            }
            else
            {
                losslimitValue = Constraints.LossLimitTen;
            }

            var setLossLimitRes = await Services.Lobby.LossLimitSetAsync(losslimitValue, false);
            if (setLossLimitRes.IsSuccess == false)
            {
                LobbyErrorInfo errorInfo;
                if (setLossLimitRes.Error.Code == ErrorCode.ELossLimitSame)
                {
                    errorInfo = StaticData.GetLobbyErrorInfo(ErrorCode.ELossLimitSame);
                    txtSame.text = errorInfo.message_Kr;
                    samePopup.SetActive(true);
                }
                else if (setLossLimitRes.Error.Code == ErrorCode.ELossLimitChangeWithin2Day)
                {
                    errorInfo = StaticData.GetLobbyErrorInfo(ErrorCode.ELossLimitChangeWithin2Day);
                    txtNoti.text = errorInfo.message_Kr;
                    notiPopup.SetActive(true);
                }
                else if (setLossLimitRes.Error.Code == ErrorCode.ELossLimitNotChange)
                {
                    errorInfo = StaticData.GetLobbyErrorInfo(ErrorCode.ELossLimitNotChange);
                    txtChange.text = errorInfo.message_Kr;
                    changePopup.SetActive(true);
                }
                else
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ServerErrorWithReason].StringToLocal}{setLossLimitRes.Error}"));
                }
            }
            else
            {
                var memberData = await Services.Lobby.MemberReqAsync(LoginData.Cloud.loginValue.userAutoToken);
                if (memberData.IsSuccess)
                    CPPlayer.UserInfo.memberDatabase = memberData.Data;

                
                Extension.eLog("success set loss limit", Color.cyan);
                checkPopup.SetActive(false);
                Close();
                CPPlayer.OutGame.onLossLimitChanged?.Invoke();
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.LossLimitChanged].StringToLocal, false));
            }
        }
    }
}
