using CAPYBARA.Core;
using CAPYBARA.Bundles;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CAPYBARA.lobby;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using AIFLogger;
using BlackTree.Bundles;

namespace CAPYBARA
{
    public class ControllerProfile
    {
        public enum TopBtnType
        {
            profile = 0,
            record
        }

        TopBtnType topBtnType;

        ViewProfileWindow viewProfile;
        ViewCanvasLobby viewLobby;
        private CancellationTokenSource cts;

        List<AccountHistoryObj> accountObjList = new List<AccountHistoryObj>();

        private Tweener infoScrollTween;

        private List<(string avatarId, int durationSeconds)> _currentAvatarList = new List<(string, int)>();
        private AvatarEquipSlot currentSelectedAvatar;
        private string currentSelectedAvatarId;

        private List<lobby.Inventory> inventoryList = new List<lobby.Inventory>();
        private string currentEquippedAvatarId = null;
        private string originalEquippedAvatarId = null; // 아바타 창 열 때의 원래 장착 아바타 ID
        
        private List<MatchRecord> matchRecordCache = new List<MatchRecord>();

        public ControllerProfile(ViewProfileWindow _view, ViewCanvasLobby _lobby, CancellationTokenSource _cts)
        {
            cts = _cts;
            viewProfile = _view;
            viewLobby = _lobby;

            viewProfile.tabToggleGroup.onIndexChanged += OnClickTopBtnByIndex;

            viewProfile.closeBtn.onClick.AddListener(OnClickCloseProfile);

            topBtnType = TopBtnType.profile;
            OnClickTopBtn(topBtnType);

            InitWhenOpenApp();

            CPPlayer.OutGame.openProfileUI += () => OpenViewAndInit().Forget();
            viewProfile.maxChipChange.onClick.AddListener(OpenmaxChipChangeWindow);

            viewProfile.todayRecordGroup.onIndexChanged += OnClickTodayRecordTab;
            viewProfile.totalRecordGroup.onIndexChanged += OnClickTotalRecordTab;

            CPPlayer.OutGame.onLossLimitChanged += RefreshLossLimitUI;

            viewProfile.onScrollDragBegin += StopScrollAnimation;

            viewProfile.btnChangeAvatar.onClick.AddListener(OnClickChangeAvatar);
            viewProfile.btnCloseAvatar.onClick.AddListener(OnClickCloseAvatar);
            viewProfile.onBackButtonInAvatarMode = OnClickCloseAvatar;
            viewProfile.avatarScrollView.OnCellUpdate = OnAvatarCellUpdate;
        }

        void InitWhenOpenApp()
        {
            viewProfile.accountManage.onClick.AddListener(OpenAuthManage);
        }

        private async UniTask OpenViewAndInit()
        {
            // 창 열기 전에 먼저 아바타 데이터 로드
            await LoadAndDisplayEquippedAvatar();
            
            // 최신 멤버 정보 가져오기
            await RefreshMemberDataAsync();
            
            // 최신 유저 정보 가져오기
            await RefreshUserListAsync();
            
            // 최신 월 결제 정보 가져오기
            await RefreshPurchaseMonthlyAsync();
            
            viewProfile.gameObject.SetActive(true);
            
            // 창 열 때 내 정보 탭으로 초기화
            ResetToProfileTab();
            
            // 게임 기록 탭 초기화 (전체)
            viewProfile.todayRecordGroup.SetActiveToggle(0);
            viewProfile.totalRecordGroup.SetActiveToggle(0);
        
            long purchasedPrice = Constraints.RealPurchaseMaxMoney- CPPlayer.UserInfo.purchaseMonthlyDatabase.RemainAmount;
            viewProfile.usedRealMoney.text = $"{Extension.ToKoreanFormat(purchasedPrice)}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Won].StringToLocal}";
            viewProfile.monthMaxRealMoney_0.text = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MonthlyPaymentLimit].StringToLocal}{Extension.ToKoreanFormat(Constraints.RealPurchaseMaxMoney)}";

            RefreshLossLimitUI();
            
