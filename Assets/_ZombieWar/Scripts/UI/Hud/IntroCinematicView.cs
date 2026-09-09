using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // Overlay for the opening cinematic: letterbox bars, the chapter title and a skip hint while
    // the camera sweeps the soldier, then a single "GO" flash as the bars pull away.
    public sealed class IntroCinematicView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Canvas _canvas;
        // Full-screen invisible button: the whole overlay is the skip target.
        [SerializeField] private Button _skipButton;

        [Header("Letterbox")]
        [SerializeField] private RectTransform _topBar;
        [SerializeField] private RectTransform _bottomBar;
        [SerializeField] private float _barHeight = 170f;
        [SerializeField] private float _barDuration = 0.5f;
        [SerializeField] private Ease _barEase = Ease.OutCubic;

        [Header("Title")]
        [SerializeField] private CanvasGroup _titleGroup;
        [SerializeField] private RectTransform _titleRect;
        [SerializeField] private TMP_Text _chapterText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private float _titleDelay = 0.35f;
        [SerializeField] private float _titleFadeDuration = 0.5f;
        [SerializeField] private float _titleRise = 40f;
        [SerializeField] private Ease _titleEase = Ease.OutCubic;

        [Header("Skip Hint")]
        [SerializeField] private TMP_Text _skipHint;
        [SerializeField] private float _hintMinAlpha = 0.35f;
        [SerializeField] private float _hintPulseDuration = 0.7f;

        [Header("Start Flash")]
        [SerializeField] private TMP_Text _startText;
        [SerializeField] private Image _glow;
        [SerializeField] private RectTransform _punchTarget;
        [SerializeField] private float _punchScale = 0.4f;
        [SerializeField] private float _punchDuration = 0.35f;
        [SerializeField] private float _glowFadeDuration = 0.35f;
        [SerializeField] private float _startFlashDuration = 0.8f;

        private Action _onSkip;
        private Vector2 _titleRestPosition;
        private Sequence _sequence;
        private bool _showing;
        private TweenCallback _onFlashDone;

        private void Awake()
        {
            bool missing = _canvas == null || _skipButton == null || _topBar == null || _bottomBar == null
                           || _titleGroup == null || _titleRect == null || _chapterText == null || _titleText == null
                           || _skipHint == null || _startText == null || _glow == null || _punchTarget == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} IntroCinematicView has an unassigned reference.", this);
                return;
            }

            _titleRestPosition = _titleRect.anchoredPosition;
            _onFlashDone = Hide;
            _skipButton.onClick.AddListener(HandleSkipClicked);
        }

        public void Show(int chapter, string title, Action onSkip)
        {
            _onSkip = onSkip;
            _showing = true;
            KillTweens();
            _canvas.enabled = true;
            _skipButton.interactable = true;
            _chapterText.SetText("CHAPTER {0}", chapter);
            _titleText.SetText(title);
            _startText.gameObject.SetActive(false);
            _glow.gameObject.SetActive(false);

            SetBarHeight(0f);
            _titleGroup.alpha = 0f;
            _titleRect.anchoredPosition = _titleRestPosition - new Vector2(0f, _titleRise);
            _skipHint.alpha = 1f;

            _sequence = DOTween.Sequence().SetLink(gameObject);
            _sequence.Append(_topBar.DOSizeDelta(new Vector2(0f, _barHeight), _barDuration).SetEase(_barEase));
            _sequence.Join(_bottomBar.DOSizeDelta(new Vector2(0f, _barHeight), _barDuration).SetEase(_barEase));
            _sequence.Insert(_titleDelay, _titleGroup.DOFade(1f, _titleFadeDuration));
            _sequence.Insert(_titleDelay, _titleRect.DOAnchorPos(_titleRestPosition, _titleFadeDuration).SetEase(_titleEase));
            _sequence.Insert(_titleDelay, _skipHint.DOFade(_hintMinAlpha, _hintPulseDuration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));
        }

        // The fight is on: bars retract, the title goes, and the start word punches in once.
        public void Finish()
        {
            if (!_showing)
            {
                return;
            }

            _showing = false;
            KillTweens();
            _skipButton.interactable = false;
            _startText.gameObject.SetActive(true);
            _glow.gameObject.SetActive(true);

            _sequence = DOTween.Sequence().SetLink(gameObject);
            _sequence.Append(_topBar.DOSizeDelta(Vector2.zero, _barDuration).SetEase(_barEase));
            _sequence.Join(_bottomBar.DOSizeDelta(Vector2.zero, _barDuration).SetEase(_barEase));
            _sequence.Join(_titleGroup.DOFade(0f, _titleFadeDuration));
            _sequence.Join(_skipHint.DOFade(0f, _titleFadeDuration));
            _sequence.Join(_punchTarget.DOPunchScale(Vector3.one * _punchScale, _punchDuration, 1, 0f));
            _sequence.Join(_glow.DOFade(0f, _glowFadeDuration).From(1f));
            _sequence.AppendInterval(Mathf.Max(0f, _startFlashDuration - _barDuration));
            _sequence.OnComplete(_onFlashDone);
        }

        public void Hide()
        {
            _showing = false;
            KillTweens();
            _canvas.enabled = false;
        }

        private void HandleSkipClicked()
        {
            if (!_showing)
            {
                return;
            }

            _onSkip?.Invoke();
        }

        private void KillTweens()
        {
            _sequence?.Kill();
            _sequence = null;
            _punchTarget.DOKill(true);
        }

        private void SetBarHeight(float height)
        {
            _topBar.sizeDelta = new Vector2(0f, height);
            _bottomBar.sizeDelta = new Vector2(0f, height);
        }
    }
}
