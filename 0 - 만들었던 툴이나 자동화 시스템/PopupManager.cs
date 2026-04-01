using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace CAPYBARA
{
    /// <summary>
    /// 팝업 관리자
    /// 스택 기반으로 팝업을 관리하고, 뒤로가기 처리를 담당합니다.
    /// </summary>
    public class PopupManager : MonoSingleton<PopupManager>
    {
        [Header("Settings")]
        [SerializeField] private Transform popupContainer;
        
        [Header("Popup Database")]
        [Tooltip("팝업 프리팹들이 등록된 데이터베이스 (ScriptableObject)")]
        [SerializeField] private PopupDatabase popupDatabase;
        
        // 현재 열려있는 팝업 스택
        private Stack<BasePopup> popupStack = new Stack<BasePopup>();
        
        // 등록된 팝업 프리팹들
        private Dictionary<Type, BasePopup> popupPrefabs = new Dictionary<Type, BasePopup>();
        
        // 생성된 팝업 인스턴스 캐시
        private Dictionary<Type, BasePopup> popupInstances = new Dictionary<Type, BasePopup>();
        
        // 팝업별 뎁스 값 캐시
        private Dictionary<Type, int> popupDepths = new Dictionary<Type, int>();

        /// 현재 열려있는 팝업이 있는지 여부
        public bool HasOpenPopup => popupStack.Count > 0;
        
        /// 현재 최상위 팝업
        public BasePopup CurrentPopup => popupStack.Count > 0 ? popupStack.Peek() : null;
        
        /// 열려있는 팝업 개수
        public int OpenPopupCount => popupStack.Count;

        protected override void Init()
        {
            base.Init();
            
            if (popupContainer == null)
                popupContainer = transform;
            
            // PopupDatabase에서 프리팹 등록
            if (popupDatabase != null)
            {
                foreach (var entry in popupDatabase.PopupEntries)
                {
                    if (entry.prefab != null)
                        RegisterPopupInternal(entry.prefab, entry.depth);
                }
            }
        }

        // 뒤로가기 처리는 BackButtonManager에서 통합 관리
        // BasePopup이 IBackButtonHandler를 구현하여 자동 처리됨

        // 내부 프리팹 등록
        private void RegisterPopupInternal(BasePopup prefab, int depth)
        {
            var type = prefab.GetType();
            if (!popupPrefabs.ContainsKey(type))
            {
                popupPrefabs[type] = prefab;
                popupDepths[type] = depth;
#if UNITY_EDITOR
                Debug.Log($"[PopupManager] Registered: {type.Name} (Depth: {depth})");
#endif
            }
        }

        // 팝업 프리팹 등록 (런타임에서 코드로 등록)
        public void RegisterPopup<T>(T prefab, int depth = 0) where T : BasePopup
        {
            RegisterPopupInternal(prefab, depth);
        }

        // 팝업 세팅만 (Open 없이)
        public T Setup<T>(Action<T> onSetup = null) where T : BasePopup
        {
            var popup = GetOrCreatePopup<T>();
            if (popup == null)
            {
                Debug.LogError($"[PopupManager] Popup not found: {typeof(T).Name}");
                return null;
            }

            onSetup?.Invoke(popup);
            return popup;
        }
        

        // 팝업 열기
        public T Open<T>(Action<T> onSetup = null) where T : BasePopup
        {
            var popup = GetOrCreatePopup<T>();
            if (popup == null)
            {
                Debug.LogError($"[PopupManager] Popup not found: {typeof(T).Name}");
                return null;
            }

            onSetup?.Invoke(popup);
            
            // 스택에 추가
            popupStack.Push(popup);
            
            // Depth 기반으로 Sibling 순서 조정
            SortPopupsByDepth();
            
            popup.Open();
            
            return popup;
        }
        
        public T Open<T>(IPopupParameter parameter,Action<T> onSetup = null) where T : BasePopup
        {
            var popup = GetOrCreatePopup<T>();
            if (popup == null)
            {
                Debug.LogError($"[PopupManager] Popup not found: {typeof(T).Name}");
                return null;
            }

            onSetup?.Invoke(popup);
            
            // 스택에 추가
            popupStack.Push(popup);
            
            // Depth 기반으로 Sibling 순서 조정
            SortPopupsByDepth();
            
            popup.Open(parameter);
            
            return popup;
        }
        
        // 팝업을 열고 닫힐 때까지 기다리기
        public async Task OpenAsync<T>(Action<T> onSetup = null) where T : BasePopup
        {
            var popup = Open<T>(onSetup);
            if (popup == null) return;

            var tcs = new TaskCompletionSource<bool>();
            popup.OnPopupClosed += () => tcs.TrySetResult(true);
            await tcs.Task;
        }

        public async Task OpenAsync<T>(IPopupParameter parameter, Action<T> onSetup = null) where T : BasePopup
        {
            var popup = Open<T>(parameter, onSetup);
            if (popup == null) return;

            var tcs = new TaskCompletionSource<bool>();
            popup.OnPopupClosed += () => tcs.TrySetResult(true);
            await tcs.Task;
        }

        // Depth 기반으로 열려있는 팝업들의 Sibling 순서 정렬
        private void SortPopupsByDepth()
        {
            var openPopups = new List<BasePopup>();
            
            foreach (var kvp in popupInstances)
            {
                if (kvp.Value.gameObject.activeSelf || popupStack.Contains(kvp.Value))
                    openPopups.Add(kvp.Value);
            }

            var stackList = new List<BasePopup>(popupStack);
            
            // Depth 낮은 순서대로 정렬 (낮은게 뒤에, 높은게 앞에)
            openPopups.Sort((a, b) => 
            {
                var depthA = GetPopupDepth(a.GetType());
                var depthB = GetPopupDepth(b.GetType());

                // Depth가 같으면 나중에 열린 팝업이 뒤로 가게
                if (depthA == depthB)
                {
                    var indexA = stackList.IndexOf(a);
                    var indexB = stackList.IndexOf(b);
                    return indexB.CompareTo(indexA);        // 나중에 열린 것을 위로 올리도록 한다

                }
                return depthA.CompareTo(depthB);
            });
            
            // Sibling 순서 적용
            foreach (var popup in openPopups)
                popup.transform.SetAsLastSibling();
        }
        
        // 팝업의 Depth 값 가져오기
        public int GetPopupDepth(Type type)
        {
            return popupDepths.TryGetValue(type, out var depth) ? depth : 0;
        }
        
        // 팝업의 Depth 값 가져오기
        public int GetPopupDepth<T>() where T : BasePopup
        {
            return GetPopupDepth(typeof(T));
        }

        // 특정 타입의 팝업 가져오기 (없으면 생성)
        private T GetOrCreatePopup<T>() where T : BasePopup
        {
            var type = typeof(T);
            
            // 이미 생성된 인스턴스가 있으면 반환
            if (popupInstances.TryGetValue(type, out var existingPopup))
                return existingPopup as T;
            
            // 프리팹에서 생성
            if (popupPrefabs.TryGetValue(type, out var prefab))
            {
                var instance = Instantiate(prefab, popupContainer) as T;
                instance.gameObject.SetActive(false);
                instance.Init();
                popupInstances[type] = instance;
                return instance;
            }
            
            return null;
        }

        // 팝업이 닫혔을 때 호출
        public void OnPopupClosed(BasePopup popup)
        {
            // 스택에서 해당 팝업 제거
            if (popupStack.Count > 0 && popupStack.Peek() == popup)
            {
                popupStack.Pop();
            }
            else
            {
                // 스택 중간에 있는 팝업이 닫힌 경우 
                // 스택을 재구성
                var tempList = new List<BasePopup>(popupStack);
                tempList.Remove(popup);
                popupStack.Clear();
                for (int i = tempList.Count - 1; i >= 0; i--)
                    popupStack.Push(tempList[i]);
            }
        }

        // 최상위 팝업 닫기
        public void CloseTopPopup()
        {
            if (popupStack.Count > 0)
            {
                var popup = popupStack.Peek();
                popup.Close();
            }
        }

        // 특정 타입의 팝업 닫기
        public void Close<T>() where T : BasePopup
        {
            var type = typeof(T);
            if (popupInstances.TryGetValue(type, out var popup))
            {
                if (popup.gameObject.activeSelf)
                    popup.Close();
            }
        }

        // 모든 팝업 닫기
        public void CloseAll()
        {
            while (popupStack.Count > 0)
            {
                var popup = popupStack.Pop();
                popup.ForceClose();
            }
        }

        // 특정 타입의 팝업이 열려있는지 확인
        public bool IsOpen<T>() where T : BasePopup
        {
            var type = typeof(T);
            if (popupInstances.TryGetValue(type, out var popup))
                return popup.gameObject.activeSelf;

            return false;
        }

        // 특정 타입의 팝업 인스턴스 가져오기
        public T Get<T>() where T : BasePopup
        {
            var type = typeof(T);
            if (popupInstances.TryGetValue(type, out var popup))
                return popup as T;

            return null;
        }
        
        // 등록된 팝업 타입인지 확인
        public bool IsRegistered<T>() where T : BasePopup
        {
            return popupPrefabs.ContainsKey(typeof(T));
        }

        protected override void Release()
        {
            base.Release();
            CloseAll();
            popupPrefabs.Clear();
            popupInstances.Clear();
            popupDepths.Clear();
        }
    }
}