            // 본인 인증 만료일 표시
            int reVerifyAtTimestamp = CPPlayer.UserInfo.memberDatabase.ReVerifyAt;
            if (reVerifyAtTimestamp == 0)
            {
                viewProfile.initAuthDay.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IdentityVerificationNeeded].StringToLocal;
            }
            else
            {
                DateTime expireDate = DateTimeOffset.FromUnixTimeSeconds(reVerifyAtTimestamp).LocalDateTime;
                viewProfile.initAuthDay.text = $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IdentityVerifyExpiry].StringToLocal}{expireDate:yyyy.MM.dd}";
            }
            
            GameHistoryInit().Forget();
        }
        
        private void RefreshLossLimitUI()
        {
            if (CPPlayer.UserInfo.memberDatabase == null)
                return;

            long setLossLimit = CPPlayer.UserInfo.memberDatabase.LossToday;

            viewProfile.limitChips0.gameObject.SetActive(setLossLimit == 0);
            viewProfile.limitChips0.text = Extension.ToKoreanFormat(setLossLimit);
            viewProfile.limitChipsM.gameObject.SetActive(setLossLimit < 0);
            viewProfile.limitChipsM.text = Extension.ToKoreanFormat(setLossLimit);
            viewProfile.limitChipsP.gameObject.SetActive(setLossLimit > 0);
            viewProfile.limitChipsP.text = "+" + Extension.ToKoreanFormat(setLossLimit);

            viewProfile.setlimitChipRule.text = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.DailyResetRule].StringToLocal, Extension.ToKoreanFormat(CPPlayer.UserInfo.memberDatabase.LossLimit));
        }

        private async UniTask LoadAndDisplayEquippedAvatar()
        {
            var inventoryResult = await Services.Lobby.GetInventoryAsync(true, cts.Token);
            if (!inventoryResult.IsSuccess || inventoryResult.Data?.Inventory == null)
                return;

            var equippedAvatar = inventoryResult.Data.Inventory
                .FirstOrDefault(inv => inv.IsEffective && IsAvatarItem(inv.ItemId));

            if (equippedAvatar != null)
            {
                currentEquippedAvatarId = equippedAvatar.ItemId;
                UpdateMainAvatarDisplay(currentEquippedAvatarId);
            }
        }

        private async UniTask GameHistoryInit()
        {
            var matchRecordPacket = await Services.Lobby.GetMatchRecordAsync(CPPlayer.UserInfo.userDatabase.User.Uid);
            if (matchRecordPacket.IsSuccess && matchRecordPacket.Data?.MatchRecord != null)
                matchRecordCache = matchRecordPacket.Data.MatchRecord.ToList();
            else
                matchRecordCache.Clear();

            UpdateMatchRecordUI("TODAY", "");
            UpdateMatchRecordUI("TOTAL", "");
        }

        private async UniTask RefreshMemberDataAsync()
        {
            var memberData = await Services.Lobby.MemberReqAsync(LoginData.Cloud.loginValue.userAutoToken);
            if (memberData.IsSuccess)
                CPPlayer.UserInfo.memberDatabase = memberData.Data;
        }

        private async UniTask RefreshPurchaseMonthlyAsync()
        {
            var purchaseHistoryRes = await Services.Lobby.PurchaseMonthlyInfoAsync();
            if (purchaseHistoryRes.IsSuccess)
                CPPlayer.UserInfo.purchaseMonthlyDatabase = purchaseHistoryRes.Data;
        }

        private async UniTask RefreshUserListAsync()
        {
            var usersInfo = await Services.Lobby.GetUserListInfoAsync(LoginData.Cloud.loginValue.userAutoToken, cts.Token);
            if (usersInfo.IsSuccess)
                CPPlayer.UserInfo.userDatabaseList = usersInfo.Data.Users.ToList();

            await RefreshAccountListAsync();
        }

        private async UniTask RefreshAccountListAsync()
        {
            if (CPPlayer.UserInfo.userDatabaseList == null)
                return;

            var sortedList = CPPlayer.UserInfo.userDatabaseList.Where(x => x.IsActive).OrderByDescending(x => x.Gold).ToList();
            var initTasks = new List<UniTask>();
            
            for (int i = 0; i < sortedList.Count; i++)
            {
                int index = i;
                var slotinfo = sortedList[index];
                AccountHistoryObj slotObj;
                if (accountObjList.Count > index)
                {
                    slotObj = accountObjList[index];
                }
                else
                {
                    slotObj = GameObject.Instantiate(viewProfile.slotPrefab);
                    slotObj.transform.SetParent(viewProfile.slotParent, false);
                    accountObjList.Add(slotObj);
                }

                bool isMe = slotinfo.Id == CPPlayer.UserInfo.userDatabase.User.Id;
                // 병렬 처리를 위해 Task 수집
                initTasks.Add(slotObj.Init(slotinfo, isMe));
            }
            
            // 모든 Init을 병렬로 실행
            await UniTask.WhenAll(initTasks);

            for (int i = sortedList.Count; i < accountObjList.Count; i++)
                accountObjList[i].gameObject.SetActive(false);
        }

        void OpenAuthManage()
        {
            PopupManager.Instance.Open<PopupInfoAccountDelete>();
        }

        void OpenmaxChipChangeWindow()
        {
            CPPlayer.OutGame.openLossLimitWindow?.Invoke();
        }

        void OnClickTopBtnByIndex(int index)
        {
            TopBtnType type = index == 0 ? TopBtnType.profile : TopBtnType.record;
            OnClickTopBtn(type);
        }

        public void OnClickTopBtn(TopBtnType _type)
        {
            if (viewProfile.MyAvatarWindow.activeSelf)
            {
                CloseAvatarAndSwitchTab(_type).Forget();
                return;
            }
            
            SwitchTab(_type);
        }

        private async UniTask CloseAvatarAndSwitchTab(TopBtnType _type)
        {
            await SaveAvatarIfChanged();
            
            viewProfile.MyAvatarWindow.SetActive(false);
            viewProfile.btnChangeAvatar.gameObject.SetActive(true);
            viewProfile.tabToggleGroup.SetInteractable(true);
            
            SwitchTab(_type);
        }

        private void SwitchTab(TopBtnType _type)
        {
            bool isSameType = topBtnType == _type;
            
            StopScrollAnimation();
            topBtnType = _type;

            if (isSameType)
                AnimateScrollToStart();
        }

        void OnClickTodayRecordTab(int _index)
        {
            string gameType = _index switch
            {
                1 => "7POKER",
                2 => "BADUGI",
                3 => "HOLDEM",
                _ => "" // 전체
            };
            UpdateMatchRecordUI("TODAY", gameType);
        }

        private void AnimateScrollToStart()
        {
            var rect = GetCurrentScrollRect();
            if (rect.normalizedPosition.y >= 0.99f)
                return;

            infoScrollTween?.Kill();

            infoScrollTween = DOTween.To
            (
                () => rect.normalizedPosition,
                pos => rect.normalizedPosition = pos,
                new Vector2(rect.normalizedPosition.x, 1),
                0.3f
            ).SetEase(Ease.OutQuad);
        }

        private ScrollRect GetCurrentScrollRect() => topBtnType switch
        {
            TopBtnType.profile => viewProfile.infoScrollRect,
            //TopBtnType.record => viewProfile.historyScrollRect,
            _ => viewProfile.infoScrollRect
        };

        private void ResetAllScrollPositions()
        {
            viewProfile.infoScrollRect.normalizedPosition = new Vector2(0, 1);
            //viewProfile.historyScrollRect.normalizedPosition = new Vector2(0, 1);
        }

        private void ResetToProfileTab()
        {
            topBtnType = TopBtnType.profile;

            viewProfile.tabToggleGroup.SetInteractable(true);
            viewProfile.tabToggleGroup.SetActiveToggle(0);
            viewProfile.MyAvatarWindow.SetActive(false);
            viewProfile.btnChangeAvatar.gameObject.SetActive(true);
            
            ResetAllScrollPositions();
        }

        private void StopScrollAnimation()
        {
            infoScrollTween?.Kill();
            infoScrollTween = null;
        }

        private void OnClickChangeAvatar()
        {
            OpenAvatarWindow().Forget();
        }
        
        private async UniTask OpenAvatarWindow()
        {
            viewProfile.MyAvatarWindow.SetActive(true);
            viewProfile.avatarScrollView.SetItemCount(0);
            viewProfile.tabToggleGroup.DeactivateAll();
            viewProfile.btnChangeAvatar.gameObject.SetActive(false);

            if (viewProfile.txtCloseAvatar != null)
                viewProfile.txtCloseAvatar.text = "뒤로가기";

            currentEquippedAvatarId = null;
            inventoryList.Clear();
            var inventoryResult = await Services.Lobby.GetInventoryAsync(true, cts.Token);
            if (inventoryResult.IsSuccess && inventoryResult.Data?.Inventory != null)
            {
                inventoryList.AddRange(inventoryResult.Data.Inventory);
                var equippedAvatar = inventoryList
                    .FirstOrDefault(inv => inv.IsEffective && IsAvatarItem(inv.ItemId));

                if (equippedAvatar != null)
                    currentEquippedAvatarId = equippedAvatar.ItemId;
            }

            originalEquippedAvatarId = currentEquippedAvatarId;

            InitAvatarSlots();
        }

        private void InitAvatarSlots()
        {
            var avatarDisplayList = BuildAvatarDisplayList();
            _currentAvatarList = avatarDisplayList;

            // 현재 장착 중인 아바타 ID 설정 (SetItemCount 전에 설정해야 OnCellUpdate에서 사용 가능)
            currentSelectedAvatar = null;
            currentSelectedAvatarId = currentEquippedAvatarId;

            viewProfile.avatarScrollView.SetItemCount(avatarDisplayList.Count);
        }

        private void OnAvatarCellUpdate(GameObject cell, int index)
        {
            if (index < 0 || index >= _currentAvatarList.Count) return;

            var slot = cell.GetComponent<AvatarEquipSlot>();
            var (avatarId, durationSeconds) = _currentAvatarList[index];

            var avatarBundle = ItemBundle.Loaded;
            var avatarSprite = avatarBundle?.GetAvatarSprite(avatarId);
            var avatarOffset = avatarBundle?.GetAvatarOffset(avatarId) ?? Vector2.zero;
            var avatarName = Core.StaticData.GetItemName(avatarId);

            slot.SetAvatar(avatarSprite, avatarId, durationSeconds, avatarName, avatarOffset);
            slot.hideNameOnDeselect = true;
            slot.onClickEquip = OnClickAvatarSlot;

            bool isEquipped = avatarId == currentSelectedAvatarId;
            slot.SetEquip(isEquipped);

            if (isEquipped)
                currentSelectedAvatar = slot;
        }

        private void OnClickAvatarSlot(AvatarEquipSlot clickedSlot)
        {
            if (currentSelectedAvatar == clickedSlot)
                return;

            // 기존 선택 해제
            currentSelectedAvatar?.SetEquip(false);

            // 새로운 슬롯 선택 (UI만 변경, 서버 통신은 창 닫을 때)
            currentSelectedAvatar = clickedSlot;
            currentEquippedAvatarId = clickedSlot.AvatarId;
            clickedSlot.SetEquip(true);
            
            // 왼쪽 큰 아바타 이미지 변경
            UpdateMainAvatarDisplay(clickedSlot.AvatarId);
        }
        
        private void UpdateMainAvatarDisplay(string avatarId)
        {
            var avatarBundle = ItemBundle.Loaded;
            if (avatarBundle == null)
                return;

            var avatarSprite = avatarBundle.GetAvatarSprite(avatarId);
            if (avatarSprite != null)
            {
                viewProfile.imgAvatar.sprite = avatarSprite;
                viewProfile.imgAvatar.SetNativeSize();

                if (viewLobby != null && viewLobby.avatarImage != null)
                {
                    viewLobby.avatarImage.sprite = avatarSprite;
                    viewLobby.avatarImage.SetNativeSize();
                }
            }

            var shadowSprite = avatarBundle.GetAvatarShadowSprite(avatarId);
            if (shadowSprite != null)
            {
                viewProfile.imgAvatarShadow.sprite = shadowSprite;
                viewProfile.imgAvatarShadow.SetNativeSize();
            }
        }

        private void OnClickCloseAvatar()
        {
            CloseAvatarWindow().Forget();
        }
        
        private void OnClickCloseProfile()
        {
            CloseProfileWindow().Forget();
        }
        
        private async UniTask CloseProfileWindow()
        {
            // 아바타 창이 열려있었다면 아바타 저장 처리
            await SaveAvatarIfChanged();
            
            viewProfile.btnChangeAvatar.gameObject.SetActive(true);
            
            viewProfile.gameObject.SetActive(false);
        }
        
        private async UniTask CloseAvatarWindow()
        {
            // 아바타가 변경되었으면 서버에 저장
            await SaveAvatarIfChanged();
            
            viewProfile.MyAvatarWindow.SetActive(false);
            viewProfile.btnChangeAvatar.gameObject.SetActive(true);
            
            viewProfile.tabToggleGroup.SetInteractable(true);
            viewProfile.tabToggleGroup.SetActiveToggle(viewProfile.tabToggleGroup.CurrentIndex);
        }
        
        private async UniTask SaveAvatarIfChanged()
        {
            if (currentEquippedAvatarId != originalEquippedAvatarId && !string.IsNullOrEmpty(currentEquippedAvatarId))
            {
                var result = await Services.Lobby.InventoryChangeAsync(currentEquippedAvatarId, cts.Token);
                if (!result.IsSuccess)
                {
                    currentEquippedAvatarId = originalEquippedAvatarId;
                    if (!string.IsNullOrEmpty(originalEquippedAvatarId))
                        UpdateMainAvatarDisplay(originalEquippedAvatarId);
                }
                else
                {
                    originalEquippedAvatarId = currentEquippedAvatarId;
                }
            }
        }

        private bool IsGroupedAvatar(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId) || !avatarId.StartsWith("AVATAR_"))
                return false;

            int underscoreCount = avatarId.Count(c => c == '_');
            return underscoreCount == 2;
        }

        private bool IsGroupParentAvatar(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId) || !avatarId.StartsWith("AVATAR_"))
                return false;

            int underscoreCount = avatarId.Count(c => c == '_');
            if (underscoreCount != 1)
                return false;

            string groupPrefix = avatarId + "_";
            return inventoryList.Any(inv => inv.ItemId.StartsWith(groupPrefix));
        }

        private bool IsAvatarItem(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && itemId.StartsWith("AVATAR_");
        }

        private int CalculateRemainingSeconds(lobby.Inventory inventory)
        {
            if (inventory.EffectEndAt == 0)
                return 0;
            
            int currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int remaining = inventory.EffectEndAt - currentTimestamp;
            return remaining > 0 ? remaining : 0;
        }

        private List<(string avatarId, int durationSeconds)> BuildAvatarDisplayList()
        {
            var result = new List<(string avatarId, int durationSeconds)>();

            if (inventoryList == null)
                return result;

            foreach (var inventory in inventoryList)
            {
                if (!IsAvatarItem(inventory.ItemId))
                    continue;

                if (IsGroupedAvatar(inventory.ItemId))
                    continue;

                int remainingSeconds = CalculateRemainingSeconds(inventory);
                result.Add((inventory.ItemId, remainingSeconds));
            }

            result = result.OrderBy(x => GetAvatarSortKey(x.avatarId)).ToList();
            return result;
        }

        private int GetAvatarSortKey(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId) || !avatarId.StartsWith("AVATAR_"))
                return int.MaxValue;

            string afterPrefix = avatarId.Substring(7);

            string numberPart = "";
            foreach (char c in afterPrefix)
            {
                if (char.IsDigit(c))
                    numberPart += c;
                else
                    break;
            }

            return int.TryParse(numberPart, out int num) ? num : -1;
        }

        void OnClickTotalRecordTab(int _index)
        {
            string gameType = _index switch
            {
                1 => "7POKER",
                2 => "BADUGI",
                3 => "HOLDEM",
                _ => "" // 전체
            };
            UpdateMatchRecordUI("TOTAL", gameType);
        }

        private void UpdateMatchRecordUI(string matchStats, string gameTypeStr)
        {
            int winCount = 0;
            int loseCount = 0;

            if (string.IsNullOrEmpty(gameTypeStr))
            {
                foreach (var record in matchRecordCache.Where(r => r.MatchStats == matchStats))
                {
                    winCount += record.Win;
                    loseCount += record.Lose;
                }
            }
            else
            {
                var record = matchRecordCache.FirstOrDefault(r => r.MatchStats == matchStats && r.GameType == gameTypeStr);
                if (record != null)
                {
                    winCount = record.Win;
                    loseCount = record.Lose;
                }
            }

            int total = winCount + loseCount;
            float winRate = total == 0 ? 0f : (float)winCount / total * 100f;
            float roundedRate = (float)Math.Round(winRate, 1, MidpointRounding.AwayFromZero);

            if (matchStats == "TODAY")
            {
                viewProfile.allHistory_day.text = total.ToString();
                viewProfile.allHistory_day_win.text = winCount.ToString();
                viewProfile.allHistory_day_lose.text = loseCount.ToString();
                viewProfile.allHistory_day_per.text = $"{roundedRate:F1}%";
            }
            else
            {
                viewProfile.allHistory_total.text = total.ToString();
                viewProfile.allHistory_total_win.text = winCount.ToString();
                viewProfile.allHistory_total_lose.text = loseCount.ToString();
                viewProfile.allHistory_total_per.text = $"{roundedRate:F1}%";
            }
        }
    }
}