using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieWar.Audio;
using ZombieWar.Core;

namespace ZombieWar.UI
{
    public sealed class ResultPanelView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private GameFlowController _flow;
        [SerializeField] private AudioService _audio;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _healthBonusText;
        [SerializeField] private TMP_Text _damageTakenText;
        [SerializeField] private TMP_Text _totalScoreText;
        [SerializeField] private GameObject _newBestBadge;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _menuButton;

        [Header("Copy")]
        [SerializeField] private string _wonTitle = "LEVEL CLEARED";
        [SerializeField] private string _lostTitle = "YOU DIED";

        [Header("Audio")]
        [SerializeField] private AudioClip _winClip;
        [SerializeField] private AudioClip _loseClip;
        [SerializeField] private AudioClip _tapClip;

        private void Awake()
        {
            bool missing = _flow == null || _audio == null || _canvas == null || _titleText == null || _killsText == null || _scoreText == null
                           || _healthBonusText == null || _damageTakenText == null || _totalScoreText == null || _newBestBadge == null
                           || _retryButton == null || _nextButton == null || _menuButton == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} ResultPanelView has an unassigned reference.", this);
                return;
            }

            _retryButton.onClick.AddListener(HandleRetryClicked);
            _nextButton.onClick.AddListener(HandleNextClicked);
            _menuButton.onClick.AddListener(HandleMenuClicked);
        }

        private void OnEnable()
        {
            _flow.OnLevelEnded += HandleLevelEnded;
        }

        private void OnDisable()
        {
            _flow.OnLevelEnded -= HandleLevelEnded;
        }

        private void HandleLevelEnded(LevelResult result)
        {
            _titleText.text = result.Won ? _wonTitle : _lostTitle;
            _killsText.SetText("{0}", result.Kills);
            _scoreText.SetText("{0}", result.Score);
            _healthBonusText.SetText("{0}", result.HealthBonus);
            _damageTakenText.SetText("{0}", Mathf.RoundToInt(result.DamageTaken));
            _totalScoreText.SetText("{0}", result.TotalScore);
            _newBestBadge.SetActive(result.IsNewBest);
            _nextButton.gameObject.SetActive(result.Won && _flow.Level.NextLevel != null);
            _canvas.enabled = true;
            _audio.PlayUi(result.Won ? _winClip : _loseClip);
        }

        private void HandleRetryClicked()
        {
            _audio.PlayUi(_tapClip);
            _flow.Retry();
        }

        private void HandleNextClicked()
        {
            _audio.PlayUi(_tapClip);
            _flow.GoToNextLevel();
        }

        private void HandleMenuClicked()
        {
            _audio.PlayUi(_tapClip);
            _flow.GoToMenu();
        }
    }
}
