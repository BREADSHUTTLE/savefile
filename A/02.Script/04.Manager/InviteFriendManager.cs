using System.Collections.Generic;
using CAPYBARA.Core;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA
{
    public static class InviteFriendManager
    {
        public static string GenerateInviteLink()
        {
            string code = CPPlayer.UserInfo.userDatabase.User.Code;
            string uriScheme = UnityEngine.Networking.UnityWebRequest.EscapeURL($"atozpoker://invite?code={code}");
            return $"{Constraints.OneLinkBaseUrl}?deep_link_value=invite&af_sub1={code}&af_dp={uriScheme}&af_force_deeplink=true";
        }

        public static void ShareInviteLink()
        {
            string link = GenerateInviteLink();
            string message = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InviteFriendMessage].StringToLocal, link);

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (var intentObj = new AndroidJavaObject("android.content.Intent"))
            {
                intentObj.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intentObj.Call<AndroidJavaObject>("setType", "text/plain");
                intentObj.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), message);

                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObj, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InviteFriend].StringToLocal))
                {
                    activity.Call("startActivity", chooser);
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS 네이티브 공유는 별도 플러그인 필요 시 추가
            GUIUtility.systemCopyBuffer = link;
           PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InviteLinkCopied].StringToLocal, false));
#else
            GUIUtility.systemCopyBuffer = link;
            PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InviteLinkCopied].StringToLocal, false));
#endif

            var values = new Dictionary<string, string>
            {
                { "invite_code", CPPlayer.UserInfo.userDatabase.User.Code }
            };
            AppsFlyerSDK.AppsFlyer.sendEvent("invite_friend_share", values);
        }

        public static async UniTask ProcessPendingInviteCode()
        {
            if (!DeepLinkData.HasPendingInvite)
                return;

            string code = DeepLinkData.PendingInviteCode;

            string myCode = CPPlayer.UserInfo.userDatabase?.User?.Code;
            if (string.IsNullOrEmpty(myCode))
            {
                Debug.LogWarning("[InviteFriend] User info not loaded yet, will retry on next lobby entry");
                return;
            }

            DeepLinkData.Clear();

            if (code == myCode)
            {
                Debug.Log("[InviteFriend] Cannot invite yourself");
                return;
            }

            Debug.Log($"[InviteFriend] Processing invite code: {code}");

            var result = await Services.Lobby.FriendsJoinReqAsync(code);
            if (result.IsSuccess)
            {
                Debug.Log("[InviteFriend] Invite code accepted");
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InviteFriendCompleted].StringToLocal, false));

                var values = new Dictionary<string, string>
                {
                    { "invite_code", code },
                    { "invited_code", myCode }
                };
                AppsFlyerSDK.AppsFlyer.sendEvent("invite_friend_accepted", values);
            }
            else
            {
                var errorCode = result.Error?.Code;
                if (errorCode == ErrorCode.EFriendsAlreadyInvite)
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AlreadyInvited].StringToLocal, false));
                else if (errorCode == ErrorCode.EFriendsInvalidInviteUid)
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InvalidInviteCode].StringToLocal, false));
                else
                    Debug.LogWarning($"[InviteFriend] Failed: {result.Error}");
            }
        }

        public static async UniTask<List<Friends>> GetInvitedFriendsList()
        {
            var result = await Services.Lobby.FriendsJoinListReqAsync();
            if (result.IsSuccess && result.Data?.Friends != null)
                return new List<Friends>(result.Data.Friends);

            return new List<Friends>();
        }
    }
}
