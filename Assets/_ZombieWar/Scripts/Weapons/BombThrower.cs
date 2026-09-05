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
        private float _cooldown;
        private int _appliedChargeBonus;

        public event Action<int> OnChargesChanged;
        public event Action<float> OnCooldownProgress;

        public int Charges { get; private set; }

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

            Charges = _definition.ChargesPerLevel;
            _pool = new ComponentPool<Bomb>(_prefab, _poolParent, _definition.ChargesPerLevel);
            _active = new List<Bomb>(_definition.ChargesPerLevel);
        }

        private void OnEnable()
        {
            _stats.OnChanged += HandleStatsChanged;
        }

        private void OnDisable()
        {
            _stats.OnChanged -= HandleStatsChanged;
        }

        private void Start()
        {
            OnChargesChanged?.Invoke(Charges);
            OnCooldownProgress?.Invoke(1f);
        }

        // An extra charge is handed over the moment the skill is picked, not at the next run.
        private void HandleStatsChanged()
        {
            int bonus = Mathf.RoundToInt(_stats.Additive(StatId.BombCharges));
            int delta = bonus - _appliedChargeBonus;
            if (delta == 0)
            {
                return;
            }

            _appliedChargeBonus = bonus;
            Charges = Mathf.Max(0, Charges + delta);
            OnChargesChanged?.Invoke(Charges);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (_cooldown > 0f)
            {
                _cooldown -= deltaTime;
                OnCooldownProgress?.Invoke(1f - Mathf.Clamp01(_cooldown / _definition.Cooldown));
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Bomb bomb = _active[i];
                if (!bomb.TickFuse(deltaTime))
                {
                    continue;
                }

                Explode(bomb.Position, bomb.BlastRadius);
                int last = _active.Count - 1;
                _active[i] = _active[last];
                _active.RemoveAt(last);
                _pool.Release(bomb);
            }
        }

        public void RequestThrow()
        {
            bool blocked = _flow.State != GameState.Playing || !_health.IsAlive || Charges <= 0 || _cooldown > 0f;
            if (blocked)
            {
                return;
            }

            Vector3 origin = _throwOrigin.position;
            Vector3 target = ResolveTarget();
            Vector3 velocity = ComputeLaunchVelocity(origin, target, _definition.FlightTime);
            float blastRadius = _definition.BlastRadius * _stats.Multiplier(StatId.BombRadius);

            Bomb bomb = _pool.Get(origin, Quaternion.identity);
            bomb.Launch(velocity, _definition.FuseDuration, _definition.TelegraphLead, blastRadius);
            _active.Add(bomb);
            _audio.PlayWorld(_definition.ThrowClip, origin);

            Charges--;
            _cooldown = _definition.Cooldown;
            OnChargesChanged?.Invoke(Charges);
            OnCooldownProgress?.Invoke(0f);
        }

        private Vector3 ResolveTarget()
        {
            Vector3 feet = _transform.position;
            Vector3 offset;
            if (_aim.HasTarget)
            {
                offset = _aim.TargetPosition - feet;
                offset.y = 0f;
                if (offset.sqrMagnitude > _definition.ThrowRange * _definition.ThrowRange)
                {
                    offset = offset.normalized * _definition.ThrowRange;
                }
            }
            else
            {
                Vector3 heading = _motor.NormalizedSpeed > 0f ? _motor.WorldMoveDirection : _motor.Forward;
                heading.y = 0f;
                offset = heading.normalized * _definition.ThrowRange;
            }

            return feet + offset;
        }

        private static Vector3 ComputeLaunchVelocity(Vector3 origin, Vector3 target, float flightTime)
        {
            Vector3 displacement = target - origin;
            return displacement / flightTime - 0.5f * Physics.gravity * flightTime;
        }

        private void Explode(Vector3 center, float radius)
        {
            float damageMultiplier = _stats.Multiplier(StatId.BombDamage);
            int count = Physics.OverlapSphereNonAlloc(center, radius, _blastBuffer, _blastMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                ApplyBlast(center, radius, damageMultiplier, _blastBuffer[i]);
            }

            _vfx.Play(_definition.ExplosionVfx, center, Quaternion.identity);
            _audio.PlayWorld(_definition.ExplosionClip, center);
            _impulseSource.GenerateImpulseWithForce(_definition.CameraImpulse);
        }

        private void ApplyBlast(Vector3 center, float radius, float damageMultiplier, Collider target)
        {
            Vector3 point = target.bounds.center;
            bool occluded = Physics.Linecast(center, point, out RaycastHit hit, _obstacleMask, QueryTriggerInteraction.Ignore)
                            && hit.collider != target;
            if (occluded)
            {
                return;
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
                return;
            }

            Rigidbody body = target.attachedRigidbody;
            if (body != null && !body.isKinematic)
            {
                body.AddExplosionForce(force * _definition.PropForceMultiplier, center, radius, _definition.ExplosionUpwardsModifier, ForceMode.Impulse);
            }
        }
    }
}
