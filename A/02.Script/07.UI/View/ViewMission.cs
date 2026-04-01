using System;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CAPYBARA
{
    public class ViewMission : BackButtonView
    {
        public UISegmentedControlGroup tabGroup;
        public CPButton closeBtn;

        [Space(5)]
        [Header("Tab Windows")]
        public GameObject dailyMissionWindow;
        public GameObject achieveWindow;

        [Space(5)]
        [Header("dailyMission")]
        public RecycleScrollView dailyScrollView;
        public DailyMissionSlot dailyslotPrefab;
        public CPButton gotoPurchaseClassBtn;

        [Space(5)]
        [Header("Achieve")]
        public ScrollRect achieveScrollRect;
        public AchieveItemSlot achieveItemSlotPrefab;
        public AchieveSlot achieveSlotPrefab;

        public Action onScrollDragBegin;

        private void Awake()
        {
            AddDragTrigger(dailyScrollView.gameObject);
            AddDragTrigger(achieveScrollRect.gameObject);
        }
        
        private void AddDragTrigger(GameObject target)
        {
            var trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            entry.callback.AddListener(_ => onScrollDragBegin?.Invoke());
            trigger.triggers.Add(entry);
        }
        
        public void SetActiveWindow(int index)
        {
            dailyMissionWindow.SetActive(index == 0);
            achieveWindow.SetActive(index == 1);
        }
    }
}
