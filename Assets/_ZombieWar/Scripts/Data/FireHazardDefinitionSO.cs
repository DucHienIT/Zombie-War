using UnityEngine;
using ZombieWar.Level;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Fire Hazard Definition", fileName = "FireHazardDefinition")]
    public sealed class FireHazardDefinitionSO : ScriptableObject
    {
        [Header("Zone")]
        [SerializeField] private FireZone _prefab;
        [SerializeField] private int _poolPrewarm = 6;
        [SerializeField] private float _radius = 2.5f;
        [SerializeField] private float _telegraphDuration = 1.25f;
        [SerializeField] private float _activeDuration = 8f;

        [Header("Damage")]
        [SerializeField] private float _tickInterval = 0.5f;
        [SerializeField] private float _damagePerTick = 5f;
        [SerializeField] private float _zombieDamageFraction = 0.35f;

        [Header("Placement")]
        [SerializeField] private int _minZonesPerEvent = 2;
        [SerializeField] private int _maxZonesPerEvent = 3;
        [SerializeField] private float _minDistanceFromPlayer = 4f;
        [SerializeField] private float _maxDistanceFromPlayer = 9f;
        [SerializeField] private float _navMeshSampleRadius = 2f;
        [SerializeField] private int _placementAttempts = 10;

        [Header("Feedback")]
        [SerializeField] private AudioClip _igniteClip;

        public FireZone Prefab => _prefab;
        public int PoolPrewarm => _poolPrewarm;
        public float Radius => _radius;
        public float TelegraphDuration => _telegraphDuration;
        public float ActiveDuration => _activeDuration;
        public float TickInterval => _tickInterval;
        public float DamagePerTick => _damagePerTick;
        public float ZombieDamageFraction => _zombieDamageFraction;
        public int MinZonesPerEvent => _minZonesPerEvent;
        public int MaxZonesPerEvent => _maxZonesPerEvent;
        public float MinDistanceFromPlayer => _minDistanceFromPlayer;
        public float MaxDistanceFromPlayer => _maxDistanceFromPlayer;
        public float NavMeshSampleRadius => _navMeshSampleRadius;
        public int PlacementAttempts => _placementAttempts;
        public AudioClip IgniteClip => _igniteClip;
    }
}
