using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Data;

namespace ZombieWar.UI
{
    public sealed class HealthBarView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private FeedbackProfileSO _feedback;
        // Both bars stretch inside the track, so the fill is driven by anchors: that reads
        // right on the very first frame, before the canvas scaler has sized anything.
        [SerializeField] private RectTransform _fill;
        [SerializeField] private RectTransform _delayedFill;
        [SerializeField] private CanvasGroup _lowHealthPulse;
        [SerializeField] private Image _fillImage;

        [Header("Fill Art")]
        [SerializeField] private Sprite _healthyFill;
        [SerializeField] private Sprite _lowHealthFill;

        [Header("Low Health Pulse")]
        [SerializeField] private float _pulseDuration = 0.4f;

        private Tween _fillTween;
        private Tween _delayedTween;
        private Tween _pulseTween;

        private void Awake()
        {
            bool missing = _feedback == null || _fill == null || _delayedFill == null
                           || _lowHealthPulse == null || _fillImage == null || _healthyFill == null || _lowHealthFill == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} HealthBarView has an unassigned reference.", this);
            }
        }

        public void SetHealth(float normalized)
        {
            var target = new Vector2(Mathf.Clamp01(normalized), 1f);
            _fillTween?.Kill();
            _delayedTween?.Kill();
            _fillTween = _fill.DOAnchorMax(target, _feedback.HealthBarTweenDuration).SetLink(gameObject);
            _delayedTween = _delayedFill.DOAnchorMax(target, _feedback.HealthBarTweenDuration)
                .SetDelay(_feedback.DelayedHealthBarDelay)
                .SetLink(gameObject);

            bool low = normalized > 0f && normalized < _feedback.LowHealthFlashThreshold;
            _fillImage.sprite = low ? _lowHealthFill : _healthyFill;
            UpdateLowHealthPulse(low);
        }

        private void UpdateLowHealthPulse(bool low)
        {
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
