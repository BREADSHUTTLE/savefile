using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using UnityEngine;

namespace CAPYBARA
{
    public static partial class CPPlayer
    {
        public static class OutGame
        {
            //openUIWIndow
            public static Action openShopUI;
            public static Action<ShopMainTapType, Action> openShopUIWithTab;
            public static Action openProfileUI;
            public static Action openOptionUI;
            public static Action openVaultUI;
            public static Action openInventory;
            public static Action openChat;
            public static Action openFriends;
            public static Action<int> openFriendsWithTab;
            public static Action OpenMissionView;
            public static Action openAdMob;
            //openUIWIndow


            //new action 재화 변경시 또는 룸 불러올때 콜백
            public static Action RenewRoomList;
            //재화 갱신(애니메이션 없이)
            public static Action callbackAfterGetMoneyAndBox;
            //닉네임 변경 콜백
            public static Action nickNameChangedCallback;
            
            //notification
            
            //friends
            public static Action<long> CreateConversationFriend;
            public static Action<long> CancelRequestFriend;
            public static Action<long> AcceptRequestFriend;
            public static Action<long> RejectRequestFriend;
            public static Action<long, GameObject, GameObject> RequestFriend;
            public static bool ReturnToFriendsWhenChatCloses;
            public static Action HideFriendsForChat;
            public static Action ShowFriendsViewQuiet;
            public static Action<bool> newFriendRequestNotiCallback;
            public static bool hasNewFriendRequestNoti;
            public static HashSet<long> pendingFriendRequestNotiUids = new HashSet<long>();
            public static Action<FriendsRequestType> friendRequestTypeNotiCallback;
            public static Action refreshFriendsList;
            
            //chatroomid에 따른 메시지 콜백 
            public static Action<long,bool> newMessageNotiCallback;
            //못본 채팅이 하나라도 있을시 noti callback
            public static Action<bool> newMsgExistNotiCallback;
            
            public static Action<bool> newPostNotiCallback;
            
            //achievement
            public static Action<bool> newAchievementNotiCallback;
            
            //invite friend
            public static Action openInviteFriend;
            public static Action shareInviteLink;
            
            // 플레이 시간 미션 (로컬 캐시)
            public static int cachedPlayTimeMinutes = 0;
            public static float playTimeCheckedAt = 0f;
            public static bool playTimeMissionClaimable = false;
            public static bool playTimeMissionRewarded = false;  // 보상 수령 여부
            public static Action CheckPlayTimeNotiLocal;  // 로컬 체크 콜백
            
            //popupWindow
            //손실제한 변경 팝업 콜백
            public static Action openLossLimitWindow;
            public static Action onLossLimitChanged;
            public static bool pendingVerifyExpiredLogout;
            public static Action logoutToLogin;
            
            //inappPurchase 상품 구매후 콜백
            public static Action<IAPProduct> AfterPurchase;
            
            //inventory
            public static Action RenewInventory;
            
            // 게임 종료 후 로비 복귀 콜백
            public static Action ReturnToLobby;
            
            // 이벤트 정보
            public static List<lobby.Event> eventList;
            public static bool IsEventActive => eventList?.Any(e => e.IsEvent != 0) ?? false;

            public static void Dispose()
            {
                openShopUI = null;
                openShopUIWithTab = null;
                openProfileUI = null;
                openOptionUI = null;
                openVaultUI = null;
                openInventory = null;
                openChat = null;
                openFriends = null;
                openFriendsWithTab = null;
                OpenMissionView = null;
                RenewRoomList = null;
                callbackAfterGetMoneyAndBox = null;
                nickNameChangedCallback = null;
                
                CreateConversationFriend = null;
                CancelRequestFriend = null;
                AcceptRequestFriend = null;
                RejectRequestFriend = null;
                RequestFriend = null;
                ReturnToFriendsWhenChatCloses = false;
                HideFriendsForChat = null;
                ShowFriendsViewQuiet = null;
                newFriendRequestNotiCallback = null;
                hasNewFriendRequestNoti = false;
                pendingFriendRequestNotiUids.Clear();
                friendRequestTypeNotiCallback = null;
                refreshFriendsList = null;
                
                newMessageNotiCallback = null;
                newMsgExistNotiCallback = null;
                newPostNotiCallback = null;
                newAchievementNotiCallback = null;
                openInviteFriend = null;
                shareInviteLink = null;
                cachedPlayTimeMinutes = 0;
                playTimeCheckedAt = 0f;
                playTimeMissionClaimable = false;
                playTimeMissionRewarded = false;
                CheckPlayTimeNotiLocal = null;
                
                openLossLimitWindow = null;
                onLossLimitChanged = null;
                pendingVerifyExpiredLogout = false;
                logoutToLogin = null;
                
                AfterPurchase = null;
                RenewInventory = null;
                ReturnToLobby = null;
                eventList = null;
            }
        }
    }
}
