using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CAPYBARA.Core;

namespace CAPYBARA.Bundles
{
    public abstract class ViewBase : Poolable
    {
        private static Dictionary<string, ViewBase> _viewMap = new Dictionary<string, ViewBase>();
        //������ �ּ¹��� ���� ����  View -> DefaultUI
        private const string ViewAssetLabelName = "Default";
        public static async UniTask StartLoadAssets(CancellationTokenSource cts)
        {
            _viewMap.Clear();
            //await Addressables.LoadAssetsAsync<GameObject>(ViewAssetLabelName, OnLoadView).WithCancellation(cts.Token);

            var downloadStatus = Addressables.LoadAssetsAsync<GameObject>(ViewAssetLabelName, OnLoadView);

            while (true)
            {
                //Core.SceneLoadResources.loadingAction?.Invoke(downloadStatus.PercentComplete);

                if (downloadStatus.IsDone)
                {
                    break;
                }
                await UniTask.Yield(cts.Token);
            }
        }
        private static void OnLoadView(GameObject go)
        {
#if UNITY_EDITOR
            //Debug.Log($"<color=#f0f>[OnLoadView] {go.name}");
#endif

            var viewBase = go.GetComponent<ViewBase>();
            //var type=viewBase.GetType();
            if (!_viewMap.ContainsKey(go.name))
            {
                _viewMap.Add(go.name, viewBase);
            }

            //Core.SceneLoadResources.loadingAction?.Invoke(((float)_viewMap.Count) / 36.0f);
        }
        public static T Get<T>(string name = null) where T : ViewBase
        {
            if (name == null)
            {
                name = typeof(T).Name;
            }

            return _viewMap[name] as T;
        }
        public static T Create<T>(Transform parent, string name = null) where T : ViewBase
        {
            if (name == null)
            {
                name = typeof(T).Name;
            }

            //Debug.Log(name);
            var view = Instantiate(_viewMap[name] as T, parent);
            return view;
        }
    }
}
