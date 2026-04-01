using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Definition;
using Cysharp.Threading.Tasks;
using CAPYBARA.lobby;
using DG.Tweening;

namespace CAPYBARA.Core
{
    public class ControllerFriends
    {
        public enum ButtonType
        {
            FriendsList,
            RequestFriendsList,
        }
        
        public enum ButtonTypeInRequest
        {
            ReceivedFriendRequest,
            SentFriendRequest,
        }
        
        public enum FriendListType
        {
            Friends,
            Blocked
        }
        
        private class FoundUserData
        {
            public lobby.Friends Friend;
            public bool IsAlreadyFriendOrSent;
            public bool IsAlreadyReceived;
            public bool IsBlocked;
        }
        
        ViewFriends view;
        CancellationTokenSource cts;

        private List<lobby.Friends> _friendDataList = new List<lobby.Friends>();
        private List<lobby.BlockUserInfo> _blockedUserDataList = new List<lobby.BlockUserInfo>();
        private List<lobby.Friends> _sentReqDataList = new List<lobby.Friends>();
        private List<lobby.Friends> _receivedReqDataList = new List<lobby.Friends>();
        private List<FoundUserData> _foundUserDataList = new List<FoundUserData>();
        private HashSet<long> _blockedUids = new HashSet<long>();
        
        List<lobby.Friends> currentFriendList = new List<lobby.Friends>();
        List<lobby.Friends> currentSentFriendList = new List<lobby.Friends>();
        List<lobby.Friends> currentRecivedFriendList = new List<lobby.Friends>();
        private string currentKeyword;
        ButtonType currentbuttonType;
        ButtonTypeInRequest currentbuttonTypeInRequest;
        FriendListType currentFriendListType = FriendListType.Friends;

        private Tweener mainScrollTween;
        private Tweener scrollTween;
        private int currentTabIndex = 0;
        private int currentTabIndexInRequest = 0;
        private float _savedFoundUserScrollNormalizedY = 1f;
        private Vector2 _savedFriendsListScrollPosition = Vector2.zero;
        private Vector2 _savedBlockedListScrollPosition = Vector2.zero;
        private const int RecommendedPageSize = 50;
        private int _recommendedCurrentPage;
        private bool _isLoadingRecommendedPage;
        private bool _hasMoreRecommendedPages;
        private bool _isRecommendedMode;
        private readonly HashSet<long> _recommendedLoadedUids = new HashSet<long>();
        private bool _hasViewedReceivedRequests;
        private HashSet<long> _seenReceivedRequestUids = new HashSet<long>();
        public ControllerFriends(ViewFriends _view, CancellationTokenSource _cts)
        {
            view = _view;
            cts = _cts;

            view.onScrollDragBegin += StopScrollAnimation;
            view.onDropdownOutsideClick += () => SetDropdownOpen(false);

            Init();
        }

        void Init()
        {
            CPPlayer.OutGame.openFriends += () => OpenFriendsWithTab((int)ButtonType.FriendsList).Forget();
            CPPlayer.OutGame.openFriendsWithTab += tabIndex => OpenFriendsWithTab(tabIndex).Forget();
            CPPlayer.OutGame.refreshFriendsList += () => RefreshCurrentList().Forget();

            CPPlayer.OutGame.CreateConversationFriend += CreateConversation;
            CPPlayer.OutGame.HideFriendsForChat += HideFriendsViewIfOpen;
            CPPlayer.OutGame.ShowFriendsViewQuiet += ShowFriendsViewQuiet;
            CPPlayer.OutGame.CancelRequestFriend += s => {CancelRequestFriend(s).Forget(); };
            CPPlayer.OutGame.AcceptRequestFriend += s => {AcceptRequestFriend(s).Forget(); };;
            CPPlayer.OutGame.RejectRequestFriend += s => {RejectRequestFriend(s).Forget(); };
            CPPlayer.OutGame.RequestFriend += (s, btn, overlay) => { RequestFriend(s, btn, overlay).Forget(); };

            view.uiTabGroup.onIndexChanged += OnTabChangedEvent;
            view.findUser.onClick.AddListener(OnClickFindUser);

            view.uiTabGroupInRequest.onIndexChanged += OnTabChangedEventInRequest;
            view.emptyRecievedReqFriends.SetActive(false);
            view.emptySentReqFriends.SetActive(false);
            view.emptyFoundUsers.SetActive(true);
            view.closeButton.onClick.AddListener(() =>
            {
                ClearNotiIfViewed();
                view.gameObject.SetActive(false);
            });

            view.friendScrollView.OnCellUpdate = OnFriendCellUpdate;
            view.friendSentReqScrollView.OnCellUpdate = OnSentReqCellUpdate;
            view.friendRecievedReqScrollView.OnCellUpdate = OnReceivedReqCellUpdate;
            view.friendFoundScrollView.OnCellUpdate = OnFoundUserCellUpdate;
            var foundScrollRect = view.friendFoundScrollView.GetComponent<ScrollRect>();
            if (foundScrollRect != null)
                foundScrollRect.onValueChanged.AddListener(OnFoundUsersScrollChanged);

            CPPlayer.OutGame.newFriendRequestNotiCallback += NewFriendRequestNotiCallback;
            CPPlayer.OutGame.friendRequestTypeNotiCallback += OnFriendsRequestTypeNoti;

            InitInviteFriend();
            InitListTypeDropdown();
        }
        
