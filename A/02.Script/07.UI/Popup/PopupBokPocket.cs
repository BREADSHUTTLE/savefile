using System.Linq;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PopupBokPocket : BasePopup
    {
        [Header("Info")]
        [SerializeField] private CPButton descBtn;
        [SerializeField] private GameObject descPopup;
        
        [Header("Purchase")]
        [SerializeField] private Text txtRemainingCount;
        [SerializeField] private CPButton purchaseBokPocketBtn;

        protected override void OnInit()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (descBtn != null)
            {
                descBtn.onClick.RemoveAllListeners();
                descBtn.onClick.AddListener(() =>
                {
                    descPopup.SetActive(!descPopup.activeInHierarchy);
                });
                descPopup.SetActive(false);
            }

            if (purchaseBokPocketBtn != null)
            {
                purchaseBokPocketBtn.onClick.RemoveAllListeners();
                purchaseBokPocketBtn.onClick.AddListener(PurchaseBokPocket);
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            CPPlayer.Inventory.pointsUpdateCallback += OnPointsUpdated;
            UpdateUI();
        }

        protected override void OnClose()
        {
            CPPlayer.Inventory.pointsUpdateCallback -= OnPointsUpdated;
            base.OnClose();
        }
        
        private void OnPointsUpdated()
        {
            UpdateUI();

            var points = CPPlayer.Inventory.myPoints;
            if (points != null && (points.LuckyBox < Constraints.REQUIRED_POINTS || points.WeeklyLuckyboxCnt >= Constraints.MAX_WEEKLY_COUNT))
                Close();
        }

        private void UpdateUI()
        {
            var points = CPPlayer.Inventory.myPoints;
            if (points == null)
                return;

            int currentPoints = points.LuckyBox;
            int weeklyCount = points.WeeklyLuckyboxCnt;
            int remainingCount = Constraints.MAX_WEEKLY_COUNT - weeklyCount;
            bool canPurchase = currentPoints >= Constraints.REQUIRED_POINTS && remainingCount > 0;

            if (txtRemainingCount != null)
                txtRemainingCount.text = weeklyCount.ToString();

            if (purchaseBokPocketBtn != null)
                purchaseBokPocketBtn.enabled = canPurchase;
        }

        private void PurchaseBokPocket()
        {
            var luckyPocketProduct = StaticData.Wrapper.iAPProducts.FirstOrDefault(p => p.subTapType == ShopSubTapType.LUCKY_POCKET);
            if (luckyPocketProduct == null)
            {
                Debug.LogError("[BokPocket] LUCKY_POCKET 상품을 찾을 수 없습니다");
                return;
            }
            
            Debug.Log($"[BokPocket] IAP 결제 요청 - {luckyPocketProduct.productId}");
            CommonIAPManager.Instance.BuyProductById(luckyPocketProduct.productId, 1);
        }
    }
}
