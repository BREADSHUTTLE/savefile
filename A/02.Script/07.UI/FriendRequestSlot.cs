using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using BlackTree.Bundles;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CAPYBARA
{
    public class FriendRequestSlot : MonoBehaviour
    {
        public GameObject[] onOffBox;
        public Image avatarImage;
        public TMP_Text nickNameLabel;
        public TMP_Text goldLabel;
        public CPButton messegeButton;

        public CPButton oButton;
        public CPButton xButton;
        public CPButton sendButton;
        [SerializeField] private GameObject sendButtonOverlay;
        [SerializeField] private GameObject rightObjs;

        public CPButton Info;

        public SwipeToDelete swipeToDelete;
        public CPButton blockButton;
        public CPButton unblockButton;

        private long freindAccountId;
        private string _nick;
        private Action<long> _onBlockedCallback;
        private Action<long> _onUnblockCallback;

        private bool isRecievedFriendRequest;
        private void Awake()
        {
            Info.onClick.RemoveAllListeners();
            Info.onClick.AddListener(ClickUserProfile);

            if (blockButton != null)
                blockButton.onClick.AddListener(() => BlockUser().Forget());
            if (unblockButton != null)
                unblockButton.onClick.AddListener(() => _onUnblockCallback?.Invoke(freindAccountId));
        }

        private void ClickUserProfile()
        {
            if (freindAccountId <= 0) return;
            PopupManager.Instance.Setup<PopupUserProfile>(popup => popup.OpenWithUid(freindAccountId));
        }

        public void Init(lobby.Friends friendInfo, bool isRequestRecieved = true, Action<long> onBlocked = null)
        {
            isRecievedFriendRequest = isRequestRecieved;
            _nick = friendInfo.Nick;
            _onBlockedCallback = onBlocked;
            _onUnblockCallback = null;

            if (rightObjs != null)
                rightObjs.SetActive(true);

            sendButton.gameObject.SetActive(false);
            if (sendButtonOverlay != null)
                sendButtonOverlay.SetActive(false);

            onOffBox[0].SetActive(friendInfo.IsOnline);
            onOffBox[1].SetActive(!friendInfo.IsOnline);
            nickNameLabel.text = friendInfo.Nick;
            goldLabel.text = Extension.ToKoreanFormatReward(friendInfo.Gold);

            if (!string.IsNullOrEmpty(friendInfo.Profile) && ItemBundle.Loaded != null)
            {
                var avatarIcon = ItemBundle.Loaded.GetAvatarIcon(friendInfo.Profile);
                if (avatarIcon != null)
                    avatarImage.sprite = avatarIcon;
            }

            messegeButton.onClick.RemoveAllListeners();
            messegeButton.onClick.AddListener(() =>
            {
                CPPlayer.OutGame.CreateConversationFriend?.Invoke(friendInfo.FriendsUid);
            });

            if (isRecievedFriendRequest)
            {
                oButton.gameObject.SetActive(true);
                oButton.onClick.RemoveAllListeners();
                oButton.onClick.AddListener(() =>
                {
                    CPPlayer.OutGame.AcceptRequestFriend?.Invoke(friendInfo.FriendsUid);
                });
            }
            else
            {
                oButton.gameObject.SetActive(false);
            }

            xButton.gameObject.SetActive(true);
            xButton.onClick.RemoveAllListeners();
            xButton.onClick.AddListener(() =>
            {
                if (isRecievedFriendRequest)
                    CPPlayer.OutGame.RejectRequestFriend?.Invoke(friendInfo.FriendsUid);
                else
                    CPPlayer.OutGame.CancelRequestFriend?.Invoke(friendInfo.FriendsUid);
            });

            freindAccountId = friendInfo.FriendsUid;

            SetSwipeButtons(showBlock: true, showUnblock: false);
            if (swipeToDelete != null)
            {
                swipeToDelete.ResetState();
                swipeToDelete.priorityClickTargets.Clear();
                swipeToDelete.priorityClickTargets.Add(messegeButton.gameObject);
                swipeToDelete.priorityClickTargets.Add(oButton.gameObject);
                swipeToDelete.priorityClickTargets.Add(xButton.gameObject);
                swipeToDelete.priorityClickTargets.Add(sendButton.gameObject);
                swipeToDelete.priorityClickTargets.Add(Info.gameObject);
            }
        }
        
        //찾은 유저 초기화(유저 찾기)
        public void InitFindUser(lobby.Friends userInfo, bool alreadyFriend, bool isRequestRecieved,
            bool isBlocked, Action<long> onBlocked = null, Action<long> onUnblock = null)
        {
            freindAccountId = userInfo.FriendsUid;
            _nick = userInfo.Nick;
            _onBlockedCallback = onBlocked;
            _onUnblockCallback = onUnblock;

            if (rightObjs != null)
                rightObjs.SetActive(true);

            sendButton.gameObject.SetActive(false);
            if (sendButtonOverlay != null)
                sendButtonOverlay.SetActive(false);
            oButton.gameObject.SetActive(false);
            xButton.gameObject.SetActive(false);

            onOffBox[0].SetActive(userInfo.IsOnline);
            onOffBox[1].SetActive(!userInfo.IsOnline);
            nickNameLabel.text = userInfo.Nick;
            goldLabel.text = Extension.ToKoreanFormatReward(userInfo.Gold);
            
            if (!string.IsNullOrEmpty(userInfo.Profile) && ItemBundle.Loaded != null)
            {
                var avatarIcon = ItemBundle.Loaded.GetAvatarIcon(userInfo.Profile);
                if (avatarIcon != null)
                    avatarImage.sprite = avatarIcon;
            }
            
            messegeButton.gameObject.SetActive(true);
            
            messegeButton.onClick.RemoveAllListeners();
            messegeButton.onClick.AddListener(() =>
            {
                CPPlayer.OutGame.CreateConversationFriend?.Invoke(userInfo.FriendsUid);
            });
            oButton.gameObject.SetActive(false);
            xButton.gameObject.SetActive(false);
            
            if (alreadyFriend)
            {
                sendButton.gameObject.SetActive(true);
                sendButton.interactable = false;
                if (sendButtonOverlay != null)
                    sendButtonOverlay.SetActive(true);
            }
            else
            {
                if (isRequestRecieved)
                {
                    oButton.gameObject.SetActive(true);
                    oButton.onClick.RemoveAllListeners();
                    oButton.onClick.AddListener(() =>
                    {
                        CPPlayer.OutGame.AcceptRequestFriend?.Invoke(userInfo.FriendsUid);
                        oButton.gameObject.SetActive(false);
                    });
                }
                else
                {
                    sendButton.gameObject.SetActive(true);
                    sendButton.interactable = true;
                    if (sendButtonOverlay != null)
                        sendButtonOverlay.SetActive(false);
                    sendButton.onClick.RemoveAllListeners();
                    sendButton.onClick.AddListener(() =>
                    {
                        CPPlayer.OutGame.RequestFriend?.Invoke(userInfo.FriendsUid, sendButton.gameObject, sendButtonOverlay);
                    });
                } 
            }

            SetSwipeButtons(showBlock: !isBlocked, showUnblock: isBlocked);
            if (swipeToDelete != null)
            {
                swipeToDelete.ResetState();
                swipeToDelete.priorityClickTargets.Clear();
                swipeToDelete.priorityClickTargets.Add(messegeButton.gameObject);
                swipeToDelete.priorityClickTargets.Add(oButton.gameObject);
                swipeToDelete.priorityClickTargets.Add(xButton.gameObject);
                swipeToDelete.priorityClickTargets.Add(sendButton.gameObject);
                swipeToDelete.priorityClickTargets.Add(Info.gameObject);
            }
        }

        private void SetSwipeButtons(bool showBlock, bool showUnblock)
        {
            if (blockButton != null)
                blockButton.gameObject.SetActive(showBlock);
            if (unblockButton != null)
                unblockButton.gameObject.SetActive(showUnblock);
            if (swipeToDelete != null)
                swipeToDelete.SetOpenOffset(-150f);
        }

        private async UniTask BlockUser()
        {
            if (freindAccountId <= 0) return;

            try
            {
                var blockResult = await Services.Lobby.ChatBlockAsync(freindAccountId, 0);
                if (blockResult.IsSuccess)
                {
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockedUserMsg].StringToLocal, _nick), false));
                    _onBlockedCallback?.Invoke(freindAccountId);
                    CPPlayer.OutGame.refreshFriendsList?.Invoke();
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
    }
}