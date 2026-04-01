using UnityEngine;

namespace CAPYBARA
{
    public class TempMonoSingleton<T> : MonoBehaviour where T : TempMonoSingleton<T>
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
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
                return _instance;
            }
        }

        private void Awake()
        {
            //DontDestroyOnLoad(gameObject);
            if (_instance == null)
            {
                _instance = this as T;
                _instance.Init();
            }
            else
            {
                Destroy(this.gameObject);
            }
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
