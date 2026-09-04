using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Utils;

namespace ZombieWar.VFX
{
    public sealed class VfxService : MonoBehaviour
    {
        private const string LogPrefix = "[VFX]";
        private const int FallbackPrewarm = 4;
        private const int ActiveCapacity = 128;

        [Serializable]
        private struct VfxPoolEntry
        {
            public PooledVfx Prefab;
            public int Prewarm;
        }

        [SerializeField] private VfxPoolEntry[] _entries;
        [SerializeField] private Transform _poolParent;

        private readonly Dictionary<PooledVfx, ComponentPool<PooledVfx>> _pools = new Dictionary<PooledVfx, ComponentPool<PooledVfx>>(32);
        private readonly List<PooledVfx> _active = new List<PooledVfx>(ActiveCapacity);
        private readonly Dictionary<PooledVfx, PooledVfx> _prefabOfInstance = new Dictionary<PooledVfx, PooledVfx>(ActiveCapacity);

        private void Awake()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                VfxPoolEntry entry = _entries[i];
                if (entry.Prefab == null)
                {
                    Debug.LogError($"{LogPrefix} Entry {i} has no prefab assigned.", this);
                    continue;
                }

                _pools[entry.Prefab] = new ComponentPool<PooledVfx>(entry.Prefab, _poolParent, entry.Prewarm);
            }
        }

        public void Play(PooledVfx prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return;
            }

            if (!_pools.TryGetValue(prefab, out ComponentPool<PooledVfx> pool))
            {
                Debug.LogWarning($"{LogPrefix} No prewarmed pool for {prefab.name}; creating one at runtime. Add it to VfxService entries.", this);
                pool = new ComponentPool<PooledVfx>(prefab, _poolParent, FallbackPrewarm);
                _pools[prefab] = pool;
            }

            PooledVfx instance = pool.Get(position, rotation);
            _prefabOfInstance[instance] = prefab;
            _active.Add(instance);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                PooledVfx instance = _active[i];
                if (instance.Tick(deltaTime))
                {
                    continue;
                }

                _pools[_prefabOfInstance[instance]].Release(instance);
                int last = _active.Count - 1;
                _active[i] = _active[last];
                _active.RemoveAt(last);
            }
        }
    }
}
