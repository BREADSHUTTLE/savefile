using CAPYBARA.Bundles;
using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CAPYBARA.Core;

namespace CAPYBARA
{
    public class ViewProfileWindow : BackButtonView
    {
        public UISegmentedControlGroup tabToggleGroup;

        public CPButton closeBtn;

        [Space]
        [Header("Avatar")]
        public Image imgAvatar;
        public Image imgAvatarShadow;
        public GameObject MyAvatarWindow;
        public CPButton btnChangeAvatar;
        public CPButton btnCloseAvatar;
        public TMP_Text txtCloseAvatar;
        public RecycleScrollView avatarScrollView;

        [Space]
        [Header("UserInfoList")]
        public ScrollRect infoScrollRect;
        public AccountHistoryObj slotPrefab;
        public Transform slotParent;

        [Space]
        [Header("Player Information")]
        public TMP_Text usedRealMoney;
        public TMP_Text monthMaxRealMoney_0;

        [Space]
        [Header("UserAuthDay")]
        public TMP_Text initAuthDay;
        public CPButton accountManage;

        [Space]
        [Header("myUsedChips")]
        public TMP_Text limitChips0;
        public TMP_Text limitChipsM;
        public TMP_Text limitChipsP;
        public TMP_Text setlimitChipRule;
        public CPButton maxChipChange;



        [Space]
        [Space]
        [Header("UserHistory")]
        public UISegmentedControlGroup todayRecordGroup;
        public TMP_Text allHistory_day;
        public TMP_Text allHistory_day_win;
        public TMP_Text allHistory_day_lose;
        public TMP_Text allHistory_day_per;

        [Header("UserHistoryTotal")]
        public UISegmentedControlGroup totalRecordGroup;
        public TMP_Text allHistory_total;
        public TMP_Text allHistory_total_win;
        public TMP_Text allHistory_total_lose;
        public TMP_Text allHistory_total_per;


        public Action onScrollDragBegin;
        public Action onBackButtonInAvatarMode;

        public override void OnBackButtonPressed()
        {
            if (MyAvatarWindow != null && MyAvatarWindow.activeSelf)
            {
                onBackButtonInAvatarMode?.Invoke();
                return;
            }
            base.OnBackButtonPressed();
        }

        private void Awake()
        {
            AddDragTrigger(infoScrollRect.gameObject);
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
