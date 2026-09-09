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
        // Chapter picture on the battle page; the menu keeps its placeholder when this is empty.
        [SerializeField] private Sprite _artwork;
        [SerializeField] private LevelMap _mapPrefab;
        [SerializeField] private LevelDefinitionSO _nextLevel;

        [Header("Session")]
        [SerializeField] private float _duration = 180f;
        // Length of the opening cinematic before the first zombie spawns; the player can tap to skip it.
        [SerializeField] private float _introDuration = 4.5f;
        [SerializeField] private WavePhaseSO[] _phases;
        // When set, running out of clock is not enough: the run stays in play until the level's
        // boss is down. The boss itself is a scripted spawn in one of the phases above.
        [SerializeField] private bool _requiresBossDefeat;

        [Header("Spawning")]
        [SerializeField] private float _spawnRingInner = 12f;
        [SerializeField] private float _spawnRingOuter = 17f;
        [SerializeField] private float _minSpawnDistance = 11f;
        [SerializeField] private float _viewportMargin = 0.05f;
        [SerializeField] private float _navMeshSampleRadius = 2f;
        [SerializeField] private int _spawnAttempts = 12;

        public int LevelIndex => _levelIndex;
        public string DisplayName => _displayName;
        public Sprite Artwork => _artwork;
        public LevelMap MapPrefab => _mapPrefab;
        public LevelDefinitionSO NextLevel => _nextLevel;
        public float Duration => _duration;
        public float IntroDuration => _introDuration;
        public WavePhaseSO[] Phases => _phases;
        public bool RequiresBossDefeat => _requiresBossDefeat;
        public float SpawnRingInner => _spawnRingInner;
        public float SpawnRingOuter => _spawnRingOuter;
        public float MinSpawnDistance => _minSpawnDistance;
        public float ViewportMargin => _viewportMargin;
        public float NavMeshSampleRadius => _navMeshSampleRadius;
        public int SpawnAttempts => _spawnAttempts;
    }
}
