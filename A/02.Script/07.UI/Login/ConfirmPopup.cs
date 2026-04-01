using System;
using CAPYBARA.Bundles;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace CAPYBARA
{
    public class ConfirmPopup : MonoBehaviour,IBackButtonSender
    {
        [SerializeField] TMP_Text txtTitle;
        [SerializeField] private TMP_Text descTitle;
        [SerializeField] private CPButton confirmBtn;
        [SerializeField] private CPButton cancelBtn;
        [SerializeField] private CPButton closeBtn;
        private Action _confirmCallback;
        private Action _cancelCallback;
        private bool _callbackInvoked;

        private void Awake()
        {
            closeBtn.onClick.AddListener(OnBackButtonPressed);
        }

        public void OpenConfirmPopup(string title, string desc, Action confirmcallback, Action cancelcallback)
        {
            SceneLoadResources.OpenPopup(this);
            txtTitle.text = title;
            descTitle.text = desc;
            _confirmCallback = confirmcallback;
            _cancelCallback = cancelcallback;
            _callbackInvoked = false;
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(() =>
            {
                _callbackInvoked = true;
                _confirmCallback?.Invoke();
                OnBackButtonPressed();
            });
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(() =>
            {
                _callbackInvoked = true;
                _cancelCallback?.Invoke();
                OnBackButtonPressed();
            });
            
        }
        
        public void OpenConfirmPopup( Action confirmcallback, Action cancelcallback)
        {
            SceneLoadResources.OpenPopup(this);
            this.gameObject.SetActive(true);
            _confirmCallback = confirmcallback;
            _cancelCallback = cancelcallback;
            _callbackInvoked = false;
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(() =>
            {
                _callbackInvoked = true;
                _confirmCallback?.Invoke();
                OnBackButtonPressed();
            });
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(() =>
            {
                _callbackInvoked = true;
                _cancelCallback?.Invoke();
                OnBackButtonPressed();
            });
            
        }


        public void OnBackButtonPressed()
        {
            if (!_callbackInvoked)
            {
                _callbackInvoked = true;
                _cancelCallback?.Invoke();
            }
            SceneLoadResources.ClosePopup();
        }
        
        public void CloseThisWindow()
        {
            this.gameObject.SetActive(false);
        }
    }

}
