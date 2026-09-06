using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    // The chapter card of the battle page: title, artwork, name, best score and the START button.
    public sealed class LevelCardView : MonoBehaviour
    {
        private const string LogPrefix = "[UI]";

        [SerializeField] private Button _playButton;
        [SerializeField] private Image _artwork;
        [SerializeField] private CanvasGroup _artworkGroup;
        [SerializeField] private TMP_Text _chapterText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _bestScoreText;
        [SerializeField] private GameObject _bestScoreGroup;
        [SerializeField] private GameObject _lockedBadge;
        // The whole card, faded and slid when the page steps to another chapter.
        [SerializeField] private CanvasGroup _cardGroup;

        [Header("Locked")]
        [SerializeField] private float _lockedAlpha = 0.45f;

        [Header("Swap")]
        [SerializeField] private float _slide = 220f;
        // Used when the page itself opens, where there is no step direction to follow.
        [SerializeField] private float _openRise = 50f;
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private Ease _ease = Ease.OutCubic;

        private Action _onPlayRequested;
        private RectTransform _rect;
        private Vector2 _restPosition;

        private void Awake()
        {
            bool missing = _playButton == null || _artwork == null || _artworkGroup == null || _chapterText == null
                           || _nameText == null || _bestScoreText == null || _bestScoreGroup == null || _lockedBadge == null
                           || _cardGroup == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} LevelCardView has an unassigned reference.", this);
                return;
            }

            _rect = (RectTransform)transform;
            _restPosition = _rect.anchoredPosition;
            _playButton.onClick.AddListener(HandlePlayClicked);
        }

        private void OnDisable()
        {
            if (_rect == null)
            {
                return;
            }

            _rect.DOKill();
            _cardGroup.DOKill();
            _rect.anchoredPosition = _restPosition;
            _cardGroup.alpha = 1f;
        }

        public void Bind(in LevelCardData data, Action onPlayRequested)
        {
            _onPlayRequested = onPlayRequested;
            _chapterText.SetText("CHAPTER {0}", data.Index);
            _nameText.text = data.DisplayName;
            if (data.Artwork != null)
            {
                _artwork.sprite = data.Artwork;
            }

            _bestScoreText.SetText("{0}", data.BestScore);
            _playButton.interactable = data.Unlocked;
            _lockedBadge.SetActive(!data.Unlocked);
            _bestScoreGroup.SetActive(data.Unlocked && data.BestScore > 0);
            _artworkGroup.alpha = data.Unlocked ? 1f : _lockedAlpha;
        }

        // The new chapter flies in over the old one: binding is instant, so spamming the arrows
        // only restarts this tween instead of leaving a half-swapped card behind.
        public void PlayEnter(int direction)
        {
            if (_rect == null)
            {
                return;
            }

            _rect.DOKill();
            _cardGroup.DOKill();
            Vector2 offset = direction == 0 ? new Vector2(0f, -_openRise) : new Vector2(direction * _slide, 0f);
            _rect.anchoredPosition = _restPosition + offset;
            _cardGroup.alpha = 0f;
            _rect.DOAnchorPos(_restPosition, _duration).SetEase(_ease).SetUpdate(true).SetLink(gameObject);
            _cardGroup.DOFade(1f, _duration).SetUpdate(true).SetLink(gameObject);
        }

        private void HandlePlayClicked() => _onPlayRequested?.Invoke();
    }
}
