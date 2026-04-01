using CAPYBARA;
using CAPYBARA.Bundles;
using CAPYBARA.Definition;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using System.Linq;

namespace CAPYBARA.Core
{
    public class ControllerVault
    {
        private PopupVault view;
        private CancellationTokenSource cts;
        private PopupVault.TransactionType currentTransactionType = PopupVault.TransactionType.Deposit;
        private long currentInputAmount = 0;
        private bool hasInput = false;

        private static readonly Color COLOR_PLACEHOLDER = new Color(0.635f, 0.647f, 0.702f);
        private static readonly Color COLOR_INPUT = new Color(0.871f, 0.871f, 0.941f);

        private const float BALANCE_ANIMATION_DURATION = 0.6f;

        public ControllerVault(PopupVault _view, CancellationTokenSource _cts)
        {
            view = _view;
            cts = _cts;

            if (view == null)
                return;

            Setting();
            UpdateBalanceDisplay();
            SetTransactionType(PopupVault.TransactionType.Deposit);
        }

        private void Setting()
        {
            view.tabGroup.onIndexChanged += OnTabClick;

            // 입금하기/출금하기
            view.btnDepositAction?.onClick.AddListener(() => OnActionButtonClick().Forget());
            view.btnWithdrawAction?.onClick.AddListener(() => OnActionButtonClick().Forget());

            // 숫자패드
            for (int i = 0; i < view.numpadKeys.Count; i++)
            {
                int index = i;
                view.numpadKeys[index].btn.onClick.AddListener(() => SetMoneyButton(view.numpadKeys[index]));
            }

            // 컨트롤키
            for (int i = 0; i < view.controllInfos.Count; i++)
            {
                int index = i;
                view.controllInfos[index].btn.onClick.AddListener(() => SetControllButton(view.controllInfos[index]));
            }

            CPPlayer.OutGame.openVaultUI += OnOpenVault;
        }

        private void OnTabChanged(PopupVault.TransactionType type)
        {
            if (currentTransactionType == type)
                return;
            
            currentTransactionType = type;
            currentInputAmount = 0;
            hasInput = false;
            UpdateActionButtonVisibility();
            UpdateInputDisplay();
        }

        private void OnOpenVault()
        {
            ResetToDepositTab();
            OpenVaultAsync().Forget();
        }
        
        private async UniTaskVoid OpenVaultAsync()
        {
            if (!CPPlayer.Inventory.classExpiredNotified && CPPlayer.Inventory.classNumber > 0)
            {
                string prevClassName = CPPlayer.Inventory.GetClassDisplayName(CPPlayer.Inventory.classInfo);

                var classResult = await Services.Lobby.ClassInfoAsync();
                if (classResult.IsSuccess && classResult.Data != null)
                {
                    CPPlayer.Inventory.classInfo = classResult.Data;
                    CPPlayer.Inventory.classNumber = classResult.Data.ItemId switch
                    {
                        nameof(ItemID.CLASS_B) => 1,
                        nameof(ItemID.CLASS_A) => 2,
                        nameof(ItemID.CLASS_S) => 3,
                        _ => 0
                    };
                    CPPlayer.Inventory.classUpdateCallback?.Invoke();
                }
                else
                {
                    CPPlayer.Inventory.classInfo = null;
                    CPPlayer.Inventory.classNumber = 0;
                    CPPlayer.Inventory.classExpiredNotified = true;
                    CPPlayer.Inventory.lastExpiredClassName = prevClassName;
                    CPPlayer.Inventory.classUpdateCallback?.Invoke();

                    bool popupClosed = false;
                    PopupManager.Instance.Open<PopupExpirationClass>(expirationPopup =>
                    {
                        expirationPopup.SetDataConfirmOnly(
                            StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpired].StringToLocal,
                            StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpiredMoveToVault].StringToLocal,
                            prevClassName
                        );
                        expirationPopup.OnPopupClosed = () => popupClosed = true;
                    });
                    await UniTask.WaitUntil(() => popupClosed);
                }
            }
            
