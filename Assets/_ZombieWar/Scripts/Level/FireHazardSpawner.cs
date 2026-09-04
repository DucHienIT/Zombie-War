using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Player;
using ZombieWar.Utils;

namespace ZombieWar.Level
{
    public sealed class FireHazardSpawner : MonoBehaviour
    {
        private const string LogPrefix = "[Fire]";
        private const int OverlapBufferSize = 64;

        [SerializeField] private FireHazardDefinitionSO _definition;
        [SerializeField] private Transform _poolParent;
        [SerializeField] private WaveDirector _director;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private Transform _player;
        [SerializeField] private Collider _playerCollider;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private AudioService _audio;
        [SerializeField] private LayerMask _burnMask;

        private readonly Collider[] _overlapBuffer = new Collider[OverlapBufferSize];
        private ComponentPool<FireZone> _pool;
        private List<FireZone> _active;

        private void Awake()
        {
            bool missing = _definition == null || _definition.Prefab == null || _director == null || _flow == null || _player == null
                           || _playerCollider == null || _playerHealth == null || _zombies == null || _audio == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} FireHazardSpawner has an unassigned reference.", this);
                return;
            }

            _pool = new ComponentPool<FireZone>(_definition.Prefab, _poolParent, _definition.PoolPrewarm);
            _active = new List<FireZone>(_definition.PoolPrewarm);
        }

        private void OnEnable()
        {
            _director.OnFireHazardRequested += HandleHazardRequested;
        }

        private void OnDisable()
        {
            _director.OnFireHazardRequested -= HandleHazardRequested;
        }

        private void Update()
        {
            if (_flow.State != GameState.Playing)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                FireZone zone = _active[i];
                if (zone.Tick(deltaTime))
                {
                    ApplyBurnTick(zone.Position);
                }

                if (zone.CurrentPhase != FireZone.Phase.Finished)
                {
                    continue;
                }

                int last = _active.Count - 1;
                _active[i] = _active[last];
                _active.RemoveAt(last);
                _pool.Release(zone);
            }
        }

        private void HandleHazardRequested(float time)
        {
            int count = Random.Range(_definition.MinZonesPerEvent, _definition.MaxZonesPerEvent + 1);
            for (int i = 0; i < count; i++)
            {
                if (!TryFindZonePosition(out Vector3 position))
                {
                    continue;
                }

                FireZone zone = _pool.Get(position, Quaternion.identity);
                zone.Ignite(_definition.Radius, _definition.TelegraphDuration, _definition.ActiveDuration, _definition.TickInterval);
                _active.Add(zone);
                _audio.PlayWorld(_definition.IgniteClip, position);
            }
        }

        private bool TryFindZonePosition(out Vector3 result)
        {
            Vector3 playerPosition = _player.position;
            float minDistanceSqr = _definition.MinDistanceFromPlayer * _definition.MinDistanceFromPlayer;
            for (int attempt = 0; attempt < _definition.PlacementAttempts; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(_definition.MinDistanceFromPlayer, _definition.MaxDistanceFromPlayer);
                Vector3 candidate = playerPosition + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, _definition.NavMeshSampleRadius, NavMesh.AllAreas))
                {
                    continue;
                }

                Vector3 offset = hit.position - playerPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude < minDistanceSqr)
                {
                    continue;
                }

                result = hit.position;
                return true;
            }

            result = default;
            return false;
        }

        private void ApplyBurnTick(Vector3 center)
        {
            int count = Physics.OverlapSphereNonAlloc(center, _definition.Radius, _overlapBuffer, _burnMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = _overlapBuffer[i];
                if (hit == _playerCollider)
                {
                    _playerHealth.TakeDamage(new DamageInfo(_definition.DamagePerTick, center, Vector3.up, 0f, DamageSource.Fire));
                    continue;
                }

                if (_zombies.TryGetZombie(hit, out ZombieController zombie))
                {
                    float damage = _definition.DamagePerTick * _definition.ZombieDamageFraction;
                    zombie.TakeDamage(new DamageInfo(damage, center, Vector3.up, 0f, DamageSource.Fire));
                }
            }
        }
    }
}
