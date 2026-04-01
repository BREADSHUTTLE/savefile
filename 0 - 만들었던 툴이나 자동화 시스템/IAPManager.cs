using CAPYBARA.Core;
using CAPYBARA.Definition;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.lobby;
using UnityEngine;
using UnityEngine.Purchasing;
using Unity.Services.Core;

namespace CAPYBARA
{
    public class IAPManager : IStoreListener
    {
        private IStoreController storeController;
        private IExtensionProvider storeExtensionProvider;
        private int _pendingQuantity = 1;  // 다중구매 시 수량 저장

        public Dictionary<string, ProductType> productCatalog = new Dictionary<string, ProductType>();

        public void Initialize()
        {
            productCatalog = new Dictionary<string, ProductType>();
            
            string currentPlatform = Application.platform == RuntimePlatform.IPhonePlayer ? "APPLE" : "GOOGLE";
            
            Debug.LogWarning($"[IAPManager] 초기화 시작 - 현재 플랫폼: {currentPlatform}");
            Debug.LogWarning($"[IAPManager] ConfigDataManager.inAppItems 개수: {ConfigDataManager.inAppItems?.Count ?? 0}");
            
            foreach (var item in ConfigDataManager.inAppItems)
            {
                Debug.LogWarning($"[IAPManager] 상품 확인 - ProductId: {item.ProductId}, Platform: {item.Platform}, InAppItemId: {item.InAppItemId}");
                
                if (item.Platform != currentPlatform)
                {
                    Debug.LogWarning($"[IAPManager] 플랫폼 불일치로 스킵 - 상품 플랫폼: {item.Platform}, 현재 플랫폼: {currentPlatform}");
                    continue;
                }
                    
                var productType = item.InAppItemId.Contains("SUBSCRIBE") ? ProductType.Subscription : ProductType.Consumable;
                    
                if (!productCatalog.ContainsKey(item.ProductId))
                {
                    productCatalog.Add(item.ProductId, productType);
                    Debug.LogWarning($"[IAPManager] 상품 등록됨 - ProductId: {item.ProductId}, Type: {productType}");
                }
            }
            
            Debug.LogWarning($"[IAPManager] 총 등록된 상품 수: {productCatalog.Count}");
            InitializePurchasing().Forget();
        }

