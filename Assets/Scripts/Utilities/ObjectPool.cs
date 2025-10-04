using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Utilities
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly Transform _parent;
        private readonly int _initialSize;

        public ObjectPool(T prefab, int initialSize = 10, Transform parent = null)
        {
            _prefab = prefab;
            _initialSize = initialSize;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private T CreateNewObject()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
            return obj;
        }

        public T Get()
        {
            T obj = _pool.Count > 0 ? _pool.Dequeue() : CreateNewObject();
            obj.gameObject.SetActive(true);
            return obj;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
        }
    }
}
