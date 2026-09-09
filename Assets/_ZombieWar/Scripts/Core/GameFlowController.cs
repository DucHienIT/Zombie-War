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

        [Header("Transition")]
        // Retry, next level and "back to menu" swap runs in place behind the loading panel.
        // The cover delay is how long the panel gets to become opaque before the swap; the rest
        // of the duration is the panel holding still so the switch never flashes into view.
        [SerializeField] private float _transitionDuration = 1.1f;
        [SerializeField] private float _coverDelay = 0.35f;

        private readonly SaveService _save = new SaveService();
        private LevelDefinitionSO _level;
        private LevelTimer _timer;
        private ScoreTracker _score;
        private float _introDuration;
        private float _introElapsed;
        private LevelDefinitionSO _pendingLevel;
        private float _transitionElapsed;
        private bool _runCleared;

        public event Action<GameState> OnStateChanged;
        public event Action<LevelDefinitionSO> OnRunStarted;
        // The run that was on screen is being thrown away: every system drops what it spawned.
        // Fired while the loading panel covers the screen, so the hitch it costs is never seen.
        public event Action OnRunCleared;
        public event Action<float> OnLoadingProgress;
        // Normalized 0..1 through the opening cinematic; presenters drive camera and overlay from it.
        public event Action<float> OnIntroProgress;
        public event Action<float> OnRemainingTimeChanged;
        public event Action<int, int> OnScoreChanged;
        public event Action<LevelResult> OnLevelEnded;

        public GameState State { get; private set; }
        // A boss level runs past its clock until the boss is down, so surviving the timer is not a win on its own.
        public bool AwaitingBoss => _level != null && _level.RequiresBossDefeat && !_zombies.BossDefeated;
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
            // The editor cheat window can stamp a level onto the selection asset before entering
            // play mode; every other entry into the scene lands on the menu.
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
                case GameState.Intro:
                    TickIntro(deltaTime);
                    break;
                case GameState.Playing:
                    TickPlaying(deltaTime);
                    break;
                case GameState.Loading:
                    // Unscaled: the run being left behind may still be frozen by the pause menu.
                    TickTransition(Time.unscaledDeltaTime);
                    break;
            }
        }

        public void StartRun(LevelDefinitionSO level)
        {
            if (!PrepareRun(level))
            {
                return;
            }

            LaunchPreparedRun();
        }

        // A tap during the cinematic jumps straight to the fight instead of sitting through it.
        public void SkipIntro()
        {
            if (State != GameState.Intro)
            {
                return;
            }

            _introElapsed = _introDuration;
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

        public void Retry() => BeginTransition(_level);

        public void GoToMenu() => BeginTransition(null);

        public void GoToNextLevel() => BeginTransition(_level != null ? _level.NextLevel : null);

        // Drops whatever is running and brings up another level, the same way Retry does.
        public void LoadLevel(LevelDefinitionSO level) => BeginTransition(level);

        // Leaving a run used to reload the scene through the Loading scene, which is the cheapest
        // way to be sure pools, physics and the map start clean - but it also meant walking back
        // to the main menu made the player sit through a full load screen. The swap now happens
        // in place: the loading panel covers the screen, every system drops what the run left
        // behind, and the next run (or the menu) is built while none of it is visible.
        private void BeginTransition(LevelDefinitionSO next)
        {
            if (State == GameState.Loading)
            {
                return;
            }

            Time.timeScale = 1f;
            _pendingLevel = next;
            _transitionElapsed = 0f;
            _runCleared = false;
            SetState(GameState.Loading);
            OnLoadingProgress?.Invoke(0f);
        }

        private void TickTransition(float deltaTime)
        {
            _transitionElapsed += deltaTime;
            OnLoadingProgress?.Invoke(_transitionDuration > 0f ? Mathf.Clamp01(_transitionElapsed / _transitionDuration) : 1f);
            if (_transitionElapsed >= _coverDelay)
            {
                SwapRun();
            }

            if (_transitionElapsed < _transitionDuration)
            {
                return;
            }

            SwapRun();
            // The next run was built while covered; all that is left is to hand it the clock.
            if (_level != null)
            {
                LaunchPreparedRun();
                return;
            }

            SetState(GameState.Menu);
        }

        // The whole swap sits behind the panel: the old run is dropped and the new one built in
        // the same frame, so the intro never starts on a half-built level.
        private void SwapRun()
        {
            if (_runCleared)
            {
                return;
            }

            _runCleared = true;
            // Everything on the player still exists here; the map loader parks it right after.
            OnRunCleared?.Invoke();
            LevelDefinitionSO next = _pendingLevel;
            _pendingLevel = null;
            if (next != null && PrepareRun(next))
            {
                return;
            }

            _level = null;
            _mapLoader.Unload();
        }

        // Builds the world and the bookkeeping for one run without starting its clock.
        private bool PrepareRun(LevelDefinitionSO level)
        {
            if (level == null)
            {
                Debug.LogError($"{LogPrefix} PrepareRun called with no level.", this);
                return false;
            }

            _level = level;
            _mapLoader.Load(level);
            _timer = new LevelTimer(level.Duration);
            _score = new ScoreTracker(_scoringRules.MultiKillWindow, _scoringRules.MultiKillBonus, _scoringRules.HealthBonusPerPercent);
            _introDuration = level.IntroDuration;
            _introElapsed = 0f;
            Time.timeScale = 1f;
            OnRunStarted?.Invoke(level);
            return true;
        }

        private void LaunchPreparedRun()
        {
            SetState(GameState.Intro);
            OnRemainingTimeChanged?.Invoke(_timer.Remaining);
            OnScoreChanged?.Invoke(0, 0);
            OnIntroProgress?.Invoke(0f);
        }

        private void TickIntro(float deltaTime)
        {
            _introElapsed += deltaTime;
            bool finished = _introElapsed >= _introDuration;
            OnIntroProgress?.Invoke(finished ? 1f : _introElapsed / _introDuration);
            if (finished)
            {
                SetState(GameState.Playing);
            }
        }

        private void TickPlaying(float deltaTime)
        {
            _timer.Tick(deltaTime);
            OnRemainingTimeChanged?.Invoke(_timer.Remaining);
            if (_timer.IsFinished && !AwaitingBoss)
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
