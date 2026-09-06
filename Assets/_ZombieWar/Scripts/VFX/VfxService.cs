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
            [SerializeField] private PooledVfx _prefab;
            [SerializeField] private int _prewarm;

            public PooledVfx Prefab => _prefab;
            public int Prewarm => _prewarm;
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

            Acquire(prefab, position, rotation);
        }

        // Spawns the effect on the anchor and keeps it there: a muzzle flash left at a world
        // position trails half a metre behind a running soldier before it has even faded.
        public void Play(PooledVfx prefab, Transform anchor)
        {
            if (prefab == null || anchor == null)
            {
                return;
            }

            PooledVfx instance = Acquire(prefab, anchor.position, anchor.rotation);
            instance.AttachTo(anchor);
        }

        private PooledVfx Acquire(PooledVfx prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(prefab, out ComponentPool<PooledVfx> pool))
            {
                Debug.LogWarning($"{LogPrefix} No prewarmed pool for {prefab.name}; creating one at runtime. Add it to VfxService entries.", this);
                pool = new ComponentPool<PooledVfx>(prefab, _poolParent, FallbackPrewarm);
                _pools[prefab] = pool;
            }

            PooledVfx instance = pool.Get(position, rotation);
            _prefabOfInstance[instance] = prefab;
            _active.Add(instance);
            return instance;
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

                if (instance.IsAttached)
                {
                    instance.Detach(_poolParent);
                }

                _pools[_prefabOfInstance[instance]].Release(instance);
                _active.RemoveAtSwap(i);
            }
        }
    }
}
