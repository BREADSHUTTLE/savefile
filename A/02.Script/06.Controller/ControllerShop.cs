using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using CAPYBARA.Definition;
using CAPYBARA.Bundles;

namespace CAPYBARA.Core
{
    public class ControllerShop
    {
        private ViewShop _viewShop;
        private CancellationTokenSource cts;

        private ShopMainTapType currentSelectedTap;
        private ShopSubTapType currentSubSelectedTap;

        private List<IAPProduct> _currentGoldItems = new List<IAPProduct>();
        private List<IAPProduct> _currentClassItems = new List<IAPProduct>();
        private List<IAPProduct> _currentNormalItems = new List<IAPProduct>();

        private readonly ShopMainTapType[] mainTabs =
        {
            ShopMainTapType.AVATAR,
            ShopMainTapType.CLASS,
            ShopMainTapType.ITEM
        };
        private readonly ShopSubTapType[] goldSubTabs =
        {
            ShopSubTapType.AVATAR_NORMAL,
            ShopSubTapType.AVATAR_CLASS_B,
            ShopSubTapType.AVATAR_CLASS_A,
            ShopSubTapType.AVATAR_CLASS_S
        };
        private readonly ShopSubTapType[] classSubTabs =
        {
            ShopSubTapType.CLASS_SUB,
            ShopSubTapType.CLASS
        };
        private readonly ShopSubTapType[] itemSubTabs =
        {
            ShopSubTapType.ALL,
            ShopSubTapType.MSG,
            ShopSubTapType.NICK_CHANGE,
            ShopSubTapType.BOOSTER,
            ShopSubTapType.LUCKY_POCKET,
        };
        private ShopMainTapType? pendingMainTab;

        // 각 메인탭별 마지막 선택한 서브탭 저장
        private Dictionary<ShopMainTapType, ShopSubTapType> lastSelectedSubTab = new Dictionary<ShopMainTapType, ShopSubTapType>();
        
        // 서브탭별 스크롤 위치 저장 (같은 ScrollView를 공유하므로 별도 관리)
        private Dictionary<ShopSubTapType, Vector2> savedSubTabScrollPositions = new Dictionary<ShopSubTapType, Vector2>();

        private Tweener scrollTween;
        private bool _suppressTabCallback;

        public ControllerShop(ViewShop _view, CancellationTokenSource _cts)
        {
            cts = _cts;
            _viewShop = _view;

            currentSelectedTap = ShopMainTapType.AVATAR;
            currentSubSelectedTap = GetDefaultItemType(currentSelectedTap);
            _viewShop.closeBtn.onClick.AddListener(() => _viewShop.gameObject.SetActive(false));

            SetItemsInShopList(currentSelectedTap, currentSubSelectedTap);

            CPPlayer.Inventory.shopClassToastPopup += OpenSubscribePopup;
            CPPlayer.OutGame.openShopUI += () => OpenShop().Forget();
            CPPlayer.OutGame.openShopUIWithTab += (tab, onBeforeOpen) => OpenShopWithTab(tab, onBeforeOpen).Forget();
            CPPlayer.Inventory.pointsUpdateCallback += OnPointsUpdated;

            _viewShop.onEnabled += OnViewEnabled;
            _viewShop.onScrollDragBegin += StopScrollAnimation;

            _viewShop.mainTabGroup.onIndexChanged += OnClickMainTab;

            _viewShop.goldCategoryTab.onIndexChanged += OnClickSubTab;
            _viewShop.classCategoryTab.onIndexChanged += OnClickSubTab;
            _viewShop.itemCategoryTab.onIndexChanged += OnClickSubTab;

            _viewShop.goldScrollView.OnCellUpdate = OnGoldCellUpdate;
            _viewShop.classScrollView.OnCellUpdate = OnClassCellUpdate;
            _viewShop.normalScrollView.OnCellUpdate = OnNormalCellUpdate;

            _viewShop.classInfoBtn.onClick.RemoveAllListeners();
            _viewShop.classInfoBtn.onClick.AddListener(OnClickClassInfo);
        }

        private void OnPointsUpdated()
        {
            if (!_viewShop.gameObject.activeInHierarchy)
                return;

            UpdateItemTabsVisibility();

            if (currentSelectedTap == ShopMainTapType.ITEM)
                SetItemsInShopList(currentSelectedTap, currentSubSelectedTap);
        }

