using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ZombieWar.UI
{
    // Drives both loading screens: the one the Loading scene shows while Gameplay streams in,
    // and the panel inside UIRoot that covers the screen while one run is swapped for another.
    public sealed class LoadingView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";
        private const int PercentScale = 100;

        // Anchor-driven like the health and XP bars: a 9-slice pill would lose its rounded cap
        // under Image.Type.Filled, and anchors read right on the very first frame.
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _percentText;
        [SerializeField] private TMP_Text _hintText;

        [Header("Hint Pulse")]
        [SerializeField] private float _hintMinAlpha = 0.35f;
        [SerializeField] private float _hintPulseDuration = 0.7f;
        [SerializeField] private Ease _hintPulseEase = Ease.InOutSine;

        [Header("Fade")]
        // Only the in-UI panel fades: the Loading scene owns the whole screen for its lifetime,
        // so its instance leaves this empty and never calls Show or Hide.
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _fadeInDuration = 0.22f;
        [SerializeField] private float _fadeOutDuration = 0.3f;

        private int _shownPercent = -1;
        private Tween _fade;
        private TweenCallback _onFadedOut;

        private void Awake()
        {
            if (_fill == null || _percentText == null || _hintText == null)
            {
                Debug.LogError($"{LogPrefix} LoadingView has an unassigned reference.", this);
                return;
            }

            _onFadedOut = HandleFadedOut;
            _hintText.DOFade(_hintMinAlpha, _hintPulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(_hintPulseEase)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void SetProgress(float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);
            _fill.anchorMax = new Vector2(clamped, 1f);

            int percent = Mathf.RoundToInt(clamped * PercentScale);
            if (percent == _shownPercent)
            {
                return;
            }

            _shownPercent = percent;
            _percentText.SetText("{0}%", percent);
        }

        // Unscaled all the way through: the run being covered is usually frozen at this point.
        public void Show()
        {
            gameObject.SetActive(true);
            SetProgress(0f);
            if (_group == null)
            {
                return;
            }

            _fade?.Kill();
            _group.alpha = 0f;
            _group.blocksRaycasts = true;
            _fade = _group.DOFade(1f, _fadeInDuration).SetUpdate(true).SetLink(gameObject);
        }

        public void Hide()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            if (_group == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _fade?.Kill();
            _group.blocksRaycasts = false;
            _fade = _group.DOFade(0f, _fadeOutDuration).SetUpdate(true).SetLink(gameObject).OnComplete(_onFadedOut);
        }

        private void HandleFadedOut() => gameObject.SetActive(false);
    }
}
