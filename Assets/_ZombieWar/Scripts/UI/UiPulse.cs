using DG.Tweening;
using UnityEngine;

namespace ZombieWar.UI
{
    // Idle breathing for a decorative graphic (a glow, a badge). Never put it on a button:
    // UiButtonFx kills the target's tweens on press and the loop would not come back.
    public sealed class UiPulse : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        // Optional: left empty the pulse only scales.
        [SerializeField] private CanvasGroup _group;

        [Header("Pulse")]
        [SerializeField] private float _scale = 1.06f;
        [SerializeField] private float _minAlpha = 0.55f;
        [SerializeField] private float _maxAlpha = 1f;
        [SerializeField] private float _duration = 1.1f;
        [SerializeField] private Ease _ease = Ease.InOutSine;

        private Vector3 _restScale;

        private void Awake() => _restScale = _target.localScale;

        private void OnEnable()
        {
            _target.DOKill();
            _target.localScale = _restScale;
            _target.DOScale(_restScale * _scale, _duration).SetEase(_ease).SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true).SetLink(gameObject);
            if (_group == null)
            {
                return;
            }

            _group.DOKill();
            _group.alpha = _maxAlpha;
            _group.DOFade(_minAlpha, _duration).SetEase(_ease).SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true).SetLink(gameObject);
        }

        private void OnDisable()
        {
            _target.DOKill();
            _target.localScale = _restScale;
            if (_group == null)
            {
                return;
            }

            _group.DOKill();
            _group.alpha = _maxAlpha;
        }
    }
}
