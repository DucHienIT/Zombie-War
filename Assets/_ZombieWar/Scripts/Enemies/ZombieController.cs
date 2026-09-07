using System;
using UnityEngine;
using UnityEngine.AI;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Utils;

namespace ZombieWar.Enemies
{
    public sealed class ZombieController : MonoBehaviour, IDamageable, IPoolable
    {
        private const string LogPrefix = "[Zombie]";
        private const int StaggerSlots = 8;
        private const float FacingThresholdSqr = 0.0001f;
        private const float NavMeshRecoverRadius = 2f;
        // Lateral component of the hit direction beyond which the flinch favours a flank over the head.
        private const float SideHitThreshold = 0.5f;

        [SerializeField] private ZombieDefinitionSO _definition;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Collider _collider;
        [SerializeField] private ZombieAnimationPresenter _animation;
        [SerializeField] private ZombieMaterialFx _materialFx;
        // Chest-height point used by the player for line-of-sight checks.
        [SerializeField] private Transform _aimPoint;

        private Transform _transform;
        private ZombieManager _owner;
        private IDamageable _player;
        private FeedbackProfileSO _feedback;
        private int _aliveLayer;
        private int _corpseLayer;
        private float _hp;
        private ZombieState _state;
        private float _stateTimer;
        private float _attackCooldown;
        private bool _attackLanded;
        private float _destinationInterval;
        private float _destinationTimer;
        private float _staggerOffset;
        private float _pathFailTimer;
        private float _dissolveTimer;

        public event Action<ZombieController> OnDied;
        public event Action<ZombieController> OnDespawnReady;

        public ZombieDefinitionSO Definition => _definition;
        public Collider Collider => _collider;
        public Transform AimPoint => _aimPoint;
        public Vector3 Position => _transform.position;
        public ZombieState State => _state;
        public bool IsAlive => _hp > 0f;
        public float HealthNormalized => _definition.MaxHp > 0f ? Mathf.Clamp01(_hp / _definition.MaxHp) : 0f;
        public bool IsTargetable => IsAlive && _state != ZombieState.Spawning;