        private async UniTask InitializePurchasing()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    Debug.LogWarning("[IAPManager] Unity Gaming Services 초기화 중...");
                    await UnityServices.InitializeAsync();
                    Debug.LogWarning("[IAPManager] Unity Gaming Services 초기화 완료");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAPManager] Unity Gaming Services 초기화 실패: {e.Message}");
                return;
            }

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var item in productCatalog)
            {
                builder.AddProduct(item.Key, item.Value, new IDs
                {
                    { item.Key, AppleAppStore.Name },
                    { item.Key, GooglePlay.Name },
                });
            }

            UnityPurchasing.Initialize(this, builder);
        }

        public bool IsInitialized()
        {
            return storeController != null && storeExtensionProvider != null;
        }

        public void BuyProductById(string productId)
        {
            Debug.Log($"[INAPPPURCHASE] BuyProductById: {productId}]_0");
            CheckAndProcessPurchase(productId).Forget();
        }

        private async UniTask CheckAndProcessPurchase(string productId)
        {
            var purchaseMonthlyRes = await Services.Lobby.PurchaseMonthlyInfoAsync();
            if (purchaseMonthlyRes.IsSuccess)
                CPPlayer.UserInfo.purchaseMonthlyDatabase = purchaseMonthlyRes.Data;

            var productPrice = GetLocalPricedecimal(productId);
            if (CPPlayer.UserInfo.purchaseMonthlyDatabase.RemainAmount > productPrice)
                ProcessPurchaseProduct(productId);
            else
                PopupManager.Instance.Open<PopupPaymentLimitExceeded>(popup => popup.SetRemainingAmount(CPPlayer.UserInfo.purchaseMonthlyDatabase.RemainAmount));
        }

        public void CheckLimitAndPurchase(string productId)
        {
            if (!IsInitialized())
            {
                Debug.LogWarning("[IAPManager] Unity IAP 초기화되지 않음");
                return;
            }

            string stripped = StripProductPrefix(productId);
            if (stripped.Contains("class_"))
            {
                CheckSubscriptionAndBuy(productId).Forget();
                return;
            }

            ProcessPurchaseProduct(productId);
        }

        private async UniTask CheckSubscriptionAndBuy(string productId)
        {
            var configItem = ConfigDataManager.GetInAppItemByProductId(productId);
            if (configItem == null)
            {
                Debug.LogWarning($"[IAPManager] config에서 상품을 찾을 수 없음: {productId}");
                return;
            }

            bool isPurchaseSubscribe = configItem.InAppItemId.Contains("SUBSCRIBE");
            bool isPurchase30Day = configItem.InAppItemId.Contains("_30");
            var paymentType = isPurchaseSubscribe ? CAPYBARA.lobby.ClassPaymentType.Recurring : CAPYBARA.lobby.ClassPaymentType.Single;

            int purchaseGradeNumber = 0;
            var gradeType = CAPYBARA.lobby.ClassGradeType.GradeNone;
            if (Enum.TryParse<ItemID>(configItem.ItemId, out var itemId))
            {
                gradeType = GetClassGradeType(itemId);
                purchaseGradeNumber = GetClassGradeNumber(itemId);
            }

            int currentClassNumber = CPPlayer.Inventory.classNumber;
            bool isCurrentSubscription = CPPlayer.Inventory.classInfo?.ClassPaymentType?.ToUpper() == "RECURRING";
            bool isAppStore = Application.platform == RuntimePlatform.IPhonePlayer;
            
            Debug.Log($"[IAPManager] 클래스 구매 체크 - currentClassNumber: {currentClassNumber}, purchaseGradeNumber: {purchaseGradeNumber}, isCurrentSubscription: {isCurrentSubscription}, isPurchase30Day: {isPurchase30Day}, ClassPaymentType: {CPPlayer.Inventory.classInfo?.ClassPaymentType}");
            
            // 구독 > 30일권 구매 - 구독 취소 안내
            if (isCurrentSubscription && isPurchase30Day)
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowPopupTwoButtons(
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SubscriptionCancelNotice].StringToLocal,
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Subscription30DayCancelMsg].StringToLocal,
                    () => ProcessPurchaseProduct(productId),
                    null
                ));
                return;
            }

            // 앱스토어 구독 > 구독 재구매 - 철회 안내
            if (isAppStore && isCurrentSubscription && isPurchaseSubscribe)
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowPopupTwoButtons(
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Notice].StringToLocal,
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ExistingSubscriptionWarning].StringToLocal,
                    () => ProcessPurchaseProduct(productId),
                    null
                ));
                return;
            }

            // 30일 > 30일 구매 (비구독 상태에서 30일권 구매)
            if (currentClassNumber > 0 && !isCurrentSubscription && isPurchase30Day)
            {
                // 동일 등급이면 팝업 없이 바로 구매
                if (currentClassNumber == purchaseGradeNumber)
                {
                    Debug.Log($"[IAPManager] 동일 등급 30일권 재구매 - 바로 진행: {productId}");
                    ProcessPurchaseProduct(productId);
                    return;
                }
                
                // 다른 등급이면 기존 등급 사라짐 안내
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowPopupTwoButtons(
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassChangeNotice].StringToLocal,
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.GradeWillDisappear].StringToLocal,
                    () => ProcessPurchaseProduct(productId),
                    null
                ));
                return;
            }

            Debug.Log($"[IAPManager] 클래스 구매 진행: {productId}");
            ProcessPurchaseProduct(productId);
        }

        private void ContinueClassPurchase(string productId, CAPYBARA.lobby.ClassGradeType gradeType, CAPYBARA.lobby.ClassPaymentType paymentType, int purchaseGradeNumber)
        {
            Debug.Log($"[IAPManager] 클래스 구매 진행: {productId}");
            ProcessPurchaseProduct(productId);
        }

        public void ProcessPurchaseProduct(string productId, int quantity = 1)
        {
            string actualProductId = GetActualProductId(productId, quantity);
            
#if UNITY_EDITOR || IAP_TEST
            Debug.Log($"<color=yellow>테스트 모드 - 스토어 없이 구매 처리 (수량: {quantity}, 상품ID: {actualProductId})</color>");
            EditorTestPurchase(actualProductId, quantity).Forget();
          
            return;
#endif
            if (!IsInitialized())
            {
                Debug.LogWarning($"[IAPManager] Unity IAP 초기화되지 않음 - 구매 불가: {actualProductId}");
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PurchaseFailed].StringToLocal, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.StoreInitializing].StringToLocal, null));
                return;
            }
            
            _pendingQuantity = quantity;
            
            Product product = storeController.products.WithID(actualProductId);

            if (product != null && product.availableToPurchase)
            {
                storeController.InitiatePurchase(product);
                Debug.Log($"[IAPManager] 구매 요청: {actualProductId}, 수량: {quantity}");
            }
            else
            {
                Debug.LogError($"[IAPManager] 유효하지 않은 상품 ID이거나 구매 불가 상태: {actualProductId}");
            }

            Debug.Log($"[INAPPPURCHASE] BuyProductById: {actualProductId}_1");
        }

