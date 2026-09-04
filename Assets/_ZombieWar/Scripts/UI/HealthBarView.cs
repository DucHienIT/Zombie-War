using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Data;
using ZombieWar.Player;

namespace ZombieWar.UI
{
    public sealed class HealthBarView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private PlayerHealth _health;
        [SerializeField] private FeedbackProfileSO _feedback;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _delayedFill;
        [SerializeField] private CanvasGroup _lowHealthPulse;

        [Header("Low Health Pulse")]
        [SerializeField] private float _pulseDuration = 0.4f;

        private Tween _fillTween;
        private Tween _delayedTween;
        private Tween _pulseTween;

        private void Awake()
        {
            if (_health == null || _feedback == null || _fill == null || _delayedFill == null || _lowHealthPulse == null)
            {
                Debug.LogError($"{LogPrefix} HealthBarView has an unassigned reference.", this);
            }
        }

        private void OnEnable()
        {
            _health.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            _health.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            float normalized = max > 0f ? current / max : 0f;
            _fillTween?.Kill();
            _delayedTween?.Kill();
            _fillTween = _fill.DOFillAmount(normalized, _feedback.HealthBarTweenDuration).SetLink(gameObject);
            _delayedTween = _delayedFill.DOFillAmount(normalized, _feedback.HealthBarTweenDuration)
                .SetDelay(_feedback.DelayedHealthBarDelay)
                .SetLink(gameObject);

            UpdateLowHealthPulse(normalized);
        }

        private void UpdateLowHealthPulse(float normalized)
        {
            bool low = normalized > 0f && normalized < _feedback.LowHealthFlashThreshold;
            bool pulsing = _pulseTween != null && _pulseTween.IsActive();
            if (low == pulsing)
            {
                return;
            }

            if (low)
            {
                _pulseTween = _lowHealthPulse.DOFade(1f, _pulseDuration).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
                return;
            }

            _pulseTween.Kill();
            _pulseTween = null;
            _lowHealthPulse.alpha = 0f;
        }
    }
}
