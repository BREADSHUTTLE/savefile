using System.Collections.Generic;
using System.Linq;
using CAPYBARA.Core;
using UnityEngine;

namespace CAPYBARA
{
    public class BackButtonManager : MonoSingleton<BackButtonManager>
    {
        private List<IBackButtonHandler> handlers = new List<IBackButtonHandler>();
        private bool isExitPopupOpen = false;
        private PopupToast _exitPopup;

        private bool _disabled;                                                                 
        public void Disable() => _disabled = true;
        public void Enable() => _disabled = false;
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                HandleBackButton();
        }

        public void Register(IBackButtonHandler handler)
        {
            if (handler == null || handlers.Contains(handler))
                return;
                
            handlers.Add(handler);
#if UNITY_EDITOR
            Debug.Log($"[BackButtonManager] Registered: {handler.GetType().Name}");
#endif
        }

        public void Unregister(IBackButtonHandler handler)
        {
            if (handler == null)
                return;
                
            handlers.Remove(handler);
#if UNITY_EDITOR
            Debug.Log($"[BackButtonManager] Unregistered: {handler.GetType().Name}");
#endif
        }

        private void HandleBackButton()
        {
            handlers.RemoveAll(h => h == null || (h is Object obj && obj == null));
            
            var activeHandler = handlers
                .Where(h => h.CanHandleBackButton)
                .OrderByDescending(h => h.BackButtonPriority)
                .FirstOrDefault();

            if (activeHandler != null)
            {
                activeHandler.OnBackButtonPressed();
#if UNITY_EDITOR
                Debug.Log($"[BackButtonManager] Handled by: {activeHandler.GetType().Name} (Priority: {activeHandler.BackButtonPriority})");
#endif
            }
            else
            {
                ShowExitConfirmIfNeeded();
            }
        }

        private void ShowExitConfirmIfNeeded()
        {
            if (_disabled)
                return;

            if (isExitPopupOpen)
            {
                isExitPopupOpen = false;
                _exitPopup?.OnBackButtonPressed();
                _exitPopup = null;
                return;
            }

            // if (Application.platform != RuntimePlatform.Android)
            //     return;

            if (CPPlayer.InGame.isInGame)
                return;
            
            isExitPopupOpen = true;
            PopupManager.Instance.Open<PopupToast>(popup =>
            {
                _exitPopup = popup;
                popup.ShowPopupTwoButtons(
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.GameExit].StringToLocal,
                    StaticData.Wrapper.localizeddescDict[LocalizeDescKeys.GameExitConfirm].StringToLocal,
                    () =>
                    {
                        isExitPopupOpen = false;
                        _exitPopup = null;
                        Application.Quit();
                    },
                    () =>
                    {
                        isExitPopupOpen = false;
                        _exitPopup = null;
                    }
                );
            });
        
        }

        public int ActiveHandlerCount => handlers.Count(h => h != null && h.CanHandleBackButton);

        public bool HasActiveHandlerAbovePriority(int priority)
        {
            return handlers.Any(h => h != null && h.CanHandleBackButton && h.BackButtonPriority > priority);
        }

        protected override void Release()
        {
            base.Release();
            handlers.Clear();
        }
    }
}