#if UNITY_EDITOR || IAP_TEST
        private async UniTask EditorTestPurchase(string productId, int quantity = 1)
        {
            Debug.Log($"<color=cyan>테스트 구매 시작: {productId}, 수량: {quantity}</color>");

            var productData = StaticData.Wrapper.iAPProducts.FirstOrDefault(o => o.productId == productId);
            var configItems = ConfigDataManager.GetInAppItemsByProductId(productId);
            
            string baseProductId = GetBaseProductId(productId, quantity);
            if (productData == null && baseProductId != productId)
                productData = StaticData.Wrapper.iAPProducts.FirstOrDefault(o => o.productId == baseProductId);
            if ((configItems == null || configItems.Count == 0) && baseProductId != productId)
                configItems = ConfigDataManager.GetInAppItemsByProductId(baseProductId);
            
            string currentPlatform = Application.platform == RuntimePlatform.IPhonePlayer ? "APPLE" : "GOOGLE";
            var targetItems = configItems?.Where(i => string.IsNullOrEmpty(i.Platform) || i.Platform == currentPlatform).ToList();

            if (targetItems == null || targetItems.Count == 0)
            {
                Debug.LogError($"<color=red>테스트 구매 실패 - config item 없음: {productId} (platform: {currentPlatform})</color>");
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowPopupOneButton($"테스트 구매 실패: config item 없음 ({productId})"));
                return;
            }

            if (StripProductPrefix(productId).StartsWith("class_"))
            {
                Debug.Log($"<color=green>[테스트] 클래스 구매 진행: {productId}</color>");
                targetItems = targetItems.OrderBy(i => i.ItemId.Contains("CURRENCY") ? 1 : 0).ToList();
            }

            string serverProductId = GetServerProductId(productId);

            // 인벤토리 아이템 추가
            foreach (var configItem in targetItems)
            {
                var itemId = configItem?.ItemId;
                if (string.IsNullOrEmpty(itemId))
                {
                    Debug.LogError($"<color=red>테스트 구매 실패 - itemId 없음: {productId}</color>");
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowPopupOneButton($"테스트 구매 실패: itemId 없음 ({productId})"));
                    return;
                }

                var amount = (int)Math.Max(1, Math.Min(configItem.Amount, (long)int.MaxValue));
                var result = await Services.Lobby.AddItemToInventoryAsync(itemId, amount, serverProductId);

                if (!result.IsSuccess)
                {
                    Debug.LogError($"<color=red>테스트 구매 실패 - 서버 응답 에러: {result.Error}</color>");
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowPopupOneButton($"테스트 구매 실패: {result.Error}"));
                    return;
                }

                Debug.Log($"<color=green>테스트 - 서버 인벤토리 추가 성공: {itemId} x{amount}</color>");
            }

            await HandleTestPurchaseReward(productId, productData, quantity);
        }

        private async UniTask ContinueEditorTestPurchase(string productId, IAPProduct productData, List<CAPYBARA.lobby.ConfigInAppItems> targetItems, CAPYBARA.lobby.ClassGradeType gradeType, CAPYBARA.lobby.ClassPaymentType paymentType, int purchaseGradeNumber)
        {
            int currentClassNumber = CPPlayer.Inventory.classNumber;
            if (currentClassNumber > 0)
            {
                string currentClassName = GetClassNameByNumber(currentClassNumber);
                string purchaseClassName = GetClassNameByNumber(purchaseGradeNumber);
                Debug.Log($"<color=yellow>[테스트] 클래스 변경: {currentClassName} -> {purchaseClassName} (현재 클래스 즉시 해지)</color>");
            }
            
            Debug.Log($"<color=green>[테스트] 클래스 구매 진행!</color>");

            string serverProductId = GetServerProductId(productId);

            // 인벤토리 아이템 추가
            foreach (var configItem in targetItems)
            {
                var itemId = configItem?.ItemId;
                if (string.IsNullOrEmpty(itemId))
                {
                    Debug.LogError($"<color=red>테스트 구매 실패 - itemId 없음: {productId}</color>");
                    PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowPopupOneButton($"테스트 구매 실패: itemId 없음 ({productId})"));
                    return;
                }

                var amount = (int)Math.Max(1, Math.Min(configItem.Amount, (long)int.MaxValue));
                var result = await Services.Lobby.AddItemToInventoryAsync(itemId, amount, serverProductId);

                if (!result.IsSuccess)
                {
                    Debug.LogError($"<color=red>테스트 구매 실패 - 서버 응답 에러: {result.Error}</color>");
                    PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton($"테스트 구매 실패: {result.Error}"));
                    return;
                }

                Debug.Log($"<color=green>테스트 - 서버 인벤토리 추가 성공: {itemId} x{amount}</color>");
            }

            await HandleTestPurchaseReward(productId, productData);
        }

        private async UniTask HandleTestPurchaseReward(string productId, IAPProduct productData, int quantity = 1)
        {
            string stripped = StripProductPrefix(productId);
            if (stripped == "booster")
            {
                CPPlayer.UserInfo.hasBooster = true;
                CPPlayer.Inventory.eventHasBooster?.Invoke();
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup("[테스트] 부스터를 획득하셨습니다", false));
            }
            else if (stripped == "nickname_change")
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup("[테스트] 닉네임 변경권을 획득하셨습니다", false));
                PopupManager.Instance.Open<PopupChangeNickname>();
            }
            else if (stripped.StartsWith("gold_"))
            {
                long beforeGold = CPPlayer.UserInfo.userDatabase?.User?.Gold ?? 0;
                var userInfo = await Services.Lobby.GetUserInfoAsync();
                if (userInfo.IsSuccess)
                {
                    CPPlayer.UserInfo.userDatabase = userInfo.Data;
                    long afterGold = userInfo.Data.User.Gold;
                    CPPlayer.Balance.MyBalTextAnimEvent?.Invoke(beforeGold, afterGold);
                }

                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup("[테스트] 골드를 획득하셨습니다", false));
            }
            else if (stripped.StartsWith("messege_"))
            {
                string quantityMsg = quantity > 1 ? $" x{quantity}" : "";
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"[테스트] 쪽지를 획득하셨습니다{quantityMsg}", false));
            }
            else if (stripped.StartsWith("class_"))
            {
                long beforeGold = CPPlayer.UserInfo.userDatabase?.User?.Gold ?? 0;
                var userInfo = await Services.Lobby.GetUserInfoAsync();
                if (userInfo.IsSuccess)
                {
                    CPPlayer.UserInfo.userDatabase = userInfo.Data;
                    long afterGold = userInfo.Data.User.Gold;
                    CPPlayer.Balance.MyBalTextAnimEvent?.Invoke(beforeGold, afterGold);
                }

                var classInfoResult = await Services.Lobby.ClassInfoAsync();
                if (classInfoResult.IsSuccess && classInfoResult.Data != null)
                {
                    CPPlayer.Inventory.classInfo = classInfoResult.Data;
                    CPPlayer.Inventory.classNumber = GetClassNumberByItemId(classInfoResult.Data.ItemId);
                    CPPlayer.Inventory.classExpiredNotified = false;
                    CPPlayer.Inventory.classUpdateCallback?.Invoke();
                }

                var inventoryRes = await Services.Lobby.GetInventoryAsync(true);
                if (inventoryRes.IsSuccess)
                {
                    CPPlayer.Inventory.inventoryInfo = inventoryRes.Data;
                    CPPlayer.Inventory.inventoryUpdateCallback?.Invoke();
                }

                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"[테스트] 클래스를 획득하셨습니다", false));
            }
            else if (stripped.Contains("lucky") || (productData != null && productData.subTapType == ShopSubTapType.LUCKY_POCKET))
            {
                // 복주머니 구매 시 PointsRewardReqAsync 호출 (포인트 차감 + 골드 지급)
                Debug.Log("[IAPManager] 테스트 - 복주머니 IAP 결제 완료 - PointsRewardReqAsync 호출");
                var rewardRes = await Services.Lobby.PointsRewardReqAsync(PointsRewardType.PRT_LUCKYBOX.ToString());
                if (rewardRes.IsSuccess)
                {
                    CPPlayer.Inventory.myPoints = rewardRes.Data?.Points ?? new lobby.Points();
                    CPPlayer.Inventory.pointsUpdateCallback?.Invoke();
                    CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                }
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"[테스트] 복주머니를 구매하셨습니다", false));
            }
            else
            {
                string quantityMsg = quantity > 1 ? $" x{quantity}" : "";
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup($"[테스트] {productId} 구매 완료{quantityMsg}", false));
            }

            CPPlayer.OutGame.AfterPurchase?.Invoke(productData);
            Debug.Log($"<color=green>테스트 구매 완료: {productId}, 수량: {quantity}</color>");
        }
