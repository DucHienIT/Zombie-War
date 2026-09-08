using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using ZombieWar.Audio;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Player;
using ZombieWar.Utils;
using ZombieWar.VFX;

namespace ZombieWar.Weapons
{
    // Throws and detonates bombs. It has no pacing of its own any more: the Auto Bomb skill
    // decides when a volley goes out, this only decides where it lands and what it does.
    public sealed class BombThrower : MonoBehaviour
    {
        private const string LogPrefix = "[Bomb]";
        private const int BlastBufferSize = 96;

        [SerializeField] private BombDefinitionSO _definition;
        [SerializeField] private Bomb _prefab;
        [SerializeField] private Transform _poolParent;
        [SerializeField] private Transform _throwOrigin;
        [SerializeField] private PlayerAim _aim;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private PlayerStatSheet _stats;
        [SerializeField] private GameFlowController _flow;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private VfxService _vfx;
        [SerializeField] private AudioService _audio;
        [SerializeField] private CinemachineImpulseSource _impulseSource;
        [SerializeField] private LayerMask _blastMask;
        [SerializeField] private LayerMask _obstacleMask;

        private readonly Collider[] _blastBuffer = new Collider[BlastBufferSize];
        private ComponentPool<Bomb> _pool;
        private List<Bomb> _active;
        private Transform _transform;

        // Blast centre and how many zombies it reached; feel systems key off the count.
        public event Action<Vector3, int> OnExploded;

        private void Awake()
        {
            _transform = transform;
            bool missing = _definition == null || _prefab == null || _throwOrigin == null || _aim == null || _motor == null || _health == null
                           || _stats == null || _flow == null || _zombies == null || _vfx == null || _audio == null || _impulseSource == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} BombThrower has an unassigned reference.", this);
                return;
            }

            _pool = new ComponentPool<Bomb>(_prefab, _poolParent, _definition.PoolSize);
            _active = new List<Bomb>(_definition.PoolSize);
        }

        private void OnEnable()
        {
            _flow.OnRunCleared += DespawnAll;
        }

        private void OnDisable()
        {
            _flow.OnRunCleared -= DespawnAll;
        }

        // The run is over: a bomb still in the air belongs to it and must never land on the next one.
        private void DespawnAll()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _pool.Release(_active[i]);
            }

            _active.Clear();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Bomb bomb = _active[i];
                if (!bomb.TickFuse(deltaTime))
                {
                    continue;
                }

                Explode(bomb.Position, bomb.BlastRadius);
                _active.RemoveAtSwap(i);
                _pool.Release(bomb);
            }
        }

        // One volley per ability trigger. The only refusal is the run not being playable;
        // how often this is called is the skill's business.
        public void ThrowVolley()
        {
            if (_flow.State != GameState.Playing || !_health.IsAlive)
            {
                return;
            }

            Vector3 origin = _throwOrigin.position;
            Vector3 target = ThrowSolver.GroundTarget(_transform.position, _aim, _motor, _definition.ThrowRange);
            float blastRadius = _definition.BlastRadius * _stats.Multiplier(StatId.BombRadius);
            int count = 1 + Mathf.RoundToInt(_stats.Additive(StatId.ExtraBombsPerVolley));
            for (int i = 0; i < count; i++)
            {
                Vector3 landing = i == 0 ? target : target + Scatter(_definition.VolleyScatter);
                Launch(origin, landing, blastRadius);
            }

            _audio.PlayWorld(_definition.ThrowClip, origin);
        }

        private void Launch(Vector3 origin, Vector3 target, float blastRadius)
        {
            Vector3 velocity = ThrowSolver.LaunchVelocity(origin, target, _definition.FlightTime);
            Bomb bomb = _pool.Get(origin, Quaternion.identity);
            bomb.Launch(velocity, _definition.FuseDuration, blastRadius);
            _active.Add(bomb);
        }

        private static Vector3 Scatter(float radius)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
            return new Vector3(offset.x, 0f, offset.y);
        }

        private void Explode(Vector3 center, float radius)
        {
            float damageMultiplier = _stats.Multiplier(StatId.BombDamage);
            int count = Physics.OverlapSphereNonAlloc(center, radius, _blastBuffer, _blastMask, QueryTriggerInteraction.Ignore);
            int zombiesHit = 0;
            for (int i = 0; i < count; i++)
            {
                if (ApplyBlast(center, radius, damageMultiplier, _blastBuffer[i]))
                {
                    zombiesHit++;
                }
            }

            _vfx.Play(_definition.ExplosionVfx, center, Quaternion.identity);
            _audio.PlayWorld(_definition.ExplosionClip, center);
            _impulseSource.GenerateImpulseWithForce(_definition.CameraImpulse);
            OnExploded?.Invoke(center, zombiesHit);
        }

        // True when the target was a zombie that took the blast.
        private bool ApplyBlast(Vector3 center, float radius, float damageMultiplier, Collider target)
        {
            Vector3 point = target.bounds.center;
            bool occluded = Physics.Linecast(center, point, out RaycastHit hit, _obstacleMask, QueryTriggerInteraction.Ignore)
                            && hit.collider != target;
            if (occluded)
            {
                return false;
            }

            Vector3 toTarget = point - center;
            float distance = toTarget.magnitude;
            float falloff = Mathf.Clamp01(distance / radius);
            float force = Mathf.Lerp(_definition.KnockbackAtCenter, _definition.KnockbackAtEdge, falloff);
            Vector3 direction = distance > 0f ? toTarget / distance : Vector3.up;

            if (_zombies.TryGetZombie(target, out ZombieController zombie))
            {
                float damage = Mathf.Lerp(_definition.DamageAtCenter, _definition.DamageAtEdge, falloff) * damageMultiplier;
                zombie.TakeDamage(new DamageInfo(damage, center, direction, force, DamageSource.Bomb));
                return true;
            }

            Rigidbody body = target.attachedRigidbody;
            if (body != null && !body.isKinematic)
            {
                body.AddExplosionForce(force * _definition.PropForceMultiplier, center, radius, _definition.ExplosionUpwardsModifier, ForceMode.Impulse);
            }

            return false;
        }
    }
}
