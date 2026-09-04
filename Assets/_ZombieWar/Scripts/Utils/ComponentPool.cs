using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZombieWar.Utils
{
    public sealed class ComponentPool<T> where T : Component, IPoolable
    {
        private const string LogPrefix = "[Pool]";

        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Action<T> _onCreated;
        private readonly Stack<T> _inactive;
        private int _totalCreated;

        public ComponentPool(T prefab, Transform parent, int prewarmCount, Action<T> onCreated = null)
        {
            _prefab = prefab;
            _parent = parent;
            _onCreated = onCreated;
            _inactive = new Stack<T>(prewarmCount);
            for (int i = 0; i < prewarmCount; i++)
            {
                _inactive.Push(CreateInstance());
            }
        }

        public int TotalCreated => _totalCreated;

        public T Get(Vector3 position, Quaternion rotation)
        {
            T instance;
            if (_inactive.Count > 0)
            {
                instance = _inactive.Pop();
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} Pool for {_prefab.name} exhausted at {_totalCreated} instances; expanding. Raise the prewarm size.");
                instance = CreateInstance();
            }

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            instance.OnSpawned();
            return instance;
        }

        public void Release(T instance)
        {
            instance.OnDespawned();
            instance.gameObject.SetActive(false);
            _inactive.Push(instance);
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            _totalCreated++;
            _onCreated?.Invoke(instance);
            return instance;
        }
    }
}
