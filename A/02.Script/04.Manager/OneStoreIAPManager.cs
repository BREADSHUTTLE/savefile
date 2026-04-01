using System;
using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using UnityEngine;
using OneStore.Purchasing;
using OneStore.Auth;

namespace CAPYBARA
{
#if UNITY_ANDROID
    public class OneStoreIAPManager : IPurchaseCallback
    {
        private string licenseKey =
            "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCEsCfHBvenhMLgmJUhKCHJDUI0Cd9MnfcGHiQ41hSVBxijstLdz3YSkKJTzFHUfuSOBP+Zs+lOLQrn8HS9sttDOAzXFXwLk0NE8Rd/xxbW7K2KPfF/7iLGqH4q77sI2rsuB+3DjD+lan0JEgOOM6N8igi3bYaEgfhUe6RjXbw34wIDAQAB";

        private PurchaseClientImpl purchaseClient;
        private OneStoreAuthClientImpl authClient;
        private Dictionary<string, ProductType> productCatalog = new Dictionary<string, ProductType>();

        private List<ProductDetail> oneStoreproductDetails = new List<ProductDetail>();
        private int _pendingQuantity = 1; // 다중구매 시 수량 저장

        public void Initialize()
        {
            authClient = new OneStoreAuthClientImpl();
            authClient.LaunchSignInFlow((SignInResult) =>
            {
                if (SignInResult.IsSuccessful())
                {
                    InitPurchase();
                }
                else
                {
                    Debug.LogError($"로그인 에러{SignInResult.Message}");
                }
            });
        }

        private void InitPurchase()
        {
            purchaseClient = new PurchaseClientImpl(licenseKey);
            purchaseClient.Initialize(this);

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

                var productType = item.InAppItemId.Contains("SUBSCRIBE") ? ProductType.SUBS : ProductType.INAPP;
                if (!productCatalog.ContainsKey(item.ProductId))
                {
                    productCatalog.Add(item.ProductId, productType);
                    Debug.LogWarning($"[IAPManager] 상품 등록됨 - ProductId: {item.ProductId}, Type: {productType}");
                }
            }

            QueryProduct();

            purchaseClient.QueryPurchases(ProductType.INAPP);
            purchaseClient.QueryPurchases(ProductType.SUBS);
        }

        private void QueryProduct()
        {
            Dictionary<OneStore.Purchasing.ProductType, List<string>> productTypeDic = new Dictionary<OneStore.Purchasing.ProductType, List<string>>();
            foreach (var item in productCatalog)
            {
                OneStore.Purchasing.ProductType productType = OneStore.Purchasing.ProductType.INAPP;
                if (item.Value == ProductType.SUBS)
                {
                    productType = OneStore.Purchasing.ProductType.SUBS;
                }
                else
                {
                    productType = OneStore.Purchasing.ProductType.INAPP;
                }

                if (productTypeDic.ContainsKey(productType))
                {
                    productTypeDic[productType].Add(item.Key);
                }
                else
                {
                    List<string> newlist = new List<string>();
                    newlist.Add(item.Key);
                    productTypeDic.Add(productType, newlist);
                }
            }

            foreach (var productType in productTypeDic)
            {
                purchaseClient.QueryProductDetails(productType.Value.AsReadOnly(), productType.Key);
            }
        }

