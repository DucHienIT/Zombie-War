using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ZombieWar.UI
{
    // Only on screen while a boss is alive; the binder owns that decision, this just draws it.
    public sealed class BossHealthBarView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private CanvasGroup _group;
        // Anchor driven like the player bar: a filled image would clip the rounded cap of the pill sprite.
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _nameLabel;

        [Header("Motion")]
        [SerializeField] private float _fadeDuration = 0.25f;
        [SerializeField] private float _fillDuration = 0.12f;

        private Tween _fadeTween;
        private Tween _fillTween;

        private void Awake()
        {
            bool missing = _group == null || _fill == null || _nameLabel == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} BossHealthBarView has an unassigned reference.", this);
                return;
            }

            _group.alpha = 0f;
        }

        public void Show(string displayName)
        {
            _nameLabel.text = displayName;
            SetFill(1f, 0f);
            Fade(1f);
        }

        public void SetHealth(float normalized) => SetFill(normalized, _fillDuration);

        public void Hide() => Fade(0f);

        private void SetFill(float normalized, float duration)
        {
            var target = new Vector2(Mathf.Clamp01(normalized), 1f);
            _fillTween?.Kill();
            if (duration <= 0f)
            {
                _fill.anchorMax = target;
                return;
            }

            _fillTween = _fill.DOAnchorMax(target, duration).SetLink(gameObject);
        }

        private void Fade(float alpha)
        {
            _fadeTween?.Kill();
            _fadeTween = _group.DOFade(alpha, _fadeDuration).SetLink(gameObject);
        }
    }
}
