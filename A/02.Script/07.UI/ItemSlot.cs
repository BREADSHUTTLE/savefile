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
    public class ItemSlot : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text itemAmount;
        [SerializeField] private CPButton eventBtn;

        private ItemID itemId;
        private string isEffective;
        private string serverItemName;

        public void Init(ItemID id, CAPYBARA.lobby.Inventory item)
        {
            itemId = id;
            isEffective = item.Item.EffectApplyType;
            serverItemName = item.Item.Name;
            
            SetImage(itemId);
            SetText(GetItemName(itemId), item.Amount.ToString("N0"));
            
            eventBtn.onClick.RemoveAllListeners();
            eventBtn.onClick.AddListener(TouchBtn);
        }

        private void SetImage(ItemID id)
        {
            var sprite = ItemBundle.Loaded.GetItemSprite(id.ToString());
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
        }

        private string GetItemName(ItemID id)
        {
            var itemNameInfo = StaticData.GetItemNameInfo(id);
            if (itemNameInfo != null && !string.IsNullOrEmpty(itemNameInfo.message_Kr))
                return itemNameInfo.message_Kr;
            
            if (!string.IsNullOrEmpty(serverItemName))
                return serverItemName;
            
            var product = StaticData.Wrapper.iAPProducts?.FirstOrDefault(p => p.productId.ToUpper().Contains(id.ToString()) || id.ToString().Contains(p.productId.ToUpper()));
            if (product != null && !string.IsNullOrEmpty(product.title_Kr))
                return product.title_Kr;
            
            return id.ToString().Replace("_", " ");
        }

        private bool IsEmoticonItem(ItemID id)
        {
            return id == ItemID.EMOTICON_INVITE_FRIEND || id == ItemID.EMOTICON_PLAY_POINT;
        }

        private void SetText(string name, string amount)
        {
            if (itemName != null)
                itemName.text = name;

            if (itemAmount != null)
                itemAmount.text = $"{amount}개";
        }

        private void TouchBtn()
        {
            _ = itemId switch
            {
                ItemID.NICKNAME_CHANGE => (BasePopup)PopupManager.Instance.Open<PopupChangeNickname>(),
                ItemID.NICKNAME_CHANGE_FIRST => (BasePopup)PopupManager.Instance.Open<PopupCreateNickname>(),
                _ => null
            };
        }
    }
}