#endif

        #region IStoreListener Implementation

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            storeExtensionProvider = extensions;
            Debug.Log($"[INAPPPURCHASE] Initialize success");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InitFailed].StringToLocal, $"Unity IAP 초기화 실패 원인: {error}", null));
            Extension.eLog($"[IAPManager] Unity IAP 초기화 실패: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.InitFailed].StringToLocal, $"Unity IAP 초기화 실패 원인: {message}", null));
            Debug.LogError($"[IAPManager] Unity IAP 초기화 실패: {error} / {message}");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PurchaseFailed].StringToLocal, $"구매 실패하였습니다. 원인: {failureReason}", null));
            Debug.LogError($"[IAPManager] 구매 실패: {product.definition.id}, 이유: {failureReason}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string productId = args.purchasedProduct.definition.id;
            string receipt = args.purchasedProduct.receipt;

            Extension.eLog($"[IAPManager] 구매 완료: {productId}, 수량: {_pendingQuantity}", Color.green);
            
            int quantity = _pendingQuantity;
            _pendingQuantity = 1;
            
            SendReceiptAndGiveItemToUser(productId, receipt, quantity).Forget();

            Debug.Log($"[INAPPPURCHASE] BuyProductById: {productId}]_2");

            return PurchaseProcessingResult.Complete;
        }

        #endregion

        #region Purchase Processing

        private async UniTask SendReceiptAndGiveItemToUser(string productId, string receipt, int quantity)
        {
            Debug.Log($"[INAPPPURCHASE] BuyProductById: {productId}]_2.5");
            
            string serverProductId = GetServerProductId(productId);
            var packetRes = await Services.Lobby.PurchaseReqAsync(serverProductId, receipt,InAppPlatform.Google);
            
            if (packetRes == null)
                return;
            
            Debug.Log($"[INAPPPURCHASE] BuyProductById: {productId}]_4");
            HandleReward(productId, quantity).Forget();
        }

        private async UniTask HandleReward(string productId, int quantity)
        {
            Debug.Log($"[INAPPPURCHASE] BuyProductById: {productId}]_6");

            var productData = StaticData.Wrapper.iAPProducts.FirstOrDefault(o => o.productId == productId);
            if (productData == null && quantity > 1)
            {
                string baseId = GetBaseProductId(productId, quantity);
                if (baseId != productId)
                    productData = StaticData.Wrapper.iAPProducts.FirstOrDefault(o => o.productId == baseId);
            }

            string stripped = StripProductPrefix(productId);
            string message = GetPurchaseMessage(productId, quantity);
            if (!string.IsNullOrEmpty(message))
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(message, false));

            if (stripped == "nickname_change")
                PopupManager.Instance.Open<PopupChangeNickname>();
            
            if (stripped == "booster" || (productData != null && productData.subTapType == ShopSubTapType.BOOSTER))
            {
                Debug.Log("[IAPManager] 부스터 IAP 결제 완료 - 인벤토리 갱신");
                var inventoryRes = await Services.Lobby.GetInventoryAsync(true);
                if (inventoryRes.IsSuccess)
                {
                    CPPlayer.Inventory.inventoryInfo = inventoryRes.Data;
                    CPPlayer.Inventory.inventoryUpdateCallback?.Invoke();
                }
            }

            if (stripped.Contains("lucky") || (productData != null && productData.subTapType == ShopSubTapType.LUCKY_POCKET))
            {
                Debug.Log("[IAPManager] 복주머니 IAP 결제 완료 - PointsRewardReqAsync 호출");
                var rewardRes = await Services.Lobby.PointsRewardReqAsync(PointsRewardType.PRT_LUCKYBOX.ToString());
                if (rewardRes.IsSuccess)
                {
                    CPPlayer.Inventory.myPoints = rewardRes.Data?.Points ?? new lobby.Points();
                    CPPlayer.Inventory.pointsUpdateCallback?.Invoke();
                    CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                }
            }

            if (stripped.StartsWith("class_"))
            {
                long beforeGold = CPPlayer.UserInfo.userDatabase?.User?.Gold ?? 0;
                var userInfo = await Services.Lobby.GetUserInfoAsync();
                if (userInfo.IsSuccess)
                {
                    CPPlayer.UserInfo.userDatabase = userInfo.Data;
                    long afterGold = userInfo.Data.User.Gold;
                    CPPlayer.Balance.MyBalTextAnimEvent?.Invoke(beforeGold, afterGold);
                }

                var classInfoResult = await Services.Lobby.ClassInfoAsync();
                if (classInfoResult.IsSuccess && classInfoResult.Data != null)
                {
                    CPPlayer.Inventory.classInfo = classInfoResult.Data;
                    CPPlayer.Inventory.classNumber = GetClassNumberByItemId(classInfoResult.Data.ItemId);
                    CPPlayer.Inventory.classExpiredNotified = false;
                    CPPlayer.Inventory.classUpdateCallback?.Invoke();
                }

                var inventoryRes = await Services.Lobby.GetInventoryAsync(true);
                if (inventoryRes.IsSuccess)
                {
                    CPPlayer.Inventory.inventoryInfo = inventoryRes.Data;
                    CPPlayer.Inventory.inventoryUpdateCallback?.Invoke();
                }
            }

            Debug.Log($"[INAPPPURCHASE] BuyProductById: {productId}]_7");
            CPPlayer.OutGame.AfterPurchase?.Invoke(productData);
        }

        private string GetPurchaseMessage(string productId, int quantity)
        {
            string stripped = StripProductPrefix(productId);

            if (stripped.StartsWith("gold_"))
                return quantity > 1
                    ? $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AvatarGoldPurchased].StringToLocal} (X {quantity})"
                    : StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.AvatarGoldPurchased].StringToLocal;

            if (stripped == "nickname_change")
                return quantity > 1 ? $"{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.NicknameChangePurchased].StringToLocal} (X {quantity}개)" : StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.NicknameChangePurchased].StringToLocal;

            if (stripped == "booster")
                return StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.BoosterPurchased].StringToLocal;

            if (stripped.StartsWith("messege_"))
            {
                int messageCount = 0;
                if (stripped.StartsWith("messege_100")) messageCount = 100;
                else if (stripped.StartsWith("messege_50")) messageCount = 50;
                else if (stripped.StartsWith("messege_20")) messageCount = 20;
                
                if (messageCount > 0)
                {
                    int totalCount = messageCount * quantity;
                    return quantity > 1 
                        ? string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PurchaseNoteWithQuantity].StringToLocal, totalCount, messageCount, quantity) 
                        : string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PurchaseNote].StringToLocal, messageCount);
                }
            }

            if (stripped.StartsWith("class_"))
            {
                return stripped switch
                {
                    "class_b_30" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassB30Purchased].StringToLocal,
                    "class_a_30" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassA30Purchased].StringToLocal,
                    "class_s_30" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassS30Purchased].StringToLocal,
                    "class_b_subscribe" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassBSubPurchased].StringToLocal,
                    "class_a_subscribe" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassASubPurchased].StringToLocal,
                    "class_s_subscribe" => StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ClassSSubPurchased].StringToLocal,
                    _ => null
                };
            }

            Debug.Log($"[보상] 기타 상품 지급 처리: {productId}");
            return null;
        }

        #endregion

        #region Utility Methods

        public int GetQuantity(PurchaseEventArgs args)
        {
            try
            {
                var receipt = args.purchasedProduct.receipt;
                var receiptWrapper = JObject.Parse(receipt);
                var payloadStr = receiptWrapper["Payload"]?.ToString();

                if (!string.IsNullOrEmpty(payloadStr))
                {
                    var payloadJson = JObject.Parse(payloadStr);
                    var jsonStr = payloadJson["json"]?.ToString();

                    if (!string.IsNullOrEmpty(jsonStr))
                    {
                        var json = JObject.Parse(jsonStr);
                        return json["quantity"]?.Value<int>() ?? 1;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"수량 파싱 실패: {e.Message}");
            }

            return 1;
        }

        public decimal GetLocalPricedecimal(string productId)
        {
            if (storeController?.products?.WithID(productId) == null)
                return 0;

            Product p = storeController.products.WithID(productId);
            decimal price = p.metadata.localizedPrice;
            
            Debug.Log($"{productId}::가격은 {price}");
            return price;
        }

        public string GetLocalPriceString(string productId)
        {
            if (storeController?.products?.WithID(productId) == null)
                return string.Empty;

            Product p = storeController.products.WithID(productId);
            string price = p.metadata.localizedPriceString;
            
            Debug.Log($"{productId}::가격은 {price}");
            return price;
        }

        private static CAPYBARA.lobby.ClassGradeType GetClassGradeType(ItemID itemId)
        {
            return itemId switch
            {
                ItemID.CLASS_S => CAPYBARA.lobby.ClassGradeType.ClassS,
                ItemID.CLASS_A => CAPYBARA.lobby.ClassGradeType.ClassA,
                ItemID.CLASS_B => CAPYBARA.lobby.ClassGradeType.ClassB,
                _ => CAPYBARA.lobby.ClassGradeType.GradeNone
            };
        }

        private static int GetClassGradeNumber(ItemID itemId)
        {
            return itemId switch
            {
                ItemID.CLASS_B => 1,
                ItemID.CLASS_A => 2,
                ItemID.CLASS_S => 3,
                _ => 0
            };
        }

        private static string GetClassNameByNumber(int classNumber)
        {
            return classNumber switch
            {
                1 => "B",
                2 => "A",
                3 => "S",
                _ => ""
            };
        }

        private static int GetClassNumberByItemId(string itemId)
        {
            return itemId switch
            {
                nameof(ItemID.CLASS_B) => 1,
                nameof(ItemID.CLASS_A) => 2,
                nameof(ItemID.CLASS_S) => 3,
                _ => 0
            };
        }

        private const string ProductPrefix = "pokergame_";

        public static string StripProductPrefix(string productId)
        {
            if (!string.IsNullOrEmpty(productId) && productId.StartsWith(ProductPrefix, StringComparison.OrdinalIgnoreCase))
                return productId.Substring(ProductPrefix.Length);
            return productId;
        }

        public static string GetStaticMatchKey(string productId)
        {
            string key = StripProductPrefix(productId);
            int xIdx = key.LastIndexOf("_X");
            if (xIdx > 0 && int.TryParse(key.Substring(xIdx + 2), out _))
                return key.Substring(0, xIdx);
            if (key.Contains("subscribe"))
            {
                int lastUnderscore = key.LastIndexOf('_');
                if (lastUnderscore > 0 && int.TryParse(key.Substring(lastUnderscore + 1), out _))
                    return key.Substring(0, lastUnderscore);
            }
            return key;
        }

        public static string GetServerProductId(string productId)
        {
            if (productId.Contains("subscribe"))
            {
                int userIdx = CPPlayer.UserInfo.userDatabase?.User?.UserIdx ?? 0;
                if (userIdx > 0)
                    return $"{productId}_{userIdx}";
            }
            return productId;
        }

        public static string GetActualProductId(string baseProductId, int quantity)
        {
            return quantity > 1 ? $"{baseProductId}_X{quantity}" : baseProductId;
        }

        public static string GetBaseProductId(string actualProductId, int quantity)
        {
            string suffix = $"_X{quantity}";
            if (quantity > 1 && actualProductId.EndsWith(suffix))
            {
                return actualProductId.Substring(0, actualProductId.Length - suffix.Length);
            }
            return actualProductId;
        }

        #endregion
    }
}
