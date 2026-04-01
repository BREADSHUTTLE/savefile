using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace CAPYBARA.Bundles
{
    public class ViewCanvasLobby : ViewCanvas
    {
        public CPButton option;
        public TMP_Text userNickName;

        [Header("Game Tabs")]
        public UISegmentedControlGroup gameTabGroup;

        [Header("avatar")]
        public UnityEngine.UI.Image avatarImage;
        public CPButton btnAvaterProfile;

        [Header("")]
        public CPButton shopOpenBtn;
        public CPButton InvenOpenBtn;
        public GameObject PostNoti;
        public CPButton MessageOpenBtn;
        public GameObject MessageNoti;
        public CPButton friendsOpenBtn;
        public GameObject friendsNoti;
        public CPButton achievementOpenBtn;
        public GameObject achievementNoti;
        public CPButton customerServiceOpenBtn;
        public CPButton announceOpenBtn;
        public CPButton advertiseOpenBtn;
        public CPButton prfileOpenBtn;

        public ScrollRect badugiSlotParent;
        public ScrollRect holdemSlotParent;
        public ScrollRect SpokerSlotParent;

        
        public ViewRoomEnterSlot slotPrefab;
        
        [Header("viewers")]
        public ViewShop viewShop;
        public ViewProfileWindow profileWindow;
        public ViewOption viewOption;
        public ViewInventory viewInventory;
        public ViewChat viewChat;
        public ViewFriends viewFriends;
        public ViewMission viewMission;
        

        [Header("bottom icons")]
        public CPButton boosterIcon;
        public CPButton bokPocketIcon;

        public CPButton guideBookIcon;


        private void OnDestroy()
        {
            Debug.Log("ViewCanvasLobby Destroyed");
        }
    }

}

