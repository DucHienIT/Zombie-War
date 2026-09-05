using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ZombieWar.Data;
using ZombieWar.Player;

namespace ZombieWar.Core
{
    // Drives the vignette of the gameplay volume: a spike on every hit and a slow breathing
    // pulse while health is low. Only the intensity moves; colour and shape stay on the profile.
    public sealed class HitVignettePresenter : MonoBehaviour
    {
        private const string LogPrefix = "[Feel]";

        [SerializeField] private Volume _volume;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private FeedbackProfileSO _feedback;

        private Vignette _vignette;
        private float _restIntensity;
        private float _hitTimer;
        private float _pulsePhase;
        private float _applied;
        private bool _lowHealth;

        private void Awake()
        {
            if (_volume == null || _health == null || _feedback == null)
            {
                Debug.LogError($"{LogPrefix} HitVignettePresenter has an unassigned reference.", this);
                return;
            }

            // Volume.profile is the runtime copy, so the asset never picks up gameplay writes.
            if (!_volume.profile.TryGet(out _vignette))
            {
                Debug.LogError($"{LogPrefix} {_volume.sharedProfile.name} has no Vignette override to drive.", this);
                return;
            }

            _restIntensity = _vignette.intensity.value;
            _applied = _restIntensity;
        }

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnHealthChanged -= HandleHealthChanged;
        }

        private void Update()
        {
            if (_vignette == null)
            {
                return;
            }

            // Unscaled so the flash reads the same through hit-stop and death slow-motion.
            float deltaTime = Time.unscaledDeltaTime;
            float target = _restIntensity;
            if (_lowHealth)
            {
                _pulsePhase += deltaTime * _feedback.LowHealthPulseFrequency;
                float wave = 0.5f + 0.5f * Mathf.Sin(_pulsePhase * Mathf.PI * 2f);
                target = Mathf.Lerp(_restIntensity, _feedback.LowHealthVignetteIntensity, wave);
            }

            if (_hitTimer > 0f)
            {
                _hitTimer -= deltaTime;
                float blend = Mathf.Clamp01(_hitTimer / _feedback.PlayerVignetteDuration);
                target = Mathf.Max(target, Mathf.Lerp(_restIntensity, _feedback.PlayerHitVignetteIntensity, blend));
            }

            Apply(target);
        }

        private void Apply(float intensity)
        {
            if (Mathf.Approximately(intensity, _applied))
            {
                return;
            }

            _applied = intensity;
            _vignette.intensity.value = intensity;
        }

        private void HandleDamaged(DamageInfo info)
        {
            _hitTimer = _feedback.PlayerVignetteDuration;
        }

        private void HandleHealthChanged(float current, float max)
        {
            float normalized = max > 0f ? current / max : 0f;
            _lowHealth = normalized > 0f && normalized < _feedback.LowHealthFlashThreshold;
            if (!_lowHealth)
            {
                _pulsePhase = 0f;
            }
        }
    }
}