        void InitListTypeDropdown()
        {
            SetDropdownOpen(false);
            
            if (view.listTypeDropdownButton != null)
            {
                view.listTypeDropdownButton.onClick.AddListener(() =>
                {
                    bool isOpen = view.dropdownPanel != null && view.dropdownPanel.activeSelf;
                    SetDropdownOpen(!isOpen);
                });
            }
            
            
            if (view.friendListButton != null)
            {
                view.friendListButton.onClick.AddListener(() =>
                {
                    OnSelectListType(FriendListType.Friends);
                });
            }
            
            if (view.blockListButton != null)
            {
                view.blockListButton.onClick.AddListener(() =>
                {
                    OnSelectListType(FriendListType.Blocked);
                });
            }
            
            UpdateListTypeUI();
        }
        
        void SetDropdownOpen(bool open)
        {
            if (view.dropdownPanel != null)
                view.dropdownPanel.SetActive(open);

            if (view.dropdownArrow != null)
                view.dropdownArrow.localRotation = Quaternion.Euler(0, 0, open ? 180f : 0f);
        }
        
        void OnSelectListType(FriendListType listType)
        {
            SetDropdownOpen(false);
            
            if (currentFriendListType == listType)
                return;

            SaveMainListScrollPosition(currentFriendListType);
            
            currentFriendListType = listType;
            UpdateListTypeUI();
            
            if (listType == FriendListType.Friends)
                GetFriendslist().Forget();
            else
                GetBlockedUserList().Forget();
        }

        void SaveMainListScrollPosition(FriendListType listType)
        {
            var rect = view.friendScrollView != null ? view.friendScrollView.GetComponent<ScrollRect>() : null;
            if (rect?.content == null)
                return;

            if (listType == FriendListType.Friends)
                _savedFriendsListScrollPosition = rect.content.anchoredPosition;
            else
                _savedBlockedListScrollPosition = rect.content.anchoredPosition;
        }
        
        void UpdateListTypeUI()
        {
            bool isFriendList = currentFriendListType == FriendListType.Friends;
            
            if (view.listTypeText != null)
                view.listTypeText.text = isFriendList ? StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FriendList].StringToLocal : StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockList].StringToLocal;

