using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using CAPYBARA.Model;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using AdvancedInputFieldPlugin;

namespace CAPYBARA
{
    public class ControllerChat
    {
        ViewChat view;
        CancellationTokenSource cts;

        Dictionary<long, ChatPlayerSlot> playerRoomDict = new Dictionary<long, ChatPlayerSlot>();
        private HashSet<long> _blockedUids = new HashSet<long>();

        List<ChatCloudSlot> playerChatList = new List<ChatCloudSlot>();
        List<ChatCloudDateSlot> chatdateList = new List<ChatCloudDateSlot>();

        Dictionary<long, long> newMsgRoomIdDict = new Dictionary<long, long>();

        Dictionary<long, List<lobby.Message>> messageListDict = new Dictionary<long, List<lobby.Message>>();

        private const int fixedCountperPage = 10;
        private bool isLoading = false;
        private bool hasMoreOldMessages = true;
        private long currentActivateRoomId = -1;
        private bool isSettingChatWindow = false;
        private int pendingNewMessageCount = 0;  // SetChatWindow 중 도착한 새 메시지 개수
        
        private bool _allowChatLoadMoreFromTop = true;
        
        private const int MaxChatMessageLength = 100;

        public ControllerChat(ViewChat _view, CancellationTokenSource _cts)
        {
            view = _view;
            cts = _cts;
            Init();
        }

        private void Init()
        {
            view.closeBtn.onClick.AddListener(() => view.CloseView());
            view.OnAfterClose = OnChatViewAfterClose;

            CPPlayer.OutGame.openChat += () =>
            {
                OpenChatWindowAsync().Forget();
            };

            view.goToPortraitChatBtn.onClick.AddListener(OnPortraitButtonClicked);
            view.backToNormalChatBtn.onClick.AddListener(SetNormalChatMode);
            view.onBackToNormalMode = SetNormalChatMode;
            view.sendMsgBtn.onClick.AddListener(() => SendMessage().Forget());
            if (view.advancedInputField != null)
            {
                view.sendMsgBtn.onClickDown.AddListener(() =>
                {
                    if (isPortraitMode)
                        view.advancedInputField.Select();
                });
                view.sendMsgBtn.onClickUp.AddListener(() =>
                {
                    if (isPortraitMode)
                    {
                        view.advancedInputField.Select();
                        return;
                    }
                });
            }
            if (view.advancedInputField != null)
            {
                var limitFilter = view.advancedInputField.GetComponent<ChatLengthLimitFilter>();
                if (limitFilter == null)
                    limitFilter = view.advancedInputField.gameObject.AddComponent<ChatLengthLimitFilter>();

                limitFilter.Configure(MaxChatMessageLength);
                view.advancedInputField.LiveProcessingFilter = limitFilter;
            }

            view.chatRoomPinToggle.onValueChanged.AddListener(isOn =>
            {
                PinRegist(isOn);
            });

            
            if (view.btnEmptySend != null)
            {
                view.btnEmptySend.onClick.AddListener(() =>
                {
                    CPPlayer.OutGame.openFriendsWithTab?.Invoke(1);
                });
            }

            CPPlayer.OutGame.CreateConversationFriend += (friendId) =>
            {
                OpenNewConversationAsync(friendId).Forget();
            };

            CPPlayer.Chat.ChatRoomClickEvent += chatroominfo =>
            {
                // 다른 채팅방 클릭 시 이모지 박스 닫기
                ChatCloudSlot.CloseAllEmotionBox();

                // 이미 열려 있는 같은 방이면 OpenChatWindow 생략 (목록/스크롤 리셋 방지)
                if (chatroominfo.ChatroomId == currentActivateRoomId)
                    return;
                
                long loadMsgCount = 0;
                if (newMsgRoomIdDict.ContainsKey(chatroominfo.ChatroomId))
                    loadMsgCount = newMsgRoomIdDict[chatroominfo.ChatroomId];

                if (loadMsgCount > 0)
                    OpenChatWindow(chatroominfo.ChatroomId, loadMsgCount, true).Forget();
                else
                    OpenChatWindow(chatroominfo.ChatroomId, 0, false).Forget();
            };

            CPPlayer.OutGame.newMessageNotiCallback += NewMessageNotiReceive;
            newMsgRoomIdDict.Clear();
            
            view.chatBoxParent.SetActive(true);
            view.backToNormalChatBtn.gameObject.SetActive(false);

            view.chatScrollview.onValueChanged.AddListener(OnChatScrollValueChanged);
            
            ChatCloudSlot.OnEmotionReaction += OnEmotionReaction;
            ChatCloudSlot.OnEmotionRemove += OnEmotionRemove;

            ServerInit().Forget();
        }
        
        public bool IsPortraitModeEnabled => CPPlayer.Cloud.optionValue.chatVerticalMode;
        
        private void OnPortraitButtonClicked()
        {
            view.advancedInputField.Select();
            
            if (CPPlayer.Cloud.optionValue.chatVerticalMode)
                SetChatModeAsync().Forget();
        }
        
        private void OnEmotionReaction(long messageId, string newEmotionId, string originalMessage, string existingEmotions)
        {
            SendEmotionReaction(messageId, newEmotionId, originalMessage, existingEmotions).Forget();
        }
        
        private void OnEmotionRemove(long messageId, string emotionIdToRemove, string originalMessage, string existingEmotions)
        {
            SendEmotionRemove(messageId, emotionIdToRemove, originalMessage, existingEmotions).Forget();
        }

        
        private async UniTask SendEmotionRemove(long messageId, string emotionIdToRemove, string originalMessage, string existingEmotions)
        {
            string updatedEmotions = RemoveEmotionFromString(existingEmotions, emotionIdToRemove);
            
            if (updatedEmotions == existingEmotions)
                return;
            
            var sendRes = await Services.Lobby.MessageSendReqAsync(currentActivateRoomId, originalMessage, updatedEmotions, messageId);
            if (sendRes.IsSuccess)
                UpdateMessageSlotEmotion(messageId, updatedEmotions);
        }
        
        private string RemoveEmotionFromString(string existingEmotions, string emotionToRemove)
        {
            if (string.IsNullOrEmpty(existingEmotions))
                return "";
            
            var emotionList = new List<string>(existingEmotions.Split(','));
            emotionList.RemoveAll(e => e.Trim() == emotionToRemove);
            
            return string.Join(",", emotionList);
        }
        
