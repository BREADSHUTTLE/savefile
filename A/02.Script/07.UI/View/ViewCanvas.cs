using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace CAPYBARA.Bundles
{
    public abstract class ViewCanvas : MonoBehaviour
    {
        public bool IsCloseOnTouchAnother => isCloseOnTouchAnother;
        public RectTransform Wrapped => _popupAnimationPanel;
        public CPFixedAspectScaler aspectScaler;
        public Image blackBG;

        [SerializeField] protected bool isCloseable = false;
        [SerializeField] private bool isCloseOnTouchAnother;
        [SerializeField] private string sortingLayerName = "UI";
        [SerializeField] private RectTransform _popupAnimationPanel;
        protected Canvas canvas;
        private static Dictionary<Type, ViewCanvas> _viewCanvasPrefab = new Dictionary<Type, ViewCanvas>();
        private static Dictionary<Type, ViewCanvas> _viewCanvasMap = new Dictionary<Type, ViewCanvas>();
        private static Dictionary<Type, ViewCanvas> _visibleViewCanvasMap = new Dictionary<Type, ViewCanvas>();

        private static Dictionary<Type, ViewCanvas> _visibleCloseableViewCanvasMap = new Dictionary<Type, ViewCanvas>();
        //������ �ּ¹��� ���� ����_ViewCanvas -> UICanvas
        private const string ViewCanvasAssetLabelName = "Canvas";
        private UnityAction<bool> _onChangeVisible;

        public static event Action OnBackButton;
        private bool IsSetCloseable { get; set; }
        public bool IsVisible { get; private set; }

        public static Dictionary<Type, ViewCanvas> AllActivateCanvas
        {
            get
            {
                return _viewCanvasMap;
            }
        }

        public Canvas MyCanvas
        {
            get
            {
                return canvas;
            }
        }
        public void SetCloseable(bool value)
        {
            IsSetCloseable = value;
            isCloseable = value;

            var type = GetType();

            switch (isCloseable)
            {
                case true:
                    if (!_visibleViewCanvasMap.ContainsKey(type))
                        _visibleViewCanvasMap.Add(type, this);
                    if (!_visibleCloseableViewCanvasMap.ContainsKey(type))
                        _visibleCloseableViewCanvasMap.Add(type, this);
                    break;
                case false:
                    if (_visibleCloseableViewCanvasMap.ContainsKey(type))
                        _visibleCloseableViewCanvasMap.Remove(type);
                    if (_visibleViewCanvasMap.ContainsKey(type))
                        _visibleViewCanvasMap.Remove(type);
                    break;
            }
        }

        public ViewCanvas UnbindOnChangeVisible()
        {
            _onChangeVisible = null;
            return this;
        }
        public ViewCanvas BindOnChangeVisible(UnityAction<bool> action)
        {
            _onChangeVisible += action;
            return this;
        }
        public static ICollection<ViewCanvas> GetVisibleCanvases()
        {
            return _visibleViewCanvasMap.Values;
        }
        public static ICollection<ViewCanvas> GetCloseableCanvases()
        {
            return _visibleCloseableViewCanvasMap.Values;
        }
        public static bool GetHasCloseable()
        {

            return _visibleCloseableViewCanvasMap.Count > 0;
        }
        public static void GoHome()
        {
            var types = new List<Type>();
            foreach (var kv in _visibleCloseableViewCanvasMap)
            {
                kv.Value.canvas.enabled = false;

                kv.Value._onChangeVisible?.Invoke(false);
                types.Add(kv.Key);
            }

            _visibleCloseableViewCanvasMap.Clear();
            for (int i = types.Count - 1; i >= 0; i--)
            {

                _visibleViewCanvasMap.Remove(types[i]);
            }
        }
        public ViewCanvas SetVisibleForce(bool flag)
        {
            canvas.enabled = flag;
            return this;
        }
        public ViewCanvas SetVisible(bool flag)
        {
            if (IsVisible == flag) return this;
            if (IsSetCloseable && !flag) SetCloseable(false);
            IsVisible = flag;
            gameObject.SetActive(flag);
            canvas.enabled = flag;
            _onChangeVisible?.Invoke(flag);


            var type = GetType();
            if (flag)
            {
                if (!_visibleViewCanvasMap.ContainsKey(type))
                {

                    _visibleViewCanvasMap.Add(type, this);
                }

                if (this.isCloseable)
                {
                    if (!_visibleCloseableViewCanvasMap.ContainsKey(type))
                    {
                        _visibleCloseableViewCanvasMap.Add(type, this);
                    }
                }
            }
            else
            {
                if (_visibleCloseableViewCanvasMap.ContainsKey(type))
                {
                    _visibleCloseableViewCanvasMap.Remove(type);
                }
                if (_visibleViewCanvasMap.ContainsKey(type))
                {
                    _visibleViewCanvasMap.Remove(type);
                }
            }
            return this;
        }
     
        public static async UniTask StartLoadAssets(CancellationTokenSource cts)
        {
            _viewCanvasPrefab.Clear();
            _visibleViewCanvasMap.Clear();
            _viewCanvasMap.Clear();
            _visibleCloseableViewCanvasMap.Clear();

            
            //Debug.LogError("리소스 로드 7");
            var downloadStatus = Addressables.LoadAssetsAsync<GameObject>(ViewCanvasAssetLabelName, OnLoadViewCanvas);
            
            while (true)
            {
                if (downloadStatus.IsDone)
                {
                    break;
                }
                await UniTask.Yield(cts.Token);
            }
        }
        public static void GoBack()
        {
            //PrintForDebug();
            if (OnBackButton != null)
            {
                OnBackButton.Invoke();
                OnBackButton = null;
                return;
            }

            if (_visibleCloseableViewCanvasMap.Count > 0)
            {
                var view = _visibleCloseableViewCanvasMap.Values.Last();
                if (view != null && view.isCloseable)
                {
                    view.SetVisible(false);
                }
            }
        }
        public static void PrintForDebug()
        {
            int index = 0;
            foreach (var viewCanvas in _visibleViewCanvasMap.Values)
                if (viewCanvas.isCloseable)
                {
                    Debug.Log($"<color=#0cc>{index++} : {viewCanvas.name}</color>");
                }
        }
        public static T Get<T>() where T : ViewCanvas
        {
            var type = typeof(T);
            return _viewCanvasMap[type] as T;
        }
        public static T Create<T>(Transform parent) where T : ViewCanvas
        {
            var view = Instantiate(_viewCanvasPrefab[typeof(T)] as T, parent);
            view.Setup();
            return view;
        }
        public static void OnLoadViewCanvas(GameObject go)
        {
            var viewCanvas = go.GetComponent<ViewCanvas>();
            
            // ViewCanvas를 상속받지 않는 프리팹은 스킵 (예: BasePopup으로 변경된 경우)
            if (viewCanvas == null)
                return;
            
            var type = viewCanvas.GetType();
            if (!_viewCanvasPrefab.ContainsKey(type))
            {
                _viewCanvasPrefab.Add(type, viewCanvas);
            }
        }
        
        private ViewCanvas Setup()
        {
            if (canvas == null)
                canvas = GetComponentInChildren<Canvas>();
            if (canvas.worldCamera == null)
                canvas.worldCamera = Camera.main != null
                    ? Camera.main
                    : GameObject.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

            // canvas.GetComponent<CanvasScaler>().matchWidthOrHeight =
            //     Mathf.Clamp(canvas.worldCamera.aspect, 0, 1);
            canvas.sortingLayerName = sortingLayerName;
            canvas.enabled = false;

            var type = GetType();
            if (!_viewCanvasMap.ContainsKey(type))
            {
                _viewCanvasMap.Add(type, this);
            }
            else
            {
                _viewCanvasMap[type] = this;
            }
            return this;
        }


#if UNITY_EDITOR
        void Update()
        {
            if (canvas != null)
            {
                // canvas.GetComponent<CanvasScaler>().matchWidthOrHeight =
                //     Mathf.Clamp(canvas.worldCamera.aspect, 0, 1);
            }
        }
#endif

    }

}
