using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Bundles;
using UnityEngine;
using System.Threading;
using CAPYBARA.badugi;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using BlackTree.Bundles;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using KickReason = CAPYBARA.lobby.KickReason;
using MaintenanceNoti = CAPYBARA.lobby.MaintenanceNoti;

namespace CAPYBARA.Core
{
    public class ControllerLobby:IDisposable
    {
        private ViewCanvasLobby _viewCanvasLobby;
        private CancellationTokenSource _cts;

        GameType currentSelected;

        private List<ViewRoomEnterSlot> holdemSlotList = new List<ViewRoomEnterSlot>();
        private List<ViewRoomEnterSlot> badugiSlotList = new List<ViewRoomEnterSlot>();
        private List<ViewRoomEnterSlot> spokerSlotList = new List<ViewRoomEnterSlot>();

        public ControllerLobby(Transform mainsceneTr, CancellationTokenSource cts)
        {
            _cts = cts;
            _viewCanvasLobby = ViewCanvas.Create<ViewCanvasLobby>(mainsceneTr);
            
            holdemSlotList.Clear();
            badugiSlotList.Clear();
            spokerSlotList.Clear();
            //InitLobbyDisplay().Forget();
        }

        public void Dispose()
        {
            DeepLinkData.OnInviteCodeReceived -= OnDeepLinkInviteReceived;
        }

        void OnDeepLinkInviteReceived(string code)
        {
            InviteFriendManager.ProcessPendingInviteCode().Forget();
        }
        
        public async UniTask GameStartSet()
        {
            await UniTask.WaitUntil(() => initComplete);
            CPPlayer.OutGame.callbackAfterGetMoneyAndBox += LobbyGoldTextAnimStart;
            
            RestoreLastGameTab();
            
            _viewCanvasLobby.holdemSlotParent.onValueChanged.AddListener(_ => SaveScrollPosition(GameType.HOLDEM));
            _viewCanvasLobby.badugiSlotParent.onValueChanged.AddListener(_ => SaveScrollPosition(GameType.LOW_BADUGI));
            _viewCanvasLobby.SpokerSlotParent.onValueChanged.AddListener(_ => SaveScrollPosition(GameType.SEVEN_POKER));
            
            SettingLobbyDisplay();
            
            CPPlayer.Inventory.inventoryUpdateCallback += InventoryItemUpdateCallback;
            CPPlayer.Inventory.pointsUpdateCallback += InventoryItemUpdateCallback;
            CPPlayer.OutGame.ReturnToLobby += OnReturnToLobby;
            CPPlayer.OutGame.CheckPlayTimeNotiLocal += CheckPlayTimeNotiLocal;
            CPPlayer.OutGame.RenewInventory += RenewInventoryAndAvatar;
            
            CPPlayer.OutGame.newAchievementNotiCallback += SetAchievementNoti;
            
            if (_viewCanvasLobby.achievementNoti != null)
                _viewCanvasLobby.achievementNoti.SetActive(false);
            
            await InitLobbyData();
            
            _viewCanvasLobby.SetVisible(true);
            
            InviteFriendManager.ProcessPendingInviteCode().Forget();
            DeepLinkData.OnInviteCodeReceived += OnDeepLinkInviteReceived;

            CPPlayer.OutGame.nickNameChangedCallback += NickNameChangedCallback;

            bool isNotNickNameSet = CPPlayer.UserInfo.userDatabase.User.IsFirstLogin == 1;
            if (isNotNickNameSet)
                OpenSetFirstNickNameWindow();

            CPPlayer.OutGame.shareInviteLink += InviteFriendManager.ShareInviteLink;
            
            CPPlayer.OutGame.RenewRoomList += CurrentOpenRoomList;
            CPPlayer.OutGame.openLossLimitWindow += OpenSetLossLimitWindow;
            CPPlayer.OutGame.AfterPurchase += (o) => AfterPurchaseCallback(o).Forget();
            
            //shop
            CPPlayer.Inventory.shopnormalToastPopup += OpenNormalItemPurchasePopup;
            
            //lobbyNotiPush
            LobbyDispatchPushHub.onFriendsNoti += FriendsRequest_Noti;
            LobbyDispatchPushHub.onPostsNoti += PostsRecieved_Noti;
            LobbyDispatchPushHub.onMessageNoti += MsgRecieved_Noti;
            LobbyDispatchPushHub.onKickNoti += KickRecieved_Noti;
            LobbyDispatchPushHub.onMaintenanceNoti += Maintenance_Noti;

            CPPlayer.OutGame.newFriendRequestNotiCallback += (o) => { _viewCanvasLobby.friendsNoti.gameObject.SetActive(o); };
            CPPlayer.OutGame.newMsgExistNotiCallback += (o) => { _viewCanvasLobby.MessageNoti.gameObject.SetActive(o); };
            CPPlayer.OutGame.newPostNotiCallback += (o) => { _viewCanvasLobby.PostNoti.gameObject.SetActive(o); };
            
            AudioManager.Instance.Play(AudioSourceKey.LobbyBGM);
            
           
            
            // 화면 뜬 후 노티 상태 반영
            if (CPPlayer.OutGame.playTimeMissionClaimable)
                CPPlayer.OutGame.newAchievementNotiCallback?.Invoke(true);
            
            
          
            
            //CheckExpiredClassFromInventory();
       
        }