            if (view.friendListCheck != null)
                view.friendListCheck.SetActive(isFriendList);
            if (view.blockListCheck != null)
                view.blockListCheck.SetActive(!isFriendList);
        }


        void OnTabChangedEvent(int buttonType)
        {
            if (currentTabIndex == buttonType)
            {
                AnimateScrollToStart();
                return;
            }

            if (currentTabIndex == (int)ButtonType.FriendsList)
                SaveMainListScrollPosition(currentFriendListType);

            if (currentTabIndex == (int)ButtonType.RequestFriendsList)
            {
                var rect = view.friendFoundScrollView.GetComponent<ScrollRect>();
                if (rect != null)
                    _savedFoundUserScrollNormalizedY = rect.normalizedPosition.y;
            }

            currentTabIndex = buttonType;
            currentbuttonType = (ButtonType)buttonType;

            switch (currentbuttonType)
            {
                case ButtonType.FriendsList:
                    OnTabChangedFriendsList().Forget();
                    break;
                case ButtonType.RequestFriendsList:
                    OnTabChangedRequestFriendsList().Forget();
                    break;
            }
        }
        
        private async UniTask OnTabChangedFriendsList()
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
            await RefreshCurrentList();
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
        }
        
        private async UniTask OnTabChangedRequestFriendsList()
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
            
            int savedTabIndex = Mathf.Max(currentTabIndexInRequest, 0);
            
            currentTabIndexInRequest = -1;
            view.uiTabGroupInRequest.SetActiveToggle(savedTabIndex, false);
            await OnTabChangedEventInRequestAsync(savedTabIndex);

            if (savedTabIndex == (int)ButtonTypeInRequest.ReceivedFriendRequest)
            {
                view.friendSentReqScrollView.SaveScrollPosition();
                await GetSentFriendsRequestlist();
                view.emptySentReqFriends.SetActive(false);
            }
            else
            {
                await GetReceivedFriendsRequestlist();
                view.emptyRecievedReqFriends.SetActive(false);
            }

            await GetRecommendedUsers();
            
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
        }
        
        private void OnTabChangedEventInRequest(int buttonType)
        {
            if (currentTabIndexInRequest == buttonType && currentbuttonType == ButtonType.RequestFriendsList)
            {
                var subScrollView = currentTabIndexInRequest switch
                {
                    0 => view.friendRecievedReqScrollView,
                    1 => view.friendSentReqScrollView,
                    _ => view.friendRecievedReqScrollView
                };
                AnimateScrollToStart(subScrollView, ref scrollTween);
                return;
            }

            OnTabChangedEventInRequestWithLoading(buttonType).Forget();
        }
        
        private async UniTask OnTabChangedEventInRequestWithLoading(int buttonType)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
            await OnTabChangedEventInRequestAsync(buttonType);
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
        }
        
        private async UniTask OnTabChangedEventInRequestAsync(int buttonType)
        {
            currentTabIndexInRequest = buttonType;
            currentbuttonTypeInRequest = (ButtonTypeInRequest)buttonType;

            view.emptyRecievedReqFriends.SetActive(false);
            view.emptySentReqFriends.SetActive(false);

            switch (currentbuttonTypeInRequest)
            {
                case ButtonTypeInRequest.ReceivedFriendRequest:
                    view.friendRecievedReqScrollView.SaveScrollPosition();
                    await GetReceivedFriendsRequestlist();
                    _hasViewedReceivedRequests = true;
                    break;
                case ButtonTypeInRequest.SentFriendRequest:
                    view.friendSentReqScrollView.SaveScrollPosition();
                    await GetSentFriendsRequestlist();
                    break;
            }
        }

        private async UniTask RefreshCurrentList()
        {
            if (currentFriendListType == FriendListType.Friends)
                await GetFriendslist();
            else
                await GetBlockedUserList();
        }
        
        private async UniTask GetFriendslist()
        {
            currentFriendListType = FriendListType.Friends;
            UpdateListTypeUI();
            
            var packetRes = await Services.Lobby.FriendsListReqAsync(FriendsListType.Friendslist);
            
            if (!packetRes.IsSuccess)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ServerErrorWithReason].StringToLocal}{packetRes.Error}"));
                return;
            }

            var friendsRes = packetRes.Data;

            if (friendsRes == null || friendsRes.Friends.Count == 0)
            {
                _friendDataList.Clear();
                currentFriendList.Clear();
                view.friendScrollView.SetItemCount(0);
                UpdateScrollMovementType(view.friendScrollView, 0, 5);
                view.emptyFriends.SetActive(true);
                if (view.emptyBlockedUsers != null)
                    view.emptyBlockedUsers.SetActive(false);
                view.friendCountText.text = "(0/100)";
                return;
            }
            
            var sortedFriendList = friendsRes.Friends.OrderByDescending(f => f.IsOnline).ThenByDescending(f => f.Gold).ToList();

            _friendDataList = sortedFriendList;
            currentFriendList = sortedFriendList;
            
            view.friendScrollView.SaveScrollPosition(_savedFriendsListScrollPosition);
            view.friendScrollView.SetItemCount(_friendDataList.Count);
            UpdateScrollMovementType(view.friendScrollView, _friendDataList.Count, 5);
            view.emptyFriends.SetActive(false);
            if (view.emptyBlockedUsers != null)
                view.emptyBlockedUsers.SetActive(false);
            view.friendCountText.text = $"({_friendDataList.Count}/100)";
            
            await UniTask.Yield();
        }
        
        private async UniTask GetBlockedUserList()
        {
            currentFriendListType = FriendListType.Blocked;
            UpdateListTypeUI();
            
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
            
            var packetRes = await Services.Lobby.ChatBlockListAsync();
            
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
            
            if (!packetRes.IsSuccess)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ServerErrorWithReason].StringToLocal}{packetRes.Error}"));
                return;
            }

            var blockListRes = packetRes.Data;

            if (blockListRes == null || blockListRes.Block.Count == 0)
            {
                _blockedUserDataList.Clear();
                view.friendScrollView.SetItemCount(0);
                UpdateScrollMovementType(view.friendScrollView, 0, 5);
                view.emptyFriends.SetActive(false);
                if (view.emptyBlockedUsers != null)
                    view.emptyBlockedUsers.SetActive(true);
                view.friendCountText.text = "(0/100)";
                return;
            }
            
            _blockedUserDataList = blockListRes.Block.ToList();
            
            view.friendScrollView.SaveScrollPosition(_savedBlockedListScrollPosition);
            view.friendScrollView.SetItemCount(_blockedUserDataList.Count);
            UpdateScrollMovementType(view.friendScrollView, _blockedUserDataList.Count, 5);
            view.emptyFriends.SetActive(false);
            if (view.emptyBlockedUsers != null)
                view.emptyBlockedUsers.SetActive(false);
            view.friendCountText.text = $"({_blockedUserDataList.Count}/100)";
            
            await UniTask.Yield();
        }

        private async UniTask GetSentFriendsRequestlist()
        {
            var packetRes = await Services.Lobby.FriendsListReqAsync(FriendsListType.Requestlist);
            if (!packetRes.IsSuccess)
                return;
            var sentFriendsRes = packetRes.Data;
            
            currentSentFriendList.Clear();
            
            if (sentFriendsRes == null || sentFriendsRes.Friends.Count == 0)
            {
                _sentReqDataList.Clear();
                view.friendSentReqScrollView.SetItemCount(0);
                UpdateScrollMovementType(view.friendSentReqScrollView, 0, 5);
                view.emptySentReqFriends.SetActive(true);
                return;
            }
            
            _sentReqDataList = sentFriendsRes.Friends.ToList();
            currentSentFriendList = _sentReqDataList;
            
            view.friendSentReqScrollView.SetItemCount(_sentReqDataList.Count);
            UpdateScrollMovementType(view.friendSentReqScrollView, _sentReqDataList.Count, 5);
            view.emptySentReqFriends.SetActive(false);

            await UniTask.Yield();
        }
        
        private async UniTask GetReceivedFriendsRequestlist()
        {
            var packetRes = await Services.Lobby.FriendsListReqAsync(FriendsListType.Receivedlist);
            if (!packetRes.IsSuccess)
                return;    
            var recievedFriendsRes = packetRes.Data;
            
            currentRecivedFriendList.Clear();
            
            if (recievedFriendsRes == null || recievedFriendsRes.Friends.Count == 0)
            {
                _receivedReqDataList.Clear();
                view.friendRecievedReqScrollView.SetItemCount(0);
                UpdateScrollMovementType(view.friendRecievedReqScrollView, 0, 5);
                view.emptyRecievedReqFriends.SetActive(true);
                return;
            }
            
            var allRequests = recievedFriendsRes.Friends.ToList();
            
            bool hasNoti = CPPlayer.OutGame.hasNewFriendRequestNoti
                || view.newFriendRequestNotiObj_topTap.activeSelf
                || view.newFriendRequestNotiObj_midTap.activeSelf;
            if (!hasNoti)
            {
                foreach (var f in allRequests)
                    _seenReceivedRequestUids.Add(f.FriendsUid);
            }

            if (hasNoti)
            {
                var unseen = allRequests
                    .Where(f => !_seenReceivedRequestUids.Contains(f.FriendsUid))
                    .OrderByDescending(f => f.CreatedAt).ToList();
                var seen = allRequests
                    .Where(f => _seenReceivedRequestUids.Contains(f.FriendsUid)).ToList();
                _receivedReqDataList = unseen.Concat(seen).ToList();
            }
            else
            {
                _receivedReqDataList = allRequests;
            }
            currentRecivedFriendList = _receivedReqDataList;
            
            view.friendRecievedReqScrollView.SetItemCount(_receivedReqDataList.Count);
            UpdateScrollMovementType(view.friendRecievedReqScrollView, _receivedReqDataList.Count, 5);
            view.emptyRecievedReqFriends.SetActive(false);
            
            await UniTask.Yield();
        }

        private async UniTask FindUserAndListing(string keyword)
        {
            currentKeyword = keyword;
            _isRecommendedMode = false;
            _hasMoreRecommendedPages = false;

            var findUserRes = await Services.Lobby.FriendsFindReqAsync(keyword);
            if (!findUserRes.IsSuccess)
            {
                if (findUserRes.Error.Code == ErrorCode.EInvalidParameter)
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MinTwoCharactersRequired].StringToLocal, true));
                else
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SearchError].StringToLocal, true));

                return;
            }

            view.friendFoundScrollView.ScrollToTop();

            if (findUserRes.Data.Friends.Count == 0)
            {
                _foundUserDataList.Clear();
                view.friendFoundScrollView.SetItemCount(0);
                UpdateScrollMovementType(view.friendFoundScrollView, 0, 4);
                view.emptyFoundUsers.SetActive(true);
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.NoSearchResults].StringToLocal, true));
                view.inputField.text = "";
                return;
            }

            _foundUserDataList.Clear();
            foreach (var friend in findUserRes.Data.Friends)
            {
                bool isAlreadyFriend = currentFriendList.Any(o => o.FriendsUid == friend.FriendsUid);
                bool isAlreadySentFriend = currentSentFriendList.Any(o => o.FriendsUid == friend.FriendsUid);
                bool isAlreadyRecievedFriend = currentRecivedFriendList.Any(o => o.FriendsUid == friend.FriendsUid);

                _foundUserDataList.Add(new FoundUserData
                {
                    Friend = friend,
                    IsAlreadyFriendOrSent = isAlreadyFriend || isAlreadySentFriend,
                    IsAlreadyReceived = isAlreadyRecievedFriend,
                    IsBlocked = _blockedUids.Contains(friend.FriendsUid)
                });
            }

            view.friendFoundScrollView.SetItemCount(_foundUserDataList.Count);
            UpdateScrollMovementType(view.friendFoundScrollView, _foundUserDataList.Count, 4);
            view.emptyFoundUsers.SetActive(false);
            view.inputField.text = "";
        }

        private async UniTask GetRecommendedUsers()
        {
            view.emptyFoundUsers.SetActive(false);

            _isRecommendedMode = true;
            _recommendedCurrentPage = 0;
            _hasMoreRecommendedPages = true;
            _isLoadingRecommendedPage = false;
            _recommendedLoadedUids.Clear();
            _foundUserDataList.Clear();
            view.friendFoundScrollView.SetItemCount(0);
            UpdateScrollMovementType(view.friendFoundScrollView, 0, 4);

            await LoadMoreRecommendedUsers();
            currentKeyword = "";

            var scrollRect = view.friendFoundScrollView.GetComponent<ScrollRect>();
            if (scrollRect != null)
                scrollRect.normalizedPosition = new Vector2(scrollRect.normalizedPosition.x, _savedFoundUserScrollNormalizedY);
        }

        private async UniTask LoadMoreRecommendedUsers()
        {
            if (_isLoadingRecommendedPage || !_hasMoreRecommendedPages)
                return;

            _isLoadingRecommendedPage = true;

            int page = _recommendedCurrentPage;
            var topPointsRes = await Services.Lobby.FriendsTopPointsReqAsync(RecommendedPageSize, page);
            if (!topPointsRes.IsSuccess)
            {
                if (page == 0)
                    view.emptyFoundUsers.SetActive(true);
                _hasMoreRecommendedPages = false;
                _isLoadingRecommendedPage = false;
                return;
            }

            var serverFriends = topPointsRes.Data?.Friends;
            if (serverFriends == null || serverFriends.Count == 0)
            {
                if (page == 0)
                    view.emptyFoundUsers.SetActive(true);
                _hasMoreRecommendedPages = false;
                _isLoadingRecommendedPage = false;
                return;
            }

            var myUid = CPPlayer.UserInfo.userDatabase.User.Uid;
            int addedCount = 0;
            foreach (var friend in serverFriends)
            {
                if (friend.FriendsUid == myUid)
                    continue;
                if (!_recommendedLoadedUids.Add(friend.FriendsUid))
                    continue;

                bool isAlreadyFriend = currentFriendList.Any(o => o.FriendsUid == friend.FriendsUid);
                bool isAlreadySentFriend = currentSentFriendList.Any(o => o.FriendsUid == friend.FriendsUid);
                bool isAlreadyRecievedFriend = currentRecivedFriendList.Any(o => o.FriendsUid == friend.FriendsUid);

                _foundUserDataList.Add(new FoundUserData
                {
                    Friend = friend,
                    IsAlreadyFriendOrSent = isAlreadyFriend || isAlreadySentFriend,
                    IsAlreadyReceived = isAlreadyRecievedFriend,
                    IsBlocked = _blockedUids.Contains(friend.FriendsUid)
                });
                addedCount++;
            }

            _hasMoreRecommendedPages = serverFriends.Count > 0;
            if (_hasMoreRecommendedPages)
                _recommendedCurrentPage++;

            if (addedCount > 0)
            {
                view.friendFoundScrollView.SaveScrollPosition();
                view.friendFoundScrollView.SetItemCount(_foundUserDataList.Count);
                UpdateScrollMovementType(view.friendFoundScrollView, _foundUserDataList.Count, 4);
                view.emptyFoundUsers.SetActive(false);
            }
            else if (_hasMoreRecommendedPages)
            {
                _isLoadingRecommendedPage = false;
                await LoadMoreRecommendedUsers();
                return;
            }
            else if (page == 0 && _foundUserDataList.Count == 0)
            {
                view.emptyFoundUsers.SetActive(true);
            }

            _isLoadingRecommendedPage = false;
        }

        private void OnFoundUsersScrollChanged(Vector2 scrollPos)
        {
            if (!_isRecommendedMode || !_hasMoreRecommendedPages || _isLoadingRecommendedPage)
                return;
            if (!view.gameObject.activeInHierarchy || currentbuttonType != ButtonType.RequestFriendsList)
                return;

            if (scrollPos.y <= 0.05f)
                LoadMoreRecommendedUsers().Forget();
        }

        private void CreateConversation(long friendAccountId)
        {
            ClearNotiIfViewed();
        }

        private void HideFriendsViewIfOpen()
        {
            if (view == null || !view.gameObject.activeSelf)
                return;

            CPPlayer.OutGame.ReturnToFriendsWhenChatCloses = true;
            view.gameObject.SetActive(false);
        }

        private void ShowFriendsViewQuiet()
        {
            if (view != null)
                view.gameObject.SetActive(true);
        }
        
        private async UniTask CancelRequestFriend(long reqId)
        {
            var res=await Services.Lobby.FriendsRequestAsync(FriendsRequestType.Cancel,reqId);
            
            GetSentFriendsRequestlist().Forget();
        }
        
        private async UniTask AcceptRequestFriend(long reqId)
        {
            var res=await Services.Lobby.FriendsRequestAsync(FriendsRequestType.Friends,reqId);
            _seenReceivedRequestUids.Remove(reqId);
            await GetReceivedFriendsRequestlist();
        }
        
        private async UniTask RejectRequestFriend(long reqId)
        {
            var res=await Services.Lobby.FriendsRequestAsync(FriendsRequestType.Reject,reqId);
            _seenReceivedRequestUids.Remove(reqId);
            GetReceivedFriendsRequestlist().Forget();
        }
        
        private async UniTask RequestFriend(long friendAccountId, GameObject addObj, GameObject overlayObj)
        {
            var res=await Services.Lobby.FriendsRequestAsync(FriendsRequestType.Request,friendAccountId);
            if (!res.IsSuccess)
            {
                if (res.Error.Code == ErrorCode.EFriendsBlocked)
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.GetLobbyErrorMessage(ErrorCode.EFriendsBlocked), true));
                else if (res.Error.Code == ErrorCode.EFriendsMeBlocker)
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.GetLobbyErrorMessage(ErrorCode.EFriendsMeBlocker), true));
                return;
            }

            var result = res.Data;
            var btn = addObj.GetComponent<CAPYBARA.Bundles.CPButton>();
            if (btn != null)
                btn.interactable = false;
            else
            {
                var unityBtn = addObj.GetComponent<Button>();
                if (unityBtn != null)
                    unityBtn.interactable = false;
            }
            if (overlayObj != null)
                overlayObj.SetActive(true);

            var found = _foundUserDataList.FirstOrDefault(f => f.Friend.FriendsUid == friendAccountId);
            if (found != null)
            {
                found.IsAlreadyFriendOrSent = true;
                if (view.friendFoundScrollView.TotalCount == _foundUserDataList.Count)
                    view.friendFoundScrollView.RefreshAllCells();
            }

            await GetSentFriendsRequestlist();
        }
        
        private void ClearNotiIfViewed()
        {
            if (_hasViewedReceivedRequests)
            {
                foreach (var req in _receivedReqDataList)
                    _seenReceivedRequestUids.Add(req.FriendsUid);
                
                CPPlayer.OutGame.pendingFriendRequestNotiUids.Clear();
                CPPlayer.OutGame.newFriendRequestNotiCallback?.Invoke(false);
                _hasViewedReceivedRequests = false;
            }
        }

        private void NewFriendRequestNotiCallback(bool ison)
        {
            CPPlayer.OutGame.hasNewFriendRequestNoti = ison;
            view.newFriendRequestNotiObj_topTap.SetActive(ison);
            view.newFriendRequestNotiObj_midTap.SetActive(ison);
        }

        private void OnFriendsRequestTypeNoti(FriendsRequestType requestType)
        {
            if (!view.gameObject.activeInHierarchy)
                return;

            switch (requestType)
            {
                case FriendsRequestType.Request:
                case FriendsRequestType.Cancel:
                    GetReceivedFriendsRequestlist().Forget();
                    break;
                case FriendsRequestType.Friends:
                case FriendsRequestType.Reject:
                    GetSentFriendsRequestlist().Forget();
                    break;
            }
        }

        private async UniTask OpenFriendsWithTab(int tabIndex)
        {
            _hasViewedReceivedRequests = false;
            view.gameObject.SetActive(false);
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            await LoadBlockedUids();

            int clampedIndex = Mathf.Clamp(tabIndex, 0, view.uiTabGroup.Toggles.Count - 1);
            currentTabIndex = clampedIndex;
            currentbuttonType = (ButtonType)clampedIndex;
            currentTabIndexInRequest = 0;
            view.uiTabGroup.SetActiveToggle(clampedIndex, false);
            view.uiTabGroupInRequest.SetActiveToggle(0, false);
            ResetAllScrollPositions();

            if (currentTabIndex == (int)ButtonType.FriendsList)
            {
                await GetFriendslist();
            }
            else
            {
                currentTabIndexInRequest = -1; // 강제로 리스트 로드되도록 초기화
                view.uiTabGroupInRequest.SetActiveToggle(0, false);
                await GetFriendslist();
                await OnTabChangedEventInRequestAsync(0);
                await GetSentFriendsRequestlist();
                view.emptySentReqFriends.SetActive(false);
                await GetRecommendedUsers();
            }

            LoadInviteData().Forget();

            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));

            view.gameObject.SetActive(true);
        }

        private void OnFriendCellUpdate(GameObject cell, int index)
        {
            var slot = cell.GetComponent<FriendSlot>();
            
            if (currentFriendListType == FriendListType.Friends)
            {
                if (index < 0 || index >= _friendDataList.Count)
                    return;
                
                slot.Init(_friendDataList[index]);
            }
            else
            {
                if (index < 0 || index >= _blockedUserDataList.Count)
                    return;
                
                slot.InitBlocked(_blockedUserDataList[index], OnUnblockUser);
            }
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

        private void OnBlockFromReceivedRequest(long uid)
        {
            _blockedUids.Add(uid);
            RejectRequestFriend(uid).Forget();
        }

        private void OnBlockFromSentRequest(long uid)
        {
            _blockedUids.Add(uid);
            CancelRequestFriend(uid).Forget();
        }

        private void OnBlockFromFoundUser(long uid)
        {
            _blockedUids.Add(uid);
            var found = _foundUserDataList.FirstOrDefault(f => f.Friend.FriendsUid == uid);
            if (found != null)
            {
                found.IsBlocked = true;
                view.friendFoundScrollView.RefreshAllCells();
            }
        }

        private void OnUnblockFromFoundUser(long uid)
        {
            UnblockFoundUser(uid).Forget();
        }

        private async UniTask UnblockFoundUser(long uid)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
            var result = await Services.Lobby.ChatBlockAsync(uid, 1);
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));

            if (result.IsSuccess)
            {
                _blockedUids.Remove(uid);
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockReleased].StringToLocal, false));
                var found = _foundUserDataList.FirstOrDefault(f => f.Friend.FriendsUid == uid);
                if (found != null)
                {
                    found.IsBlocked = false;
                    view.friendFoundScrollView.RefreshAllCells();
                }
            }
            else
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockReleaseFailed].StringToLocal, false));
            }
        }

        private void OnUnblockUser(long uid)
        {
            UnblockUser(uid).Forget();
        }
        
        private async UniTask UnblockUser(long uid)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
            
            var result = await Services.Lobby.ChatBlockAsync(uid, 1);
            
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
            
            if (result.IsSuccess)
            {
                _blockedUids.Remove(uid);
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockReleased].StringToLocal, false));
                await GetBlockedUserList();
            }
            else
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BlockReleaseFailed].StringToLocal, false));
            }
        }
        
        private void OnSentReqCellUpdate(GameObject cell, int index)
        {
            if (index < 0 || index >= _sentReqDataList.Count)
                return;
            
            var slot = cell.GetComponent<FriendRequestSlot>();
            slot.Init(_sentReqDataList[index], false, OnBlockFromSentRequest);
        }
        
        private void OnReceivedReqCellUpdate(GameObject cell, int index)
        {
            if (index < 0 || index >= _receivedReqDataList.Count)
                return;
            
            var slot = cell.GetComponent<FriendRequestSlot>();
            slot.Init(_receivedReqDataList[index], true, OnBlockFromReceivedRequest);
        }
        
        private void OnFoundUserCellUpdate(GameObject cell, int index)
        {
            if (index < 0 || index >= _foundUserDataList.Count)
                return;
            
            var data = _foundUserDataList[index];
            var slot = cell.GetComponent<FriendRequestSlot>();
            slot.InitFindUser(data.Friend, data.IsAlreadyFriendOrSent, data.IsAlreadyReceived, data.IsBlocked, OnBlockFromFoundUser, OnUnblockFromFoundUser);
        }

        private void AnimateScrollToStart()
        {
            var mainScroll = currentTabIndex == 0 ? view.friendScrollView : view.friendFoundScrollView;
            AnimateScrollToStart(mainScroll, ref mainScrollTween);

            if (currentTabIndex == 1)
            {
                var subScrollView = currentTabIndexInRequest switch
                {
                    0 => view.friendRecievedReqScrollView,
                    1 => view.friendSentReqScrollView,
                    _ => view.friendRecievedReqScrollView
                };
                AnimateScrollToStart(subScrollView, ref scrollTween);

                var otherScrollView = currentTabIndexInRequest switch
                {
                    0 => view.friendSentReqScrollView,
                    _ => view.friendRecievedReqScrollView
                };
                otherScrollView.ScrollToTop();
            }
        }

        private void AnimateScrollToStart(RecycleScrollView scrollView, ref Tweener tween)
        {
            var rect = scrollView.GetComponent<ScrollRect>();
            if (rect.normalizedPosition.y >= 0.99f)
                return;

            tween?.Kill();
            tween = DOTween.To(() => rect.normalizedPosition, pos => rect.normalizedPosition = pos, new Vector2(rect.normalizedPosition.x, 1), 0.3f).SetEase(Ease.OutQuad);
        }

        private void ResetAllScrollPositions()
        {
            view.friendScrollView.ScrollToTop();
            view.friendSentReqScrollView.ScrollToTop();
            view.friendRecievedReqScrollView.ScrollToTop();
            view.friendFoundScrollView.ScrollToTop();

            _savedFoundUserScrollNormalizedY = 1f;
            _foundUserDataList.Clear();
            view.friendFoundScrollView.SetItemCount(0);
            UpdateScrollMovementType(view.friendFoundScrollView, 0, 4);
            view.emptyFoundUsers.SetActive(true);
            currentKeyword = "";
            if (view.inputField != null)
                view.inputField.text = "";
        }

        private void StopScrollAnimation()
        {
            mainScrollTween?.Kill();
            mainScrollTween = null;
            scrollTween?.Kill();
            scrollTween = null;
        }

        private void UpdateScrollMovementType(RecycleScrollView scrollView, int itemCount, int threshold)
        {
            var rect = scrollView.GetComponent<ScrollRect>();
            if (rect != null)
                rect.movementType = itemCount >= threshold ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
        }

        private void OnClickFindUser()
        {
            var keyword = view.inputField.text;

            if (string.IsNullOrEmpty(keyword) || keyword.Length < 2)
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.MinTwoCharactersRequired].StringToLocal, true));
                return;
            }

            if (keyword == currentKeyword && _foundUserDataList.Count > 0)
            {
                AnimateScrollToStart(view.friendFoundScrollView, ref mainScrollTween);
                return;
            }

            FindUserAndListing(keyword).Forget();
        }

        #region InviteFriend

        private static readonly string[] InviteQuestIds = 
        {
            "INVITE_FRIEND_1", "INVITE_FRIEND_2", "INVITE_FRIEND_3", "INVITE_FRIEND_4", "INVITE_FRIEND_5"
        };

        private void InitInviteFriend()
        {
            if (view.inviteBtn != null)
                view.inviteBtn.onClick.AddListener(OnClickInvite);
        }

        private void OnClickInvite()
        {
            InviteFriendManager.ShareInviteLink();
        }

        private async UniTask LoadInviteData()
        {
            if (view.inviteGetSlots == null || view.inviteGetSlots.Length == 0)
                return;

            var questListResult = await Services.Lobby.UserQuestListAsync();
            if (!questListResult.IsSuccess)
                return;

            var userQuests = questListResult.Data?.QuestList;

            for (int i = 0; i < view.inviteGetSlots.Length; i++)
            {
                if (view.inviteGetSlots[i] == null || i >= InviteQuestIds.Length)
                    continue;

                string questId = InviteQuestIds[i];

                var configQuest = ConfigDataManager.quests.FirstOrDefault(q => q.QuestId == questId);
                if (configQuest == null)
                    continue;

                var userQuest = userQuests?.FirstOrDefault(q => q.QuestId == questId);

                var merged = new lobby.Quest
                {
                    QuestId = configQuest.QuestId,
                    QuestType = configQuest.QuestType,
                    Type = configQuest.Type,
                    MaxCount = configQuest.MaxCount,
                    RewardItemId = configQuest.RewardItemId,
                    RewardValue = configQuest.RewardValue,
                    QuestValue = userQuest?.QuestValue ?? 0,
                    ReceivedRewardValue = userQuest?.ReceivedRewardValue ?? 0
                };

                view.inviteGetSlots[i].Init(i, merged, () => LoadInviteData().Forget());
            }
        }

        #endregion
    }
}
