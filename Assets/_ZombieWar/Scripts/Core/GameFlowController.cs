using System;
using UnityEngine;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Level;
using ZombieWar.Player;

namespace ZombieWar.Core
{
    public sealed class GameFlowController : MonoBehaviour
    {
        private const string LogPrefix = "[Flow]";
        private const int TargetFrameRate = 60;

        [SerializeField] private LevelMapLoader _mapLoader;
        [SerializeField] private ScoringRulesSO _scoringRules;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private ZombieManager _zombies;
        [SerializeField] private LevelLoader _levelLoader;

        private readonly SaveService _save = new SaveService();
        private LevelDefinitionSO _level;
        private LevelTimer _timer;
        private ScoreTracker _score;
        private float _countdownRemaining;
        private int _lastCountdownShown = -1;

        public event Action<GameState> OnStateChanged;
        public event Action<int> OnCountdownChanged;
        public event Action<float> OnRemainingTimeChanged;
        public event Action<int, int> OnScoreChanged;
        public event Action<LevelResult> OnLevelEnded;

        public GameState State { get; private set; }
        public LevelDefinitionSO Level => _level;
        public float ElapsedTime => _timer.Elapsed;
        public float RemainingTime => _timer.Remaining;
        public int Kills => _score.Kills;
        public int Score => _score.Score;

        private void Awake()
        {
            bool missing = _mapLoader == null || _scoringRules == null || _playerHealth == null || _zombies == null || _levelLoader == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} GameFlowController has an unassigned reference.", this);
                return;
            }

            // LevelMapLoader runs first (DefaultExecutionOrder) so the level is already resolved here.
            _level = _mapLoader.Level;
            Application.targetFrameRate = TargetFrameRate;
            Time.timeScale = 1f;
            _timer = new LevelTimer(_level.Duration);
            _score = new ScoreTracker(_scoringRules.MultiKillWindow, _scoringRules.MultiKillBonus, _scoringRules.HealthBonusPerPercent);
            _countdownRemaining = _level.CountdownDuration;
            State = GameState.Countdown;
        }

        private void OnEnable()
        {
            _playerHealth.OnDied += HandlePlayerDied;
            _zombies.OnZombieKilled += HandleZombieKilled;
        }

        private void OnDisable()
        {
            _playerHealth.OnDied -= HandlePlayerDied;
            _zombies.OnZombieKilled -= HandleZombieKilled;
        }

        private void Start()
        {
            OnStateChanged?.Invoke(State);
            OnRemainingTimeChanged?.Invoke(_timer.Remaining);
            OnScoreChanged?.Invoke(0, 0);
            PublishCountdown();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            switch (State)
            {
                case GameState.Countdown:
                    TickCountdown(deltaTime);
                    break;
                case GameState.Playing:
                    TickPlaying(deltaTime);
                    break;
            }
        }

        public void Pause()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void Retry() => _levelLoader.ReloadCurrent();

        public void GoToMenu() => _levelLoader.LoadMenu();

        public void GoToNextLevel()
        {
            if (_level.NextLevel == null)
            {
                _levelLoader.LoadMenu();
                return;
            }

            _levelLoader.LoadLevel(_level.NextLevel);
        }

        private void TickCountdown(float deltaTime)
        {
            _countdownRemaining -= deltaTime;
            PublishCountdown();
            if (_countdownRemaining <= 0f)
            {
                SetState(GameState.Playing);
            }
        }

        private void PublishCountdown()
        {
            int shown = Mathf.CeilToInt(_countdownRemaining);
            if (shown == _lastCountdownShown)
            {
                return;
            }

            _lastCountdownShown = shown;
            OnCountdownChanged?.Invoke(shown);
        }

        private void TickPlaying(float deltaTime)
        {
            _timer.Tick(deltaTime);
            OnRemainingTimeChanged?.Invoke(_timer.Remaining);
            if (_timer.IsFinished)
            {
                EndLevel(true);
            }
        }

        private void HandlePlayerDied()
        {
            if (State == GameState.Playing)
            {
                EndLevel(false);
            }
        }

        private void HandleZombieKilled(ZombieController zombie)
        {
            if (State != GameState.Playing)
            {
                return;
            }

            _score.RegisterKill(zombie.Definition.ScoreReward, _timer.Elapsed);
            OnScoreChanged?.Invoke(_score.Kills, _score.Score);
        }

        private void EndLevel(bool won)
        {
            int healthBonus = won ? _score.ComputeHealthBonus(_playerHealth.Normalized) : 0;
            int total = _score.Score + healthBonus;
            bool isNewBest = _save.TrySubmitScore(_level.LevelIndex, total);
            if (won)
            {
                _zombies.DespawnAll();
                if (_level.NextLevel != null)
                {
                    _save.UnlockLevel(_level.NextLevel.LevelIndex);
                }
            }

            var result = new LevelResult(won, _score.Kills, _score.Score, healthBonus, _playerHealth.DamageTaken, isNewBest);
            SetState(won ? GameState.Won : GameState.Lost);
            OnLevelEnded?.Invoke(result);
        }

        private void SetState(GameState next)
        {
            State = next;
            OnStateChanged?.Invoke(next);
        }
    }
}
