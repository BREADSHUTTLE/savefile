using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID
using OneStore.Common;
#endif


namespace CAPYBARA
{
    public class CommonIAPManager : MonoSingleton<CommonIAPManager>
    {
#if UNITY_ANDROID
        StoreType storeType;
        OneStoreIAPManager oneStoreIAPManager;
#endif
        IAPManager iapManager;

        public void Initialize()
        {
#if UNITY_EDITOR
            iapManager = new IAPManager();
            iapManager.Initialize();
#elif UNITY_ANDROID
            storeType = StoreEnvironment.GetStoreType();
            if (storeType == StoreType.ONESTORE)
            {
                oneStoreIAPManager = new OneStoreIAPManager();
                oneStoreIAPManager.Initialize();
            }
            else
            {
                iapManager = new IAPManager();
                iapManager.Initialize();
            }
#else
            // iOS
            iapManager = new IAPManager();
            iapManager.Initialize();
#endif
        }

        public void BuyProductById(string productId, int quantity = 1)
        {
            Debug.Log($"[INAPPPURCHASE] BuyProductById: {productId}, quantity: {quantity}]_0");
            
            if (IAPManager.StripProductPrefix(productId).StartsWith("class_") && quantity <= 1)
            {
#if UNITY_EDITOR
                iapManager.CheckLimitAndPurchase(productId);
#elif UNITY_ANDROID
                if (storeType == StoreType.ONESTORE)
                {
                    oneStoreIAPManager.PurchaseProduct(productId);
                }
                else
                {
                    iapManager.CheckLimitAndPurchase(productId);
                }
#else
                // iOS
                iapManager.CheckLimitAndPurchase(productId);
#endif
                return;
            }
            
            CheckAndProcessPurchase(productId, quantity).Forget();
        }

        async UniTask CheckAndProcessPurchase(string productId, int quantity = 1)
        {
            var purchaseMonthlyRes = await Services.Lobby.PurchaseMonthlyInfoAsync();
            if (purchaseMonthlyRes.IsSuccess)
                CPPlayer.UserInfo.purchaseMonthlyDatabase = purchaseMonthlyRes.Data;

            // 수량별 실제 상품ID로 가격 조회 (예 messege_100_X5의 가격)
            string actualProductId = IAPManager.GetActualProductId(productId, quantity);
            var productPrice = GetLocalPricedecimal(actualProductId);
            // 수량 상품 가격을 못 찾으면 기본 상품 가격 * 수량으로 폴백
            if (productPrice == 0 && quantity > 1)
                productPrice = GetLocalPricedecimal(productId) * quantity;
            
            if (CPPlayer.UserInfo.purchaseMonthlyDatabase.RemainAmount >= productPrice)
            {
#if UNITY_EDITOR
                iapManager.ProcessPurchaseProduct(productId, quantity);
#elif UNITY_ANDROID
                if (storeType == StoreType.ONESTORE)
                {
                    oneStoreIAPManager.PurchaseProduct(productId, quantity);
                }
                else
                {
                    iapManager.ProcessPurchaseProduct(productId, quantity);
                }
#else
                // iOS
                iapManager.ProcessPurchaseProduct(productId, quantity);
#endif
            }
            else
            {
                PopupManager.Instance.Open<PopupPaymentLimitExceeded>(popup => popup.SetRemainingAmount(CPPlayer.UserInfo.purchaseMonthlyDatabase.RemainAmount));
            }
        }

        public decimal GetLocalPricedecimal(string productId)
        {
#if UNITY_EDITOR
            return iapManager.GetLocalPricedecimal(productId);
#elif UNITY_ANDROID
            if (storeType == StoreType.ONESTORE)
            {
                return oneStoreIAPManager.GetLocalPricedecimal(productId);
            }
            else
            {
                return iapManager.GetLocalPricedecimal(productId);
            }
#else
            // iOS
            return iapManager.GetLocalPricedecimal(productId);
#endif
        }
    }
}