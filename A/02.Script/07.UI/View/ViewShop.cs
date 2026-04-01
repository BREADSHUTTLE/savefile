using System;
using CAPYBARA.Bundles;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CAPYBARA.Core;

namespace CAPYBARA
{
    public class ViewShop : BackButtonView
    {
        public CPButton closeBtn;
        public CPButton classInfoBtn;
        public UISegmentedControlGroup mainTabGroup;

        [Space(10)]
        public UISegmentedControlGroup goldCategoryTab;
        public UISegmentedControlGroup classCategoryTab;
        public UISegmentedControlGroup itemCategoryTab;

        [Space(10)]
        public RecycleScrollView goldScrollView;
        public RecycleScrollView classScrollView;
        public RecycleScrollView normalScrollView;
        
        public Action onEnabled;
        public Action onScrollDragBegin;

        private void Awake()
        {
            // 각 ScrollRect에 드래그 시작 이벤트 연결
            AddDragTrigger(goldScrollView.gameObject);
            AddDragTrigger(classScrollView.gameObject);
            AddDragTrigger(normalScrollView.gameObject);
        }

        private void AddDragTrigger(GameObject target)
        {
            var trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
            
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            entry.callback.AddListener(_ => onScrollDragBegin?.Invoke());
            trigger.triggers.Add(entry);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            onEnabled?.Invoke();
        }
    }
}
