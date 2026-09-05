using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    public sealed class ResultPopupUI : PopupBase
    {
        private const string LogPrefix = "[Popup]";

        [Header("Header")]
        [SerializeField] private Image _titleBanner;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Sprite _wonBanner;
        [SerializeField] private Sprite _lostBanner;
        [SerializeField] private string _wonTitle = "LEVEL CLEARED";
        [SerializeField] private string _lostTitle = "YOU DIED";

        [Header("Stats")]
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _healthBonusText;
        [SerializeField] private TMP_Text _damageTakenText;
        [SerializeField] private TMP_Text _totalText;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private TMP_Text _xpText;
        [SerializeField] private GameObject _newBestBadge;

        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _menuButton;

        [Header("Total Count Up")]
        [SerializeField] private float _countUpDuration = 0.6f;

        private Action _onRetry;
        private Action _onNext;
        private Action _onMenu;
        private Action _exitAction;
        private ResultData _result;
        private int _shownTotal;

        protected override void Awake()
        {
            base.Awake();
            bool missing = _titleBanner == null || _titleText == null || _killsText == null || _scoreText == null
                           || _healthBonusText == null || _damageTakenText == null || _totalText == null
                           || _coinsText == null || _xpText == null || _newBestBadge == null || _retryButton == null || _nextButton == null || _menuButton == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} ResultPopupUI has an unassigned reference.", this);
                return;
            }

            _retryButton.onClick.AddListener(HandleRetryClicked);
            _nextButton.onClick.AddListener(HandleNextClicked);
            _menuButton.onClick.AddListener(HandleMenuClicked);
        }

        public void Setup(in ResultData result, Action onRetry, Action onNext, Action onMenu)
        {
            _result = result;
            _onRetry = onRetry;
            _onNext = onNext;
            _onMenu = onMenu;
            _exitAction = null;
        }

        protected override void OnShowing()
        {
            _titleBanner.sprite = _result.Won ? _wonBanner : _lostBanner;
            _titleText.text = _result.Won ? _wonTitle : _lostTitle;
            _killsText.SetText("{0}", _result.Kills);
            _scoreText.SetText("{0}", _result.Score);
            _healthBonusText.SetText("{0}", _result.HealthBonus);
            _damageTakenText.SetText("{0}", _result.DamageTaken);
            _coinsText.SetText("+{0}", _result.CoinsEarned);
            _xpText.SetText("+{0}", _result.XpEarned);
            _newBestBadge.SetActive(_result.IsNewBest);
            _nextButton.gameObject.SetActive(_result.Won && _result.HasNextLevel);
            PlayTotalCountUp(_result.TotalScore);
        }

        protected override void OnClosed()
        {
            Action exit = _exitAction;
            _exitAction = null;
            exit?.Invoke();
        }

        // The score is already banked; this only walks the label up to a number the player owns.
        private void PlayTotalCountUp(int total)
        {
            DOTween.Kill(_totalText);
            _shownTotal = 0;
            _totalText.SetText("{0}", 0);
            DOTween.To(GetShownTotal, SetShownTotal, total, _countUpDuration)
                .SetTarget(_totalText)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private int GetShownTotal() => _shownTotal;

        private void SetShownTotal(int value)
        {
            _shownTotal = value;
            _totalText.SetText("{0}", value);
        }

        private void HandleRetryClicked()
        {
            _exitAction = _onRetry;
            Close();
        }

        private void HandleNextClicked()
        {
            _exitAction = _onNext;
            Close();
        }

        private void HandleMenuClicked()
        {
            _exitAction = _onMenu;
            Close();
        }
    }
}