        bool initComplete = false;
        public async UniTask InitLobbyDisplay()
        {
            initComplete = false;
            await UpdateRoomsInfo();
            await UniTask.Yield();
            
            //each UI new
            new ControllerShop(_viewCanvasLobby.viewShop, _cts);
            new ControllerProfile(_viewCanvasLobby.profileWindow, _viewCanvasLobby, _cts);
            new ControllerOption(_viewCanvasLobby.viewOption, _cts);
          
            new ControllerInventory(_viewCanvasLobby.viewInventory, _cts);
            new ControllerChat(_viewCanvasLobby.viewChat, _cts);
            new ControllerFriends(_viewCanvasLobby.viewFriends, _cts);
            new ControllerMission(_viewCanvasLobby.viewMission, _cts);
            var vaultPopup = PopupManager.Instance.Setup<PopupVault>();
            if (vaultPopup != null)
                new ControllerVault(vaultPopup, _cts);
            

            _viewCanvasLobby.gameTabGroup.onIndexChanged += OnGameTabChanged;
          
            _viewCanvasLobby.shopOpenBtn.onClick.AddListener(() => TryOpenWithExpiryCheck(() => CPPlayer.OutGame.openShopUI?.Invoke()).Forget());
            _viewCanvasLobby.prfileOpenBtn.onClick.AddListener(() => TryOpenWithExpiryCheck(() => CPPlayer.OutGame.openProfileUI?.Invoke()).Forget());
            _viewCanvasLobby.btnAvaterProfile.onClick.AddListener(() => TryOpenWithExpiryCheck(() => CPPlayer.OutGame.openProfileUI?.Invoke()).Forget());

            _viewCanvasLobby.option.onClick.AddListener(() => TryOpenWithExpiryCheck(() => CPPlayer.OutGame.openOptionUI?.Invoke()).Forget());
            _viewCanvasLobby.InvenOpenBtn.onClick.AddListener(() => TryOpenWithExpiryCheck(() => CPPlayer.OutGame.openInventory?.Invoke()).Forget());
            _viewCanvasLobby.MessageOpenBtn.onClick.AddListener(() => TryOpenWithExpiryCheck(() => CPPlayer.OutGame.openChat?.Invoke()).Forget());
            _viewCanvasLobby.friendsOpenBtn.onClick.AddListener(() => TryOpenWithExpiryCheck(() => CPPlayer.OutGame.openFriends?.Invoke()).Forget());
            _viewCanvasLobby.achievementOpenBtn.onClick.AddListener(() => TryOpenWithExpiryCheck(() => CPPlayer.OutGame.OpenMissionView?.Invoke()).Forget());
            _viewCanvasLobby.advertiseOpenBtn.onClick.AddListener(() => { PopupManager.Instance.Open<PopupAdMob>(); });

            _viewCanvasLobby.announceOpenBtn.onClick.RemoveAllListeners();
            _viewCanvasLobby.announceOpenBtn.onClick.AddListener(() => PopupManager.Instance.Open<PopupNotice>());

            if (_viewCanvasLobby.customerServiceOpenBtn != null)
            {
                _viewCanvasLobby.customerServiceOpenBtn.onClick.RemoveAllListeners();
                _viewCanvasLobby.customerServiceOpenBtn.onClick.AddListener(() => PopupManager.Instance.Open<PopupCustomerService>());
            }
            
            _viewCanvasLobby.boosterIcon.onClick.AddListener(() => { PopupManager.Instance.Open<PopupBooster>(); });
            _viewCanvasLobby.bokPocketIcon.onClick.AddListener(() => { PopupManager.Instance.Open<PopupBokPocket>(); });

            PopupManager.Instance.Setup<PopupGuideBook>();
            _viewCanvasLobby.guideBookIcon.onClick.AddListener(() =>
            {
                PopupManager.Instance.Open<PopupGuideIntro>();
            });



            initComplete = true;
        }

        private async UniTask InitLobbyData()
        {
            await FetchInventoryInfo();
            await FetchClassInfo();
            await FetchPointsInfo();
            await LoadLobbyAvatar();
            await CheckClaimableAchievements();
            await CheckPlayTimeMission();
        }

        private async UniTask FetchClassInfo()
        {
            var classInfoResult = await Services.Lobby.ClassInfoAsync();
            if (classInfoResult.IsSuccess && classInfoResult.Data != null)
            {
                CPPlayer.Inventory.classInfo = classInfoResult.Data;
                CPPlayer.Inventory.classNumber = classInfoResult.Data.ItemId switch
                {
                    nameof(ItemID.CLASS_B) => 1,
                    nameof(ItemID.CLASS_A) => 2,
                    nameof(ItemID.CLASS_S) => 3,
                    _ => 0
                };
            }
            else
            {
                CPPlayer.Inventory.classInfo = null;
                CPPlayer.Inventory.classNumber = 0;
            }
            CPPlayer.Inventory.classUpdateCallback?.Invoke();
        }