            var popup = PopupManager.Instance.Open<PopupVault>();
            if (popup == null)
                return;

            view = popup;
            ResetToDepositTab();
            UpdateBalanceDisplay();
            ClearInput();
        }

        private void ResetToDepositTab()
        {
            SetTransactionType(PopupVault.TransactionType.Deposit);
            view?.tabGroup?.SetActiveToggle((int)PopupVault.TransactionType.Deposit, false);
        }

        private void OnTabClick(int index)
        {
            if ((int)currentTransactionType == index)
                return;

            OnTabChanged((PopupVault.TransactionType)index);
        }

        private void SetTransactionType(PopupVault.TransactionType type)
        {
            currentTransactionType = type;
            currentInputAmount = 0;
            hasInput = false;
            UpdateActionButtonVisibility();
            UpdateInputDisplay();
        }

        private void UpdateActionButtonVisibility()
        {
            bool isDeposit = currentTransactionType == PopupVault.TransactionType.Deposit;
            
            if (view.btnDepositAction != null)
                view.btnDepositAction.gameObject.SetActive(isDeposit);
            
            if (view.btnWithdrawAction != null)
                view.btnWithdrawAction.gameObject.SetActive(!isDeposit);

            if (view.deposit != null && view.deposit.Length > 0)
            {
                for (int i = 0; i < view.deposit.Length; i++)
                {
                    view.deposit[i].SetActive(isDeposit);
                    ColorUtility.TryParseHtmlString("#86C2FF", out var activeColor);
                    ColorUtility.TryParseHtmlString("#4D5B9A", out var disableColor);
                    view.imageDosit.color = isDeposit ? activeColor : disableColor;
                    view.txtDosit[i].color = isDeposit ? activeColor : disableColor;
                }
            }

            if (view.withdraw != null && view.withdraw.Length > 0)
            {
                for (int i = 0; i < view.withdraw.Length; i++)
                {
                    view.withdraw[i].SetActive(!isDeposit);
                    ColorUtility.TryParseHtmlString("#86C2FF", out var activeColor);
                    ColorUtility.TryParseHtmlString("#4D5B9A", out var disableColor);
                    view.imageWithdraw.color = !isDeposit ? activeColor : disableColor;
                    view.txtWithdraw[i].color = !isDeposit ? activeColor : disableColor;
                }
            }
        }

        private void SetMoneyButton(PopupVault.NumberInfo _info)
        {
            long newAmount = _info.key switch
            {
                PopupVault.NumpadKey.Num0 => currentInputAmount * 10,
                PopupVault.NumpadKey.Num00 => currentInputAmount * 100,
                PopupVault.NumpadKey.Num000 => currentInputAmount * 1000,
                _ => currentInputAmount * 10 + _info.amount
            };

            if (newAmount == 0)
                return;

            long limit = Math.Min(GetAvailableMoney(), GetMaxTransferAmount());
            if (newAmount > limit)
            {
                if (_info.key == PopupVault.NumpadKey.Num00 || _info.key == PopupVault.NumpadKey.Num000)
                {
                    int zerosToAdd = _info.key == PopupVault.NumpadKey.Num000 ? 3 : 2;
                    newAmount = currentInputAmount;

                    for (int i = 0; i < zerosToAdd; i++)
                    {
                        long next = newAmount * 10;
                        if (next > limit)
                            break;
                        newAmount = next;
                    }

                    if (newAmount == currentInputAmount)
                        return;
                }
                else
                {
                    return;
                }
            }

            hasInput = true;
            currentInputAmount = newAmount;
            UpdateInputDisplay();
        }

        private void SetControllButton(PopupVault.ControllInfo _info)
        {
            switch (_info.key)
            {
                case PopupVault.ControllKey.Backspace:
                    currentInputAmount /= 10;
                    if (currentInputAmount == 0)
                        hasInput = false;
                    break;

                case PopupVault.ControllKey.Clear:
                    currentInputAmount = 0;
                    hasInput = false;
                    break;

                case PopupVault.ControllKey.Max:
                    hasInput = true;
                    long availableMoney = GetAvailableMoney();
                    long maxTransfer = GetMaxTransferAmount();
                    currentInputAmount = System.Math.Min(availableMoney, maxTransfer);
                    break;
            }

            UpdateInputDisplay();
        }

        private void ClearInput()
        {
            currentInputAmount = 0;
            hasInput = false;
            UpdateInputDisplay();
        }

        private void UpdateInputDisplay()
        {
            if (view.txtInput == null) return;

            if (!hasInput)
            {
                view.txtInput.text = currentTransactionType == PopupVault.TransactionType.Deposit ? StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.HowMuchDeposit].StringToLocal : StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.HowMuchWithdraw].StringToLocal;
                view.txtInput.color = COLOR_PLACEHOLDER;
            }
            else
            {
                view.txtInput.text = Extension.ToKoreanFormat(currentInputAmount);
                view.txtInput.color = COLOR_INPUT;
            }
        }

        private void UpdateBalanceDisplay()
        {
            long myGold = CPPlayer.UserInfo.userDatabase.User.Gold;
            long vaultGold = CPPlayer.UserInfo.userDatabase.User.Safe;

            if (view.txtMyGold != null)
                view.txtMyGold.text = Extension.ToKoreanFormat(myGold);

            if (view.txtVaultGold != null)
                view.txtVaultGold.text = Extension.ToKoreanFormat(vaultGold);
        }

        private async UniTaskVoid OnActionButtonClick()
        {
            if (currentInputAmount <= 0)
                return;

            long prevMyGold = CPPlayer.UserInfo.userDatabase.User.Gold;
            long prevVaultGold = CPPlayer.UserInfo.userDatabase.User.Safe;

            try
            {
                if (currentTransactionType == PopupVault.TransactionType.Deposit)
                {
                    var result = await Services.Lobby.SafeInAsync(currentInputAmount);
                    CPPlayer.UserInfo.userDatabase.User.Gold = result.Data.Gold;
                    CPPlayer.UserInfo.userDatabase.User.Safe = result.Data.Safe;
                }
                else
                {
                    var result = await Services.Lobby.SafeOutAsync(currentInputAmount);
                    CPPlayer.UserInfo.userDatabase.User.Gold = result.Data.Gold;
                    CPPlayer.UserInfo.userDatabase.User.Safe = result.Data.Safe;
                }

                long newMyGold = CPPlayer.UserInfo.userDatabase.User.Gold;
                long newVaultGold = CPPlayer.UserInfo.userDatabase.User.Safe;

                if (view.txtMyGold != null)
                    view.MoneyAnimation(view.txtMyGold, prevMyGold, newMyGold, BALANCE_ANIMATION_DURATION);
                
                if (view.txtVaultGold != null)
                    view.MoneyAnimation(view.txtVaultGold, prevVaultGold, newVaultGold, BALANCE_ANIMATION_DURATION);

                CPPlayer.Balance.MyBalTextAnimEvent?.Invoke(prevMyGold, newMyGold);
                CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();

                ClearInput();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Vault] Transaction failed: {e.Message}");
            }
        }

        private long GetAvailableMoney() => currentTransactionType == PopupVault.TransactionType.Deposit 
                                            ? CPPlayer.UserInfo.userDatabase.User.Gold 
                                            : CPPlayer.UserInfo.userDatabase.User.Safe;
        
        private long GetMaxTransferAmount()
        {
            if (currentTransactionType == PopupVault.TransactionType.Deposit)
                return Constraints.GetMaxLimitVault() - CPPlayer.UserInfo.userDatabase.User.Safe;
            else
                return Constraints.GetMaxLimitGold() - CPPlayer.UserInfo.userDatabase.User.Gold;
        }

        public long GetCurrentInputAmount() => currentInputAmount;
    }
}
