using UnityEngine;
using ZombieWar.Data;
using ZombieWar.Enemies;
using ZombieWar.Weapons;

namespace ZombieWar.Core
{
    // Freezes or slows the run for a few frames on the big moments. It only owns the time scale
    // for the window it asked for: a pause or level-up that lands in between takes it over, and
    // the freeze then lets go without writing anything back over that.
    public sealed class HitStopController : MonoBehaviour
    {
        private const string LogPrefix = "[Feel]";
        private const float NormalTimeScale = 1f;

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private FeedbackProfileSO _feedback;
        [SerializeField] private BombThrower _bombs;
        [SerializeField] private ZombieManager _zombies;

        private float _remaining;
        private float _appliedScale;
        private bool _active;

        private void Awake()
        {
            if (_flow == null || _feedback == null || _bombs == null || _zombies == null)
            {
                Debug.LogError($"{LogPrefix} HitStopController has an unassigned reference.", this);
            }
        }

        private void OnEnable()
        {
            _flow.OnStateChanged += HandleStateChanged;
            _flow.OnLevelEnded += HandleLevelEnded;
            _bombs.OnExploded += HandleBombExploded;
            _zombies.OnZombieKilled += HandleZombieKilled;
        }

        private void OnDisable()
        {
            _flow.OnStateChanged -= HandleStateChanged;
            _flow.OnLevelEnded -= HandleLevelEnded;
            _bombs.OnExploded -= HandleBombExploded;
            _zombies.OnZombieKilled -= HandleZombieKilled;
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f)
            {
                Release();
            }
        }

        private void Request(float duration, float timeScale)
        {
            if (duration <= 0f)
            {
                return;
            }

            GameState state = _flow.State;
            if (state != GameState.Playing && state != GameState.Lost)
            {
                return;
            }

            // Overlapping requests keep the deeper freeze and the longer tail.
            if (_active)
            {
                _remaining = Mathf.Max(_remaining, duration);
                _appliedScale = Mathf.Min(_appliedScale, timeScale);
            }
            else
            {
                _remaining = duration;
                _appliedScale = timeScale;
                _active = true;
            }

            Time.timeScale = _appliedScale;
        }

        private void Release()
        {
            _active = false;
            // Pause or level-up may have taken the time scale in the meantime; only undo our own write.
            if (Mathf.Approximately(Time.timeScale, _appliedScale))
            {
                Time.timeScale = NormalTimeScale;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            // Lost keeps running so the death slow-motion can still land right after this.
            if (_active && state != GameState.Lost)
            {
                Release();
            }
        }

        private void HandleLevelEnded(LevelResult result)
        {
            if (!result.Won)
            {
                Request(_feedback.PlayerDeathSlowMotionDuration, _feedback.PlayerDeathSlowMotionTimeScale);
            }
        }

        private void HandleBombExploded(Vector3 center, int zombiesHit)
        {
            if (zombiesHit > 0)
            {
                Request(_feedback.BombHitStopDuration, _feedback.BombHitStopTimeScale);
            }
        }

        private void HandleZombieKilled(ZombieController zombie)
        {
            Request(zombie.Definition.DeathHitStopDuration, _feedback.KillHitStopTimeScale);
        }
    }
}
