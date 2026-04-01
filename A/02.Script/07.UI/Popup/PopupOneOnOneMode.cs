using System;
using System.Collections.Generic;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    [Serializable]
    public class RoomToggleData
    {
        public lobby.RoomInfo roomInfo;
        public Toggle roomToggle;
        public TMP_Text roomName;
    }

    public class PopupOneOnOneMode : BasePopup
    {
        [SerializeField] private RoomToggleData[] roomToggleDatas;
        [SerializeField] private CPButton cancelBtn;
        [SerializeField] private CPButton enterBtn;

        private lobby.RoomInfo selectedRoomInfo;

        protected override void OnInit()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (cancelBtn != null)
            {
                cancelBtn.onClick.RemoveAllListeners();
                cancelBtn.onClick.AddListener(Close);
            }

            if (enterBtn != null)
            {
                enterBtn.onClick.RemoveAllListeners();
                enterBtn.onClick.AddListener(() =>
                {
                    if (CPPlayer.Inventory.CheckClassExpiredLocally())
                    {
                        Close();
                        var roomInfo = selectedRoomInfo;
                        PopupManager.Instance.Open<PopupExpirationClass>(popup =>
                        {
                            popup.SetData(
                                StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpired].StringToLocal,
                                StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemExpiredEnterRoom].StringToLocal,
                                CPPlayer.Inventory.lastExpiredClassName,
                                () =>
                                {
                                    popup.Close();
                                    CPPlayer.InGame.EnterInGame?.Invoke(GameType.LOW_BADUGI, GameMode.TwoVS, roomInfo);
                                }
                            );
                            popup.OnPopupClosed = () => RefreshClassInfoAfterExpiry().Forget();
                        });
                    }
                    else
                    {
                        CPPlayer.InGame.EnterInGame?.Invoke(GameType.LOW_BADUGI, GameMode.TwoVS, selectedRoomInfo);
                        Close();
                    }
                });
            }
        }

        public void SetData(List<lobby.RoomInfo> roomList)
        {
            for (int i = 0; i < roomList.Count; i++)
            {
                int index = i;
                roomToggleDatas[index].roomInfo = roomList[index];
                roomToggleDatas[index].roomName.text = Extension.ToKoreanFormat(roomList[index].Ante);
                roomToggleDatas[index].roomToggle.onValueChanged.RemoveAllListeners();
                roomToggleDatas[index].roomToggle.onValueChanged.AddListener(isOn =>
                {
                    currentRoomIndex = index;
                    selectedRoomInfo = roomToggleDatas[currentRoomIndex].roomInfo;
                });
            }

            roomToggleDatas[0].roomToggle.isOn = true;
            //selectedRoomInfo=roomToggleDatas[0].roomInfo; // [ default]
        }
        
        int currentRoomIndex = 0;

        protected override void OnOpen()
        {
            base.OnOpen();
            selectedRoomInfo = roomToggleDatas[currentRoomIndex].roomInfo;
        }

        protected override void OnClose()
        {
            base.OnClose();
        }

        private async UniTaskVoid RefreshClassInfoAfterExpiry()
        {
            var result = await Services.Lobby.ClassInfoAsync();
            if (result.IsSuccess && result.Data != null)
            {
                CPPlayer.Inventory.classInfo = result.Data;
                CPPlayer.Inventory.classNumber = result.Data.ItemId switch
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
            }
            CPPlayer.Inventory.classUpdateCallback?.Invoke();
        }
    }
}
