using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Player;
using ZombieWar.Utils;
using ZombieWar.VFX;

namespace ZombieWar.Enemies
{
    public sealed class ZombieManager : MonoBehaviour
    {
        private const string LogPrefix = "[Zombie]";
        private const int ActiveCapacity = 64;

        [SerializeField] private ZombieDefinitionSO[] _definitions;
        [SerializeField] private Transform _poolParent;
        [SerializeField] private PlayerHealth _player;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private FeedbackProfileSO _feedback;
        [SerializeField] private AudioService _audio;
        [SerializeField] private VfxService _vfx;

        [Header("Voice Limit")]
        // Minimum spacing between zombie voice clips so a crowd never becomes a noise wall.
        [SerializeField] private float _voiceInterval = 0.12f;

        [Header("Corpse")]
        // Layer a body launched by a lethal blast moves to while it dissolves. Bullets, aim and
        // later blasts leave it out of their masks so the flying corpse never soaks a hit.
        [SerializeField] private string _corpseLayerName = "Corpse";

        private readonly Dictionary<ZombieDefinitionSO, ComponentPool<ZombieController>> _pools = new Dictionary<ZombieDefinitionSO, ComponentPool<ZombieController>>(4);
        private readonly Dictionary<Collider, ZombieController> _byCollider = new Dictionary<Collider, ZombieController>(ActiveCapacity * 2);
        private readonly List<ZombieController> _active = new List<ZombieController>(ActiveCapacity);
        private readonly List<ZombieController> _despawnQueue = new List<ZombieController>(16);

        private Action<ZombieController> _onCreated;
        private Action<ZombieController> _onDied;
        private Action<ZombieController> _onDespawnReady;
        private int _createdCount;
        private int _corpseLayer;
        private float _lastVoiceTime;

        public event Action<ZombieController> OnZombieKilled;

        public int ActiveCount => _active.Count;

        private void Awake()
        {
            bool missing = _definitions == null || _definitions.Length == 0 || _player == null || _playerTransform == null
                           || _flow == null || _feedback == null || _audio == null || _vfx == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} ZombieManager has an unassigned reference.", this);
                return;
            }

            _corpseLayer = LayerMask.NameToLayer(_corpseLayerName);
            if (_corpseLayer < 0)
            {
                Debug.LogError($"{LogPrefix} Layer {_corpseLayerName} does not exist; add it in Project Settings > Tags and Layers.", this);
            }

            _onCreated = HandleCreated;
            _onDied = HandleDied;
            _onDespawnReady = HandleDespawnReady;
        }

        private void OnEnable()
        {
            _flow.OnRunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            _flow.OnRunStarted -= HandleRunStarted;
        }

        // Pools are filled once the map exists: a NavMeshAgent that wakes up with no NavMesh
        // under it logs an error and never attaches, and the map carries the baked data.
        private void HandleRunStarted(LevelDefinitionSO level)
        {
            if (_pools.Count > 0)
            {
                return;
            }

            for (int i = 0; i < _definitions.Length; i++)
            {
                ZombieDefinitionSO definition = _definitions[i];
                _pools[definition] = new ComponentPool<ZombieController>(definition.Prefab, _poolParent, definition.PoolPrewarm, _onCreated);
            }
        }

        public ZombieController Spawn(ZombieDefinitionSO definition, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(definition, out ComponentPool<ZombieController> pool))
            {
                Debug.LogError($"{LogPrefix} {definition.name} is not registered in this level's ZombieManager.", this);
                return null;
            }

            ZombieController zombie = pool.Get(position, rotation);
            _active.Add(zombie);
            _audio.PlayWorld(definition.SpawnClip, position);
            return zombie;
        }

        public bool TryGetZombie(Collider collider, out ZombieController zombie)
        {
            return _byCollider.TryGetValue(collider, out zombie);
        }

        public void PlayVoice(AudioClip[] clips, Vector3 position)
        {
            float now = Time.time;
            if (now - _lastVoiceTime < _voiceInterval)
            {
                return;
            }

            _lastVoiceTime = now;
            _audio.PlayWorldRandom(clips, position);
        }

        public void DespawnAll()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                Release(_active[i]);
            }

            _active.Clear();
            _despawnQueue.Clear();
        }

        private void Update()
        {
            GameState state = _flow.State;
            if (state != GameState.Playing && state != GameState.Lost)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            Vector3 playerPosition = _playerTransform.position;
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].Tick(deltaTime, playerPosition);
            }

            FlushDespawnQueue();
        }

        private void FlushDespawnQueue()
        {
            for (int i = 0; i < _despawnQueue.Count; i++)
            {
                ZombieController zombie = _despawnQueue[i];
                int index = _active.IndexOf(zombie);
                if (index < 0)
                {
                    continue;
                }

                _active.RemoveAtSwap(index);
                Release(zombie);
            }

            _despawnQueue.Clear();
        }

        private void Release(ZombieController zombie)
        {
            _pools[zombie.Definition].Release(zombie);
        }

        private void HandleCreated(ZombieController zombie)
        {
            zombie.Initialize(this, _player, _feedback, _createdCount++, _corpseLayer);
            zombie.OnDied += _onDied;
            zombie.OnDespawnReady += _onDespawnReady;
            _byCollider[zombie.Collider] = zombie;
        }

        // The burst belongs to the killing blow, not to the despawn a second later.
        private void HandleDied(ZombieController zombie)
        {
            _vfx.Play(zombie.Definition.DeathVfx, zombie.Position, Quaternion.identity);
            OnZombieKilled?.Invoke(zombie);
        }

        private void HandleDespawnReady(ZombieController zombie)
        {
            _despawnQueue.Add(zombie);
        }
    }
}
