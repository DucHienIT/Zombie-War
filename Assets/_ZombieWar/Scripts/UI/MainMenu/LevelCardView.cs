using System;
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

        [Header("Locked")]
        [SerializeField] private float _lockedAlpha = 0.45f;

        private Action _onPlayRequested;

        private void Awake()
        {
            bool missing = _playButton == null || _artwork == null || _artworkGroup == null || _chapterText == null
                           || _nameText == null || _bestScoreText == null || _bestScoreGroup == null || _lockedBadge == null;
            if (missing)
            {
                Debug.LogError($"{LogPrefix} LevelCardView has an unassigned reference.", this);
                return;
            }

            _playButton.onClick.AddListener(HandlePlayClicked);
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

        private void HandlePlayClicked() => _onPlayRequested?.Invoke();
    }
}
