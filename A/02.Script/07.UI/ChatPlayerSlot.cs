using System;
using System.Collections.Generic;
using System.Linq;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class ChatPlayerSlot : Poolable
    {
        public Image avartarImage;
        public TMP_Text playerName;
        public Image OnSelectImage;
        public Image OffSelectImage;

        public GameObject newIcon;
        public TMP_Text newMsgCountTxt;

        public GameObject pin;

        public CPButton button;
        
        [HideInInspector]public string conversationId;
        public string partnerAccountId;

        public ChatRoomList chatRoomInfo;
        public string friendNickName;
        public bool ispinned=false;
        public bool IsWithdrawal { get; private set; }

        [SerializeField] private Image imgLine;

        [Header("프로필")]
        [SerializeField] private CPButton btnProfile;

        [Header("탈퇴 유저 표시")]
        [SerializeField] private GameObject imgWithdrawal;
        [SerializeField] private GameObject withdrawalUI;

        [Header("스와이프 차단")]
        public SwipeToDelete swipeToDelete;
        public CPButton blockButton;
        public CPButton unblockButton;
        public CPButton deleteButton;

        private long _friendUid;
        private Action<long> _onBlockedCallback;
        private Action<long> _onUnblockCallback;
        private Action<long> _onDeleteCallback;

        public void SetLineVisible(bool visible)
        {
            if (imgLine != null)
                imgLine.gameObject.SetActive(visible);
        }

        public void Init(ChatRoomList _chatRoomInfo, User friendUser, bool isBlocked = false, Action<long> onBlocked = null, Action<long> onUnblock = null, Action<long> onDelete = null)
        {
            chatRoomInfo = _chatRoomInfo;
            _onBlockedCallback = onBlocked;
            _onUnblockCallback = onUnblock;
            _onDeleteCallback = onDelete;

            long friendId;
            if (chatRoomInfo.Uid1 == CPPlayer.UserInfo.userDatabase.User.Uid)
                friendId = chatRoomInfo.Uid2;
            else
                friendId = chatRoomInfo.Uid1;

            _friendUid = friendId;

            ApplyUserInfo(friendUser);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                CPPlayer.Chat.ChatRoomClickEvent?.Invoke(chatRoomInfo);
            });

            SetupBlockButtons();
            SetSwipeButtons(!isBlocked && !IsWithdrawal, isBlocked && !IsWithdrawal, IsWithdrawal);
            if (swipeToDelete != null)
            {
                swipeToDelete.clickTarget = button.gameObject;
                swipeToDelete.priorityClickTargets.Clear();
                if (btnProfile != null)
                    swipeToDelete.priorityClickTargets.Add(btnProfile.gameObject);
                swipeToDelete.ResetState();
            }

            newIcon.SetActive(false);
        }

        public void SetPinned(bool _isPinned)
        {
            ispinned = _isPinned;
            pin.SetActive(_isPinned);
        }

        public void SetNewNotification(bool isOn,long newMsgCount)
        {
            newIcon.SetActive(isOn);
            newMsgCountTxt.text=newMsgCount.ToString();
        }

        private void ApplyUserInfo(User user)
        {
            if (user == null)
                return;

            friendNickName = user.Nick;
            playerName.text = user.Nick;

            // 아바타 이미지 설정
            if (!string.IsNullOrEmpty(user.AvatarId) && ItemBundle.Loaded != null)
            {
                var avatarIcon = ItemBundle.Loaded.GetAvatarIcon(user.AvatarId);
                if (avatarIcon != null)
                    avartarImage.sprite = avatarIcon;
            }

            bool isWithdrawal = user.WithdrawalDt > 0;
            IsWithdrawal = isWithdrawal;
            bool isOnline = user.IsOnline;
            OnSelectImage.gameObject.SetActive(isOnline && !isWithdrawal);
            OffSelectImage.gameObject.SetActive(!isOnline && !isWithdrawal);

            btnProfile.onClick.RemoveAllListeners();
            btnProfile.onClick.AddListener(() =>
            {
                PopupManager.Instance.Setup<PopupUserProfile>(popup => popup.OpenWithUid(user.Uid));
            });

            if (imgWithdrawal != null)
                imgWithdrawal.SetActive(isWithdrawal);

            if (withdrawalUI != null)
            {
                withdrawalUI.SetActive(isWithdrawal);
                if (isWithdrawal)
                    EnsureWithdrawalSwipeForwarder();
            }

            //button.enabled = !isWithdrawal;
        }
      
        private void SetupBlockButtons()
        {
            if (blockButton != null)
            {
                blockButton.onClick.RemoveAllListeners();
                blockButton.onClick.AddListener(() => BlockUser().Forget());
            }
            if (unblockButton != null)
            {
                unblockButton.onClick.RemoveAllListeners();
                unblockButton.onClick.AddListener(() => _onUnblockCallback?.Invoke(_friendUid));
            }
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => _onDeleteCallback?.Invoke(chatRoomInfo.ChatroomId));
            }
        }

        private void SetSwipeButtons(bool showBlock, bool showUnblock, bool isWithdrawal = false)
        {
            if (isWithdrawal)
            {
                if (blockButton != null)
                    blockButton.gameObject.SetActive(false);
                if (unblockButton != null)
                    unblockButton.gameObject.SetActive(false);
                if (deleteButton != null)
                    deleteButton.gameObject.SetActive(true);
                if (swipeToDelete != null)
                    swipeToDelete.SetOpenOffset(-150f);
            }
            else
            {
                if (blockButton != null)
                    blockButton.gameObject.SetActive(showBlock);
                if (unblockButton != null)
                    unblockButton.gameObject.SetActive(showUnblock);
                if (deleteButton != null)
                    deleteButton.gameObject.SetActive(true);
                if (swipeToDelete != null)
                    swipeToDelete.SetOpenOffset(-300f);
            }
        }

        private async UniTask BlockUser()
        {
            if (_friendUid <= 0) return;

            try
            {
                var blockResult = await Services.Lobby.ChatBlockAsync(_friendUid, 0);
                if (blockResult.IsSuccess)
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockedUserMsg].StringToLocal, friendNickName), false));
                    _onBlockedCallback?.Invoke(_friendUid);
                }
                else
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockFailed].StringToLocal, true));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ServerErrorWithReason].StringToLocal}{e}", true));
                throw;
            }
        }

        private void EnsureWithdrawalSwipeForwarder()
        {
            if (withdrawalUI == null || swipeToDelete == null)
                return;
            var graphic = withdrawalUI.GetComponent<Graphic>() ?? withdrawalUI.GetComponentInChildren<Graphic>(true);
            var targetGo = graphic != null ? graphic.gameObject : withdrawalUI;
            var forwarder = targetGo.GetComponent<SwipeDragForwarder>();
            if (forwarder == null)
                forwarder = targetGo.AddComponent<SwipeDragForwarder>();
            forwarder.target = swipeToDelete;
        }

        public bool IsAccountPinned(string accountId)
        {
            const string key = "PinnedAccounts";

            // 저장된 문자열 가져오기
            string raw = PlayerPrefs.GetString(key, "");

            // ,로 구분된 목록에서 accountId가 포함되어 있는지 확인
            List<string> pinnedList = raw.Split(',').Where(id => !string.IsNullOrEmpty(id)).ToList();

            return pinnedList.Contains(accountId);
        }
    }
}
