using System;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CAPYBARA.lobby;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class ControllerInventory
    {
        private ViewInventory view;
        private CancellationTokenSource cts;
        private List<lobby.Posts> postDataList = new List<lobby.Posts>();
        private List<(ItemID itemId, lobby.Inventory item)> itemDataList = new List<(ItemID, lobby.Inventory)>();
        private List<lobby.Inventory> emoticonDataList = new List<lobby.Inventory>();
        
        private int currentSelectedEmoticonIndex = -1;

        private Tweener scrollTween;
        private int currentTabIndex = 0;

        public ControllerInventory(ViewInventory _view, CancellationTokenSource _cts)
        {
            view = _view;
            cts = _cts;

            view.onScrollDragBegin += StopScrollAnimation;

            Init();

            SetEmoticonAsync().Forget();
        }

        private void Init()
        {
            CPPlayer.OutGame.openInventory += () => OpenInventory().Forget();
            CPPlayer.OutGame.nickNameChangedCallback += () => SetItemList().Forget();

            view.toggleGroup.onIndexChanged += OnClickTap;
            view.btnClose.onClick.RemoveAllListeners();
            view.btnClose.onClick.AddListener(OnClickClose);

            view.recieveCheckedPost.onClick.AddListener(() => ReceiveAllPosts().Forget());
            view.goToShopBtn.onClick.AddListener(OnClickGoToShop);

            UpdateReceiveAllButtonState();
        }

        private async UniTask OpenInventory()
        {
            view.gameObject.SetActive(false);
            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));

            if (view.goldLimitToast != null)
                view.goldLimitToast.SetActive(false);

            // 클래스 정보 갱신 (만료 체크 포함)
            int prevClassNumber = CPPlayer.Inventory.classNumber;
            string prevClassName = CPPlayer.Inventory.GetClassDisplayName(CPPlayer.Inventory.classInfo);
            await RefreshClassInfo();

            if (CPPlayer.Inventory.CheckClassExpiredFromServer(prevClassNumber))
            {
                CPPlayer.Inventory.lastExpiredClassName = prevClassName;
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));
                await UniTask.Yield();

                bool popupClosed = false;
                PopupManager.Instance.Open<PopupExpirationClass>(popup =>
                {
                    popup.SetDataConfirmOnly(
                        StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpired].StringToLocal,
                        StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpiredMoveToVault].StringToLocal,
                        prevClassName
                    );
                    popup.OnPopupClosed = () => popupClosed = true;
                });
                await UniTask.WaitUntil(() => popupClosed);
                
                PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(true));
                await RefreshClassInfo();
            }

            // 이벤트 정보 갱신
            await RefreshEventInfo();

            await SetGoldPostList();
            await SetItemList();
            SetEmoticonList();
            SetClass();

            currentTabIndex = 0;
            view.toggleGroup.SetActiveToggle(0);
            ResetAllScrollPositions();

            PopupManager.Instance.Open<PopupToast>(popup => popup.LoadingOutGamePopupActive(false));

            view.gameObject.SetActive(true);

            view.postRecycleScrollView?.RefreshAllCells();
        }

        private void OnClickTap(int index)
        {
            if (currentTabIndex == index)
            {
                AnimateScrollToStart();
                return;
            }

            currentTabIndex = index;
        }

        private void AnimateScrollToStart()
        {
            scrollTween?.Kill();

            switch (currentTabIndex)
            {
                case 0:
                    AnimateScrollToStart(view.postRecycleScrollView, view.postScrollrect, true);
                    break;
                case 1:
                    AnimateScrollToStart(view.itemRecycleScrollView, view.itemScrollrect, false);
                    break;
                case 2:
                    AnimateScrollToStart(view.emoticonRecycleScrollView, view.emoticonScrollrect, false);
                    break;
            }
        }

        private void AnimateScrollToStart(RecycleScrollView recycleScrollView, ScrollRect scrollRect, bool isVertical)
        {
            if (recycleScrollView != null)
            {
                var content = scrollRect.content;
                float value = isVertical ? content.anchoredPosition.y : content.anchoredPosition.x;
                if (isVertical ? value <= 1f : value >= -1f)
                    return;

                scrollTween = DOTween.To(
                    () => content.anchoredPosition,
                    pos => content.anchoredPosition = pos,
                    Vector2.zero,
                    0.3f
                ).SetEase(Ease.OutQuad);
            }
            else
            {
                float value = isVertical ? scrollRect.normalizedPosition.y : scrollRect.normalizedPosition.x;
                if (isVertical ? value >= 0.99f : value <= 0.01f)
                    return;

                Vector2 target = isVertical
                    ? new Vector2(scrollRect.normalizedPosition.x, 1)
                    : new Vector2(0, scrollRect.normalizedPosition.y);

                scrollTween = DOTween.To(
                    () => scrollRect.normalizedPosition,
                    pos => scrollRect.normalizedPosition = pos,
                    target,
                    0.3f
                ).SetEase(Ease.OutQuad);
            }
        }

        private void ResetAllScrollPositions()
        {
            if (view.postRecycleScrollView != null)
                view.postRecycleScrollView.ScrollToTop();
            else
                view.postScrollrect.normalizedPosition = new Vector2(0, 1);
            
            if (view.itemRecycleScrollView != null)
                view.itemRecycleScrollView.ScrollToTop();
            else
                view.itemScrollrect.normalizedPosition = new Vector2(0, 1);
            
            if (view.emoticonRecycleScrollView != null)
                view.emoticonRecycleScrollView.ScrollToTop();
            else
                view.emoticonScrollrect.normalizedPosition = new Vector2(0, 1);
        }

        private void StopScrollAnimation()
        {
            scrollTween?.Kill();
            scrollTween = null;
        }

        private ScrollRect GetCurrentScrollRect() => currentTabIndex switch
        {
            0 => view.postScrollrect,
            1 => view.itemScrollrect,
            2 => view.emoticonScrollrect,
            _ => view.postScrollrect
        };

        private async UniTask SetGoldPostList()
        {
            var postListPacket = await Services.Lobby.PostsReqAsync(lobby.PostsType.PostsNoneType, lobby.PostsState.PostsNoneState);
            if (!postListPacket.IsSuccess)
            {
                Debug.Log("인벤토리 골드 탭 에러 (에러코드 확인 필요)");
                return;
            }

            var postInfores = postListPacket.Data;
            int currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // 기간 만료된 항목 제외 (LimitedAt이 0이면 무제한, 0보다 크고 현재시간보다 작으면 만료)
            postDataList = postInfores.Posts.Where(p => p.State == 1 && (p.LimitedAt == 0 || p.LimitedAt > currentTimestamp)).
                                            OrderBy(p => p.LimitedAt == 0 ? 1 : 0).ThenBy(p => p.LimitedAt == 0 ? 0 : p.LimitedAt).ThenBy(p => p.Id).ToList();
            
            if (view.postRecycleScrollView != null)
            {
                view.postRecycleScrollView.OnCellUpdate = OnPostCellUpdate;
                view.postRecycleScrollView.SetItemCount(postDataList.Count);
                view.postRecycleScrollView.RefreshAllCells();
            }

            view.emptyPost.SetActive(postDataList.Count == 0);
            view.postScrollrect.movementType = postDataList.Count >= 6 ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
            UpdateReceiveAllButtonState();
        }

        private void OnPostCellUpdate(GameObject cellObject, int index)
        {
            var slot = cellObject.GetComponent<PostSlot>();
            if (slot == null || index < 0 || index >= postDataList.Count) 
                return;

            var post = postDataList[index];
            
            slot.id = post.Id;
            slot.uid = post.Uid;
            slot.amount = post.Amount;
            slot.message = post.Message;
            slot.itemID = Extension.StringToEnum<ItemID>(post.ItemId);
            slot.type = post.Type;
            slot.state = post.State;
            slot.postsInfo = post;
            slot.dataIndex = index;

            slot.onReceive = ReceiveSinglePost;
            slot.Init();
        }

        private void UpdateReceiveAllButtonState()
        {
            if (view.checkRecieveText != null)
                view.checkRecieveText.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ReceiveAll].StringToLocal;

            bool hasItems = postDataList.Count > 0;

            if (view.recieveCheckedPost != null)
                view.recieveCheckedPost.interactable = hasItems;

            if (view.recieveCheckedPostDim != null)
                view.recieveCheckedPostDim.SetActive(!hasItems);
        }

        private async UniTask ReceiveAllPosts()
        {
            if (postDataList.Count == 0)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.NoGoldToReceive].StringToLocal));
                return;
            }

            var recvIdList = postDataList.OrderBy(p => p.LimitedAt == 0 ? long.MaxValue : p.LimitedAt).Select(p => p.Id).ToList();
            var resPacket = await Services.Lobby.PostsRecvAsync(recvIdList);

            if (!resPacket.IsSuccess && resPacket.Error.Code != ErrorCode.EMaxValue)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ReceiveFailed].StringToLocal));
                return;
            }

            var userinfo = await Services.Lobby.GetUserInfoAsync();
            if (userinfo.IsSuccess)
                CPPlayer.UserInfo.userDatabase = userinfo.Data;

            CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();

            await SetGoldPostList();

            if (resPacket.Error.Code == ErrorCode.EMaxValue)
                ShowGoldLimitToast();
        }

        private async void ReceiveSinglePost(PostSlot slot)
        {
            if (slot == null || slot.postsInfo == null)
                return;

            var resPacket = await Services.Lobby.PostsRecvAsync(new List<int> { slot.postsInfo.Id });

            if (resPacket.Error.Code == ErrorCode.EMaxValue)
            {
                ShowGoldLimitToast();
                return;
            }

            if (!resPacket.IsSuccess)
            {
                PopupManager.Instance.Open<PopupToast>(popup => popup.ShowPopupOneButton(StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ReceiveFailed].StringToLocal));
                return;
            }

            var userinfo = await Services.Lobby.GetUserInfoAsync();
            if (userinfo.IsSuccess)
                CPPlayer.UserInfo.userDatabase = userinfo.Data;

            CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();

            int postId = slot.postsInfo.Id;
            postDataList.RemoveAll(p => p.Id == postId);
            
            if (view.postRecycleScrollView != null)
                view.postRecycleScrollView.SetItemCount(postDataList.Count);

            view.emptyPost.SetActive(postDataList.Count == 0);
            view.postScrollrect.movementType = postDataList.Count >= 6 ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
            UpdateReceiveAllButtonState();
        }

        private void ShowGoldLimitToast()
        {
            if (view.goldLimitToast != null)
                view.goldLimitToast.SetActive(true);
            if (view.goldLimitToastDismiss != null)
                view.goldLimitToastDismiss.gameObject.SetActive(true);
        }

        private async UniTask SetItemList()
        {
            var itemListPacket = await Services.Lobby.GetInventoryAsync(true);
            if (!itemListPacket.IsSuccess)
            {
                Debug.Log("인벤토리 아이템 탭 에러 (에러코드 확인 필요)");
                return;
            }

            CPPlayer.Inventory.inventoryInfo = itemListPacket.Data;
            CPPlayer.Inventory.inventoryUpdateCallback?.Invoke();
            var itemInfoRes = itemListPacket.Data;

            // 아이템 데이터 필터링
            itemDataList.Clear();
            for (int i = 0; i < itemInfoRes.Inventory.Count; i++)
            {
                var itemIdStr = itemInfoRes.Inventory[i].ItemId.ToUpper();
                var itemId = Extension.StringToEnum<ItemID>(itemIdStr);

                if (IsEmoticonItem(itemId))
                    continue;

                if (IsAvatarItem(itemId))
                    continue;

                if (IsClassItem(itemId))
                    continue;

                // DEFAULT_CURRENCY 필터링 (Enum 비교 + 문자열 비교)
                if (itemId == ItemID.DEFAULT_CURRENCY || itemIdStr.Contains("DEFAULT_CURRENCY"))
                    continue;

                // 복주머니 필터링
                if (itemId == ItemID.LUCKY_POCKET)
                    continue;

                // 서버에서 의도사항인지 확인중 = 닉네임 변경권 amount가 0인데 현재 데이터가 내려오는 상황이라 예외처리 해둠
                if ((itemId != ItemID.NICKNAME_CHANGE && itemId != ItemID.NICKNAME_CHANGE_FIRST) || itemInfoRes.Inventory[i].Amount > 0)
                {
                    if (itemInfoRes.Inventory[i].Item.IsActive)
                        itemDataList.Add((itemId, itemInfoRes.Inventory[i]));
                }
            }

            if (view.itemRecycleScrollView != null)
            {
                view.itemRecycleScrollView.OnCellUpdate = OnItemCellUpdate;
                view.itemRecycleScrollView.SetItemCount(itemDataList.Count);
            }

            view.emptyItem.SetActive(itemDataList.Count <= 0);
            view.itemScrollrect.movementType = itemDataList.Count >= 5 ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
        }

        private void OnItemCellUpdate(GameObject cellObject, int index)
        {
            var slot = cellObject.GetComponent<ItemSlot>();
            if (slot == null || index < 0 || index >= itemDataList.Count)
                return;

            var (itemId, item) = itemDataList[index];
            slot.Init(itemId, item);
        }

        private async UniTask SetEmoticonAsync()
        {
            await UniTask.WaitUntil(()=>CPPlayer.Inventory.inventoryInfo != null);
            SetEmoticonList();
        }
        private void SetEmoticonList()
        {
            if (CPPlayer.Inventory.inventoryInfo == null)
                return;

            var itemInfoRes = CPPlayer.Inventory.inventoryInfo;

            // 이모티콘 데이터 필터링 (서버 ItemTypeId 기준)
            emoticonDataList.Clear();
            currentSelectedEmoticonIndex = -1;
            
            for (int i = 0; i < itemInfoRes.Inventory.Count; i++)
            {
                var inv = itemInfoRes.Inventory[i];
                bool isEmoticon = inv.ItemTypeId.Equals("EMOTICON", StringComparison.OrdinalIgnoreCase)
                    || inv.ItemId.StartsWith("EMOTICON_", StringComparison.OrdinalIgnoreCase);

                if (!isEmoticon || inv.Amount <= 0)
                    continue;

                emoticonDataList.Add(inv);
            }

            emoticonDataList = emoticonDataList.OrderByDescending(x => x.IsEffective).ToList();
            currentSelectedEmoticonIndex = emoticonDataList.Count > 0 && emoticonDataList[0].IsEffective ? 0 : -1;
            
            if (currentSelectedEmoticonIndex >= 0 && currentSelectedEmoticonIndex < emoticonDataList.Count)
            {
                if (Enum.TryParse<ItemID>(emoticonDataList[currentSelectedEmoticonIndex].ItemId, true, out var parsedId))
                    CPPlayer.InGame.currentEquippedEmoticon = parsedId;
                else
                    CPPlayer.InGame.currentEquippedEmoticon = ItemID.EMOTICON_1;
            }
            else
                CPPlayer.InGame.currentEquippedEmoticon = ItemID.EMOTICON_1;

            if (view.emoticonRecycleScrollView != null)
            {
                view.emoticonRecycleScrollView.OnCellUpdate = OnEmoticonCellUpdate;
                view.emoticonRecycleScrollView.SetItemCount(emoticonDataList.Count);
            }

            if (view.emptyEmoticon != null)
                view.emptyEmoticon.SetActive(emoticonDataList.Count <= 0);
            view.emoticonScrollrect.movementType = emoticonDataList.Count >= 5 ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
        }

        private void OnEmoticonCellUpdate(GameObject cellObject, int index)
        {
            var slot = cellObject.GetComponent<InventoryEmoticonSlot>();
            if (slot == null || index < 0 || index >= emoticonDataList.Count)
                return;

            var item = emoticonDataList[index];
            slot.Init(item);
            slot.onClickSelect = OnClickEmoticonSlot;

            slot.SetEquipped(index == currentSelectedEmoticonIndex);
        }
        
        private void OnClickEmoticonSlot(InventoryEmoticonSlot clickedSlot)
        {
            if (view.emoticonRecycleScrollView == null)
                return;

            int clickedIndex = view.emoticonRecycleScrollView.GetCellIndex(clickedSlot.gameObject);
            if (clickedIndex < 0 || clickedIndex == currentSelectedEmoticonIndex)
                return;

            OnClickEmoticonSlotAsync(clickedSlot, clickedIndex).Forget();
        }

        private async UniTaskVoid OnClickEmoticonSlotAsync(InventoryEmoticonSlot clickedSlot, int clickedIndex)
        {
            var item = emoticonDataList[clickedIndex];
            
            var result = await Services.Lobby.InventoryChangeAsync(item.ItemId, cts.Token);
            if (!result.IsSuccess)
                return;

            if (currentSelectedEmoticonIndex >= 0)
            {
                var prevSlot = view.emoticonRecycleScrollView.GetCellByIndex(currentSelectedEmoticonIndex);
                if (prevSlot != null)
                    prevSlot.GetComponent<InventoryEmoticonSlot>()?.SetEquipped(false);
            }

            currentSelectedEmoticonIndex = clickedIndex;
            clickedSlot.SetEquipped(true);
        }

        private bool IsEmoticonItem(ItemID itemId)
        {
            return itemId.ToString().Contains("EMOTICON_");
        }

        private bool IsAvatarItem(ItemID itemId)
        {
            return itemId.ToString().StartsWith("AVATAR");
        }

        private bool IsClassItem(ItemID itemId)
        {
            return itemId == ItemID.CLASS_A || itemId == ItemID.CLASS_B || itemId == ItemID.CLASS_S;
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
                // 클래스 없음
                CPPlayer.Inventory.classInfo = null;
                CPPlayer.Inventory.classNumber = 0;
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

        private void SetClass()
        {
            if (CPPlayer.Inventory.classNumber == 0)
            {
                view.emptyClass.SetActive(true);
                for (int i = 0; i < view.classWindows.Length; i++)
                    view.classWindows[i].gameObject.SetActive(false);
            }
            else
            {
                view.emptyClass.SetActive(false);
                for (int i = 0; i < view.classWindows.Length; i++)
                    view.classWindows[i].gameObject.SetActive(i + 1 == CPPlayer.Inventory.classNumber);
            }
        }

        private void OnClickGoToShop()
        {
            CPPlayer.OutGame.openShopUIWithTab?.Invoke(ShopMainTapType.CLASS, () => view.gameObject.SetActive(false));
        }

        private void OnClickClose()
        {
            view.gameObject.SetActive(false);
        }
    }
}
