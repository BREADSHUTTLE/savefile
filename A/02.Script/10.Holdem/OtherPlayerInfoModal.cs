using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.holdem;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class OtherPlayerInfoModal : MonoBehaviour
    {
        
        public TMP_Text userNick;
        public TMP_Text userChips;
        public TMP_Text userMatchRecrod;

        public CPButton closeModalBtn;
        public CPButton reportUserBtn;
        public CPButton exileUserBtn;
        public CPButton addFriendsBtn;
        
        [Header("ReportUser")]
        public GameObject reportUserPanel;
        public Toggle[] reportReasonToggles;
        public CPButton reportSubmitBtn;
        public CPButton closeReportPanelBtn;


        holdem.Player playerInfo;
        badugi.Player badugiPlayerInfo;
        sevenPoker.Player spokerPlayerInfo;
        private long playeruid;
        private lobby.ReportReason reportReason;

        private void Awake()
        {
        }
        
        public static event System.Action<GameObject> OnModalAutoClose;


        private float elapsedNoReactionTime = 0;
        [SerializeField] private float maxClosedTime = 5f;
        private void Update()
        {
            elapsedNoReactionTime+=Time.deltaTime;
            if (elapsedNoReactionTime > maxClosedTime)
            {
                OnModalAutoClose?.Invoke(gameObject);
                gameObject.SetActive(false);
            }
                
        }

        public void Init()
        {
            closeModalBtn?.onClick.AddListener(()=>
            {
                OnModalAutoClose?.Invoke(gameObject);
                this.gameObject.SetActive(false);
            });
            reportUserBtn?.onClick.AddListener(() =>
                {
                    elapsedNoReactionTime = 0;
                    reportUserPanel.SetActive(true);
                }
            );
            reportSubmitBtn?.onClick.AddListener(() =>
            {
                elapsedNoReactionTime = 0;
                ReportUser().Forget();
            });
            for (int i = 0; i < reportReasonToggles.Length; i++)
            {
                int index = i;
                reportReasonToggles[index]?.onValueChanged.AddListener(ison=>
                {
                    elapsedNoReactionTime = 0;
                    ToggleReportReason(index, ison);
                });
            }
            closeReportPanelBtn?.onClick.AddListener(() => {elapsedNoReactionTime = 0;reportUserPanel.SetActive(false); });
            exileUserBtn?.onClick.AddListener(()=>
            {
                elapsedNoReactionTime = 0;
                KickUser().Forget();
            });
            addFriendsBtn?.onClick.AddListener(()=>
            {
                elapsedNoReactionTime = 0;
                AddFriends().Forget();
            });
            reportReason = lobby.ReportReason.ResonCheating;
        }
        public void Set_OpenWindow(holdem.Player _playerInfo)
        {
            elapsedNoReactionTime = 0;
            if(reportUserPanel!=null)
                reportUserPanel?.SetActive(false);
            playerInfo=_playerInfo;
            playeruid=_playerInfo.Uid;
            userNick.text = playerInfo.Nick;
     
            bool isMe = playerInfo.Uid == CPPlayer.UserInfo.userDatabase.User.Uid;
            if(reportUserBtn!=null)
                reportUserBtn?.gameObject.SetActive(!isMe);
            if(exileUserBtn!=null)
                exileUserBtn?.gameObject.SetActive(!isMe);
            if(addFriendsBtn!=null)
                addFriendsBtn?.gameObject.SetActive(!isMe);
            GetuserInfo(GameType.HOLDEM,() => { gameObject.SetActive(true); }).Forget();
        }

        public void Set_OpenWindow(badugi.Player _playerInfo)
        {
            elapsedNoReactionTime = 0;
            if(reportUserPanel!=null)
                reportUserPanel?.SetActive(false);
            badugiPlayerInfo=_playerInfo;
            playeruid=_playerInfo.Uid;
            userNick.text = badugiPlayerInfo.Nick;
  
            bool isMe = badugiPlayerInfo.Uid == CPPlayer.UserInfo.userDatabase.User.Uid;
            if(reportUserBtn!=null)
                reportUserBtn?.gameObject.SetActive(!isMe);
            if(exileUserBtn!=null)
                exileUserBtn?.gameObject.SetActive(!isMe);
            if(addFriendsBtn!=null)
                addFriendsBtn?.gameObject.SetActive(!isMe);
            GetuserInfo(GameType.LOW_BADUGI,() => { gameObject.SetActive(true); }).Forget();
        }
        
        public void Set_OpenWindow(sevenPoker.Player _playerInfo)
        {
            elapsedNoReactionTime = 0;
            if(reportUserPanel!=null)
                reportUserPanel?.SetActive(false);
            spokerPlayerInfo=_playerInfo;
            playeruid=_playerInfo.Uid;
            userNick.text = spokerPlayerInfo.Nick;

            bool isMe = spokerPlayerInfo.Uid == CPPlayer.UserInfo.userDatabase.User.Uid;
            if(reportUserBtn!=null)
                reportUserBtn?.gameObject.SetActive(!isMe);
            if(exileUserBtn!=null)
                exileUserBtn?.gameObject.SetActive(!isMe);
            if(addFriendsBtn!=null)
                addFriendsBtn?.gameObject.SetActive(!isMe);
            GetuserInfo(GameType.SEVEN_POKER,() => { gameObject.SetActive(true); }).Forget();
        }
        
        async UniTask GetuserInfo(GameType gameType,Action callback)
        {
            switch (gameType)
            {
                case GameType.ALL:
                    break;
                case GameType.LOW_BADUGI:
                    var userinforesPacket_ba = await Services.Badugi.UserInfoReqAsync(playeruid);
                    if (userinforesPacket_ba.IsSuccess)
                    {
                        int winCount_0 = userinforesPacket_ba.Data.Win;
                        int loseCount_0 = userinforesPacket_ba.Data.Lose;
                        userMatchRecrod.text = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.WinLoseRecord].StringToLocal, 0, winCount_0, loseCount_0);
                        string haveChips = userChips.text;
                        string winchip=Extension.ToKoreanFormat(userinforesPacket_ba.Data.WinChip,Extension.KoreanFormatMode.Planning);
                        userChips.text = $"{winchip}";
                    }
                    break;
                case GameType.HOLDEM:
                    var userinforesPacket = await Services.Holdem.UserInfoReqAsync(playeruid);
                    if (userinforesPacket.IsSuccess)
                    {
                        int winCount = userinforesPacket.Data.Win;
                        int loseCount = userinforesPacket.Data.Lose;
                        userMatchRecrod.text = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.WinLoseRecord].StringToLocal, 0, winCount, loseCount);    
                        string haveChips = userChips.text;
                        string winchip=Extension.ToKoreanFormat(userinforesPacket.Data.WinChip,Extension.KoreanFormatMode.Planning);
                        userChips.text = $"{winchip}";
                    }
                    break;
                case GameType.SEVEN_POKER:
                    var userinforesPacket_SP = await Services.SevenPoker.UserInfoReqAsync(playeruid);
                    if (userinforesPacket_SP.IsSuccess)
                    {
                        int winCount = userinforesPacket_SP.Data.Win;
                        int loseCount = userinforesPacket_SP.Data.Lose;
                        userMatchRecrod.text = string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.WinLoseRecord].StringToLocal, 0, winCount, loseCount);    
                        string haveChips = userChips.text;
                        string winchip=Extension.ToKoreanFormat(userinforesPacket_SP.Data.WinChip,Extension.KoreanFormatMode.Planning);
                        userChips.text = $"{winchip}";
                    }
                    break;
                case GameType.END:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null);
            }

            callback?.Invoke();
        }

        async UniTask ReportUser()
        {
            long myuid = CPPlayer.UserInfo.userDatabase.User.Uid;
            long reporteduid = playeruid;
            
           var res= await Services.Lobby.UserReportReqAsync(myuid,reporteduid,reportReason);
           if (res.IsSuccess)
           {
               PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ReportAccepted].StringToLocal, false));   
           }
           else
           {
               Extension.eLog($"신고에 실패했습니다. 사유:{res.Error}");
               PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AlreadyReported].StringToLocal, false));
           }
            
        }

        async UniTask KickUser()
        {
            if (!CPPlayer.InGame.haveKickVote)
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.NoVoteTicket].StringToLocal, false));
                return;
            }
            
            
            switch (CPPlayer.InGame.currentGameType)
            {
                case GameType.LOW_BADUGI:
                    var packetResultH = await Services.Holdem.KickVoteAsync(CPPlayer.Badugi.currentTableId,badugiPlayerInfo.Uid);
                    if (packetResultH.IsSuccess == false)
                    {
                        PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.KickVoteFailed].StringToLocal}{packetResultH.Error}", false));
                        return;
                    }
                    break;
                case GameType.HOLDEM:
                    var packetResultB = await Services.Holdem.KickVoteAsync(CPPlayer.Holdem.currentTableId,playerInfo.Uid);
                    if (packetResultB.IsSuccess == false)
                    {
                        PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.KickVoteFailed].StringToLocal}{packetResultB.Error}", false));
                        return;
                    }
                    break;
                case GameType.SEVEN_POKER:
                    var packetResultS =await Services.Holdem.KickVoteAsync(CPPlayer.Holdem.currentTableId,spokerPlayerInfo.Uid);
                    if (packetResultS.IsSuccess == false)
                    {
                        PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.KickVoteFailed].StringToLocal}{packetResultS.Error}", false));
                        return;
                    }
                    break;
            }
            PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.KickVoteCompleted].StringToLocal, false));
            
         
        }

        void ToggleReportReason(int index,bool ison)
        {
            if (!ison)
                return;
            
            switch (index)
            {
                case 0:
                    reportReason = lobby.ReportReason.ResonCheating;
                    break;
                case 1:
                    reportReason = lobby.ReportReason.ResonCollusion;
                    break;
                case 2:
                    reportReason =lobby.ReportReason.ResonOthers;
                    break;
            }
        }

        async UniTask AddFriends()
        {
            try
            {
                LobbyClient.PacketResult<FriendsRequestRes> friendsRequest=null;
                switch (CPPlayer.InGame.currentGameType)
                {
                    case GameType.LOW_BADUGI:
                        friendsRequest=await Services.Lobby.FriendsRequestAsync(FriendsRequestType.Request, badugiPlayerInfo.Uid);
                        break;
                    case GameType.HOLDEM:
                        friendsRequest=await Services.Lobby.FriendsRequestAsync(FriendsRequestType.Request, playerInfo.Uid);
                        break;
                    case GameType.SEVEN_POKER:
                        friendsRequest=await Services.Lobby.FriendsRequestAsync(FriendsRequestType.Request, spokerPlayerInfo.Uid);
                        break;
                }
                if (friendsRequest != null)
                {
                    if (!friendsRequest.IsSuccess)
                    {
                        if (friendsRequest.Error.Code == ErrorCode.EFriendsBlocked)
                            PopupManager.Instance.Open<PopupToast>(popup => popup.ShowBottomPopup(StaticData.GetLobbyErrorMessage(ErrorCode.EFriendsBlocked), true));
                        return;
                    }
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FriendRequestCompleted].StringToLocal, false));    
                }
                
            }
            catch (Exception e)
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FriendRequestFailed].StringToLocal}{e}", true));
                throw;
            }
        }
    }
}
