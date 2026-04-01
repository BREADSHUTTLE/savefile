using System;
using CAPYBARA.Bundles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CAPYBARA
{
    public class GetRewardPopupParameter : IPopupParameter
    {
        public string Title { get; set; }
        public Sprite ItemIcon { get; set; }
        public string Description { get; set; }
        public string ConfirmButtonText { get; set; }
        public Action OnConfirm { get; set; }
    }

    public class PopupGetReward : BasePopup
    {
        [Header("UI")]
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private Image imgItemIcon;
        [SerializeField] private TMP_Text txtDescription;
        
        [Header("Buttons")]
        [SerializeField] private CPButton confirmButton;
        [SerializeField] private TMP_Text txtConfirmButton;

        private Action _onConfirmCallback;

        protected override void OnInit()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }
        }

        protected override void ConfigurePopupContent(IPopupParameter parameter)
        {
            if (parameter is GetRewardPopupParameter rewardParam)
            {
                SetTitle(rewardParam.Title);
                SetItemIcon(rewardParam.ItemIcon);
                SetDescription(rewardParam.Description);
                SetConfirmButtonText(rewardParam.ConfirmButtonText);
                _onConfirmCallback = rewardParam.OnConfirm;
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnClose()
        {
            base.OnClose();
            _onConfirmCallback = null;
        }

        public void SetTitle(string title)
        {
            if (txtTitle != null && !string.IsNullOrEmpty(title))
                txtTitle.text = title;
        }

        public void SetItemIcon(Sprite icon)
        {
            if (imgItemIcon != null && icon != null)
                imgItemIcon.sprite = icon;
        }

        public void SetDescription(string description)
        {
            if (txtDescription != null)
            {
                txtDescription.gameObject.SetActive(!string.IsNullOrEmpty(description));
                txtDescription.text = description ?? string.Empty;
            }
        }

        public void SetConfirmButtonText(string text)
        {
            if (txtConfirmButton != null && !string.IsNullOrEmpty(text))
                txtConfirmButton.text = text;
        }

        public void SetOnConfirmCallback(Action callback)
        {
            _onConfirmCallback = callback;
        }

        private void OnConfirmButtonClicked()
        {
            _onConfirmCallback?.Invoke();
            Close();
        }
    }
}
