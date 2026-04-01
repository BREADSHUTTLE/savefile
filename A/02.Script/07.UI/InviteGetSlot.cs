using System;
using System.Linq;
using BlackTree.Bundles;
using CAPYBARA.Bundles;
using CAPYBARA.Core;
using CAPYBARA.Definition;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class InviteGetSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtCount;
        [SerializeField] private TMP_Text txtState;
        [SerializeField] private TMP_Text txtRewardAmount;
        [SerializeField] private Image imgRewardIcon;
        [SerializeField] private CPButton btnClaim;
        [SerializeField] private GameObject goDone;
        [SerializeField] private GameObject[] goClaimEffect;

        private Quest _quest;
        private Action _onClaimed;
        private int _index;

        public void Init(int index, Quest quest, Action onClaimed)
        {
            _quest = quest;
            _onClaimed = onClaimed;
            _index = index;

            if (txtCount != null)
                txtCount.text = $"{index + 1}명";

            SetRewardIcon();
            SetRewardAmount();
            UpdateState();
        }

        private void Awake()
        {
            if (btnClaim != null)
                btnClaim.onClick.AddListener(OnClickClaim);
        }

        private void SetRewardAmount()
        {
            if (_quest == null || txtRewardAmount == null)
                return;

            if (!Enum.TryParse<ItemID>(_quest.RewardItemId, out var itemId))
                return;

            if (itemId == ItemID.DEFAULT_CURRENCY || itemId == ItemID.DEFAULT_CURRENCY_SALE)
                txtRewardAmount.text = Extension.ToKoreanFormatReward(_quest.RewardValue);
            else if (_quest.RewardItemId.StartsWith("AVATAR"))
                txtRewardAmount.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Avatar].StringToLocal;
            else if (_quest.RewardItemId.StartsWith("EMOTICON"))
                txtRewardAmount.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Emot].StringToLocal;
            else
                txtRewardAmount.text = _quest.RewardItemId;
        }

        private void SetRewardIcon()
        {
            if (_quest == null || imgRewardIcon == null)
                return;

            if (!Enum.TryParse<ItemID>(_quest.RewardItemId, out var itemId))
                return;

            var itemBundle = ItemBundle.Loaded;
            if (itemBundle == null)
                return;

            if (itemId == ItemID.DEFAULT_CURRENCY)
            {
                var sprite = itemBundle.GetItemSprite($"COIN_{_index + 2}");
                if (sprite != null)
                    imgRewardIcon.sprite = sprite;
            }
            else
            {
                var sprite = itemBundle.GetItemSprite(_quest.RewardItemId, true);
                if (sprite != null)
                    imgRewardIcon.sprite = sprite;
            }
        }

        private void UpdateState()
        {
            if (_quest == null)
                return;

            bool claimed = _quest.ReceivedRewardValue > 0;
            bool canClaim = _quest.QuestValue >= _quest.MaxCount && !claimed;

            if (goDone != null)
                goDone.SetActive(claimed);

            if (goClaimEffect != null && goClaimEffect.Length > 0)
            {
                for (int i = 0; i < goClaimEffect.Length; i++)
                    goClaimEffect[i].SetActive(canClaim);
            }

            if (txtState != null)
            {
                if (claimed)
                    txtState.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ReceiveCompleted].StringToLocal;
                else if (canClaim)
                    txtState.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ReceiveAvailable].StringToLocal;
                else
                    txtState.text = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ReceiveNotAvailable].StringToLocal;
            }

            if (btnClaim != null)
                btnClaim.interactable = canClaim;
        }

        private void OnClickClaim()
        {
            if (_quest == null)
                return;

            bool claimed = _quest.ReceivedRewardValue > 0;
            bool canClaim = _quest.QuestValue >= _quest.MaxCount && !claimed;
            if (!canClaim)
                return;

            ClaimReward().Forget();
        }

        private async UniTask ClaimReward()
        {
            var result = await Services.Lobby.UserQuestRequestAsync(_quest.QuestId);
            if (result == null || !result.IsSuccess)
                return;

            var updatedQuest = result.Data?.QuestList?.FirstOrDefault(q => q.QuestId == _quest.QuestId);
            if (updatedQuest != null)
                _quest = updatedQuest;

            if (Enum.TryParse<ItemID>(_quest.RewardItemId, out var itemId))
            {
                if (itemId == ItemID.DEFAULT_CURRENCY)
                {
                    var userinfo = await Services.Lobby.GetUserInfoAsync();
                    if (userinfo.IsSuccess)
                        CPPlayer.UserInfo.userDatabase = userinfo.Data;

                    CPPlayer.OutGame.callbackAfterGetMoneyAndBox?.Invoke();
                }
                else
                {
                    var itemBundle = ItemBundle.Loaded;
                    var sprite = itemBundle?.GetItemSprite(_quest.RewardItemId, true);
                    PopupManager.Instance.Open<PopupGetReward>(new GetRewardPopupParameter
                    {
                        Title = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.ItemAcquired].StringToLocal,
                        ItemIcon = sprite,
                        Description = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.CheckItemInVault].StringToLocal,
                        ConfirmButtonText = StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.Close].StringToLocal
                    });
                    CPPlayer.OutGame.RenewInventory?.Invoke();
                }
            }

            UpdateState();
            _onClaimed?.Invoke();
        }
    }
}
