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

        [Header("HUD")]
        [SerializeField] private float _healthBarTweenDuration = 0.15f;
        [SerializeField] private float _delayedHealthBarDelay = 0.25f;
        [SerializeField] private float _lowHealthFlashThreshold = 0.25f;

        public float ZombieHitFlashDuration => _zombieHitFlashDuration;
        public float DeathPoseDuration => _deathPoseDuration;
        public float DissolveDuration => _dissolveDuration;
        public float PlayerHitFlashDuration => _playerHitFlashDuration;
        public float PlayerVignetteDuration => _playerVignetteDuration;
        public float HealthBarTweenDuration => _healthBarTweenDuration;
        public float DelayedHealthBarDelay => _delayedHealthBarDelay;
        public float LowHealthFlashThreshold => _lowHealthFlashThreshold;
    }
}
