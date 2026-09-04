using UnityEngine;
using ZombieWar.Level;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private int _levelIndex = 1;
        [SerializeField] private string _displayName;
        [SerializeField] private LevelMap _mapPrefab;
        [SerializeField] private LevelDefinitionSO _nextLevel;

        [Header("Session")]
        [SerializeField] private float _duration = 180f;
        [SerializeField] private float _countdownDuration = 3f;
        [SerializeField] private WavePhaseSO[] _phases;

        [Header("Spawning")]
        [SerializeField] private float _spawnRingInner = 12f;
        [SerializeField] private float _spawnRingOuter = 17f;
        [SerializeField] private float _minSpawnDistance = 11f;
        [SerializeField] private float _viewportMargin = 0.05f;
        [SerializeField] private float _navMeshSampleRadius = 2f;
        [SerializeField] private int _spawnAttempts = 12;

        public int LevelIndex => _levelIndex;
        public string DisplayName => _displayName;
        public LevelMap MapPrefab => _mapPrefab;
        public LevelDefinitionSO NextLevel => _nextLevel;
        public float Duration => _duration;
        public float CountdownDuration => _countdownDuration;
        public WavePhaseSO[] Phases => _phases;
        public float SpawnRingInner => _spawnRingInner;
        public float SpawnRingOuter => _spawnRingOuter;
        public float MinSpawnDistance => _minSpawnDistance;
        public float ViewportMargin => _viewportMargin;
        public float NavMeshSampleRadius => _navMeshSampleRadius;
        public int SpawnAttempts => _spawnAttempts;
    }
}
