using System;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class PopupCreateAvatar : BasePopup
    {
        [SerializeField] private TMP_Text txtBtn;
        [SerializeField] private AvatarEquipSlot[] avatarSlots;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private Transform frontLayer;
        [SerializeField] private CPButton btnConfirm;

        [SerializeField] private string[] defaultAvatarIds;

        public override bool CanCloseByBackButton => false;
        public override bool CanCloseByDimClick => false;
        
        private AvatarEquipSlot selectedSlot;
        private string selectedAvatarId;
        
        private Transform[] originalParents;
        private int[] originalSiblingIndices;
        
        private bool isProcessing;
        private bool isLayoutLocked = false;
        public Action<string> OnAvatarSelected;

        protected override void OnInit()
        {
            base.OnInit();

            if (btnConfirm != null)
            {
                btnConfirm.onClick.RemoveAllListeners();
                btnConfirm.onClick.AddListener(() => OnConfirmClicked().Forget());
            }

            if (closeButton != null)
                closeButton.gameObject.SetActive(false);

            if (txtBtn != null)
                txtBtn.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.SelectionComplete].StringToLocal;
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            isProcessing = false;
            selectedSlot = null;
            selectedAvatarId = null;
            isLayoutLocked = false;

            SetupAvatarSlots();
        }

        private void SetupAvatarSlots()
        {
            if (avatarSlots == null || avatarSlots.Length == 0)
                return;

            var itemBundle = ItemBundle.Loaded;
            if (itemBundle == null)
                return;

            originalParents = new Transform[avatarSlots.Length];
            originalSiblingIndices = new int[avatarSlots.Length];

            for (int i = 0; i < avatarSlots.Length; i++)
            {
                var slot = avatarSlots[i];
                if (slot == null) continue;

                slot.skipSiblingReorder = true;

                var layoutElement = slot.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    slot.gameObject.AddComponent<LayoutElement>();

                slot.transform.SetSiblingIndex(i);
                originalParents[i] = slot.transform.parent;
                originalSiblingIndices[i] = i;

                string avatarId = (defaultAvatarIds != null && i < defaultAvatarIds.Length) ? defaultAvatarIds[i] : null;

                if (!string.IsNullOrEmpty(avatarId))
                {
                    var avatarData = itemBundle.GetAvatarById(avatarId);
                    if (avatarData != null)
                    {
                        string avatarName = GetAvatarNameFromStaticData(avatarId);
                        
                        slot.SetAvatar(avatarData.AvatarSprite, avatarData.AvatarId, 0, avatarName, avatarData.Offset);
                        slot.hideNameOnDeselect = true;
                        slot.onClickEquip = OnAvatarSlotClicked;
                        slot.SetEquip(false);
                    }
                }
            }

            var container = slotContainer != null ? slotContainer : avatarSlots[0].transform.parent;
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);

            int randomIndex = UnityEngine.Random.Range(0, avatarSlots.Length);
            if (avatarSlots[randomIndex] != null)
                SelectAvatar(avatarSlots[randomIndex]);
        }

        private string GetAvatarNameFromStaticData(string avatarId)
        {
            return Core.StaticData.GetItemName(avatarId);
        }

        private void OnAvatarSlotClicked(AvatarEquipSlot slot)
        {
            SelectAvatar(slot);
        }

        private void SelectAvatar(AvatarEquipSlot slot)
        {
            LockAllSlotsLayout();
            
            if (selectedSlot != null)
            {
                selectedSlot.SetEquip(false);
                ReturnSlotToOriginalParent(selectedSlot);
            }

            selectedSlot = slot;
            selectedAvatarId = slot.AvatarId;
            
            MoveSlotToFrontLayer(slot);
            
            slot.SetEquip(true);
        }
        
        private void LockAllSlotsLayout()
        {
            if (isLayoutLocked)
                return;

            isLayoutLocked = true;
            
            foreach (var slot in avatarSlots)
            {
                if (slot == null)
                    continue;
                
                var layoutElement = slot.GetComponent<LayoutElement>();
                if (layoutElement != null)
                    layoutElement.ignoreLayout = true;
            }
        }
        
        private void MoveSlotToFrontLayer(AvatarEquipSlot slot)
        {
            if (frontLayer == null)
                return;
            
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null)
                return;
            
            var worldPos = slot.transform.position;
            var layoutElement = slot.GetComponent<LayoutElement>();
            if (layoutElement != null)
                layoutElement.ignoreLayout = true;
            
            slot.transform.SetParent(frontLayer);
            slot.transform.position = worldPos;
        }
        
        private void ReturnSlotToOriginalParent(AvatarEquipSlot slot)
        {
            int index = System.Array.IndexOf(avatarSlots, slot);
            if (index < 0 || originalParents == null || index >= originalParents.Length) 
                return;
            
            var originalParent = originalParents[index];
            if (originalParent == null)
                return;
            
            var rectTransform = slot.GetComponent<RectTransform>();
            if (rectTransform == null)
                return;
            
            var worldPos = slot.transform.position;
            slot.transform.SetParent(originalParent);
            slot.transform.SetSiblingIndex(originalSiblingIndices[index]);
            slot.transform.position = worldPos;
        }

        private async UniTaskVoid OnConfirmClicked()
        {
            if (string.IsNullOrEmpty(selectedAvatarId))
                return;

            if (isProcessing)
                return;
            
            isProcessing = true;

            try
            {
                var result = await Services.Lobby.InventoryChangeAsync(selectedAvatarId);
                if (!result.IsSuccess)
                {
                    isProcessing = false;
                    return;
                }

                var userInfo = await Services.Lobby.GetUserInfoAsync();
                if (userInfo.IsSuccess)
                    CPPlayer.UserInfo.userDatabase = userInfo.Data;

                OnAvatarSelected?.Invoke(selectedAvatarId);
                OnAvatarSelected = null;

                Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"아바타 설정 중 오류: {e.Message}");
                isProcessing = false;
            }
        }

        protected override void OnClose()
        {
            base.OnClose();
            OnAvatarSelected = null;
        }
    }
}
