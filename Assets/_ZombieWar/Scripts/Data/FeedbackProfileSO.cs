using UnityEngine;

namespace ZombieWar.Data
{
    [CreateAssetMenu(menuName = "Zombie War/Feedback Profile", fileName = "FeedbackProfile")]
    public sealed class FeedbackProfileSO : ScriptableObject
    {
        [Header("Zombie")]
        [SerializeField] private float _zombieHitFlashDuration = 0.1f;
        [SerializeField] private float _deathPoseDuration = 0.15f;
        [SerializeField] private float _dissolveDuration = 0.65f;

        [Header("Player")]
        [SerializeField] private float _playerHitFlashDuration = 0.1f;
        [SerializeField] private float _playerVignetteDuration = 0.18f;
        // Peak of the hit spike; the resting intensity is whatever the volume profile authors.
        [SerializeField] private float _playerHitVignetteIntensity = 0.45f;
        [SerializeField] private float _lowHealthVignetteIntensity = 0.38f;
        [SerializeField] private float _lowHealthPulseFrequency = 1.2f;

        [Header("Hit Stop")]
        // Freezes run on unscaled time; a zero duration switches that moment off.
        [SerializeField] private float _bombHitStopDuration = 0.07f;
        [SerializeField] private float _bombHitStopTimeScale = 0.12f;
        [SerializeField] private float _killHitStopTimeScale = 0.1f;
        [SerializeField] private float _playerDeathSlowMotionDuration = 1.4f;
        [SerializeField] private float _playerDeathSlowMotionTimeScale = 0.25f;

        [Header("HUD")]
        [SerializeField] private float _healthBarTweenDuration = 0.15f;
        [SerializeField] private float _delayedHealthBarDelay = 0.25f;
        [SerializeField] private float _lowHealthFlashThreshold = 0.25f;

        public float ZombieHitFlashDuration => _zombieHitFlashDuration;
        public float DeathPoseDuration => _deathPoseDuration;
        public float DissolveDuration => _dissolveDuration;
        public float PlayerHitFlashDuration => _playerHitFlashDuration;
        public float PlayerVignetteDuration => _playerVignetteDuration;
        public float PlayerHitVignetteIntensity => _playerHitVignetteIntensity;
        public float LowHealthVignetteIntensity => _lowHealthVignetteIntensity;
        public float LowHealthPulseFrequency => _lowHealthPulseFrequency;
        public float BombHitStopDuration => _bombHitStopDuration;
        public float BombHitStopTimeScale => _bombHitStopTimeScale;
        public float KillHitStopTimeScale => _killHitStopTimeScale;
        public float PlayerDeathSlowMotionDuration => _playerDeathSlowMotionDuration;
        public float PlayerDeathSlowMotionTimeScale => _playerDeathSlowMotionTimeScale;
        public float HealthBarTweenDuration => _healthBarTweenDuration;
        public float DelayedHealthBarDelay => _delayedHealthBarDelay;
        public float LowHealthFlashThreshold => _lowHealthFlashThreshold;
    }
}
