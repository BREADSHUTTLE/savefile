using CAPYBARA.Bundles;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.holdem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CAPYBARA
{
    public class ControllerInGame:IDisposable
    {
        private ViewCanvasInGame _viewCanvasInGame;
        private CancellationTokenSource _cts;

        GameType currentSelected;

        float chipAnimationDuration = 0.6f; // 애니메이션 지속 시간 (초)
        private bool isAFKBtnClicked = false;


        HoldemController _holdemController;
        BadugiController _badugiController;
        SPokerController _spokerController;
            
        public ControllerInGame(Transform mainsceneTr, CancellationTokenSource cts)
        {
            _cts = cts;

            _viewCanvasInGame = ViewCanvas.Create<ViewCanvasInGame>(mainsceneTr);
            
            GameRuleProvider ruleProvider = new GameRuleProvider();
            
            _holdemController=new HoldemController(_viewCanvasInGame.gameObject, _viewCanvasInGame.HoldemView, _cts);
            _badugiController=new BadugiController(_viewCanvasInGame.gameObject, _viewCanvasInGame.badugiView, _cts,ruleProvider);
            _spokerController= new SPokerController(_viewCanvasInGame.gameObject, _viewCanvasInGame.sevenpokerView, _cts,ruleProvider);
            
            _viewCanvasInGame.ingameOptionWindow.Init();
            _viewCanvasInGame.ingameOptionWindow_badugi.Init();
            _viewCanvasInGame.ingameOptionWindow_SPoker.Init();
            
            _viewCanvasInGame.afkBtn.onClick.AddListener(() => { UserActiveThisGame().Forget(); });
        }

        public void Dispose()
        {
            _holdemController?.Dispose();
            _badugiController?.Dispose();
            _spokerController?.Dispose();
        }
        
        public void GameStartSet()
        {
            _viewCanvasInGame.HoldemView.gameObject.SetActive(false);
            _viewCanvasInGame.badugiView.gameObject.SetActive(false);

            _viewCanvasInGame.SetVisible(true);
  
            CPPlayer.InGame.EnterInGame += (t, m, d) => EnterInGame(t, m, d).Forget();
            CPPlayer.InGame.LeaveGame += (gt) => LeaveGameAsync(gt).Forget();
            CPPlayer.InGame.MoveTable += (gt) => { LeaveGameAndMoveRoom(gt).Forget(); };

            _viewCanvasInGame.afkPanel.SetActive(false);

            CPPlayer.InGame.AFKPopupActive += ActiveAFKPopup;
            isAFKBtnClicked = false;
            
            _holdemController.StartSet();
            _badugiController.StartSet();
            _spokerController.StartSet();

            _holdemController.onWaitGamePopup = ActiveWaitGamePopup;
        }

        async UniTask<bool> EnterInGame(GameType gametype, GameMode gameMode, lobby.RoomInfo data)
        {
            //server connect
            if (CPPlayer.Server.currentConnectedGameType != gametype)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingInGamePopupActive(true));
           
                switch (CPPlayer.Server.currentConnectedGameType)
                {
                    case GameType.ALL:
                        break;
                    case GameType.LOW_BADUGI:
                        ConnectionManager.Instance.CloseBadugiConnection();
                        break;
                    case GameType.HOLDEM:
                        ConnectionManager.Instance.CloseHoldemConnection();
                        break;
                    case GameType.SEVEN_POKER:
                        ConnectionManager.Instance.CloseSevenPokerConnection();
                        break;
                    case GameType.END:
                        break;
                }
                switch (gametype)
                {
                    case GameType.ALL:
                        break;
                    case GameType.LOW_BADUGI:
                        await ConnectionManager.Instance.BadugiConnect();
                        CPPlayer.Server.CallbackAfterBadugiConnect?.Invoke();
                        break;
                    case GameType.HOLDEM:
                        await ConnectionManager.Instance.HoldemConnect();
                        CPPlayer.Server.CallbackAfterHoldemConnect?.Invoke();
                        break;
                    case GameType.SEVEN_POKER:
                        await ConnectionManager.Instance.SevenPokerConnect();
                        CPPlayer.Server.CallbackAfterSPokerConnect?.Invoke();
                        break;
                    case GameType.END:
                        break;
                }
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingInGamePopupActive(false));
            }
            
            //260210 이모티콘 정보 위한 인벤토리 비동기 로드_임재현
            var inventoryRes = await Services.Lobby.GetInventoryAsync(true);
            if (inventoryRes.IsSuccess)
            {
                CPPlayer.Inventory.inventoryInfo = inventoryRes.Data;
                CPPlayer.Inventory.inventoryUpdateCallback?.Invoke();
            }
            
            List<(ItemID itemId, lobby.Inventory item)> emoticonDataList = new List<(ItemID, lobby.Inventory)>();
            emoticonDataList.Clear();
            int currentSelectedEmoticonIndex = -1;
            
            var itemInfoRes = CPPlayer.Inventory.inventoryInfo;
            for (int i = 0; i < itemInfoRes.Inventory.Count; i++)
            {
                var itemId = Extension.StringToEnum<ItemID>(itemInfoRes.Inventory[i].ItemId.ToUpper());

                if (!itemId.ToString().Contains("EMOTICON_"))
                    continue;

                if (itemInfoRes.Inventory[i].Amount > 0)
                    emoticonDataList.Add((itemId, itemInfoRes.Inventory[i]));
            }

            emoticonDataList = emoticonDataList.OrderByDescending(x => x.item.IsEffective).ToList();
            currentSelectedEmoticonIndex = emoticonDataList.Count > 0 && emoticonDataList[0].item.IsEffective ? 0 : -1;
            
            //이모티콘 정보 추가 260206_임재현
            if (currentSelectedEmoticonIndex >= 0 && currentSelectedEmoticonIndex < emoticonDataList.Count)
                CPPlayer.InGame.currentEquippedEmoticon = emoticonDataList[currentSelectedEmoticonIndex].itemId;
            else
                CPPlayer.InGame.currentEquippedEmoticon = ItemID.EMOTICON_1;
            //이모티콘 정보 추가 260206_임재현
            
            //260210 이모티콘 정보 위한 인벤토리 비동기 로드_임재현
            
            if (gametype == GameType.HOLDEM)
            {
                var hRes= await Services.Holdem.EnterRoomAsync(data.RoomId, CPPlayer.UserInfo.userDatabase.User.Gold, CPPlayer.Holdem.currentTableId);
                if (hRes.IsSuccess)
                {
                    CPPlayer.InGame.currentRoomInfo = data;
                    CPPlayer.InGame.currentGameMode=gameMode;
                    CPPlayer.InGame.currentGameType = gametype;
                    
                    CPPlayer.Holdem.currentTableId = hRes.Data.TableId;
                    CPPlayer.Holdem.initialBuyIn = data.Ante;
                    _viewCanvasInGame.HoldemView.gameObject.SetActive(true);
                    CPPlayer.Holdem.EnterRoom?.Invoke(hRes.Data);
            
                    _viewCanvasInGame.SetVisible(true);
                    AudioManager.Instance.Stop(AudioSourceKey.LobbyBGM);
                }
                else
                {
                    return false;
                    //CPPlayer.InGame.errorToastPopup?.Invoke($"Server error Occured.\nMessage:{hRes.Error}");
                }
            }
            else if(gametype == GameType.LOW_BADUGI)
            {
                var bRes= await Services.Badugi.EnterRoomAsync(data.RoomId, CPPlayer.UserInfo.userDatabase.User.Gold, CPPlayer.Badugi.currentTableId);
                if (bRes.IsSuccess)
                {
                    CPPlayer.InGame.currentRoomInfo = data;
                    CPPlayer.InGame.currentGameMode=gameMode;
                    CPPlayer.InGame.currentGameType = gametype;
                    
                    CPPlayer.Badugi.currentTableId = bRes.Data.TableId;
                    CPPlayer.Badugi.initialBuyIn = data.Ante;
                    _viewCanvasInGame.badugiView.gameObject.SetActive(true);
                    CPPlayer.Badugi.EnterRoom?.Invoke(bRes.Data);
            
                    _viewCanvasInGame.SetVisible(true);
                    AudioManager.Instance.Stop(AudioSourceKey.LobbyBGM);
                }
                else
                {
                    return false;
                   // CPPlayer.InGame.errorToastPopup?.Invoke($"Server error Occured.\nMessage:{bRes.Error}");
                }
            }
            else if(gametype == GameType.SEVEN_POKER)
            {
                var Res= await Services.SevenPoker.EnterRoomAsync(data.RoomId, CPPlayer.UserInfo.userDatabase.User.Gold, CPPlayer.SPoker.currentTableId);
                if (Res.IsSuccess)
                {
                    CPPlayer.InGame.currentRoomInfo = data;
                    CPPlayer.InGame.currentGameMode=gameMode;
                    CPPlayer.InGame.currentGameType = gametype;
                    
                    CPPlayer.SPoker.currentTableId = Res.Data.TableId;
                    CPPlayer.SPoker.initialBuyIn = data.Ante;
                    _viewCanvasInGame.sevenpokerView.gameObject.SetActive(true);
                    CPPlayer.SPoker.EnterRoom?.Invoke(Res.Data);
            
                    _viewCanvasInGame.SetVisible(true);
                    AudioManager.Instance.Stop(AudioSourceKey.LobbyBGM);
                }
                else
                {
                    return false;
                    //CPPlayer.InGame.errorToastPopup?.Invoke($"Server error Occured.\nMessage:{Res.Error}");
                }
            }

            CPPlayer.InGame.isInGame = true;
            return true;


        }
        
        void ActiveAFKPopup(bool isUserAFK)
        {
            _viewCanvasInGame.afkPanel.SetActive(isUserAFK);
            CPPlayer.InGame.AFKPopupActiveFlag = isUserAFK;
        }

        void ActiveWaitGamePopup(bool isWaitGame)
        {
            _viewCanvasInGame.waitGamePanel.SetActive(isWaitGame);
        }

        async UniTask UserActiveThisGame()
        {
            if (isAFKBtnClicked)
                return;
            isAFKBtnClicked = true;

            try
            {
                switch (CPPlayer.InGame.currentGameType)
                {
                    case GameType.LOW_BADUGI:
                        await Services.Badugi.ActivateAsync( CPPlayer.Badugi.currentTableId);
                        break;
                    case GameType.HOLDEM:
                        await Services.Holdem.ActivateAsync( CPPlayer.Holdem.currentTableId);
                        break;
                    case GameType.SEVEN_POKER:
                        await Services.SevenPoker.ActivateAsync( CPPlayer.SPoker.currentTableId);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Debug.LogError("server error");
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AbsenceActivationError].StringToLocal}{e.Message}"));
            }
        
            CPPlayer.InGame.AFKPopupActive?.Invoke(false);
            CPPlayer.InGame.isUserAFK = false;
            CPPlayer.InGame.AFKPopupActiveFlag = false;
            
            isAFKBtnClicked = false;
        }

        async UniTask LeaveGameAsync(GameType gametype)
        {
            CPPlayer.InGame.isInGame = false;
            AudioManager.Instance.Play(AudioSourceKey.LobbyBGM);
            await UserSetting(gametype);

            await CheckAndRequestAllinReward(gametype);

            if (await IsIdentityVerificationExpiredOnLobbyEntry())
            {
                CPPlayer.OutGame.pendingVerifyExpiredLogout = true;
                CPPlayer.OutGame.logoutToLogin = () => ForceLogoutToLogin().Forget();
            }

            CPPlayer.OutGame.ReturnToLobby?.Invoke();
            switch (gametype)
            {
                case GameType.ALL:
                    break;
                case GameType.LOW_BADUGI:
                    ViewCanvas.Get<ViewCanvasInGame>().badugiView.gameObject.SetActive(false);
                    break;
                case GameType.HOLDEM:
                    ViewCanvas.Get<ViewCanvasInGame>().HoldemView.gameObject.SetActive(false);
                    break;
                case GameType.SEVEN_POKER:
                    ViewCanvas.Get<ViewCanvasInGame>().sevenpokerView.gameObject.SetActive(false);
                    break;
                case GameType.END:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gametype), gametype, null);
            }
            _viewCanvasInGame.ingameOptionWindow.gameObject.SetActive(false);
            _viewCanvasInGame.ingameOptionWindow_badugi.gameObject.SetActive(false);
            _viewCanvasInGame.ingameOptionWindow_SPoker.gameObject.SetActive(false);
            
            _viewCanvasInGame.waitGamePanel.SetActive(false);
            
            CPPlayer.OutGame.RenewRoomList?.Invoke();
        }
        
        async UniTask LeaveGameAndMoveRoom(GameType gametype)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(true));
            
            await UserSetting(gametype);
            bool canEnter=await EnterInGame(gametype, CPPlayer.InGame.currentGameMode, CPPlayer.InGame.currentRoomInfo);
            if (canEnter == false)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(false));
                AudioManager.Instance.Play(AudioSourceKey.LobbyBGM);   
                if (await IsIdentityVerificationExpiredOnLobbyEntry())
                {
                    CPPlayer.OutGame.pendingVerifyExpiredLogout = true;
                    CPPlayer.OutGame.logoutToLogin = () => ForceLogoutToLogin().Forget();
                }
                
                CPPlayer.OutGame.ReturnToLobby?.Invoke();
                switch (gametype)
                {
                    case GameType.ALL:
                        break;
                    case GameType.LOW_BADUGI:
                        ViewCanvas.Get<ViewCanvasInGame>().badugiView.gameObject.SetActive(false);
                        break;
                    case GameType.HOLDEM:
                        ViewCanvas.Get<ViewCanvasInGame>().HoldemView.gameObject.SetActive(false);
                        break;
                    case GameType.SEVEN_POKER:
                        ViewCanvas.Get<ViewCanvasInGame>().sevenpokerView.gameObject.SetActive(false);
                        break;
                    case GameType.END:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(gametype), gametype, null);
                }
                _viewCanvasInGame.ingameOptionWindow.gameObject.SetActive(false);
                _viewCanvasInGame.ingameOptionWindow_badugi.gameObject.SetActive(false);
                _viewCanvasInGame.ingameOptionWindow_SPoker.gameObject.SetActive(false);
            
                _viewCanvasInGame.waitGamePanel.SetActive(false);
            
                CPPlayer.OutGame.RenewRoomList?.Invoke();
            }
            else
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ServerLoadingPopupActive(false));
            }

            CPPlayer.InGame.isMovingTable = false;
        }
        
        async UniTask UserSetting(GameType gametype)
        {
            var userPacket=await Services.Lobby.GetUserInfoAsync();
            if (userPacket.IsSuccess)
                CPPlayer.UserInfo.userDatabase = userPacket.Data;
            
            CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
        }

        private async UniTask CheckAndRequestAllinReward(GameType gametype)
        {
            try
            {
                var user = CPPlayer.UserInfo.userDatabase?.User;
                if (user == null) 
                    return;

                long totalGold = user.Gold + user.Safe;
                if (totalGold > 0) 
                    return;

                var joinType = gametype switch
                {
                    GameType.HOLDEM => lobby.GameJoinType.HoldemGame,
                    GameType.LOW_BADUGI => lobby.GameJoinType.BadugiGame,
                    GameType.SEVEN_POKER => lobby.GameJoinType.SevenPokerGame,
                    _ => lobby.GameJoinType.NoneGame
                };

                if (joinType == lobby.GameJoinType.NoneGame) 
                    return;

                var result = await Services.Lobby.AllinRewardReqAsync(joinType);
                if (result.IsSuccess)
                {
                    var userPacket = await Services.Lobby.GetUserInfoAsync();
                    if (userPacket.IsSuccess)
                        CPPlayer.UserInfo.userDatabase = userPacket.Data;

                    CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();

                    PopupManager.Instance.Open<PopupToast>(popup =>popup.ActivateBigwindowTwoBtn("올인 보상", "올인 보상이 지급되었습니다.", null));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AllinReward] 올인 보상 요청 중 예외: {e.Message}");
            }
        }

        async UniTask<bool> IsIdentityVerificationExpiredOnLobbyEntry()
        {
            if (Application.isEditor)
                return false;
            if (IsIdentityVerificationSkipByBuild())
                return false;
            if (IsTestAccountByUserId())
                return false;

            var memberData = await Services.Lobby.MemberReqAsync(LoginData.Cloud.loginValue.userAutoToken);
            if (!memberData.IsSuccess)
                return false;
            
            int reVerifyAt = memberData.Data.ReVerifyAt;
            if (reVerifyAt <= 0)
                return true;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now >= reVerifyAt;
        }

        bool IsIdentityVerificationSkipByBuild()
        {
#if SKIP_IDENTITY_VERIFICATION
            return true;
#else
            return false;
#endif
        }

        bool IsTestAccountByUserId()
        {
            var userId = LoginData.Cloud.loginValue.userAccountID;
            return !string.IsNullOrEmpty(userId)
                   && userId.IndexOf("atest", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        async UniTask ForceLogoutToLogin()
        {
            try
            {
                var json = JsonUtility.ToJson(CPPlayer.Cloud);
                await Services.Lobby.UserSettingsSetReq(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Reverify] logout save failed: {e.Message}");
            }

            LocalSaveLoader.DeleteCloudData();
            
            ConnectionManager.Instance.Dispose();
            
            CPPlayer.Dispose();
            PoolManager.Clear();

            SceneManager.LoadScene("Loading");
        }

        
    }    
}

