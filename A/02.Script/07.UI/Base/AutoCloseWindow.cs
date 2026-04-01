using System;
using UnityEngine;

namespace CAPYBARA
{
    public class AutoCloseWindow : MonoBehaviour
    {
        [SerializeField] private float liveTime = 5.0f;
        float elapsedTime = 0.0f;
        
        public static event System.Action<GameObject> OnModalAutoClose;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        private void OnEnable()
        {
            elapsedTime = 0.0f;
        }

        // Update is called once per frame
        void Update()
        {
            if (this.gameObject.activeInHierarchy)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime > liveTime)
                {
                    OnModalAutoClose?.Invoke(gameObject);
                    this.gameObject.SetActive(false);
                }
                    
            }
        }

        public void CloseThisWindow()
        { 
            OnModalAutoClose?.Invoke(gameObject);
            this.gameObject.SetActive(false);
        }
    }

}
