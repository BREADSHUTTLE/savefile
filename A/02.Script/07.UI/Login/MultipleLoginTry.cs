using System;
using CAPYBARA.Bundles;
using CAPYBARA.lobby;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA
{
    public class MultipleLoginTry : MonoBehaviour
    {
        public CPButton cancelBtn;
        public CPButton loginBtn;
        public CPButton closeBtn;
        
        private Action action;
        private Action cancelaction;
        
        private void Awake()
        {
            loginBtn.onClick.AddListener(()=>Login().Forget());
            cancelBtn.onClick.AddListener(() =>
            {
                gameObject.SetActive(false); cancelaction?.Invoke();
            });
            closeBtn.onClick.AddListener(() =>
            {
                gameObject.SetActive(false); cancelaction?.Invoke();
            });
        }

        public void OpenPopupWithAction(Action _action,Action _cancelAction)
        {
            action=_action;
            cancelaction = _cancelAction;
            gameObject.SetActive(true);
        }
        async UniTask Login()
        {
            action?.Invoke();
        }
    }

}
