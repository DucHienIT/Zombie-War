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
        [SerializeField] private ProfileService _profile;

        private readonly SaveService _save = new SaveService();
        private LevelDefinitionSO _level;
        private LevelTimer _timer;
        private ScoreTracker _score;
        private float _countdownRemaining;
        private int _lastCountdownShown = -1;

        public event Action<GameState> OnStateChanged;
        public event Action<LevelDefinitionSO> OnRunStarted;
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
            bool missing = _mapLoader == null || _scoringRules == null || _playerHealth == null || _zombies == null || _levelLoader == null
                           || _profile == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} GameFlowController has an unassigned reference.", this);
                return;
            }

            Application.targetFrameRate = TargetFrameRate;
            Time.timeScale = 1f;
            State = GameState.Menu;
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
            // Retry and "next level" reload the scene with the level already chosen; every
            // other entry into the scene lands on the menu.
            if (_levelLoader.ConsumeAutoStart() && _levelLoader.PendingLevel != null)
            {
                StartRun(_levelLoader.PendingLevel);
                return;
            }

            SetState(GameState.Menu);
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

        public void StartRun(LevelDefinitionSO level)
        {
            if (level == null)
            {
                Debug.LogError($"{LogPrefix} StartRun called with no level.", this);
                return;
            }

            _level = level;
            _levelLoader.Remember(level);
            _mapLoader.Load(level);
            _timer = new LevelTimer(level.Duration);
            _score = new ScoreTracker(_scoringRules.MultiKillWindow, _scoringRules.MultiKillBonus, _scoringRules.HealthBonusPerPercent);
            _countdownRemaining = level.CountdownDuration;
            _lastCountdownShown = -1;
            Time.timeScale = 1f;

            OnRunStarted?.Invoke(level);
            SetState(GameState.Countdown);
            OnRemainingTimeChanged?.Invoke(_timer.Remaining);
            OnScoreChanged?.Invoke(0, 0);
            PublishCountdown();
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

        // A level-up choice stops the run dead until a card is picked. It gets a state of its
        // own so the HUD never mistakes it for the pause menu.
        public void PauseForLevelUp()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(GameState.LevelUp);
        }

        public void ResumeFromLevelUp()
        {
            if (State != GameState.LevelUp)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        // Editor-only test hook (see Scripts/Editor/DebugCheatWindow): ends the run immediately
        // instead of waiting for the timer or the player dying, so result popups and rewards can
        // be checked without playing a level to completion.
        public void DebugForceEndLevel(bool won)
        {
            if (_level == null || State == GameState.Menu || State == GameState.Won || State == GameState.Lost)
            {
                return;
            }

            Time.timeScale = 1f;
            EndLevel(won);
        }

        public void Retry() => _levelLoader.RestartWith(_level);

        public void GoToMenu() => _levelLoader.ReturnToMenu();

        public void GoToNextLevel()
        {
            if (_level.NextLevel == null)
            {
                _levelLoader.ReturnToMenu();
                return;
            }

            _levelLoader.RestartWith(_level.NextLevel);
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

            _profile.GrantRunRewards(_score.Kills, total, won, out int coinsEarned, out int xpEarned);
            var result = new LevelResult(won, _score.Kills, _score.Score, healthBonus, _playerHealth.DamageTaken, isNewBest, coinsEarned, xpEarned);
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