        private async UniTask OpenShop()
        {
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            await RefreshClassInfo();
            await RefreshEventInfo();

            _viewShop.gameObject.SetActive(true);
            await UniTask.Yield();

            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
        }

        // 특정 탭으로 상점 열기 (onBeforeOpen: 상점 뷰 활성화 직전에 호출할 콜백)
        private async UniTask OpenShopWithTab(ShopMainTapType targetTab, Action onBeforeOpen = null)
        {
            pendingMainTab = targetTab;

            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            await RefreshClassInfo();
            await RefreshEventInfo();

            // 상점이 열리기 직전에 이전 화면 닫기
            onBeforeOpen?.Invoke();

            _viewShop.gameObject.SetActive(true);
            await UniTask.Yield();

            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
        }

        private async UniTask RefreshClassInfo()
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
                Debug.Log("[ControllerShop] 클래스 정보 없음");
            }
        }

        private async UniTask RefreshEventInfo()
        {
            var eventResult = await Services.Lobby.EventGetAsync();
            if (eventResult.IsSuccess && eventResult.Data != null)
                CPPlayer.OutGame.eventList = eventResult.Data.EventList.ToList();
            else
                CPPlayer.OutGame.eventList = null;
        }

        private void OnViewEnabled()
        {
            Canvas.ForceUpdateCanvases();

            UpdateItemTabsVisibility();
            UpdateAvatarClassTabsVisibility();

            currentSelectedTap = pendingMainTab ?? ShopMainTapType.AVATAR;
            currentSubSelectedTap = GetDefaultItemType(currentSelectedTap);
            pendingMainTab = null;

            lastSelectedSubTab.Clear();
            savedSubTabScrollPositions.Clear();

            _suppressTabCallback = true;

            int mainTabIndex = System.Array.IndexOf(mainTabs, currentSelectedTap);
            if (mainTabIndex < 0) mainTabIndex = 0;
            _viewShop.mainTabGroup.SetActiveToggle(mainTabIndex);

            var subTab = GetCurrentSubCategoryTab();
            int subTabIndex = GetSubTabIndex(currentSubSelectedTap);
            subTab.SetActiveToggle(subTabIndex);

            _suppressTabCallback = false;

            SetItemsInShopList(currentSelectedTap, currentSubSelectedTap);
            ResetAllScrollPositions();
        }

        private void UpdateItemTabsVisibility()
        {
            // 부스터 탭 표시 조건 : 가입 후 7일 이내 && 인벤토리에 BOOSTER 아이템 없음
            bool canShowBooster = !IsWeekPassedSinceSignup() && !HasBoosterInInventory();

            // 복주머니 탭 표시 조건 : LuckyBox >= 100000 && 주간 구매 횟수 3회 미만
            bool canShowLuckyPocket = CPPlayer.Inventory.myPoints != null
                                    && CPPlayer.Inventory.myPoints.LuckyBox >= 100000
                                    && CPPlayer.Inventory.myPoints.WeeklyLuckyboxCnt < 3;

            var toggles = _viewShop.itemCategoryTab.Toggles;
            if (toggles.Count > 3)
                toggles[3].gameObject.SetActive(canShowBooster);
            if (toggles.Count > 4)
                toggles[4].gameObject.SetActive(canShowLuckyPocket);
        }

        private void UpdateAvatarClassTabsVisibility()
        {
            // 이벤트 중이 아니면 AVATAR 탭의 클래스 서브 탭들 숨김
            bool isEventActive = CPPlayer.OutGame.IsEventActive;

            var toggles = _viewShop.goldCategoryTab.Toggles;
            if (toggles.Count > 1)
                toggles[1].gameObject.SetActive(isEventActive);
            if (toggles.Count > 2)
                toggles[2].gameObject.SetActive(isEventActive);
            if (toggles.Count > 3)
                toggles[3].gameObject.SetActive(isEventActive);
        }

        private bool IsWeekPassedSinceSignup()
        {
            long regDt = LoginData.Cloud.loginValue.loginres?.RegDt ?? 0;
            if (regDt == 0)
                return true; // 가입일자를 알 수 없으면 부스터 표시 안함
            
            DateTime signupDate = DateTimeOffset.FromUnixTimeSeconds(regDt).LocalDateTime;
            TimeSpan diff = DateTime.Now - signupDate;
            return diff.TotalDays >= 7; // 7일 지남
        }

        private bool HasBoosterInInventory()
        {
            if (CPPlayer.Inventory.inventoryInfo == null) return false;

            for (int i = 0; i < CPPlayer.Inventory.inventoryInfo.Inventory.Count; i++)
            {
                if (CPPlayer.Inventory.inventoryInfo.Inventory[i].ItemId.Equals("BOOSTER", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void ResetAllScrollPositions()
        {
            _viewShop.goldScrollView.ScrollToTop();
            _viewShop.classScrollView.ScrollToTop();
            _viewShop.normalScrollView.ScrollToTop();
        }

        private void SetActiveCategoryTab(ShopMainTapType tapType)
        {
            _viewShop.goldCategoryTab.gameObject.SetActive(tapType == ShopMainTapType.AVATAR);
            _viewShop.classCategoryTab.gameObject.SetActive(tapType == ShopMainTapType.CLASS);
            _viewShop.itemCategoryTab.gameObject.SetActive(tapType == ShopMainTapType.ITEM);
        }

        private void SetItemsInShopList(ShopMainTapType topTapType, ShopSubTapType itemType)
        {
            SetActiveCategoryTab(topTapType);

            List<IAPProduct> selectedinfos;
            bool isEventActive = CPPlayer.OutGame.IsEventActive;
            var allProductIds = new HashSet<string>(ConfigDataManager.inAppItems.Select(c => c.ProductId));

            switch (topTapType)
            {
                case ShopMainTapType.AVATAR:
                    var productIds = ConfigDataManager.inAppItems.Select(c => NormalizeProductId(c.ProductId, allProductIds)).Distinct().Where(productId =>
                        {
                            var s = IAPManager.StripProductPrefix(productId);
                            switch (itemType)
                            {
                                case ShopSubTapType.AVATAR_NORMAL:
                                    if (isEventActive)
                                        return s.StartsWith("gold_x_");
                                    else
                                        return s.StartsWith("gold_n_");
                                case ShopSubTapType.AVATAR_CLASS_B:
                                    return s.StartsWith("gold_b_");
                                case ShopSubTapType.AVATAR_CLASS_A:
                                    return s.StartsWith("gold_a_");
                                case ShopSubTapType.AVATAR_CLASS_S:
                                    return s.StartsWith("gold_s_");
                                default:
                                    return false;
                            }
                        })
                        .ToList();

                    selectedinfos = productIds
                        .Select(pid =>
                        {
                            var staticProduct = StaticData.Wrapper.iAPProducts?.FirstOrDefault(p => p.productId == IAPManager.GetStaticMatchKey(pid));
                            return new IAPProduct
                            {
                                tapType = staticProduct?.tapType ?? ShopMainTapType.AVATAR,
                                subTapType = staticProduct?.subTapType ?? InferSubTapType(pid),
                                productId = pid,
                                title_Kr = staticProduct?.title_Kr ?? "",
                                title_En = staticProduct?.title_En ?? "",
                                desc_Kr = staticProduct?.desc_Kr ?? "",
                                desc_En = staticProduct?.desc_En ?? "",
                                discount = staticProduct?.discount ?? 0
                            };
                        })
                        .OrderByDescending(p => ConfigDataManager.GetInAppItemByProductId(p.productId)?.Price ?? 0)
                        .ToList();

                    if (selectedinfos.Count == 0)
                        return;

                    PopulateGoldItems(selectedinfos);
                    break;

                case ShopMainTapType.CLASS:
                    int myUserIdx = CPPlayer.UserInfo.userDatabase?.User?.UserIdx ?? 0;
                    selectedinfos = ConfigDataManager.inAppItems
                        .Where(c =>
                        {
                            var s = IAPManager.StripProductPrefix(c.ProductId);
                            if (!s.StartsWith("class_", StringComparison.OrdinalIgnoreCase))
                                return false;
                            if (s.Contains("subscribe") && myUserIdx > 0)
                                return c.ProductId.EndsWith($"_{myUserIdx}");
                            return true;
                        })
                        .Select(c => NormalizeProductId(c.ProductId, allProductIds))
                        .Distinct()
                        .Select(pid =>
                        {
                            var staticProduct = StaticData.Wrapper.iAPProducts?.FirstOrDefault(p => p.productId == IAPManager.GetStaticMatchKey(pid));
                            return new IAPProduct
                            {
                                tapType = staticProduct?.tapType ?? ShopMainTapType.CLASS,
                                subTapType = staticProduct?.subTapType ?? InferSubTapType(pid),
                                productId = pid,
                                title_Kr = staticProduct?.title_Kr ?? "",
                                title_En = staticProduct?.title_En ?? "",
                                desc_Kr = staticProduct?.desc_Kr ?? "",
                                desc_En = staticProduct?.desc_En ?? "",
                                discount = staticProduct?.discount ?? 0
                            };
                        })
                        .Where(o =>
                        {
                            var configItem = ConfigDataManager.GetInAppItemByProductId(o.productId);
                            if (configItem == null)
                                return false;

                            if (itemType == ShopSubTapType.CLASS_SUB)
                                return configItem.InAppItemId.Contains("SUBSCRIBE");
                            else if (itemType == ShopSubTapType.CLASS)
                                return configItem.InAppItemId.Contains("_30");
                            return true;
                        })
                        .OrderByDescending(o => ConfigDataManager.GetInAppItemByProductId(o.productId)?.Price ?? 0)
                        .ToList();
                    if (selectedinfos.Count == 0)
                    {
                        LogError();
                        return;
                    }
                    PopulateClassItems(selectedinfos);
                    break;

                case ShopMainTapType.ITEM:
                    bool canShowBooster = !IsWeekPassedSinceSignup() && !HasBoosterInInventory();
                    bool canShowLuckyPocket = CPPlayer.Inventory.myPoints != null
                        && CPPlayer.Inventory.myPoints.LuckyBox >= 100000
                        && CPPlayer.Inventory.myPoints.WeeklyLuckyboxCnt < 3;

                    selectedinfos = ConfigDataManager.inAppItems
                        .Where(c => string.Equals(c.InAppItemType, "ITEM", StringComparison.OrdinalIgnoreCase))
                        .Select(c => NormalizeProductId(c.ProductId, allProductIds))
                        .Distinct()
                        .Select(pid =>
                        {
                            var staticProduct = StaticData.Wrapper.iAPProducts?.FirstOrDefault(p => p.productId == IAPManager.GetStaticMatchKey(pid));
                            return new IAPProduct
                            {
                                tapType = staticProduct?.tapType ?? ShopMainTapType.ITEM,
                                subTapType = staticProduct?.subTapType ?? InferSubTapType(pid),
                                productId = pid,
                                title_Kr = staticProduct?.title_Kr ?? "",
                                title_En = staticProduct?.title_En ?? "",
                                desc_Kr = staticProduct?.desc_Kr ?? "",
                                desc_En = staticProduct?.desc_En ?? "",
                                discount = staticProduct?.discount ?? 0
                            };
                        })
                        .Where(o =>
                        {
                            if (o.subTapType == ShopSubTapType.BOOSTER && !canShowBooster)
                                return false;
                            if (o.subTapType == ShopSubTapType.LUCKY_POCKET && !canShowLuckyPocket)
                                return false;

                            if (itemType == ShopSubTapType.ALL)
                                return true;

                            if (itemType == ShopSubTapType.NICK_CHANGE)
                            {
                                var configItem = ConfigDataManager.GetInAppItemByProductId(o.productId);
                                return configItem != null && configItem.InAppItemId.Contains("NICKNAME");
                            }

                            return o.subTapType == itemType;
                        })
                        .OrderBy(o => GetItemTabLayoutOrder(o.productId))
                        .ThenBy(o => o.productId, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (selectedinfos.Count == 0)
                    {
                        LogError();
                        return;
                    }
                    PopulateNormalItems(selectedinfos);
                    break;
            }

            void LogError() => Debug.LogError("잘못된 요청입니다! 테이블 데이터 또는 코드를 다시 확인!");
        }

        private void PopulateGoldItems(List<IAPProduct> items)
        {
            _currentGoldItems = items;
            _viewShop.goldScrollView.SetItemCount(items.Count);
        }

        private void OnGoldCellUpdate(GameObject cell, int index)
        {
            if (index < 0 || index >= _currentGoldItems.Count) return;

            var slot = cell.GetComponent<ShopItemSlot>();
            slot.SetSlotInfo(index, _currentGoldItems[index], _currentGoldItems.Count);
        }

        private void PopulateClassItems(List<IAPProduct> items)
        {
            _currentClassItems = items;
            _viewShop.classScrollView.SetItemCount(items.Count);
        }

        private void OnClassCellUpdate(GameObject cell, int index)
        {
            if (index < 0 || index >= _currentClassItems.Count) return;

            var slot = cell.GetComponent<ShopClassItemSlot>();
            slot.SetSlotInfo(_currentClassItems[index]);
        }

        private void PopulateNormalItems(List<IAPProduct> items)
        {
            _currentNormalItems = items;
            _viewShop.normalScrollView.SetItemCount(items.Count);
        }

        private void OnNormalCellUpdate(GameObject cell, int index)
        {
            if (index < 0 || index >= _currentNormalItems.Count) return;

            var slot = cell.GetComponent<ShopNormalItemSlot>();
            slot.SetSlotInfo(_currentNormalItems[index]);
        }

        private void OnClickMainTab(int index)
        {
            if (_suppressTabCallback)
                return;

            var newMainTab = (index >= 0 && index < mainTabs.Length) ? mainTabs[index] : ShopMainTapType.AVATAR;

            if (newMainTab == currentSelectedTap)
            {
                AnimateScrollToStart();
                return;
            }

            SaveCurrentScrollPosition();
            currentSelectedTap = newMainTab;

            if (lastSelectedSubTab.TryGetValue(currentSelectedTap, out var savedSubTab))
                currentSubSelectedTap = savedSubTab;
            else
                currentSubSelectedTap = GetDefaultItemType(currentSelectedTap);

            _suppressTabCallback = true;
            var subTab = GetCurrentSubCategoryTab();
            int subTabIndex = GetSubTabIndex(currentSubSelectedTap);
            subTab.SetActiveToggle(subTabIndex);
            _suppressTabCallback = false;

            RestoreScrollPositionForSubTab(currentSubSelectedTap);
            SetItemsInShopList(currentSelectedTap, currentSubSelectedTap);
        }

        private void OnClickSubTab(int index)
        {
            if (_suppressTabCallback)
                return;

            var newSubTap = GetCurrentSubTabItemType();

            if (newSubTap == currentSubSelectedTap)
            {
                AnimateScrollToStart();
                return;
            }

            SaveCurrentScrollPosition();
            currentSubSelectedTap = newSubTap;

            lastSelectedSubTab[currentSelectedTap] = currentSubSelectedTap;

            RestoreScrollPositionForSubTab(currentSubSelectedTap);
            SetItemsInShopList(currentSelectedTap, currentSubSelectedTap);
        }

        private ShopSubTapType GetDefaultItemType(ShopMainTapType tapType) => tapType switch
        {
            ShopMainTapType.AVATAR => ShopSubTapType.AVATAR_NORMAL,
            ShopMainTapType.CLASS => ShopSubTapType.CLASS_SUB,
            ShopMainTapType.ITEM => ShopSubTapType.ALL,
            _ => ShopSubTapType.AVATAR_NORMAL
        };

        private int GetSubTabIndex(ShopSubTapType itemType)
        {
            var array = GetCurrentSubTabArray();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == itemType)
                    return i;
            }
            return 0;
        }

        private ShopSubTapType[] GetCurrentSubTabArray() => currentSelectedTap switch
        {
            ShopMainTapType.AVATAR => goldSubTabs,
            ShopMainTapType.CLASS => classSubTabs,
            ShopMainTapType.ITEM => itemSubTabs,
            _ => goldSubTabs
        };

        private UISegmentedControlGroup GetCurrentSubCategoryTab() => currentSelectedTap switch
        {
            ShopMainTapType.AVATAR => _viewShop.goldCategoryTab,
            ShopMainTapType.CLASS => _viewShop.classCategoryTab,
            ShopMainTapType.ITEM => _viewShop.itemCategoryTab,
            _ => _viewShop.goldCategoryTab
        };

        private RecycleScrollView GetCurrentScrollView() => currentSelectedTap switch
        {
            ShopMainTapType.AVATAR => _viewShop.goldScrollView,
            ShopMainTapType.CLASS => _viewShop.classScrollView,
            ShopMainTapType.ITEM => _viewShop.normalScrollView,
            _ => _viewShop.goldScrollView
        };

        private void AnimateScrollToStart()
        {
            scrollTween?.Kill();

            var scrollView = GetCurrentScrollView();
            var rect = scrollView.GetComponent<ScrollRect>();
            rect.StopMovement();

            var content = rect.content;
            if (content == null || content.anchoredPosition.sqrMagnitude < 1f)
                return;

            scrollTween = DOTween.To(() => content.anchoredPosition, pos => content.anchoredPosition = pos, Vector2.zero, 0.3f).SetEase(Ease.OutQuad);
        }

        private void StopScrollAnimation()
        {
            scrollTween?.Kill();
            scrollTween = null;
        }

        private void SaveCurrentScrollPosition()
        {
            var scrollView = GetCurrentScrollView();
            var content = scrollView.GetComponent<ScrollRect>().content;
            if (content != null)
                savedSubTabScrollPositions[currentSubSelectedTap] = content.anchoredPosition;
        }

        private void RestoreScrollPositionForSubTab(ShopSubTapType subTab)
        {
            var scrollView = GetCurrentScrollView();
            if (savedSubTabScrollPositions.TryGetValue(subTab, out var savedPos))
                scrollView.SaveScrollPosition(savedPos);
        }

        private ShopSubTapType GetCurrentSubTabItemType()
        {
            var subTab = GetCurrentSubCategoryTab();
            var array = GetCurrentSubTabArray();
            int index = subTab.CurrentIndex;
            return (index >= 0 && index < array.Length) ? array[index] : array[0];
        }

        private static string NormalizeProductId(string productId, HashSet<string> allProductIds)
        {
            int xIdx = productId.LastIndexOf("_X", StringComparison.OrdinalIgnoreCase);
            if (xIdx > 0 && int.TryParse(productId.Substring(xIdx + 2), out _))
            {
                string baseId = productId.Substring(0, xIdx);
                if (allProductIds.Contains(baseId))
                    return baseId;
            }
            return productId;
        }

        private static int GetItemTabLayoutOrder(string productId)
        {
            var key = IAPManager.GetStaticMatchKey(productId ?? "");
            const string messegePrefix = "messege_";
            if (key.StartsWith(messegePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var qtyStr = key.Length > messegePrefix.Length ? key.Substring(messegePrefix.Length) : "";
                if (int.TryParse(qtyStr, out var qty))
                {
                    if (qty == 20)
                        return 0;
                    if (qty == 50)
                        return 1;
                    if (qty == 100)
                        return 2;
                    return 3 + qty;
                }
                return 10_000;
            }
            if (string.Equals(key, "nickname_change", StringComparison.OrdinalIgnoreCase))
                return 100;
            if (string.Equals(key, "lucky_pocket", StringComparison.OrdinalIgnoreCase))
                return 101;
            if (string.Equals(key, "booster", StringComparison.OrdinalIgnoreCase))
                return 102;
            return 20_000;
        }

        private static ShopSubTapType InferSubTapType(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return ShopSubTapType.NONE;

            var s = IAPManager.StripProductPrefix(productId);

            // AVATAR
            if (s.StartsWith("gold_n_", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.AVATAR_NORMAL;
            if (s.StartsWith("gold_x_", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.AVATAR_EVENT;
            if (s.StartsWith("gold_b_", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.AVATAR_CLASS_B;
            if (s.StartsWith("gold_a_", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.AVATAR_CLASS_A;
            if (s.StartsWith("gold_s_", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.AVATAR_CLASS_S;

            // CLASS
            if (s.IndexOf("subscribe", StringComparison.OrdinalIgnoreCase) >= 0)
                return ShopSubTapType.CLASS_SUB;
            if (s.StartsWith("class_", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.CLASS;

            // ITEM
            if (s.StartsWith("messege_", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.MSG;
            if (s.StartsWith("nickname", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.NICK_CHANGE;
            if (s.StartsWith("booster", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.BOOSTER;
            if (s.StartsWith("lucky", StringComparison.OrdinalIgnoreCase))
                return ShopSubTapType.LUCKY_POCKET;

            return ShopSubTapType.NONE;
        }

        private void OpenSubscribePopup(IAPType iaptype, bool isSubscribe, System.Action callback)
        {
            PopupManager.Instance.Open<PopupClassPurchase>(popup =>
            {
                popup.SetData(iaptype, isSubscribe, () => { callback?.Invoke(); });
            });
        }

        private void OnClickClassInfo()
        {
            PopupManager.Instance.Open<PopupClassComparison>();
        }
    }
}