using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CAPYBARA
{
    public class ViewInventory : BackButtonView
    {
        public CPButton btnClose;
        public UISegmentedControlGroup toggleGroup;
        public PostSlot postItemslotPrefab;
        public ItemSlot itemSlotPrefab;
        public InventoryEmoticonSlot emoticonSlotPrefab;

        public GameObject emptyPost;
        public GameObject emptyItem;
        public GameObject emptyEmoticon;
        public ScrollRect postScrollrect;
        public ScrollRect itemScrollrect;
        public ScrollRect emoticonScrollrect;
        
        [Header("RecycleScrollView (Optional)")]
        public RecycleScrollView postRecycleScrollView;
        public RecycleScrollView itemRecycleScrollView;
        public RecycleScrollView emoticonRecycleScrollView;

        public UserClassInfoWindow[] classWindows;
        public GameObject emptyClass;
        public CPButton goToShopBtn;

        public CPButton recieveCheckedPost;
        public TMP_Text checkRecieveText;
        public GameObject recieveCheckedPostDim;

        public GameObject goldLimitToast;

        public Action onScrollDragBegin;
        public CPButton goldLimitToastDismiss;

        private void Awake()
        {
            AddDragTrigger(postScrollrect.gameObject);
            AddDragTrigger(itemScrollrect.gameObject);
            AddDragTrigger(emoticonScrollrect.gameObject);

            if (goldLimitToastDismiss != null)
            {
                goldLimitToastDismiss.gameObject.SetActive(false);
                goldLimitToastDismiss.onClick.AddListener(() =>
                {
                    if (goldLimitToast != null) goldLimitToast.SetActive(false);
                    goldLimitToastDismiss.gameObject.SetActive(false);
                });
            }
        }

        private void AddDragTrigger(GameObject target)
        {
            var trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            entry.callback.AddListener(_ => onScrollDragBegin?.Invoke());
            trigger.triggers.Add(entry);
        }
    }
}
