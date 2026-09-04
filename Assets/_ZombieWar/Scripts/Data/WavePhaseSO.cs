using UnityEngine;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Wave Phase", fileName = "WavePhase")]
    public sealed class WavePhaseSO : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField] private float _startTime;
        [SerializeField] private float _endTime = 25f;
        [SerializeField] private float _spawnInterval = 1f;
        [SerializeField] private int _aliveCap = 12;

        [Header("Enemies")]
        [SerializeField] private SpawnWeight[] _weights;
        // Must be authored in ascending time order; the director walks them with a cursor.
        [SerializeField] private ScriptedSpawn[] _scriptedSpawns;

        [Header("Events")]
        [SerializeField] private float[] _fireHazardTimes;

        public float StartTime => _startTime;
        public float EndTime => _endTime;
        public float SpawnInterval => _spawnInterval;
        public int AliveCap => _aliveCap;
        public SpawnWeight[] Weights => _weights;
        public ScriptedSpawn[] ScriptedSpawns => _scriptedSpawns;
        public float[] FireHazardTimes => _fireHazardTimes;
    }
}
