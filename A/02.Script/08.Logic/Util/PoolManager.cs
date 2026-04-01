using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;


namespace CAPYBARA.Core
{
    public class Poolable : MonoBehaviour
    {
        public bool isUsing;
    }
    public static class PoolManager
    {
        #region Pool
        class Pool
        {
            public GameObject Original { get; private set; }
            public Transform Root { get; set; }

            Queue<Poolable> _poolStack = new Queue<Poolable>();


            public void Init(GameObject original, int count = 5)
            {
                Original = original;
                Root = new GameObject().transform;
                Root.name = $"{original.name}_Root";

                for (int i = 0; i < count; i++)
                    Push(Create());
            }

            Poolable Create()
            {
                GameObject go = Object.Instantiate<GameObject>(Original);
                go.name = Original.name;
                return go.GetOrAddComponent<Poolable>();
            }

            public void Push(Poolable poolable)
            {
                if (poolable == null)
                    return;

                //poolable.transform.parent = Root;
                poolable.transform.SetParent(Root, false);
                poolable.gameObject.SetActive(false);
                poolable.CancelInvoke();
                poolable.isUsing = false;

                _poolStack.Enqueue(poolable);
            }

            public Poolable Pop(Transform parent, Vector2 spawnPos)
            {
                Poolable poolable;

                if (_poolStack.Count > 0)
                {
                    while (true)
                    {
                        poolable = _poolStack.Dequeue();
                        if (poolable.gameObject.activeInHierarchy)
                        {
                            poolable.gameObject.SetActive(false);
                            _poolStack.Enqueue(poolable);
                        }
                        else
                        {
                            break;
                        }
                    }

                }
                else
                {
                    poolable = Create();
                }



                poolable.transform.position = spawnPos;
                poolable.transform.up = Vector2.up;
                poolable.gameObject.SetActive(true);
                poolable.transform.SetParent(parent, false);
                poolable.isUsing = true;

                foreach (var tr in poolable.GetComponentsInChildren<TrailRenderer>())
                {
                    if (tr != null)
                        tr.Clear();
                }
                return poolable;
            }

        }
        #endregion



        static Dictionary<string, Pool> _pool = new Dictionary<string, Pool>();
        static Transform _root;



        public static void Init()
        {
            if (_root == null)
            {
                _root = new GameObject { name = "@Pool_Root" }.transform;
            }
        }



        public static void CreatePool(GameObject original, int count = 5)
        {
            Pool pool = new Pool();
            pool.Init(original, count);

            pool.Root.SetParent(_root, false);

            _pool.Add(original.name, pool);
        }
        public static void CreatePool(GameObject original,Transform parent, int count = 5)
        {
            Pool pool = new Pool();
            pool.Init(original, count);

            pool.Root.SetParent(parent, false);

            _pool.Add(original.name, pool);
        }

        public static void Push(Poolable poolable)
        {
            string name = poolable.gameObject.name;
            if (_pool.ContainsKey(name) == false)
            {
                GameObject.Destroy(poolable.gameObject);
                return;
            }

            _pool[name].Push(poolable);
        }

        public static Poolable Pop(GameObject original, Transform parent, Vector2 spawnPos, int count = 5)
        {
            try
            {
                if (_pool.ContainsKey(original.name) == false)
                    CreatePool(original, count);


                return _pool[original.name].Pop(parent, spawnPos);
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public static T Pop<T>(T original, Transform parent, Vector2 spawnPos, int count = 5) where T : Poolable
        {
            try
            {
                if (_pool.ContainsKey(original.name) == false)
                    CreatePool(original.gameObject, count);


                return _pool[original.name].Pop(parent, spawnPos) as T;
            }
            catch (Exception e)
            {

                throw e;
            }
        }



        public static GameObject GetOriginal(string name)
        {
            if (_pool.ContainsKey(name) == false)
                return null;
            return _pool[name].Original;
        }


        public static void Clear()
        {
            if (_root != null)
            {
                foreach (Transform child in _root)
                    GameObject.Destroy(child.gameObject);    
            }
            

            _pool.Clear();
        }
    }


}