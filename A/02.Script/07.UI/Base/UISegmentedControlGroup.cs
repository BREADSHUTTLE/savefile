using System;
using System.Collections.Generic;
using UnityEngine;

namespace CAPYBARA.Bundles
{
    public class UISegmentedControlGroup : MonoBehaviour
    {
        [SerializeField] private List<UISegmentedControl> toggles = new List<UISegmentedControl>();

        public Action<int> onIndexChanged;

        private int currentIndex = 0;
        public int CurrentIndex => currentIndex;
        public List<UISegmentedControl> Toggles => toggles;
        private bool _initialized;

        private void Start()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (_initialized)
                return;

            for (int i = 0; i < toggles.Count; i++)
            {
                int index = i;
                if (toggles[i].Button != null)
                    toggles[i].Button.onClick.AddListener(() => SetActiveToggle(index));
            }

            SetActiveToggle(currentIndex, false);
            _initialized = true;
        }

        public void SetActiveToggle(int index, bool notify = true)
        {
            if (index < 0 || index >= toggles.Count)
                return;

            currentIndex = index;

            for (int i = 0; i < toggles.Count; i++)
                toggles[i].SetIsOn(i == index, false);

            if (notify)
                onIndexChanged?.Invoke(index);
        }

        private void OnDisable()
        {
        }

        public void DeactivateAll()
        {
            for (int i = 0; i < toggles.Count; i++)
                toggles[i].SetIsOn(false, false);
        }

        public void ReactivateCurrent()
        {
            if (currentIndex >= 0 && currentIndex < toggles.Count)
                toggles[currentIndex].SetIsOn(true, false);
        }
        
        public void SetInteractable(bool interactable)
        {
            for (int i = 0; i < toggles.Count; i++)
            {
                if (toggles[i].Button != null)
                    toggles[i].Button.interactable = interactable;
            }
        }
    }
}
