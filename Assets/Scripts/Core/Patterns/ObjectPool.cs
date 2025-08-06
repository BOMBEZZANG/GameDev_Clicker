using System.Collections.Generic;
using UnityEngine;

namespace GameDevClicker.Core.Patterns
{
    public abstract class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly HashSet<T> _activeItems = new HashSet<T>();
        private Transform _poolContainer;
        private readonly int _initialSize;
        private readonly int _maxSize;

        protected ObjectPool(int initialSize = 10, int maxSize = 100)
        {
            _initialSize = initialSize;
            _maxSize = maxSize;
            Initialize();
        }

        private void Initialize()
        {
            GameObject container = new GameObject($"{typeof(T).Name}Pool");
            _poolContainer = container.transform;

            for (int i = 0; i < _initialSize; i++)
            {
                T item = CreatePooledItem();
                item.gameObject.SetActive(false);
                item.transform.SetParent(_poolContainer);
                _pool.Enqueue(item);
            }
        }

        protected abstract T CreatePooledItem();

        public virtual T Get()
        {
            T item;

            if (_pool.Count > 0)
            {
                item = _pool.Dequeue();
            }
            else
            {
                item = CreatePooledItem();
                item.transform.SetParent(_poolContainer);
            }

            item.gameObject.SetActive(true);
            _activeItems.Add(item);
            OnItemTaken(item);

            return item;
        }

        public virtual void Return(T item)
        {
            if (item == null || !_activeItems.Contains(item))
                return;

            _activeItems.Remove(item);
            OnItemReturned(item);

            item.gameObject.SetActive(false);

            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(item);
            }
            else
            {
                Object.Destroy(item.gameObject);
            }
        }

        public void ReturnAll()
        {
            List<T> itemsToReturn = new List<T>(_activeItems);
            foreach (T item in itemsToReturn)
            {
                Return(item);
            }
        }

        public void Clear()
        {
            ReturnAll();

            while (_pool.Count > 0)
            {
                T item = _pool.Dequeue();
                if (item != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }

            _pool.Clear();
            _activeItems.Clear();
        }

        protected virtual void OnItemTaken(T item) { }
        protected virtual void OnItemReturned(T item) { }

        public int AvailableCount => _pool.Count;
        public int ActiveCount => _activeItems.Count;
        public int TotalCount => _pool.Count + _activeItems.Count;
    }

    public class GenericObjectPool<T> : ObjectPool<T> where T : Component
    {
        private readonly GameObject _prefab;

        public GenericObjectPool(GameObject prefab, int initialSize = 10, int maxSize = 100) 
            : base(initialSize, maxSize)
        {
            _prefab = prefab;
        }

        protected override T CreatePooledItem()
        {
            GameObject instance = Object.Instantiate(_prefab);
            T component = instance.GetComponent<T>();
            
            if (component == null)
            {
                Debug.LogError($"Prefab does not have component of type {typeof(T).Name}");
                Object.Destroy(instance);
                return null;
            }

            return component;
        }
    }
}