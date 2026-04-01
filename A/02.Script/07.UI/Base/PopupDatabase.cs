using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System.Threading;
using CAPYBARA.Bundles;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CAPYBARA
{
    [CreateAssetMenu(fileName = "PopupDatabase", menuName = "CAPYBARA/Popup Database")]
    public class PopupDatabase : ScriptableObject
    {
        public static PopupDatabase Loaded;
        private const string BundleResourcekey = "PopupDatabase";

        [Serializable]
        public class PopupEntry
        {
            [Tooltip("팝업 이름")]
            public string popupName;
            
            [Tooltip("팝업 프리팹")]
            public BasePopup prefab;
            
            [Tooltip("팝업 뎁스 (높을수록 위에 표시)")]
            [Range(0, 1000)]
            public int depth;
        }

        [Header("등록된 팝업 목록 (Toast 팝업은 여기서 뎁스 정리 안합니다! 옵션 분리)")]
        [SerializeField] private List<PopupEntry> popupEntries = new List<PopupEntry>();
        public IReadOnlyList<PopupEntry> PopupEntries => popupEntries;

        public T GetPopupPrefab<T>() where T : BasePopup
        {
            foreach (var entry in popupEntries)
            {
                if (entry.prefab != null && entry.prefab is T)
                {
                    return entry.prefab as T;
                }
            }
            return null;
        }

        public int GetDepth<T>() where T : BasePopup
        {
            foreach (var entry in popupEntries)
            {
                if (entry.prefab != null && entry.prefab is T)
                {
                    return entry.depth;
                }
            }
            return 0;
        }
        
        public int GetDepth(Type type)
        {
            foreach (var entry in popupEntries)
            {
                if (entry.prefab != null && entry.prefab.GetType() == type)
                {
                    return entry.depth;
                }
            }
            return 0;
        }

        public BasePopup GetPopupPrefab(string name)
        {
            foreach (var entry in popupEntries)
            {
                if (entry.popupName == name || 
                    (entry.prefab != null && entry.prefab.name == name))
                {
                    return entry.prefab;
                }
            }
            return null;
        }

        public static AsyncOperationHandle<PopupDatabase> BeginLoad()
        {
            return Addressables.LoadAssetAsync<PopupDatabase>(BundleResourcekey);
        }
        public static async UniTask StartLoadAsset(CancellationTokenSource cts)
        {
           Loaded = await Addressables.LoadAssetAsync<PopupDatabase>(BundleResourcekey).WithCancellation(cts.Token);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var entry in popupEntries)
            {
                if (entry.prefab != null && string.IsNullOrEmpty(entry.popupName))
                {
                    entry.popupName = entry.prefab.GetType().Name;
                }
            }
        }
#endif
    }
}
