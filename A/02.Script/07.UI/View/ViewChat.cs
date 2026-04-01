using System;
using AdvancedInputFieldPlugin;
using CAPYBARA.Bundles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class ViewChat : BackButtonView
    {
        public ScrollRect friendsScrollView;
        public ChatPlayerSlot friendSlotPrefab;

        public ScrollRect chatScrollview;
        public ChatCloudSlot chatCloudPrefab;
        public ChatCloudDateSlot chatDatePrefab;

        public TMP_Text chatRoomName;
        public Image chatRoomAvatar;
        public Toggle chatRoomPinToggle;

        [Header("SEND MSG")]
        public AdvancedInputField advancedInputField;
        public CPButton sendMsgBtn;
        

        public CPButton backToNormalChatBtn;
        public CPButton goToPortraitChatBtn;
        public GameObject chatBoxParent;
        public RectTransform chatBox;
        public GameObject chatWindowParent;
        //public GameObject PopupAfterDeleteChat;

        public CPButton closeBtn;

        public GameObject emptyWindow;
        public CPButton btnEmptySend;
        public Action onBackToNormalMode;
        public Action OnAfterClose;

        public override void CloseView()
        {
            base.CloseView();
            OnAfterClose?.Invoke();
        }

        public override void OnBackButtonPressed()
        {
            if (chatWindowParent != null && chatWindowParent.activeInHierarchy)
            {
                if (advancedInputField != null && advancedInputField.Selected)
                {
                    advancedInputField.ManualDeselect();
                    NativeKeyboardManager.HideKeyboard();
                }
                
                onBackToNormalMode?.Invoke();
            }
            else
            {
                base.OnBackButtonPressed();
            }
        }
    }
}
