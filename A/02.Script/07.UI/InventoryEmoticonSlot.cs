using System;
using System.Linq;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class InventoryEmoticonSlot : MonoBehaviour
    {
        [SerializeField] private Image imgEmoticon;
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtRemainTime;
        [SerializeField] private GameObject objEquipped;
        [SerializeField] private CPButton btnSelect;
        
        public Action<InventoryEmoticonSlot> onClickSelect;
        
        public string ItemId { get; private set; }
        public int DurationSeconds { get; private set; }
        public bool IsEquipped { get; private set; }
        
        private void Awake()
        {
            if (btnSelect != null)
                btnSelect.onClick.AddListener(() => onClickSelect?.Invoke(this));
        }
        
        public void Init(CAPYBARA.lobby.Inventory item)
        {
            ItemId = item.ItemId;
            DurationSeconds = CalculateRemainingSeconds(item.EffectEndAt);
            IsEquipped = item.IsEffective;

            SetImage(item.ItemId);

            string displayName = null;
            if (Enum.TryParse<ItemID>(item.ItemId, true, out var parsedId))
            {
                var itemNameInfo = StaticData.Wrapper.itemNameInfo?.FirstOrDefault(x => x.itemID == parsedId);
                displayName = itemNameInfo?.message_Kr;
            }
            
            if (string.IsNullOrEmpty(displayName))
                displayName = item.Item?.Name;

            SetName(displayName);
            UpdateRemainTimeText();
            SetEquipped(IsEquipped);
        }
        
        private void SetImage(string itemIdStr)
        {
            var sprite = ItemBundle.Loaded?.GetItemSprite(itemIdStr);
            if (imgEmoticon != null)
            {
                imgEmoticon.sprite = sprite;
                imgEmoticon.gameObject.SetActive(sprite != null);
            }
        }
        
        private void SetName(string name)
        {
            if (txtName != null)
                txtName.text = !string.IsNullOrEmpty(name) ? name : "";
        }
        
        private int CalculateRemainingSeconds(int effectEndAt)
        {
            if (effectEndAt == 0)
                return 0;
            
            int currentTimestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int remaining = effectEndAt - currentTimestamp;
            return remaining > 0 ? remaining : 0;
        }
        
        private void UpdateRemainTimeText()
        {
            if (txtRemainTime == null)
                return;
            
            if (DurationSeconds <= 0)
            {
                txtRemainTime.gameObject.SetActive(true);
                txtRemainTime.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.PermanentOwnership].StringToLocal;
                return;
            }
            
            txtRemainTime.gameObject.SetActive(true);
            
            int remainDays = DurationSeconds / 86400;
            int remainHours = (DurationSeconds % 86400) / 3600;
            int remainMinutes = (DurationSeconds % 3600) / 60;
            
            if (remainDays > 0)
                txtRemainTime.text = $"{remainDays}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.DaysRemaining].StringToLocal}";
            else if (remainHours > 0)
                txtRemainTime.text = $"{remainHours}{StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.HoursRemaining].StringToLocal}";
            else
                txtRemainTime.text = remainMinutes > 0 ? $"{remainMinutes}분 남음" : StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ExpiringSoon].StringToLocal;
        }
        
        public void SetEquipped(bool isEquipped)
        {
            IsEquipped = isEquipped;
            if (objEquipped != null)
                objEquipped.SetActive(isEquipped);
        }
    }
}