        private async UniTask SendEmotionReaction(long messageId, string newEmotionId, string originalMessage, string existingEmotions)
        {
            if (currentActivateRoomId < 0)
                return;
            
            string finalEmotionId = MergeEmotions(existingEmotions, newEmotionId);
            if (finalEmotionId == existingEmotions)
                return;
            
            var sendRes = await Services.Lobby.MessageSendReqAsync(currentActivateRoomId, originalMessage, finalEmotionId, messageId);
            if (sendRes.IsSuccess)
                UpdateMessageSlotEmotion(messageId, finalEmotionId, newEmotionId: newEmotionId);
            else
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.EmojiSendFailed].StringToLocal, true));
        }
        
        private string MergeEmotions(string existingEmotions, string newEmotionId)
        {
            if (string.IsNullOrEmpty(existingEmotions))
                return newEmotionId;
            
            var existingList = existingEmotions.Split(',').Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e)).ToList();
            
            if (existingList.Contains(newEmotionId))
                return existingEmotions;
            
            existingList.Add(newEmotionId);
            return string.Join(",", existingList);
        }
        
        private void UpdateMessageSlotEmotion(long messageId, string newEmotion, string newEmotionId = null)
        {
            var slot = playerChatList.Find(s => s.myChatData.MessageId == messageId);
            if (slot == null)
                return;
            
            slot.myChatData.Emotion = newEmotion;
            
            if (messageListDict.ContainsKey(currentActivateRoomId))
            {
                var msg = messageListDict[currentActivateRoomId].Find(m => m.MessageId == messageId);
                if (msg != null)
                    msg.Emotion = newEmotion;
            }
            
            slot.SetChat(slot.isMe, slot.myChatData, skipAnimation: true, skipEmojiAnimation: true, newEmotionId: newEmotionId);
        }

        private async UniTask ServerInit()
        {
            await NewMessageInit();
        }
        
        private async UniTask OpenChatWindowAsync()
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
            
            await LoadBlockedUids();
            currentActivateRoomId = -1;

            var viewCg = view.GetComponent<CanvasGroup>();
            if (viewCg == null) viewCg = view.gameObject.AddComponent<CanvasGroup>();
            viewCg.alpha = 0f;
            view.gameObject.SetActive(true);

            await SetChatWindow();
            
            viewCg.alpha = 1f;
            
            // 뒤로가기 등으로 View가 닫혔으면 세로모드 전환하지 않고 중단
            if (!view.gameObject.activeInHierarchy)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
                return;
            }
            
            // 옵션에서 세로모드가 켜져 있으면 자동으로 세로모드로 전환
            if (CPPlayer.Cloud.optionValue.chatVerticalMode)
                await SetChatModeAsync();
            
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
        }

        private async UniTask OpenNewConversationAsync(long friendId)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            var viewCg = view.GetComponent<CanvasGroup>();
            if (viewCg == null)
                viewCg = view.gameObject.AddComponent<CanvasGroup>();
            viewCg.alpha = 0f;
            viewCg.blocksRaycasts = true;
            view.gameObject.SetActive(true);

            await LoadBlockedUids();

            await CreateNewChatAndLoad(friendId);

            viewCg.alpha = 1f;

            CPPlayer.OutGame.HideFriendsForChat?.Invoke();

            if (!view.gameObject.activeInHierarchy)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
                return;
            }

            Canvas.ForceUpdateCanvases();
            view.friendsScrollView.movementType = playerRoomDict.Count >= 6 ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;

            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
        }

        private void OnChatViewAfterClose()
        {
            if (!CPPlayer.OutGame.ReturnToFriendsWhenChatCloses)
                return;

            CPPlayer.OutGame.ReturnToFriendsWhenChatCloses = false;
            CPPlayer.OutGame.ShowFriendsViewQuiet?.Invoke();
        }
        
        private async UniTask SetChatWindow()
        {
            isSettingChatWindow = true;
            pendingNewMessageCount = 0;
            ResetFreindScrollPosition();
            
            await SettingChatFriends();
            if (currentActivateRoomId >= 0)
            {
                view.emptyWindow.SetActive(false);
                view.chatBox.gameObject.SetActive(true);
                if (newMsgRoomIdDict.ContainsKey(currentActivateRoomId))
                {
                    if (newMsgRoomIdDict[currentActivateRoomId] > 0)
                        await OpenChatWindow(currentActivateRoomId, newMsgRoomIdDict[currentActivateRoomId], true);
                    else
                        await OpenChatWindow(currentActivateRoomId, 0, false);
                }
                else
                {
                    await OpenChatWindow(currentActivateRoomId, 0, false);
                }
            }
            else
            {
                view.emptyWindow.SetActive(true);
                view.chatBox.gameObject.SetActive(false);
            }

            isSettingChatWindow = false;
            
            // SetChatWindow 중에 도착한 새 메시지가 있으면 추가로 불러오기
            if (pendingNewMessageCount > 0 && currentActivateRoomId >= 0)
            {
                await OpenChatWindow(currentActivateRoomId, pendingNewMessageCount, true);
                pendingNewMessageCount = 0;
            }
        }

        private async UniTask NewMessageInit()
        {
            await SettingChatFriends();

            var roomIds = playerRoomDict.Keys.ToList();
            if (roomIds.Count == 0)
                return;

            var newMsgCountPacket = await Services.Lobby.NewMessageListCountAsync(roomIds);
            if (!newMsgCountPacket.IsSuccess)
                return;

            bool newmsgExist = false;
            foreach (var item in newMsgCountPacket.Data.MessageNewCount)
            {
                if (item.Count == 0)
                    continue;

                long roomId = item.RoomId;
                long newMsgCount = item.Count;

                if (newMsgRoomIdDict.ContainsKey(roomId))
                    newMsgRoomIdDict[roomId] = newMsgCount;
                else
                    newMsgRoomIdDict.Add(roomId, newMsgCount);

                if (playerRoomDict.TryGetValue(roomId, out var slot))
                    slot.SetNewNotification(true, newMsgCount);

                newmsgExist = true;
            }

            if (newmsgExist)
                CPPlayer.OutGame.newMsgExistNotiCallback?.Invoke(true);
        }

        void OnChatScrollValueChanged(Vector2 pos)
        {
            if (isSettingChatWindow)
                return;
            if (!_allowChatLoadMoreFromTop)
                return;
                
            if (pos.y >= 0.80f)
            {
                if (isLoading || !hasMoreOldMessages)
                    return;
                isLoading = true;
                LoadMoreOldMessagesLight().Forget();
            }
        }

        private async UniTaskVoid LoadMoreOldMessagesLight()
        {
            try
            {
                if (currentActivateRoomId < 0 || !messageListDict.ContainsKey(currentActivateRoomId))
                    return;

                var currentRoomMsgList = messageListDict[currentActivateRoomId];
                int msgCount = currentRoomMsgList.Count;
                int page = msgCount / fixedCountperPage + 1;

                var roomMsgInfoPacket = await Services.Lobby.MessageListReqAsync(currentActivateRoomId, page, fixedCountperPage);
                var loadMsgList = roomMsgInfoPacket.Data.Message.ToList();

                if (loadMsgList.Count < fixedCountperPage)
                    hasMoreOldMessages = false;

                var existingIds = new HashSet<long>(currentRoomMsgList.Select(o => o.MessageId));
                loadMsgList.RemoveAll(b => existingIds.Contains(b.MessageId));

                if (loadMsgList.Count == 0)
                {
                    isLoading = false;
                    return;
                }

                foreach (var loaded in loadMsgList)
                    currentRoomMsgList.Add(loaded);

                var scrollCg = view.chatScrollview.GetComponent<CanvasGroup>();
                if (scrollCg == null) scrollCg = view.chatScrollview.gameObject.AddComponent<CanvasGroup>();
                scrollCg.alpha = 0f;

                view.chatScrollview.velocity = Vector2.zero;

                var contentRT = view.chatScrollview.content;
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
                Canvas.ForceUpdateCanvases();
                float prevPosY = contentRT.anchoredPosition.y;

                // 기존 최상단 슬롯의 Content 내 로컬 위치를 기억 (위치 보정 앵커)
                ChatCloudSlot anchorSlot = playerChatList
                    .Where(s => s != null && s.myChatData != null)
                    .OrderBy(s => s.myChatData.CreatedAt)
                    .FirstOrDefault();
                float anchorLocalY = anchorSlot != null ? anchorSlot.rectTransform.anchoredPosition.y : 0f;

                // 기존 맨 위 날짜 슬롯 제거 (새 메시지가 위에 추가되므로 재계산 필요)
                if (chatdateList.Count > 0)
                {
                    var topDateSlot = chatdateList[chatdateList.Count - 1];
                    if (topDateSlot != null && topDateSlot.transform.GetSiblingIndex() == 0)
                    {
                        chatdateList.Remove(topDateSlot);
                        PoolManager.Push(topDateSlot);
                    }
                }

                // 기존 맨 위 채팅 슬롯의 날짜 기억
                string existingTopDate = "";
                if (anchorSlot != null)
                    existingTopDate = FormatDateWithDayOfWeek(
                        DateTimeOffset.FromUnixTimeSeconds(anchorSlot.myChatData.CreatedAt).LocalDateTime);

                var sortedNewMsgs = loadMsgList.OrderByDescending(o => o.CreatedAt).ToList();
                foreach (var msginfo in sortedNewMsgs)
                {
                    ChatCloudSlot chatCloudSlot = PoolManager.Pop(view.chatCloudPrefab, view.chatScrollview.content, Vector2.zero, 5);
                    playerChatList.Add(chatCloudSlot);

                    bool isMe = msginfo.Uid == CPPlayer.UserInfo.userDatabase.User.Uid;
                    chatCloudSlot.SetChat(isMe, msginfo, skipAnimation: true, showTime: true);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatCloudSlot.rectTransform);
                    chatCloudSlot.transform.SetAsFirstSibling();
                }

                // 새로 추가된 메시지 + 기존 상단 경계 부분만 날짜/시간 갱신
                var allSlots = playerChatList
                    .Where(s => s != null && s.myChatData != null)
                    .OrderBy(s => s.myChatData.CreatedAt)
                    .ToList();

                // 시간 표시: 새로 추가된 슬롯 + 기존 맨 위 슬롯만 갱신
                int newCount = loadMsgList.Count;
                int updateRange = Mathf.Min(newCount + 1, allSlots.Count);
                for (int i = 0; i < updateRange; i++)
                {
                    var currentSlot = allSlots[i];
                    bool showTime = true;
                    if (i < allSlots.Count - 1)
                    {
                        var nextSlot = allSlots[i + 1];
                        bool sameUser = currentSlot.myChatData.Uid == nextSlot.myChatData.Uid;
                        DateTime ct = DateTimeOffset.FromUnixTimeSeconds(currentSlot.myChatData.CreatedAt).LocalDateTime;
                        DateTime nt = DateTimeOffset.FromUnixTimeSeconds(nextSlot.myChatData.CreatedAt).LocalDateTime;
                        if (sameUser && ct.Year == nt.Year && ct.Month == nt.Month && ct.Day == nt.Day && ct.Hour == nt.Hour && ct.Minute == nt.Minute)
                            showTime = false;
                    }
                    currentSlot.UpdateTimeDisplay(showTime);
                }

                // 날짜 구분선: 새로 추가된 영역에만 삽입
                for (int i = 0; i < allSlots.Count - 1 && i < newCount; i++)
                {
                    DateTime curDate = DateTimeOffset.FromUnixTimeSeconds(allSlots[i].myChatData.CreatedAt).LocalDateTime;
                    DateTime nextDate = DateTimeOffset.FromUnixTimeSeconds(allSlots[i + 1].myChatData.CreatedAt).LocalDateTime;
                    string curDateStr = FormatDateWithDayOfWeek(curDate);
                    string nextDateStr = FormatDateWithDayOfWeek(nextDate);

                    if (curDateStr != nextDateStr)
                    {
                        ChatCloudDateSlot dateSlot = PoolManager.Pop(view.chatDatePrefab, view.chatScrollview.content, Vector2.zero, 3);
                        chatdateList.Add(dateSlot);
                        dateSlot.SetDate(nextDateStr);
                        int insertIdx = allSlots[i + 1].transform.GetSiblingIndex();
                        dateSlot.transform.SetSiblingIndex(insertIdx);
                    }
                }

                // 맨 위 날짜 슬롯 추가
                if (allSlots.Count > 0)
                {
                    string topDate = FormatDateWithDayOfWeek(
                        DateTimeOffset.FromUnixTimeSeconds(allSlots[0].myChatData.CreatedAt).LocalDateTime);
                    ChatCloudDateSlot firstDateSlot = PoolManager.Pop(view.chatDatePrefab, view.chatScrollview.content, Vector2.zero, 3);
                    chatdateList.Add(firstDateSlot);
                    firstDateSlot.SetDate(topDate);
                    firstDateSlot.transform.SetAsFirstSibling();
                }

                // 동기적으로 전체 레이아웃 확정
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
                Canvas.ForceUpdateCanvases();

                // 앵커 슬롯이 얼마나 밀렸는지로 스크롤 위치 보정
                if (anchorSlot != null)
                {
                    float newAnchorLocalY = anchorSlot.rectTransform.anchoredPosition.y;
                    float shift = anchorLocalY - newAnchorLocalY;
                    contentRT.anchoredPosition = new Vector2(contentRT.anchoredPosition.x, prevPosY + shift);
                }
                view.chatScrollview.velocity = Vector2.zero;

                await UniTask.Yield();
                scrollCg.alpha = 1f;
            }
            finally
            {
                isLoading = false;
            }
        }

        private void NewMessageNotiReceive(long roomId, bool isNewMsgRecieve)
        {
            // SetChatWindow 실행 중일 때는 대기 카운트만 증가
            if (isSettingChatWindow && currentActivateRoomId == roomId && isNewMsgRecieve)
            {
                pendingNewMessageCount++;
                return;
            }
            
            // SetChatWindow 실행 중이 아닐 때만 OpenChatWindow 호출
            if (view.gameObject.activeInHierarchy && currentActivateRoomId == roomId && isNewMsgRecieve)
            {
                OpenChatWindow(currentActivateRoomId,1,true).Forget();
                return;
            }
            
            if (isNewMsgRecieve)
            {
                if (newMsgRoomIdDict.ContainsKey(roomId))
                    newMsgRoomIdDict[roomId]++;
                else
                    newMsgRoomIdDict.Add(roomId, 1);
            }
            else
            {
                if (newMsgRoomIdDict.ContainsKey(roomId))
                {
                    newMsgRoomIdDict.Remove(roomId);
                    playerRoomDict[roomId].SetNewNotification(false, 0);
                }
            }

            if (view.gameObject.activeInHierarchy)
            {
                if (playerRoomDict.ContainsKey(roomId) && newMsgRoomIdDict.ContainsKey(roomId))
                    playerRoomDict[roomId].SetNewNotification(isNewMsgRecieve, newMsgRoomIdDict[roomId]);
            }

            if (newMsgRoomIdDict.Count == 0)
                CPPlayer.OutGame.newMsgExistNotiCallback?.Invoke(false);
        }

        private async UniTask SettingChatFriends()
        {
            var chatListPacket = await Services.Lobby.ChatReqAsync();

            foreach (var chatPlayerSlot in playerRoomDict)
                PoolManager.Push(chatPlayerSlot.Value);

            playerRoomDict.Clear();

            if (chatListPacket.IsSuccess)
            {
                var roomList = chatListPacket.Data.ChatRoomList;

                var myUid = CPPlayer.UserInfo.userDatabase.User.Uid;
                var friendUids = roomList.Select(r => r.Uid1 == myUid ? r.Uid2 : r.Uid1).Where(uid => uid > 0).Distinct().ToList();
                var userLookup = new Dictionary<long, lobby.User>();
                if (friendUids.Count > 0)
                {
                    var usersResult = await Services.Lobby.UserReqByUserIdsAsync(friendUids);
                    if (usersResult.IsSuccess && usersResult.Data?.User != null)
                        userLookup = usersResult.Data.User.ToDictionary(u => u.Uid);
                }

                foreach (var chatRoom in roomList)
                {
                    long friendUid = chatRoom.Uid1 == myUid ? chatRoom.Uid2 : chatRoom.Uid1;
                    bool isBlocked = _blockedUids.Contains(friendUid);
                    userLookup.TryGetValue(friendUid, out var friendUser);

                    ChatPlayerSlot roomSlot = PoolManager.Pop(view.friendSlotPrefab, view.friendsScrollView.content, Vector2.zero, 5);
                    roomSlot.Init(chatRoom, friendUser, isBlocked, OnBlockFromChat, OnUnblockFromChat, OnDeleteFromSwipe);
                    if (roomSlot.swipeToDelete != null)
                        roomSlot.swipeToDelete.parentScrollRect = view.friendsScrollView;
                    var pinnedData = CPPlayer.Cloud.pinChatUserinfo.pinnedInfo.Find(o => o.roomId == chatRoom.ChatroomId);
                    bool isPinned = pinnedData != null;
                    roomSlot.SetPinned(isPinned);
                    playerRoomDict.Add(chatRoom.ChatroomId, roomSlot);
                    roomSlot.gameObject.SetActive(true);
                }

                SortChatFriendsList();

                foreach (var chatPlayerSlot in playerRoomDict)
                {
                    long roomId = chatPlayerSlot.Key;
                    if (newMsgRoomIdDict.ContainsKey(roomId))
                        chatPlayerSlot.Value.SetNewNotification(true, newMsgRoomIdDict[roomId]);
                }                
            }

            view.friendsScrollView.movementType = playerRoomDict.Count >= 6 ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
            await UniTask.Yield();
        }

        private void PinRegist(bool ison)
        {
            if (currentActivateRoomId < 0)
                return;
            
            if (playerRoomDict.ContainsKey(currentActivateRoomId))
                playerRoomDict[currentActivateRoomId].SetPinned(ison);
            
            int nowUnix = (int)(DateTimeOffset.Now.ToUnixTimeSeconds());
            var pinnedData = CPPlayer.Cloud.pinChatUserinfo.pinnedInfo.Find(o => o.roomId == currentActivateRoomId);
            if (pinnedData != null)
            {
                if (ison)
                    pinnedData.pinned_at = nowUnix;    
                else
                    CPPlayer.Cloud.pinChatUserinfo.pinnedInfo.Remove(pinnedData);
            }
            else
            {
                if (ison)
                {
                    UserCloudData.PinChatRoomInfo pinnedNewData = new UserCloudData.PinChatRoomInfo();
                    pinnedNewData.pinned_at=nowUnix;
                    pinnedNewData.roomId = currentActivateRoomId;
                    CPPlayer.Cloud.pinChatUserinfo.pinnedInfo.Add(pinnedNewData);
                }
            }
         
            SortChatFriendsList();
            LocalSaveLoader.SaveUserCloudData();
        }

        private void SortChatFriendsList()
        {
            var sorted = playerRoomDict.Values.OrderByDescending(slot => slot.ispinned).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var slot = sorted[i];
                slot.transform.SetParent(view.friendsScrollView.content, false);
                slot.transform.SetSiblingIndex(i);
                slot.SetLineVisible(i < sorted.Count - 1);
            }

            if (sorted.Count > 0)
            {
                if (currentActivateRoomId < 0)
                    currentActivateRoomId = sorted[0].chatRoomInfo.ChatroomId;
            }
        }

        private async UniTask OpenChatWindow(long chatRoomId, long countPerPage = 0, bool isNewMsg = false, bool skipEmojiAnimation = true)
        {
            bool isOtherUser = chatRoomId != currentActivateRoomId;
            
            if (isOtherUser)
                hasMoreOldMessages = true;
            
            currentActivateRoomId = chatRoomId;

            //pin setting
            var pinnedData = CPPlayer.Cloud.pinChatUserinfo.pinnedInfo.Find(o => o.roomId == chatRoomId);
            bool isPin = pinnedData != null;
            view.chatRoomPinToggle.isOn = isPin;
            
            if (isOtherUser)
            {
                view.chatScrollview.gameObject.SetActive(false);
                
                for (int i = 0; i < playerChatList.Count; i++)
                    PoolManager.Push(playerChatList[i]);

                playerChatList.Clear();
            }

            // 기존 chatdateList 정리
            for (int i = 0; i < chatdateList.Count; i++)
                PoolManager.Push(chatdateList[i]);

            chatdateList.Clear();
            
            // content에 남아있는 모든 ChatCloudDateSlot도 제거 (혹시 모를 누락 방지)
            var scrollContent = view.chatScrollview.content;
            for (int i = scrollContent.childCount - 1; i >= 0; i--)
            {
                var child = scrollContent.GetChild(i);
                var dateSlot = child.GetComponent<ChatCloudDateSlot>();
                if (dateSlot != null)
                    PoolManager.Push(dateSlot);
            }

            view.chatRoomName.text = playerRoomDict[chatRoomId].friendNickName;
            if (view.sendMsgBtn != null)
                view.sendMsgBtn.interactable = true;
            
            // 채팅방 아바타 이미지 설정
            if (playerRoomDict[chatRoomId].avartarImage != null && playerRoomDict[chatRoomId].avartarImage.sprite != null)
                view.chatRoomAvatar.sprite = playerRoomDict[chatRoomId].avartarImage.sprite;

            if (messageListDict.ContainsKey(chatRoomId) == false)
                messageListDict.Add(chatRoomId, new List<lobby.Message>());

            int msgCount = messageListDict[chatRoomId].Count;
            int page = msgCount / fixedCountperPage;
            int leftMsg = msgCount % fixedCountperPage;
            List<lobby.Message> loadMsgList = new List<Message>();
            List<lobby.Message> newloadMsgList = new List<Message>();

            bool isLoadMsgExist = false;
            //아무것도 없이 신규메세지만 불러올 경우 이전 메세지 한번 더 요청
            if (isNewMsg && msgCount == 0)
            {
                var roomMsgInfoPacket = await Services.Lobby.MessageListReqAsync(currentActivateRoomId, 1, fixedCountperPage);
                loadMsgList = roomMsgInfoPacket.Data.Message.ToList();
            }
            
            if (countPerPage > 0)
            {
                if (isNewMsg)
                {
                    var roomMsgInfoPacket = await Services.Lobby.MessageRecvReqAsync(currentActivateRoomId, 1, countPerPage);
                    newloadMsgList = roomMsgInfoPacket.Data.Message.ToList();
                }
                else
                {
                    if (countPerPage > 10)
                    {
                        var roomMsgInfoPacket = await Services.Lobby.MessageListReqAsync(currentActivateRoomId, 1, countPerPage);
                        loadMsgList = roomMsgInfoPacket.Data.Message.ToList();
                    }
                    else
                    {
                        var roomMsgInfoPacket = await Services.Lobby.MessageListReqAsync(currentActivateRoomId, page+1 , fixedCountperPage);
                        loadMsgList.AddRange(roomMsgInfoPacket.Data.Message);
                    }
                }
            }
            else
            {
                if (messageListDict[currentActivateRoomId].Count == 0)
                {
                    var roomMsgInfoPacket = await Services.Lobby.MessageListReqAsync(currentActivateRoomId, 1, fixedCountperPage);
                    loadMsgList = roomMsgInfoPacket.Data.Message.ToList();
                }
                else
                {
                    isLoadMsgExist = true;
                    //값비교를 해야하므로 얕은복사(깊은복사는 필요없음)
                    loadMsgList =messageListDict[currentActivateRoomId].ToList();
                }
            }

            var currentRoomMsgList = messageListDict[chatRoomId];
            var existingIds = new HashSet<long>(currentRoomMsgList.Select(o => o.MessageId));
            if (isLoadMsgExist == false)
                loadMsgList.RemoveAll(b => existingIds.Contains(b.MessageId));
            
            foreach (var loaded in loadMsgList)
            {
                if (!existingIds.Contains(loaded.MessageId))
                {
                    currentRoomMsgList.Add(loaded);
                    existingIds.Add(loaded.MessageId); // 중복 방지용 set 업데이트
                }
            }

            var currentTotalMsgList = currentRoomMsgList.OrderBy(o => o.CreatedAt).ToList();
            var currentSortedLoadMsgList = loadMsgList.OrderByDescending(o => o.CreatedAt).ToList();
            
            foreach (var msginfo in currentSortedLoadMsgList)
            {
                var existingSlot = playerChatList.Find(o => o.myChatData.MessageId == msginfo.MessageId);
                if (existingSlot != null)
                {
                    // 이미 있는 슬롯은 데이터만 업데이트 (이모지 등 변경사항 반영)
                    bool isMe = msginfo.Uid == CPPlayer.UserInfo.userDatabase.User.Uid;
                    existingSlot.SetChat(isMe, msginfo, skipAnimation: true, skipEmojiAnimation: skipEmojiAnimation);
                    continue;
                }

                ChatCloudSlot chatCloudSlot = PoolManager.Pop(view.chatCloudPrefab, view.chatScrollview.content, Vector2.zero, 5);
                playerChatList.Add(chatCloudSlot);

                bool isMeNew = msginfo.Uid == CPPlayer.UserInfo.userDatabase.User.Uid;
                chatCloudSlot.SetChat(isMeNew, msginfo, skipAnimation: true, showTime: true);
                await UniTask.Yield();
                chatCloudSlot.transform.SetAsFirstSibling();
            }
         
            if (newloadMsgList.Count > 0)
            {
                foreach (var loaded in newloadMsgList)
                    currentRoomMsgList.Add(loaded);

                var newSortedMsgList = newloadMsgList.OrderBy(o => o.CreatedAt).ToList();
                foreach (var msginfo in newSortedMsgList)
                {
                    ChatCloudSlot chatCloudSlot = PoolManager.Pop(view.chatCloudPrefab, view.chatScrollview.content, Vector2.zero, 5);
                    playerChatList.Add(chatCloudSlot);

                    bool isMe = msginfo.Uid == CPPlayer.UserInfo.userDatabase.User.Uid;
                    chatCloudSlot.SetChat(isMe, msginfo, skipAnimation: false, showTime: true);
                    chatCloudSlot.transform.SetAsLastSibling();
                }
            }

            CPPlayer.OutGame.newMessageNotiCallback?.Invoke(chatRoomId, false);
      
            await UniTask.Yield();
            view.chatScrollview.gameObject.SetActive(true);

            List<ChatCloudSlot> chatlistObjs = new List<ChatCloudSlot>();
            var content = view.chatScrollview.content;
            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                    continue;

                var slot = child.GetComponent<ChatCloudSlot>();
                if (slot != null && slot.myChatData != null)
                    chatlistObjs.Add(slot);
            }

            chatlistObjs = chatlistObjs.OrderBy(o => o.myChatData.CreatedAt).ToList();

            // 같은 사람이 같은 분에 연속 메시지를 보내면 마지막에만 시간 표시
            for (int i = 0; i < chatlistObjs.Count; i++)
            {
                var currentSlot = chatlistObjs[i];
                bool showTime = true;
                
                if (i < chatlistObjs.Count - 1)
                {
                    var nextSlot = chatlistObjs[i + 1];
                    bool sameUser = currentSlot.myChatData.Uid == nextSlot.myChatData.Uid;
                    
                    DateTime currentTime = DateTimeOffset.FromUnixTimeSeconds(currentSlot.myChatData.CreatedAt).LocalDateTime;
                    DateTime nextTime = DateTimeOffset.FromUnixTimeSeconds(nextSlot.myChatData.CreatedAt).LocalDateTime;
                    bool sameMinute = currentTime.Year == nextTime.Year && 
                                      currentTime.Month == nextTime.Month && 
                                      currentTime.Day == nextTime.Day && 
                                      currentTime.Hour == nextTime.Hour && 
                                      currentTime.Minute == nextTime.Minute;
                    
                    if (sameUser && sameMinute)
                        showTime = false;
                }
                
                currentSlot.UpdateTimeDisplay(showTime);
            }

            string dateTime1 = "";

            if (chatlistObjs.Count > 0)
            {
                DateTime current = DateTimeOffset.FromUnixTimeSeconds(chatlistObjs[chatlistObjs.Count-1].myChatData.CreatedAt).LocalDateTime;
                string currentdateInfo = FormatDateWithDayOfWeek(current);
                dateTime1 = currentdateInfo;
            }
            
            //중간마다 인덱스 변화 때문에 역순으로
            for (int i = chatlistObjs.Count - 1; i >= 0; i--)
            {
                DateTime current = DateTimeOffset.FromUnixTimeSeconds(chatlistObjs[i].myChatData.CreatedAt).LocalDateTime;
                string currentdateInfo = FormatDateWithDayOfWeek(current);
                
                if (dateTime1 != currentdateInfo)
                {
                    ChatCloudDateSlot chatCloudDateSlot;

                    chatCloudDateSlot = PoolManager.Pop(view.chatDatePrefab, view.chatScrollview.content, Vector2.zero, 3);
                    chatdateList.Add(chatCloudDateSlot);
                    chatCloudDateSlot.SetDate(dateTime1);
                    chatCloudDateSlot.gameObject.SetActive(false);
                    await UniTask.Yield();
                    chatCloudDateSlot.gameObject.SetActive(true);
                    chatCloudDateSlot.transform.SetSiblingIndex(i + 1);
                    dateTime1 = currentdateInfo;
                }
            }

            // 가장 오래된 메시지 위에 해당 날짜 구분선 추가
            if (chatlistObjs.Count > 0)
            {
                ChatCloudDateSlot firstDateSlot = PoolManager.Pop(view.chatDatePrefab, view.chatScrollview.content, Vector2.zero, 3);
                chatdateList.Add(firstDateSlot);
                firstDateSlot.SetDate(dateTime1);
                firstDateSlot.gameObject.SetActive(false);
                await UniTask.Yield();
                firstDateSlot.gameObject.SetActive(true);
                firstDateSlot.transform.SetAsFirstSibling();
            }
            
            //render 초기화 위한 과정
            view.chatBox.gameObject.SetActive(true);
            //view.PopupAfterDeleteChat.SetActive(false);
            await UniTask.Yield();
            await UniTask.Yield();

            // verticalNormalizedPosition: 1 = 콘텐츠 상단(오래된 쪽), 0 = 하단(최신)
            if (newloadMsgList.Count > 0)
            {
                _allowChatLoadMoreFromTop = true;
                ScrollToBottomNextFrame().Forget();
            }
            else if (isOtherUser)
            {
                _allowChatLoadMoreFromTop = false;
                view.chatScrollview.verticalNormalizedPosition = 1f;
                ArmChatLoadMoreAfterTopOpenAsync().Forget();
            }
            else
                _allowChatLoadMoreFromTop = true;

            Canvas.ForceUpdateCanvases();
            isLoading = false;
        }

        private async UniTaskVoid ArmChatLoadMoreAfterTopOpenAsync()
        {
            await UniTask.DelayFrame(3);
            _allowChatLoadMoreFromTop = true;
        }

        private void ResetFreindScrollPosition()
        {
            view.friendsScrollView.normalizedPosition = new Vector2(0, 1);
        }

        private async UniTask CreateNewChatAndLoad(long friendAccountId)
        {
            var myUid = CPPlayer.UserInfo.userDatabase.User.Uid;
            var friendUid = friendAccountId;
            var charCreateRes = await Services.Lobby.ChatCreateAsync(myUid, friendUid);

            if (!charCreateRes.IsSuccess || charCreateRes.Data?.ChatRoom == null)
            {
                Debug.LogWarning($"[Chat] ChatCreate 실패: {charCreateRes.Error?.Code}");
                return;
            }

            long chatRoomId = charCreateRes.Data.ChatRoom.ChatroomId;
            currentActivateRoomId = chatRoomId;
            Debug.Log($"현재 룸아이디{currentActivateRoomId}");
            await SetChatWindow();
            view.friendsScrollView.movementType = playerRoomDict.Count >= 6 ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
            await UniTask.Yield();
        }

        private async UniTask SendMessage()
        {
            ChatCloudSlot.CloseAllEmotionBox();

            if (currentActivateRoomId < 0 || !playerRoomDict.ContainsKey(currentActivateRoomId))
                return;
            
            if (playerRoomDict[currentActivateRoomId].IsWithdrawal)
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.CannotMessageDeactivatedUser].StringToLocal, true));
                return;
            }
            
            if (string.IsNullOrEmpty(view.advancedInputField.Text))
                return;

            string message = view.advancedInputField.Text;
            if (string.IsNullOrWhiteSpace(message))
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlankOnlyNotAllowed].StringToLocal, true));
                return;
            }

            var sendMsgRes = await Services.Lobby.MessageSendReqAsync(currentActivateRoomId, message);

            if (sendMsgRes.IsSuccess)
            {
                view.advancedInputField.Text = "";
                if (isPortraitMode)
                    view.advancedInputField.Select();
                OpenChatWindow(currentActivateRoomId, 1, true).Forget();
            }
            else
            {
                string errorMsg = sendMsgRes.Error.Code switch
                {
                    ErrorCode.EMessageItemNotEnough => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MessageItemInsufficient].StringToLocal,
                    ErrorCode.EMessageIsTooLong => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MessageTooLong].StringToLocal,
                    ErrorCode.EMessageIsEmpty => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.EnterMessage].StringToLocal,
                    ErrorCode.EMessageUserNotFound => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.RecipientNotFound].StringToLocal,
                    ErrorCode.EMessageParticipantNotFound => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.UserNotInChatRoom].StringToLocal,
                    ErrorCode.EChatBlocked => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockedByRecipient].StringToLocal,
                    ErrorCode.EBanPermanent => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockedByRecipient].StringToLocal,
                    ErrorCode.EBanTemporary => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockedByRecipient].StringToLocal,
                    _ => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MessageSendFailed].StringToLocal
                };
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(errorMsg, true));
            }

        }

        public bool IsAccountPinned(string accountId)
        {
            const string key = "PinnedAccounts";

            string raw = PlayerPrefs.GetString(key, "");

            List<string> pinnedList = raw.Split(',').Where(id => !string.IsNullOrEmpty(id)).ToList();

            return pinnedList.Contains(accountId);
        }

        private async UniTask LoadBlockedUids()
        {
            var res = await Services.Lobby.ChatBlockListAsync();
            _blockedUids.Clear();
            if (res.IsSuccess && res.Data?.Block != null)
            {
                foreach (var b in res.Data.Block)
                    _blockedUids.Add(b.Uid);
            }
        }

        private void OnBlockFromChat(long uid)
        {
            _blockedUids.Add(uid);
            SetChatWindow().Forget();
        }

        private void OnUnblockFromChat(long uid)
        {
            UnblockFromChat(uid).Forget();
        }

        private async UniTask UnblockFromChat(long uid)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
            var result = await Services.Lobby.ChatBlockAsync(uid, 1);
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));

            if (result.IsSuccess)
            {
                _blockedUids.Remove(uid);
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockReleased].StringToLocal, false));
                await SetChatWindow();
            }
            else
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockReleaseFailed].StringToLocal, false));
            }
        }

        private void OnDeleteFromSwipe(long roomId)
        {
            DeleteConversation(roomId).Forget();
        }

        private async UniTask DeleteConversation(long targetRoomId = -1)
        {
            if (targetRoomId < 0)
                targetRoomId = currentActivateRoomId;
            
            try
            {
                Debug.Log($"채팅 삭제 시도 룸아이디{targetRoomId}");
                var chatDeletePacket = await Services.Lobby.ChatExitReqAsync(targetRoomId);
                if (chatDeletePacket != null)
                {
                    if (playerRoomDict.ContainsKey(targetRoomId))
                    {
                        playerRoomDict[targetRoomId].gameObject.SetActive(false);
                        playerRoomDict.Remove(targetRoomId);
                    }
                    
                    if (playerRoomDict.Count == 0)
                    {
                        currentActivateRoomId = -1;
                        view.chatBox.gameObject.SetActive(false);
                        view.emptyWindow.SetActive(true);
                    }
                    else
                    {
                        if (targetRoomId == currentActivateRoomId)
                        {
                            currentActivateRoomId = playerRoomDict.Keys.First();
                            SetChatWindow().Forget();
                        }
                    }

                    view.friendsScrollView.movementType = playerRoomDict.Count >= 6 ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ChatDeleted].StringToLocal, false));
                }
                else
                {
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ChatDeleteFailedServerError].StringToLocal, true));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ChatDeleteFailed].StringToLocal}{e}", true));
                throw;
            }
        }


        //chat 세로 가로모드 변환 설정

        #region ChatModeSetting

        private bool isPortraitMode = false;
        private Canvas chatCanvas;
        private UITop _uiTop;
        private void ToggleTopUI(bool visible)
        {
            if (_uiTop == null)
            {
#if UNITY_2022_2_OR_NEWER
                var found = UnityEngine.Object.FindObjectsByType<UITop>(
                    UnityEngine.FindObjectsInactive.Include,
                    UnityEngine.FindObjectsSortMode.None);
                if (found != null && found.Length > 0)
                {
                    _uiTop = found[0];
                }
#else
                var all = UnityEngine.Resources.FindObjectsOfTypeAll<UITop>();
                if (all != null && all.Length > 0)
                {
                    _uiTop = all[0];
                }
#endif
            }

            if (_uiTop != null)
            {
                _uiTop.SetVisible(visible);
            }
        }

        private async UniTask SetChatModeAsync()
        {
            CPFixedAspectScaler.portraitMode = true;
            isPortraitMode = true;
            view.chatWindowParent.gameObject.SetActive(true);
            ToggleTopUI(false);

            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;

            // 화면 전환 완료 대기 (에뮬레이터 호환성)
            await UniTask.DelayFrame(5);

            // 대기 중 뒤로가기 등으로 View가 닫혔으면 가로모드 복구 후 중단
            if (!view.gameObject.activeInHierarchy)
            {
                SetNormalChatMode();
                return;
            }

            foreach (var _canvas in ViewCanvas.AllActivateCanvas)
            {
                _canvas.Value.aspectScaler.scalerWrapper.anchorMin = Vector2.zero;
                _canvas.Value.aspectScaler.scalerWrapper.anchorMax = Vector2.one;
                _canvas.Value.aspectScaler.scalerWrapper.offsetMin = Vector2.zero;
                _canvas.Value.aspectScaler.scalerWrapper.offsetMax = Vector2.zero;
                _canvas.Value.aspectScaler.scalerWrapper.pivot = new Vector2(0.5f, 0.5f);
                _canvas.Value.aspectScaler.scalerWrapper.localScale = Vector3.one;
                _canvas.Value.aspectScaler.canvasScaler.matchWidthOrHeight = 0.5f;
            }

            view.chatBox.SetParent(view.chatWindowParent.transform, false);
            view.chatBox.anchorMin = Vector2.zero;
            view.chatBox.anchorMax = Vector2.one;
            view.chatBox.offsetMin = Vector2.zero;
            view.chatBox.offsetMax = Vector2.zero;
            view.chatBox.pivot = new Vector2(0.5f, 0.5f);
            view.chatBox.localScale = Vector3.one;

            view.goToPortraitChatBtn.gameObject.SetActive(false);
            view.backToNormalChatBtn.gameObject.SetActive(true);

            if (chatCanvas == null)
                chatCanvas = view.GetComponentInParent<Canvas>();

            NativeKeyboardManager.AddKeyboardHeightChangedListener(OnKeyboardHeightChanged);
        }

        private async UniTask ScrollToBottomNextFrame()
        {
            await UniTask.Yield();

            LayoutRebuilder.ForceRebuildLayoutImmediate(view.chatScrollview.content);
            Canvas.ForceUpdateCanvases();
            view.chatScrollview.verticalNormalizedPosition = 0f;
        }

        private void SetNormalChatMode()
        {
            CPFixedAspectScaler.portraitMode = false;
            isPortraitMode = false;
            view.chatBoxParent.gameObject.SetActive(true);
            ToggleTopUI(true);

            NativeKeyboardManager.RemoveKeyboardHeightChangedListener(OnKeyboardHeightChanged);
            view.chatBox.offsetMin = Vector2.zero;
            view.chatBox.offsetMax = Vector2.zero;

            Screen.orientation = ScreenOrientation.LandscapeLeft;

            view.chatBox.SetParent(view.chatBoxParent.transform, false);
            view.chatBox.anchorMin = new Vector2(1f, 0f);
            view.chatBox.anchorMax = new Vector2(1f, 1f);
            view.chatBox.pivot = new Vector2(1f, 0.5f);
            view.chatBox.offsetMin = new Vector2(-1310f, 0f);
            view.chatBox.offsetMax = new Vector2(0f, 0f);
            view.chatBox.anchoredPosition = new Vector2(0f, 0f);

            EnableLandscapeAutoRotationNextFrame().Forget();

            view.chatWindowParent.gameObject.SetActive(false);
            view.goToPortraitChatBtn.gameObject.SetActive(true);
            view.backToNormalChatBtn.gameObject.SetActive(false);
        }

        private void OnKeyboardHeightChanged(int keyboardHeight)
        {
            if (!isPortraitMode)
                return;

            if (keyboardHeight > 0 && chatCanvas != null)
            {
                float adjustedHeight = keyboardHeight / chatCanvas.scaleFactor;
                view.chatBox.offsetMin = new Vector2(0, adjustedHeight);
                view.chatBox.offsetMax = Vector2.zero;
            }
            else
            {
                view.chatBox.offsetMin = Vector2.zero;
                view.chatBox.offsetMax = Vector2.zero;
            }
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(view.chatScrollview.content);
            Canvas.ForceUpdateCanvases();
            view.chatScrollview.verticalNormalizedPosition = 0f;
        }

        private async UniTask EnableLandscapeAutoRotationNextFrame()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            await UniTask.Yield();

            Screen.fullScreen = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(view.chatScrollview.content);

            view.chatScrollview.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
        }

        #endregion
        
        private string FormatDateWithDayOfWeek(DateTime dateTime)
        {
            string[] dayNames = { StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Sunday].StringToLocal, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Monday].StringToLocal, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Tuesday].StringToLocal, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Wednesday].StringToLocal, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Thursday].StringToLocal, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Friday].StringToLocal, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Saturday].StringToLocal };
            string dayOfWeek = dayNames[(int)dateTime.DayOfWeek];
            return $"{dateTime.Year}.{dateTime.Month}.{dateTime.Day}";
        }
    }
}