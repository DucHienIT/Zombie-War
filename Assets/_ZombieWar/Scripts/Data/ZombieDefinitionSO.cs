using UnityEngine;
using ZombieWar.Enemies;
using ZombieWar.VFX;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Zombie Definition", fileName = "ZombieDefinition")]
    public sealed class ZombieDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private ZombieController _prefab;
        [SerializeField] private int _poolPrewarm = 16;

        [Header("Stats")]
        [SerializeField] private float _maxHp = 48f;
        [SerializeField] private float _moveSpeed = 2.3f;
        [SerializeField] private int _scoreReward = 10;

        [Header("Attack")]
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _attackRange = 1.4f;
        [SerializeField] private float _attackWindup = 0.35f;
        // Extra reach granted when the wind-up lands so a player stepping back a little still gets hit.
        [SerializeField] private float _attackReachTolerance = 0.4f;
        // Zero means a single-target melee hit; Giant slam uses 2.8.
        [SerializeField] private float _attackAreaRadius;

        [Header("Navigation")]
        [SerializeField] private float _destinationUpdateHz = 4f;
        [SerializeField] private float _spawnDelay = 0.25f;
        [SerializeField] private float _repathTimeout = 1f;

        [Header("Reactions")]
        [SerializeField] private float _hitStunDuration = 0.14f;
        [SerializeField] private float _knockbackForceMultiplier = 1f;
        // Hits with force at or above this go through the Rigidbody knockback state instead of a nudge.
        [SerializeField] private float _physicsKnockbackThreshold = 4f;
        [SerializeField] private float _knockbackUpwardsFactor = 0.35f;
        [SerializeField] private float _knockbackMinDuration = 0.35f;
        [SerializeField] private float _knockbackMaxDuration = 0.55f;
        [SerializeField] private float _knockbackRestSpeed = 0.4f;
        [SerializeField] private float _bulletNudgeDistance = 0.12f;

        [Header("Feedback")]
        [SerializeField] private PooledVfx _deathVfx;
        [SerializeField] private AudioClip[] _attackClips;
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip[] _deathClips;
        [SerializeField] private AudioClip _spawnClip;

        public string Id => _id;
        public string DisplayName => _displayName;
        public ZombieController Prefab => _prefab;
        public int PoolPrewarm => _poolPrewarm;
        public float MaxHp => _maxHp;
        public float MoveSpeed => _moveSpeed;
        public int ScoreReward => _scoreReward;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackRange => _attackRange;
        public float AttackWindup => _attackWindup;
        public float AttackReachTolerance => _attackReachTolerance;
        public float AttackAreaRadius => _attackAreaRadius;
        public float DestinationUpdateHz => _destinationUpdateHz;
        public float SpawnDelay => _spawnDelay;
        public float RepathTimeout => _repathTimeout;
        public float HitStunDuration => _hitStunDuration;
        public float KnockbackForceMultiplier => _knockbackForceMultiplier;
        public float PhysicsKnockbackThreshold => _physicsKnockbackThreshold;
        public float KnockbackUpwardsFactor => _knockbackUpwardsFactor;
        public float KnockbackMinDuration => _knockbackMinDuration;
        public float KnockbackMaxDuration => _knockbackMaxDuration;
        public float KnockbackRestSpeed => _knockbackRestSpeed;
        public float BulletNudgeDistance => _bulletNudgeDistance;
        public PooledVfx DeathVfx => _deathVfx;
        public AudioClip[] AttackClips => _attackClips;
        public AudioClip[] HitClips => _hitClips;
        public AudioClip[] DeathClips => _deathClips;
        public AudioClip SpawnClip => _spawnClip;
    }
}
