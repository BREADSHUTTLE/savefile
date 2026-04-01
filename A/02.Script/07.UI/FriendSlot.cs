using System;
using CAPYBARA.Bundles;
using BlackTree.Bundles;
using CAPYBARA.Core;
using UnityEngine;
using UnityEngine.UI;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using TMPro;

namespace CAPYBARA
{
    public class FriendSlot : MonoBehaviour
    {
        public GameObject[] onOffBox;
        public Image avatarImage;
        public CPButton userProfile;
        public TMP_Text idLabel;
        public TMP_Text goldLabel;
        public CPButton messegeButton;

        public CPButton removeButton;
        public CPButton blockButton;
        public CPButton unblockButton;

        public SwipeToDelete swipeToDelete;
        
        [Header("탈퇴 유저 표시")]
        public GameObject withdrawalUI;
        public Color withdrawalAvatarColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        private Color normalAvatarColor = Color.white;

        lobby.Friends friendsInfo;
        private long blockedUserUid;
        private Action<long> onUnblockCallback;

        private void Awake()
        {
            removeButton.onClick.AddListener(() => { RemoveFriend().Forget(); });
            if (blockButton != null)
                blockButton.onClick.AddListener(() => { BlockFriend().Forget(); });
            if (unblockButton != null)
                unblockButton.onClick.AddListener(OnClickUnblock);
            userProfile.onClick.RemoveAllListeners();
            userProfile.onClick.AddListener(ClickUserProfile);
        }

        public void Init(lobby.Friends friendInfo)
        {
            bool isWithdrawal = friendInfo.IsWithdrawal;
            SetMode(false, isWithdrawal);

            if (withdrawalUI != null)
            {
                withdrawalUI.SetActive(isWithdrawal);
                if (isWithdrawal)
                    EnsureWithdrawalSwipeForwarder();
            }

            if (isWithdrawal)
            {
                onOffBox[0].SetActive(false);
                onOffBox[1].SetActive(false);
                onOffBox[2].SetActive(true);
            }
            else
            {
                onOffBox[0].SetActive(friendInfo.IsOnline);
                onOffBox[1].SetActive(!friendInfo.IsOnline);
                onOffBox[2].SetActive(false);
            }

            idLabel.text = friendInfo.Nick;
            goldLabel.text = Extension.ToKoreanFormatReward(friendInfo.Gold);

            if (!string.IsNullOrEmpty(friendInfo.Profile) && ItemBundle.Loaded != null)
            {
                var avatarIcon = ItemBundle.Loaded.GetAvatarIcon(friendInfo.Profile);
                if (avatarIcon != null)
                    avatarImage.sprite = avatarIcon;
            }
            avatarImage.color = isWithdrawal ? withdrawalAvatarColor : normalAvatarColor;

            // if (messegeButton != null)
            //     messegeButton.gameObject.SetActive(!isWithdrawal);

            messegeButton.onClick.RemoveAllListeners();
            if (!isWithdrawal)
            {
                messegeButton.onClick.AddListener(() =>
                {
                    CPPlayer.OutGame.CreateConversationFriend?.Invoke(friendInfo.FriendsUid);
                });
            }

            friendsInfo = friendInfo;

            if (swipeToDelete != null)
            {
                swipeToDelete.ResetState();
                swipeToDelete.priorityClickTargets.Clear();
                swipeToDelete.priorityClickTargets.Add(userProfile.gameObject);
                swipeToDelete.priorityClickTargets.Add(messegeButton.gameObject);
            }
        }
        
        public void InitBlocked(lobby.BlockUserInfo blockedUser, Action<long> onUnblock)
        {
            bool isWithdrawal = !blockedUser.IsActive;
            SetMode(true);

            friendsInfo = null;
            
            blockedUserUid = blockedUser.Uid;
            onUnblockCallback = onUnblock;

            if (withdrawalUI != null)
            {
                withdrawalUI.SetActive(isWithdrawal);
                if (isWithdrawal)
                    EnsureWithdrawalSwipeForwarder();
            }

            if (isWithdrawal) 
            {
                onOffBox[0].SetActive(false);
                onOffBox[1].SetActive(false);
                onOffBox[2].SetActive(true);
            }
            else 
            {                
                onOffBox[0].SetActive(blockedUser.IsOnline);
                onOffBox[1].SetActive(!blockedUser.IsOnline);
                onOffBox[2].SetActive(false);
            }
            
            idLabel.text = blockedUser.Nick;
            goldLabel.text = Extension.ToKoreanFormatReward(blockedUser.Gold);

            if (!string.IsNullOrEmpty(blockedUser.AvatarId) && ItemBundle.Loaded != null)
            {
                var avatarIcon = ItemBundle.Loaded.GetAvatarIcon(blockedUser.AvatarId);
                if (avatarIcon != null)
                    avatarImage.sprite = avatarIcon;
            }
            avatarImage.color = isWithdrawal ? withdrawalAvatarColor : normalAvatarColor;

            if (messegeButton != null)
            {
                messegeButton.gameObject.SetActive(!isWithdrawal);
                messegeButton.onClick.RemoveAllListeners();
                if (!isWithdrawal)
                {
                    messegeButton.onClick.AddListener(() =>
                    {
                        CPPlayer.OutGame.CreateConversationFriend?.Invoke(blockedUser.Uid);
                    });
                }
            }

            if (swipeToDelete != null)
                swipeToDelete.ResetState();
        }
        
        private void SetMode(bool blockedMode, bool isWithdrawal = false)
        {
            if (removeButton != null)
                removeButton.gameObject.SetActive(!blockedMode);
            if (blockButton != null)
                blockButton.gameObject.SetActive(!blockedMode && !isWithdrawal);
            if (unblockButton != null)
                unblockButton.gameObject.SetActive(blockedMode);

            if (swipeToDelete != null)
            {
                bool singleButton = blockedMode || isWithdrawal;
                swipeToDelete.SetOpenOffset(singleButton ? -150f : -300f);
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

        private void OnClickUnblock()
        {
            onUnblockCallback?.Invoke(blockedUserUid);
        }

        private async UniTask RemoveFriend()
        {
            if (friendsInfo == null)
                return;
            
            try
            {
                var friendremove = await Services.Lobby.FriendsRequestAsync(FriendsRequestType.Remove, friendsInfo.FriendsUid);
                if (friendremove != null)
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FriendDeleteCompleted].StringToLocal, false));
                    CPPlayer.OutGame.refreshFriendsList?.Invoke();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ServerErrorWithReason].StringToLocal}{e}", true));
                throw;
            }
        }

        private async UniTask BlockFriend()
        {
            if (friendsInfo == null)
                return;
            
            try
            {
                var blockResult = await Services.Lobby.ChatBlockAsync(friendsInfo.FriendsUid, 0);
                if (blockResult.IsSuccess)
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockedUserMsg].StringToLocal, friendsInfo.Nick), false));
                    CPPlayer.OutGame.refreshFriendsList?.Invoke();
                }
                else
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockFailed].StringToLocal, true));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                 PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ServerErrorWithReason].StringToLocal}{e}", true));
                throw;
            }
        }

        private void ClickUserProfile()
        {
            long uid = friendsInfo != null ? friendsInfo.FriendsUid : blockedUserUid;
            if (uid <= 0)
                return;
            PopupManager.Instance.Setup<PopupUserProfile>(popup => popup.OpenWithUid(uid));
        }
    }
}