        private void CheckExpiredClassFromInventory()
        {
            if (CPPlayer.Inventory.inventoryInfo?.Inventory == null)
                return;

            foreach (var inv in CPPlayer.Inventory.inventoryInfo.Inventory)
            {
                if (string.IsNullOrEmpty( CPPlayer.Inventory.classInfo.ExpiredClassId))
                    continue;

                string className = CPPlayer.Inventory.GetClassDisplayNameFromItemId(CPPlayer.Inventory.classInfo.ExpiredClassId);
                if (string.IsNullOrEmpty(className))
                    break;

                CPPlayer.Inventory.classExpiredNotified = true;
                CPPlayer.Inventory.lastExpiredClassName = className;
                CPPlayer.Inventory.classNumber = 0;
                CPPlayer.Inventory.classInfo = null;
                CPPlayer.Inventory.classUpdateCallback?.Invoke();

                PopupManager.Instance.Open<PopupExpirationClass>(popup =>
                {
                    popup.SetDataConfirmOnly(
                        StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpired].StringToLocal,
                        StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpiredMoveToVault].StringToLocal,
                        className
                    );
                    popup.OnPopupClosed = () => RefreshClassInfoOnExpiry().Forget();
                });
                break;
            }
        }
        
        private async UniTask CheckClaimableAchievements()
        {
            bool hasClaimable = false;
            
            var missionPacket = await Services.Lobby.UserQuestListAsync();
            if (missionPacket.IsSuccess && missionPacket.Data?.QuestList != null)
            {
                // 받을 수 있는 미션 확인:
                // MaxCount > 0 (잘못된 데이터 무시)
                // QuestValue >= MaxCount (완료됨)
                // ReceivedRewardValue <= 0 (아직 안 받음)
                // ALL_IN, WATCH_AD 타입은 무시
                hasClaimable = missionPacket.Data.QuestList.Any(q => 
                                                                q.MaxCount > 0 && 
                                                                q.QuestValue >= q.MaxCount && 
                                                                q.ReceivedRewardValue <= 0 &&
                                                                !string.IsNullOrEmpty(q.Type) &&
                                                                !q.Type.Contains("ALL_IN") && 
                                                                !q.Type.Contains("WATCH_AD"));
            }
            
            CPPlayer.OutGame.newAchievementNotiCallback?.Invoke(hasClaimable);
        }

        void NickNameChangedCallback()
        {
            SettingLobbyDisplay();
            CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
            CheckAndOpenAvatarSelectPopup().Forget();
        }

        public void UpdateRoomHistoryAfterLogin()
        {
            //로그인시 기존 테이블 이력이 있다면 강제 입장
            if (LoginData.Cloud.loginValue.loginres.Position.RoomId > 0)
            {
                var loginPosition = LoginData.Cloud.loginValue.loginres.Position;
                switch (loginPosition.GameType)
                {
                    case Common.GameType.GtHoldem:
                        var holdemslot=holdemSlotList.Find(o => o.inGameRoomdata.RoomId == loginPosition.RoomId);
                        CPPlayer.InGame.EnterInGame?.Invoke(GameType.HOLDEM,holdemslot.gameMode, holdemslot.inGameRoomdata);
                        break;
                    case Common.GameType.GtBadugi:
                        var badugislot=holdemSlotList.Find(o => o.inGameRoomdata.RoomId == loginPosition.RoomId);
                        CPPlayer.InGame.EnterInGame?.Invoke(GameType.LOW_BADUGI,badugislot.gameMode, badugislot.inGameRoomdata);
                        break;
                    case Common.GameType.GtSevenPoker:
                        var sevenpokerslot=holdemSlotList.Find(o => o.inGameRoomdata.RoomId == loginPosition.RoomId);
                        CPPlayer.InGame.EnterInGame?.Invoke(GameType.SEVEN_POKER,sevenpokerslot.gameMode, sevenpokerslot.inGameRoomdata);
                        break;
                }
            }
        }

        private float elapsedUpdateRoomTime = 0;
        private float updateRoomTime = 5.0f;

        /// <summary>
        /// 룸 정보 업데이트(홀덤, 바두기 등)
        /// </summary>
        async UniTask UpdateRoomsInfo()
        {
            var roompacket_lobby=await Services.Lobby.GameRoomsReq();

            if (!roompacket_lobby.IsSuccess)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.FailedToLoadGameRoom].StringToLocal));
                return;
            }

            var holdemRoomInfo=roompacket_lobby.Data.List.FirstOrDefault(o => o.GameType == Common.GameType.GtHoldem);
            
            for (int i = 0; i < holdemRoomInfo.List.Count; i++)
            {
                int index = i;
                var slot = UnityEngine.GameObject.Instantiate(_viewCanvasLobby.slotPrefab);
                slot.transform.SetParent(_viewCanvasLobby.holdemSlotParent.content, false);
                slot.Init(holdemRoomInfo.List[index], GameType.HOLDEM, GameMode.Default);
                slot.enterGame.onClick.AddListener(() =>
                    OnClickHoldemRoomSlotEvent(GameType.HOLDEM, slot, holdemRoomInfo.List[index]));

                holdemSlotList.Add(slot);
            }
            
            var badugiRoomInfo=roompacket_lobby.Data.List.FirstOrDefault(o => o.GameType == Common.GameType.GtBadugi);
            
            List<lobby.RoomInfo> oneTooneRooms = badugiRoomInfo.List.Where(x => x.Type == lobby.RoomType.RtOneToOne).ToList();
            bool twoModeExist = false;
            for (int i = 0; i < badugiRoomInfo.List.Count; i++)
            {
                int index = i;
                if (badugiRoomInfo.List[index].Type == lobby.RoomType.RtOneToOne)
                {
                    twoModeExist = true;
                    continue;
                }
                var slot = UnityEngine.GameObject.Instantiate(_viewCanvasLobby.slotPrefab);
                slot.transform.SetParent(_viewCanvasLobby.badugiSlotParent.content, false);
                slot.Init(badugiRoomInfo.List[index], GameType.LOW_BADUGI, GameMode.Default);
                slot.enterGame.onClick.AddListener(() =>
                    OnClickBadugiRoomSlotEvent(GameType.LOW_BADUGI, slot, badugiRoomInfo.List[index],GameMode.Default));
            
                badugiSlotList.Add(slot);
            }

            if (twoModeExist)
            {
                var slot = UnityEngine.GameObject.Instantiate(_viewCanvasLobby.slotPrefab);
                slot.transform.SetParent(_viewCanvasLobby.badugiSlotParent.content, false);
                
                PopupManager.Instance.Setup<PopupOneOnOneMode>(popup => { popup.SetData(oneTooneRooms); });
                slot.Init(oneTooneRooms, GameType.LOW_BADUGI, GameMode.TwoVS);
                slot.enterGame.onClick.AddListener(() =>
                    OnClickBadugiRoomSlotEvent(GameType.LOW_BADUGI, slot, oneTooneRooms[0],GameMode.TwoVS));
                
                badugiSlotList.Add(slot);
            }
            
            
            var SpokerRoomInfo=roompacket_lobby.Data.List.FirstOrDefault(o => o.GameType == Common.GameType.GtSevenPoker);
            
            for (int i = 0; i < SpokerRoomInfo.List.Count; i++)
            {
                int index = i;
                var slot = UnityEngine.GameObject.Instantiate(_viewCanvasLobby.slotPrefab);
                slot.transform.SetParent(_viewCanvasLobby.SpokerSlotParent.content, false);
                slot.Init(SpokerRoomInfo.List[index], GameType.SEVEN_POKER, GameMode.Default);
                slot.enterGame.onClick.AddListener(() =>
                    OnClickSpokerRoomSlotEvent(GameType.SEVEN_POKER, slot, SpokerRoomInfo.List[index]));

                spokerSlotList.Add(slot);
            }
            
 
        }

        void OpenSetFirstNickNameWindow()
        {
            PopupManager.Instance.Open<PopupCreateNickname>();
            //PopupManager.Instance.Open<PopupCreateNickname>();
            //CPPlayer.OutGame.nickNameChangePopup?.Invoke(ItemID.FIRST_NICKNAME_CHANGE);
        }

        void OpenSetLossLimitWindow()
        {
            PopupManager.Instance.Open<PopupLossLimit>();
        }


        protected void SettingLobbyDisplay()
        {
            _viewCanvasLobby.userNickName.text = CPPlayer.UserInfo.userDatabase.User.Nick;
        }

        /// <summary>
        /// Enter Game
        /// </summary>
        /// <param name="roomdata"></param>
        private void OnClickHoldemRoomSlotEvent(GameType gameType, ViewRoomEnterSlot slot, lobby.RoomInfo data)
        {
            // if (slot.canEnterGame == false)
            //     return;

            TryEnterGameWithClassCheck(gameType, slot.gameMode, data);
        }
        private void OnClickBadugiRoomSlotEvent(GameType gameType, ViewRoomEnterSlot slot, lobby.RoomInfo data,GameMode gamemode = GameMode.Default)
        {
            // if (slot.canEnterGame == false)
            //     return;

            if (gamemode == GameMode.TwoVS)
                PopupManager.Instance.Open<PopupOneOnOneMode>();
            else
                TryEnterGameWithClassCheck(gameType, slot.gameMode, data);
        }
        private void OnClickSpokerRoomSlotEvent(GameType gameType, ViewRoomEnterSlot slot, lobby.RoomInfo data)
        {
            // if (slot.canEnterGame == false)
            //     return;
            
            TryEnterGameWithClassCheck(gameType, slot.gameMode, data);
        }

        private void TryEnterGameWithClassCheck(GameType gameType, GameMode gameMode, lobby.RoomInfo data)
        {
            bool canEnter = false;
            if (data.MaxBuyIn <= 0)
            {
                canEnter = CPPlayer.UserInfo.userDatabase.User.Gold >= data.MinBuyIn;
            }
            else
            {
                canEnter=CPPlayer.UserInfo.userDatabase.User.Gold>=data.MinBuyIn &&
                         CPPlayer.UserInfo.userDatabase.User.Gold<=data.MaxBuyIn;    
            }

            if (canEnter == false)
            {
                if (CPPlayer.UserInfo.userDatabase.User.Gold < data.MinBuyIn)
                {
                    PopupManager.Instance.Open<PopupCantEnterTable>(popup=>popup.SetDesc(data.MinBuyIn,true));
                }
                else
                {
                    PopupManager.Instance.Open<PopupCantEnterTable>(popup=>popup.SetDesc(data.MaxBuyIn,false));
                }

                return;
            }
            
            if (CPPlayer.Inventory.CheckClassExpiredLocally())
            {
                PopupManager.Instance.Open<PopupExpirationClass>(popup =>
                {
                    popup.SetData(
                        StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpired].StringToLocal,
                        StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpiredEnterRoom].StringToLocal,
                        CPPlayer.Inventory.lastExpiredClassName,
                        () =>
                        {
                            popup.Close();
                            CPPlayer.InGame.EnterInGame?.Invoke(gameType, gameMode, data);
                        }
                    );
                    popup.OnPopupClosed = () => RefreshClassInfoOnExpiry().Forget();
                });
            }
            else
            {
                CPPlayer.InGame.EnterInGame?.Invoke(gameType, gameMode, data);
            }
        }

        private void OnGameTabChanged(int index)
        {
            GameType gameType = index switch
            {
                0 => GameType.SEVEN_POKER,
                1 => GameType.LOW_BADUGI,
                2 => GameType.HOLDEM,
                _ => GameType.SEVEN_POKER
            };
            
            PlayerPrefs.SetString("LobbyGameTab", gameType.ToString());
            PlayerPrefs.Save();
            
            OpenRoomList(gameType);
        }

        private void CurrentOpenRoomList()
        {
            OpenRoomList(currentSelected); 
        }

        private void RestoreLastGameTab()
        {
            string savedType = PlayerPrefs.GetString("LobbyGameTab", GameType.HOLDEM.ToString());
            if (Enum.TryParse(savedType, out GameType gameType))
                currentSelected = gameType;
            else
                currentSelected = GameType.HOLDEM;

            int tabIndex = currentSelected switch
            {
                GameType.SEVEN_POKER => 0,
                GameType.LOW_BADUGI => 1,
                GameType.HOLDEM => 2,
                _ => 2
            };
            _viewCanvasLobby.gameTabGroup.SetActiveToggle(tabIndex, false);
            OpenRoomList(currentSelected);
            RestoreScrollPosition(currentSelected);
        }

        private void SaveScrollPosition(GameType gameType)
        {
            var scrollRect = GetScrollRect(gameType);
            if (scrollRect != null)
            {
                PlayerPrefs.SetFloat($"LastScrollPos_{gameType}", scrollRect.normalizedPosition.x);
                PlayerPrefs.Save();
            }
        }

        private void RestoreScrollPosition(GameType gameType)
        {
            var scrollRect = GetScrollRect(gameType);
            if (scrollRect == null) return;

            float savedPos = PlayerPrefs.GetFloat($"LastScrollPos_{gameType}", 0f);
            scrollRect.normalizedPosition = new Vector2(savedPos, scrollRect.normalizedPosition.y);
        }

        private ScrollRect GetScrollRect(GameType gameType)
        {
            return gameType switch
            {
                GameType.HOLDEM => _viewCanvasLobby.holdemSlotParent,
                GameType.LOW_BADUGI => _viewCanvasLobby.badugiSlotParent,
                GameType.SEVEN_POKER => _viewCanvasLobby.SpokerSlotParent,
                _ => null
            };
        }

        private void OpenRoomList(GameType gamet)
        {
            currentSelected = gamet;

            _viewCanvasLobby.badugiSlotParent.gameObject.SetActive(currentSelected == GameType.LOW_BADUGI);
            _viewCanvasLobby.holdemSlotParent.gameObject.SetActive(currentSelected == GameType.HOLDEM);
            _viewCanvasLobby.SpokerSlotParent.gameObject.SetActive(currentSelected == GameType.SEVEN_POKER);
            ActiveRoomSlotList();
        }

        void ActiveRoomSlotList()
        {
            if (currentSelected == GameType.LOW_BADUGI)
            {
                foreach (var viewRoomEnterSlot in badugiSlotList)
                {
                    viewRoomEnterSlot.ActivateSlot();
                }
            }
            else if (currentSelected == GameType.HOLDEM)
            {
                foreach (var viewRoomEnterSlot in holdemSlotList)
                {
                    viewRoomEnterSlot.ActivateSlot();
                }
            }
            else
            {
                foreach (var viewRoomEnterSlot in spokerSlotList)
                {
                    viewRoomEnterSlot.ActivateSlot();
                }
            }                                       
        }

        private void OnClickOpenOption()
        {
            _viewCanvasLobby.viewOption.gameObject.SetActive(true);
        }

        private void OnClickOpenProfile()
        {
        }

        void LobbyGoldTextAnimStart()
        {
            ActiveRoomSlotList();
        }

        async UniTask AfterPurchaseCallback(IAPProduct productData)
        {
            if (productData == null)
                return;

            if (productData.tapType == ShopMainTapType.AVATAR)
            {
                var userinfo = await Services.Lobby.GetUserInfoAsync();
                if (userinfo.IsSuccess)
                    CPPlayer.UserInfo.userDatabase = userinfo.Data;

                CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
            }

            if (productData.tapType == ShopMainTapType.CLASS)
            {
                CPPlayer.OutGame.RenewInventory?.Invoke();
            }
            if (productData.tapType == ShopMainTapType.ITEM)
            {
                CPPlayer.OutGame.RenewInventory?.Invoke();
                var userinfo = await Services.Lobby.GetUserInfoAsync();
                if (userinfo.IsSuccess)
                    CPPlayer.UserInfo.userDatabase = userinfo.Data;
                
                CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                
                if (IAPManager.StripProductPrefix(productData.productId) == "lucky_pocket")
                {
                    var pointsRes = await Services.Lobby.PointsReqAsync();
                    if (pointsRes.IsSuccess)
                    {
                        CPPlayer.Inventory.myPoints = pointsRes.Data.Points;
                        CPPlayer.Inventory.pointsUpdateCallback?.Invoke();
                    }
                }
            }
        }
        
        private void OpenNormalItemPurchasePopup(IAPProduct iapProduct, Sprite productSprite, System.Action<int> callback)
        {
            PopupManager.Instance.Open<PopupInAppPurchase>(popup => { popup.SetItemAndShowWindow(iapProduct, productSprite, callback); });
        }

        void InventoryItemUpdateCallback()
        {
            if (CPPlayer.Inventory.inventoryInfo == null)
            {
                _viewCanvasLobby.boosterIcon.gameObject.SetActive(false);
            }
            else
            {
                bool hasBooster = false;
                for (int i = 0; i < CPPlayer.Inventory.inventoryInfo.Inventory.Count; i++)
                {
                    if (CPPlayer.Inventory.inventoryInfo.Inventory[i].ItemId.Equals("BOOSTER", StringComparison.OrdinalIgnoreCase))
                    {
                        hasBooster = true;
                        break;
                    }
                }
                _viewCanvasLobby.boosterIcon.gameObject.SetActive(hasBooster);
            }

            if (CPPlayer.Inventory.myPoints != null)
            {
                bool canShowBokPocket = CPPlayer.Inventory.myPoints.LuckyBox >= 100000 && CPPlayer.Inventory.myPoints.WeeklyLuckyboxCnt < 3;
                _viewCanvasLobby.bokPocketIcon.gameObject.SetActive(canShowBokPocket);
            }
            else
            {
                _viewCanvasLobby.bokPocketIcon.gameObject.SetActive(false);
            }
        }

        private async UniTask FetchPointsInfo()
        {
            var pointsRes = await Services.Lobby.PointsReqAsync();
            if (pointsRes.IsSuccess)
            {
                CPPlayer.Inventory.myPoints = pointsRes.Data.Points;
                CPPlayer.Inventory.pointsUpdateCallback?.Invoke();
            }
        }

        private async UniTask FetchInventoryInfo()
        {
            var inventoryRes = await Services.Lobby.GetInventoryAsync(true, _cts.Token);
            if (inventoryRes.IsSuccess)
            {
                CPPlayer.Inventory.inventoryInfo = inventoryRes.Data;
                CPPlayer.Inventory.inventoryUpdateCallback?.Invoke();
            }
        }

        private void OnReturnToLobby()
        {
            // 로비로 돌아올 때 포인트 정보 새로고침
            FetchPointsInfo().Forget();
            // 플레이 시간 체크 (1시간 미션)
            CheckPlayTimeMission().Forget();
            HandleVerifyExpiredOnLobby();
            // 클래스 만료 체크
            CheckClassExpiryOnReturn();
        }

        private async UniTask CheckPlayTimeMission()
        {
            // 이미 노티가 켜져 있거나 보상 받았으면 스킵
            if (CPPlayer.OutGame.playTimeMissionClaimable || CPPlayer.OutGame.playTimeMissionRewarded)
                return;

            var res = await Services.Lobby.UserGameOnReqAsync();
            if (!res.IsSuccess || res.Data == null)
                return;

            int playTimeMinutes = (int)(res.Data.OnSec / 60);
            CPPlayer.OutGame.cachedPlayTimeMinutes = playTimeMinutes;
            CPPlayer.OutGame.playTimeCheckedAt = Time.realtimeSinceStartup;

            if (playTimeMinutes < 60)
                return;

            await Services.Lobby.UserQuestAddAsync("PLAY_TIME");

            var missionPacket = await Services.Lobby.UserQuestListAsync();
            if (missionPacket.IsSuccess && missionPacket.Data?.QuestList != null)
            {
                var playTimeMission = missionPacket.Data.QuestList.FirstOrDefault(q => q.Type == "PLAY_TIME");
                if (playTimeMission != null && playTimeMission.ReceivedRewardValue > 0)
                {
                    CPPlayer.OutGame.playTimeMissionRewarded = true;
                    return;
                }
            }

            CPPlayer.OutGame.playTimeMissionClaimable = true;
        }

        private void CheckPlayTimeNotiLocal()
        {
            if (CPPlayer.OutGame.playTimeMissionClaimable || CPPlayer.OutGame.playTimeMissionRewarded)
                return;

            if (CPPlayer.OutGame.playTimeCheckedAt <= 0)
                return;

            float elapsedSeconds = Time.realtimeSinceStartup - CPPlayer.OutGame.playTimeCheckedAt;
            int currentPlayTimeMinutes = CPPlayer.OutGame.cachedPlayTimeMinutes + (int)(elapsedSeconds / 60);

            if (currentPlayTimeMinutes >= 60)
            {
                CPPlayer.OutGame.playTimeMissionClaimable = true;
                CPPlayer.OutGame.newAchievementNotiCallback?.Invoke(true);
            }
        }

        private void CheckClassExpiryOnReturn()
        {
            TryOpenWithExpiryCheck(null).Forget();
        }

        private void ShowExpiryConfirmPopup(string className, Action afterConfirm = null)
        {
            PopupManager.Instance.Open<PopupExpirationClass>(popup =>
            {
                popup.SetDataConfirmOnly(
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpired].StringToLocal,
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpiredMoveToVault].StringToLocal,
                    className
                );
                popup.OnPopupClosed = () =>
                {
                    afterConfirm?.Invoke();
                };
            });
        }

        private async UniTaskVoid TryOpenWithExpiryCheck(Action openAction)
        {
            if (CPPlayer.Inventory.classExpiredNotified)
            {
                openAction?.Invoke();
                return;
            }

            if (CPPlayer.Inventory.classNumber > 0)
            {
                string prevClassName = CPPlayer.Inventory.GetClassDisplayName(CPPlayer.Inventory.classInfo);

                var result = await Services.Lobby.ClassInfoAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    CPPlayer.Inventory.classInfo = result.Data;
                    CPPlayer.Inventory.classNumber = result.Data.ItemId switch
                    {
                        nameof(ItemID.CLASS_B) => 1,
                        nameof(ItemID.CLASS_A) => 2,
                        nameof(ItemID.CLASS_S) => 3,
                        _ => 0
                    };
                    CPPlayer.Inventory.classUpdateCallback?.Invoke();
                }
                else
                {
                    CPPlayer.Inventory.classInfo = null;
                    CPPlayer.Inventory.classNumber = 0;
                    CPPlayer.Inventory.classExpiredNotified = true;
                    CPPlayer.Inventory.lastExpiredClassName = prevClassName;
                    CPPlayer.Inventory.classUpdateCallback?.Invoke();

                    ShowExpiryConfirmPopup(prevClassName, openAction);
                    return;
                }
            }

            openAction?.Invoke();
        }

        private async UniTaskVoid RefreshClassInfoOnExpiry()
        {
            var classInfoResult = await Services.Lobby.ClassInfoAsync();
            if (classInfoResult.IsSuccess && classInfoResult.Data != null)
            {
                CPPlayer.Inventory.classInfo = classInfoResult.Data;
                CPPlayer.Inventory.classNumber = classInfoResult.Data.ItemId switch
                {
                    nameof(ItemID.CLASS_B) => 1,
                    nameof(ItemID.CLASS_A) => 2,
                    nameof(ItemID.CLASS_S) => 3,
                    _ => 0
                };
            }
            else
            {
                CPPlayer.Inventory.classInfo = null;
                CPPlayer.Inventory.classNumber = 0;
            }
            CPPlayer.Inventory.classUpdateCallback?.Invoke();
        }

        private void HandleVerifyExpiredOnLobby()
        {
            if (!CPPlayer.OutGame.pendingVerifyExpiredLogout)
                return;

            CPPlayer.OutGame.pendingVerifyExpiredLogout = false;
            var logout = CPPlayer.OutGame.logoutToLogin;
            CPPlayer.OutGame.logoutToLogin = null;

            PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(
                StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IdentityVerificationExpiredTitle].StringToLocal,
                StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.IdentityVerificationExpiredLogin].StringToLocal,
                () => logout?.Invoke()
            ));
            
        }

        private void FriendsRequest_Noti(lobby.FriendsNoti noti)
        {
            if (noti == null)
                return;

            if (noti.RequestType == lobby.FriendsRequestType.Request)
                CPPlayer.OutGame.pendingFriendRequestNotiUids.Add(noti.FriendsUid);
            else
                CPPlayer.OutGame.pendingFriendRequestNotiUids.Remove(noti.FriendsUid);

            CPPlayer.OutGame.friendRequestTypeNotiCallback?.Invoke(noti.RequestType);

            if (noti.RequestType == lobby.FriendsRequestType.Request)
            {
                CPPlayer.OutGame.hasNewFriendRequestNoti = true;
                CPPlayer.OutGame.newFriendRequestNotiCallback?.Invoke(true);
            }
        }
        
        private void PostsRecieved_Noti(lobby.PostsNoti noti)
        {
            CPPlayer.OutGame.newPostNotiCallback?.Invoke(true);
        }
        
        private void MsgRecieved_Noti(lobby.MessageNoti noti)
        {
            CPPlayer.OutGame.newMsgExistNotiCallback?.Invoke(true);
            CPPlayer.OutGame.newMessageNotiCallback?.Invoke(noti.RoomId,true);
        }

        private void RenewInventoryAndAvatar()
        {
            RenewInventoryAndAvatarAsync().Forget();
        }

        void SetAchievementNoti(bool active)
        {
            if (_viewCanvasLobby.achievementNoti != null) 
                _viewCanvasLobby.achievementNoti.SetActive(active);
        }
        
        private async UniTaskVoid RenewInventoryAndAvatarAsync()
        {
            await FetchInventoryInfo();
            await LoadLobbyAvatar();
        }

        private async UniTask LoadLobbyAvatar()
        {
            if (CPPlayer.Inventory.inventoryInfo?.Inventory == null)
                return;
            
            var equippedAvatar = CPPlayer.Inventory.inventoryInfo.Inventory
                .FirstOrDefault(inv => inv.IsEffective && IsAvatarItem(inv.ItemId));
            
            if (equippedAvatar == null)
            {
                if (_viewCanvasLobby.avatarImage != null)
                    _viewCanvasLobby.avatarImage.gameObject.SetActive(false);

                // 닉네임이 설정 안 됐으면 아바타 선택 팝업을 열지 않음 (닉네임 설정 후 열림)
                bool isNotNickNameSet = CPPlayer.UserInfo.userDatabase.User.IsFirstLogin == 1;
                if (!isNotNickNameSet)
                    OpenAvatarSelectPopup();
                return;
            }

            UpdateLobbyAvatarImage(equippedAvatar.ItemId);
        }

        private bool IsAvatarItem(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && itemId.StartsWith("AVATAR_");
        }
        
        private void OpenAvatarSelectPopup()
        {
            PopupManager.Instance.Open<PopupCreateAvatar>(popup =>
            {
                popup.OnAvatarSelected = (avatarId) =>
                {
                    RefreshLobbyAvatarAfterSelection(avatarId).Forget();
                };
            });
        }
        
        private async UniTaskVoid CheckAndOpenAvatarSelectPopup()
        {
            var inventoryRes = await Services.Lobby.GetInventoryAsync(true, _cts.Token);
            if (!inventoryRes.IsSuccess || inventoryRes.Data?.Inventory == null)
                return;
            
            var equippedAvatar = inventoryRes.Data.Inventory
                .FirstOrDefault(inv => inv.IsEffective && IsAvatarItem(inv.ItemId));
            
            if (equippedAvatar == null)
                OpenAvatarSelectPopup();
        }
        
        private async UniTaskVoid RefreshLobbyAvatarAfterSelection(string avatarId)
        {
            await FetchInventoryInfo();
            UpdateLobbyAvatarImage(avatarId);
        }
        
        private void UpdateLobbyAvatarImage(string avatarId)
        {
            var avatarBundle = ItemBundle.Loaded;
            if (avatarBundle == null)
                return;

            var avatarSprite = avatarBundle.GetAvatarSprite(avatarId);
            if (avatarSprite != null && _viewCanvasLobby.avatarImage != null)
            {
                _viewCanvasLobby.avatarImage.gameObject.SetActive(true);
                _viewCanvasLobby.avatarImage.sprite = avatarSprite;
                _viewCanvasLobby.avatarImage.SetNativeSize();
                _viewCanvasLobby.avatarImage.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
            }
        }

        private void KickRecieved_Noti(lobby.KickNoti kickNoti)
        {
            Debug.LogError("kicknoti send");
            if (kickNoti.Reason == KickReason.KrDuplicate)
            {
                //ConnectionManager.Instance.Dispose();
                PopupManager.Instance.Open<PopupToast>(popup => popup.ActivateBigwindowTwoBtn(
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.DuplicateAccess].StringToLocal,StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.DuplicateAccessMsg].StringToLocal,()=>
                    {
                        LogoutForThisAccount();
                    } ));
            }
            else if (kickNoti.Reason == KickReason.KrMaintenance)
            {
                //ConnectionManager.Instance.Dispose();
                PopupManager.Instance.Open<PopupServerMaintenance>(popup=>popup.SetMaintenanceKick());
            }
            else
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"{kickNoti.Reason}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ServerConnectionDisconnected].StringToLocal}"));
            }
        }
        
        private void Maintenance_Noti(lobby.MaintenanceNoti maintenance)
        {
            PopupManager.Instance.Open<PopupServerMaintenance>(popup=>popup.SetMaintenanceTime(maintenance));
        }

        private const string LogOutSceneName = "Loading";
        
        void LogoutForThisAccount()
        {
            
            CPPlayer.Dispose();
            PoolManager.Clear();
            PopupManager.Instance.CloseAll();
            
            SceneManager.LoadScene(LogOutSceneName);
        }

    }
}