        public void PurchaseProduct(string productId, int quantity = 1)
        {
            var productDetail = oneStoreproductDetails.FirstOrDefault(o => o.productId == productId);
            if (productDetail == null)
            {
                Debug.LogError($"[OneStoreIAPManager] 상품을 찾을 수 없음 - productID: {productId}");
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PurchaseFailed].StringToLocal, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.StoreInitializing].StringToLocal, null));
                return;
            }

            _pendingQuantity = quantity;

            if (IAPManager.StripProductPrefix(productId).Contains("class_"))
            {
                CheckSubscriptionAndBuy(productId).Forget();
                return;
            }

            ProcessPurchaseProduct(productId);
        }

        public void ProcessPurchaseProduct(string productId)
        {
            var productDetail = oneStoreproductDetails.FirstOrDefault(o => o.productId == productId);
            if (productDetail == null)
            {
                Debug.LogError($"[OneStoreIAPManager] 상품을 찾을 수 없음 - productID: {productId}");
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PurchaseFailed].StringToLocal, StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.StoreInitializing].StringToLocal, null));
                return;
            }

            ProductType productType = ProductType.Get(productDetail.type);

            var purchaseFlowParams = new PurchaseFlowParams.Builder()
                .SetProductId(productId) // mandatory
                .SetProductType(productType) // mandatory
                //.SetDeveloperPayload(developerPayload)  // optional
                //.SetQuantity(quantity)                  // optional
                // .SetProductName(null)                // optional: Change the name of the product to appear on the purchase screen.

                // It should be used only in advance consultation with the person in charge of the One Store business, and is not normally used.
                // .SetGameUserId(null)                 // optional: User ID to use for promotion.
                // .SetPromotionApplicable(false)       // optional: Whether to participate in the promotion.
                .Build();

            Debug.LogError($"구매 바로 전 로그 추적: productId:{productId}, productType:{productType}");

            purchaseClient.Purchase(purchaseFlowParams);
        }

        public void OnSetupFailed(IapResult iapResult)
        {
            throw new System.NotImplementedException();
        }

        public void OnProductDetailsSucceeded(List<ProductDetail> productDetails)
        {
            Debug.Log("초기화 완료");

            for (int i = 0; i < productDetails.Count; i++)
            {
                oneStoreproductDetails.Add(productDetails[i]);
            }
        }

        public void OnProductDetailsFailed(IapResult iapResult)
        {
            Debug.Log($"초기화 실패 / 사유:{iapResult.Message}");
            if (iapResult.Message == "RESULT_NEED_UPDATE")
            {
                purchaseClient.LaunchUpdateOrInstallFlow(OneStoreServiceInstalled);
            }
        }

        void OneStoreServiceInstalled(IapResult iapResult)
        {
            Debug.Log($"oneStore 서비스 설치 결과 / {iapResult.Message}");
        }

        public void OnPurchaseSucceeded(List<PurchaseData> purchases)
        {
            for (int i = 0; i < purchases.Count; i++)
            {
                PurchaseData args = purchases[i];
                string productId = args.ProductId;
                string receipt = args.JsonReceipt;
                string token = args.PurchaseToken;

                Debug.LogError($"{args.ProductId} call and purchased");
                int quantity = _pendingQuantity;
                // 구독 상품은 여기서 처리하고 리셋, 비구독 상품은 OnConsumeSucceeded에서 리셋

                if (args.ProductId.Contains("subscribe"))
                {
                    _pendingQuantity = 1;
                    SendReceiptAndGiveItemToUser(args, quantity).Forget();
                }
                else
                {
                    // _pendingQuantity는 OnConsumeSucceeded에서 리셋됨
                    handlePurchase(args);
                }

                Debug.LogError(
                    $"purchaseState:{args.PurchaseState}//productId:{args.ProductId}//receipt:{args.JsonReceipt}//token:{args.PurchaseToken}//orderId:{args.OrderId}//RecurringState:{args.RecurringState}");
            }

            Debug.Log("구매에 성공하였습니다.");

            //서버로 토큰 값 등 전달
            //서버 토큰값 전달 후 소비 함수 호출
        }

        private async UniTask CheckSubscriptionAndBuy(string productId)
        {
            var configItem = ConfigDataManager.GetInAppItemByProductId(productId);
            if (configItem == null)
            {
                Debug.LogWarning($"[OneStoreIAPManager] config에서 상품을 찾을 수 없음: {productId}");
                return;
            }

            bool isPurchaseSubscribe = configItem.InAppItemId.Contains("SUBSCRIBE");
            bool isPurchase30Day = configItem.InAppItemId.Contains("_30");

            int purchaseGradeNumber = 0;
            if (Enum.TryParse<ItemID>(configItem.ItemId, out var itemId))
            {
                purchaseGradeNumber = GetClassGradeNumber(itemId);
            }

            int currentClassNumber = CPPlayer.Inventory.classNumber;
            bool isCurrentSubscription = CPPlayer.Inventory.classInfo?.ClassPaymentType?.ToUpper() == "RECURRING";

            Debug.Log(
                $"[OneStoreIAPManager] 클래스 구매 체크 - currentClassNumber: {currentClassNumber}, purchaseGradeNumber: {purchaseGradeNumber}, isCurrentSubscription: {isCurrentSubscription}, isPurchase30Day: {isPurchase30Day}");

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

            if (isCurrentSubscription && isPurchaseSubscribe)
            {
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowPopupTwoButtons(
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Notice].StringToLocal,
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ExistingSubscriptionWarning].StringToLocal,
                    () => ProcessPurchaseProduct(productId),
                    null
                ));
                return;
            }

            if (currentClassNumber > 0 && !isCurrentSubscription && isPurchase30Day)
            {
                // 동일 등급이면 팝업 없이 바로 구매
                if (currentClassNumber == purchaseGradeNumber)
                {
                    Debug.Log($"[OneStoreIAPManager] 동일 등급 30일권 재구매 - 바로 진행: {productId}");
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

            Debug.Log($"[OneStoreIAPManager] 클래스 구매 진행: {productId}");
            ProcessPurchaseProduct(productId);
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


        public void handlePurchase(PurchaseData purchaseData)
        {
            purchaseClient.ConsumePurchase(purchaseData);
        }

        public void OnPurchaseFailed(IapResult iapResult)
        {
            Debug.Log("구매에 실패하였습니다.");

            Debug.LogError($"구매 실패:{iapResult.Message}//{iapResult.Code}");
        }

        public void OnConsumeSucceeded(PurchaseData purchase)
        {
            int quantity = _pendingQuantity;
            _pendingQuantity = 1;
            SendReceiptAndGiveItemToUser(purchase, quantity).Forget();
        }

        async UniTask SendReceiptAndGiveItemToUser(PurchaseData purchase, int quantity)
        {
            Debug.Log("구매 후 영수증 검증 통과 후 아이템 사용 성공");

            Debug.Log($"[INAPPPURCHASE] BuyProductById: {purchase.ProductId}, 수량: {quantity}]_2.5");

            string serverProductId = IAPManager.GetServerProductId(purchase.ProductId);
            var packetRes = await Services.Lobby.PurchaseReqAsync(serverProductId, purchase.PurchaseToken, InAppPlatform.Onestore);

            if (packetRes == null)
                return;
            if (packetRes.IsSuccess)
            {
                Debug.Log($"[INAPPPURCHASE] BuyProductById: {purchase.ProductId}]_4");
                HandleReward(purchase.ProductId, quantity).Forget();
            }
        }

        private async UniTask HandleReward(string productId, int quantity)
        {
            Debug.Log($"[INAPPPURCHASE] BuyProductById: {productId}]_6");

            var productData = StaticData.Wrapper.iAPProducts.FirstOrDefault(o => o.productId == productId);
            string stripped = IAPManager.StripProductPrefix(productId);

            string message = GetPurchaseMessage(productId, quantity);
            if (!string.IsNullOrEmpty(message))
                PopupManager.Instance.Open<PopupToast>(popup=>popup.ShowBottomPopup(message, false));

            if (stripped == "nickname_change")
                PopupManager.Instance.Open<PopupChangeNickname>();

            if (stripped == "booster" || (productData != null && productData.subTapType == ShopSubTapType.BOOSTER))
            {
                Debug.Log("[OneStoreIAPManager] 부스터 IAP 결제 완료 - 인벤토리 갱신");
                var inventoryRes = await Services.Lobby.GetInventoryAsync(true);
                if (inventoryRes.IsSuccess)
                {
                    CPPlayer.Inventory.inventoryInfo = inventoryRes.Data;
                    CPPlayer.Inventory.inventoryUpdateCallback?.Invoke();
                }
            }

            if (stripped.Contains("lucky") || (productData != null && productData.subTapType == ShopSubTapType.LUCKY_POCKET))
            {
                Debug.Log("[OneStoreIAPManager] 복주머니 IAP 결제 완료 - PointsRewardReqAsync 호출");
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
            string stripped = IAPManager.StripProductPrefix(productId);

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
                int messageCount = stripped switch
                {
                    "messege_100" => 100,
                    "messege_50" => 50,
                    "messege_20" => 20,
                    _ => 0
                };
                if (messageCount > 0)
                    return quantity > 1 ? string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PurchaseNoteWithQuantity].StringToLocal, messageCount, messageCount, quantity) : string.Format(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PurchaseNote].StringToLocal, messageCount);
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


        public void OnConsumeFailed(IapResult iapResult)
        {
            Debug.Log("구매 후 영수증 검증 통과 후 아이템 사용 실패");
        }

        public void OnAcknowledgeSucceeded(PurchaseData purchase, ProductType type)
        {
            throw new System.NotImplementedException();
        }

        public void OnAcknowledgeFailed(IapResult iapResult)
        {
            throw new System.NotImplementedException();
        }

        public void OnManageRecurringProduct(IapResult iapResult, PurchaseData purchase, RecurringAction action)
        {
            throw new System.NotImplementedException();
        }

        public void OnNeedUpdate()
        {
            throw new System.NotImplementedException();
        }

        public void OnNeedLogin()
        {
            throw new System.NotImplementedException();
        }

        public decimal GetLocalPricedecimal(string productId)
        {
            var productDetail = oneStoreproductDetails.FirstOrDefault(o => o.productId == productId);
            if (productDetail == null)
            {
                Debug.LogWarning($"[OneStoreIAPManager] 가격 조회 실패 - 상품을 찾을 수 없음: {productId}");
                return 0;
            }

            var price_long = productDetail.priceAmountMicros / 1000000;
            decimal price = price_long;

            Debug.Log($"{productId}::가격은 {price}");
            return price;
        }
    }
#endif
}