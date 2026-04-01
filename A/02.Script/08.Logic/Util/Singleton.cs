using UnityEngine;

namespace CAPYBARA
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static bool _applicationIsQuitting;
        private bool _isInitialized = false;
        
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                    return null;

                if (_instance == null)
                {
                    var obj = FindFirstObjectByType<T>();
                    _instance = obj;
                }
                if (_instance == null)
                {
                    var obj = new GameObject(typeof(T).Name);
                    _instance = obj.AddComponent<T>();
                }
                
                // Instance 접근 시 Init이 안 됐으면 호출
                if (_instance != null && !_instance._isInitialized)
                {
                    _instance._isInitialized = true;
                    _instance.Init();
                }
                
                return _instance;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (_instance == null)
            {
                _instance = this as T;
                if (!_isInitialized)
                {
                    _isInitialized = true;
                    _instance.Init();
                }
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            Release();
        }

        protected virtual void Init()
        {
        }

        protected virtual void Release()
        {
        }
    }
}