        private void Awake()
        {
            _transform = transform;
            _aliveLayer = gameObject.layer;
            bool missing = _definition == null || _agent == null || _rigidbody == null || _collider == null
                           || _animation == null || _materialFx == null || _aimPoint == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} {name} has an unassigned reference.", this);
            }
        }

        public void Initialize(ZombieManager owner, IDamageable player, FeedbackProfileSO feedback, int instanceIndex, int corpseLayer)
        {
            _owner = owner;
            _player = player;
            _feedback = feedback;
            _corpseLayer = corpseLayer;
            _destinationInterval = 1f / _definition.DestinationUpdateHz;
            _staggerOffset = (instanceIndex % StaggerSlots) / (float)StaggerSlots * _destinationInterval;
        }

        public void OnSpawned()
        {
            _hp = _definition.MaxHp;
            _state = ZombieState.Spawning;
            _stateTimer = _definition.SpawnDelay;
            _attackCooldown = 0f;
            _attackLanded = false;
            _pathFailTimer = 0f;
            _dissolveTimer = 0f;
            _destinationTimer = _staggerOffset;

            RestoreBody();
            _collider.enabled = false;
            _agent.enabled = true;
            _agent.Warp(_transform.position);
            _agent.speed = _definition.MoveSpeed;
            _agent.isStopped = true;

            _animation.ResetAll();
            _materialFx.ResetAll();
            _materialFx.SetDissolve(1f);
        }

        public void OnDespawned()
        {
            _agent.enabled = false;
            _collider.enabled = false;
            RestoreBody();
        }

        public void Tick(float deltaTime, Vector3 playerPosition)
        {
            _materialFx.Tick(deltaTime);
            switch (_state)
            {
                case ZombieState.Spawning:
                    TickSpawning(deltaTime);
                    break;
                case ZombieState.Chase:
                    TickChase(deltaTime, playerPosition);
                    break;
                case ZombieState.Attack:
                    TickAttack(deltaTime, playerPosition);
                    break;
                case ZombieState.HitStun:
                    TickHitStun(deltaTime);
                    break;
                case ZombieState.Knockback:
                    TickKnockback(deltaTime);
                    break;
                case ZombieState.Dying:
                    TickDying(deltaTime);
                    break;
            }
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive)
            {
                return;
            }

            _hp -= info.Amount;
            _owner.ReportDamage(this, info.Amount);
            _materialFx.FlashHit(_feedback.ZombieHitFlashDuration);
            _owner.PlayVoice(_definition.HitClips, _transform.position);

            float force = info.Force * _definition.KnockbackForceMultiplier;
            bool physical = force >= _definition.PhysicsKnockbackThreshold;
            if (_hp <= 0f)
            {
                Die(info.Direction, physical ? force : 0f);
                return;
            }

            HitSide side = HitSideOf(info.Direction);
            if (physical)
            {
                EnterKnockback(info.Direction, force, side);
            }
            else if (_state != ZombieState.Knockback)
            {
                EnterHitStun(info.Direction, info.Force, side);
            }
        }

        // A bullet travelling towards the zombie's right enters through its left flank.
        private HitSide HitSideOf(Vector3 direction)
        {
            float lateral = Vector3.Dot(_transform.right, direction);
            if (lateral > SideHitThreshold)
            {
                return HitSide.Left;
            }

            return lateral < -SideHitThreshold ? HitSide.Right : HitSide.Front;
        }

        private void TickSpawning(float deltaTime)
        {
            _stateTimer -= deltaTime;
            _materialFx.SetDissolve(Mathf.Clamp01(_stateTimer / _definition.SpawnDelay));
            if (_stateTimer > 0f)
            {
                return;
            }

            _materialFx.SetDissolve(0f);
            _collider.enabled = true;
            EnterChase();
        }

        private void EnterChase()
        {
            _state = ZombieState.Chase;
            _agent.isStopped = false;
        }

        private void TickChase(float deltaTime, Vector3 playerPosition)
        {
            _destinationTimer -= deltaTime;
            if (_destinationTimer <= 0f)
            {
                _destinationTimer += _destinationInterval;
                _agent.SetDestination(playerPosition);
            }

            TrackPathFailure(deltaTime);
            // The locomotion blend tree is keyed in m/s so every archetype picks the clip that matches its real pace.
            _animation.SetSpeed(_agent.velocity.magnitude, deltaTime);

            if (IsWithin(playerPosition, _definition.AttackRange))
            {
                EnterAttack();
            }
        }

        private void TrackPathFailure(float deltaTime)
        {
            bool pathBroken = _agent.pathStatus == NavMeshPathStatus.PathInvalid
                              || (_agent.hasPath && _agent.pathStatus == NavMeshPathStatus.PathPartial);
            if (!pathBroken)
            {
                _pathFailTimer = 0f;
                return;
            }

            _pathFailTimer += deltaTime;
            if (_pathFailTimer >= _definition.RepathTimeout)
            {
                // Unreachable for too long: give the slot back so the director respawns it off-screen.
                _hp = 0f;
                OnDespawnReady?.Invoke(this);
            }
        }

        private void EnterAttack()
        {
            _state = ZombieState.Attack;
            _stateTimer = _definition.AttackWindup;
            _attackLanded = false;
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _animation.SetSpeed(0f, 0f);
            _animation.TriggerAttack();
            _owner.PlayVoice(_definition.AttackClips, _transform.position);
        }

        private void TickAttack(float deltaTime, Vector3 playerPosition)
        {
            FaceTowards(playerPosition, deltaTime);

            if (!_attackLanded)
            {
                _stateTimer -= deltaTime;
                if (_stateTimer > 0f)
                {
                    return;
                }

                LandAttack(playerPosition);
                return;
            }

            _attackCooldown -= deltaTime;
            if (_attackCooldown > 0f)
            {
                return;
            }

            if (IsWithin(playerPosition, _definition.AttackRange))
            {
                EnterAttack();
            }
            else
            {
                EnterChase();
            }
        }

        private void LandAttack(Vector3 playerPosition)
        {
            _attackLanded = true;
            _attackCooldown = Mathf.Max(0f, _definition.AttackCooldown - _definition.AttackWindup);

            float reach = _definition.AttackRange + _definition.AttackReachTolerance + _definition.AttackAreaRadius;
            if (!IsWithin(playerPosition, reach))
            {
                return;
            }

            Vector3 direction = playerPosition - _transform.position;
            direction.y = 0f;
            var info = new DamageInfo(_definition.AttackDamage, playerPosition, direction.normalized, 0f, DamageSource.Melee);
            _player.TakeDamage(info);
        }

        private void EnterHitStun(Vector3 direction, float force, HitSide side)
        {
            _state = ZombieState.HitStun;
            _stateTimer = _definition.HitStunDuration;
            _agent.isStopped = true;
            _animation.TriggerHit(side);

            Vector3 nudge = direction;
            nudge.y = 0f;
            _agent.Move(nudge.normalized * (_definition.BulletNudgeDistance * force));
        }

        private void TickHitStun(float deltaTime)
        {
            _stateTimer -= deltaTime;
            if (_stateTimer <= 0f)
            {
                EnterChase();
            }
        }

        private void EnterKnockback(Vector3 direction, float force, HitSide side)
        {
            if (_state != ZombieState.Knockback)
            {
                _agent.enabled = false;
                _rigidbody.isKinematic = false;
                _animation.SetKnockback(true);
            }

            _state = ZombieState.Knockback;
            _stateTimer = _definition.KnockbackMaxDuration;
            _rigidbody.AddForce(KnockbackImpulse(direction) * force, ForceMode.Impulse);
            _animation.TriggerHit(side);
        }

        private void TickKnockback(float deltaTime)
        {
            _stateTimer -= deltaTime;
            float elapsed = _definition.KnockbackMaxDuration - _stateTimer;
            float restSpeedSqr = _definition.KnockbackRestSpeed * _definition.KnockbackRestSpeed;
            bool rested = elapsed >= _definition.KnockbackMinDuration && _rigidbody.velocity.sqrMagnitude <= restSpeedSqr;
            if (rested || _stateTimer <= 0f)
            {
                ExitKnockback();
            }
        }

        private void ExitKnockback()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            _animation.SetKnockback(false);

            bool onNavMesh = NavMesh.SamplePosition(_transform.position, out NavMeshHit hit, NavMeshRecoverRadius, NavMesh.AllAreas);
            if (!onNavMesh)
            {
                _hp = 0f;
                OnDespawnReady?.Invoke(this);
                return;
            }

            _transform.position = hit.position;
            _agent.enabled = true;
            EnterChase();
        }

        private void Die(Vector3 direction, float launchForce)
        {
            _hp = 0f;
            _state = ZombieState.Dying;
            _stateTimer = _feedback.DeathPoseDuration;
            _dissolveTimer = 0f;
            if (_agent.enabled)
            {
                _agent.isStopped = true;
                _agent.enabled = false;
            }

            _animation.SetKnockback(false);
            _animation.TriggerDeath();
            _owner.PlayVoice(_definition.DeathClips, _transform.position);

            if (launchForce > 0f)
            {
                LaunchCorpse(direction, launchForce);
            }
            else
            {
                SettleBody();
            }

            OnDied?.Invoke(this);
        }

        // A lethal blast keeps the body dynamic so it flies with the shockwave. The collider stays
        // on so the corpse lands instead of sinking, but on the corpse layer so nothing targets it.
        private void LaunchCorpse(Vector3 direction, float force)
        {
            gameObject.layer = _corpseLayer;
            _collider.enabled = true;
            _rigidbody.isKinematic = false;
            _rigidbody.AddForce(KnockbackImpulse(direction) * force, ForceMode.Impulse);
        }

        private void SettleBody()
        {
            _collider.enabled = false;
            // Velocity can only be cleared while the body is still dynamic (mid-knockback deaths).
            if (!_rigidbody.isKinematic)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }
        }

        private void RestoreBody()
        {
            if (!_rigidbody.isKinematic)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }

            gameObject.layer = _aliveLayer;
        }

        private Vector3 KnockbackImpulse(Vector3 direction)
        {
            Vector3 impulse = direction;
            impulse.y = 0f;
            impulse = impulse.normalized + Vector3.up * _definition.KnockbackUpwardsFactor;
            return impulse.normalized;
        }

        private void TickDying(float deltaTime)
        {
            if (_stateTimer > 0f)
            {
                _stateTimer -= deltaTime;
                return;
            }

            _dissolveTimer += deltaTime;
            float amount = Mathf.Clamp01(_dissolveTimer / _feedback.DissolveDuration);
            _materialFx.SetDissolve(amount);
            if (amount >= 1f)
            {
                OnDespawnReady?.Invoke(this);
            }
        }

        private void FaceTowards(Vector3 target, float deltaTime)
        {
            Vector3 direction = target - _transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= FacingThresholdSqr)
            {
                return;
            }

            Quaternion look = Quaternion.LookRotation(direction);
            _transform.rotation = Quaternion.RotateTowards(_transform.rotation, look, _agent.angularSpeed * deltaTime);
        }

        private bool IsWithin(Vector3 target, float range)
        {
            Vector3 offset = target - _transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= range * range;
        }
    }
}
