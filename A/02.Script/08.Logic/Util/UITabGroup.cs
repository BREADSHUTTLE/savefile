using CAPYBARA.Bundles;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UITabGroup : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public string key;           
        public CPButton button;      
        public GameObject onObject;  
        public GameObject window;    
    }

    public Tab[] tabs;
    public int defaultIndex = 0;
    public bool selectOnAwake = true;
    [HideInInspector]public Action<int> onTabChanged; 

    private int _current = -1;
    private UnityAction[] _handlers;

    void Awake()
    {
        Wire();
    }

    void OnEnable()
    {
        if (selectOnAwake && _current < 0)
            Select(defaultIndex, false);
    }

    void OnDestroy() => Unwire();

    void Wire()
    {
        if (tabs == null) return;
        _handlers = new UnityAction[tabs.Length];

        for (int i = 0; i < tabs.Length; i++)
        {
            int idx = i;
            var t = tabs[i];
            if (t == null || t.button == null) continue;

            UnityAction h = () => Select(idx);
            _handlers[i] = h;

            t.button.onClick.RemoveListener(h);
            t.button.onClick.AddListener(h);
        }
    }

    void Unwire()
    {
        if (tabs == null || _handlers == null) return;
        for (int i = 0; i < tabs.Length; i++)
        {
            var t = tabs[i];
            var h = _handlers[i];
            if (t != null && t.button != null && h != null)
                t.button.onClick.RemoveListener(h);
        }
        _handlers = null;
    }

    public void Select(int index)
    {
        Select(index, true);
    }
    
    public void Select(int index, bool invokeCallback)
    {
        if (tabs == null || tabs.Length == 0) return;
        index = Mathf.Clamp(index, 0, tabs.Length - 1);

        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = (i == index);
            var t = tabs[i];
            if (t == null) continue;

            if (t.button != null)
            {
                if (active)
                    t.button.Select();
                else
                    t.button.UnSelect();
            }
            
            if (t.onObject)
            {
                t.onObject.SetActive(active);
                
                // 활성화될 때 알파값 복원 및 DOTween 애니메이션 재시작
                if (active)
                {
                    var image = t.onObject.GetComponent<Image>();
                    if (image != null)
                    {
                        var color = image.color;
                        color.a = 1f;
                        image.color = color;
                    }
                    
                    t.onObject.transform.DORestart();
                }
            }
            if (t.window) 
                t.window.SetActive(active);
        }
        if (tabs[index].onObject) 
            tabs[index].onObject?.SetActive(true);
        if (tabs[index].window)
        tabs[index].window.SetActive(true);

        _current = index;
        
        if (invokeCallback)
            onTabChanged?.Invoke(index);
    }

    public void SelectByKey(string key)
    {
        if (string.IsNullOrEmpty(key) || tabs == null) return;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] != null && tabs[i].key == key)
            {
                Select(i);
                return;
            }
        }
    }

    public int CurrentIndex => _current;
    public string CurrentKey =>
        (_current >= 0 && _current < (tabs?.Length ?? 0)) ? tabs[_current].key : null;

    private void Refresh(int index)
    {
        for (int i = 0; i < (tabs?.Length ?? 0); i++)
        {
            bool active = (i == index);
            var t = tabs[i];
            if (t == null) continue;
            
            if (t.button != null)
            {
                if (active)
                    t.button.Select();
                else
                    t.button.UnSelect();
            }
            
            if (t.onObject) t.onObject.SetActive(active);
            if (t.window) t.window.SetActive(active);
        }
    }
}
