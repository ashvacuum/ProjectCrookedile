using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Crookedile.Data.Database
{
    public abstract class GameDatabase<T> : ScriptableObject, IDatabase<T> where T : ScriptableObject
    {
        [SerializeField] protected List<T> _items = new List<T>();

        protected Dictionary<string, T> _itemDictionary;
        protected bool _isInitialized = false;

        public int Count => _items.Count;

        protected virtual void OnEnable()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            if (_isInitialized) return;

            _itemDictionary = new Dictionary<string, T>();

            foreach (var item in _items)
            {
                if (item != null)
                {
                    string id = GetItemID(item);
                    if (!string.IsNullOrEmpty(id))
                    {
                        _itemDictionary[id] = item;
                    }
                }
            }

            _isInitialized = true;
        }

        protected abstract string GetItemID(T item);

        public virtual T GetByID(string id)
        {
            if (!_isInitialized) Initialize();

            if (_itemDictionary.TryGetValue(id, out T item))
            {
                return item;
            }

            return null;
        }

        public virtual List<T> GetAll()
        {
            return new List<T>(_items);
        }

        public virtual bool Contains(string id)
        {
            if (!_isInitialized) Initialize();
            return _itemDictionary.ContainsKey(id);
        }

        public virtual List<T> FindAll(System.Predicate<T> predicate)
        {
            return _items.FindAll(predicate);
        }

        public virtual T Find(System.Predicate<T> predicate)
        {
            return _items.Find(predicate);
        }

        public virtual T GetRandom()
        {
            if (_items.Count == 0) return null;
            return _items[Random.Range(0, _items.Count)];
        }

        public virtual List<T> GetRandom(int count)
        {
            if (_items.Count == 0) return new List<T>();

            List<T> shuffled = new List<T>(_items);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                T temp = shuffled[i];
                shuffled[i] = shuffled[randomIndex];
                shuffled[randomIndex] = temp;
            }

            return shuffled.Take(Mathf.Min(count, shuffled.Count)).ToList();
        }

#if UNITY_EDITOR
        public virtual void RefreshDatabase()
        {
            _items.Clear();
            _itemDictionary?.Clear();
            _isInitialized = false;

            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                T item = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                if (item != null && !_items.Contains(item))
                {
                    _items.Add(item);
                }
            }

            Initialize();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"Database refreshed: {_items.Count} items found");
        }
#endif
    }
}
