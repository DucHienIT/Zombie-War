using System;
using UnityEngine;
using ZombieWar.Core;
using ZombieWar.Data;
using ZombieWar.Enemies;

namespace ZombieWar.Level
{
    public sealed class WaveDirector : MonoBehaviour
    {
        private const string LogPrefix = "[Wave]";

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _player;

        [Header("Anti Spike")]
        // Below this average frame rate the spawn interval is stretched, never the damage or HP.
        [SerializeField] private float _lowFpsThreshold = 28f;
        [SerializeField] private float _maxIntervalStretch = 1.2f;
        [SerializeField] private float _fpsAverageWindow = 2f;

        private LevelDefinitionSO _level;
        private SpawnPointResolver _resolver;
        private int _phaseIndex;
        private int _scriptedCursor;
        // Bodies still owed by the scripted entry under the cursor; a boss must never be lost to a failed placement.
        private int _scriptedRemaining;
        private int _hazardCursor;
        private float _spawnTimer;
        private float _smoothedDeltaTime;

        public event Action<float> OnFireHazardRequested;

        public WavePhaseSO CurrentPhase => _level.Phases[_phaseIndex];

        private void Awake()
        {
            bool missing = _flow == null || _zombies == null || _camera == null || _player == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} WaveDirector has an unassigned reference.", this);
            }
        }

        private void OnEnable()
        {
            _flow.OnRunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            _flow.OnRunStarted -= HandleRunStarted;
        }

        // The level is only known once the player picks one, so the schedule is armed here
        // rather than in Awake.
        private void HandleRunStarted(LevelDefinitionSO level)
        {
            if (level.Phases == null || level.Phases.Length == 0)
            {
                Debug.LogError($"{LogPrefix} {level.name} has no wave phases.", this);
                return;
            }

            _level = level;
            _resolver = new SpawnPointResolver(level, _camera);
            _phaseIndex = 0;
            _scriptedCursor = 0;
            _scriptedRemaining = 0;
            _hazardCursor = 0;
            _spawnTimer = 0f;
            _smoothedDeltaTime = Time.fixedDeltaTime;
        }

        private void Update()
        {
            if (_level == null || _flow.State != GameState.Playing)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float elapsed = _flow.ElapsedTime;
            TrackFrameRate(deltaTime);
            AdvancePhase(elapsed);

            WavePhaseSO phase = CurrentPhase;
            FireScriptedSpawns(phase, elapsed);
            FireHazards(phase, elapsed);
            TickContinuousSpawn(phase, deltaTime);
        }

        private void TrackFrameRate(float deltaTime)
        {
            float blend = Mathf.Clamp01(deltaTime / _fpsAverageWindow);
            _smoothedDeltaTime = Mathf.Lerp(_smoothedDeltaTime, deltaTime, blend);
        }

        private void AdvancePhase(float elapsed)
        {
            WavePhaseSO[] phases = _level.Phases;
            while (_phaseIndex < phases.Length - 1 && elapsed >= phases[_phaseIndex].EndTime)
            {
                _phaseIndex++;
                _scriptedCursor = 0;
                _scriptedRemaining = 0;
                _hazardCursor = 0;
            }
        }

        private void FireScriptedSpawns(WavePhaseSO phase, float elapsed)
        {
            ScriptedSpawn[] spawns = phase.ScriptedSpawns;
            if (spawns == null)
            {
                return;
            }

            while (_scriptedCursor < spawns.Length && elapsed >= spawns[_scriptedCursor].Time)
            {
                ScriptedSpawn spawn = spawns[_scriptedCursor];
                if (_scriptedRemaining <= 0)
                {
                    _scriptedRemaining = spawn.Count;
                }

                while (_scriptedRemaining > 0 && TrySpawn(spawn.Definition))
                {
                    _scriptedRemaining--;
                }

                if (_scriptedRemaining > 0)
                {
                    // The ring had no free spot this frame; the debt is kept and retried next frame.
                    return;
                }

                _scriptedCursor++;
            }
        }

        private void FireHazards(WavePhaseSO phase, float elapsed)
        {
            float[] times = phase.FireHazardTimes;
            if (times == null)
            {
                return;
            }

            while (_hazardCursor < times.Length && elapsed >= times[_hazardCursor])
            {
                OnFireHazardRequested?.Invoke(times[_hazardCursor]);
                _hazardCursor++;
            }
        }

        private void TickContinuousSpawn(WavePhaseSO phase, float deltaTime)
        {
            _spawnTimer -= deltaTime;
            if (_spawnTimer > 0f)
            {
                return;
            }

            _spawnTimer = phase.SpawnInterval * CurrentIntervalStretch();
            if (_zombies.ActiveCount >= phase.AliveCap)
            {
                // At cap the tick is skipped, never banked: no catch-up burst when the crowd thins.
                return;
            }

            ZombieDefinitionSO definition = WeightedPicker.Pick(phase.Weights, UnityEngine.Random.value);
            if (definition != null)
            {
                TrySpawn(definition);
            }
        }

        private float CurrentIntervalStretch()
        {
            float lowFpsDeltaTime = 1f / _lowFpsThreshold;
            return _smoothedDeltaTime > lowFpsDeltaTime ? _maxIntervalStretch : 1f;
        }

        private bool TrySpawn(ZombieDefinitionSO definition)
        {
            Vector3 playerPosition = _player.position;
            if (!_resolver.TryResolve(playerPosition, out Vector3 spawnPosition))
            {
                return false;
            }

            Vector3 facing = playerPosition - spawnPosition;
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > 0f ? Quaternion.LookRotation(facing) : Quaternion.identity;
            _zombies.Spawn(definition, spawnPosition, rotation);
            return true;
        }
    }
}
