using System;
using System.Collections.Generic;
using AdvancedInputFieldPlugin;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CAPYBARA
{
    public class ViewFriends : BackButtonView
    {
        public UISegmentedControlGroup uiTabGroup;
        public CPButton closeButton;

        //내친구들
        [Space(5)]
        [Header("MyFriends")]
        public TMP_Text friendCountText;
        public FriendSlot friendSlotPrefab;
        public RecycleScrollView friendScrollView;
        public GameObject emptyFriends;
        
        [Header("친구/차단 목록")]
        public CPButton listTypeDropdownButton;
        public TMP_Text listTypeText;
        public GameObject dropdownPanel;
        public CPButton friendListButton;
        public CPButton blockListButton;
        public GameObject friendListCheck;
        public GameObject blockListCheck;
        public RectTransform dropdownArrow;
        public GameObject emptyBlockedUsers;

        //요청 친구
        [Space(5)]
        [Header("RequestFriends")]
        public UISegmentedControlGroup uiTabGroupInRequest;
        public FriendRequestSlot friendReqSlotPrefab;
        public RecycleScrollView friendSentReqScrollView;
        public RecycleScrollView friendRecievedReqScrollView;
        public GameObject emptySentReqFriends;
        public GameObject emptyRecievedReqFriends;

        public GameObject newFriendRequestNotiObj_topTap;
        public GameObject newFriendRequestNotiObj_midTap;
        //친구 찾기
        [Space(5)]
        [Header("FindFriends")]
        public InputField inputField;
        public CPButton findUser;
        public RecycleScrollView friendFoundScrollView;
        public GameObject emptyFoundUsers;

        //친구 초대
        [Space(5)]
        [Header("InviteFriend")]
        public CPButton inviteBtn;
        public InviteGetSlot[] inviteGetSlots;

        public Action onScrollDragBegin;
        public Action onDropdownOutsideClick;

        private void Awake()
        {
            AddDragTrigger(friendScrollView.gameObject);
            AddDragTrigger(friendSentReqScrollView.gameObject);
            AddDragTrigger(friendRecievedReqScrollView.gameObject);
            AddDragTrigger(friendFoundScrollView.gameObject);
        }
        
        private void Update()
        {
            // 드롭다운이 열려있을 때 밖을 클릭하면 닫기
            if (dropdownPanel != null && dropdownPanel.activeSelf)
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    if (!IsPointerOverDropdown())
                        onDropdownOutsideClick?.Invoke();
                }
            }
        }
        
        private bool IsPointerOverDropdown()
        {
            if (EventSystem.current == null)
                return false;
            
            var pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = Input.mousePosition;
            
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);
            
            foreach (var result in raycastResults)
            {
                // 클릭한 UI가 드롭다운 버튼이나 패널 또는 자식인지 확인
                if (listTypeDropdownButton != null && 
                    (result.gameObject == listTypeDropdownButton.gameObject || 
                     result.gameObject.transform.IsChildOf(listTypeDropdownButton.transform)))
                    return true;
                    
                if (dropdownPanel != null && 
                    (result.gameObject == dropdownPanel || 
                     result.gameObject.transform.IsChildOf(dropdownPanel.transform)))
                    return true;
            }
            
            return false;
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
